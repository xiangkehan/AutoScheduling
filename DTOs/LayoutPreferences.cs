namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 布局偏好数据传输对象
    /// </summary>
    public class LayoutPreferences
    {
        /// <summary>
        /// 左侧面板宽度（比例，0-1之间）
        /// </summary>
        public double LeftPanelWidth { get; set; } = 0.2;

        /// <summary>
        /// 右侧面板宽度（比例，0-1之间）
        /// </summary>
        public double RightPanelWidth { get; set; } = 0.25;

        /// <summary>
        /// 左侧面板是否可见
        /// </summary>
        public bool IsLeftPanelVisible { get; set; } = true;

        /// <summary>
        /// 右侧面板是否可见
        /// </summary>
        public bool IsRightPanelVisible { get; set; } = false;

        /// <summary>
        /// 偏好的视图模式
        /// </summary>
        public string PreferredViewMode { get; set; } = "Grid";

        /// <summary>
        /// 筛选栏是否展开
        /// </summary>
        public bool IsFilterExpanded { get; set; } = false;
    }
}
