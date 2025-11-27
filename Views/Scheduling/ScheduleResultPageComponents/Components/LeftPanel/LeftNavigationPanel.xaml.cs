using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.UI.Xaml.Controls;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.LeftPanel
{
    /// <summary>
    /// 左侧导航/摘要面板
    /// 包含排班信息、统计摘要、冲突列表和折叠按钮
    /// </summary>
    public sealed partial class LeftNavigationPanel : UserControl
    {
        /// <summary>
        /// 视图模型
        /// </summary>
        public ScheduleResultViewModel ViewModel { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public LeftNavigationPanel()
        {
            this.InitializeComponent();
            ViewModel = this.DataContext as ScheduleResultViewModel;
        }
    }
}