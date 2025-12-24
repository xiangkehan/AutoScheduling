using System;
using System.Collections.Generic;
using System.Linq;
using AutoScheduling3.Models;

namespace AutoScheduling3.SchedulingEngine.Core
{
    /// <summary>
    /// 状态快照：保存分配前的系统状态，用于回溯恢复
    /// 使用写时复制策略优化内存使用
    /// 对应需求: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4
    /// </summary>
    public class StateSnapshot
    {
        /// <summary>
        /// 可行性张量的状态（使用写时复制）
        /// 存储格式：压缩的字节数组
        /// </summary>
        public byte[] TensorState { get; set; }

        /// <summary>
        /// MRV策略的候选计数
        /// [哨位索引, 时段索引] -> 候选人员数
        /// </summary>
        public int[,] CandidateCounts { get; set; }

        /// <summary>
        /// MRV策略的分配标记
        /// [哨位索引, 时段索引] -> 是否已分配
        /// </summary>
        public bool[,] AssignedFlags { get; set; }

        /// <summary>
        /// 调度上下文的分配记录（单日模式）
        /// Key: (时段索引, 哨位索引), Value: 人员索引
        /// </summary>
        public Dictionary<(int period, int position), int> Assignments { get; set; }

        /// <summary>
        /// 多日分配记录（全局模式）
        /// Key: 日期, Value: 该日期的分配记录 (period, position) -> personIdx
        /// 对应需求: 1.1, 1.2, 1.3
        /// </summary>
        public Dictionary<DateTime, Dictionary<(int period, int position), int>>? MultiDayAssignments { get; set; }

        /// <summary>
        /// 人员评分状态快照
        /// Key: personId, Value: 该人员的评分状态快照
        /// 对应需求: 2.1, 2.2, 2.3
        /// </summary>
        public Dictionary<int, PersonScoreStateSnapshot>? PersonScoreStatesSnapshot { get; set; }

        /// <summary>
        /// 人员分配时间戳快照
        /// Key: personId, Value: 该人员的分配时间戳集合（深拷贝）
        /// 对应需求: 3.1, 3.3
        /// </summary>
        public Dictionary<int, SortedSet<int>>? PersonAssignmentTimestampsSnapshot { get; set; }

        /// <summary>
        /// 人员分配详情快照
        /// Key: personId, Value: timestamp -> (date, period, positionIdx)
        /// 对应需求: 3.2, 3.4
        /// </summary>
        public Dictionary<int, Dictionary<int, (DateTime date, int period, int positionIdx)>>? PersonAssignmentDetailsSnapshot { get; set; }

        /// <summary>
        /// 是否为全局模式快照
        /// 对应需求: 1.1
        /// </summary>
        public bool IsGlobalMode { get; set; }

        /// <summary>
        /// 创建快照的时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 快照深度（用于调试和统计）
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// 是否使用写时复制（共享引用）
        /// </summary>
        private bool _isShared;

        /// <summary>
        /// 引用计数（用于写时复制）
        /// </summary>
        private int _referenceCount;

        public StateSnapshot()
        {
            TensorState = Array.Empty<byte>();
            CandidateCounts = new int[0, 0];
            AssignedFlags = new bool[0, 0];
            Assignments = new Dictionary<(int, int), int>();
            MultiDayAssignments = null;
            PersonScoreStatesSnapshot = null;
            PersonAssignmentTimestampsSnapshot = null;
            PersonAssignmentDetailsSnapshot = null;
            IsGlobalMode = false;
            Timestamp = DateTime.UtcNow;
            Depth = 0;
            _isShared = false;
            _referenceCount = 1;
        }

        /// <summary>
        /// 从可行性张量创建快照（兼容旧接口）
        /// </summary>
        public static StateSnapshot CreateFromTensor(
            FeasibilityTensor tensor,
            int[,] candidateCounts,
            bool[,] assignedFlags,
            Dictionary<DateTime, int[,]> contextAssignments,
            DateTime currentDate,
            int depth = 0)
        {
            return CreateFromTensor(tensor, candidateCounts, assignedFlags, contextAssignments, currentDate, depth, false, null);
        }

