using System;
using System.Collections.Generic;
using System.Linq;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine.Strategies
{
    /// <summary>
    /// 全局MRV策略：在所有天数的所有时段中选择候选人员最少的位置
    /// 对应需求3.1-3.5
    /// </summary>
    public class GlobalMRVStrategy
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
        private void InitializeCandidateCounts()
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
        public (int positionIdx, int globalPeriodIdx) SelectNextSlot()
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
        /// 对应需求1.1, 1.3, 1.4
        /// </summary>
        private void UpdateAdjacentPeriodCounts(int personIdx, int globalPeriodIdx)
        {
            // 前一个时段（可能跨日）
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

            // 后一个时段（可能跨日）
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
        }

        /// <summary>
        /// 更新夜哨时段的候选人员数（支持跨日夜哨）
        /// 对应需求1.5, 7.3
        /// </summary>
        private void UpdateNightShiftCounts(int personIdx, int globalPeriodIdx)
        {
            var (dayIndex, localPeriod) = _periodMapper.ToLocalPeriod(globalPeriodIdx);

            // 夜哨时段：11, 0, 1, 2
            int[] nightPeriods = { 11, 0, 1, 2 };

            if (!nightPeriods.Contains(localPeriod))
                return;

            // 识别跨日的夜哨周期
            // 时段11属于当天夜哨的开始，时段0-2属于次日夜哨的延续
            int nightCycleDay = localPeriod == 11 ? dayIndex : dayIndex - 1;

            // 更新同一夜哨周期的其他时段
            foreach (var np in nightPeriods)
            {
                int targetDay = np == 11 ? nightCycleDay : nightCycleDay + 1;

                // 确保目标日期在范围内
                if (!_periodMapper.IsValidDayIndex(targetDay))
                    continue;

                int targetGlobalPeriod = _periodMapper.ToGlobalPeriod(targetDay, np);

                if (targetGlobalPeriod == globalPeriodIdx)
                    continue;

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
        public List<(int positionIdx, int globalPeriodIdx)> GetUnassignedWithNoCandidates()
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
        public List<(int positionIdx, int globalPeriodIdx)> GetUnassignedSlots()
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
        internal int[,] GetCandidateCountsReference()
        {
            return _candidateCounts;
        }

        /// <summary>
        /// 获取分配标记数组的引用（用于状态恢复）
        /// </summary>
        internal bool[,] GetAssignedFlagsReference()
        {
            return _assignedFlags;
        }
    }
}
