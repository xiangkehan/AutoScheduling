using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.MainContent
{
    /// <summary>
    /// 自定义筛选对话框，用于实现多维度筛选和预设保存
    /// </summary>
    public sealed partial class CustomFilterDialog : ContentDialog, INotifyPropertyChanged
    {
        /// <summary>
        /// 视图模型属性
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel), typeof(object), typeof(CustomFilterDialog), new PropertyMetadata(null));

        /// <summary>
        /// 视图模型
        /// </summary>
        public object ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// 筛选预设集合
        /// </summary>
        public ObservableCollection<string> FilterPresets { get; set; }

        /// <summary>
        /// 人员筛选项集合
        /// </summary>
        public ObservableCollection<FilterItem> PersonnelItems { get; set; }

        /// <summary>
        /// 哨位筛选项集合
        /// </summary>
        public ObservableCollection<FilterItem> PositionItems { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public CustomFilterDialog()
        {
            this.InitializeComponent();
            
            // 初始化数据集合
            FilterPresets = new ObservableCollection<string>();
            PersonnelItems = new ObservableCollection<FilterItem>();
            PositionItems = new ObservableCollection<FilterItem>();
            
            // 初始化筛选预设
            InitializeFilterPresets();
            
            // 初始化人员和哨位数据
            InitializeFilterItems();
            
            // 设置数据源
            PresetComboBox.ItemsSource = FilterPresets;
            PersonnelList.ItemsSource = PersonnelItems;
            PositionList.ItemsSource = PositionItems;
            
            // 绑定事件
            this.PrimaryButtonClick += CustomFilterDialog_PrimaryButtonClick;
            this.SecondaryButtonClick += CustomFilterDialog_SecondaryButtonClick;
            this.CloseButtonClick += CustomFilterDialog_CloseButtonClick;
            SavePresetDialog.PrimaryButtonClick += SavePresetDialog_PrimaryButtonClick;
        }

        /// <summary>
        /// 初始化筛选预设
        /// </summary>
        private void InitializeFilterPresets()
        {
            // 模拟筛选预设数据
            FilterPresets.Add("全部数据");
            FilterPresets.Add("今天的班次");
            FilterPresets.Add("本周的夜哨");
            FilterPresets.Add("有冲突的班次");
        }

        /// <summary>
        /// 初始化筛选项数据
        /// </summary>
        private void InitializeFilterItems()
        {
            // 模拟人员数据
            var personnel = new List<string> { "张三", "李四", "王五", "赵六", "孙七", "周八", "吴九", "郑十" };
            foreach (var p in personnel)
            {
                PersonnelItems.Add(new FilterItem { Name = p, IsSelected = true });
            }
            
            // 模拟哨位数据
            var positions = new List<string> { "哨位A", "哨位B", "哨位C", "哨位D", "哨位E" };
            foreach (var pos in positions)
            {
                PositionItems.Add(new FilterItem { Name = pos, IsSelected = true });
            }
        }

        /// <summary>
        /// 应用筛选按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="args">事件参数</param>
        private void CustomFilterDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 应用筛选条件
            ApplyFilters();
        }

        /// <summary>
        /// 取消按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="args">事件参数</param>
        private void CustomFilterDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 取消筛选，不做任何操作
        }

        /// <summary>
        /// 清除按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="args">事件参数</param>
        private void CustomFilterDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 清除所有筛选条件
            ClearAllFilters();
        }

        /// <summary>
        /// 保存预设按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void SavePresetButton_Click(object sender, RoutedEventArgs e)
        {
            // 显示保存预设对话框
            SavePresetDialog.ShowAsync();
        }

        /// <summary>
        /// 删除预设按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void DeletePresetButton_Click(object sender, RoutedEventArgs e)
        {
            // 删除选中的预设
            if (PresetComboBox.SelectedItem is string selectedPreset && selectedPreset != "全部数据")
            {
                FilterPresets.Remove(selectedPreset);
            }
        }

        /// <summary>
        /// 保存预设对话框确认按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="args">事件参数</param>
        private void SavePresetDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 保存筛选预设
            var presetName = PresetNameTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(presetName) && !FilterPresets.Contains(presetName))
            {
                FilterPresets.Add(presetName);
                PresetNameTextBox.Text = string.Empty;
            }
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private void ApplyFilters()
        {
            // 获取筛选条件
            var filters = GetCurrentFilters();
            
            // 实际项目中应该调用ViewModel的方法来应用筛选
            // ViewModel.ApplyCustomFilters(filters);
        }

        /// <summary>
        /// 清除所有筛选条件
        /// </summary>
        private void ClearAllFilters()
        {
            // 清除日期筛选
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            
            // 清除人员筛选
            foreach (var item in PersonnelItems)
            {
                item.IsSelected = true;
            }
            
            // 清除哨位筛选
            foreach (var item in PositionItems)
            {
                item.IsSelected = true;
            }
            
            // 清除时段筛选
            DayShiftCheckBox.IsChecked = true;
            NightShiftCheckBox.IsChecked = true;
            
            // 清除状态筛选
            HasConflictCheckBox.IsChecked = true;
            AssignedCheckBox.IsChecked = true;
            UnassignedCheckBox.IsChecked = true;
            
            // 实际项目中应该调用ViewModel的方法来清除筛选
            // ViewModel.ClearAllFilters();
        }

        /// <summary>
        /// 获取当前筛选条件
        /// </summary>
        /// <returns>筛选条件</returns>
        private CustomFilterData GetCurrentFilters()
        {
            return new CustomFilterData
            {
                StartDate = StartDatePicker.Date,
                EndDate = EndDatePicker.Date,
                SelectedPersonnel = PersonnelItems.Where(p => p.IsSelected).Select(p => p.Name).ToList(),
                SelectedPositions = PositionItems.Where(p => p.IsSelected).Select(p => p.Name).ToList(),
                IncludeDayShift = DayShiftCheckBox.IsChecked ?? true,
                IncludeNightShift = NightShiftCheckBox.IsChecked ?? true,
                IncludeConflicts = HasConflictCheckBox.IsChecked ?? true,
                IncludeAssigned = AssignedCheckBox.IsChecked ?? true,
                IncludeUnassigned = UnassignedCheckBox.IsChecked ?? true
            };
        }

        /// <summary>
        /// 属性变化通知事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变化通知
        /// </summary>
        /// <param name="propertyName">属性名</param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 筛选项类
    /// </summary>
    public class FilterItem : INotifyPropertyChanged
    {
        /// <summary>
        /// 名称
        /// </summary>
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        /// <summary>
        /// 是否选中
        /// </summary>
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        /// <summary>
        /// 属性变化通知事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变化通知
        /// </summary>
        /// <param name="propertyName">属性名</param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 自定义筛选数据类
    /// </summary>
    public class CustomFilterData
    {
        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTimeOffset? StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }

        /// <summary>
        /// 选中的人员列表
        /// </summary>
        public List<string> SelectedPersonnel { get; set; }

        /// <summary>
        /// 选中的哨位列表
        /// </summary>
        public List<string> SelectedPositions { get; set; }

        /// <summary>
        /// 是否包含日哨
        /// </summary>
        public bool IncludeDayShift { get; set; }

        /// <summary>
        /// 是否包含夜哨
        /// </summary>
        public bool IncludeNightShift { get; set; }

        /// <summary>
        /// 是否包含有冲突的班次
        /// </summary>
        public bool IncludeConflicts { get; set; }

        /// <summary>
        /// 是否包含已分配的班次
        /// </summary>
        public bool IncludeAssigned { get; set; }

        /// <summary>
        /// 是否包含未分配的班次
        /// </summary>
        public bool IncludeUnassigned { get; set; }
    }
}