        /// <summary>
        /// 从可行性张量和上下文创建快照（支持全局模式）
        /// 对应需求: 1.1, 1.3, 2.1, 3.1, 3.2
        /// </summary>
        /// <param name="tensor">可行性张量</param>
        /// <param name="candidateCounts">候选计数</param>
        /// <param name="assignedFlags">分配标记</param>
        /// <param name="contextAssignments">上下文分配记录</param>
        /// <param name="currentDate">当前日期</param>
        /// <param name="depth">快照深度</param>
        /// <param name="isGlobalMode">是否为全局模式</param>
        /// <param name="context">调度上下文（全局模式下需要）</param>
        public static StateSnapshot CreateFromTensor(
            FeasibilityTensor tensor,
            int[,] candidateCounts,
            bool[,] assignedFlags,
            Dictionary<DateTime, int[,]> contextAssignments,
            DateTime currentDate,
            int depth,
            bool isGlobalMode,
            SchedulingContext? context)
        {
            if (tensor == null) throw new ArgumentNullException(nameof(tensor));
            if (candidateCounts == null) throw new ArgumentNullException(nameof(candidateCounts));
            if (assignedFlags == null) throw new ArgumentNullException(nameof(assignedFlags));
            if (contextAssignments == null) throw new ArgumentNullException(nameof(contextAssignments));

            var snapshot = new StateSnapshot
            {
                Depth = depth,
                Timestamp = DateTime.UtcNow,
                IsGlobalMode = isGlobalMode
            };

            // 序列化可行性张量状态
            snapshot.TensorState = tensor.SerializeState();

            // 深拷贝候选计数
            snapshot.CandidateCounts = (int[,])candidateCounts.Clone();

            // 深拷贝分配标记
            snapshot.AssignedFlags = (bool[,])assignedFlags.Clone();

            // 深拷贝分配记录
            snapshot.Assignments = new Dictionary<(int, int), int>();

            if (isGlobalMode)
            {
                // 全局模式：保存所有日期的分配记录
                snapshot.MultiDayAssignments = new Dictionary<DateTime, Dictionary<(int period, int position), int>>();
                
                foreach (var kvp in contextAssignments)
                {
                    var dateAssignments = kvp.Value;
                    var dayAssignments = new Dictionary<(int, int), int>();
                    
                    for (int period = 0; period < dateAssignments.GetLength(0); period++)
                    {
                        for (int position = 0; position < dateAssignments.GetLength(1); position++)
                        {
                            int personIdx = dateAssignments[period, position];
                            if (personIdx >= 0)
                            {
                                dayAssignments[(period, position)] = personIdx;
                            }
                        }
                    }
                    
                    snapshot.MultiDayAssignments[kvp.Key] = dayAssignments;
                }

                // 全局模式下保存人员状态
                if (context != null)
                {
                    // 深拷贝 PersonScoreStates
                    snapshot.PersonScoreStatesSnapshot = context.CreatePersonScoreStatesSnapshot();

                    // 深拷贝 PersonAssignmentTimestamps
                    snapshot.PersonAssignmentTimestampsSnapshot = context.CreatePersonAssignmentTimestampsSnapshot();

                    // 深拷贝 PersonAssignmentDetails
                    snapshot.PersonAssignmentDetailsSnapshot = context.CreatePersonAssignmentDetailsSnapshot();
                }
            }
            else
            {
                // 单日模式：只保存当前日期的分配记录
                if (contextAssignments.TryGetValue(currentDate, out var dateAssignments))
                {
                    for (int period = 0; period < dateAssignments.GetLength(0); period++)
                    {
                        for (int position = 0; position < dateAssignments.GetLength(1); position++)
                        {
                            int personIdx = dateAssignments[period, position];
                            if (personIdx >= 0)
                            {
                                snapshot.Assignments[(period, position)] = personIdx;
                            }
                        }
                    }
                }
            }

            return snapshot;
        }

        /// <summary>
        /// 恢复状态到可行性张量和MRV策略（兼容旧接口）
        /// </summary>
        public void RestoreToTensor(
            FeasibilityTensor tensor,
            int[,] candidateCounts,
            bool[,] assignedFlags,
            Dictionary<DateTime, int[,]> contextAssignments,
            DateTime currentDate)
        {
            RestoreToContext(tensor, candidateCounts, assignedFlags, contextAssignments, currentDate, null);
        }

