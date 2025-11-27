using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.UI.Xaml.Controls;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.RightPanel
{
    /// <summary>
    /// 右侧详情区容器
    /// 包含标题栏、详情内容和操作按钮
    /// </summary>
    public sealed partial class RightDetailPanel : UserControl
    {
        /// <summary>
        /// 视图模型
        /// </summary>
        public ScheduleResultViewModel ViewModel { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public RightDetailPanel()
        {
            this.InitializeComponent();
            ViewModel = this.DataContext as ScheduleResultViewModel;
        }
    }
}