using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.UI.Xaml;
using AutoScheduling3.DTOs;
using Microsoft.UI.Xaml.Input;

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
        }

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
    }
}
