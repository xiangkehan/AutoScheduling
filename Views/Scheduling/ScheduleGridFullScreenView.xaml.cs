using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AutoScheduling3.DTOs;
using AutoScheduling3.Controls;

namespace AutoScheduling3.Views.Scheduling
{
    /// <summary>
    /// 排班表格全屏视图
    /// </summary>
    public sealed partial class ScheduleGridFullScreenView : UserControl
    {
        /// <summary>
        /// 表格数据
        /// </summary>
        public ScheduleGridData? GridData { get; set; }

        /// <summary>
        /// 关闭请求事件
        /// </summary>
        public event EventHandler? CloseRequested;

        /// <summary>
        /// 初始化全屏视图
        /// </summary>
        public ScheduleGridFullScreenView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 初始化全屏视图（带数据）
        /// </summary>
        /// <param name="gridData">表格数据</param>
        public ScheduleGridFullScreenView(ScheduleGridData gridData)
        {
            InitializeComponent();
            GridData = gridData;

            // 更新副标题显示数据信息
            if (gridData != null)
            {
                SubtitleText.Text = $"日期范围: {gridData.StartDate:yyyy-MM-dd} 至 {gridData.EndDate:yyyy-MM-dd} | " +
                                   $"总天数: {gridData.TotalDays} | " +
                                   $"总时段数: {gridData.TotalPeriods} | " +
                                   $"哨位数: {gridData.Columns?.Count ?? 0}";
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 触发关闭请求事件
            CloseRequested?.Invoke(this, EventArgs.Empty);

            // 尝试查找父级 ContentDialog 并关闭
            var dialog = FindParentContentDialog(this);
            dialog?.Hide();
        }

        /// <summary>
        /// 表格控件全屏请求事件（在全屏模式下禁用）
        /// </summary>
        private void GridControl_FullScreenRequested(object? sender, EventArgs e)
        {
            // 已经在全屏模式，不做任何操作
            // 可以选择显示提示消息
        }

        /// <summary>
        /// 表格控件导出请求事件
        /// </summary>
        private void GridControl_ExportRequested(object? sender, ExportRequestedEventArgs e)
        {
            // 转发导出请求到父级处理
            // 在实际实现中，可以通过事件或回调处理导出逻辑
        }

        /// <summary>
        /// 查找父级 ContentDialog
        /// </summary>
        /// <param name="element">起始元素</param>
        /// <returns>找到的 ContentDialog，如果没有则返回 null</returns>
        private ContentDialog? FindParentContentDialog(DependencyObject element)
        {
            var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element);
            
            while (parent != null)
            {
                if (parent is ContentDialog dialog)
                {
                    return dialog;
                }
                parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
            }
            
            return null;
        }
    }
}
