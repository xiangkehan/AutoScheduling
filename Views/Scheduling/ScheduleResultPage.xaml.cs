using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.UI.Xaml;
using AutoScheduling3.DTOs;
using Microsoft.UI.Xaml.Input;
using AutoScheduling3.Helpers;

namespace AutoScheduling3.Views.Scheduling
{
    public sealed partial class ScheduleResultPage : Page
    {
        public ScheduleResultViewModel ViewModel { get; }

        public ScheduleResultPage()
        {
            this.InitializeComponent();
            ViewModel = ((App)Application.Current).ServiceProvider.GetRequiredService<ScheduleResultViewModel>();
            this.DataContext = ViewModel;

            // 页面加载完成后附加光标行为
            this.Loaded += OnPageLoaded;
        }

        /// <summary>
        /// 页面加载完成时的处理
        /// </summary>
        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // 页面加载完成后的初始化逻辑
        }

        #region 光标交互处理

        /// <summary>
        /// 统计卡片鼠标进入时显示手型光标
        /// </summary>
        private void StatCard_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // 在 WinUI 3 中，通过设置 ProtectedCursor 属性来改变光标
            // 由于这是在 Page 类内部，我们可以访问 this.ProtectedCursor
            this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(
                Microsoft.UI.Input.InputSystemCursorShape.Hand);
        }

        /// <summary>
        /// 统计卡片鼠标离开时恢复默认光标
        /// </summary>
        private void StatCard_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // 恢复默认箭头光标
            this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(
                Microsoft.UI.Input.InputSystemCursorShape.Arrow);
        }

        #endregion

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is int scheduleId)
            {
                _ = ViewModel.LoadScheduleCommand.ExecuteAsync(scheduleId);
            }
            else if (e.Parameter is SchedulingRequestDto)
            {
                // This case is for when we navigate back from rescheduling.
                // The ViewModel on the Create page should handle this.
                // Here, we just make sure we don't try to load a schedule with a DTO.
            }
        }

        #region 键盘快捷键处理

        /// <summary>
        /// Ctrl+F: 打开筛选
        /// </summary>
        private void FilterAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            // 聚焦到筛选工具栏的第一个输入控件
            PersonnelSearchBox?.Focus(FocusState.Keyboard);
            args.Handled = true;
        }

        /// <summary>
        /// Ctrl+E: 导出
        /// </summary>
        private void ExportAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (ViewModel.ExportExcelCommand.CanExecute(null))
            {
                _ = ViewModel.ExportExcelCommand.ExecuteAsync(null);
            }
            args.Handled = true;
        }

        /// <summary>
        /// Ctrl+S: 保存更改
        /// </summary>
        private void SaveAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (ViewModel.HasUnsavedChanges && ViewModel.SaveChangesCommand.CanExecute(null))
            {
                _ = ViewModel.SaveChangesCommand.ExecuteAsync(null);
            }
            args.Handled = true;
        }

        /// <summary>
        /// Esc: 关闭冲突面板
        /// </summary>
        private void EscapeAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (ViewModel.IsConflictPaneOpen)
            {
                ViewModel.IsConflictPaneOpen = false;
                args.Handled = true;
            }
        }

        #endregion

        #region 日期筛选处理

        /// <summary>
        /// 开始日期改变时的处理
        /// </summary>
        private void FilterStartDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (args.NewDate.HasValue)
            {
                ViewModel.FilterStartDate = args.NewDate.Value.DateTime;
            }
        }

        /// <summary>
        /// 结束日期改变时的处理
        /// </summary>
        private void FilterEndDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (args.NewDate.HasValue)
            {
                ViewModel.FilterEndDate = args.NewDate.Value.DateTime;
            }
        }

        #endregion

        #region 冲突操作处理

        /// <summary>
        /// 定位冲突按钮点击处理
        /// </summary>
        private void LocateConflictButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                if (ViewModel.LocateConflictCommand.CanExecute(button.Tag))
                {
                    _ = ViewModel.LocateConflictCommand.ExecuteAsync(button.Tag);
                }
            }
        }

        /// <summary>
        /// 忽略冲突按钮点击处理
        /// </summary>
        private void IgnoreConflictButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                if (ViewModel.IgnoreConflictCommand.CanExecute(button.Tag))
                {
                    _ = ViewModel.IgnoreConflictCommand.ExecuteAsync(button.Tag);
                }
            }
        }

        /// <summary>
        /// 修复冲突按钮点击处理
        /// </summary>
        private void FixConflictButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                if (ViewModel.FixConflictCommand.CanExecute(button.Tag))
                {
                    _ = ViewModel.FixConflictCommand.ExecuteAsync(button.Tag);
                }
            }
        }

        #endregion

        #region 班次操作处理

        /// <summary>
        /// 查看班次详情按钮点击处理
        /// </summary>
        private void ViewShiftDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                if (ViewModel.ViewShiftDetailsCommand.CanExecute(button.Tag))
                {
                    _ = ViewModel.ViewShiftDetailsCommand.ExecuteAsync(button.Tag);
                }
            }
        }

        /// <summary>
        /// 编辑班次按钮点击处理
        /// </summary>
        private void EditShiftButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                if (ViewModel.EditShiftCommand.CanExecute(button.Tag))
                {
                    _ = ViewModel.EditShiftCommand.ExecuteAsync(button.Tag);
                }
            }
        }

        #endregion
    }
}