        /// <summary>
        /// 恢复状态到可行性张量、MRV策略和上下文
        /// 对应需求: 1.2, 2.2, 2.4, 3.3, 3.4
        /// </summary>
        /// <param name="tensor">可行性张量</param>
        /// <param name="candidateCounts">候选计数</param>
        /// <param name="assignedFlags">分配标记</param>
        /// <param name="contextAssignments">上下文分配记录</param>
        /// <param name="currentDate">当前日期</param>
        /// <param name="context">调度上下文（全局模式下需要）</param>
        public void RestoreToContext(
            FeasibilityTensor tensor,
            int[,] candidateCounts,
            bool[,] assignedFlags,
            Dictionary<DateTime, int[,]> contextAssignments,
            DateTime currentDate,
            SchedulingContext? context)
        {
            if (tensor == null) throw new ArgumentNullException(nameof(tensor));
            if (candidateCounts == null) throw new ArgumentNullException(nameof(candidateCounts));
            if (assignedFlags == null) throw new ArgumentNullException(nameof(assignedFlags));
            if (contextAssignments == null) throw new ArgumentNullException(nameof(contextAssignments));

            // 反序列化可行性张量状态
            tensor.DeserializeState(TensorState);

            // 恢复候选计数
            Array.Copy(CandidateCounts, candidateCounts, CandidateCounts.Length);

            // 恢复分配标记
            Array.Copy(AssignedFlags, assignedFlags, AssignedFlags.Length);

            if (IsGlobalMode && MultiDayAssignments != null)
            {
                // 全局模式：恢复所有日期的分配记录
                foreach (var kvp in contextAssignments)
                {
                    var dateAssignments = kvp.Value;
                    
                    // 清空当前分配
                    for (int period = 0; period < dateAssignments.GetLength(0); period++)
                    {
                        for (int position = 0; position < dateAssignments.GetLength(1); position++)
                        {
                            dateAssignments[period, position] = -1;
                        }
                    }

                    // 恢复快照中的分配
                    if (MultiDayAssignments.TryGetValue(kvp.Key, out var dayAssignments))
                    {
                        foreach (var assignment in dayAssignments)
                        {
                            var (period, position) = assignment.Key;
                            dateAssignments[period, position] = assignment.Value;
                        }
                    }
                }

                // 恢复人员状态
                if (context != null)
                {
                    // 恢复 PersonScoreStates
                    if (PersonScoreStatesSnapshot != null)
                    {
                        context.RestorePersonScoreStates(PersonScoreStatesSnapshot);
                    }

                    // 恢复 PersonAssignmentTimestamps
                    if (PersonAssignmentTimestampsSnapshot != null)
                    {
                        context.RestorePersonAssignmentTimestamps(PersonAssignmentTimestampsSnapshot);
                    }

                    // 恢复 PersonAssignmentDetails
                    if (PersonAssignmentDetailsSnapshot != null)
                    {
                        context.RestorePersonAssignmentDetails(PersonAssignmentDetailsSnapshot);
                    }

                    // 更新全局平均工作量
                    context.UpdateAverageWorkload();
                }
            }
            else
            {
                // 单日模式：只恢复当前日期的分配记录
                if (contextAssignments.TryGetValue(currentDate, out var dateAssignments))
                {
                    // 清空当前分配
                    for (int period = 0; period < dateAssignments.GetLength(0); period++)
                    {
                        for (int position = 0; position < dateAssignments.GetLength(1); position++)
                        {
                            dateAssignments[period, position] = -1;
                        }
                    }

                    // 恢复快照中的分配
                    foreach (var kvp in Assignments)
                    {
                        var (period, position) = kvp.Key;
                        dateAssignments[period, position] = kvp.Value;
                    }
                }
            }
        }

