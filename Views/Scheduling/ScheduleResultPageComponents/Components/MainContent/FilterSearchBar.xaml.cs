using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.MainContent
{
    /// <summary>
    /// 筛选搜索栏组件，提供搜索和筛选功能
    /// </summary>
    public sealed partial class FilterSearchBar : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// 搜索建议项集合
        /// </summary>
        public ObservableCollection<string> SearchSuggestions { get; set; }

        /// <summary>
        /// 搜索历史记录集合
        /// </summary>
        public ObservableCollection<string> SearchHistory { get; set; }

        /// <summary>
        /// 搜索防抖的CancellationTokenSource
        /// </summary>
        private CancellationTokenSource _debounceCts;

        /// <summary>
        /// 视图模型属性
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel), typeof(object), typeof(FilterSearchBar), new PropertyMetadata(null));

        /// <summary>
        /// 视图模型
        /// </summary>
        public object ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public FilterSearchBar()
        {
            this.InitializeComponent();
            
            // 初始化搜索建议和历史记录集合
            SearchSuggestions = new ObservableCollection<string>();
            SearchHistory = new ObservableCollection<string>();
            
            // 加载搜索历史记录
            LoadSearchHistory();
            
            // 绑定AutoSuggestBox的事件
            SearchBox.TextChanged += SearchBox_TextChanged;
            SearchBox.QuerySubmitted += SearchBox_QuerySubmitted;
            SearchBox.SuggestionChosen += SearchBox_SuggestionChosen;
            
            // 设置AutoSuggestBox的数据源
            SearchBox.ItemsSource = SearchSuggestions;
        }

        /// <summary>
        /// 搜索框文本变化事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="args">事件参数</param>
        private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // 只有用户输入时才生成搜索建议
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                // 取消之前的防抖任务
                _debounceCts?.Cancel();
                _debounceCts = new CancellationTokenSource();
                
                try
                {
                    // 300ms防抖
                    await Task.Delay(300, _debounceCts.Token);
                    
                    // 生成搜索建议
                    await GenerateSearchSuggestions(sender.Text);
                }
                catch (TaskCanceledException)
                {
                    // 防抖任务被取消，忽略
                }
            }
        }

        /// <summary>
        /// 生成搜索建议
        /// </summary>
        /// <param name="query">搜索查询</param>
        private async Task GenerateSearchSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                // 如果查询为空，显示搜索历史
                SearchSuggestions.Clear();
                foreach (var historyItem in SearchHistory)
                {
                    SearchSuggestions.Add(historyItem);
                }
                return;
            }
            
            // 清空当前搜索建议
            SearchSuggestions.Clear();
            
            // 模拟搜索建议生成（实际项目中应从ViewModel获取数据）
            var suggestions = new List<string>
            {
                $"人员: {query}",
                $"哨位: {query}",
                $"日期: {query}",
                $"关键词: {query}",
                $"冲突: {query}",
                $"夜哨: {query}",
                $"手动: {query}"
            };
            
            // 添加搜索建议
            foreach (var suggestion in suggestions)
            {
                SearchSuggestions.Add(suggestion);
            }
        }

        /// <summary>
        /// 搜索提交事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="args">事件参数</param>
        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var queryText = args.QueryText;
            
            // 保存搜索历史
            SaveSearchHistory(queryText);
            
            // 触发搜索命令（实际项目中应绑定到ViewModel的搜索命令）
            // ViewModel.SearchCommand.Execute(queryText);
        }

        /// <summary>
        /// 选择搜索建议事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="args">事件参数</param>
        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string selectedSuggestion)
            {
                sender.Text = selectedSuggestion;
            }
        }

        /// <summary>
        /// 保存搜索历史记录
        /// </summary>
        /// <param name="query">搜索查询</param>
        private void SaveSearchHistory(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;
            
            // 移除已存在的相同记录
            var existingItem = SearchHistory.FirstOrDefault(item => item.Equals(query, StringComparison.OrdinalIgnoreCase));
            if (existingItem != null)
            {
                SearchHistory.Remove(existingItem);
            }
            
            // 添加到历史记录开头
            SearchHistory.Insert(0, query);
            
            // 只保留最近10条历史记录
            while (SearchHistory.Count > 10)
            {
                SearchHistory.RemoveAt(SearchHistory.Count - 1);
            }
            
            // 实际项目中应保存到本地存储
            // ApplicationData.Current.LocalSettings.Values["SearchHistory"] = JsonConvert.SerializeObject(SearchHistory);
        }

        /// <summary>
        /// 加载搜索历史记录
        /// </summary>
        private void LoadSearchHistory()
        {
            // 实际项目中应从本地存储加载
            // var historyJson = ApplicationData.Current.LocalSettings.Values["SearchHistory"] as string;
            // if (!string.IsNullOrWhiteSpace(historyJson))
            // {
            //     var history = JsonConvert.DeserializeObject<List<string>>(historyJson);
            //     foreach (var item in history)
            //     {
            //         SearchHistory.Add(item);
            //     }
            // }
            
            // 模拟搜索历史记录
            var mockHistory = new List<string>
            {
                "张三",
                "哨位A",
                "今天",
                "夜哨",
                "有冲突"
            };
            
            foreach (var item in mockHistory)
            {
                SearchHistory.Add(item);
            }
        }

        /// <summary>
        /// 折叠/展开按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // 按钮点击时会自动显示Flyout，这里不需要额外处理
        }

        /// <summary>
        /// 快速筛选按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void QuickFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filterType)
            {
                // 根据筛选类型执行不同的筛选操作
                switch (filterType)
                {
                    case "today":
                        ApplyDateFilter(DateTime.Today, DateTime.Today);
                        break;
                    case "week":
                        var today = DateTime.Today;
                        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                        var endOfWeek = startOfWeek.AddDays(6);
                        ApplyDateFilter(startOfWeek, endOfWeek);
                        break;
                    case "month":
                        var currentMonth = DateTime.Today;
                        var startOfMonth = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                        ApplyDateFilter(startOfMonth, endOfMonth);
                        break;
                    case "day_shift":
                        ApplyTimeSlotFilter(0, 5); // 假设0-5时段为日哨
                        break;
                    case "night_shift":
                        ApplyTimeSlotFilter(6, 11); // 假设6-11时段为夜哨
                        break;
                    case "has_conflict":
                        ApplyConflictFilter(true);
                        break;
                    case "assigned":
                        ApplyAssignmentFilter(true);
                        break;
                    case "unassigned":
                        ApplyAssignmentFilter(false);
                        break;
                    case "all_positions":
                        ClearPositionFilter();
                        break;
                    case "all_personnel":
                        ClearPersonnelFilter();
                        break;
                }
            }
        }

        /// <summary>
        /// 应用日期筛选
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        private void ApplyDateFilter(DateTime startDate, DateTime endDate)
        {
            // 实际项目中应该调用ViewModel的方法来应用筛选
            // ViewModel.ApplyDateFilter(startDate, endDate);
        }

        /// <summary>
        /// 应用时段筛选
        /// </summary>
        /// <param name="startPeriod">开始时段索引</param>
        /// <param name="endPeriod">结束时段索引</param>
        private void ApplyTimeSlotFilter(int startPeriod, int endPeriod)
        {
            // 实际项目中应该调用ViewModel的方法来应用筛选
            // ViewModel.ApplyTimeSlotFilter(startPeriod, endPeriod);
        }

        /// <summary>
        /// 应用冲突筛选
        /// </summary>
        /// <param name="hasConflict">是否有冲突</param>
        private void ApplyConflictFilter(bool hasConflict)
        {
            // 实际项目中应该调用ViewModel的方法来应用筛选
            // ViewModel.ApplyConflictFilter(hasConflict);
        }

        /// <summary>
        /// 应用分配状态筛选
        /// </summary>
        /// <param name="isAssigned">是否已分配</param>
        private void ApplyAssignmentFilter(bool isAssigned)
        {
            // 实际项目中应该调用ViewModel的方法来应用筛选
            // ViewModel.ApplyAssignmentFilter(isAssigned);
        }

        /// <summary>
        /// 清除哨位筛选
        /// </summary>
        private void ClearPositionFilter()
        {
            // 实际项目中应该调用ViewModel的方法来清除筛选
            // ViewModel.ClearPositionFilter();
        }

        /// <summary>
        /// 清除人员筛选
        /// </summary>
        private void ClearPersonnelFilter()
        {
            // 实际项目中应该调用ViewModel的方法来清除筛选
            // ViewModel.ClearPersonnelFilter();
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
}