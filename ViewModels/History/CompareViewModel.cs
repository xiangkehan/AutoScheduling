using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.Comparison;
using AutoScheduling3.Helpers;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AutoScheduling3.ViewModels.History
{
    /// <summary>
    /// 对比视图模式
    /// </summary>
    public enum CompareViewMode
    {
        /// <summary>
        /// 网格对比视图
        /// </summary>
        Grid,

        /// <summary>
        /// 差异列表视图
        /// </summary>
        DiffList,

        /// <summary>
        /// 统计对比视图
        /// </summary>
        Statistics
    }

    /// <summary>
    /// 排班对比 ViewModel
    /// </summary>
    public partial class CompareViewModel : ViewModelBase
    {
        private readonly IHistoryService _historyService;
        private readonly IScheduleComparisonService _comparisonService;
        private readonly DialogService _dialogService;

        #region 数据源

        /// <summary>
        /// 所有可选排班表列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<HistoryScheduleDto> _schedules = new();

        /// <summary>
        /// 第二个选择器的可选排班表列表（排除已选的第一个）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<HistoryScheduleDto> _availableSchedules2 = new();

        /// <summary>
        /// 可筛选的哨位列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PositionDto> _filterablePositions = new();

        /// <summary>
        /// 可筛选的人员列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _filterablePersonnel = new();

        #endregion

        #region 选择状态

        /// <summary>
        /// 选中的排班表1
        /// </summary>
        [ObservableProperty]
        private HistoryScheduleDto? _selectedSchedule1;

        /// <summary>
        /// 选中的排班表2
        /// </summary>
        [ObservableProperty]
        private HistoryScheduleDto? _selectedSchedule2;

        /// <summary>
        /// 预选的排班表ID（从历史列表传入）
        /// </summary>
        [ObservableProperty]
        private int? _preselectedScheduleId;

        #endregion

        #region 对比结果

        /// <summary>
        /// 对比结果
        /// </summary>
        [ObservableProperty]
        private ComparisonResultDto? _comparisonResult;

        /// <summary>
        /// 筛选后的差异列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ShiftDiffDto> _filteredDifferences = new();

        /// <summary>
        /// 网格对比数据
        /// </summary>
        [ObservableProperty]
        private GridComparisonDto? _gridData;

        #endregion

        #region 视图模式

        /// <summary>
        /// 当前视图模式
        /// </summary>
        [ObservableProperty]
        private CompareViewMode _viewMode = CompareViewMode.DiffList;

        /// <summary>
        /// 可用的视图模式列表
        /// </summary>
        public IReadOnlyList<CompareViewMode> ViewModes { get; } = new List<CompareViewMode>
        {
            CompareViewMode.Grid,
            CompareViewMode.DiffList,
            CompareViewMode.Statistics
        };

        #endregion

        #region 筛选条件

        /// <summary>
        /// 差异类型筛选
        /// </summary>
        [ObservableProperty]
        private DiffType? _typeFilter;

        /// <summary>
        /// 哨位ID筛选
        /// </summary>
        [ObservableProperty]
        private int? _positionFilter;

        /// <summary>
        /// 人员ID筛选
        /// </summary>
        [ObservableProperty]
        private int? _personnelFilter;

        /// <summary>
        /// 可选的差异类型列表（用于筛选下拉框）
        /// </summary>
        public IReadOnlyList<DiffType?> DiffTypeOptions { get; } = new List<DiffType?>
        {
            null,  // 全部
            DiffType.Added,
            DiffType.Removed,
            DiffType.Changed
        };

        #endregion

        #region 状态

        /// <summary>
        /// 是否可以执行对比
        /// </summary>
        [ObservableProperty]
        private bool _canCompare;

        /// <summary>
        /// 是否已执行对比
        /// </summary>
        [ObservableProperty]
        private bool _hasCompared;

        /// <summary>
        /// 是否正在对比
        /// </summary>
        [ObservableProperty]
        private bool _isComparing;

        /// <summary>
        /// 是否需要聚焦第二个选择器
        /// </summary>
        [ObservableProperty]
        private bool _shouldFocusSchedule2Selector;

        #endregion

        #region 构造函数

        public CompareViewModel(
            IHistoryService historyService,
            IScheduleComparisonService comparisonService,
            DialogService dialogService)
        {
            _historyService = historyService;
            _comparisonService = comparisonService;
            _dialogService = dialogService;
            Title = "排班对比";
        }

        #endregion

        #region 属性变更处理

        /// <summary>
        /// 当选中的排班表1变更时
        /// </summary>
        partial void OnSelectedSchedule1Changed(HistoryScheduleDto? value)
        {
            UpdateAvailableSchedules2();
            UpdateCanCompare();
        }

        /// <summary>
        /// 当选中的排班表2变更时
        /// </summary>
        partial void OnSelectedSchedule2Changed(HistoryScheduleDto? value)
        {
            UpdateCanCompare();
        }

        /// <summary>
        /// 当差异类型筛选变更时
        /// </summary>
        partial void OnTypeFilterChanged(DiffType? value)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 当哨位筛选变更时
        /// </summary>
        partial void OnPositionFilterChanged(int? value)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 当人员筛选变更时
        /// </summary>
        partial void OnPersonnelFilterChanged(int? value)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 当预选排班表ID变更时
        /// </summary>
        partial void OnPreselectedScheduleIdChanged(int? value)
        {
            if (value.HasValue && Schedules.Any())
            {
                ApplyPreselection();
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载排班表列表
        /// </summary>
        public async Task LoadAsync()
        {
            await ExecuteAsync(async () =>
            {
                var result = await _historyService.GetHistorySchedulesAsync(new HistoryQueryOptions());
                
                Schedules.Clear();
                foreach (var item in result.OrderByDescending(i => i.ConfirmTime))
                {
                    Schedules.Add(item);
                }

                // 初始化第二个选择器的可选列表
                UpdateAvailableSchedules2();

                // 如果有预选ID，应用预选
                if (PreselectedScheduleId.HasValue)
                {
                    ApplyPreselection();
                }

                IsLoaded = true;
                IsEmpty = !Schedules.Any();
            }, "正在加载排班表列表...");
        }

        /// <summary>
        /// 设置预选排班表（从历史列表导航时调用）
        /// </summary>
        /// <param name="scheduleId">预选的排班表ID</param>
        public void SetPreselectedSchedule(int scheduleId)
        {
            PreselectedScheduleId = scheduleId;
        }

        #endregion

        #region 命令

        /// <summary>
        /// 执行对比命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCompare))]
        private async Task CompareAsync()
        {
            if (SelectedSchedule1 == null || SelectedSchedule2 == null)
                return;

            IsComparing = true;

            try
            {
                await ExecuteAsync(async () =>
                {
                    ComparisonResult = await _comparisonService.CompareSchedulesAsync(
                        SelectedSchedule1.Id,
                        SelectedSchedule2.Id);

                    // 构建网格对比数据
                    GridData = _comparisonService.BuildGridData(ComparisonResult);

                    // 更新可筛选的哨位和人员列表
                    UpdateFilterableLists();

                    // 应用筛选
                    ApplyFilters();

                    HasCompared = true;
                }, "正在计算差异...");
            }
            finally
            {
                IsComparing = false;
            }
        }

        /// <summary>
        /// 切换视图模式命令
        /// </summary>
        [RelayCommand]
        private void SwitchView(CompareViewMode mode)
        {
            ViewMode = mode;
        }

        /// <summary>
        /// 应用筛选命令
        /// </summary>
        [RelayCommand]
        private void ApplyFilter()
        {
            ApplyFilters();
        }

        /// <summary>
        /// 清除筛选命令
        /// </summary>
        [RelayCommand]
        private void ClearFilter()
        {
            TypeFilter = null;
            PositionFilter = null;
            PersonnelFilter = null;
            // ApplyFilters 会在属性变更时自动调用
        }

        /// <summary>
        /// 交换两个排班表命令
        /// </summary>
        [RelayCommand]
        private void SwapSchedules()
        {
            var temp = SelectedSchedule1;
            SelectedSchedule1 = SelectedSchedule2;
            SelectedSchedule2 = temp;
        }

        /// <summary>
        /// 导出为 Excel 命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasCompared))]
        private async Task ExportExcelAsync()
        {
            if (ComparisonResult == null)
                return;

            try
            {
                var bytes = await _comparisonService.ExportToExcelAsync(ComparisonResult);

                // 创建文件保存对话框
                var savePicker = new FileSavePicker();
                InitializeWithWindow.Initialize(savePicker, App.MainWindowHandle);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Excel 文件", new List<string>() { ".xlsx" });
                savePicker.SuggestedFileName = $"排班对比_{SelectedSchedule1?.Name}_{SelectedSchedule2?.Name}_{DateTime.Now:yyyyMMdd}";

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteBytesAsync(file, bytes);
                    await _dialogService.ShowSuccessAsync($"导出成功，文件已保存到: {file.Path}");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("导出失败", ex);
            }
        }

        /// <summary>
        /// 导出为 JSON 命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasCompared))]
        private async Task ExportJsonAsync()
        {
            if (ComparisonResult == null)
                return;

            try
            {
                var json = _comparisonService.ExportToJson(ComparisonResult);

                // 创建文件保存对话框
                var savePicker = new FileSavePicker();
                InitializeWithWindow.Initialize(savePicker, App.MainWindowHandle);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("JSON 文件", new List<string>() { ".json" });
                savePicker.SuggestedFileName = $"排班对比_{SelectedSchedule1?.Name}_{SelectedSchedule2?.Name}_{DateTime.Now:yyyyMMdd}";

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteTextAsync(file, json);
                    await _dialogService.ShowSuccessAsync($"导出成功，文件已保存到: {file.Path}");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("导出失败", ex);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新第二个选择器的可选列表（排除已选的第一个）
        /// </summary>
        private void UpdateAvailableSchedules2()
        {
            AvailableSchedules2.Clear();

            foreach (var schedule in Schedules)
            {
                // 排除已选的第一个排班表
                if (SelectedSchedule1 == null || schedule.Id != SelectedSchedule1.Id)
                {
                    AvailableSchedules2.Add(schedule);
                }
            }

            // 如果当前选中的第二个排班表被排除了，清除选择
            if (SelectedSchedule2 != null && 
                SelectedSchedule1 != null && 
                SelectedSchedule2.Id == SelectedSchedule1.Id)
            {
                SelectedSchedule2 = null;
            }
        }

        /// <summary>
        /// 更新是否可以执行对比
        /// </summary>
        private void UpdateCanCompare()
        {
            CanCompare = SelectedSchedule1 != null && 
                         SelectedSchedule2 != null && 
                         SelectedSchedule1.Id != SelectedSchedule2.Id;
            
            CompareCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 应用预选排班表
        /// </summary>
        private void ApplyPreselection()
        {
            if (!PreselectedScheduleId.HasValue)
                return;

            var preselected = Schedules.FirstOrDefault(s => s.Id == PreselectedScheduleId.Value);
            if (preselected != null)
            {
                SelectedSchedule1 = preselected;
                ShouldFocusSchedule2Selector = true;
            }
        }

        /// <summary>
        /// 更新可筛选的哨位和人员列表
        /// </summary>
        private void UpdateFilterableLists()
        {
            FilterablePositions.Clear();
            FilterablePersonnel.Clear();

            if (ComparisonResult == null)
                return;

            // 合并两个排班表的哨位
            var positions = new Dictionary<int, PositionDto>();
            foreach (var p in ComparisonResult.Schedule1.Positions)
            {
                positions[p.Id] = p;
            }
            foreach (var p in ComparisonResult.Schedule2.Positions)
            {
                if (!positions.ContainsKey(p.Id))
                {
                    positions[p.Id] = p;
                }
            }
            foreach (var p in positions.Values.OrderBy(p => p.Name))
            {
                FilterablePositions.Add(p);
            }

            // 合并两个排班表的人员
            var personnel = new Dictionary<int, PersonnelDto>();
            foreach (var p in ComparisonResult.Schedule1.Personnel)
            {
                personnel[p.Id] = p;
            }
            foreach (var p in ComparisonResult.Schedule2.Personnel)
            {
                if (!personnel.ContainsKey(p.Id))
                {
                    personnel[p.Id] = p;
                }
            }
            foreach (var p in personnel.Values.OrderBy(p => p.Name))
            {
                FilterablePersonnel.Add(p);
            }
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private void ApplyFilters()
        {
            FilteredDifferences.Clear();

            if (ComparisonResult == null || ComparisonResult.Differences == null)
                return;

            var filtered = _comparisonService.FilterDifferences(
                ComparisonResult.Differences,
                TypeFilter,
                PositionFilter,
                PersonnelFilter);

            foreach (var diff in filtered)
            {
                FilteredDifferences.Add(diff);
            }
        }

        #endregion

        #region 辅助属性

        /// <summary>
        /// 获取差异类型的显示文本
        /// </summary>
        public static string GetDiffTypeDisplayText(DiffType? type)
        {
            return type switch
            {
                null => "全部",
                DiffType.Added => "新增",
                DiffType.Removed => "删除",
                DiffType.Changed => "变更",
                _ => type.ToString() ?? "未知"
            };
        }

        /// <summary>
        /// 获取视图模式的显示文本
        /// </summary>
        public static string GetViewModeDisplayText(CompareViewMode mode)
        {
            return mode switch
            {
                CompareViewMode.Grid => "网格对比",
                CompareViewMode.DiffList => "差异列表",
                CompareViewMode.Statistics => "统计对比",
                _ => mode.ToString()
            };
        }

        /// <summary>
        /// 差异总数
        /// </summary>
        public int TotalDifferencesCount => ComparisonResult?.Differences?.Count ?? 0;

        /// <summary>
        /// 筛选后的差异数
        /// </summary>
        public int FilteredDifferencesCount => FilteredDifferences.Count;

        /// <summary>
        /// 新增班次数
        /// </summary>
        public int AddedCount => ComparisonResult?.Statistics?.AddedCount ?? 0;

        /// <summary>
        /// 删除班次数
        /// </summary>
        public int RemovedCount => ComparisonResult?.Statistics?.RemovedCount ?? 0;

        /// <summary>
        /// 变更班次数
        /// </summary>
        public int ChangedCount => ComparisonResult?.Statistics?.ChangedCount ?? 0;

        #endregion
    }
}