        /// <summary>
        /// 创建浅拷贝（写时复制）
        /// </summary>
        public StateSnapshot CreateShallowCopy()
        {
            _isShared = true;
            _referenceCount++;

            return new StateSnapshot
            {
                TensorState = TensorState, // 共享引用
                CandidateCounts = CandidateCounts, // 共享引用
                AssignedFlags = AssignedFlags, // 共享引用
                Assignments = Assignments, // 共享引用
                MultiDayAssignments = MultiDayAssignments, // 共享引用
                PersonScoreStatesSnapshot = PersonScoreStatesSnapshot, // 共享引用
                PersonAssignmentTimestampsSnapshot = PersonAssignmentTimestampsSnapshot, // 共享引用
                PersonAssignmentDetailsSnapshot = PersonAssignmentDetailsSnapshot, // 共享引用
                IsGlobalMode = IsGlobalMode,
                Timestamp = Timestamp,
                Depth = Depth,
                _isShared = true,
                _referenceCount = 1
            };
        }

        /// <summary>
        /// 在修改前确保数据已复制（写时复制）
        /// 对应需求: 8.1
        /// </summary>
        private void EnsureWritable()
        {
            if (_isShared && _referenceCount > 1)
            {
                // 执行深拷贝
                TensorState = (byte[])TensorState.Clone();
                CandidateCounts = (int[,])CandidateCounts.Clone();
                AssignedFlags = (bool[,])AssignedFlags.Clone();
                Assignments = new Dictionary<(int, int), int>(Assignments);
                
                // 深拷贝新增字段
                if (MultiDayAssignments != null)
                {
                    var newMultiDay = new Dictionary<DateTime, Dictionary<(int, int), int>>();
                    foreach (var kvp in MultiDayAssignments)
                    {
                        newMultiDay[kvp.Key] = new Dictionary<(int, int), int>(kvp.Value);
                    }
                    MultiDayAssignments = newMultiDay;
                }

                if (PersonScoreStatesSnapshot != null)
                {
                    var newScoreStates = new Dictionary<int, PersonScoreStateSnapshot>();
                    foreach (var kvp in PersonScoreStatesSnapshot)
                    {
                        newScoreStates[kvp.Key] = new PersonScoreStateSnapshot
                        {
                            PersonalId = kvp.Value.PersonalId,
                            TotalAssignments = kvp.Value.TotalAssignments,
                            NightShiftCount = kvp.Value.NightShiftCount,
                            DayShiftCount = kvp.Value.DayShiftCount,
                            WorkloadScore = kvp.Value.WorkloadScore
                        };
                    }
                    PersonScoreStatesSnapshot = newScoreStates;
                }

                if (PersonAssignmentTimestampsSnapshot != null)
                {
                    var newTimestamps = new Dictionary<int, SortedSet<int>>();
                    foreach (var kvp in PersonAssignmentTimestampsSnapshot)
                    {
                        newTimestamps[kvp.Key] = new SortedSet<int>(kvp.Value);
                    }
                    PersonAssignmentTimestampsSnapshot = newTimestamps;
                }

                if (PersonAssignmentDetailsSnapshot != null)
                {
                    var newDetails = new Dictionary<int, Dictionary<int, (DateTime, int, int)>>();
                    foreach (var kvp in PersonAssignmentDetailsSnapshot)
                    {
                        newDetails[kvp.Key] = new Dictionary<int, (DateTime, int, int)>(kvp.Value);
                    }
                    PersonAssignmentDetailsSnapshot = newDetails;
                }
                
                _referenceCount--;
                _isShared = false;
                _referenceCount = 1;
            }
        }

