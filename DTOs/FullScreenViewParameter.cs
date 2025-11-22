namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 全屏视图参数
    /// </summary>
    public class FullScreenViewParameter
    {
        /// <summary>
        /// 视图模式
        /// </summary>
        public ViewMode ViewMode { get; set; }

        /// <summary>
        /// Grid View 数据
        /// </summary>
        public ScheduleGridData? GridData { get; set; }

        /// <summary>
        /// Position View 数据
        /// </summary>
        public PositionScheduleData? PositionScheduleData { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; } = "全屏视图";
    }
}
