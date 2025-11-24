using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using AutoScheduling3.Models;
using AutoScheduling3.ViewModels.Base;

namespace AutoScheduling3.ViewModels.Scheduling
{

    /// <summary>
    /// 排班结果页面 ViewModel，负责排班查看、确认、导出等操作
    /// </summary>
    public partial class ScheduleResultViewModel : ViewModelBase
    {
        #region 依赖注入

        private readonly ISchedulingService _schedulingService;
        private readonly DialogService _dialogService;
        private readonly NavigationService _navigationService;
        private readonly IPersonnelService _personnelService;
        private readonly IPositionService _positionService;
        private readonly IScheduleGridExporter _gridExporter;
        private readonly PersonnelSearchHelper _searchHelper = new();

        #endregion

        #region 基础属性

        private ScheduleDto _schedule;
        public ScheduleDto Schedule
        {
            get => _schedule;
            set
            {
                if (SetProperty(ref _schedule, value))
                {
                    OnPropertyChanged(nameof(GridData));
                }
            }
        }

        /// <summary>
        /// 表格数据，从 Schedule 转换而来
        /// </summary>
        public ScheduleGridData? GridData
        {
            get
            {
                if (Schedule == null) return null;
                return ConvertScheduleToGridData(Schedule);
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isConfirming;
        public bool IsConfirming
        {
            get => _isConfirming;
            set => SetProperty(ref _isConfirming, value);
        }

        private ViewMode _currentViewMode = ViewMode.Grid;
        public ViewMode CurrentViewMode
        {
            get => _currentViewMode;
            set
            {
                if (SetProperty(ref _currentViewMode, value))
                {
                    _ = OnViewModeChangedAsync(value);
                }
            }
        }

        private bool _isConflictPaneOpen = true;
        public bool IsConflictPaneOpen
        {
            get => _isConflictPaneOpen;
            set => SetProperty(ref _isConflictPaneOpen, value);
        }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        #endregion

        #region Position View 数据

        private ObservableCollection<PositionScheduleData> _positionSchedules = new();
        public ObservableCollection<PositionScheduleData> PositionSchedules
        {
            get => _positionSchedules;
            set => SetProperty(ref _positionSchedules, value);
        }

        private PositionScheduleData? _selectedPositionSchedule;
        public PositionScheduleData? SelectedPositionSchedule
        {
            get => _selectedPositionSchedule;
            set => SetProperty(ref _selectedPositionSchedule, value);
        }

        private int _currentWeekIndex;
        public int CurrentWeekIndex
        {
            get => _currentWeekIndex;
            set => SetProperty(ref _currentWeekIndex, value);
        }

        #endregion

        #region Personnel View 数据

        private ObservableCollection<PersonnelScheduleData> _personnelSchedules = new();
        public ObservableCollection<PersonnelScheduleData> PersonnelSchedules
        {
            get => _personnelSchedules;
            set => SetProperty(ref _personnelSchedules, value);
        }

        private PersonnelScheduleData? _selectedPersonnelSchedule;
        public PersonnelScheduleData? SelectedPersonnelSchedule
        {
            get => _selectedPersonnelSchedule;
            set => SetProperty(ref _selectedPersonnelSchedule, value);
        }

        private ObservableCollection<PersonnelScheduleData> _personnelSelectorSuggestions = new();
        public ObservableCollection<PersonnelScheduleData> PersonnelSelectorSuggestions
        {
            get => _personnelSelectorSuggestions;
            set => SetProperty(ref _personnelSelectorSuggestions, value);
        }

        private string _personnelSelectorSearchText = string.Empty;
        public string PersonnelSelectorSearchText
        {
            get => _personnelSelectorSearchText;
            set => SetProperty(ref _personnelSelectorSearchText, value);
        }

        #endregion

        #region List View 数据

        private ObservableCollection<ShiftListItem> _shiftList = new();
        public ObservableCollection<ShiftListItem> ShiftList
        {
            get => _shiftList;
            set => SetProperty(ref _shiftList, value);
        }

        private string _listSortBy = "Date";
        public string ListSortBy
        {
            get => _listSortBy;
            set => SetProperty(ref _listSortBy, value);
        }

        private string _listGroupBy = "None";
        public string ListGroupBy
        {
            get => _listGroupBy;
            set => SetProperty(ref _listGroupBy, value);
        }

        #endregion

        #region 统计数据

        private ObservableCollection<PersonnelWorkload> _personnelWorkloads = new();
        public ObservableCollection<PersonnelWorkload> PersonnelWorkloads
        {
            get => _personnelWorkloads;
            set => SetProperty(ref _personnelWorkloads, value);
        }

        private ObservableCollection<PositionCoverage> _positionCoverages = new();
        public ObservableCollection<PositionCoverage> PositionCoverages
        {
            get => _positionCoverages;
            set => SetProperty(ref _positionCoverages, value);
        }

        private SchedulingStatistics? _statistics;
        public SchedulingStatistics? Statistics
        {
            get => _statistics;
            set => SetProperty(ref _statistics, value);
        }

        #endregion

        #region 冲突数据

        private ObservableCollection<ConflictDto> _conflicts = new();
        public ObservableCollection<ConflictDto> Conflicts
        {
            get => _conflicts;
            set => SetProperty(ref _conflicts, value);
        }

        private ObservableCollection<ConflictInfo> _conflictInfos = new();
        public ObservableCollection<ConflictInfo> ConflictInfos
        {
            get => _conflictInfos;
            set => SetProperty(ref _conflictInfos, value);
        }

        #endregion

        #region 筛选条件

        private DateTime _filterStartDate;
        public DateTime FilterStartDate
        {
            get => _filterStartDate;
            set => SetProperty(ref _filterStartDate, value);
        }

        private DateTime _filterEndDate;
        public DateTime FilterEndDate
        {
            get => _filterEndDate;
            set => SetProperty(ref _filterEndDate, value);
        }

        private ObservableCollection<int> _selectedPositionIds = new();
        public ObservableCollection<int> SelectedPositionIds
        {
            get => _selectedPositionIds;
            set => SetProperty(ref _selectedPositionIds, value);
        }

        private string _personnelSearchText = string.Empty;
        public string PersonnelSearchText
        {
            get => _personnelSearchText;
            set => SetProperty(ref _personnelSearchText, value);
        }

        private ObservableCollection<PersonnelDto> _allPersonnel = new();
        public ObservableCollection<PersonnelDto> AllPersonnel
        {
            get => _allPersonnel;
            set => SetProperty(ref _allPersonnel, value);
        }

        private ObservableCollection<PersonnelDto> _personnelSuggestions = new();
        public ObservableCollection<PersonnelDto> PersonnelSuggestions
        {
            get => _personnelSuggestions;
            set => SetProperty(ref _personnelSuggestions, value);
        }

        private PersonnelDto? _selectedPersonnel;
        public PersonnelDto? SelectedPersonnel
        {
            get => _selectedPersonnel;
            set
            {
                if (SetProperty(ref _selectedPersonnel, value))
                {
                    if (value != null)
                    {
                        PersonnelSearchText = value.Name;
                    }
                }
            }
        }

        #endregion

        #region 命令

        public IAsyncRelayCommand<int> LoadScheduleCommand { get; }
        public IAsyncRelayCommand ConfirmCommand { get; }
        public IRelayCommand BackCommand { get; }
        public IAsyncRelayCommand ExportExcelCommand { get; }
        public IRelayCommand RescheduleCommand { get; }
        public IRelayCommand<ViewMode> ChangeViewModeCommand { get; }

        // 新增命令
        public IAsyncRelayCommand ApplyFiltersCommand { get; }
        public IAsyncRelayCommand ResetFiltersCommand { get; }
        public IAsyncRelayCommand<PositionScheduleData> SelectPositionCommand { get; }
        public IAsyncRelayCommand<PersonnelScheduleData> SelectPersonnelCommand { get; }
        public IRelayCommand<int> ChangeWeekCommand { get; }
        public IAsyncRelayCommand<ShiftListItem> ViewShiftDetailsCommand { get; }
        public IAsyncRelayCommand<ShiftListItem> EditShiftCommand { get; }
        public IAsyncRelayCommand SaveChangesCommand { get; }
        public IRelayCommand DiscardChangesCommand { get; }
        public IAsyncRelayCommand<ConflictInfo> LocateConflictCommand { get; }
        public IAsyncRelayCommand<ConflictInfo> IgnoreConflictCommand { get; }
        public IAsyncRelayCommand<ConflictInfo> FixConflictCommand { get; }
        public IAsyncRelayCommand ToggleFullScreenCommand { get; }
        public IAsyncRelayCommand CompareSchedulesCommand { get; }

        #endregion

        #region 缓存

        private Dictionary<int, string> _personnelNameCache = new();
        private Dictionary<int, string> _positionNameCache = new();

        #endregion


        public ScheduleResultViewModel(
            ISchedulingService schedulingService,
            DialogService dialogService,
            NavigationService navigationService,
            IPersonnelService personnelService,
            IPositionService positionService,
            IScheduleGridExporter gridExporter,
            IConflictDetectionService? conflictDetectionService = null,
            IConflictReportService? conflictReportService = null,
            IConflictResolutionService? conflictResolutionService = null)
        {
            _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _personnelService = personnelService ?? throw new ArgumentNullException(nameof(personnelService));
            _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
            _gridExporter = gridExporter ?? throw new ArgumentNullException(nameof(gridExporter));
            
            // 冲突管理服务（可选）
            _conflictDetectionService = conflictDetectionService;
            _conflictReportService = conflictReportService;
            _conflictResolutionService = conflictResolutionService;

            // 初始化命令
            LoadScheduleCommand = new AsyncRelayCommand<int>(LoadScheduleAsync);
            ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, CanConfirm);
            BackCommand = new RelayCommand(() => _navigationService.GoBack());
            ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync, CanExport);
            RescheduleCommand = new RelayCommand(Reschedule, CanReschedule);
            ChangeViewModeCommand = new RelayCommand<ViewMode>(viewMode => CurrentViewMode = viewMode);

            // 初始化新增命令
            ApplyFiltersCommand = new AsyncRelayCommand(ApplyFiltersAsync);
            ResetFiltersCommand = new AsyncRelayCommand(ResetFiltersAsync);
            SelectPositionCommand = new AsyncRelayCommand<PositionScheduleData>(SelectPositionAsync);
            SelectPersonnelCommand = new AsyncRelayCommand<PersonnelScheduleData>(SelectPersonnelAsync);
            ChangeWeekCommand = new RelayCommand<int>(ChangeWeek);
            ViewShiftDetailsCommand = new AsyncRelayCommand<ShiftListItem>(ViewShiftDetailsAsync);
            EditShiftCommand = new AsyncRelayCommand<ShiftListItem>(EditShiftAsync);
            SaveChangesCommand = new AsyncRelayCommand(SaveChangesAsync, CanSaveChanges);
            DiscardChangesCommand = new RelayCommand(DiscardChanges, CanDiscardChanges);
            LocateConflictCommand = new AsyncRelayCommand<ConflictInfo>(LocateConflictAsync);
            IgnoreConflictCommand = new AsyncRelayCommand<ConflictInfo>(IgnoreConflictAsync);
            FixConflictCommand = new AsyncRelayCommand<ConflictInfo>(FixConflictAsync);
            ToggleFullScreenCommand = new AsyncRelayCommand(ToggleFullScreenAsync);
            CompareSchedulesCommand = new AsyncRelayCommand(CompareSchedulesAsync);
            
            // 初始化冲突相关命令
            InitializeConflictCommands();

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Schedule))
                {
                    ConfirmCommand.NotifyCanExecuteChanged();
                    ExportExcelCommand.NotifyCanExecuteChanged();
                    RescheduleCommand.NotifyCanExecuteChanged();
                    if (Schedule?.Conflicts != null)
                    {
                        Conflicts = new ObservableCollection<ConflictDto>(Schedule.Conflicts);
                    }
                    // 自动检测冲突
                    _ = DetectConflictsAsync();
                }
                if (e.PropertyName is nameof(IsLoading) or nameof(IsConfirming))
                {
                    ConfirmCommand.NotifyCanExecuteChanged();
                }
                if (e.PropertyName == nameof(HasUnsavedChanges))
                {
                    SaveChangesCommand.NotifyCanExecuteChanged();
                    DiscardChangesCommand.NotifyCanExecuteChanged();
                }
            };
        }

        private bool CanConfirm() => Schedule != null && Schedule.ConfirmedAt == null && !IsLoading && !IsConfirming;
        private bool CanExport() => Schedule != null;
        private bool CanReschedule() => Schedule != null;

        private async Task LoadScheduleAsync(int id)
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                var dto = await _schedulingService.GetScheduleByIdAsync(id);
                if (dto == null)
                {
                    await _dialogService.ShowWarningAsync("δ�ҵ��Ű�����");
                    _navigationService.GoBack();
                    return;
                }
                Schedule = dto;

                // 加载所有人员数据用于筛选
                await LoadAllPersonnelAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("�����Ű�����ʧ��", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载所有人员数据
        /// </summary>
        private async Task LoadAllPersonnelAsync()
        {
            try
            {
                var personnel = await _personnelService.GetAllAsync();
                AllPersonnel = new ObservableCollection<PersonnelDto>(personnel);
                PersonnelSuggestions = new ObservableCollection<PersonnelDto>(personnel);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("加载人员列表失败", ex);
            }
        }

        /// <summary>
        /// 更新人员搜索建议（使用模糊匹配）
        /// </summary>
        public async void UpdatePersonnelSuggestions(string searchText)
        {
            try
            {
                // 使用PersonnelSearchHelper进行模糊搜索
                var searchResults = await _searchHelper.SearchAsync(searchText, AllPersonnel);
                PersonnelSuggestions = new ObservableCollection<PersonnelDto>(searchResults);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ScheduleResultViewModel: 搜索失败: {ex.Message}");
                
                // 降级到简单搜索
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    PersonnelSuggestions = new ObservableCollection<PersonnelDto>(AllPersonnel);
                }
                else
                {
                    var filtered = AllPersonnel
                        .Where(p => p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    PersonnelSuggestions = new ObservableCollection<PersonnelDto>(filtered);
                }
            }
        }

        /// <summary>
        /// 搜索人员排班数据（支持模糊匹配和拼音首字母）
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <returns>匹配的人员排班数据列表</returns>
        public async Task<List<PersonnelScheduleData>> SearchPersonnelAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return PersonnelSchedules.ToList();
            }

            try
            {
                // 转换为PersonnelDto进行搜索
                var personnelDtos = PersonnelSchedules.Select(p => new PersonnelDto
                {
                    Id = p.PersonnelId,
                    Name = p.PersonnelName
                }).ToList();

                var matchedDtos = await _searchHelper.SearchAsync(searchText, personnelDtos);

                // 转换回PersonnelScheduleData
                var matchedIds = matchedDtos.Select(d => d.Id).ToHashSet();
                var results = PersonnelSchedules
                    .Where(p => matchedIds.Contains(p.PersonnelId))
                    .ToList();

                return results;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"人员搜索失败: {ex.Message}");
                
                // 降级到简单匹配
                return PersonnelSchedules
                    .Where(p => p.PersonnelName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        /// <summary>
        /// 更新人员选择器的建议列表（同步版本，用于 AutoSuggestBox）
        /// </summary>
        public void UpdatePersonnelSelectorSuggestions(string searchText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    PersonnelSelectorSuggestions = new ObservableCollection<PersonnelScheduleData>(PersonnelSchedules);
                    return;
                }

                // 转换为PersonnelDto进行搜索
                var personnelDtos = PersonnelSchedules.Select(p => new PersonnelDto
                {
                    Id = p.PersonnelId,
                    Name = p.PersonnelName
                }).ToList();

                // 使用立即搜索（不防抖）
                var matchedDtos = _searchHelper.SearchImmediate(searchText, personnelDtos);

                // 转换回PersonnelScheduleData
                var matchedIds = matchedDtos.Select(d => d.Id).ToHashSet();
                var results = PersonnelSchedules
                    .Where(p => matchedIds.Contains(p.PersonnelId))
                    .ToList();

                PersonnelSelectorSuggestions = new ObservableCollection<PersonnelScheduleData>(results);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新人员选择器建议失败: {ex.Message}");
                
                // 降级到简单匹配
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    PersonnelSelectorSuggestions = new ObservableCollection<PersonnelScheduleData>(PersonnelSchedules);
                }
                else
                {
                    var results = PersonnelSchedules
                        .Where(p => p.PersonnelName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    PersonnelSelectorSuggestions = new ObservableCollection<PersonnelScheduleData>(results);
                }
            }
        }

        private async Task ConfirmAsync()
        {
            if (!CanConfirm()) return;
            var ok = await _dialogService.ShowConfirmAsync("ȷ���Ű�", "ȷ�Ϻ��޷��޸ģ��Ƿ����?", "ȷ��", "ȡ��");
            if (!ok) return;
            IsConfirming = true;
            try
            {
                await _schedulingService.ConfirmScheduleAsync(Schedule.Id);
                var dto = await _schedulingService.GetScheduleByIdAsync(Schedule.Id);
                Schedule = dto;
                await _dialogService.ShowSuccessAsync("�Ű���ȷ��");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("ȷ���Ű�ʧ��", ex);
            }
            finally
            {
                IsConfirming = false;
            }
        }

        private async Task ExportExcelAsync()
        {
            if (!CanExport()) return;

            try
            {
                // 显示导出格式选择对话框
                var dialog = new Views.Scheduling.ExportFormatDialog();
                dialog.XamlRoot = App.MainWindow.Content.XamlRoot;
                
                var result = await dialog.ShowAsync();
                if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                    return;

                var format = dialog.SelectedFormat.ToString().ToLower(); // "excel", "csv", "pdf"
                var options = new DTOs.ExportOptions
                {
                    IncludeHeader = true,
                    IncludeEmptyCells = dialog.Options.IncludeEmptyCells,
                    HighlightConflicts = dialog.Options.HighlightConflicts,
                    HighlightManualAssignments = true,
                    Title = Schedule.Title
                };

                byte[] bytes;
                string fileExtension;
                string fileTypeDescription;

                // 根据当前视图模式选择导出方法
                switch (CurrentViewMode)
                {
                    case ViewMode.Grid:
                        bytes = await _gridExporter.ExportAsync(GridData, format, options);
                        break;

                    case ViewMode.ByPosition:
                        if (SelectedPositionSchedule == null)
                        {
                            await _dialogService.ShowWarningAsync("请先选择一个哨位");
                            return;
                        }
                        bytes = await _gridExporter.ExportPositionScheduleAsync(SelectedPositionSchedule, format, options);
                        break;

                    case ViewMode.ByPersonnel:
                        if (SelectedPersonnelSchedule == null)
                        {
                            await _dialogService.ShowWarningAsync("请先选择一个人员");
                            return;
                        }
                        bytes = await _gridExporter.ExportPersonnelScheduleAsync(SelectedPersonnelSchedule, format, options);
                        break;

                    case ViewMode.List:
                        bytes = await _gridExporter.ExportShiftListAsync(ShiftList, format, options);
                        break;

                    default:
                        await _dialogService.ShowWarningAsync("当前视图模式不支持导出");
                        return;
                }

                // 设置文件扩展名和类型描述
                switch (format.ToLower())
                {
                    case "excel":
                        fileExtension = ".xlsx";
                        fileTypeDescription = "Excel 工作簿";
                        break;
                    case "csv":
                        fileExtension = ".csv";
                        fileTypeDescription = "CSV 文件";
                        break;
                    case "pdf":
                        fileExtension = ".pdf";
                        fileTypeDescription = "PDF 文档";
                        break;
                    default:
                        fileExtension = ".dat";
                        fileTypeDescription = "数据文件";
                        break;
                }

                // 保存文件
                var savePicker = new FileSavePicker();
                InitializeWithWindow.Initialize(savePicker, App.MainWindowHandle);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add(fileTypeDescription, new List<string>() { fileExtension });
                savePicker.SuggestedFileName = $"{Schedule.Title}_{DateTime.Now:yyyyMMdd}";

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteBytesAsync(file, bytes);
                    await _dialogService.ShowSuccessAsync($"导出成功，文件已保存到: {file.Path}");
                }
            }
            catch (NotImplementedException ex)
            {
                await _dialogService.ShowWarningAsync("功能开发中", ex.Message);
            }
            catch (NotSupportedException ex)
            {
                await _dialogService.ShowWarningAsync("不支持的操作", ex.Message);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("导出失败", ex);
            }
        }

        private void Reschedule()
        {
            if (!CanReschedule()) return;

            var request = new SchedulingRequestDto
            {
                Title = $"{Schedule.Title} (����)",
                StartDate = Schedule.StartDate,
                EndDate = Schedule.EndDate,
                PersonnelIds = Schedule.PersonnelIds.ToList(),
                PositionIds = Schedule.PositionIds.ToList(),
                // Note: Constraint information is not part of ScheduleDto, so it cannot be carried over.
                // The user will need to re-configure constraints on the creation page.
            };

            _navigationService.NavigateTo("CreateScheduling", request);
        }

        /// <summary>
        /// 将 ScheduleDto 转换为 ScheduleGridData
        /// </summary>
        private ScheduleGridData ConvertScheduleToGridData(ScheduleDto schedule)
        {
            var gridData = new ScheduleGridData
            {
                StartDate = schedule.StartDate,
                EndDate = schedule.EndDate,
                PositionIds = schedule.PositionIds,
                TotalDays = (schedule.EndDate - schedule.StartDate).Days + 1,
                TotalPeriods = 12 // 每天12个时段
            };

            // 创建列（哨位）
            var positionGroups = schedule.Shifts
                .GroupBy(s => s.PositionId)
                .OrderBy(g => g.Key)
                .ToList();

            int colIndex = 0;
            foreach (var posGroup in positionGroups)
            {
                var firstShift = posGroup.First();
                gridData.Columns.Add(new ScheduleGridColumn
                {
                    ColumnIndex = colIndex,
                    PositionId = firstShift.PositionId,
                    PositionName = firstShift.PositionName
                });
                colIndex++;
            }

            // 创建行（日期+时段）
            int rowIndex = 0;
            for (var date = schedule.StartDate.Date; date <= schedule.EndDate.Date; date = date.AddDays(1))
            {
                for (int period = 0; period < 12; period++)
                {
                    var periodStart = date.AddHours(period * 2);
                    var periodEnd = periodStart.AddHours(2);
                    
                    gridData.Rows.Add(new ScheduleGridRow
                    {
                        RowIndex = rowIndex,
                        Date = date,
                        PeriodIndex = period,
                        DisplayText = $"{date:MM-dd} {periodStart:HH:mm}-{periodEnd:HH:mm}"
                    });
                    rowIndex++;
                }
            }

            // 创建单元格（班次）
            foreach (var shift in schedule.Shifts)
            {
                var shiftDate = shift.StartTime.Date;
                var periodIndex = shift.PeriodIndex;
                
                // 找到对应的行索引
                var row = gridData.Rows.FirstOrDefault(r => 
                    r.Date.Date == shiftDate && r.PeriodIndex == periodIndex);
                
                // 找到对应的列索引
                var col = gridData.Columns.FirstOrDefault(c => c.PositionId == shift.PositionId);
                
                if (row != null && col != null)
                {
                    var cellKey = $"{row.RowIndex}_{col.ColumnIndex}";
                    gridData.Cells[cellKey] = new ScheduleGridCell
                    {
                        RowIndex = row.RowIndex,
                        ColumnIndex = col.ColumnIndex,
                        PersonnelId = shift.PersonnelId,
                        PersonnelName = shift.PersonnelName,
                        IsAssigned = true
                    };
                }
            }

            return gridData;
        }

        #region 辅助方法

        /// <summary>
        /// 获取人员姓名（带缓存）
        /// </summary>
        private async Task<string> GetPersonnelNameAsync(int personnelId)
        {
            if (_personnelNameCache.TryGetValue(personnelId, out var name))
                return name;

            var personnel = await _personnelService.GetByIdAsync(personnelId);
            name = personnel?.Name ?? $"人员{personnelId}";
            _personnelNameCache[personnelId] = name;
            return name;
        }

        /// <summary>
        /// 获取哨位名称（带缓存）
        /// </summary>
        private async Task<string> GetPositionNameAsync(int positionId)
        {
            if (_positionNameCache.TryGetValue(positionId, out var name))
                return name;

            var position = await _positionService.GetByIdAsync(positionId);
            name = position?.Name ?? $"哨位{positionId}";
            _positionNameCache[positionId] = name;
            return name;
        }

        /// <summary>
        /// 获取时段描述
        /// </summary>
        private string GetTimeSlotDescription(int periodIndex)
        {
            var startHour = periodIndex * 2;
            var endHour = startHour + 2;
            return $"{startHour:D2}:00-{endHour:D2}:00";
        }

        /// <summary>
        /// 判断是否为夜哨
        /// </summary>
        private bool IsNightShift(int periodIndex)
        {
            // 夜哨时段：11, 0, 1, 2 (22:00-06:00)
            return periodIndex == 11 || periodIndex == 0 || periodIndex == 1 || periodIndex == 2;
        }

        /// <summary>
        /// 获取班次备注
        /// </summary>
        private string? GetShiftRemarks(ShiftDto shift)
        {
            var remarks = new List<string>();
            
            if (shift.IsManualAssignment)
                remarks.Add("手动指定");
            
            if (IsNightShift(shift.PeriodIndex))
                remarks.Add("夜哨");
            
            return remarks.Count > 0 ? string.Join(", ", remarks) : null;
        }

        #endregion

        #region 数据构建方法

        /// <summary>
        /// 构建 Position View 数据
        /// </summary>
        private async Task BuildPositionScheduleDataAsync()
        {
            if (Schedule == null) return;

            var positionSchedules = new List<PositionScheduleData>();

            // 遍历每个哨位
            foreach (var positionId in Schedule.PositionIds)
            {
                var positionSchedule = new PositionScheduleData
                {
                    PositionId = positionId,
                    PositionName = await GetPositionNameAsync(positionId),
                    Weeks = new List<WeekData>(),
                    CurrentWeekIndex = 0
                };

                // 计算周数
                var totalDays = (Schedule.EndDate - Schedule.StartDate).Days + 1;
                var totalWeeks = (int)Math.Ceiling(totalDays / 7.0);

                // 构建每周的数据
                for (int weekIndex = 0; weekIndex < totalWeeks; weekIndex++)
                {
                    var weekStartDate = Schedule.StartDate.AddDays(weekIndex * 7);
                    var weekEndDate = weekStartDate.AddDays(6);
                    if (weekEndDate > Schedule.EndDate)
                        weekEndDate = Schedule.EndDate;

                    var weekData = new WeekData
                    {
                        WeekNumber = weekIndex + 1,
                        StartDate = weekStartDate,
                        EndDate = weekEndDate,
                        Cells = new Dictionary<string, PositionScheduleCell>()
                    };

                    // 填充单元格数据
                    for (int periodIndex = 0; periodIndex < 12; periodIndex++)
                    {
                        for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
                        {
                            var date = weekStartDate.AddDays(dayOfWeek);
                            if (date > Schedule.EndDate)
                                continue;

                            var cellKey = $"{periodIndex}_{dayOfWeek}";

                            // 查找该时段的分配
                            var shift = Schedule.Shifts.FirstOrDefault(s =>
                                s.PositionId == positionId &&
                                s.StartTime.Date == date &&
                                s.PeriodIndex == periodIndex);

                            weekData.Cells[cellKey] = new PositionScheduleCell
                            {
                                PeriodIndex = periodIndex,
                                DayOfWeek = dayOfWeek,
                                Date = date,
                                PersonnelId = shift?.PersonnelId,
                                PersonnelName = shift != null ? shift.PersonnelName : null,
                                IsAssigned = shift != null,
                                IsManualAssignment = shift?.IsManualAssignment ?? false,
                                HasConflict = false // TODO: 需要检测冲突
                            };
                        }
                    }

                    positionSchedule.Weeks.Add(weekData);
                }

                positionSchedules.Add(positionSchedule);
            }

            PositionSchedules = new ObservableCollection<PositionScheduleData>(positionSchedules);
            if (PositionSchedules.Count > 0)
            {
                SelectedPositionSchedule = PositionSchedules[0];
            }
        }

        /// <summary>
        /// 构建 Personnel View 数据
        /// </summary>
        private async Task BuildPersonnelScheduleDataAsync()
        {
            if (Schedule == null) return;

            var personnelSchedules = new List<PersonnelScheduleData>();

            // 获取所有参与排班的人员ID
            var personnelIds = Schedule.Shifts.Select(s => s.PersonnelId).Distinct();

            // 遍历每个人员
            foreach (var personnelId in personnelIds)
            {
                var personnelSchedule = new PersonnelScheduleData
                {
                    PersonnelId = personnelId,
                    PersonnelName = await GetPersonnelNameAsync(personnelId),
                    Shifts = new List<PersonnelShift>(),
                    CalendarData = new Dictionary<DateTime, List<PersonnelShift>>()
                };

                // 获取该人员的所有班次
                var personnelShifts = Schedule.Shifts
                    .Where(s => s.PersonnelId == personnelId)
                    .OrderBy(s => s.StartTime)
                    .ThenBy(s => s.PeriodIndex);

                foreach (var shift in personnelShifts)
                {
                    var personnelShift = new PersonnelShift
                    {
                        Date = shift.StartTime.Date,
                        PeriodIndex = shift.PeriodIndex,
                        TimeSlot = GetTimeSlotDescription(shift.PeriodIndex),
                        PositionId = shift.PositionId,
                        PositionName = shift.PositionName,
                        IsManualAssignment = shift.IsManualAssignment,
                        IsNightShift = IsNightShift(shift.PeriodIndex),
                        Remarks = GetShiftRemarks(shift)
                    };

                    personnelSchedule.Shifts.Add(personnelShift);

                    // 添加到日历数据
                    if (!personnelSchedule.CalendarData.ContainsKey(shift.StartTime.Date))
                    {
                        personnelSchedule.CalendarData[shift.StartTime.Date] = new List<PersonnelShift>();
                    }
                    personnelSchedule.CalendarData[shift.StartTime.Date].Add(personnelShift);
                }

                // 计算工作量
                personnelSchedule.Workload = new PersonnelWorkload
                {
                    PersonnelId = personnelId,
                    PersonnelName = personnelSchedule.PersonnelName,
                    TotalShifts = personnelSchedule.Shifts.Count,
                    DayShifts = personnelSchedule.Shifts.Count(s => !s.IsNightShift),
                    NightShifts = personnelSchedule.Shifts.Count(s => s.IsNightShift)
                };

                personnelSchedules.Add(personnelSchedule);
            }

            PersonnelSchedules = new ObservableCollection<PersonnelScheduleData>(personnelSchedules);
            
            // 初始化人员选择器建议列表
            PersonnelSelectorSuggestions = new ObservableCollection<PersonnelScheduleData>(personnelSchedules);
            
            if (PersonnelSchedules.Count > 0)
            {
                SelectedPersonnelSchedule = PersonnelSchedules[0];
            }
        }

        /// <summary>
        /// 构建 List View 数据
        /// </summary>
        private async Task BuildShiftListDataAsync()
        {
            if (Schedule == null) return;

            var shiftList = new List<ShiftListItem>();

            foreach (var shift in Schedule.Shifts)
            {
                var listItem = new ShiftListItem
                {
                    ShiftId = shift.Id,
                    Date = shift.StartTime.Date,
                    PeriodIndex = shift.PeriodIndex,
                    TimeSlot = GetTimeSlotDescription(shift.PeriodIndex),
                    DateTimeDescription = $"{shift.StartTime:yyyy-MM-dd} {GetTimeSlotDescription(shift.PeriodIndex)}",
                    PositionId = shift.PositionId,
                    PositionName = shift.PositionName,
                    PersonnelId = shift.PersonnelId,
                    PersonnelName = shift.PersonnelName,
                    IsManualAssignment = shift.IsManualAssignment,
                    HasConflict = false, // TODO: 需要检测冲突
                    ConflictMessage = null
                };

                shiftList.Add(listItem);
            }

            ShiftList = new ObservableCollection<ShiftListItem>(shiftList);
        }

        /// <summary>
        /// 构建人员工作量统计
        /// </summary>
        private async Task BuildPersonnelWorkloadsAsync()
        {
            if (Schedule == null) return;

            var workloads = new List<PersonnelWorkload>();
            var personnelIds = Schedule.Shifts.Select(s => s.PersonnelId).Distinct();

            foreach (var personnelId in personnelIds)
            {
                var personnelShifts = Schedule.Shifts.Where(s => s.PersonnelId == personnelId).ToList();
                
                workloads.Add(new PersonnelWorkload
                {
                    PersonnelId = personnelId,
                    PersonnelName = await GetPersonnelNameAsync(personnelId),
                    TotalShifts = personnelShifts.Count,
                    DayShifts = personnelShifts.Count(s => !IsNightShift(s.PeriodIndex)),
                    NightShifts = personnelShifts.Count(s => IsNightShift(s.PeriodIndex))
                });
            }

            PersonnelWorkloads = new ObservableCollection<PersonnelWorkload>(workloads);
        }

        /// <summary>
        /// 构建哨位覆盖率统计
        /// </summary>
        private async Task BuildPositionCoveragesAsync()
        {
            if (Schedule == null) return;

            var coverages = new List<PositionCoverage>();
            var totalDays = (Schedule.EndDate - Schedule.StartDate).Days + 1;
            var totalSlotsPerPosition = totalDays * 12; // 每天12个时段

            foreach (var positionId in Schedule.PositionIds)
            {
                var assignedSlots = Schedule.Shifts.Count(s => s.PositionId == positionId);
                
                coverages.Add(new PositionCoverage
                {
                    PositionId = positionId,
                    PositionName = await GetPositionNameAsync(positionId),
                    AssignedSlots = assignedSlots,
                    TotalSlots = totalSlotsPerPosition,
                    CoverageRate = (double)assignedSlots / totalSlotsPerPosition
                });
            }

            PositionCoverages = new ObservableCollection<PositionCoverage>(coverages);
        }

        /// <summary>
        /// 构建统计信息
        /// </summary>
        private async Task BuildStatisticsAsync()
        {
            if (Schedule == null) return;

            await BuildPersonnelWorkloadsAsync();
            await BuildPositionCoveragesAsync();

            Statistics = new SchedulingStatistics
            {
                TotalAssignments = Schedule.Shifts.Count,
                PersonnelWorkloads = PersonnelWorkloads.ToDictionary(w => w.PersonnelId),
                PositionCoverages = PositionCoverages.ToDictionary(c => c.PositionId),
                SoftScores = Schedule.SoftScores ?? new SoftConstraintScores()
            };
        }

        #endregion

        #region 视图模式切换

        /// <summary>
        /// 视图模式变化处理
        /// </summary>
        private async Task OnViewModeChangedAsync(ViewMode newMode)
        {
            if (Schedule == null) return;

            try
            {
                IsLoading = true;

                switch (newMode)
                {
                    case ViewMode.Grid:
                        // Grid View 数据已经通过 GridData 属性自动构建
                        break;

                    case ViewMode.ByPosition:
                        if (PositionSchedules.Count == 0)
                        {
                            await BuildPositionScheduleDataAsync();
                        }
                        break;

                    case ViewMode.ByPersonnel:
                        if (PersonnelSchedules.Count == 0)
                        {
                            await BuildPersonnelScheduleDataAsync();
                        }
                        break;

                    case ViewMode.List:
                        if (ShiftList.Count == 0)
                        {
                            await BuildShiftListDataAsync();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("视图切换失败", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 应用筛选
        /// </summary>
        private async Task ApplyFiltersAsync()
        {
            // TODO: 实现筛选逻辑
            await Task.CompletedTask;
        }

        /// <summary>
        /// 重置筛选
        /// </summary>
        private async Task ResetFiltersAsync()
        {
            FilterStartDate = Schedule?.StartDate ?? DateTime.Now;
            FilterEndDate = Schedule?.EndDate ?? DateTime.Now;
            SelectedPositionIds.Clear();
            PersonnelSearchText = string.Empty;
            
            await ApplyFiltersAsync();
        }

        /// <summary>
        /// 选择哨位
        /// </summary>
        private async Task SelectPositionAsync(PositionScheduleData position)
        {
            SelectedPositionSchedule = position;
            await Task.CompletedTask;
        }

        /// <summary>
        /// 选择人员
        /// </summary>
        private async Task SelectPersonnelAsync(PersonnelScheduleData personnel)
        {
            SelectedPersonnelSchedule = personnel;
            await Task.CompletedTask;
        }

        /// <summary>
        /// 切换周次
        /// </summary>
        private void ChangeWeek(int weekIndex)
        {
            CurrentWeekIndex = weekIndex;
        }

        /// <summary>
        /// 查看班次详情
        /// </summary>
        private async Task ViewShiftDetailsAsync(ShiftListItem shift)
        {
            // TODO: 打开班次详情对话框
            await Task.CompletedTask;
        }

        /// <summary>
        /// 编辑班次
        /// </summary>
        private async Task EditShiftAsync(ShiftListItem shift)
        {
            // TODO: 打开编辑对话框
            await Task.CompletedTask;
        }

        /// <summary>
        /// 保存更改
        /// </summary>
        private async Task SaveChangesAsync()
        {
            // TODO: 保存更改到数据库
            HasUnsavedChanges = false;
            await Task.CompletedTask;
        }

        private bool CanSaveChanges() => HasUnsavedChanges;

        /// <summary>
        /// 撤销更改
        /// </summary>
        private void DiscardChanges()
        {
            // TODO: 撤销所有未保存的更改
            HasUnsavedChanges = false;
        }

        private bool CanDiscardChanges() => HasUnsavedChanges;

        /// <summary>
        /// 定位冲突
        /// </summary>
        private async Task LocateConflictAsync(ConflictInfo conflict)
        {
            // TODO: 定位并高亮冲突单元格
            await Task.CompletedTask;
        }

        /// <summary>
        /// 忽略冲突
        /// </summary>
        private async Task IgnoreConflictAsync(ConflictInfo conflict)
        {
            // TODO: 标记冲突为已忽略
            await Task.CompletedTask;
        }

        /// <summary>
        /// 修复冲突
        /// </summary>
        private async Task FixConflictAsync(ConflictInfo conflict)
        {
            // TODO: 打开修复对话框
            await Task.CompletedTask;
        }

        /// <summary>
        /// 切换全屏
        /// </summary>
        private async Task ToggleFullScreenAsync()
        {
            try
            {
                // 准备全屏视图参数
                FullScreenViewParameter parameter;

                // 根据当前视图模式设置数据
                switch (CurrentViewMode)
                {
                    case ViewMode.Grid:
                        parameter = new FullScreenViewParameter
                        {
                            ViewMode = ViewMode.Grid,
                            GridData = GridData,
                            Title = $"{Schedule?.Title ?? "排班结果"} - 网格视图（全屏）"
                        };
                        break;

                    case ViewMode.ByPosition:
                        if (SelectedPositionSchedule == null)
                        {
                            await _dialogService.ShowWarningAsync("请先选择一个哨位");
                            return;
                        }
                        parameter = new FullScreenViewParameter
                        {
                            ViewMode = ViewMode.ByPosition,
                            PositionScheduleData = SelectedPositionSchedule,
                            Title = $"{SelectedPositionSchedule.PositionName} - 哨位视图（全屏）"
                        };
                        break;

                    default:
                        await _dialogService.ShowWarningAsync("当前视图模式不支持全屏");
                        return;
                }

                // 导航到全屏视图
                _navigationService.NavigateTo("ScheduleGridFullScreen", parameter);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("打开全屏视图失败", ex);
            }
        }

        /// <summary>
        /// 比较排班
        /// </summary>
        private async Task CompareSchedulesAsync()
        {
            // TODO: 打开排班比较视图
            await Task.CompletedTask;
        }

        #endregion
    }
}
