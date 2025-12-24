using System;
using System.Collections.Generic;
using System.Linq;
using AutoScheduling3.Constants;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine.Strategies
{
    /// <summary>
    /// 全局MRV策略：在所有天数的所有时段中选择候选人员最少的位置
    /// 对应需求3.1-3.5
    /// </summary>
    public class GlobalMRVStrategy : ISchedulingStrategy
    {
        private readonly FeasibilityTensor _tensor;
        private readonly SchedulingContext _context;
        private readonly PeriodMapper _periodMapper;

        /// <summary>
        /// 候选人员数缓存：[哨位索引, 全局时段索引] -> 候选人员数
        /// </summary>
        private readonly int[,] _candidateCounts;

        /// <summary>
        /// 已分配标记：[哨位索引, 全局时段索引] -> 是否已分配
        /// </summary>
        private readonly bool[,] _assignedFlags;

        public GlobalMRVStrategy(FeasibilityTensor tensor, SchedulingContext context, PeriodMapper periodMapper)
        {
            _tensor = tensor ?? throw new ArgumentNullException(nameof(tensor));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _periodMapper = periodMapper ?? throw new ArgumentNullException(nameof(periodMapper));

            // 验证张量的时段数与PeriodMapper的总时段数一致
            if (_tensor.PeriodCount != _periodMapper.TotalPeriods)
            {
                throw new ArgumentException(
                    $"张量的时段数({_tensor.PeriodCount})与PeriodMapper的总时段数({_periodMapper.TotalPeriods})不一致");
            }

            _candidateCounts = new int[tensor.PositionCount, tensor.PeriodCount];
            _assignedFlags = new bool[tensor.PositionCount, tensor.PeriodCount];

            InitializeCandidateCounts();
        }

        /// <summary>
        /// 初始化候选人员数缓存
        /// </summary>
        public void InitializeCandidateCounts()
        {
            for (int posIdx = 0; posIdx < _tensor.PositionCount; posIdx++)
            {
                for (int globalPeriod = 0; globalPeriod < _tensor.PeriodCount; globalPeriod++)
                {
                    _candidateCounts[posIdx, globalPeriod] = _tensor.CountFeasiblePersons(posIdx, globalPeriod);
                    _assignedFlags[posIdx, globalPeriod] = false;
                }
            }
        }

        /// <summary>
        /// 选择候选人员最少的未分配位置（全局MRV）
        /// 对应需求3.1
        /// </summary>
        /// <returns>选中的(哨位索引, 全局时段索引)，如果所有位置已分配则返回(-1, -1)</returns>
        public (int positionIdx, int periodIdx) SelectNextSlot()
        {
            int minCandidates = int.MaxValue;
            int selectedPosIdx = -1;
            int selectedGlobalPeriodIdx = -1;

            // 遍历所有未分配位置（全局范围）
            // 对应需求3.1: 在所有天数的所有时段中寻找候选人员最少的位置
            for (int globalPeriod = 0; globalPeriod < _tensor.PeriodCount; globalPeriod++)
            {
                for (int posIdx = 0; posIdx < _tensor.PositionCount; posIdx++)
                {
                    if (_assignedFlags[posIdx, globalPeriod])
                        continue;  // 已分配，跳过

                    int candidates = _candidateCounts[posIdx, globalPeriod];

                    // 选择候选人员最少且大于0的位置
                    if (candidates > 0 && candidates < minCandidates)
                    {
                        minCandidates = candidates;
                        selectedPosIdx = posIdx;
                        selectedGlobalPeriodIdx = globalPeriod;
                    }
                }
            }

            return (selectedPosIdx, selectedGlobalPeriodIdx);
        }

        /// <summary>
        /// 标记位置已分配
        /// </summary>
        public void MarkAsAssigned(int positionIdx, int globalPeriodIdx)
        {
            _assignedFlags[positionIdx, globalPeriodIdx] = true;
        }

        /// <summary>
        /// 增量更新候选人员数（在分配后调用）
        /// 对应需求3.3, 3.4
        /// </summary>
        /// <param name="assignedPosIdx">已分配的哨位索引</param>
        /// <param name="assignedGlobalPeriodIdx">已分配的全局时段索引</param>
        /// <param name="assignedPersonIdx">已分配的人员索引</param>
        public void UpdateCandidateCountsAfterAssignment(
            int assignedPosIdx, int assignedGlobalPeriodIdx, int assignedPersonIdx)
        {
            // 1. "一人一哨"约束：该人员在当前时段的其他哨位候选数-1
            for (int posIdx = 0; posIdx < _tensor.PositionCount; posIdx++)
            {
                if (posIdx != assignedPosIdx && !_assignedFlags[posIdx, assignedGlobalPeriodIdx])
                {
                    if (_tensor[posIdx, assignedGlobalPeriodIdx, assignedPersonIdx])
                    {
                        _candidateCounts[posIdx, assignedGlobalPeriodIdx]--;
                    }
                }
            }

            // 2. "单人上哨"约束：当前哨位时段的其他候选人员已不可行
            _candidateCounts[assignedPosIdx, assignedGlobalPeriodIdx] = 0;

            // 3. "时段不连续"约束：该人员在相邻时段的候选数需要更新（支持跨日）
            // 对应需求1.1, 1.3, 1.4
            UpdateAdjacentPeriodCounts(assignedPersonIdx, assignedGlobalPeriodIdx);

            // 4. "夜哨唯一"约束：如果是夜哨时段，更新同一夜哨周期的其他时段（支持跨日）
            // 对应需求1.5, 7.3
            UpdateNightShiftCounts(assignedPersonIdx, assignedGlobalPeriodIdx);
        }

        /// <summary>
        /// 更新相邻时段的候选人员数（支持跨日）
        /// 对应需求1.1, 1.3, 1.4, 10.3
        /// </summary>
        private void UpdateAdjacentPeriodCounts(int personIdx, int globalPeriodIdx)
        {
            // 边界检查：验证全局时段索引是否有效
            // 对应需求10.3
            if (!_periodMapper.IsValidGlobalPeriod(globalPeriodIdx))
                return;

            // 前一个时段（可能跨日）
            // 已知 globalPeriodIdx 有效，只需检查 > 0
            if (globalPeriodIdx > 0)
            {
                int prevGlobalPeriod = globalPeriodIdx - 1;
                for (int posIdx = 0; posIdx < _tensor.PositionCount; posIdx++)
                {
                    if (!_assignedFlags[posIdx, prevGlobalPeriod] &&
                        _tensor[posIdx, prevGlobalPeriod, personIdx])
                    {
                        _candidateCounts[posIdx, prevGlobalPeriod]--;
                    }
                }
            }
            // else: 第一个全局时段，没有前一时段，无需更新

            // 后一个时段（可能跨日）
            // 已知 globalPeriodIdx 有效，只需检查 < TotalPeriods - 1
            if (globalPeriodIdx < _tensor.PeriodCount - 1)
            {
                int nextGlobalPeriod = globalPeriodIdx + 1;
                for (int posIdx = 0; posIdx < _tensor.PositionCount; posIdx++)
                {
                    if (!_assignedFlags[posIdx, nextGlobalPeriod] &&
                        _tensor[posIdx, nextGlobalPeriod, personIdx])
                    {
                        _candidateCounts[posIdx, nextGlobalPeriod]--;
                    }
                }
            }
            // else: 最后一个全局时段，没有后一时段，无需更新
        }

        /// <summary>
        /// 更新夜哨时段的候选人员数（支持跨日夜哨）
        /// 对应需求1.5, 7.3, 10.3
        /// </summary>
        private void UpdateNightShiftCounts(int personIdx, int globalPeriodIdx)
        {
            // 边界检查：验证全局时段索引是否有效
            // 对应需求10.3
            if (!_periodMapper.IsValidGlobalPeriod(globalPeriodIdx))
                return;

            var (dayIndex, localPeriod) = _periodMapper.ToLocalPeriod(globalPeriodIdx);

            // 夜哨时段：11, 0, 1, 2
            if (!SchedulingConstants.NightShiftPeriods.Contains(localPeriod))
                return;

            // 识别跨日的夜哨周期
            // 时段11属于当天夜哨的开始，时段0-2属于次日夜哨的延续
            int nightCycleDay = localPeriod == 11 ? dayIndex : dayIndex - 1;

            // 边界条件特殊处理：
            // 1. 如果是第一天的时段0-2，nightCycleDay会是-1，表示前一天不存在
            // 2. 如果是最后一天的时段11，次日时段0-2不存在
            // 对应需求10.3

            // 更新同一夜哨周期的其他时段
            foreach (var np in SchedulingConstants.NightShiftPeriods)
            {
                int targetDay = np == SchedulingConstants.MaxPeriodIndex ? nightCycleDay : nightCycleDay + 1;

                // 边界条件检查：确保目标日期在有效范围内
                // 对应需求10.3
                if (!_periodMapper.IsValidDayIndex(targetDay))
                    continue; // 目标日期超出范围，跳过该时段

                int targetGlobalPeriod = _periodMapper.ToGlobalPeriod(targetDay, np);

                // 跳过当前时段
                if (targetGlobalPeriod == globalPeriodIdx)
                    continue;

                // ToGlobalPeriod 已确保返回有效索引，无需双重验证
                for (int posIdx = 0; posIdx < _tensor.PositionCount; posIdx++)
                {
                    if (!_assignedFlags[posIdx, targetGlobalPeriod] &&
                        _tensor[posIdx, targetGlobalPeriod, personIdx])
                    {
                        _candidateCounts[posIdx, targetGlobalPeriod]--;
                    }
                }
            }
        }

        /// <summary>
        /// 获取指定位置的候选人员数
        /// </summary>
        public int GetCandidateCount(int positionIdx, int globalPeriodIdx)
        {
            return _candidateCounts[positionIdx, globalPeriodIdx];
        }

        /// <summary>
        /// 检查是否所有位置都已分配
        /// </summary>
        public bool AllAssigned()
        {
            for (int posIdx = 0; posIdx < _tensor.PositionCount; posIdx++)
            {
                for (int globalPeriod = 0; globalPeriod < _tensor.PeriodCount; globalPeriod++)
                {
                    if (!_assignedFlags[posIdx, globalPeriod])
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取所有未分配且无候选人员的位置（无解检测）
        /// </summary>
        public List<(int positionIdx, int periodIdx)> GetUnassignedWithNoCandidates()
        {
            var result = new List<(int, int)>();

            for (int posIdx = 0; posIdx < _tensor.PositionCount; posIdx++)
            {
                for (int globalPeriod = 0; globalPeriod < _tensor.PeriodCount; globalPeriod++)
                {
                    if (!_assignedFlags[posIdx, globalPeriod] && _candidateCounts[posIdx, globalPeriod] == 0)
                    {
                        result.Add((posIdx, globalPeriod));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取所有未分配的位置
        /// </summary>
        public List<(int positionIdx, int periodIdx)> GetUnassignedSlots()
        {
            var result = new List<(int, int)>();

            for (int posIdx = 0; posIdx < _tensor.PositionCount; posIdx++)
            {
                for (int globalPeriod = 0; globalPeriod < _tensor.PeriodCount; globalPeriod++)
                {
                    if (!_assignedFlags[posIdx, globalPeriod])
                    {
                        result.Add((posIdx, globalPeriod));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取全局MRV统计信息
        /// </summary>
        public string GetStatistics()
        {
            int totalSlots = _tensor.PositionCount * _tensor.PeriodCount;
            int assignedCount = 0;
            int zeroCandidate = 0;
            int minCandidates = int.MaxValue;
            int maxCandidates = 0;

            for (int posIdx = 0; posIdx < _tensor.PositionCount; posIdx++)
            {
                for (int globalPeriod = 0; globalPeriod < _tensor.PeriodCount; globalPeriod++)
                {
                    if (_assignedFlags[posIdx, globalPeriod])
                    {
                        assignedCount++;
                    }
                    else
                    {
                        int count = _candidateCounts[posIdx, globalPeriod];
                        if (count == 0) zeroCandidate++;
                        if (count > 0 && count < minCandidates) minCandidates = count;
                        if (count > maxCandidates) maxCandidates = count;
                    }
                }
            }

            int totalDays = _periodMapper.TotalDays;
            return $"总位置: {totalSlots} ({totalDays}天 × {_periodMapper.PeriodsPerDay}时段 × {_tensor.PositionCount}哨位), " +
                   $"已分配: {assignedCount}, 无候选: {zeroCandidate}, " +
                   $"最少候选: {(minCandidates == int.MaxValue ? 0 : minCandidates)}, 最多候选: {maxCandidates}";
        }

        /// <summary>
        /// 获取候选计数数组的副本（用于状态快照）
        /// </summary>
        public int[,] GetCandidateCountsCopy()
        {
            return (int[,])_candidateCounts.Clone();
        }

        /// <summary>
        /// 获取分配标记数组的副本（用于状态快照）
        /// </summary>
        public bool[,] GetAssignedFlagsCopy()
        {
            return (bool[,])_assignedFlags.Clone();
        }

        /// <summary>
        /// 获取候选计数数组的引用（用于状态恢复）
        /// </summary>
        public int[,] GetCandidateCountsReference()
        {
            return _candidateCounts;
        }

        /// <summary>
        /// 获取分配标记数组的引用（用于状态恢复）
        /// </summary>
        public bool[,] GetAssignedFlagsReference()
        {
            return _assignedFlags;
        }

        /// <summary>
        /// 检查全局时段索引是否在边界位置
        /// 对应需求10.3
        /// </summary>
        /// <param name="globalPeriodIdx">全局时段索引</param>
        /// <returns>边界信息：(是否是第一个时段, 是否是最后一个时段)</returns>
        public (bool isFirst, bool isLast) CheckBoundaryPosition(int globalPeriodIdx)
        {
            bool isFirst = globalPeriodIdx == 0;
            bool isLast = globalPeriodIdx == _tensor.PeriodCount - 1;
            return (isFirst, isLast);
        }

        /// <summary>
        /// 获取有效的相邻时段索引
        /// 对应需求10.3
        /// </summary>
        /// <param name="globalPeriodIdx">当前全局时段索引</param>
        /// <returns>有效的相邻时段索引列表（可能包含前一时段和/或后一时段）</returns>
        public List<int> GetValidAdjacentPeriods(int globalPeriodIdx)
        {
            var adjacentPeriods = new List<int>();

            // 前一时段（如果存在）
            if (globalPeriodIdx > 0)
            {
                int prevPeriod = globalPeriodIdx - 1;
                if (_periodMapper.IsValidGlobalPeriod(prevPeriod))
                {
                    adjacentPeriods.Add(prevPeriod);
                }
            }

            // 后一时段（如果存在）
            if (globalPeriodIdx < _tensor.PeriodCount - 1)
            {
                int nextPeriod = globalPeriodIdx + 1;
                if (_periodMapper.IsValidGlobalPeriod(nextPeriod))
                {
                    adjacentPeriods.Add(nextPeriod);
                }
            }

            return adjacentPeriods;
        }
    }
}
