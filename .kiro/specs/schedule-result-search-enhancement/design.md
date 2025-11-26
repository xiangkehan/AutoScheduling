# 设计文档

## 概述

本设计文档描述了排班结果页面人员筛选功能的增强方案。该功能通过复用现有的高亮机制（`HighlightedCellKeys`）和右侧面板（`SplitView`），为用户提供直观的搜索结果展示。核心设计理念是基于班次 ID 而非坐标进行高亮管理，确保跨视图兼容性和未来扩展性。

## 架构

### 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                    ScheduleResultPage                        │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Filter Toolbar (人员筛选器)                            │ │
│  │  [日期] [哨位] [人员搜索框] [应用] [重置]              │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌──────────────────────────┬───────────────────────────┐   │
│  │                          │  Right Pane (SplitView)   │   │
│  │  Schedule Grid           │  ┌─────────────────────┐  │   │
│  │  (排班表格)              │  │ TabView             │  │   │
│  │                          │  │ ┌─────┬──────────┐  │  │   │
│  │  ┌────┬────┬────┐        │  │ │搜索 │ 冲突管理 │  │  │   │
│  │  │ 🟡 │    │ 🟡 │        │  │ └─────┴──────────┘  │  │   │
│  │  ├────┼────┼────┤        │  │                     │  │   │
│  │  │    │ 🟢 │    │ ← 焦点 │  │  Search Results     │  │   │
│  │  ├────┼────┼────┤        │  │  ┌───────────────┐  │  │   │
│  │  │ 🟡 │    │    │        │  │  │ 📋 班次列表   │  │  │   │
│  │  └────┴────┴────┘        │  │  │ • 2024-01-01  │  │  │   │
│  │                          │  │  │ • 2024-01-02  │  │  │   │
│  │                          │  │  │ • 2024-01-03  │  │  │   │
│  │                          │  │  └───────────────┘  │  │   │
│  │                          │  │  [◀上一个][下一个▶] │  │   │
│  └──────────────────────────┴───────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘

图例：
🟡 = 搜索结果高亮 (Search Result Highlight)
🟢 = 焦点高亮 (Focus Highlight)
```

### 组件层次

```
ScheduleResultPage (View)
├── FilterToolbar (现有)
│   └── PersonnelSearchBox (增强)
├── ScheduleGrid (现有)
│   └── HighlightedCells (复用现有机制)
└── SplitView.Pane (现有)
    └── TabView (新增)
        ├── SearchResultsTab (新增)
        │   ├── FilterSummary
        │   ├── ResultCount
        │   ├── ShiftResultList
        │   └── NavigationButtons (新增)
        │       ├── PreviousButton
        │       └── NextButton
        └── ConflictManagementTab (现有，重构)
            └── ConflictPanel (现有内容)
