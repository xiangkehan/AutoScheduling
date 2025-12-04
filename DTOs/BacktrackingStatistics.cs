using System;

namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 回溯统计信息
    /// 对应需求: 4.2, 4.5
    /// </summary>
    public class BacktrackingStatistics
    {
        /// <summary>
        /// 总回溯次数
        /// </summary>
        public int TotalBacktracks { get; set; }

        /// <summary>
        /// 最大回溯深度
        /// </summary>
        public int MaxDepthReached { get; set; }

        /// <summary>
        /// 成功回溯次数（找到可行解）
        /// </summary>
        public int SuccessfulBacktracks { get; set; }

        /// <summary>
        /// 失败回溯次数（未找到可行解）
        /// </summary>
        public int FailedBacktracks { get; set; }

        /// <summary>
        /// 平均回溯深度
        /// </summary>
        public double AverageBacktrackDepth { get; set; }

        /// <summary>
        /// 回溯耗时（毫秒）
        /// </summary>
        public long BacktrackingTimeMs { get; set; }

        /// <summary>
        /// 避免的重复路径数量（通过路径记忆）
        /// </summary>
        public int AvoidedDuplicatePaths { get; set; }

        /// <summary>
        /// 总尝试的候选人员数量
        /// </summary>
        public int TotalCandidatesTried { get; set; }

        /// <summary>
        /// 死胡同检测次数
        /// </summary>
        public int DeadEndDetections { get; set; }

        /// <summary>
        /// 智能回溯点选择次数
        /// </summary>
        public int SmartBacktrackSelections { get; set; }

        /// <summary>
        /// 状态快照创建次数
        /// </summary>
        public int SnapshotsCreated { get; set; }

        /// <summary>
        /// 状态恢复次数
        /// </summary>
        public int StateRestores { get; set; }

        /// <summary>
        /// 峰值内存使用（MB）
        /// </summary>
        public double PeakMemoryUsageMB { get; set; }

        /// <summary>
        /// 当前内存使用（MB）
        /// </summary>
        public double CurrentMemoryUsageMB { get; set; }

        /// <summary>
        /// 内存压力事件次数
        /// </summary>
        public int MemoryPressureEvents { get; set; }

        public BacktrackingStatistics()
        {
            TotalBacktracks = 0;
            MaxDepthReached = 0;
            SuccessfulBacktracks = 0;
            FailedBacktracks = 0;
            AverageBacktrackDepth = 0.0;
            BacktrackingTimeMs = 0;
            AvoidedDuplicatePaths = 0;
            TotalCandidatesTried = 0;
            DeadEndDetections = 0;
            SmartBacktrackSelections = 0;
            SnapshotsCreated = 0;
            StateRestores = 0;
            PeakMemoryUsageMB = 0.0;
            CurrentMemoryUsageMB = 0.0;
            MemoryPressureEvents = 0;
        }

        /// <summary>
        /// 更新平均回溯深度
        /// </summary>
        public void UpdateAverageDepth()
        {
            if (TotalBacktracks > 0)
            {
                AverageBacktrackDepth = (double)MaxDepthReached / TotalBacktracks;
            }
        }

        /// <summary>
        /// 记录回溯操作
        /// </summary>
        /// <param name="depth">回溯深度</param>
        /// <param name="success">是否成功</param>
        public void RecordBacktrack(int depth, bool success)
        {
            TotalBacktracks++;
            
            if (depth > MaxDepthReached)
            {
                MaxDepthReached = depth;
            }

            if (success)
            {
                SuccessfulBacktracks++;
            }
            else
            {
                FailedBacktracks++;
            }

            UpdateAverageDepth();
        }

        /// <summary>
        /// 记录死胡同检测
        /// </summary>
        public void RecordDeadEnd()
        {
            DeadEndDetections++;
        }

        /// <summary>
        /// 记录智能回溯点选择
        /// </summary>
        public void RecordSmartSelection()
        {
            SmartBacktrackSelections++;
        }

        /// <summary>
        /// 记录状态快照创建
        /// </summary>
        public void RecordSnapshotCreated()
        {
            SnapshotsCreated++;
        }

        /// <summary>
        /// 记录状态恢复
        /// </summary>
        public void RecordStateRestore()
        {
            StateRestores++;
        }

        /// <summary>
        /// 记录避免的重复路径
        /// </summary>
        public void RecordAvoidedPath()
        {
            AvoidedDuplicatePaths++;
        }

        /// <summary>
        /// 记录尝试的候选人员
        /// </summary>
        /// <param name="count">候选人员数量</param>
        public void RecordCandidatesTried(int count)
        {
            TotalCandidatesTried += count;
        }

        /// <summary>
        /// 更新内存使用
        /// </summary>
        /// <param name="currentUsageMB">当前内存使用（MB）</param>
        public void UpdateMemoryUsage(double currentUsageMB)
        {
            CurrentMemoryUsageMB = currentUsageMB;
            
            if (currentUsageMB > PeakMemoryUsageMB)
            {
                PeakMemoryUsageMB = currentUsageMB;
            }
        }

        /// <summary>
        /// 记录内存压力事件
        /// 对应需求: 6.5
        /// </summary>
        public void RecordMemoryPressure()
        {
            MemoryPressureEvents++;
        }

        /// <summary>
        /// 获取成功率
        /// </summary>
        public double SuccessRate
        {
            get
            {
                if (TotalBacktracks == 0) return 0.0;
                return (double)SuccessfulBacktracks / TotalBacktracks * 100.0;
            }
        }

        /// <summary>
        /// 获取格式化的统计信息
        /// </summary>
        public string GetFormattedSummary()
        {
            return $"回溯统计: 总次数={TotalBacktracks}, 成功={SuccessfulBacktracks}, " +
                   $"失败={FailedBacktracks}, 最大深度={MaxDepthReached}, " +
                   $"平均深度={AverageBacktrackDepth:F2}, 耗时={BacktrackingTimeMs}ms, " +
                   $"避免重复={AvoidedDuplicatePaths}, 峰值内存={PeakMemoryUsageMB:F2}MB, " +
                   $"内存压力={MemoryPressureEvents}";
        }

        /// <summary>
        /// 获取详细的统计信息
        /// </summary>
        public string GetDetailedSummary()
        {
            return $"回溯统计详情:\n" +
                   $"  总回溯次数: {TotalBacktracks}\n" +
                   $"  成功回溯: {SuccessfulBacktracks} ({SuccessRate:F1}%)\n" +
                   $"  失败回溯: {FailedBacktracks}\n" +
                   $"  最大深度: {MaxDepthReached}\n" +
                   $"  平均深度: {AverageBacktrackDepth:F2}\n" +
                   $"  回溯耗时: {BacktrackingTimeMs}ms\n" +
                   $"  死胡同检测: {DeadEndDetections}\n" +
                   $"  智能选择: {SmartBacktrackSelections}\n" +
                   $"  避免重复路径: {AvoidedDuplicatePaths}\n" +
                   $"  尝试候选人员: {TotalCandidatesTried}\n" +
                   $"  快照创建: {SnapshotsCreated}\n" +
                   $"  状态恢复: {StateRestores}\n" +
                   $"  峰值内存: {PeakMemoryUsageMB:F2}MB\n" +
                   $"  当前内存: {CurrentMemoryUsageMB:F2}MB\n" +
                   $"  内存压力事件: {MemoryPressureEvents}";
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void Reset()
        {
            TotalBacktracks = 0;
            MaxDepthReached = 0;
            SuccessfulBacktracks = 0;
            FailedBacktracks = 0;
            AverageBacktrackDepth = 0.0;
            BacktrackingTimeMs = 0;
            AvoidedDuplicatePaths = 0;
            TotalCandidatesTried = 0;
            DeadEndDetections = 0;
            SmartBacktrackSelections = 0;
            SnapshotsCreated = 0;
            StateRestores = 0;
            PeakMemoryUsageMB = 0.0;
            CurrentMemoryUsageMB = 0.0;
            MemoryPressureEvents = 0;
        }

        public override string ToString()
        {
            return GetFormattedSummary();
        }
    }
}
