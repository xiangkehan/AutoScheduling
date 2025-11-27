using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.UI.Xaml.Controls;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.LeftPanel
{
    /// <summary>
    /// 排班信息卡片
    /// 显示排班标题、状态、日期范围和基本统计信息
    /// </summary>
    public sealed partial class ScheduleInfoCard : UserControl
    {
        /// <summary>
        /// 视图模型
        /// </summary>
        public ScheduleResultViewModel ViewModel { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ScheduleInfoCard()
        {
            this.InitializeComponent();
            ViewModel = this.DataContext as ScheduleResultViewModel;
        }
    }
}