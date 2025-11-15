using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls; // Added for Page
using AutoScheduling3.ViewModels.DataManagement;
using AutoScheduling3.Helpers;
using System.Linq;
using System.Collections.Generic;

namespace AutoScheduling3.Views.DataManagement
{
    public sealed partial class PersonnelPage : Page
    {
        public PersonnelViewModel ViewModel { get; }
        private bool _isUpdatingSelection = false; // 标记是否正在程序化更新选择
        private System.Collections.Generic.List<int> _timeSlotsList; // 时段列表 (0-11)

        public PersonnelPage()
        {
            this.InitializeComponent();
            ViewModel = ((App)Application.Current).ServiceProvider.GetRequiredService<PersonnelViewModel>();
            this.Loaded += PersonnelPage_Loaded;
            
            // 初始化时段列表
            _timeSlotsList = Enumerable.Range(0, 12).ToList();
        }

        private async void PersonnelPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadDataAsync();
            
            // 初始化响应式布局
            if (MainContentGrid.ActualWidth > 0)
            {
                ApplyResponsiveLayout(MainContentGrid.ActualWidth);
            }

            // 监听编辑模式变化，同步选中状态
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            // 初始化时段列表
            CreateTimeSlotsListView.ItemsSource = _timeSlotsList;
            EditTimeSlotsListView.ItemsSource = _timeSlotsList;
        }

