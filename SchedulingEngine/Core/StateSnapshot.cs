using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.SchedulingEngine.Core
{
    /// <summary>
    /// 状态快照：保存分配前的系统状态，用于回溯恢复
    /// 使用写时复制策略优化内存使用
    /// 对应需求: 2.2, 2.4
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
        /// 调度上下文的分配记录
        /// Key: (时段索引, 哨位索引), Value: 人员索引
        /// </summary>
        public Dictionary<(int period, int position), int> Assignments { get; set; }

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
            Timestamp = DateTime.UtcNow;
            Depth = 0;
            _isShared = false;
            _referenceCount = 1;
        }

        /// <summary>
        /// 从可行性张量创建快照
        /// </summary>
        public static StateSnapshot CreateFromTensor(
            FeasibilityTensor tensor,
            int[,] candidateCounts,
            bool[,] assignedFlags,
            Dictionary<DateTime, int[,]> contextAssignments,
            DateTime currentDate,
            int depth = 0)
        {
            if (tensor == null) throw new ArgumentNullException(nameof(tensor));
            if (candidateCounts == null) throw new ArgumentNullException(nameof(candidateCounts));
            if (assignedFlags == null) throw new ArgumentNullException(nameof(assignedFlags));
            if (contextAssignments == null) throw new ArgumentNullException(nameof(contextAssignments));

            var snapshot = new StateSnapshot
            {
                Depth = depth,
                Timestamp = DateTime.UtcNow
            };

            // 序列化可行性张量状态
            snapshot.TensorState = tensor.SerializeState();

            // 深拷贝候选计数
            snapshot.CandidateCounts = (int[,])candidateCounts.Clone();

            // 深拷贝分配标记
            snapshot.AssignedFlags = (bool[,])assignedFlags.Clone();

            // 深拷贝当前日期的分配记录
            snapshot.Assignments = new Dictionary<(int, int), int>();
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

            return snapshot;
        }

        /// <summary>
        /// 恢复状态到可行性张量和MRV策略
        /// </summary>
        public void RestoreToTensor(
            FeasibilityTensor tensor,
            int[,] candidateCounts,
            bool[,] assignedFlags,
            Dictionary<DateTime, int[,]> contextAssignments,
            DateTime currentDate)
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

            // 恢复分配记录
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
                Timestamp = Timestamp,
                Depth = Depth,
                _isShared = true,
                _referenceCount = 1
            };
        }

        /// <summary>
        /// 在修改前确保数据已复制（写时复制）
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
                
                _referenceCount--;
                _isShared = false;
                _referenceCount = 1;
            }
        }

        /// <summary>
        /// 获取快照的内存占用估算（字节）
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

            // 其他字段
            size += sizeof(long) + sizeof(int) + sizeof(bool) + sizeof(int); // Timestamp, Depth, _isShared, _referenceCount

            return size;
        }

        /// <summary>
        /// 验证快照的完整性
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
