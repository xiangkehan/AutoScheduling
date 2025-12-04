using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.SchedulingEngine.Core
{
    /// <summary>
    /// 分配栈：记录分配历史，支持回溯操作
    /// 对应需求: 2.1, 2.3
    /// </summary>
    public class AssignmentStack
    {
        private readonly Stack<AssignmentRecord> _stack;
        private readonly int _maxDepth;

        /// <summary>
        /// 当前栈深度
        /// </summary>
        public int Depth => _stack.Count;

        /// <summary>
        /// 检查栈是否为空
        /// </summary>
        public bool IsEmpty => _stack.Count == 0;

        /// <summary>
        /// 最大深度限制
        /// </summary>
        public int MaxDepth => _maxDepth;

        /// <summary>
        /// 是否已达到最大深度
        /// </summary>
        public bool IsAtMaxDepth => _stack.Count >= _maxDepth;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxDepth">最大栈深度，默认50</param>
        public AssignmentStack(int maxDepth = 50)
        {
            if (maxDepth <= 0)
            {
                throw new ArgumentException("最大深度必须大于0", nameof(maxDepth));
            }

            _maxDepth = maxDepth;
            _stack = new Stack<AssignmentRecord>(maxDepth);
        }

        /// <summary>
        /// 压入分配记录
        /// </summary>
        /// <param name="record">分配记录</param>
        /// <exception cref="InvalidOperationException">当栈已满时抛出</exception>
        public void Push(AssignmentRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (IsAtMaxDepth)
            {
                throw new InvalidOperationException($"分配栈已达到最大深度 {_maxDepth}");
            }

            if (!record.Validate())
            {
                throw new ArgumentException("分配记录验证失败", nameof(record));
            }

            // 设置记录的深度
            record.Depth = _stack.Count;

            _stack.Push(record);
        }

        /// <summary>
        /// 弹出最近的分配记录
        /// </summary>
        /// <returns>最近的分配记录，如果栈为空则返回null</returns>
        public AssignmentRecord? Pop()
        {
            if (IsEmpty)
            {
                return null;
            }

            return _stack.Pop();
        }

        /// <summary>
        /// 查看栈顶记录但不弹出
        /// </summary>
        /// <returns>栈顶记录，如果栈为空则返回null</returns>
        public AssignmentRecord? Peek()
        {
            if (IsEmpty)
            {
                return null;
            }

            return _stack.Peek();
        }

        /// <summary>
        /// 清空栈
        /// </summary>
        public void Clear()
        {
            _stack.Clear();
        }

        /// <summary>
        /// 获取栈中所有记录（从栈顶到栈底）
        /// </summary>
        public List<AssignmentRecord> GetAll()
        {
            return _stack.ToList();
        }

        /// <summary>
        /// 获取栈中所有记录（从栈底到栈顶）
        /// </summary>
        public List<AssignmentRecord> GetAllReversed()
        {
            var list = _stack.ToList();
            list.Reverse();
            return list;
        }

        /// <summary>
        /// 获取指定深度的记录
        /// </summary>
        /// <param name="depth">深度（0为栈底，Depth-1为栈顶）</param>
        public AssignmentRecord? GetAtDepth(int depth)
        {
            if (depth < 0 || depth >= _stack.Count)
            {
                return null;
            }

            var list = GetAllReversed();
            return list[depth];
        }

        /// <summary>
        /// 获取栈的统计信息
        /// </summary>
        public StackStatistics GetStatistics()
        {
            var stats = new StackStatistics
            {
                CurrentDepth = Depth,
                MaxDepth = _maxDepth,
                IsEmpty = IsEmpty,
                IsAtMaxDepth = IsAtMaxDepth,
                ManualAssignmentCount = 0,
                AutoAssignmentCount = 0,
                TotalMemoryBytes = 0
            };

            foreach (var record in _stack)
            {
                if (record.IsManual)
                {
                    stats.ManualAssignmentCount++;
                }
                else
                {
                    stats.AutoAssignmentCount++;
                }

                // 估算内存使用
                stats.TotalMemoryBytes += record.Snapshot.GetMemoryUsageEstimate();
                stats.TotalMemoryBytes += record.Candidates.Count * sizeof(int);
            }

            return stats;
        }

        /// <summary>
        /// 验证栈的完整性
        /// </summary>
        public bool ValidateIntegrity()
        {
            try
            {
                int expectedDepth = 0;
                foreach (var record in GetAllReversed())
                {
                    // 验证记录本身
                    if (!record.Validate())
                    {
                        return false;
                    }

                    // 验证深度一致性
                    if (record.Depth != expectedDepth)
                    {
                        return false;
                    }

                    expectedDepth++;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取栈的描述信息（用于调试）
        /// </summary>
        public override string ToString()
        {
            var stats = GetStatistics();
            return $"AssignmentStack [Depth={Depth}/{_maxDepth}, " +
                   $"Manual={stats.ManualAssignmentCount}, Auto={stats.AutoAssignmentCount}, " +
                   $"Memory={stats.TotalMemoryBytes / 1024.0:F2}KB]";
        }

        /// <summary>
        /// 获取栈的详细信息（用于日志）
        /// </summary>
        public string ToDetailedString()
        {
            if (IsEmpty)
            {
                return "AssignmentStack [Empty]";
            }

            var lines = new List<string>
            {
                $"AssignmentStack [Depth={Depth}/{_maxDepth}]:"
            };

            int index = 0;
            foreach (var record in _stack)
            {
                lines.Add($"  [{index}] {record.ToShortString()}");
                index++;
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    /// <summary>
    /// 栈统计信息
    /// </summary>
    public class StackStatistics
    {
        /// <summary>
        /// 当前深度
        /// </summary>
        public int CurrentDepth { get; set; }

        /// <summary>
        /// 最大深度
        /// </summary>
        public int MaxDepth { get; set; }

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty { get; set; }

        /// <summary>
        /// 是否已达最大深度
        /// </summary>
        public bool IsAtMaxDepth { get; set; }

        /// <summary>
        /// 手动分配数量
        /// </summary>
        public int ManualAssignmentCount { get; set; }

        /// <summary>
        /// 自动分配数量
        /// </summary>
        public int AutoAssignmentCount { get; set; }

        /// <summary>
        /// 总内存使用（字节）
        /// </summary>
        public long TotalMemoryBytes { get; set; }

        /// <summary>
        /// 总内存使用（MB）
        /// </summary>
        public double TotalMemoryMB => TotalMemoryBytes / (1024.0 * 1024.0);

        public override string ToString()
        {
            return $"StackStats [Depth={CurrentDepth}/{MaxDepth}, " +
                   $"Manual={ManualAssignmentCount}, Auto={AutoAssignmentCount}, " +
                   $"Memory={TotalMemoryMB:F2}MB]";
        }
    }
}