```

## 组件和接口

### 1. ViewModel 扩展 (ScheduleResultViewModel.Search.cs)

创建新的 partial class 文件来管理搜索相关功能：

```csharp
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
            set => SetProperty(ref _hasActiveSearch, value);
        }

        /// <summary>
        /// 右侧面板当前激活的标签页索引（0=搜索结果，1=冲突管理）
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
        /// 应用筛选（重写现有方法以支持搜索功能）
        /// </summary>
        private async Task ApplyFiltersAsync()
        {
            if (Schedule == null) return;

            try
            {
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

                // 更新搜索结果
                SearchResults = new ObservableCollection<SearchResultItem>(
                    matchedShifts.Select(s => new SearchResultItem(s)));

                // 更新高亮
                await UpdateHighlightsAsync(matchedShifts);

                // 更新状态
                HasActiveSearch = matchedShifts.Any();
                
                if (HasActiveSearch)
                {
                    // 切换到搜索结果标签页
                    RightPaneTabIndex = 0;
                    
                    // 滚动到第一个结果
                    CurrentHighlightIndex = 0;
                    await ScrollToHighlightAsync(0);
                }

                // 通知命令状态变化
                NavigateToPreviousHighlightCommand?.NotifyCanExecuteChanged();
                NavigateToNextHighlightCommand?.NotifyCanExecuteChanged();
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

            // 生成高亮键（基于班次 ID 和视图类型）
            var highlightKeys = new HashSet<string>();
            var viewType = GetCurrentViewTypeString();

            foreach (var shift in matchedShifts)
            {
                var key = GenerateCellKey(shift.Id, viewType);
                highlightKeys.Add(key);
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
        /// 重置筛选（重写现有方法）
        /// </summary>
        private async Task ResetFiltersAsync()
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
            _ = ScrollToHighlightAsync(CurrentHighlightIndex);
        }

        /// <summary>
        /// 导航到下一个高亮结果
        /// </summary>
        private void NavigateToNextHighlight()
        {
            if (!CanNavigateToNextHighlight()) return;

            CurrentHighlightIndex++;
            _ = ScrollToHighlightAsync(CurrentHighlightIndex);
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

        #region 视图切换支持

        /// <summary>
        /// 当视图模式改变时（重写现有方法）
        /// </summary>
        private async Task OnViewModeChangedAsync(ViewMode newMode)
        {
            // 调用原有逻辑
            // ... 现有代码 ...

            // 如果有活动搜索，重新映射高亮
            if (HasActiveSearch && SearchResults.Any())
            {
                var matchedShifts = SearchResults.Select(r => r.Shift).ToList();
                await UpdateHighlightsAsync(matchedShifts);
            }
        }

        #endregion
    }
}
```

## 数据模型

### SearchResultItem

```csharp
namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 搜索结果项
    /// </summary>
    public class SearchResultItem
    {
        /// <summary>
        /// 班次数据
        /// </summary>
        public ShiftDto Shift { get; set; }

        /// <summary>
        /// 班次 ID
        /// </summary>
        public int ShiftId => Shift.Id;

        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date => Shift.StartTime.Date;

        /// <summary>
        /// 星期
        /// </summary>
        public string DayOfWeek => Shift.StartTime.ToString("dddd");

        /// <summary>
        /// 时段名称
        /// </summary>
        public string PeriodName => GetPeriodName(Shift.PeriodIndex);

        /// <summary>
        /// 哨位名称
        /// </summary>
        public string PositionName => Shift.PositionName;

        /// <summary>
        /// 人员姓名
        /// </summary>
        public string PersonnelName => Shift.PersonnelName;

        /// <summary>
        /// 是否为夜哨
        /// </summary>
        public bool IsNightShift => Shift.PeriodIndex is 11 or 0 or 1 or 2;

        /// <summary>
        /// 是否为手动分配
        /// </summary>
        public bool IsManualAssignment => Shift.IsManualAssignment;

        /// <summary>
        /// 是否存在冲突
        /// </summary>
        public bool HasConflict { get; set; }

        /// <summary>
        /// 在当前视图中的行索引
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// 在当前视图中的列索引
        /// </summary>
        public int ColumnIndex { get; set; }

        /// <summary>
        /// 显示文本
        /// </summary>
        public string DisplayText => $"{Date:yyyy-MM-dd} {DayOfWeek} {PeriodName} - {PositionName} - {PersonnelName}";

        public SearchResultItem(ShiftDto shift)
        {
            Shift = shift ?? throw new ArgumentNullException(nameof(shift));
        }

        private string GetPeriodName(int periodIndex)
        {
            return periodIndex switch
            {
                0 => "00:00-02:00",
                1 => "02:00-04:00",
                2 => "04:00-06:00",
                3 => "06:00-08:00",
                4 => "08:00-10:00",
                5 => "10:00-12:00",
                6 => "12:00-14:00",
                7 => "14:00-16:00",
                8 => "16:00-18:00",
                9 => "18:00-20:00",
                10 => "20:00-22:00",
                11 => "22:00-00:00",
                _ => "未知"
            };
        }
    }
}
```

### SearchFilters

```csharp
namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 搜索筛选条件
    /// </summary>
    public class SearchFilters
    {
        /// <summary>
        /// 人员 ID
        /// </summary>
        public int? PersonnelId { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 哨位 ID 列表
        /// </summary>
        public List<int>? PositionIds { get; set; }

        /// <summary>
        /// 是否有任何活动筛选
        /// </summary>
        public bool HasAnyFilter =>
            PersonnelId.HasValue ||
            StartDate.HasValue ||
            EndDate.HasValue ||
            (PositionIds != null && PositionIds.Any());

        /// <summary>
        /// 获取筛选条件摘要
        /// </summary>
        public string GetSummary()
        {
            var parts = new List<string>();

            if (PersonnelId.HasValue)
                parts.Add($"人员ID: {PersonnelId}");
            
            if (StartDate.HasValue)
                parts.Add($"开始: {StartDate:yyyy-MM-dd}");
            
            if (EndDate.HasValue)
                parts.Add($"结束: {EndDate:yyyy-MM-dd}");
            
            if (PositionIds != null && PositionIds.Any())
                parts.Add($"哨位: {PositionIds.Count}个");

            return parts.Any() ? string.Join(", ", parts) : "无筛选条件";
        }
    }
}
```

## 错误处理

### 错误场景

1. **搜索超时**: 当排班表包含大量班次时，搜索可能超时
   - 处理：使用 `Task.Delay` 模拟异步操作，添加超时机制
   - 用户反馈：显示加载指示器和超时提示

2. **视图切换时高亮丢失**: 不同视图的坐标系统不同
   - 处理：基于班次 ID 重新计算坐标
   - 降级：如果无法映射，清除高亮并提示用户

3. **无效的班次 ID**: 高亮键引用的班次不存在
   - 处理：过滤掉无效的高亮键
   - 日志：记录警告信息

4. **并发搜索**: 用户快速连续触发多次搜索
   - 处理：使用防抖机制（300ms）
   - 取消：取消前一个未完成的搜索任务

## 正确性属性

*属性是一个特征或行为，应该在系统的所有有效执行中保持为真——本质上是关于系统应该做什么的正式陈述。属性作为人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1: 筛选结果一致性
*对于任意*排班数据和筛选条件，搜索返回的班次集合应该与高亮键集合表示的班次集合完全一致
**验证需求: 1.1, 2.2**

### 属性 2: 高亮状态转换
*对于任意*两个不同的人员筛选，切换选择应该导致高亮集合完全替换，且新高亮集合仅包含新选中人员的班次
**验证需求: 1.2**

### 属性 3: 重置幂等性
*对于任意*筛选状态，执行重置操作后，所有筛选条件、高亮集合和搜索结果应该为空
**验证需求: 1.3, 4.6**

### 属性 4: 滚动事件触发
*对于任意*非空搜索结果，应用筛选后应该触发 ScrollToCellRequested 事件，且事件参数指向第一个结果的坐标
**验证需求: 1.4**

### 属性 5: 导航按钮状态
*对于任意*搜索结果集合，当结果数量大于 1 时，导航按钮应该可用；当索引为 0 时"上一个"禁用，当索引为最后一个时"下一个"禁用
**验证需求: 1.5**

### 属性 6: 焦点高亮唯一性
*对于任意*时刻，最多只有一个班次拥有焦点高亮（FocusedShiftId 为 null 或指向单个班次）
**验证需求: 1.6, 2.4, 2.6**

### 属性 7: 跨视图高亮保持
*对于任意*视图切换，高亮的班次 ID 集合应该保持不变，只有单元格键的格式发生变化
**验证需求: 1.7**

### 属性 8: 单元格键格式
*对于任意*班次 ID 和视图类型，生成的单元格键应该匹配正则表达式 `^shift_\d+_(Grid|ByPosition|ByPersonnel|List)$`
**验证需求: 1.8, 7.1**

### 属性 9: AND 逻辑组合
*对于任意*多个筛选条件的组合，搜索结果应该是所有单个条件结果的交集
**验证需求: 4.1, 4.2, 4.3**

### 属性 10: 筛选条件摘要完整性
*对于任意*非空筛选条件，GetSummary() 返回的字符串应该包含所有已设置条件的描述
**验证需求: 4.4**

### 属性 11: 实时更新响应性
*对于任意*筛选条件的修改，应该触发搜索操作并更新 SearchResults 和 HighlightedCellKeys
**验证需求: 4.5**

### 属性 12: 显示文本完整性
*对于任意*班次，SearchResultItem.DisplayText 应该包含日期、星期、时段、哨位和人员信息
**验证需求: 5.1**

### 属性 13: 夜哨判断正确性
*对于任意*班次，当且仅当 PeriodIndex 为 11, 0, 1, 2 时，IsNightShift 应该为 true
**验证需求: 5.2**

### 属性 14: 手动分配映射
*对于任意*班次，SearchResultItem.IsManualAssignment 应该等于 Shift.IsManualAssignment
**验证需求: 5.3**

### 属性 15: 搜索性能约束
*对于任意*包含 1000+ 班次的排班表，搜索和高亮操作应该在 500 毫秒内完成
**验证需求: 6.1**

### 属性 16: 防抖机制
*对于任意*在 300 毫秒内的连续搜索触发，只有最后一次应该实际执行
**验证需求: 6.2**

### 属性 17: 视图切换性能约束
*对于任意*视图切换操作，高亮重映射应该在 300 毫秒内完成
**验证需求: 6.4**

### 属性 18: 加载状态指示
*对于任意*搜索操作，在操作开始时 IsLoading 应该为 true，操作完成后应该为 false
**验证需求: 6.5**

### 属性 19: 班次 ID 到坐标映射
*对于任意*有效的班次 ID 和视图类型，应该能够计算出对应的行列坐标
**验证需求: 7.2**

## 测试策略

### 单元测试

单元测试用于验证特定示例和边界情况：

1. **SearchFilters 测试**
   - 测试空筛选条件时 `HasAnyFilter` 返回 false
   - 测试单个条件时 `HasAnyFilter` 返回 true
   - 测试 `GetSummary()` 在无条件时返回"无筛选条件"
   - 测试 `GetSummary()` 在有条件时包含所有条件描述

2. **SearchResultItem 测试**
   - 测试时段 0-11 的名称映射正确性
   - 测试边界时段（11, 0, 1, 2）的夜哨判断
   - 测试非夜哨时段（3-10）的夜哨判断
   - 测试 DisplayText 格式符合预期

3. **单元格键生成测试**
   - 测试 Grid 视图的键格式
   - 测试 ByPosition 视图的键格式
   - 测试 ByPersonnel 视图的键格式
   - 测试 List 视图的键格式

4. **边界情况测试**
   - 测试空搜索结果的处理
   - 测试单个搜索结果的导航按钮状态
   - 测试第一个/最后一个结果的导航限制

### 属性测试

使用属性测试框架（如 FsCheck 或 CsCheck）验证通用属性：

1. **属性 1: 筛选结果一致性**
   ```csharp
   // Feature: schedule-result-search-enhancement, Property 1: 筛选结果一致性
   [Property(MinimumNumberOfTests = 100)]
   public Property FilterResultConsistency()
   {
       return Prop.ForAll(
           GenerateScheduleAndFilters(),
           async (schedule, filters) =>
           {
               var viewModel = CreateViewModel(schedule);
               await viewModel.ApplyFiltersAsync();
               
               var searchResultShiftIds = viewModel.SearchResults
                   .Select(r => r.ShiftId)
                   .ToHashSet();
               
               var highlightedShiftIds = viewModel.HighlightedCellKeys
                   .Select(k => ExtractShiftIdFromKey(k))
                   .ToHashSet();
               
               return searchResultShiftIds.SetEquals(highlightedShiftIds);
           });
   }
   ```

2. **属性 8: 单元格键格式**
   ```csharp
   // Feature: schedule-result-search-enhancement, Property 8: 单元格键格式
   [Property(MinimumNumberOfTests = 100)]
   public Property CellKeyFormat()
   {
       return Prop.ForAll(
           Arb.From<int>().Where(id => id > 0),
           Arb.From<ViewMode>(),
           (shiftId, viewMode) =>
           {
               var key = GenerateCellKey(shiftId, viewMode.ToString());
               var regex = new Regex(@"^shift_\d+_(Grid|ByPosition|ByPersonnel|List)$");
               return regex.IsMatch(key);
           });
   }
   ```

3. **属性 9: AND 逻辑组合**
   ```csharp
   // Feature: schedule-result-search-enhancement, Property 9: AND 逻辑组合
   [Property(MinimumNumberOfTests = 100)]
   public Property AndLogicCombination()
   {
       return Prop.ForAll(
           GenerateScheduleAndMultipleFilters(),
           async (schedule, filters) =>
           {
               var viewModel = CreateViewModel(schedule);
               
               // 应用所有筛选条件
               await viewModel.ApplyFiltersAsync();
               var combinedResults = viewModel.SearchResults.Select(r => r.ShiftId).ToHashSet();
               
               // 分别应用每个筛选条件并求交集
               var individualResults = new List<HashSet<int>>();
               foreach (var filter in filters)
               {
                   var vm = CreateViewModel(schedule);
                   await vm.ApplyFiltersAsync();
                   individualResults.Add(vm.SearchResults.Select(r => r.ShiftId).ToHashSet());
               }
               
               var expectedIntersection = individualResults.Aggregate((a, b) => a.Intersect(b).ToHashSet());
               
               return combinedResults.SetEquals(expectedIntersection);
           });
   }
   ```

4. **属性 13: 夜哨判断正确性**
   ```csharp
   // Feature: schedule-result-search-enhancement, Property 13: 夜哨判断正确性
   [Property(MinimumNumberOfTests = 100)]
   public Property NightShiftDetection()
   {
       return Prop.ForAll(
           GenerateShift(),
           shift =>
           {
               var item = new SearchResultItem(shift);
               var expectedIsNight = shift.PeriodIndex is 11 or 0 or 1 or 2;
               return item.IsNightShift == expectedIsNight;
           });
   }
   ```

### 集成测试

集成测试验证组件之间的交互：

1. **筛选功能测试**
   - 测试人员筛选 + 日期筛选的组合
   - 测试人员筛选 + 哨位筛选的组合
   - 测试所有筛选条件的组合
   - 测试重置筛选后状态恢复

2. **高亮功能测试**
   - 测试应用筛选后高亮键的生成
   - 测试视图切换后高亮键的重映射
   - 测试焦点高亮的应用和移除
   - 测试高亮与搜索结果的同步

3. **导航功能测试**
   - 测试导航按钮的启用/禁用状态
   - 测试导航时 FocusedShiftId 的更新
   - 测试导航时滚动事件的触发
   - 测试边界条件（第一个/最后一个）

4. **标签页切换测试**
   - 测试应用筛选后自动切换到搜索结果标签页
   - 测试无活动搜索时隐藏搜索结果标签页
   - 测试手动切换标签页的行为

### 性能测试

性能测试验证系统在负载下的表现：

1. **大数据量测试**
   - 生成 1000+ 班次的排班表
   - 测量搜索和高亮操作的执行时间
   - 验证执行时间 < 500ms（属性 15）
   - 测量视图切换时高亮重映射的时间
   - 验证执行时间 < 300ms（属性 17）

2. **防抖测试**
   - 在 300ms 内连续触发 10 次搜索
   - 验证只有最后一次实际执行（属性 16）
   - 使用 mock 计数器验证执行次数

3. **渲染性能测试**
   - 生成 100+ 搜索结果
   - 测量列表渲染时间
   - 验证使用虚拟化后的性能提升

### 测试工具和框架

- **单元测试**: xUnit + Moq
- **属性测试**: CsCheck 或 FsCheck.Xunit
- **性能测试**: BenchmarkDotNet
- **UI 测试**: WinAppDriver（可选）

### 测试覆盖率目标

- 代码覆盖率: > 80%
- 属性测试覆盖率: 所有 19 个属性都有对应的测试
- 边界情况覆盖率: 所有已识别的边界情况都有测试

## 文件结构设计

### 新增文件

```
ViewModels/Scheduling/
├── ScheduleResultViewModel.Search.cs (新增, ~400 行)
│   └── 搜索功能的 partial class

