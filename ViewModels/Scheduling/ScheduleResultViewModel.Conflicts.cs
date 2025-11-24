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
        /// 冲突类型筛选器（All, hard, soft, info, unassigned）
        /// </summary>
        private string _conflictTypeFilter = "All";
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
        /// 冲突严重程度筛选器（All, 1-5）
        /// </summary>
        private string _conflictSeverityFilter = "All";
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
        /// 冲突排序方式（Type, Date, Severity）
        /// </summary>
        private string _conflictSortBy = "Type";
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
            if (ConflictTypeFilter != "All")
            {
                filtered = filtered.Where(c => c.Type == ConflictTypeFilter);
            }

            // 按严重程度筛选
            if (ConflictSeverityFilter != "All")
            {
                if (int.TryParse(ConflictSeverityFilter, out var severity))
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
                "Type" => filtered.OrderBy(c => c.Type).ThenBy(c => c.SubType),
                "Date" => filtered.OrderBy(c => c.StartTime),
                "Severity" => filtered.OrderByDescending(c => c.Severity),
                _ => filtered
            };

            FilteredConflicts = new ObservableCollection<ConflictDto>(filtered);
        }

        #endregion

        #region 冲突定位

        /// <summary>
        /// 定位冲突到表格
        /// </summary>
        private async Task LocateConflictInGridAsync(ConflictDto? conflict)
        {
            if (conflict == null || Schedule == null || GridData == null) return;

            try
            {
                // 清除之前的高亮
                HighlightedCellKeys.Clear();

                // 根据冲突相关的班次ID找到对应的单元格
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
                        HighlightedCellKeys.Add(cellKey);
                    }
                }

                // 触发UI更新
                OnPropertyChanged(nameof(HighlightedCellKeys));

                // TODO: 触发滚动到第一个高亮单元格（需要在View中实现）
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("定位冲突失败", ex);
            }
        }

        /// <summary>
        /// 清除所有高亮
        /// </summary>
        private void ClearHighlights()
        {
            HighlightedCellKeys.Clear();
            OnPropertyChanged(nameof(HighlightedCellKeys));
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
            if (conflict == null) return;

            try
            {
                // TODO: 打开修复对话框
                await _dialogService.ShowWarningAsync("功能开发中", "冲突修复功能正在开发中");
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
}