        private void PersonnelGridView_Loaded(object sender, RoutedEventArgs e)
        {
            // 订阅SelectedItem变化事件以实现自动滚动
            ViewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(ViewModel.SelectedItem) && ViewModel.SelectedItem != null)
                {
                    // 延迟执行以确保UI已更新
                    _ = this.DispatcherQueue.TryEnqueue(() =>
                    {
                        PersonnelGridView.ScrollIntoView(ViewModel.SelectedItem, ScrollIntoViewAlignment.Default);
                    });
                }
            };
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.IsEditing))
            {
                if (ViewModel.IsEditing && ViewModel.EditingPersonnel != null)
                {
                    // 延迟同步以确保UI已更新
                    _ = this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                    {
                        SyncEditSelections();
                    });
                }
                else if (!ViewModel.IsEditing)
                {
                    // 退出编辑模式时清除选择
                    EditSkillsListView.SelectedItems.Clear();
                    EditSelectedSkillCountText.Text = "已选择: 0";
                }
            }
            else if (e.PropertyName == nameof(ViewModel.IsEditingRule))
            {
                if (ViewModel.IsEditingRule && ViewModel.EditingFixedPositionRule != null)
                {
                    // 延迟同步以确保UI已更新
                    _ = this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                    {
                        SyncEditRuleSelections();
                    });
                }
                else if (!ViewModel.IsEditingRule)
                {
                    // 退出编辑模式时清除选择
                    EditPositionsListView.SelectedItems.Clear();
                    EditTimeSlotsListView.SelectedItems.Clear();
                    EditSelectedPositionCountText.Text = "已选择: 0";
                    EditSelectedTimeSlotCountText.Text = "已选择: 0";
                }
            }
        }

        private void SyncEditSelections()
        {
            if (ViewModel.EditingPersonnel == null || ViewModel.AvailableSkills.Count == 0) 
                return;

            // 设置标记，防止触发SelectionChanged事件处理
            _isUpdatingSelection = true;

            try
            {
                // 清除当前选择
                EditSkillsListView.SelectedItems.Clear();
                
                // 同步技能选择
                foreach (var skillId in ViewModel.EditingPersonnel.SkillIds)
                {
                    var skill = ViewModel.AvailableSkills.FirstOrDefault(s => s.Id == skillId);
                    if (skill != null)
                    {
                        EditSkillsListView.SelectedItems.Add(skill);
                    }
                }
                
                // 更新选中技能数量显示
                EditSelectedSkillCountText.Text = $"已选择: {EditSkillsListView.SelectedItems.Count}";
            }
            finally
            {
                // 恢复标记
                _isUpdatingSelection = false;
            }
        }

        private void SyncEditRuleSelections()
        {
            if (ViewModel.EditingFixedPositionRule == null) 
                return;

            // 设置标记，防止触发SelectionChanged事件处理
            _isUpdatingSelection = true;

            try
            {
                // 清除当前选择
                EditPositionsListView.SelectedItems.Clear();
                EditTimeSlotsListView.SelectedItems.Clear();
                
                // 同步哨位选择
                if (ViewModel.EditingFixedPositionRule.AllowedPositionIds != null)
                {
                    foreach (var positionId in ViewModel.EditingFixedPositionRule.AllowedPositionIds)
                    {
                        var position = ViewModel.AvailablePositions.FirstOrDefault(p => p.Id == positionId);
                        if (position != null)
                        {
                            EditPositionsListView.SelectedItems.Add(position);
                        }
                    }
                }
                
                // 同步时段选择
                if (ViewModel.EditingFixedPositionRule.AllowedTimeSlots != null)
                {
                    foreach (var timeSlot in ViewModel.EditingFixedPositionRule.AllowedTimeSlots)
                    {
                        // 在时段列表中查找对应的时段
                        if (timeSlot >= 0 && timeSlot < _timeSlotsList.Count)
                        {
                            EditTimeSlotsListView.SelectedItems.Add(timeSlot);
                        }
                    }
                }
                
                // 更新选中数量显示
                EditSelectedPositionCountText.Text = $"已选择: {EditPositionsListView.SelectedItems.Count}";
                EditSelectedTimeSlotCountText.Text = $"已选择: {EditTimeSlotsListView.SelectedItems.Count}";
            }
            finally
            {
                // 恢复标记
                _isUpdatingSelection = false;
            }
        }

        private void ResetForm_Click(object sender, RoutedEventArgs e)
        {
            // 调用ViewModel的重置方法
            ViewModel.ResetCreateForm();
            
            // 清除ListView选择
            SkillsListView.SelectedItems.Clear();
            
            // 重置选中技能数量显示
            SelectedSkillCountText.Text = "已选择: 0";
            
            // 聚焦到姓名输入框
            NameTextBox.Focus(FocusState.Programmatic);
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.ValidateName();
        }

        private void SkillsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedSkills = SkillsListView.SelectedItems.Cast<DTOs.SkillDto>().ToList();
            ViewModel.NewPersonnel.SkillIds = selectedSkills.Select(s => s.Id).ToList();
            ViewModel.ValidateSkills();
            
            // 更新选中技能数量显示
            SelectedSkillCountText.Text = $"已选择: {SkillsListView.SelectedItems.Count}";
        }

        private void EditSkillsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 如果正在程序化更新选择，跳过处理
            if (_isUpdatingSelection)
                return;

            if (ViewModel.EditingPersonnel != null)
            {
                var selectedSkills = EditSkillsListView.SelectedItems.Cast<DTOs.SkillDto>().ToList();
                ViewModel.EditingPersonnel.SkillIds = selectedSkills.Select(s => s.Id).ToList();
                
                // 验证技能选择
                ValidateEditSkills();
            }
            
            // 更新选中技能数量显示
            EditSelectedSkillCountText.Text = $"已选择: {EditSkillsListView.SelectedItems.Count}";
        }

        private void EditNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateEditName();
        }

        private void ValidateEditName()
        {
            if (ViewModel.EditingPersonnel == null)
                return;

            var name = ViewModel.EditingPersonnel.Name?.Trim();
            
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowEditFieldError(EditNameErrorText, "姓名不能为空");
            }
            else if (name.Length > 50)
            {
                ShowEditFieldError(EditNameErrorText, "姓名长度不能超过50字符");
            }
            else
            {
                HideEditFieldError(EditNameErrorText);
            }
        }

        private void ValidateEditSkills()
        {
            if (ViewModel.EditingPersonnel == null)
                return;

            // 允许人员没有技能，不再验证技能必选
            HideEditFieldError(EditSkillsErrorText);
        }

        private void ShowEditFieldError(TextBlock errorText, string message)
        {
            errorText.Text = message;
            errorText.Visibility = Visibility.Visible;
        }

        private void HideEditFieldError(TextBlock errorText)
        {
            errorText.Visibility = Visibility.Collapsed;
        }

        private void MainContentGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout(e.NewSize.Width);
        }

        private void ApplyResponsiveLayout(double availableWidth)
        {
            var breakpoint = ResponsiveHelper.GetCurrentBreakpoint(availableWidth);
            
            // 根据断点调整布局
            switch (breakpoint)
            {
                case ResponsiveHelper.Breakpoint.XSmall:
                case ResponsiveHelper.Breakpoint.Small:
                    // 小屏幕：垂直布局
                    ApplyVerticalLayout();
                    break;
                    
                case ResponsiveHelper.Breakpoint.Medium:
                case ResponsiveHelper.Breakpoint.Large:
                case ResponsiveHelper.Breakpoint.XLarge:
                    // 大屏幕：水平布局
                    ApplyHorizontalLayout();
                    break;
            }

            // 应用响应式边距
            var margin = ResponsiveHelper.GetResponsiveMargin(breakpoint);
            MainContentGrid.Margin = new Thickness(margin.Left / 2, margin.Top / 2, margin.Right / 2, margin.Bottom / 2);
        }

        private void ApplyVerticalLayout()
        {
            // 清除列定义，使用行布局
            MainContentGrid.ColumnDefinitions.Clear();
            MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 设置行定义
            MainContentGrid.RowDefinitions.Clear();
            MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) }); // 间距
            MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 重新定位元素
            Grid.SetColumn(PersonnelListGrid, 0);
            Grid.SetRow(PersonnelListGrid, 0);
            
            Grid.SetColumn(DetailScrollViewer, 0);
            Grid.SetRow(DetailScrollViewer, 2);

            // 隐藏分隔线
            SeparatorBorder.Visibility = Visibility.Collapsed;
        }

        private void ApplyHorizontalLayout()
        {
            // 设置列定义
            MainContentGrid.ColumnDefinitions.Clear();
            MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) }); // 间距
            MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.4, GridUnitType.Star), MinWidth = 300, MaxWidth = 500 });

            // 清除行定义
            MainContentGrid.RowDefinitions.Clear();
            MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 重新定位元素
            Grid.SetColumn(PersonnelListGrid, 0);
            Grid.SetRow(PersonnelListGrid, 0);
            
            Grid.SetColumn(DetailScrollViewer, 2);
            Grid.SetRow(DetailScrollViewer, 0);

            Grid.SetColumn(SeparatorBorder, 1);
            Grid.SetRow(SeparatorBorder, 0);

            // 显示分隔线
            SeparatorBorder.Visibility = Visibility.Visible;
        }

        // 定岗规则创建表单事件处理

        private void CreatePositionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedPositions = CreatePositionsListView.SelectedItems.Cast<DTOs.PositionDto>().ToList();
            ViewModel.NewFixedPositionRule.AllowedPositionIds = selectedPositions.Select(p => p.Id).ToList();
            
            // 更新选中哨位数量显示
            CreateSelectedPositionCountText.Text = $"已选择: {CreatePositionsListView.SelectedItems.Count}";
            
            // 触发验证
            ViewModel.ValidateCreateRuleForm();
        }

        private void CreateTimeSlotsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTimeSlots = CreateTimeSlotsListView.SelectedItems.Cast<int>().ToList();
            ViewModel.NewFixedPositionRule.AllowedTimeSlots = selectedTimeSlots;
            
            // 更新选中时段数量显示
            CreateSelectedTimeSlotCountText.Text = $"已选择: {CreateTimeSlotsListView.SelectedItems.Count}";
            
            // 触发验证
            ViewModel.ValidateCreateRuleForm();
        }

        private void CancelCreateRule_Click(object sender, RoutedEventArgs e)
        {
            // 重置表单
            ViewModel.ResetFixedPositionRuleForm();
            
            // 清除ListView选择
            CreatePositionsListView.SelectedItems.Clear();
            CreateTimeSlotsListView.SelectedItems.Clear();
            
            // 重置选中数量显示
            CreateSelectedPositionCountText.Text = "已选择: 0";
            CreateSelectedTimeSlotCountText.Text = "已选择: 0";
            
            // 关闭创建表单
            ViewModel.IsCreatingRule = false;
        }

        private void AddFixedPositionRule_Click(object sender, RoutedEventArgs e)
        {
            // 重置表单并显示创建界面
            ViewModel.ResetFixedPositionRuleForm();
            ViewModel.IsCreatingRule = true;
        }

        // 定岗规则编辑表单事件处理

        private void EditPositionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 如果正在程序化更新选择，跳过处理
            if (_isUpdatingSelection)
                return;

            if (ViewModel.EditingFixedPositionRule != null)
            {
                var selectedPositions = EditPositionsListView.SelectedItems.Cast<DTOs.PositionDto>().ToList();
                ViewModel.EditingFixedPositionRule.AllowedPositionIds = selectedPositions.Select(p => p.Id).ToList();
                
                // 触发验证
                ViewModel.ValidateEditRuleForm();
            }
            
            // 更新选中哨位数量显示
            EditSelectedPositionCountText.Text = $"已选择: {EditPositionsListView.SelectedItems.Count}";
        }

        private void EditTimeSlotsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 如果正在程序化更新选择，跳过处理
            if (_isUpdatingSelection)
                return;

            if (ViewModel.EditingFixedPositionRule != null)
            {
                var selectedTimeSlots = EditTimeSlotsListView.SelectedItems.Cast<int>().ToList();
                ViewModel.EditingFixedPositionRule.AllowedTimeSlots = selectedTimeSlots;
                
                // 触发验证
                ViewModel.ValidateEditRuleForm();
            }
            
            // 更新选中时段数量显示
            EditSelectedTimeSlotCountText.Text = $"已选择: {EditTimeSlotsListView.SelectedItems.Count}";
        }
    }
}
