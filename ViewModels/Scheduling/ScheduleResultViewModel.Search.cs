using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// ScheduleResultViewModel 的搜索功能部分
    /// </summary>
    public partial class ScheduleResultViewModel
    {
        #region 搜索相关属性

        /// <summary>
        /// 搜索结果列表
        /// </summary>
        private ObservableCollection<SearchResultItem> _searchResults = new();
        public ObservableCollection<SearchResultItem> SearchResults
        {
            get => _searchResults;
            set => SetProperty(ref _searchResults, value);
        }

        /// <summary>
        /// 当前选中的搜索结果项
        /// </summary>
        private SearchResultItem? _selectedSearchResult;
        public SearchResultItem? SelectedSearchResult
        {
            get => _selectedSearchResult;
            set
            {
                if (SetProperty(ref _selectedSearchResult, value))
                {
                    _ = OnSearchResultSelectedAsync(value);
                }
            }
        }

        /// <summary>
        /// 当前焦点高亮的班次 ID
        /// </summary>
        private int? _focusedShiftId;
        public int? FocusedShiftId
        {
            get => _focusedShiftId;
            set => SetProperty(ref _focusedShiftId, value);
        }

        /// <summary>
        /// 当前高亮结果的索引（用于导航）
        /// </summary>
        private int _currentHighlightIndex = 0;
        public int CurrentHighlightIndex
        {
            get => _currentHighlightIndex;
            set => SetProperty(ref _currentHighlightIndex, value);
        }

        /// <summary>
        /// 是否有活动的搜索筛选
        /// </summary>
        private bool _hasActiveSearch;
        public bool HasActiveSearch
        {
            get => _hasActiveSearch;
            set
            {
                if (SetProperty(ref _hasActiveSearch, value))
                {
                    OnPropertyChanged(nameof(IsSearchResultsTabVisible));
                }
            }
        }

        /// <summary>
        /// 右侧面板当前激活的标签页索引（0=搜索筛选，1=冲突管理）
        /// </summary>
        private int _rightPaneTabIndex = 1; // 默认显示冲突管理
        public int RightPaneTabIndex
        {
            get => _rightPaneTabIndex;
            set => SetProperty(ref _rightPaneTabIndex, value);
        }

        /// <summary>
        /// 搜索结果标签页是否可见
        /// </summary>
        public bool IsSearchResultsTabVisible => HasActiveSearch;

        /// <summary>
        /// 防抖计时器
        /// </summary>
        private CancellationTokenSource? _debounceTokenSource;

        /// <summary>
        /// 防抖延迟（毫秒）
        /// </summary>
        private const int DebounceDelayMs = 300;

        #endregion

        #region 搜索相关命令

        /// <summary>
        /// 导航到上一个高亮结果命令
        /// </summary>
        public IRelayCommand? NavigateToPreviousHighlightCommand { get; private set; }

        /// <summary>
        /// 导航到下一个高亮结果命令
        /// </summary>
        public IRelayCommand? NavigateToNextHighlightCommand { get; private set; }

        /// <summary>
        /// 选择搜索结果项命令
        /// </summary>
        public IAsyncRelayCommand<SearchResultItem>? SelectSearchResultCommand { get; private set; }

        #endregion

        #region 搜索命令初始化

        /// <summary>
        /// 初始化搜索相关命令
        /// </summary>
        private void InitializeSearchCommands()
        {
            NavigateToPreviousHighlightCommand = new RelayCommand(
                NavigateToPreviousHighlight,
                CanNavigateToPreviousHighlight);
            
            NavigateToNextHighlightCommand = new RelayCommand(
                NavigateToNextHighlight,
                CanNavigateToNextHighlight);
            
            SelectSearchResultCommand = new AsyncRelayCommand<SearchResultItem>(
                SelectSearchResultAsync);
        }

        #endregion

        #region 搜索核心逻辑

        /// <summary>
        /// 应用筛选（增强版，支持搜索功能和防抖）
        /// </summary>
        private async Task ApplyFiltersWithSearchAsync()
        {
            // 取消之前的防抖操作
            _debounceTokenSource?.Cancel();
            _debounceTokenSource = new CancellationTokenSource();
            var token = _debounceTokenSource.Token;

            try
            {
                // 防抖延迟
                await Task.Delay(DebounceDelayMs, token);

                if (Schedule == null) return;

                IsLoading = true;

                // 收集所有筛选条件
                var filters = new SearchFilters
                {
                    PersonnelId = SelectedPersonnel?.Id,
                    StartDate = FilterStartDate != default ? FilterStartDate : null,
                    EndDate = FilterEndDate != default ? FilterEndDate : null,
                    PositionIds = SelectedPositionIds.Any() ? SelectedPositionIds.ToList() : null
                };

                // 执行搜索
                var matchedShifts = await SearchShiftsAsync(filters);

                // 检查是否已取消
                if (token.IsCancellationRequested) return;

                // 更新搜索结果（计算坐标）
                // 验证班次有效性
                var validShiftIds = Schedule?.Shifts?.Select(s => s.Id).ToHashSet() ?? new HashSet<int>();
                var searchResults = new List<SearchResultItem>();
                var invalidCount = 0;

                foreach (var shift in matchedShifts)
                {
                    // 过滤无效班次
                    if (!validShiftIds.Contains(shift.Id))
                    {
                        invalidCount++;
                        LogWarning($"搜索结果中检测到无效的班次 ID: {shift.Id}，已跳过");
                        continue;
                    }

                    var item = new SearchResultItem(shift);
                    
                    // 根据当前视图模式计算坐标
                    var (rowIndex, columnIndex) = CalculateShiftCoordinates(shift);
                    item.RowIndex = rowIndex;
                    item.ColumnIndex = columnIndex;
                    
                    searchResults.Add(item);
                }

                // 如果有无效班次，记录汇总信息
                if (invalidCount > 0)
                {
                    LogWarning($"搜索结果中共检测到 {invalidCount} 个无效班次，已过滤");
                }
                
                SearchResults = new ObservableCollection<SearchResultItem>(searchResults);

                // 更新高亮
                await UpdateHighlightsAsync(matchedShifts);

                // 更新状态
                HasActiveSearch = matchedShifts.Any();
                
                if (HasActiveSearch)
                {
                    // 滚动到第一个结果
                    CurrentHighlightIndex = 0;
                    await ScrollToHighlightAsync(0);
                }

                // 通知命令状态变化
                NavigateToPreviousHighlightCommand?.NotifyCanExecuteChanged();
                NavigateToNextHighlightCommand?.NotifyCanExecuteChanged();
            }
            catch (OperationCanceledException)
            {
                // 防抖取消，正常情况
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("应用筛选失败", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 搜索班次
        /// </summary>
        private async Task<List<ShiftDto>> SearchShiftsAsync(SearchFilters filters)
        {
            if (Schedule?.Shifts == null) return new List<ShiftDto>();

            await Task.CompletedTask; // 异步占位

            var query = Schedule.Shifts.AsEnumerable();

            // 应用人员筛选
            if (filters.PersonnelId.HasValue)
            {
                query = query.Where(s => s.PersonnelId == filters.PersonnelId.Value);
            }

            // 应用日期范围筛选
            if (filters.StartDate.HasValue)
            {
                query = query.Where(s => s.StartTime.Date >= filters.StartDate.Value.Date);
            }
            if (filters.EndDate.HasValue)
            {
                query = query.Where(s => s.StartTime.Date <= filters.EndDate.Value.Date);
            }

            // 应用哨位筛选
            if (filters.PositionIds != null && filters.PositionIds.Any())
            {
                query = query.Where(s => filters.PositionIds.Contains(s.PositionId));
            }

            return query.OrderBy(s => s.StartTime).ThenBy(s => s.PeriodIndex).ToList();
        }

        /// <summary>
        /// 更新高亮显示
        /// </summary>
        private async Task UpdateHighlightsAsync(List<ShiftDto> matchedShifts)
        {
            if (matchedShifts == null || !matchedShifts.Any())
            {
                HighlightedCellKeys = new HashSet<string>();
                return;
            }

            await Task.CompletedTask; // 异步占位

            // 获取所有有效的班次 ID 集合（用于验证）
            var validShiftIds = Schedule?.Shifts?.Select(s => s.Id).ToHashSet() ?? new HashSet<int>();

            // 生成高亮键（基于班次 ID 和视图类型）
            var highlightKeys = new HashSet<string>();
            var viewType = GetCurrentViewTypeString();
            var invalidShiftCount = 0;

            foreach (var shift in matchedShifts)
            {
                // 验证班次 ID 是否有效
                if (!validShiftIds.Contains(shift.Id))
                {
                    invalidShiftCount++;
                    LogWarning($"检测到无效的班次 ID: {shift.Id}，已跳过高亮处理");
                    continue;
                }

                var key = GenerateCellKey(shift.Id, viewType);
                highlightKeys.Add(key);
            }

            // 如果有无效班次，记录汇总信息
            if (invalidShiftCount > 0)
            {
                LogWarning($"共检测到 {invalidShiftCount} 个无效班次 ID，已从高亮集合中过滤");
            }

            HighlightedCellKeys = highlightKeys;
        }

        /// <summary>
        /// 生成单元格键
        /// </summary>
        private string GenerateCellKey(int shiftId, string viewType)
        {
            return $"shift_{shiftId}_{viewType}";
        }

        /// <summary>
        /// 获取当前视图类型字符串
        /// </summary>
        private string GetCurrentViewTypeString()
        {
            return CurrentViewMode.ToString();
        }

        /// <summary>
        /// 记录警告信息到调试输出
        /// </summary>
        private void LogWarning(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Debug.WriteLine($"[{timestamp}] [ScheduleResultViewModel.Search] [WARN] {message}");
        }

        /// <summary>
        /// 重置筛选（增强版，支持搜索功能）
        /// </summary>
        private async Task ResetFiltersWithSearchAsync()
        {
            // 清除筛选条件
            SelectedPersonnel = null;
            PersonnelSearchText = string.Empty;
            FilterStartDate = default;
            FilterEndDate = default;
            SelectedPositionIds.Clear();

            // 清除搜索结果
            SearchResults.Clear();
            HasActiveSearch = false;
            FocusedShiftId = null;
            CurrentHighlightIndex = 0;

            // 清除高亮
            HighlightedCellKeys = new HashSet<string>();

            // 切换回冲突管理标签页
            RightPaneTabIndex = 1;

            await Task.CompletedTask;
        }

        #endregion

        #region 导航逻辑

        /// <summary>
        /// 导航到上一个高亮结果
        /// </summary>
        private void NavigateToPreviousHighlight()
        {
            if (!CanNavigateToPreviousHighlight()) return;

            CurrentHighlightIndex--;
            
            // 仅滚动和高亮，不触发完整的选择逻辑
            _ = ScrollToHighlightAsync(CurrentHighlightIndex);
            
            // 通知命令状态变化
            NavigateToPreviousHighlightCommand?.NotifyCanExecuteChanged();
            NavigateToNextHighlightCommand?.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 导航到下一个高亮结果
        /// </summary>
        private void NavigateToNextHighlight()
        {
            if (!CanNavigateToNextHighlight()) return;

            CurrentHighlightIndex++;
            
            // 仅滚动和高亮，不触发完整的选择逻辑
            _ = ScrollToHighlightAsync(CurrentHighlightIndex);
            
            // 通知命令状态变化
            NavigateToPreviousHighlightCommand?.NotifyCanExecuteChanged();
            NavigateToNextHighlightCommand?.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 是否可以导航到上一个
        /// </summary>
        private bool CanNavigateToPreviousHighlight()
        {
            return HasActiveSearch && CurrentHighlightIndex > 0;
        }

        /// <summary>
        /// 是否可以导航到下一个
        /// </summary>
        private bool CanNavigateToNextHighlight()
        {
            return HasActiveSearch && CurrentHighlightIndex < SearchResults.Count - 1;
        }

        /// <summary>
        /// 滚动到指定索引的高亮结果
        /// </summary>
        private async Task ScrollToHighlightAsync(int index)
        {
            if (index < 0 || index >= SearchResults.Count) return;

            var result = SearchResults[index];
            
            // 更新焦点高亮
            FocusedShiftId = result.ShiftId;

            // 触发滚动事件
            ScrollToCellRequested?.Invoke(this, new ScrollToCellEventArgs(
                result.RowIndex,
                result.ColumnIndex));

            await Task.CompletedTask;
        }

        /// <summary>
        /// 选择搜索结果项
        /// </summary>
        private async Task SelectSearchResultAsync(SearchResultItem? item)
        {
            if (item == null) return;

            // 更新焦点高亮
            FocusedShiftId = item.ShiftId;

            // 更新当前索引
            CurrentHighlightIndex = SearchResults.IndexOf(item);

            // 滚动到对应单元格
            ScrollToCellRequested?.Invoke(this, new ScrollToCellEventArgs(
                item.RowIndex,
                item.ColumnIndex));

            // 通知命令状态变化
            NavigateToPreviousHighlightCommand?.NotifyCanExecuteChanged();
            NavigateToNextHighlightCommand?.NotifyCanExecuteChanged();

            await Task.CompletedTask;
        }

        /// <summary>
        /// 当搜索结果被选中时
        /// </summary>
        private async Task OnSearchResultSelectedAsync(SearchResultItem? item)
        {
            if (item != null)
            {
                await SelectSearchResultAsync(item);
            }
        }

        #endregion

        #region 坐标计算

        /// <summary>
        /// 根据当前视图模式计算班次的行列坐标
        /// </summary>
        private (int rowIndex, int columnIndex) CalculateShiftCoordinates(ShiftDto shift)
        {
            switch (CurrentViewMode)
            {
                case ViewMode.Grid:
                    return CalculateGridViewCoordinates(shift);
                
                case ViewMode.ByPosition:
                    return CalculatePositionViewCoordinates(shift);
                
                case ViewMode.ByPersonnel:
                    // 人员视图暂不支持坐标定位
                    return (0, 0);
                
                case ViewMode.List:
                    // 列表视图暂不支持坐标定位
                    return (0, 0);
                
                default:
                    return (0, 0);
            }
        }

        /// <summary>
        /// 计算网格视图的坐标
        /// </summary>
        private (int rowIndex, int columnIndex) CalculateGridViewCoordinates(ShiftDto shift)
        {
            if (GridData == null) return (0, 0);

            // 查找对应的行（日期+时段）
            var row = GridData.Rows.FirstOrDefault(r =>
                r.Date.Date == shift.StartTime.Date &&
                r.PeriodIndex == shift.PeriodIndex);

            // 查找对应的列（哨位）
            var col = GridData.Columns.FirstOrDefault(c =>
                c.PositionId == shift.PositionId);

            if (row != null && col != null)
            {
                return (row.RowIndex, col.ColumnIndex);
            }

            return (0, 0);
        }

        /// <summary>
        /// 计算哨位视图的坐标
        /// </summary>
        private (int rowIndex, int columnIndex) CalculatePositionViewCoordinates(ShiftDto shift)
        {
            if (Schedule == null) return (0, 0);

            // 计算周开始日期
            var daysDiff = (shift.StartTime.Date - Schedule.StartDate.Date).Days;
            var weekIndex = daysDiff / 7;
            var weekStartDate = Schedule.StartDate.AddDays(weekIndex * 7);

            // 计算星期几（0-6）
            var dayOfWeek = (shift.StartTime.Date - weekStartDate).Days;

            // 行索引 = 时段索引，列索引 = 星期几
            return (shift.PeriodIndex, dayOfWeek);
        }

        #endregion

        #region 视图切换支持

        /// <summary>
        /// 当视图模式改变时（增强版，支持高亮重映射）
        /// 注意：此方法由主 ViewModel 的 OnViewModeChangedAsync 调用
        /// </summary>
        private async Task UpdateSearchResultsForViewChange()
        {
            // 如果有活动搜索，重新计算坐标和高亮
            if (HasActiveSearch && SearchResults.Any())
            {
                // 重新计算所有搜索结果的坐标
                foreach (var item in SearchResults)
                {
                    var (rowIndex, columnIndex) = CalculateShiftCoordinates(item.Shift);
                    item.RowIndex = rowIndex;
                    item.ColumnIndex = columnIndex;
                }

                // 重新生成高亮键
                var matchedShifts = SearchResults.Select(r => r.Shift).ToList();
                await UpdateHighlightsAsync(matchedShifts);
            }
        }

        #endregion
    }
}
