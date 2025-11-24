using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// ScheduleResultViewModel 的冲突管理部分
    /// </summary>
    public partial class ScheduleResultViewModel
    {
        #region 冲突相关依赖注入

        private IConflictDetectionService? _conflictDetectionService;
        private IConflictReportService? _conflictReportService;
        private IConflictResolutionService? _conflictResolutionService;

        #endregion

        #region 冲突相关属性

        /// <summary>
        /// 所有检测到的冲突
        /// </summary>
        private ObservableCollection<ConflictDto> _allConflicts = new();
        public ObservableCollection<ConflictDto> AllConflicts
        {
            get => _allConflicts;
            set => SetProperty(ref _allConflicts, value);
        }

        /// <summary>
        /// 筛选后的冲突列表
        /// </summary>
        private ObservableCollection<ConflictDto> _filteredConflicts = new();
        public ObservableCollection<ConflictDto> FilteredConflicts
        {
            get => _filteredConflicts;
            set => SetProperty(ref _filteredConflicts, value);
        }

        /// <summary>
        /// 冲突统计信息
        /// </summary>
        private ConflictStatistics? _conflictStatistics;
        public ConflictStatistics? ConflictStatistics
        {
            get => _conflictStatistics;
            set => SetProperty(ref _conflictStatistics, value);
        }

        /// <summary>
        /// 当前选中的冲突
        /// </summary>
        private ConflictDto? _selectedConflict;
        public ConflictDto? SelectedConflict
        {
            get => _selectedConflict;
            set => SetProperty(ref _selectedConflict, value);
        }

        /// <summary>
        /// 冲突类型筛选器（全部, 硬约束, 软约束, 信息, 未分配）
        /// </summary>
        private string _conflictTypeFilter = "全部";
        public string ConflictTypeFilter
        {
            get => _conflictTypeFilter;
            set
            {
                if (SetProperty(ref _conflictTypeFilter, value))
                {
                    ApplyConflictFilters();
                }
            }
        }

        /// <summary>
        /// 冲突严重程度筛选器（全部, 1-5）
        /// </summary>
        private string _conflictSeverityFilter = "全部";
        public string ConflictSeverityFilter
        {
            get => _conflictSeverityFilter;
            set
            {
                if (SetProperty(ref _conflictSeverityFilter, value))
                {
                    ApplyConflictFilters();
                }
            }
        }

        /// <summary>
        /// 冲突排序方式（按类型, 按日期, 按严重程度）
        /// </summary>
        private string _conflictSortBy = "按类型";
        public string ConflictSortBy
        {
            get => _conflictSortBy;
            set
            {
                if (SetProperty(ref _conflictSortBy, value))
                {
                    ApplyConflictFilters();
                }
            }
        }

        /// <summary>
        /// 冲突搜索文本（按人员或哨位名称搜索）
        /// </summary>
        private string _conflictSearchText = string.Empty;
        public string ConflictSearchText
        {
            get => _conflictSearchText;
            set
            {
                if (SetProperty(ref _conflictSearchText, value))
                {
                    ApplyConflictFilters();
                }
            }
        }

        /// <summary>
        /// 高亮显示的单元格键集合（格式：rowIndex_columnIndex）
        /// </summary>
        private HashSet<string> _highlightedCellKeys = new();
        public HashSet<string> HighlightedCellKeys
        {
            get => _highlightedCellKeys;
            set => SetProperty(ref _highlightedCellKeys, value);
        }

        /// <summary>
        /// 滚动到单元格请求事件
        /// </summary>
        public event EventHandler<ScrollToCellEventArgs>? ScrollToCellRequested;

        #endregion

        #region 冲突相关命令

        /// <summary>
        /// 刷新冲突列表命令
        /// </summary>
        public IAsyncRelayCommand? RefreshConflictsCommand { get; private set; }

        /// <summary>
        /// 定位冲突命令
        /// </summary>
        public IAsyncRelayCommand<ConflictDto>? LocateConflictInGridCommand { get; private set; }

        /// <summary>
        /// 忽略冲突命令
        /// </summary>
        public IAsyncRelayCommand<ConflictDto>? IgnoreConflictInGridCommand { get; private set; }

        /// <summary>
        /// 取消忽略冲突命令
        /// </summary>
        public IAsyncRelayCommand<ConflictDto>? UnignoreConflictCommand { get; private set; }

        /// <summary>
        /// 修复冲突命令
        /// </summary>
        public IAsyncRelayCommand<ConflictDto>? FixConflictInGridCommand { get; private set; }

        /// <summary>
        /// 忽略所有软约束冲突命令
        /// </summary>
        public IAsyncRelayCommand? IgnoreAllSoftConflictsCommand { get; private set; }

        /// <summary>
        /// 导出冲突报告命令
        /// </summary>
        public IAsyncRelayCommand? ExportConflictReportCommand { get; private set; }

        /// <summary>
        /// 显示冲突趋势命令
        /// </summary>
        public IAsyncRelayCommand? ShowConflictTrendCommand { get; private set; }

        /// <summary>
        /// 清除高亮命令
        /// </summary>
        public IRelayCommand? ClearHighlightsCommand { get; private set; }

        #endregion

        #region 冲突命令初始化

        /// <summary>
        /// 初始化冲突相关命令
        /// </summary>
        private void InitializeConflictCommands()
        {
            RefreshConflictsCommand = new AsyncRelayCommand(RefreshConflictsAsync);
            LocateConflictInGridCommand = new AsyncRelayCommand<ConflictDto>(LocateConflictInGridAsync);
            IgnoreConflictInGridCommand = new AsyncRelayCommand<ConflictDto>(IgnoreConflictInGridAsync);
            UnignoreConflictCommand = new AsyncRelayCommand<ConflictDto>(UnignoreConflictAsync);
            FixConflictInGridCommand = new AsyncRelayCommand<ConflictDto>(FixConflictInGridAsync);
            IgnoreAllSoftConflictsCommand = new AsyncRelayCommand(IgnoreAllSoftConflictsAsync);
            ExportConflictReportCommand = new AsyncRelayCommand(ExportConflictReportAsync);
            ShowConflictTrendCommand = new AsyncRelayCommand(ShowConflictTrendAsync);
            ClearHighlightsCommand = new RelayCommand(ClearHighlights);
        }

        #endregion

        #region 冲突检测

        /// <summary>
        /// 检测所有冲突
        /// </summary>
        private async Task DetectConflictsAsync()
        {
            if (Schedule == null || _conflictDetectionService == null) return;

            try
            {
                var conflicts = await _conflictDetectionService.DetectConflictsAsync(Schedule);
                AllConflicts = new ObservableCollection<ConflictDto>(conflicts);
                ConflictStatistics = _conflictDetectionService.GetConflictStatistics(conflicts);
                ApplyConflictFilters();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("冲突检测失败", ex);
            }
        }

        /// <summary>
        /// 刷新冲突列表
        /// </summary>
        private async Task RefreshConflictsAsync()
        {
            await DetectConflictsAsync();
        }

        #endregion

        #region 冲突筛选和排序

        /// <summary>
        /// 应用冲突筛选和排序
        /// </summary>
        private void ApplyConflictFilters()
        {
            var filtered = AllConflicts.AsEnumerable();

            // 按类型筛选
            if (ConflictTypeFilter != "全部")
            {
                var typeFilter = ConflictTypeFilter switch
                {
                    "硬约束" => "hard",
                    "软约束" => "soft",
                    "信息" => "info",
                    "未分配" => "unassigned",
                    _ => ConflictTypeFilter
                };
                filtered = filtered.Where(c => c.Type == typeFilter);
            }

            // 按严重程度筛选
            if (ConflictSeverityFilter != "全部")
            {
                // 提取数字部分（例如 "5 - 最高" -> "5"）
                var severityStr = ConflictSeverityFilter.Split(' ')[0];
                if (int.TryParse(severityStr, out var severity))
                {
                    filtered = filtered.Where(c => c.Severity >= severity);
                }
            }

            // 按搜索文本筛选
            if (!string.IsNullOrWhiteSpace(ConflictSearchText))
            {
                filtered = filtered.Where(c =>
                    (c.PersonnelName?.Contains(ConflictSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.PositionName?.Contains(ConflictSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    c.Message.Contains(ConflictSearchText, StringComparison.OrdinalIgnoreCase));
            }

            // 排序
            filtered = ConflictSortBy switch
            {
                "按类型" => filtered.OrderBy(c => c.Type).ThenBy(c => c.SubType),
                "按日期" => filtered.OrderBy(c => c.StartTime),
                "按严重程度" => filtered.OrderByDescending(c => c.Severity),
                _ => filtered
            };

            FilteredConflicts = new ObservableCollection<ConflictDto>(filtered);
        }

        #endregion

        #region 冲突定位

        /// <summary>
        /// 定位冲突到表格（根据当前视图模式选择定位策略）
        /// </summary>
        private async Task LocateConflictInGridAsync(ConflictDto? conflict)
        {
            if (conflict == null || Schedule == null) return;

            try
            {
                // 根据当前视图模式选择不同的定位策略
                switch (CurrentViewMode)
                {
                    case ViewMode.Grid:
                        await LocateConflictInGridViewAsync(conflict);
                        break;

                    case ViewMode.ByPosition:
                        await LocateConflictInPositionViewAsync(conflict);
                        break;

                    case ViewMode.ByPersonnel:
                        // TODO: 未来支持人员视图定位
                        await _dialogService.ShowWarningAsync("人员视图暂不支持冲突定位", "请切换到网格视图或哨位视图进行定位");
                        break;

                    case ViewMode.List:
                        // TODO: 未来支持列表视图定位
                        await _dialogService.ShowWarningAsync("列表视图暂不支持冲突定位", "请切换到网格视图或哨位视图进行定位");
                        break;

                    default:
                        await _dialogService.ShowWarningAsync("当前视图不支持冲突定位");
                        break;
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("定位冲突失败", ex);
            }
        }

        /// <summary>
        /// 在网格视图中定位冲突
        /// </summary>
        private async Task LocateConflictInGridViewAsync(ConflictDto conflict)
        {
            if (GridData == null) return;

            // 创建新的高亮集合
            var newHighlightKeys = new HashSet<string>();

            int? firstRowIndex = null;
            int? firstColumnIndex = null;

            // 处理未分配冲突（没有 RelatedShiftIds）
            if (conflict.Type == "unassigned" && conflict.PositionId.HasValue && 
                conflict.StartTime.HasValue && conflict.PeriodIndex.HasValue)
            {
                // 直接根据哨位、日期和时段定位
                var row = GridData.Rows.FirstOrDefault(r =>
                    r.Date.Date == conflict.StartTime.Value.Date &&
                    r.PeriodIndex == conflict.PeriodIndex.Value);
                var col = GridData.Columns.FirstOrDefault(c =>
                    c.PositionId == conflict.PositionId.Value);

                if (row != null && col != null)
                {
                    var cellKey = $"{row.RowIndex}_{col.ColumnIndex}";
                    newHighlightKeys.Add(cellKey);
                    firstRowIndex = row.RowIndex;
                    firstColumnIndex = col.ColumnIndex;
                }
            }
            else
            {
                // 处理其他类型的冲突（根据 RelatedShiftIds）
                foreach (var shiftId in conflict.RelatedShiftIds)
                {
                    var shift = Schedule.Shifts.FirstOrDefault(s => s.Id == shiftId);
                    if (shift == null) continue;

                    // 计算单元格键
                    var row = GridData.Rows.FirstOrDefault(r =>
                        r.Date.Date == shift.StartTime.Date &&
                        r.PeriodIndex == shift.PeriodIndex);
                    var col = GridData.Columns.FirstOrDefault(c =>
                        c.PositionId == shift.PositionId);

                    if (row != null && col != null)
                    {
                        var cellKey = $"{row.RowIndex}_{col.ColumnIndex}";
                        newHighlightKeys.Add(cellKey);

                        // 记录第一个单元格的位置（用于滚动）
                        if (firstRowIndex == null)
                        {
                            firstRowIndex = row.RowIndex;
                            firstColumnIndex = col.ColumnIndex;
                        }
                    }
                }
            }

            // 更新高亮集合（触发属性变化通知）
            HighlightedCellKeys = newHighlightKeys;

            // 触发滚动到第一个高亮单元格
            if (firstRowIndex.HasValue && firstColumnIndex.HasValue)
            {
                ScrollToCellRequested?.Invoke(this, new ScrollToCellEventArgs(
                    firstRowIndex.Value, 
                    firstColumnIndex.Value));
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 在哨位视图中定位冲突
        /// </summary>
        private async Task LocateConflictInPositionViewAsync(ConflictDto conflict)
        {
            if (PositionSchedules == null || !PositionSchedules.Any())
            {
                await _dialogService.ShowWarningAsync("哨位数据未加载", "请等待数据加载完成后再试");
                return;
            }

            // 确定目标哨位和班次
            int targetPositionId;
            ShiftDto? targetShift = null;

            // 处理未分配冲突
            if (conflict.Type == "unassigned" && conflict.PositionId.HasValue)
            {
                targetPositionId = conflict.PositionId.Value;
            }
            // 处理其他类型的冲突
            else if (conflict.RelatedShiftIds.Any())
            {
                var firstShiftId = conflict.RelatedShiftIds.First();
                targetShift = Schedule.Shifts.FirstOrDefault(s => s.Id == firstShiftId);
                
                if (targetShift == null)
                {
                    await _dialogService.ShowWarningAsync("无法定位", "未找到相关班次");
                    return;
                }
                
                targetPositionId = targetShift.PositionId;
            }
            else
            {
                await _dialogService.ShowWarningAsync("无法定位", "冲突未关联任何班次或哨位");
                return;
            }

            // 查找对应的哨位
            var targetPosition = PositionSchedules.FirstOrDefault(p => p.PositionId == targetPositionId);
            if (targetPosition == null)
            {
                await _dialogService.ShowWarningAsync("无法定位", $"未找到哨位 ID：{targetPositionId}");
                return;
            }

            // 切换到目标哨位
            SelectedPositionSchedule = targetPosition;
            PositionSelectorSearchText = targetPosition.PositionName;

            // 计算目标日期和周次
            DateTime targetDate;
            int targetPeriodIndex;

            if (conflict.Type == "unassigned" && conflict.StartTime.HasValue && conflict.PeriodIndex.HasValue)
            {
                targetDate = conflict.StartTime.Value.Date;
                targetPeriodIndex = conflict.PeriodIndex.Value;
            }
            else if (targetShift != null)
            {
                targetDate = targetShift.StartTime.Date;
                targetPeriodIndex = targetShift.PeriodIndex;
            }
            else
            {
                await _dialogService.ShowWarningAsync("无法定位", "无法确定目标日期和时段");
                return;
            }

            // 计算班次所在的周次
            var daysDiff = (targetDate - Schedule.StartDate.Date).Days;
            var weekIndex = daysDiff / 7;

            // 确保周次在有效范围内
            if (weekIndex >= 0 && weekIndex < targetPosition.Weeks.Count)
            {
                CurrentWeekIndex = weekIndex;
            }
            else
            {
                await _dialogService.ShowWarningAsync("无法定位", $"目标日期不在排班范围内（周次：{weekIndex}）");
                return;
            }

            // 创建高亮集合（用于哨位视图的单元格高亮）
            var newHighlightKeys = new HashSet<string>();

            // 计算周开始日期
            var weekStartDate = Schedule.StartDate.AddDays(weekIndex * 7);

            // 为所有相关班次创建高亮键
            if (conflict.Type == "unassigned")
            {
                // 未分配冲突：只高亮一个单元格
                var dayOfWeek = (targetDate - weekStartDate).Days;
                if (dayOfWeek >= 0 && dayOfWeek < 7)
                {
                    var cellKey = $"{targetPeriodIndex}_{dayOfWeek}";
                    newHighlightKeys.Add(cellKey);
                }
            }
            else
            {
                // 其他冲突：高亮所有相关班次
                foreach (var shiftId in conflict.RelatedShiftIds)
                {
                    var relatedShift = Schedule.Shifts.FirstOrDefault(s => s.Id == shiftId);
                    if (relatedShift == null || relatedShift.PositionId != targetPositionId) continue;

                    // 计算单元格键：periodIndex_dayOfWeek
                    var shiftDate = relatedShift.StartTime.Date;
                    var dayOfWeek = (shiftDate - weekStartDate).Days;

                    if (dayOfWeek >= 0 && dayOfWeek < 7)
                    {
                        var cellKey = $"{relatedShift.PeriodIndex}_{dayOfWeek}";
                        newHighlightKeys.Add(cellKey);
                    }
                }
            }

            // 更新高亮集合
            HighlightedCellKeys = newHighlightKeys;

            // 触发滚动到第一个高亮单元格
            var firstDayOfWeek = (targetDate - weekStartDate).Days;
            if (firstDayOfWeek >= 0 && firstDayOfWeek < 7)
            {
                // 使用 periodIndex 作为行索引，dayOfWeek 作为列索引
                ScrollToCellRequested?.Invoke(this, new ScrollToCellEventArgs(
                    targetPeriodIndex,
                    firstDayOfWeek));
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 清除所有高亮
        /// </summary>
        private void ClearHighlights()
        {
            HighlightedCellKeys = new HashSet<string>();
        }

        #endregion

        #region 冲突忽略

        /// <summary>
        /// 忽略冲突
        /// </summary>
        private async Task IgnoreConflictInGridAsync(ConflictDto? conflict)
        {
            if (conflict == null) return;

            try
            {
                // 硬约束冲突不允许忽略
                if (conflict.Type == "hard")
                {
                    await _dialogService.ShowWarningAsync("硬约束冲突不能忽略", "硬约束冲突必须解决，不能忽略。");
                    return;
                }

                // 标记为已忽略
                conflict.IsIgnored = true;

                // 更新统计信息
                if (_conflictDetectionService != null)
                {
                    ConflictStatistics = _conflictDetectionService.GetConflictStatistics(AllConflicts.ToList());
                }

                // 重新应用筛选
                ApplyConflictFilters();

                // 标记有未保存的更改
                HasUnsavedChanges = true;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("忽略冲突失败", ex);
            }
        }

        /// <summary>
        /// 取消忽略冲突
        /// </summary>
        private async Task UnignoreConflictAsync(ConflictDto? conflict)
        {
            if (conflict == null) return;

            try
            {
                // 取消忽略标记
                conflict.IsIgnored = false;

                // 更新统计信息
                if (_conflictDetectionService != null)
                {
                    ConflictStatistics = _conflictDetectionService.GetConflictStatistics(AllConflicts.ToList());
                }

                // 重新应用筛选
                ApplyConflictFilters();

                // 标记有未保存的更改
                HasUnsavedChanges = true;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("取消忽略失败", ex);
            }
        }

        /// <summary>
        /// 忽略所有软约束冲突
        /// </summary>
        private async Task IgnoreAllSoftConflictsAsync()
        {
            try
            {
                var ok = await _dialogService.ShowConfirmAsync(
                    "忽略所有软约束冲突",
                    "确定要忽略所有软约束冲突吗？",
                    "确定",
                    "取消");

                if (!ok) return;

                // 标记所有软约束冲突为已忽略
                foreach (var conflict in AllConflicts.Where(c => c.Type == "soft"))
                {
                    conflict.IsIgnored = true;
                }

                // 更新统计信息
                if (_conflictDetectionService != null)
                {
                    ConflictStatistics = _conflictDetectionService.GetConflictStatistics(AllConflicts.ToList());
                }

                // 重新应用筛选
                ApplyConflictFilters();

                // 标记有未保存的更改
                HasUnsavedChanges = true;

                await _dialogService.ShowSuccessAsync($"已忽略 {AllConflicts.Count(c => c.Type == "soft")} 个软约束冲突");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("忽略所有软约束冲突失败", ex);
            }
        }

        #endregion

        #region 冲突修复

        /// <summary>
        /// 修复冲突
        /// </summary>
        private async Task FixConflictInGridAsync(ConflictDto? conflict)
        {
            if (conflict == null || Schedule == null) return;

            try
            {
                // 检查服务是否可用
                if (_conflictResolutionService == null)
                {
                    await _dialogService.ShowWarningAsync("服务不可用", "冲突修复服务未初始化");
                    return;
                }

                // 获取修复方案
                var resolutionOptions = await _conflictResolutionService.GenerateResolutionOptionsAsync(conflict, Schedule);
                
                // 创建并显示修复对话框
                var dialog = new Views.Scheduling.ConflictResolutionDialog();
                dialog.XamlRoot = App.MainWindow.Content.XamlRoot;
                
                // 初始化对话框数据
                dialog.Initialize(conflict, resolutionOptions);
                
                // 显示对话框
                var result = await dialog.ShowAsync();
                
                // 如果用户选择了修复方案
                if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary && dialog.SelectedResolution != null)
                {
                    // 应用修复方案
                    var updatedSchedule = await _conflictResolutionService.ApplyResolutionAsync(
                        dialog.SelectedResolution,
                        Schedule);
                    
                    if (updatedSchedule != null)
                    {
                        // 更新排班数据
                        Schedule = updatedSchedule;
                        
                        // 触发表格数据更新（通过 OnPropertyChanged）
                        OnPropertyChanged(nameof(GridData));
                        
                        // 根据当前视图模式刷新对应的数据
                        await RefreshCurrentViewDataAsync();
                        
                        // 重新检测冲突
                        await DetectConflictsAsync();
                        
                        // 标记有未保存的更改
                        HasUnsavedChanges = true;
                        
                        await _dialogService.ShowSuccessAsync("冲突修复成功");
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("修复冲突失败", ex);
            }
        }

        #endregion

        #region 冲突报告

        /// <summary>
        /// 导出冲突报告
        /// </summary>
        private async Task ExportConflictReportAsync()
        {
            if (Schedule == null || _conflictReportService == null) return;

            try
            {
                // TODO: 显示格式选择对话框
                await _dialogService.ShowWarningAsync("功能开发中", "冲突报告导出功能正在开发中");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("导出冲突报告失败", ex);
            }
        }

        /// <summary>
        /// 显示冲突趋势
        /// </summary>
        private async Task ShowConflictTrendAsync()
        {
            if (Schedule == null || _conflictReportService == null) return;

            try
            {
                // 创建并显示趋势对话框
                var dialog = new Views.Scheduling.ConflictTrendDialog(_conflictReportService);
                dialog.XamlRoot = App.MainWindow.Content.XamlRoot;
                
                // 初始化对话框数据
                await dialog.InitializeAsync(Schedule.Id, AllConflicts.ToList());
                
                // 显示对话框
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("显示冲突趋势失败", ex);
            }
        }

        #endregion

        #region 实时更新

        /// <summary>
        /// 当排班数据修改时，自动重新检测冲突
        /// </summary>
        private async Task OnScheduleModifiedAsync()
        {
            if (_conflictDetectionService != null)
            {
                await DetectConflictsAsync();
            }
        }

        #endregion
    }

    /// <summary>
    /// 滚动到单元格事件参数
    /// </summary>
    public class ScrollToCellEventArgs : EventArgs
    {
        /// <summary>
        /// 行索引
        /// </summary>
        public int RowIndex { get; }

        /// <summary>
        /// 列索引
        /// </summary>
        public int ColumnIndex { get; }

        public ScrollToCellEventArgs(int rowIndex, int columnIndex)
        {
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
        }
    }
}
