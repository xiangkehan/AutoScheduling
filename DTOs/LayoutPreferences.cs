using System;

namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 布局偏好设置
    /// </summary>
    public class LayoutPreferences
    {
        /// <summary>
        /// 左侧面板宽度（默认300）
        /// </summary>
        public double LeftPanelWidth { get; set; } = 300;

        /// <summary>
        /// 右侧面板宽度（默认350）
        /// </summary>
        public double RightPanelWidth { get; set; } = 350;

        /// <summary>
        /// 左侧面板是否可见（默认true）
        /// </summary>
        public bool IsLeftPanelVisible { get; set; } = true;

        /// <summary>
        /// 右侧面板是否可见（默认false，按需显示）
        /// </summary>
        public bool IsRightPanelVisible { get; set; } = false;

        /// <summary>
        /// 左侧面板是否折叠（默认false）
        /// </summary>
        public bool IsLeftPanelCollapsed { get; set; } = false;

        /// <summary>
        /// 偏好的视图模式（默认Grid）
        /// </summary>
        public string PreferredViewMode { get; set; } = "Grid";

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
