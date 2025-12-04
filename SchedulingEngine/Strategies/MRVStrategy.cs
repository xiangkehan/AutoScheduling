using System;
using System.Collections.Generic;
using System.Linq;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine.Strategies
{
    /// <summary>
    /// MRV（Minimum Remaining Values）启发式策略：
    /// 优先选择候选人员最少的哨位-时段进行分配，减少无解风险
    /// </summary>
    public class MRVStrategy
    {
        private readonly FeasibilityTensor _tensor;
        private readonly SchedulingContext _context;

        /// <summary>
        /// 候选人员数缓存：[哨位索引, 时段索引] -> 候选人员数
        /// </summary>
        private readonly int[,] _candidateCounts;

        /// <summary>
        /// 已分配标记：[哨位索引, 时段索引] -> 是否已分配
        /// </summary>
        private readonly bool[,] _assignedFlags;

        public MRVStrategy(FeasibilityTensor tensor, SchedulingContext context)
        {
            _tensor = tensor ?? throw new ArgumentNullException(nameof(tensor));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            _candidateCounts = new int[tensor.PositionCount, tensor.PeriodCount];
            _assignedFlags = new bool[tensor.PositionCount, tensor.PeriodCount];

            InitializeCandidateCounts();
        }

        /// <summary>
        /// 初始化候选人员数缓存
        /// </summary>
        private void InitializeCandidateCounts()
        {
            for (int x = 0; x < _tensor.PositionCount; x++)
            {
                for (int y = 0; y < _tensor.PeriodCount; y++)
                {
                    _candidateCounts[x, y] = _tensor.CountFeasiblePersons(x, y);
                    _assignedFlags[x, y] = false;
                }
            }
        }

        /// <summary>
        /// 选择候选人员最少的未分配位置（MRV核心逻辑）
        /// </summary>
        /// <returns>选中的(哨位索引, 时段索引)，如果所有位置已分配则返回(-1, -1)</returns>
        public (int PositionIdx, int PeriodIdx) SelectNextSlot()
        {
            int minCandidates = int.MaxValue;
            int selectedPosIdx = -1;
            int selectedPeriodIdx = -1;

            // 遍历所有未分配位置，找到候选人员最少的
            for (int y = 0; y < _tensor.PeriodCount; y++)  // 优先按时段顺序
            {
                for (int x = 0; x < _tensor.PositionCount; x++)  // 再按哨位顺序
                {
                    if (_assignedFlags[x, y])
                        continue;  // 已分配，跳过

                    int candidates = _candidateCounts[x, y];
                    
                    // 选择候选人员最少且大于0的位置
                    if (candidates > 0 && candidates < minCandidates)
                    {
                        minCandidates = candidates;
                        selectedPosIdx = x;
                        selectedPeriodIdx = y;
                    }
                }
            }

            return (selectedPosIdx, selectedPeriodIdx);
        }

        /// <summary>
        /// 标记位置已分配
        /// </summary>
        public void MarkAsAssigned(int positionIdx, int periodIdx)
        {
            _assignedFlags[positionIdx, periodIdx] = true;
        }

        /// <summary>
        /// 增量更新候选人员数（在分配后调用）
        /// </summary>
        /// <param name="assignedPosIdx">已分配的哨位索引</param>
        /// <param name="assignedPeriodIdx">已分配的时段索引</param>
        /// <param name="assignedPersonIdx">已分配的人员索引</param>
        public void UpdateCandidateCountsAfterAssignment(
            int assignedPosIdx, int assignedPeriodIdx, int assignedPersonIdx)
        {
            // 1. "一人一哨"约束：该人员在当前时段的其他哨位候选数-1
            for (int x = 0; x < _tensor.PositionCount; x++)
            {
                if (x != assignedPosIdx && !_assignedFlags[x, assignedPeriodIdx])
                {
                    if (_tensor[x, assignedPeriodIdx, assignedPersonIdx])
                    {
                        _candidateCounts[x, assignedPeriodIdx]--;
                    }
                }
            }

            // 2. "单人上哨"约束：当前哨位时段的其他候选人员已不可行
            // 这个在分配时已经设置为0了，这里标记已分配即可
            _candidateCounts[assignedPosIdx, assignedPeriodIdx] = 0;

            // 3. "时段不连续"约束：该人员在相邻时段的候选数可能需要更新
            UpdateAdjacentPeriodCounts(assignedPersonIdx, assignedPeriodIdx);

            // 4. "夜哨唯一"约束：如果是夜哨时段，更新同一晚其他夜哨时段
            int[] nightPeriods = { 11, 0, 1, 2 };
            if (nightPeriods.Contains(assignedPeriodIdx))
            {
                UpdateNightShiftCounts(assignedPersonIdx, assignedPeriodIdx);
            }
        }

        /// <summary>
        /// 更新相邻时段的候选人员数
        /// </summary>
        private void UpdateAdjacentPeriodCounts(int personIdx, int periodIdx)
        {
            // 前一个时段
            if (periodIdx > 0)
            {
                for (int x = 0; x < _tensor.PositionCount; x++)
                {
                    if (!_assignedFlags[x, periodIdx - 1] && _tensor[x, periodIdx - 1, personIdx])
                    {
                        _candidateCounts[x, periodIdx - 1]--;
                    }
                }
            }

            // 后一个时段
            if (periodIdx < _tensor.PeriodCount - 1)
            {
                for (int x = 0; x < _tensor.PositionCount; x++)
                {
                    if (!_assignedFlags[x, periodIdx + 1] && _tensor[x, periodIdx + 1, personIdx])
                    {
                        _candidateCounts[x, periodIdx + 1]--;
                    }
                }
            }

            // 跨日情况：时段11和时段0相邻
            if (periodIdx == 11)
            {
                // 时段11分配后，次日时段0受影响（需要在日期推进时处理）
            }
            else if (periodIdx == 0)
            {
                // 时段0分配后，前一日时段11受影响（需要在日期推进时处理）
            }
        }

        /// <summary>
        /// 更新夜哨时段的候选人员数
        /// </summary>
        private void UpdateNightShiftCounts(int personIdx, int assignedNightPeriod)
        {
            int[] nightPeriods = { 11, 0, 1, 2 };
            
            foreach (var nightPeriod in nightPeriods)
            {
                if (nightPeriod == assignedNightPeriod)
                    continue;

                for (int x = 0; x < _tensor.PositionCount; x++)
                {
                    if (!_assignedFlags[x, nightPeriod] && _tensor[x, nightPeriod, personIdx])
                    {
                        _candidateCounts[x, nightPeriod]--;
                    }
                }
            }
        }

        /// <summary>
        /// 获取指定位置的候选人员数
        /// </summary>
        public int GetCandidateCount(int positionIdx, int periodIdx)
        {
            return _candidateCounts[positionIdx, periodIdx];
        }

        /// <summary>
        /// 检查是否所有位置都已分配
        /// </summary>
        public bool AllAssigned()
        {
            for (int x = 0; x < _tensor.PositionCount; x++)
            {
                for (int y = 0; y < _tensor.PeriodCount; y++)
                {
                    if (!_assignedFlags[x, y])
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取所有未分配且无候选人员的位置（无解检测）
        /// </summary>
        public List<(int PositionIdx, int PeriodIdx)> GetUnassignedWithNoCandidates()
        {
            var result = new List<(int, int)>();

            for (int x = 0; x < _tensor.PositionCount; x++)
            {
                for (int y = 0; y < _tensor.PeriodCount; y++)
                {
                    if (!_assignedFlags[x, y] && _candidateCounts[x, y] == 0)
                    {
                        result.Add((x, y));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取所有未分配的位置
        /// </summary>
        public List<(int PositionIdx, int PeriodIdx)> GetUnassignedSlots()
        {
            var result = new List<(int, int)>();

            for (int x = 0; x < _tensor.PositionCount; x++)
            {
                for (int y = 0; y < _tensor.PeriodCount; y++)
                {
                    if (!_assignedFlags[x, y])
                    {
                        result.Add((x, y));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取MRV统计信息
        /// </summary>
        public string GetStatistics()
        {
            int totalSlots = _tensor.PositionCount * _tensor.PeriodCount;
            int assignedCount = 0;
            int zeroCandidate = 0;
            int minCandidates = int.MaxValue;
            int maxCandidates = 0;

            for (int x = 0; x < _tensor.PositionCount; x++)
            {
                for (int y = 0; y < _tensor.PeriodCount; y++)
                {
                    if (_assignedFlags[x, y])
                    {
                        assignedCount++;
                    }
                    else
                    {
                        int count = _candidateCounts[x, y];
                        if (count == 0) zeroCandidate++;
                        if (count > 0 && count < minCandidates) minCandidates = count;
                        if (count > maxCandidates) maxCandidates = count;
                    }
                }
            }

            return $"总位置: {totalSlots}, 已分配: {assignedCount}, 无候选: {zeroCandidate}, " +
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
