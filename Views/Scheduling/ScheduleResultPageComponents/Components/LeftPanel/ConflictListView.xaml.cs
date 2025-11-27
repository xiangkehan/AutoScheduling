using AutoScheduling3.DTOs;
using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.LeftPanel
{
    /// <summary>
    /// 冲突列表视图
    /// 使用ItemsRepeater实现虚拟化列表，支持按类型分组、搜索和排序
    /// </summary>
    public sealed partial class ConflictListView : UserControl
    {
        /// <summary>
        /// 视图模型
        /// </summary>
        public ScheduleResultViewModel ViewModel { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ConflictListView()
        {
            this.InitializeComponent();
            ViewModel = this.DataContext as ScheduleResultViewModel;
        }

        /// <summary>
        /// 冲突项点击事件
        /// </summary>
        private void ConflictItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border && border.DataContext is ConflictDto conflict)
            {
                ViewModel.SelectConflictCommand.Execute(conflict);
            }
        }
    }
}