using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.UI.Xaml;
using AutoScheduling3.DTOs;
using Microsoft.UI.Xaml.Input;
using AutoScheduling3.Helpers;
using System.Linq;
using Microsoft.UI.Xaml.Media;

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
            
            // 订阅窗口大小变化事件（在 GridSplitter partial class 中处理）
            this.SizeChanged += OnPageSizeChanged;
            
            // 加载布局偏好
            _ = ViewModel.LoadLayoutPreferencesAsync();
        }

        /// <summary>
        /// 页面加载完成时的处理
        /// </summary>
        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // 订阅 ViewModel 的滚动请求事件
            ViewModel.ScrollToCellRequested += OnScrollToCellRequested;
        }

        /// <summary>
        /// 处理滚动到单元格请求
        /// </summary>
        private void OnScrollToCellRequested(object? sender, ScrollToCellEventArgs e)
        {
            // 根据当前视图模式选择对应的控件进行滚动
            switch (ViewModel.CurrentViewMode)
            {
                case ViewMode.Grid:
                    // 网格视图：使用 ScheduleGrid 控件
                    ScheduleGrid?.ScrollToCell(e.RowIndex, e.ColumnIndex);
                    break;

                case ViewMode.ByPosition:
                    // 哨位视图：使用 PositionScheduleControl 控件
                    PositionScheduleControl?.ScrollToCell(e.RowIndex, e.ColumnIndex);
                    break;

                // 其他视图暂不支持滚动
            }
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

        #region 人员搜索处理

        /// <summary>
        /// 人员搜索框文本改变时的处理
        /// </summary>
        private void PersonnelSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // 只在用户输入时更新建议列表
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.UpdatePersonnelSuggestions(sender.Text);
            }
        }

        /// <summary>
        /// 人员搜索框获得焦点时显示所有人员
        /// </summary>
        private void PersonnelSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is AutoSuggestBox searchBox)
            {
                // 如果搜索框为空，显示所有人员
                if (string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    ViewModel.UpdatePersonnelSuggestions(string.Empty);
                }
            }
        }

        /// <summary>
        /// 人员搜索框选择建议项时的处理
        /// </summary>
        private void PersonnelSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is PersonnelDto personnel)
            {
                ViewModel.SelectedPersonnel = personnel;
                sender.Text = personnel.Name;
            }
        }

        /// <summary>
        /// 人员搜索框提交查询时的处理
        /// </summary>
        private void PersonnelSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is PersonnelDto personnel)
            {
                // 用户从下拉列表选择了一个项
                ViewModel.SelectedPersonnel = personnel;
            }
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                // 用户直接输入并按回车
                // 尝试从建议列表中找到匹配的第一个人员
                var matchedPersonnel = ViewModel.PersonnelSuggestions.FirstOrDefault();
                if (matchedPersonnel != null)
                {
                    ViewModel.SelectedPersonnel = matchedPersonnel;
                    sender.Text = matchedPersonnel.Name;
                }
            }
        }

        /// <summary>
        /// 人员选择器文本改变时的处理
        /// </summary>
        private void PersonnelSelectorAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // 只处理用户输入的情况
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.UpdatePersonnelSelectorSuggestions(sender.Text);
            }
        }

        /// <summary>
        /// 人员选择器选择建议项时的处理
        /// </summary>
        private void PersonnelSelectorAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is PersonnelScheduleData personnelSchedule)
            {
                ViewModel.SelectedPersonnelSchedule = personnelSchedule;
                sender.Text = personnelSchedule.PersonnelName;
            }
        }

        /// <summary>
        /// 人员选择器提交查询时的处理
        /// </summary>
        private void PersonnelSelectorAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is PersonnelScheduleData personnelSchedule)
            {
                // 用户从下拉列表选择了一个项
                ViewModel.SelectedPersonnelSchedule = personnelSchedule;
            }
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                // 用户直接输入并按回车，选择第一个匹配项
                var matchedPersonnel = ViewModel.PersonnelSelectorSuggestions.FirstOrDefault();
                if (matchedPersonnel != null)
                {
                    ViewModel.SelectedPersonnelSchedule = matchedPersonnel;
                    sender.Text = matchedPersonnel.PersonnelName;
                }
            }
        }

        /// <summary>
        /// 人员选择器获得焦点时显示所有人员
        /// </summary>
        private void PersonnelSelectorAutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is AutoSuggestBox searchBox)
            {
                // 如果搜索框为空，显示所有人员
                if (string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    // 调用 ViewModel 的方法来更新建议列表，而不是直接设置
                    ViewModel.UpdatePersonnelSelectorSuggestions(string.Empty);
                }
            }
        }

        #endregion

        #region 哨位选择器处理

        /// <summary>
        /// 哨位选择器文本改变时的处理
        /// </summary>
        private void PositionSelectorAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // 只处理用户输入的情况
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.UpdatePositionSelectorSuggestions(sender.Text);
            }
        }

        /// <summary>
        /// 哨位选择器选择建议项时的处理
        /// </summary>
        private void PositionSelectorAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is PositionScheduleData positionSchedule)
            {
                ViewModel.SelectPosition(positionSchedule);
            }
        }

        /// <summary>
        /// 哨位选择器提交查询时的处理
        /// </summary>
        private void PositionSelectorAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is PositionScheduleData positionSchedule)
            {
                // 用户从下拉列表选择了一个项
                ViewModel.SelectPosition(positionSchedule);
            }
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                // 用户直接输入并按回车，选择第一个匹配项
                var matchedPosition = ViewModel.PositionSelectorSuggestions.FirstOrDefault();
                if (matchedPosition != null)
                {
                    ViewModel.SelectPosition(matchedPosition);
                }
            }
        }

        /// <summary>
        /// 哨位选择器获得焦点时显示所有哨位
        /// </summary>
        private void PositionSelectorAutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is AutoSuggestBox searchBox)
            {
                // 显示所有可用哨位
                ViewModel.UpdatePositionSelectorSuggestions(string.Empty);
                searchBox.IsSuggestionListOpen = true;
            }
        }

        #endregion

        #region 冲突操作处理

        /// <summary>
        /// 定位冲突按钮点击处理
        /// </summary>
        private void LocateConflictButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ConflictDto conflict)
            {
                if (ViewModel.LocateConflictInGridCommand?.CanExecute(conflict) == true)
                {
                    _ = ViewModel.LocateConflictInGridCommand.ExecuteAsync(conflict);
                }
            }
        }

        /// <summary>
        /// 忽略冲突按钮点击处理
        /// </summary>
        private void IgnoreConflictButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ConflictDto conflict)
            {
                if (ViewModel.IgnoreConflictInGridCommand?.CanExecute(conflict) == true)
                {
                    _ = ViewModel.IgnoreConflictInGridCommand.ExecuteAsync(conflict);
                }
            }
        }

        /// <summary>
        /// 修复冲突按钮点击处理
        /// </summary>
        private void FixConflictButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ConflictDto conflict)
            {
                if (ViewModel.FixConflictInGridCommand?.CanExecute(conflict) == true)
                {
                    _ = ViewModel.FixConflictInGridCommand.ExecuteAsync(conflict);
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

        #region 搜索结果处理

        /// <summary>
        /// 搜索结果项点击处理
        /// </summary>
        private void SearchResultItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border && border.Tag is SearchResultItem item)
            {
                if (ViewModel.SelectSearchResultCommand?.CanExecute(item) == true)
                {
                    _ = ViewModel.SelectSearchResultCommand.ExecuteAsync(item);
                }
            }
        }

        #endregion
    }
}