DTOs/
├── SearchResultItem.cs (新增, ~80 行)
│   └── 搜索结果项数据模型
└── SearchFilters.cs (新增, ~50 行)
    └── 搜索筛选条件模型

Views/Scheduling/
└── ScheduleResultPage.xaml (修改, +150 行)
    ├── 添加 TabView
    ├── 添加搜索结果列表
    ├── 添加导航按钮（位于搜索结果底部）
    └── 重构右侧面板结构
```

### 修改文件

```
ViewModels/Scheduling/
├── ScheduleResultViewModel.cs (修改, ~50 行)
│   ├── 添加 InitializeSearchCommands() 调用
│   ├── 修改 ApplyFiltersAsync() 方法签名
│   └── 修改 ResetFiltersAsync() 方法签名
└── ScheduleResultViewModel.Conflicts.cs (修改, ~20 行)
    └── 调整冲突面板在 TabView 中的位置

Views/Scheduling/
└── ScheduleResultPage.xaml.cs (修改, ~30 行)
    └── 添加导航按钮事件处理
```

### 文件大小预估

| 文件 | 类型 | 预估行数 | 说明 |
|------|------|----------|------|
| ScheduleResultViewModel.Search.cs | 新增 | 400 | 搜索功能核心逻辑 |
| SearchResultItem.cs | 新增 | 80 | 搜索结果数据模型 |
| SearchFilters.cs | 新增 | 50 | 筛选条件模型 |
| ScheduleResultPage.xaml | 修改 | +150 | UI 结构调整 |
| ScheduleResultViewModel.cs | 修改 | +50 | 集成搜索功能 |
| ScheduleResultViewModel.Conflicts.cs | 修改 | +20 | TabView 适配 |
| ScheduleResultPage.xaml.cs | 修改 | +30 | 事件处理 |

**总计**: 新增 ~530 行，修改 ~250 行

所有文件均符合 1000 行限制，最大的文件（ScheduleResultViewModel.Search.cs）仅 400 行。

## 依赖关系

```
ScheduleResultViewModel.Search.cs
├── 依赖 ScheduleResultViewModel.cs (基类)
├── 依赖 ScheduleResultViewModel.Conflicts.cs (共享高亮机制)
├── 依赖 SearchResultItem.cs (数据模型)
├── 依赖 SearchFilters.cs (筛选条件)
├── 依赖 ShiftDto.cs (现有)
└── 依赖 DialogService (现有)