        /// <summary>
        /// 获取快照的内存占用估算（字节）
        /// 对应需求: 8.3
        /// </summary>
        public long GetMemoryUsageEstimate()
        {
            long size = 0;

            // TensorState
            size += TensorState.Length;

            // CandidateCounts
            size += CandidateCounts.Length * sizeof(int);

            // AssignedFlags
            size += AssignedFlags.Length * sizeof(bool);

            // Assignments (估算)
            size += Assignments.Count * (sizeof(int) * 3); // Key (2 ints) + Value (1 int)

            // MultiDayAssignments (估算)
            if (MultiDayAssignments != null)
            {
                foreach (var dayAssignments in MultiDayAssignments.Values)
                {
                    size += sizeof(long); // DateTime
                    size += dayAssignments.Count * (sizeof(int) * 3); // Key (2 ints) + Value (1 int)
                }
            }

            // PersonScoreStatesSnapshot (估算)
            if (PersonScoreStatesSnapshot != null)
            {
                // 每个 PersonScoreStateSnapshot: PersonalId(4) + TotalAssignments(4) + NightShiftCount(4) + DayShiftCount(4) + WorkloadScore(8) = 24 bytes
                size += PersonScoreStatesSnapshot.Count * (sizeof(int) + 24);
            }

            // PersonAssignmentTimestampsSnapshot (估算)
            if (PersonAssignmentTimestampsSnapshot != null)
            {
                foreach (var timestamps in PersonAssignmentTimestampsSnapshot.Values)
                {
                    size += sizeof(int); // Key
                    size += timestamps.Count * sizeof(int); // SortedSet<int> 元素
                }
            }

            // PersonAssignmentDetailsSnapshot (估算)
            if (PersonAssignmentDetailsSnapshot != null)
            {
                foreach (var details in PersonAssignmentDetailsSnapshot.Values)
                {
                    size += sizeof(int); // Key
                    // 每个详情: timestamp(4) + date(8) + period(4) + positionIdx(4) = 20 bytes
                    size += details.Count * 20;
                }
            }

            // 其他字段
            size += sizeof(long) + sizeof(int) + sizeof(bool) * 2 + sizeof(int); // Timestamp, Depth, _isShared, IsGlobalMode, _referenceCount

            return size;
        }

        /// <summary>
        /// 验证快照的完整性
        /// 对应需求: 1.4, 6.4
        /// </summary>
        public bool ValidateIntegrity()
        {
            try
            {
                // 检查基本字段
                if (TensorState == null || CandidateCounts == null || 
                    AssignedFlags == null || Assignments == null)
                {
                    return false;
                }

                // 检查维度一致性
                if (CandidateCounts.GetLength(0) != AssignedFlags.GetLength(0) ||
                    CandidateCounts.GetLength(1) != AssignedFlags.GetLength(1))
                {
                    return false;
                }

                // 检查分配记录的有效性
                foreach (var kvp in Assignments)
                {
                    var (period, position) = kvp.Key;
                    if (period < 0 || period >= 12 || position < 0 || kvp.Value < 0)
                    {
                        return false;
                    }
                }

                // 检查全局模式下的新增字段
                if (IsGlobalMode)
                {
                    // 检查 MultiDayAssignments 有效性
                    if (MultiDayAssignments != null)
                    {
                        foreach (var dayAssignments in MultiDayAssignments.Values)
                        {
                            foreach (var kvp in dayAssignments)
                            {
                                var (period, position) = kvp.Key;
                                if (period < 0 || period >= 12 || position < 0 || kvp.Value < 0)
                                {
                                    return false;
                                }
                            }
                        }
                    }

                    // 检查 PersonScoreStatesSnapshot 有效性
                    if (PersonScoreStatesSnapshot != null)
                    {
                        foreach (var snapshot in PersonScoreStatesSnapshot.Values)
                        {
                            if (snapshot.TotalAssignments < 0 || 
                                snapshot.NightShiftCount < 0 || 
                                snapshot.DayShiftCount < 0 ||
                                snapshot.WorkloadScore < 0)
                            {
                                return false;
                            }
                        }
                    }

                    // 检查 PersonAssignmentTimestampsSnapshot 有效性
                    if (PersonAssignmentTimestampsSnapshot != null)
                    {
                        foreach (var timestamps in PersonAssignmentTimestampsSnapshot.Values)
                        {
                            if (timestamps == null)
                            {
                                return false;
                            }
                        }
                    }

                    // 检查 PersonAssignmentDetailsSnapshot 有效性
                    if (PersonAssignmentDetailsSnapshot != null)
                    {
                        foreach (var details in PersonAssignmentDetailsSnapshot.Values)
                        {
                            if (details == null)
                            {
                                return false;
                            }
                            foreach (var detail in details.Values)
                            {
                                if (detail.period < 0 || detail.period >= 12 || detail.positionIdx < -1)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取快照的描述信息（用于调试）
        /// </summary>
        public override string ToString()
        {
            return $"StateSnapshot [Depth={Depth}, Assignments={Assignments.Count}, " +
                   $"Memory={GetMemoryUsageEstimate() / 1024.0:F2}KB, Time={Timestamp:HH:mm:ss.fff}]";
        }
    }
}
