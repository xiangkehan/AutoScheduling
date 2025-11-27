using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using AutoScheduling3.ViewModels.Scheduling;
using Windows.UI;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.Shared
{
    /// <summary>
    /// 排班单元格组件
    /// </summary>
    public sealed partial class ScheduleCell : UserControl
    {
        /// <summary>
        /// 单元格ViewModel
        /// </summary>
        public ScheduleCellViewModel Cell => DataContext as ScheduleCellViewModel;

        public ScheduleCell()
        {
            this.InitializeComponent();
            this.DataContextChanged += ScheduleCell_DataContextChanged;
        }

        private void ScheduleCell_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // 当DataContext变化时，更新绑定
            Bindings.Update();
        }

        /// <summary>
        /// 单元格点击事件
        /// </summary>
        private void CellButton_Click(object sender, RoutedEventArgs e)
        {
            if (Cell == null) return;

            // 查找父级ViewModel并执行SelectCellCommand
            var mainWindow = App.MainWindow;
            if (mainWindow?.Content is Frame frame && 
                frame.Content is Page page)
            {
                if (page.DataContext is ScheduleResultViewModel viewModel)
                {
                    viewModel.SelectCellCommand.Execute(Cell);
                }
            }
        }

        /// <summary>
        /// 鼠标进入事件
        /// </summary>
        private void CellButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // 显示工具提示
            if (Cell != null && !string.IsNullOrEmpty(Cell.ConflictTooltip))
            {
                ToolTipService.SetToolTip(CellButton, Cell.ConflictTooltip);
            }
        }

        /// <summary>
        /// 鼠标离开事件
        /// </summary>
        private void CellButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // 可以在这里添加其他逻辑
        }

        /// <summary>
        /// 获取冲突边框颜色
        /// </summary>
        public Color GetConflictBorderColor(bool hasHardConflict, bool hasSoftConflict)
        {
            if (hasHardConflict)
            {
                // 红色 - 硬约束冲突
                return Color.FromArgb(255, 196, 43, 28);
            }
            else if (hasSoftConflict)
            {
                // 黄色 - 软约束冲突
                return Color.FromArgb(255, 255, 185, 0);
            }
            else
            {
                // 透明 - 无冲突
                return Colors.Transparent;
            }
        }
    }
}
