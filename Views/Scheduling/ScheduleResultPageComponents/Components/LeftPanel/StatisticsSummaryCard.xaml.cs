using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.LeftPanel
{
    /// <summary>
    /// 统计摘要卡片
    /// 显示三项关键指标：硬约束冲突、软约束冲突、未分配班次
    /// 支持点击事件处理，实现联动效果
    /// </summary>
    public sealed partial class StatisticsSummaryCard : UserControl
    {
        /// <summary>
        /// 视图模型
        /// </summary>
        public ScheduleResultViewModel ViewModel { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public StatisticsSummaryCard()
        {
            this.InitializeComponent();
            ViewModel = this.DataContext as ScheduleResultViewModel;
        }

        /// <summary>
        /// 硬约束冲突卡片点击事件
        /// </summary>
        private void HardConflictCard_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.SelectStatisticCommand.Execute("HardConflict");
        }

        /// <summary>
        /// 软约束冲突卡片点击事件
        /// </summary>
        private void SoftConflictCard_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.SelectStatisticCommand.Execute("SoftConflict");
        }

        /// <summary>
        /// 未分配班次卡片点击事件
        /// </summary>
        private void UnassignedCard_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.SelectStatisticCommand.Execute("Unassigned");
        }
    }
}