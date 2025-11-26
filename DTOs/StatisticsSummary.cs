namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 统计摘要数据传输对象
    /// </summary>
    public class StatisticsSummary
    {
        /// <summary>
        /// 硬约束冲突数量
        /// </summary>
        public int HardConflictCount { get; set; }

        /// <summary>
        /// 软约束冲突数量
        /// </summary>
        public int SoftConflictCount { get; set; }

        /// <summary>
        /// 未分配班次数量
        /// </summary>
        public int UnassignedCount { get; set; }

        /// <summary>
        /// 覆盖率（0-1之间）
        /// </summary>
        public double CoverageRate { get; set; }

        /// <summary>
        /// 总班次数量
        /// </summary>
        public int TotalShiftCount { get; set; }

        /// <summary>
        /// 已分配班次数量
        /// </summary>
        public int AssignedShiftCount { get; set; }
    }
}