ScheduleResultPage.xaml
├── 绑定 ScheduleResultViewModel (现有)
├── 使用 TabView (WinUI 3 控件)
├── 使用 SplitView (现有)
└── 使用 AutoSuggestBox (现有)
```

## 实现顺序

1. **阶段 1: 数据模型** (~1 小时)
   - 创建 `SearchResultItem.cs`
   - 创建 `SearchFilters.cs`

2. **阶段 2: ViewModel 核心逻辑** (~3 小时)
   - 创建 `ScheduleResultViewModel.Search.cs`
   - 实现搜索和筛选逻辑
   - 实现高亮管理逻辑

3. **阶段 3: UI 结构调整** (~2 小时)
   - 修改 `ScheduleResultPage.xaml`
   - 添加 TabView 和导航按钮
   - 重构右侧面板

4. **阶段 4: 事件处理和集成** (~1 小时)
   - 修改 `ScheduleResultPage.xaml.cs`
   - 集成到主 ViewModel
   - 测试基本功能

5. **阶段 5: 视图切换支持** (~2 小时)
   - 实现跨视图高亮映射
   - 测试不同视图模式

6. **阶段 6: 优化和测试** (~2 小时)
   - 性能优化
   - 单元测试和集成测试
   - Bug 修复

**总预估时间**: 11 小时
