using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using System.Collections.Generic;
using AutoScheduling3.Models.Constraints; // Լģ
using CommunityToolkit.Mvvm.Input.Internals;
using System.Diagnostics.CodeAnalysis;

namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel : ObservableObject
    {
        private readonly ISchedulingService _schedulingService;
        private readonly IPersonnelService _personnelService;
        private readonly IPositionService _positionService;
        private readonly ITemplateService _templateService; // ģ
        private readonly ISchedulingDraftService? _draftService; // 草稿服务（可选）
        private readonly DialogService _dialogService;
        private readonly NavigationService _navigation_service; // ԭֶ
        private Microsoft.UI.Xaml.Controls.ContentDialog? _progressDialog; //ȶԻ

        [ObservableProperty]
        private int _currentStep = 1;

        [ObservableProperty]
        private string _scheduleTitle = string.Empty;

        [ObservableProperty]
        private DateTimeOffset _startDate = DateTimeOffset.Now;

        [ObservableProperty]
        private DateTimeOffset _endDate = DateTimeOffset.Now.AddDays(7);

        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _availablePersonnels = new();
        [ObservableProperty]
        private ObservableCollection<PositionDto> _availablePositions = new();

        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _selectedPersonnels = new();
        [ObservableProperty]
        private ObservableCollection<PositionDto> _selectedPositions = new();

        [ObservableProperty] private List<int> _enabledFixedRules = new();
        [ObservableProperty] private List<int> _enabledManualAssignments = new();

        [ObservableProperty] private bool _useActiveHolidayConfig = true;
        [ObservableProperty] private int? _selectedHolidayConfigId;

        //ԼДʹԱ UI)
        [ObservableProperty] private ObservableCollection<HolidayConfig> _holidayConfigs = new();
        [ObservableProperty] private ObservableCollection<FixedPositionRule> _fixedPositionRules = new();
        [ObservableProperty] private ObservableCollection<ManualAssignment> _manualAssignments = new();

        // 手动指定管理器
        private readonly ManualAssignmentManager _manualAssignmentManager;

        // 哨位人员管理器
        private readonly PositionPersonnelManager _positionPersonnelManager;

        // 缓存：人员ID到PersonnelDto的映射
        private readonly Dictionary<int, PersonnelDto> _personnelCache = new();
        
        // 缓存：哨位ID到PositionDto的映射
        private readonly Dictionary<int, PositionDto> _positionCache = new();

        // 所有手动指定（绑定到UI）
        public ObservableCollection<ManualAssignmentViewModel> AllManualAssignments 
            => _manualAssignmentManager.AllAssignments;

        // 手动添加的人员ID列表（不在任何哨位的可用人员列表中）
        [ObservableProperty]
        private ObservableCollection<int> _manuallyAddedPersonnelIds = new();

        // 手动添加的人员详细信息（用于UI显示）
        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _manuallyAddedPersonnelDetails = new();

        // 自动提取的人员数量
        [ObservableProperty]
        private int _autoExtractedPersonnelCount;

        // 手动添加的人员数量
        [ObservableProperty]
        private int _manuallyAddedPersonnelCount;

        // 是否正在添加人员到哨位
        [ObservableProperty]
        private bool _isAddingPersonnelToPosition;

        // 当前正在编辑的哨位
        [ObservableProperty]
        private PositionDto? _currentEditingPosition;

        // 可添加到当前哨位的人员列表
        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _availablePersonnelForPosition = new();

        // 选中的要添加到哨位的人员ID列表
        [ObservableProperty]
        private ObservableCollection<int> _selectedPersonnelIdsForPosition = new();

        // 手动添加参与人员相关属性
        [ObservableProperty]
        private bool _isManualAddingPersonnel;

        // 可手动添加的人员列表（不在任何哨位可用人员列表中的人员）
        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _availablePersonnelForManualAdd = new();

        // 选中的要手动添加的人员ID列表
        [ObservableProperty]
        private ObservableCollection<int> _selectedPersonnelIdsForManualAdd = new();

        // 哨位人员视图模型集合（用于步骤3的UI展示）
        [ObservableProperty]
        private ObservableCollection<PositionPersonnelViewModel> _positionPersonnelViewModels = new();

        // 表单相关属性
        [ObservableProperty]
        private bool _isCreatingManualAssignment;

        [ObservableProperty]
        private bool _isEditingManualAssignment;

        [ObservableProperty]
        private CreateManualAssignmentDto? _newManualAssignment;

        [ObservableProperty]
        private ManualAssignmentViewModel? _editingManualAssignment;

        [ObservableProperty]
        private UpdateManualAssignmentDto? _editingManualAssignmentDto;

        // Wrapper properties for form binding (to handle null cases)
        public DateTimeOffset? NewManualAssignmentDate
        {
            get => NewManualAssignment != null ? new DateTimeOffset(NewManualAssignment.Date) : null;
            set
            {
                if (NewManualAssignment != null && value.HasValue && NewManualAssignment.Date != value.Value.DateTime)
                {
                    NewManualAssignment.Date = value.Value.DateTime;
                    OnPropertyChanged();
                }
            }
        }

        public int? NewManualAssignmentPersonnelId
        {
            get => NewManualAssignment?.PersonnelId;
            set
            {
                if (NewManualAssignment != null && value.HasValue && NewManualAssignment.PersonnelId != value.Value)
                {
                    NewManualAssignment.PersonnelId = value.Value;
                    OnPropertyChanged();
                }
            }
        }

        public int? NewManualAssignmentPositionId
        {
            get => NewManualAssignment?.PositionId;
            set
            {
                if (NewManualAssignment != null && value.HasValue && NewManualAssignment.PositionId != value.Value)
                {
                    NewManualAssignment.PositionId = value.Value;
                    OnPropertyChanged();
                }
            }
        }

        public int? NewManualAssignmentTimeSlot
        {
            get => NewManualAssignment?.TimeSlot;
            set
            {
                if (NewManualAssignment != null && value.HasValue && NewManualAssignment.TimeSlot != value.Value)
                {
                    NewManualAssignment.TimeSlot = value.Value;
                    OnPropertyChanged();
                }
            }
        }

        public string NewManualAssignmentRemarks
        {
            get => NewManualAssignment?.Remarks ?? string.Empty;
            set
            {
                if (NewManualAssignment != null && NewManualAssignment.Remarks != value)
                {
                    NewManualAssignment.Remarks = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTimeOffset? EditingManualAssignmentDate
        {
            get => EditingManualAssignmentDto != null ? new DateTimeOffset(EditingManualAssignmentDto.Date) : null;
            set
            {
                if (EditingManualAssignmentDto != null && value.HasValue && EditingManualAssignmentDto.Date != value.Value.DateTime)
                {
                    EditingManualAssignmentDto.Date = value.Value.DateTime;
                    OnPropertyChanged();
                }
            }
        }

        public int EditingManualAssignmentPersonnelId
        {
            get => EditingManualAssignmentDto?.PersonnelId ?? 0;
            set
            {
                if (EditingManualAssignmentDto != null && EditingManualAssignmentDto.PersonnelId != value)
                {
                    EditingManualAssignmentDto.PersonnelId = value;
                    OnPropertyChanged();
                }
            }
        }

        public int EditingManualAssignmentPositionId
        {
            get => EditingManualAssignmentDto?.PositionId ?? 0;
            set
            {
                if (EditingManualAssignmentDto != null && EditingManualAssignmentDto.PositionId != value)
                {
                    EditingManualAssignmentDto.PositionId = value;
                    OnPropertyChanged();
                }
            }
        }

        public int EditingManualAssignmentTimeSlot
        {
            get => EditingManualAssignmentDto?.TimeSlot ?? 0;
            set
            {
                if (EditingManualAssignmentDto != null && EditingManualAssignmentDto.TimeSlot != value)
                {
                    EditingManualAssignmentDto.TimeSlot = value;
                    OnPropertyChanged();
                }
            }
        }

        public string EditingManualAssignmentRemarks
        {
            get => EditingManualAssignmentDto?.Remarks ?? string.Empty;
            set
            {
                if (EditingManualAssignmentDto != null && EditingManualAssignmentDto.Remarks != value)
                {
                    EditingManualAssignmentDto.Remarks = value;
                    OnPropertyChanged();
                }
            }
        }

        // 时段选项（静态列表）
        public List<TimeSlotOption> TimeSlotOptions { get; } = TimeSlotOption.GetAll();

        // 表单验证错误消息
        [ObservableProperty]
        private string _dateValidationError = string.Empty;

        [ObservableProperty]
        private string _personnelValidationError = string.Empty;

        [ObservableProperty]
        private string _positionValidationError = string.Empty;

        [ObservableProperty]
        private string _timeSlotValidationError = string.Empty;

        [ObservableProperty]
        private ScheduleDto? _resultSchedule;

        [ObservableProperty]
        private bool _isExecuting;
        [ObservableProperty]
        private bool _isLoadingInitial;
        [ObservableProperty]
        private bool _isLoadingConstraints; // ʹԹ UI ʾ״̬

        // ģأʹԶֶΣڰ߼ͳһ
        [ObservableProperty] private int? _loadedTemplateId;
        [ObservableProperty] private bool _templateApplied;

        // 5չʾ
        [ObservableProperty] private ObservableCollection<SummarySection> _summarySections = new();

        // 
        public IAsyncRelayCommand LoadDataCommand { get; }
        public IRelayCommand NextStepCommand { get; }
        public IRelayCommand PreviousStepCommand { get; }
        public IAsyncRelayCommand ExecuteSchedulingCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IAsyncRelayCommand<int> LoadTemplateCommand { get; }
        public IAsyncRelayCommand LoadConstraintsCommand { get; }
        public IAsyncRelayCommand SaveAsTemplateCommand { get; }

        // 手动指定命令
        public IRelayCommand StartCreateManualAssignmentCommand { get; }
        public IAsyncRelayCommand SubmitCreateManualAssignmentCommand { get; }
        public IRelayCommand CancelCreateManualAssignmentCommand { get; }
        public IRelayCommand<ManualAssignmentViewModel> StartEditManualAssignmentCommand { get; }
        public IAsyncRelayCommand SubmitEditManualAssignmentCommand { get; }
        public IRelayCommand CancelEditManualAssignmentCommand { get; }
        public IAsyncRelayCommand<ManualAssignmentViewModel> DeleteManualAssignmentCommand { get; }

        // 为哨位添加人员命令
        public IRelayCommand<PositionDto> StartAddPersonnelToPositionCommand { get; }
        public IAsyncRelayCommand SubmitAddPersonnelToPositionCommand { get; }
        public IRelayCommand CancelAddPersonnelToPositionCommand { get; }

        // 移除和撤销人员命令
        public IRelayCommand<(int positionId, int personnelId)> RemovePersonnelFromPositionCommand { get; }
        public IRelayCommand<int> RevertPositionChangesCommand { get; }

        // 保存为永久命令
        public IAsyncRelayCommand<int> SavePositionChangesCommand { get; }

        // 手动添加参与人员命令（不属于任何哨位）
        public IRelayCommand StartManualAddPersonnelCommand { get; }
        public IAsyncRelayCommand SubmitManualAddPersonnelCommand { get; }
        public IRelayCommand CancelManualAddPersonnelCommand { get; }
        public IRelayCommand<int> RemoveManualPersonnelCommand { get; }

        public SchedulingViewModel(
        ISchedulingService schedulingService,
        IPersonnelService personnelService,
        IPositionService positionService,
        ITemplateService templateService,
        ISchedulingDraftService? draftService,
        DialogService dialogService,
        NavigationService navigationService)
        {
            _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
            _personnelService = personnelService ?? throw new ArgumentNullException(nameof(personnelService));
            _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _draftService = draftService; // 草稿服务可以为null
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _navigation_service = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            
            // 初始化手动指定管理器
            _manualAssignmentManager = new ManualAssignmentManager();
            
            // 初始化哨位人员管理器
            _positionPersonnelManager = new PositionPersonnelManager();

            LoadDataCommand = new AsyncRelayCommand(LoadInitialDataAsync);
            NextStepCommand = new RelayCommand(NextStep, CanGoNext);
            PreviousStepCommand = new RelayCommand(PreviousStep, () => CurrentStep > 1);
            ExecuteSchedulingCommand = new AsyncRelayCommand(ExecuteSchedulingAsync, CanExecuteScheduling);
            CancelCommand = new RelayCommand(CancelWizard);
            LoadTemplateCommand = new AsyncRelayCommand<int>(LoadTemplateAsync);
            LoadConstraintsCommand = new AsyncRelayCommand(LoadConstraintsAsync);
            SaveAsTemplateCommand = new AsyncRelayCommand(SaveAsTemplateAsync, CanSaveTemplate);

            // 初始化手动指定命令
            StartCreateManualAssignmentCommand = new RelayCommand(StartCreateManualAssignment);
            SubmitCreateManualAssignmentCommand = new AsyncRelayCommand(SubmitCreateManualAssignmentAsync);
            CancelCreateManualAssignmentCommand = new RelayCommand(CancelCreateManualAssignment);
            StartEditManualAssignmentCommand = new RelayCommand<ManualAssignmentViewModel?>(StartEditManualAssignment);
            SubmitEditManualAssignmentCommand = new AsyncRelayCommand(SubmitEditManualAssignmentAsync);
            CancelEditManualAssignmentCommand = new RelayCommand(CancelEditManualAssignment);
            DeleteManualAssignmentCommand = new AsyncRelayCommand<ManualAssignmentViewModel?>(DeleteManualAssignmentAsync);

            // 初始化为哨位添加人员命令
            StartAddPersonnelToPositionCommand = new RelayCommand<PositionDto>(StartAddPersonnelToPosition);
            SubmitAddPersonnelToPositionCommand = new AsyncRelayCommand(SubmitAddPersonnelToPositionAsync);
            CancelAddPersonnelToPositionCommand = new RelayCommand(CancelAddPersonnelToPosition);

            // 初始化移除和撤销人员命令
            RemovePersonnelFromPositionCommand = new RelayCommand<(int positionId, int personnelId)>(RemovePersonnelFromPosition);
            RevertPositionChangesCommand = new RelayCommand<int>(RevertPositionChanges);

            // 初始化保存为永久命令
            SavePositionChangesCommand = new AsyncRelayCommand<int>(SavePositionChangesAsync);

            // 初始化手动添加参与人员命令
            StartManualAddPersonnelCommand = new RelayCommand(StartManualAddPersonnel);
            SubmitManualAddPersonnelCommand = new AsyncRelayCommand(SubmitManualAddPersonnelAsync);
            CancelManualAddPersonnelCommand = new RelayCommand(CancelManualAddPersonnel);
            RemoveManualPersonnelCommand = new RelayCommand<int>(RemoveManualPersonnel);

            // Listen to property changes to refresh command states
            PropertyChanged += (s, e) =>
            {
                if (e?.PropertyName is nameof(CurrentStep) or nameof(ScheduleTitle) or nameof(StartDate) or nameof(EndDate) or nameof(IsExecuting))
                {
                    RefreshCommandStates();
                }
                
                // 当进入步骤3时，更新哨位人员视图模型
                if (e?.PropertyName == nameof(CurrentStep) && CurrentStep == 3)
                {
                    UpdatePositionPersonnelViewModels();
                }
            };
            SelectedPersonnels.CollectionChanged += (s, e) => RefreshCommandStates();
            SelectedPositions.CollectionChanged += (s, e) => 
            {
                RefreshCommandStates();
                // 当选择的哨位发生变化时，更新哨位人员视图模型
                if (CurrentStep == 3)
                {
                    UpdatePositionPersonnelViewModels();
                }
            };
        }

        private void NextStep()
        {
            if (CurrentStep < 5 && CanGoNext())
            {
                // 如果模板已应用，并且在第1步，直接跳到第5步
                if (TemplateApplied && CurrentStep == 1)
                {
                    CurrentStep = 5;
                    BuildSummarySections(); // Build summary when jumping to step 5
                }
                else
                {
                    CurrentStep++;
                }

                if (CurrentStep == 4 && !IsLoadingConstraints && FixedPositionRules.Count == 0)
                {
                    _ = LoadConstraintsAsync();
                }
                if (CurrentStep == 5)
                {
                    BuildSummarySections();
                }
                RefreshCommandStates();
            }
        }
        private void PreviousStep()
        {
            if (CurrentStep > 1)
            {
                // 如果模板已应用，防止回退到人员/岗位/约束步骤，只能回到第1步
                if (TemplateApplied && CurrentStep > 1 && CurrentStep <= 5)
                {
                    CurrentStep = 1;
                }
                else
                {
                    CurrentStep--;
                }
                RefreshCommandStates();
            }
        }
        private bool CanGoNext() => CurrentStep switch
        {
            1 => ValidateStep1(out _),
            2 => ValidateStep2(out _),
            3 => ValidateStep3(out _),
            4 => true, // Constraints step is always navigable if reached
            5 => false, // Cannot go next from summary
            _ => false
        };
        private bool CanExecuteScheduling()
        {
            var step5 = CurrentStep == 5;
            var notExecuting = !IsExecuting;
            var step1Valid = ValidateStep1(out var step1Error);
            var step2Valid = ValidateStep2(out var step2Error);
            var step3Valid = ValidateStep3(out var step3Error);
            
            var canExecute = step5 && notExecuting && step1Valid && step2Valid && step3Valid;
            
            // 只在步骤5且验证失败时输出调试信息
            if (step5 && !canExecute)
            {
                System.Diagnostics.Debug.WriteLine($"=== 开始排班按钮被禁用 ===");
                if (IsExecuting)
                    System.Diagnostics.Debug.WriteLine($"原因: 正在执行排班");
                if (!step1Valid)
                    System.Diagnostics.Debug.WriteLine($"原因: 步骤1验证失败 - {step1Error}");
                if (!step2Valid)
                    System.Diagnostics.Debug.WriteLine($"原因: 步骤2验证失败 - {step2Error}");
                if (!step3Valid)
                    System.Diagnostics.Debug.WriteLine($"原因: 步骤3验证失败 - {step3Error}");
            }
            
            return canExecute;
        }
        private void RefreshCommandStates()
        {
            NextStepCommand.NotifyCanExecuteChanged();
            PreviousStepCommand.NotifyCanExecuteChanged();
            ExecuteSchedulingCommand.NotifyCanExecuteChanged();
            SaveAsTemplateCommand.NotifyCanExecuteChanged();
        }

        private bool ValidateStep1([NotNullWhen(false)] out string? error)
        {
            if (string.IsNullOrWhiteSpace(ScheduleTitle)) { error = "排班标题不能为空"; return false; }
            if (ScheduleTitle.Length > 100) { error = "排班标题长度不能超过100字符"; return false; }
            if (StartDate.Date < DateTimeOffset.Now.Date) { error = "开始日期不能早于今天"; return false; }
            if (EndDate.Date < StartDate.Date) { error = "结束日期不能早于开始日期"; return false; }
            if ((EndDate.Date - StartDate.Date).TotalDays + 1 > 365) { error = "排班周期不能超过365天"; return false; }
            error = null; return true;
        }
        private bool ValidateStep2([NotNullWhen(false)] out string? error)
        {
            // Step 2 now validates positions (swapped with step 3)
            if (SelectedPositions == null || SelectedPositions.Count == 0) { error = "请至少选择一个岗位"; return false; }
            error = null; return true;
        }
        private bool ValidateStep3([NotNullWhen(false)] out string? error)
        {
            // Step 3 now validates personnel (swapped with step 2)
            if (SelectedPersonnels == null || SelectedPersonnels.Count == 0) { error = "请至少选择一名人员"; return false; }
            if (SelectedPersonnels.Any(p => !p.IsAvailable || p.IsRetired)) { error = "选择的人员有不可用人员，请移除"; return false; }
            error = null; return true;
        }

        private async Task LoadInitialDataAsync()
        {
            if (IsLoadingInitial) return;
            IsLoadingInitial = true;
            try
            {
                var personnelTask = _personnelService.GetAllAsync();
                var positionTask = _positionService.GetAllAsync();
                await Task.WhenAll(personnelTask, positionTask);
                AvailablePersonnels = new ObservableCollection<PersonnelDto>(personnelTask.Result);
                AvailablePositions = new ObservableCollection<PositionDto>(positionTask.Result);
                
                // 构建缓存
                BuildCaches();
                
                // 默认标题
                if (string.IsNullOrWhiteSpace(ScheduleTitle))
                    ScheduleTitle = $"排班_{DateTime.Now:yyyyMMdd}";
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("加载人员/岗位失败", ex);
            }
            finally
            {
                IsLoadingInitial = false;
            }
        }

        /// <summary>
        /// 构建人员和哨位查找缓存
        /// </summary>
        private void BuildCaches()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // 构建人员缓存
            _personnelCache.Clear();
            foreach (var personnel in AvailablePersonnels)
            {
                _personnelCache[personnel.Id] = personnel;
            }
            
            // 构建哨位缓存
            _positionCache.Clear();
            foreach (var position in AvailablePositions)
            {
                _positionCache[position.Id] = position;
            }
            
            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine($"缓存构建完成: {_personnelCache.Count}个人员, {_positionCache.Count}个哨位, 耗时{stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 从缓存中获取人员
        /// </summary>
        private PersonnelDto? GetPersonnelFromCache(int personnelId)
        {
            return _personnelCache.TryGetValue(personnelId, out var personnel) ? personnel : null;
        }

        /// <summary>
        /// 从缓存中获取哨位
        /// </summary>
        private PositionDto? GetPositionFromCache(int positionId)
        {
            return _positionCache.TryGetValue(positionId, out var position) ? position : null;
        }

        private async Task LoadConstraintsAsync()
        {
            // 重复加载检查
            if (IsLoadingConstraints)
            {
                System.Diagnostics.Debug.WriteLine("约束数据正在加载中，跳过重复请求");
                return;
            }
            
            IsLoadingConstraints = true;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            System.Diagnostics.Debug.WriteLine("=== 开始加载约束数据 ===");
            System.Diagnostics.Debug.WriteLine($"日期范围: {StartDate:yyyy-MM-dd} 到 {EndDate:yyyy-MM-dd}");
            
            try
            {
                // 并行加载三种约束数据
                var configsTask = _schedulingService.GetHolidayConfigsAsync();
                var rulesTask = _schedulingService.GetFixedPositionRulesAsync(true);
                var manualTask = _schedulingService.GetManualAssignmentsAsync(StartDate.Date, EndDate.Date, true);
                
                await Task.WhenAll(configsTask, rulesTask, manualTask);
                
                var configs = configsTask.Result;
                var rules = rulesTask.Result;
                var manuals = manualTask.Result;
                
                System.Diagnostics.Debug.WriteLine($"加载完成 - 休息日配置: {configs.Count}, 定岗规则: {rules.Count}, 手动指定: {manuals.Count}");
                
                // 更新 ObservableCollection
                HolidayConfigs = new ObservableCollection<HolidayConfig>(configs);
                FixedPositionRules = new ObservableCollection<FixedPositionRule>(rules);
                ManualAssignments = new ObservableCollection<ManualAssignment>(manuals);
                
                // 转换为DTO并加载手动指定到ManualAssignmentManager（使用缓存提高性能）
                var manualDtos = manuals.Select(m =>
                {
                    // 从缓存中查找人员和岗位名称
                    var personnel = GetPersonnelFromCache(m.PersonalId);
                    var position = GetPositionFromCache(m.PositionId);
                    
                    return new ManualAssignmentDto
                    {
                        Id = m.Id,
                        Date = m.Date,
                        PersonnelId = m.PersonalId,
                        PersonnelName = personnel?.Name ?? "未知人员",
                        PositionId = m.PositionId,
                        PositionName = position?.Name ?? "未知哨位",
                        TimeSlot = m.PeriodIndex,
                        Remarks = m.Remarks,
                        IsEnabled = m.IsEnabled,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                }).ToList();
                _manualAssignmentManager.LoadSaved(manualDtos);
                System.Diagnostics.Debug.WriteLine($"已加载 {manuals.Count} 条手动指定到ManualAssignmentManager");
                
                // 空数据警告日志
                if (configs.Count == 0)
                    System.Diagnostics.Debug.WriteLine("警告: 没有找到休息日配置");
                if (rules.Count == 0)
                    System.Diagnostics.Debug.WriteLine("警告: 没有找到定岗规则");
                if (manuals.Count == 0)
                    System.Diagnostics.Debug.WriteLine("警告: 没有找到手动指定");
                
                // 应用模板约束（如果有）
                if (TemplateApplied)
                {
                    System.Diagnostics.Debug.WriteLine("应用模板约束设置");
                    ApplyTemplateConstraints();
                }
            }
            catch (Microsoft.Data.Sqlite.SqliteException sqlEx)
            {
                System.Diagnostics.Debug.WriteLine("=== 加载约束数据失败 (数据库错误) ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {sqlEx.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误代码: {sqlEx.SqliteErrorCode}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {sqlEx.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {sqlEx.StackTrace}");
                
                await _dialogService.ShowErrorAsync(
                    "无法连接到数据库",
                    $"请检查数据库文件是否存在且未被占用。\n\n错误详情: {sqlEx.Message}");
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine("=== 加载约束数据失败 (数据格式错误) ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {jsonEx.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {jsonEx.Message}");
                System.Diagnostics.Debug.WriteLine($"路径: {jsonEx.Path}");
                System.Diagnostics.Debug.WriteLine($"行号: {jsonEx.LineNumber}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {jsonEx.StackTrace}");
                
                await _dialogService.ShowErrorAsync(
                    "约束数据格式错误",
                    $"数据库中的约束数据格式不正确，可能需要重新创建。\n\n错误详情: {jsonEx.Message}");
            }
            catch (TaskCanceledException taskEx)
            {
                System.Diagnostics.Debug.WriteLine("=== 加载约束数据失败 (超时) ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {taskEx.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {taskEx.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {taskEx.StackTrace}");
                
                await _dialogService.ShowWarningAsync("加载约束数据超时，请重试");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 加载约束数据失败 (未知错误) ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"内部异常: {ex.InnerException.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"内部异常消息: {ex.InnerException.Message}");
                }
                
                await _dialogService.ShowErrorAsync(
                    "加载约束数据失败",
                    $"发生了意外错误。\n\n错误类型: {ex.GetType().Name}\n错误消息: {ex.Message}");
            }
            finally
            {
                IsLoadingConstraints = false;
                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine($"约束数据加载耗时: {stopwatch.ElapsedMilliseconds}ms");
                System.Diagnostics.Debug.WriteLine("=== 约束数据加载流程结束 ===");
            }
        }

        private async Task ExecuteSchedulingAsync()
        {
            if (!CanExecuteScheduling())
            {
                if (!ValidateStep1(out var e1)) await _dialogService.ShowWarningAsync(e1);
                else if (!ValidateStep2(out var e2)) await _dialogService.ShowWarningAsync(e2);
                else if (!ValidateStep3(out var e3)) await _dialogService.ShowWarningAsync(e3);
                return;
            }
            
            try
            {
                // 在后台线程构建请求，避免UI冻结
                var request = await Task.Run(() => BuildSchedulingRequest());
                
                System.Diagnostics.Debug.WriteLine($"准备导航到排班进度页面: {request.Title}");
                System.Diagnostics.Debug.WriteLine($"人员数: {request.PersonnelIds.Count}, 哨位数: {request.PositionIds.Count}");
                
                // 验证请求数据的有效性
                if (request.PersonnelIds == null || !request.PersonnelIds.Any())
                {
                    await _dialogService.ShowWarningAsync("没有选择任何人员，无法开始排班");
                    return;
                }
                
                if (request.PositionIds == null || !request.PositionIds.Any())
                {
                    await _dialogService.ShowWarningAsync("没有选择任何哨位，无法开始排班");
                    return;
                }
                
                // 导航到排班进度可视化页面，传递 SchedulingRequestDto 参数
                _navigation_service.NavigateTo("SchedulingProgress", request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"执行排班失败: {ex.Message}");
                await _dialogService.ShowErrorAsync("执行失败", $"启动排班时发生错误：{ex.Message}");
            }
        }
        private SchedulingRequestDto BuildSchedulingRequest()
        {
            // 获取所有启用的手动指定
            var allEnabledAssignments = _manualAssignmentManager.GetAllEnabled();
            
            // 分离已保存的和临时的手动指定
            var enabledManualAssignmentIds = allEnabledAssignments
                .Where(a => a.Id.HasValue)
                .Select(a => a.Id!.Value)
                .ToList();
            
            var temporaryManualAssignments = allEnabledAssignments
                .Where(a => !a.Id.HasValue)
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"BuildSchedulingRequest - 已保存的手动指定: {enabledManualAssignmentIds.Count}, 临时手动指定: {temporaryManualAssignments.Count}");
            
            return new SchedulingRequestDto
            {
                Title = ScheduleTitle.Trim(),
                StartDate = StartDate.DateTime.Date,
                EndDate = EndDate.DateTime.Date,
                PersonnelIds = SelectedPersonnels.Select(p => p.Id).ToList(),
                PositionIds = SelectedPositions.Select(p => p.Id).ToList(),
                UseActiveHolidayConfig = UseActiveHolidayConfig,
                HolidayConfigId = UseActiveHolidayConfig ? null : SelectedHolidayConfigId,
                EnabledFixedRuleIds = FixedPositionRules.Where(r => r.IsEnabled).Select(r => r.Id).ToList(),
                EnabledManualAssignmentIds = enabledManualAssignmentIds,
                TemporaryManualAssignments = temporaryManualAssignments
            };
        }

        private async void CancelWizard()
        {
            // 检查是否有值得保存的进度
            if (ShouldPromptForDraftSave())
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "保留进度",
                    Content = "当前进度将保存为草稿，是否保留？下次可以继续编辑。",
                    PrimaryButtonText = "保留",
                    SecondaryButtonText = "放弃",
                    DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                    XamlRoot = App.MainWindow?.Content?.XamlRoot
                };

                if (dialog.XamlRoot != null)
                {
                    try
                    {
                        var result = await dialog.ShowAsync();

                        if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Secondary)
                        {
                            // 用户选择放弃，删除草稿
                            if (_draftService != null)
                            {
                                try
                                {
                                    await _draftService.DeleteDraftAsync();
                                    System.Diagnostics.Debug.WriteLine("[SchedulingViewModel] Draft deleted after user chose to discard");
                                }
                                catch (Exception draftEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel] Failed to delete draft: {draftEx.Message}");
                                    // 不影响主流程，仅记录日志
                                }
                            }
                        }
                        // 如果选择保留，草稿会在OnNavigatedFrom时自动保存
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel] Failed to show cancel dialog: {ex.Message}");
                        // 如果对话框显示失败，继续执行重置逻辑
                    }
                }
            }

            // 重置状态
            ResetViewModelState();
        }

        private void ResetViewModelState()
        {
            CurrentStep = 1;
            ScheduleTitle = string.Empty;
            SelectedPersonnels.Clear();
            SelectedPositions.Clear();
            EnabledFixedRules.Clear();
            EnabledManualAssignments.Clear();
            FixedPositionRules.Clear();
            ManualAssignments.Clear();
            HolidayConfigs.Clear();
            ResultSchedule = null;
            SelectedHolidayConfigId = null;
            UseActiveHolidayConfig = true;
            LoadedTemplateId = null;
            TemplateApplied = false;
            SummarySections.Clear();

            // 清空手动指定管理器
            _manualAssignmentManager.Clear();

            // 清空哨位人员管理器
            _positionPersonnelManager.Clear();

            // 清空手动添加的人员列表
            ManuallyAddedPersonnelIds.Clear();
            ManuallyAddedPersonnelDetails.Clear();
            AutoExtractedPersonnelCount = 0;
            ManuallyAddedPersonnelCount = 0;

            RefreshCommandStates();
        }

        /// <summary>
        /// 判断是否应该提示用户保存草稿
        /// </summary>
        private bool ShouldPromptForDraftSave()
        {
            // 如果已经成功创建排班，不需要保存草稿
            if (ResultSchedule != null)
            {
                return false;
            }

            // 检查是否有值得保存的进度
            return !string.IsNullOrWhiteSpace(ScheduleTitle) ||
                   SelectedPersonnels.Count > 0 ||
                   SelectedPositions.Count > 0 ||
                   AllManualAssignments.Count > 0;
        }

        private async Task LoadTemplateAsync(int templateId)
        {
            IsLoadingInitial = true;
            System.Diagnostics.Debug.WriteLine($"=== 开始加载模板 (ID: {templateId}) ===");
            
            try
            {
                var template = await _templateService.GetByIdAsync(templateId);
                if (template == null)
                {
                    System.Diagnostics.Debug.WriteLine($"模板不存在 (ID: {templateId})");
                    await _dialogService.ShowWarningAsync("模板不存在");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"模板加载成功: {template.Name}");
                System.Diagnostics.Debug.WriteLine($"模板包含 - 人员: {template.PersonnelIds.Count}, 岗位: {template.PositionIds.Count}");
                
                LoadedTemplateId = template.Id;

                // Load base data first
                if (AvailablePersonnels.Count == 0 || AvailablePositions.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("加载基础数据（人员和岗位）");
                    await LoadInitialDataAsync();
                }

                // Pre-fill selections
                var selectedPers = AvailablePersonnels.Where(p => template.PersonnelIds.Contains(p.Id)).ToList();
                var selectedPos = AvailablePositions.Where(p => template.PositionIds.Contains(p.Id)).ToList();
                
                // Validate and identify missing resources
                var missingPersonnelIds = template.PersonnelIds
                    .Except(selectedPers.Select(p => p.Id))
                    .ToList();
                var missingPositionIds = template.PositionIds
                    .Except(selectedPos.Select(p => p.Id))
                    .ToList();
                
                // Display warning if any resources are missing
                if (missingPersonnelIds.Any() || missingPositionIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine("警告: 模板中的部分资源已不存在");
                    if (missingPersonnelIds.Any())
                        System.Diagnostics.Debug.WriteLine($"- 缺失人员ID: {string.Join(", ", missingPersonnelIds)}");
                    if (missingPositionIds.Any())
                        System.Diagnostics.Debug.WriteLine($"- 缺失岗位ID: {string.Join(", ", missingPositionIds)}");
                    
                    var warningMsg = "模板中的部分资源已不存在：\n";
                    if (missingPersonnelIds.Any())
                        warningMsg += $"- 缺失人员ID: {string.Join(", ", missingPersonnelIds)}\n";
                    if (missingPositionIds.Any())
                        warningMsg += $"- 缺失岗位ID: {string.Join(", ", missingPositionIds)}\n";
                    warningMsg += "\n将仅加载可用的资源。";
                    await _dialogService.ShowWarningAsync(warningMsg);
                }
                
                SelectedPersonnels = new ObservableCollection<PersonnelDto>(selectedPers);
                SelectedPositions = new ObservableCollection<PositionDto>(selectedPos);

                // Store constraint IDs to be applied after they are loaded
                UseActiveHolidayConfig = template.UseActiveHolidayConfig;
                SelectedHolidayConfigId = template.HolidayConfigId;
                _enabledFixedRules = template.EnabledFixedRuleIds?.ToList() ?? new();
                _enabledManualAssignments = template.EnabledManualAssignmentIds?.ToList() ?? new();
                
                System.Diagnostics.Debug.WriteLine($"模板约束配置 - 固定规则: {_enabledFixedRules.Count}, 手动指定: {_enabledManualAssignments.Count}");
                System.Diagnostics.Debug.WriteLine($"节假日配置 - 使用活动配置: {UseActiveHolidayConfig}, 配置ID: {SelectedHolidayConfigId}");

                // Immediately load constraint data
                System.Diagnostics.Debug.WriteLine("开始加载约束数据以应用模板设置");
                await LoadConstraintsAsync();
                
                // Apply template constraints after loading
                System.Diagnostics.Debug.WriteLine("约束数据加载完成，开始应用模板约束设置");
                ApplyTemplateConstraints();

                TemplateApplied = true;
                CurrentStep = 1; // Stay on step 1
                RefreshCommandStates();
                
                // Calculate constraint counts
                var enabledFixedRulesCount = FixedPositionRules.Count(r => r.IsEnabled);
                var enabledManualAssignmentsCount = ManualAssignments.Count(a => a.IsEnabled);
                var totalConstraints = enabledFixedRulesCount + enabledManualAssignmentsCount;
                
                System.Diagnostics.Debug.WriteLine($"模板应用完成 - 已启用约束: {totalConstraints} (固定规则: {enabledFixedRulesCount}, 手动指定: {enabledManualAssignmentsCount})");
                System.Diagnostics.Debug.WriteLine("=== 模板加载流程结束 ===");
                
                // Display success message with statistics
                var successMsg = $"模板已加载\n" +
                                $"人员: {selectedPers.Count}\n" +
                                $"岗位: {selectedPos.Count}\n" +
                                $"约束: {totalConstraints}";
                await _dialogService.ShowSuccessAsync(successMsg);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 加载模板失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                await _dialogService.ShowErrorAsync("加载模板失败", ex);
            }
            finally
            {
                IsLoadingInitial = false;
            }
        }

        private void ApplyTemplateConstraints()
        {
            if (!TemplateApplied)
            {
                System.Diagnostics.Debug.WriteLine("ApplyTemplateConstraints: 模板未应用，跳过");
                return;
            }

            System.Diagnostics.Debug.WriteLine("=== 开始应用模板约束 ===");
            System.Diagnostics.Debug.WriteLine($"模板中的固定规则ID: [{string.Join(", ", _enabledFixedRules)}]");
            System.Diagnostics.Debug.WriteLine($"模板中的手动指定ID: [{string.Join(", ", _enabledManualAssignments)}]");
            System.Diagnostics.Debug.WriteLine($"数据库中的固定规则数量: {FixedPositionRules.Count}");
            System.Diagnostics.Debug.WriteLine($"数据库中的手动指定数量: {ManualAssignments.Count}");

            // 验证并应用固定规则
            var appliedFixedRules = 0;
            var missingFixedRuleIds = new List<int>();
            
            foreach (var rule in FixedPositionRules)
            {
                if (_enabledFixedRules.Contains(rule.Id))
                {
                    rule.IsEnabled = true;
                    appliedFixedRules++;
                    System.Diagnostics.Debug.WriteLine($"启用固定规则: ID={rule.Id}, 描述={rule.Description}");
                }
                else
                {
                    rule.IsEnabled = false;
                }
            }
            
            // 检查模板中的规则是否在数据库中存在
            var existingFixedRuleIds = FixedPositionRules.Select(r => r.Id).ToHashSet();
            foreach (var templateRuleId in _enabledFixedRules)
            {
                if (!existingFixedRuleIds.Contains(templateRuleId))
                {
                    missingFixedRuleIds.Add(templateRuleId);
                }
            }

            // 验证并应用手动指定
            var appliedManualAssignments = 0;
            var missingManualAssignmentIds = new List<int>();
            
            foreach (var assignment in ManualAssignments)
            {
                if (_enabledManualAssignments.Contains(assignment.Id))
                {
                    assignment.IsEnabled = true;
                    appliedManualAssignments++;
                    System.Diagnostics.Debug.WriteLine($"启用手动指定: ID={assignment.Id}, 日期={assignment.Date:yyyy-MM-dd}, 人员={assignment.PersonalId}, 岗位={assignment.PositionId}");
                }
                else
                {
                    assignment.IsEnabled = false;
                }
            }
            
            // 同步手动指定启用状态到ManualAssignmentManager
            foreach (var savedAssignment in _manualAssignmentManager.SavedAssignments)
            {
                if (savedAssignment.Id.HasValue)
                {
                    savedAssignment.IsEnabled = _enabledManualAssignments.Contains(savedAssignment.Id.Value);
                }
            }
            System.Diagnostics.Debug.WriteLine($"已同步 {_manualAssignmentManager.SavedAssignments.Count} 条手动指定的启用状态到ManualAssignmentManager");
            
            // 检查模板中的手动指定是否在数据库中存在
            var existingManualAssignmentIds = ManualAssignments.Select(a => a.Id).ToHashSet();
            foreach (var templateAssignmentId in _enabledManualAssignments)
            {
                if (!existingManualAssignmentIds.Contains(templateAssignmentId))
                {
                    missingManualAssignmentIds.Add(templateAssignmentId);
                }
            }

            System.Diagnostics.Debug.WriteLine($"应用结果 - 固定规则: {appliedFixedRules}/{_enabledFixedRules.Count}, 手动指定: {appliedManualAssignments}/{_enabledManualAssignments.Count}");

            // 记录缺失的约束
            if (missingFixedRuleIds.Any() || missingManualAssignmentIds.Any())
            {
                System.Diagnostics.Debug.WriteLine("警告: 模板中的部分约束在数据库中不存在");
                
                if (missingFixedRuleIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"- 缺失的固定规则ID: {string.Join(", ", missingFixedRuleIds)}");
                }
                
                if (missingManualAssignmentIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"- 缺失的手动指定ID: {string.Join(", ", missingManualAssignmentIds)}");
                }
                
                // 构建警告消息并显示给用户
                var warningMsg = "模板中的部分约束已不存在：\n";
                if (missingFixedRuleIds.Any())
                {
                    warningMsg += $"- 缺失的固定规则: {missingFixedRuleIds.Count} 条 (ID: {string.Join(", ", missingFixedRuleIds)})\n";
                }
                if (missingManualAssignmentIds.Any())
                {
                    warningMsg += $"- 缺失的手动指定: {missingManualAssignmentIds.Count} 条 (ID: {string.Join(", ", missingManualAssignmentIds)})\n";
                }
                warningMsg += "\n将仅应用存在的约束。";
                
                // 异步显示警告（不阻塞当前流程）
                _ = _dialogService.ShowWarningAsync(warningMsg);
            }
            
            System.Diagnostics.Debug.WriteLine("=== 模板约束应用完成 ===");
        }

        private bool CanSaveTemplate() => SelectedPersonnels.Count > 0 && SelectedPositions.Count > 0;
        private async Task SaveAsTemplateAsync()
        {
            if (!CanSaveTemplate()) { await _dialogService.ShowWarningAsync("当前配置不完整，无法保存为模板"); return; }
            // 创建自定义对话框 (名称、类型、描述、是否默认)
            var nameBox = new Microsoft.UI.Xaml.Controls.TextBox { PlaceholderText = "模板名称", Text = $"模板_{DateTime.Now:yyyyMMdd}" };
            var typeBox = new Microsoft.UI.Xaml.Controls.ComboBox { ItemsSource = new string[] { "regular", "holiday", "special" }, SelectedIndex = 0, MinWidth = 160 };
            var descBox = new Microsoft.UI.Xaml.Controls.TextBox { AcceptsReturn = true, Height = 80, PlaceholderText = "描述(可选)" };
            var defaultSwitch = new Microsoft.UI.Xaml.Controls.ToggleSwitch { Header = "设为默认", IsOn = false };
            var panel = new Microsoft.UI.Xaml.Controls.StackPanel { Spacing = 8 };
            panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "名称:" });
            panel.Children.Add(nameBox);
            panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "类型:" });
            panel.Children.Add(typeBox);
            panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "描述:" });
            panel.Children.Add(descBox);
            panel.Children.Add(defaultSwitch);
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "另存为模板",
                Content = panel,
                PrimaryButtonText = "保存",
                SecondaryButtonText = "取消",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };

            if (dialog.XamlRoot == null) return;

            var result = await dialog.ShowAsync();
            if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary) return;
            var name = nameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name)) { await _dialogService.ShowWarningAsync("名称不能为空"); return; }
            var type = typeBox.SelectedItem?.ToString() ?? "regular";
            if (name.Length > 100) { await _dialogService.ShowWarningAsync("名称不能超过100字符"); return; }
            
            // 保存临时手动指定到数据库
            var tempIdToSavedIdMap = new Dictionary<Guid, int>();
            var savedManualAssignmentIds = new List<int>();
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== 开始保存模板 '{name}' ===");
                System.Diagnostics.Debug.WriteLine($"临时手动指定数量: {_manualAssignmentManager.TemporaryAssignments.Count}");
                
                // 保存所有临时手动指定
                foreach (var tempAssignment in _manualAssignmentManager.TemporaryAssignments)
                {
                    var createDto = new CreateManualAssignmentDto
                    {
                        Date = tempAssignment.Date,
                        PersonnelId = tempAssignment.PersonnelId,
                        PositionId = tempAssignment.PositionId,
                        TimeSlot = tempAssignment.TimeSlot,
                        Remarks = tempAssignment.Remarks,
                        IsEnabled = tempAssignment.IsEnabled
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"保存临时手动指定: 日期={tempAssignment.Date:yyyy-MM-dd}, 人员={tempAssignment.PersonnelName}, 哨位={tempAssignment.PositionName}, 时段={tempAssignment.TimeSlot}");
                    
                    var savedDto = await _schedulingService.CreateManualAssignmentAsync(createDto);
                    tempIdToSavedIdMap[tempAssignment.TempId] = savedDto.Id;
                    savedManualAssignmentIds.Add(savedDto.Id);
                    
                    System.Diagnostics.Debug.WriteLine($"手动指定已保存，ID={savedDto.Id}");
                }
                
                // 将临时手动指定标记为已保存
                if (tempIdToSavedIdMap.Count > 0)
                {
                    _manualAssignmentManager.MarkAsSaved(tempIdToSavedIdMap);
                    System.Diagnostics.Debug.WriteLine($"已将 {tempIdToSavedIdMap.Count} 条临时手动指定标记为已保存");
                }
                
                // 收集所有启用的手动指定ID（包括已保存的和新保存的）
                var allEnabledManualAssignmentIds = ManualAssignments.Where(a => a.IsEnabled).Select(a => a.Id).ToList();
                allEnabledManualAssignmentIds.AddRange(savedManualAssignmentIds);
                
                System.Diagnostics.Debug.WriteLine($"模板将包含 {allEnabledManualAssignmentIds.Count} 条启用的手动指定");
                
                var templateDto = new CreateTemplateDto
                {
                    Name = name,
                    TemplateType = type,
                    Description = string.IsNullOrWhiteSpace(descBox.Text) ? null : descBox.Text.Trim(),
                    IsDefault = defaultSwitch.IsOn,
                    PersonnelIds = SelectedPersonnels.Select(p => p.Id).ToList(),
                    PositionIds = SelectedPositions.Select(p => p.Id).ToList(),
                    HolidayConfigId = UseActiveHolidayConfig ? null : SelectedHolidayConfigId,
                    UseActiveHolidayConfig = UseActiveHolidayConfig,
                    EnabledFixedRuleIds = FixedPositionRules.Where(r => r.IsEnabled).Select(r => r.Id).ToList(),
                    EnabledManualAssignmentIds = allEnabledManualAssignmentIds
                };
                
                var tpl = await _templateService.CreateAsync(templateDto);
                System.Diagnostics.Debug.WriteLine($"模板 '{tpl.Name}' 保存成功，ID={tpl.Id}");
                System.Diagnostics.Debug.WriteLine("=== 模板保存完成 ===");
                
                await _dialogService.ShowSuccessAsync($"模板 '{tpl.Name}' 已保存");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 保存模板失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                // 如果保存失败，需要回滚已保存的手动指定
                if (tempIdToSavedIdMap.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("尝试回滚已保存的手动指定...");
                    // 注意：这里简化处理，实际应该删除已保存的手动指定
                    // 但由于这是一个复杂的回滚操作，暂时只记录日志
                    System.Diagnostics.Debug.WriteLine($"警告: {tempIdToSavedIdMap.Count} 条手动指定已保存但模板创建失败，可能需要手动清理");
                }
                
                await _dialogService.ShowErrorAsync("保存模板失败", ex);
            }
        }

        // 准备第5步概览
        private void BuildSummarySections()
        {
            var sections = new List<SummarySection>();
            // 基本信息
            var basic = new SummarySection { Header = "基本信息" };
            basic.Lines.Add($"排班标题: {ScheduleTitle}");
            basic.Lines.Add($"日期范围: {StartDate:yyyy-MM-dd} 到 {EndDate:yyyy-MM-dd} (共计 {(EndDate.Date - StartDate.Date).TotalDays + 1} 天) ");
            sections.Add(basic);
            // 人员
            var per = new SummarySection { Header = $"参与人员 ({SelectedPersonnels.Count})" };
            foreach (var p in SelectedPersonnels.Take(20)) // 限制预览
                per.Lines.Add($"{p.Name} (ID:{p.Id})");
            if (SelectedPersonnels.Count > 20) per.Lines.Add($"... 等 {SelectedPersonnels.Count} 人");
            sections.Add(per);
            // 岗位
            var pos = new SummarySection { Header = $"涉及岗位 ({SelectedPositions.Count})" };
            foreach (var p in SelectedPositions.Take(20))
                pos.Lines.Add($"{p.Name} (ID:{p.Id})");
            if (SelectedPositions.Count > 20) pos.Lines.Add($"... 等 {SelectedPositions.Count} 个岗位");
            sections.Add(pos);
            //约束
            var cons = new SummarySection { Header = "约束配置" };
            cons.Lines.Add(UseActiveHolidayConfig ? "节假日配置: 使用当前活动配置" : $"节假日配置: 自定义配置ID={SelectedHolidayConfigId}");
            var enabledRulesCount = FixedPositionRules.Count(r => r.IsEnabled);
            var enabledAssignmentsCount = ManualAssignments.Count(a => a.IsEnabled);
            cons.Lines.Add($"固定岗位规则: {enabledRulesCount} 条");
            cons.Lines.Add($"手动指定排班: {enabledAssignmentsCount} 条");
            sections.Add(cons);
            
            // 手动指定详情
            var manualAssignmentSection = new SummarySection { Header = "手动指定" };
            var allEnabledAssignments = _manualAssignmentManager.GetAllEnabled();
            
            if (allEnabledAssignments.Count > 0)
            {
                // 按日期和时段排序
                var sortedAssignments = allEnabledAssignments
                    .OrderBy(a => a.Date)
                    .ThenBy(a => a.TimeSlot)
                    .ToList();
                
                foreach (var assignment in sortedAssignments)
                {
                    // 获取人员和哨位名称
                    var personnelName = SelectedPersonnels.FirstOrDefault(p => p.Id == assignment.PersonnelId)?.Name ?? $"人员ID:{assignment.PersonnelId}";
                    var positionName = SelectedPositions.FirstOrDefault(p => p.Id == assignment.PositionId)?.Name ?? $"哨位ID:{assignment.PositionId}";
                    
                    // 格式化时段显示
                    var startHour = assignment.TimeSlot * 2;
                    var endHour = (assignment.TimeSlot + 1) * 2;
                    var timeSlotDisplay = $"时段 {assignment.TimeSlot} ({startHour:D2}:00-{endHour:D2}:00)";
                    
                    // 构建详细信息行
                    var detailLine = $"{assignment.Date:yyyy-MM-dd} | {timeSlotDisplay} | {personnelName} → {positionName}";
                    
                    // 如果有描述，添加到详细信息中
                    if (!string.IsNullOrWhiteSpace(assignment.Remarks))
                    {
                        detailLine += $" | 备注: {assignment.Remarks}";
                    }
                    
                    manualAssignmentSection.Lines.Add(detailLine);
                }
            }
            else
            {
                manualAssignmentSection.Lines.Add("无启用的手动指定");
            }
            
            sections.Add(manualAssignmentSection);
            
            // 哨位临时更改摘要
            var positionChangesSection = new SummarySection { Header = "哨位临时更改" };
            var positionsWithChanges = _positionPersonnelManager.GetPositionsWithChanges();
            
            if (positionsWithChanges.Count > 0)
            {
                foreach (var positionId in positionsWithChanges)
                {
                    var changes = _positionPersonnelManager.GetChanges(positionId);
                    if (changes.HasChanges)
                    {
                        positionChangesSection.Lines.Add($"哨位: {changes.PositionName}");
                        
                        // 显示添加的人员
                        if (changes.AddedPersonnelIds.Count > 0)
                        {
                            positionChangesSection.Lines.Add($"  ➕ 添加人员: {string.Join(", ", changes.AddedPersonnelNames)}");
                        }
                        
                        // 显示移除的人员
                        if (changes.RemovedPersonnelIds.Count > 0)
                        {
                            positionChangesSection.Lines.Add($"  ➖ 移除人员: {string.Join(", ", changes.RemovedPersonnelNames)}");
                        }
                    }
                }
            }
            else
            {
                positionChangesSection.Lines.Add("无临时更改");
            }
            
            sections.Add(positionChangesSection);
            
            // 手动添加人员摘要
            var manuallyAddedPersonnelSection = new SummarySection { Header = "手动添加的人员（对所有哨位可用）" };
            
            if (ManuallyAddedPersonnelIds.Count > 0)
            {
                foreach (var personnelId in ManuallyAddedPersonnelIds)
                {
                    var personnel = GetPersonnelFromCache(personnelId);
                    if (personnel != null)
                    {
                        var skillsDisplay = personnel.SkillNames != null && personnel.SkillNames.Any() 
                            ? $" (技能: {string.Join(", ", personnel.SkillNames)})" 
                            : "";
                        manuallyAddedPersonnelSection.Lines.Add($"{personnel.Name}{skillsDisplay}");
                    }
                    else
                    {
                        manuallyAddedPersonnelSection.Lines.Add($"人员ID: {personnelId} (未找到详细信息)");
                    }
                }
                manuallyAddedPersonnelSection.Lines.Add("");
                manuallyAddedPersonnelSection.Lines.Add("说明: 这些人员可以被分配到任何哨位，不受哨位可用人员列表限制");
            }
            else
            {
                manuallyAddedPersonnelSection.Lines.Add("无手动添加的人员");
            }
            
            sections.Add(manuallyAddedPersonnelSection);
            
            // 模板信息
            if (TemplateApplied && LoadedTemplateId.HasValue)
            {
                sections.Add(new SummarySection { Header = "模板来源", Lines = { $"来源模板ID: {LoadedTemplateId}" } });
            }
            // 在 BuildSummarySections 方法中
            SummarySections = new ObservableCollection<SummarySection>(sections);
        }

        #region 手动指定管理方法

        /// <summary>
        /// 开始创建手动指定
        /// </summary>
        private void StartCreateManualAssignment()
        {
            NewManualAssignment = new CreateManualAssignmentDto
            {
                Date = StartDate.DateTime.Date,
                IsEnabled = true
            };
            
            // 通知所有包装属性
            OnPropertyChanged(nameof(NewManualAssignmentDate));
            OnPropertyChanged(nameof(NewManualAssignmentPersonnelId));
            OnPropertyChanged(nameof(NewManualAssignmentPositionId));
            OnPropertyChanged(nameof(NewManualAssignmentTimeSlot));
            OnPropertyChanged(nameof(NewManualAssignmentRemarks));
            
            IsCreatingManualAssignment = true;
        }

        /// <summary>
        /// 提交创建手动指定
        /// </summary>
        private async Task SubmitCreateManualAssignmentAsync()
        {
            if (NewManualAssignment == null)
                return;

            // 验证表单
            if (!ValidateManualAssignment(NewManualAssignment, out _))
            {
                // 验证错误已经设置到各个字段的错误属性中，不需要显示对话框
                return;
            }

            // 获取人员和哨位名称
            var personnel = SelectedPersonnels.FirstOrDefault(p => p.Id == NewManualAssignment.PersonnelId);
            var position = SelectedPositions.FirstOrDefault(p => p.Id == NewManualAssignment.PositionId);

            if (personnel == null || position == null)
            {
                await _dialogService.ShowWarningAsync("选择的人员或哨位不存在");
                return;
            }

            // 添加到临时列表
            _manualAssignmentManager.AddTemporary(
                NewManualAssignment,
                personnel.Name,
                position.Name
            );

            // 清空验证错误
            ClearValidationErrors();

            // 先清空 DTO
            NewManualAssignment = null;
            
            // 通知包装属性更新
            OnPropertyChanged(nameof(NewManualAssignmentDate));
            OnPropertyChanged(nameof(NewManualAssignmentPersonnelId));
            OnPropertyChanged(nameof(NewManualAssignmentPositionId));
            OnPropertyChanged(nameof(NewManualAssignmentTimeSlot));
            OnPropertyChanged(nameof(NewManualAssignmentRemarks));

            // 关闭表单
            IsCreatingManualAssignment = false;

            // 通知UI更新
            OnPropertyChanged(nameof(AllManualAssignments));
        }

        /// <summary>
        /// 取消创建手动指定
        /// </summary>
        private void CancelCreateManualAssignment()
        {
            // 先清空 DTO，避免对话框关闭时绑定更新导致 null 引用
            NewManualAssignment = null;
            
            // 通知包装属性更新
            OnPropertyChanged(nameof(NewManualAssignmentDate));
            OnPropertyChanged(nameof(NewManualAssignmentPersonnelId));
            OnPropertyChanged(nameof(NewManualAssignmentPositionId));
            OnPropertyChanged(nameof(NewManualAssignmentTimeSlot));
            OnPropertyChanged(nameof(NewManualAssignmentRemarks));
            
            // 最后关闭对话框
            IsCreatingManualAssignment = false;
        }

        /// <summary>
        /// 开始编辑手动指定
        /// </summary>
        private void StartEditManualAssignment(ManualAssignmentViewModel? assignment)
        {
            if (assignment == null || !assignment.IsTemporary)
                return;

            EditingManualAssignment = assignment;
            EditingManualAssignmentDto = new UpdateManualAssignmentDto
            {
                Date = assignment.Date,
                PersonnelId = assignment.PersonnelId,
                PositionId = assignment.PositionId,
                TimeSlot = assignment.TimeSlot,
                Remarks = assignment.Remarks,
                IsEnabled = assignment.IsEnabled
            };
            
            // 通知所有包装属性
            OnPropertyChanged(nameof(EditingManualAssignmentDate));
            OnPropertyChanged(nameof(EditingManualAssignmentPersonnelId));
            OnPropertyChanged(nameof(EditingManualAssignmentPositionId));
            OnPropertyChanged(nameof(EditingManualAssignmentTimeSlot));
            OnPropertyChanged(nameof(EditingManualAssignmentRemarks));
            
            IsEditingManualAssignment = true;
        }

        /// <summary>
        /// 提交编辑手动指定
        /// </summary>
        private async Task SubmitEditManualAssignmentAsync()
        {
            if (EditingManualAssignment == null || EditingManualAssignmentDto == null)
                return;

            // 验证表单
            if (!ValidateManualAssignment(EditingManualAssignmentDto, out _))
            {
                // 验证错误已经设置到各个字段的错误属性中，不需要显示对话框
                return;
            }

            // 获取人员和哨位名称
            var personnel = SelectedPersonnels.FirstOrDefault(p => p.Id == EditingManualAssignmentDto.PersonnelId);
            var position = SelectedPositions.FirstOrDefault(p => p.Id == EditingManualAssignmentDto.PositionId);

            if (personnel == null || position == null)
            {
                await _dialogService.ShowWarningAsync("选择的人员或哨位不存在");
                return;
            }

            // 更新临时列表
            _manualAssignmentManager.UpdateTemporary(
                EditingManualAssignment.TempId,
                EditingManualAssignmentDto,
                personnel.Name,
                position.Name
            );

            // 清空验证错误
            ClearValidationErrors();

            // 先清空 DTO
            EditingManualAssignment = null;
            EditingManualAssignmentDto = null;
            
            // 通知包装属性更新
            OnPropertyChanged(nameof(EditingManualAssignmentDate));
            OnPropertyChanged(nameof(EditingManualAssignmentPersonnelId));
            OnPropertyChanged(nameof(EditingManualAssignmentPositionId));
            OnPropertyChanged(nameof(EditingManualAssignmentTimeSlot));
            OnPropertyChanged(nameof(EditingManualAssignmentRemarks));

            // 关闭表单
            IsEditingManualAssignment = false;

            // 通知UI更新
            OnPropertyChanged(nameof(AllManualAssignments));
        }

        /// <summary>
        /// 取消编辑手动指定
        /// </summary>
        private void CancelEditManualAssignment()
        {
            // 先清空 DTO，避免对话框关闭时绑定更新导致 null 引用
            EditingManualAssignment = null;
            EditingManualAssignmentDto = null;
            
            // 通知包装属性更新
            OnPropertyChanged(nameof(EditingManualAssignmentDate));
            OnPropertyChanged(nameof(EditingManualAssignmentPersonnelId));
            OnPropertyChanged(nameof(EditingManualAssignmentPositionId));
            OnPropertyChanged(nameof(EditingManualAssignmentTimeSlot));
            OnPropertyChanged(nameof(EditingManualAssignmentRemarks));
            
            // 最后关闭对话框
            IsEditingManualAssignment = false;
        }

        /// <summary>
        /// 删除手动指定
        /// </summary>
        private async Task DeleteManualAssignmentAsync(ManualAssignmentViewModel? assignment)
        {
            if (assignment == null || !assignment.IsTemporary)
                return;

            // 显示确认对话框
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "确认删除",
                Content = "确定要删除此手动指定吗？",
                PrimaryButtonText = "删除",
                SecondaryButtonText = "取消",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Secondary,
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };

            if (dialog.XamlRoot == null) return;

            var result = await dialog.ShowAsync();
            if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                return;

            // 删除临时手动指定
            _manualAssignmentManager.RemoveTemporary(assignment.TempId);

            // 通知UI更新
            OnPropertyChanged(nameof(AllManualAssignments));
        }

        /// <summary>
        /// 验证手动指定表单
        /// </summary>
        private bool ValidateManualAssignment(CreateManualAssignmentDto dto, [NotNullWhen(false)] out string? error)
        {
            // 清空之前的错误
            ClearValidationErrors();

            bool isValid = true;

            // 验证日期范围
            if (dto.Date < StartDate.Date || dto.Date > EndDate.Date)
            {
                DateValidationError = "日期必须在排班开始日期和结束日期之间";
                isValid = false;
            }

            // 验证人员
            if (dto.PersonnelId <= 0)
            {
                PersonnelValidationError = "请选择人员";
                isValid = false;
            }
            else if (!SelectedPersonnels.Any(p => p.Id == dto.PersonnelId))
            {
                PersonnelValidationError = "选择的人员不在已选人员列表中";
                isValid = false;
            }

            // 验证哨位
            if (dto.PositionId <= 0)
            {
                PositionValidationError = "请选择哨位";
                isValid = false;
            }
            else if (!SelectedPositions.Any(p => p.Id == dto.PositionId))
            {
                PositionValidationError = "选择的哨位不在已选哨位列表中";
                isValid = false;
            }

            // 验证时段
            if (dto.TimeSlot < 0 || dto.TimeSlot > 11)
            {
                TimeSlotValidationError = "请选择时段";
                isValid = false;
            }

            error = isValid ? null : "请修正表单中的错误";
            return isValid;
        }

        /// <summary>
        /// 清空验证错误消息
        /// </summary>
        private void ClearValidationErrors()
        {
            DateValidationError = string.Empty;
            PersonnelValidationError = string.Empty;
            PositionValidationError = string.Empty;
            TimeSlotValidationError = string.Empty;
        }

        /// <summary>
        /// 验证手动指定表单（UpdateDto版本）
        /// </summary>
        private bool ValidateManualAssignment(UpdateManualAssignmentDto dto, [NotNullWhen(false)] out string? error)
        {
            // 清空之前的错误
            ClearValidationErrors();

            bool isValid = true;

            // 验证日期范围
            if (dto.Date < StartDate.Date || dto.Date > EndDate.Date)
            {
                DateValidationError = "日期必须在排班开始日期和结束日期之间";
                isValid = false;
            }

            // 验证人员
            if (dto.PersonnelId <= 0)
            {
                PersonnelValidationError = "请选择人员";
                isValid = false;
            }
            else if (!SelectedPersonnels.Any(p => p.Id == dto.PersonnelId))
            {
                PersonnelValidationError = "选择的人员不在已选人员列表中";
                isValid = false;
            }

            // 验证哨位
            if (dto.PositionId <= 0)
            {
                PositionValidationError = "请选择哨位";
                isValid = false;
            }
            else if (!SelectedPositions.Any(p => p.Id == dto.PositionId))
            {
                PositionValidationError = "选择的哨位不在已选哨位列表中";
                isValid = false;
            }

            // 验证时段
            if (dto.TimeSlot < 0 || dto.TimeSlot > 11)
            {
                TimeSlotValidationError = "请选择时段";
                isValid = false;
            }

            error = isValid ? null : "请修正表单中的错误";
            return isValid;
        }

        #endregion

        #region 为哨位添加人员方法

        /// <summary>
        /// 开始为哨位添加人员
        /// </summary>
        /// <param name="position">要添加人员的哨位</param>
        private void StartAddPersonnelToPosition(PositionDto? position)
        {
            try
            {
                if (position == null)
                {
                    System.Diagnostics.Debug.WriteLine("错误: StartAddPersonnelToPosition - position is null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"=== 开始为哨位添加人员 ===");
                System.Diagnostics.Debug.WriteLine($"哨位: {position.Name} (ID: {position.Id})");

                // 设置当前正在编辑的哨位
                CurrentEditingPosition = position;

                // 从AvailablePersonnels中筛选可添加的人员
                // 可以添加所有可用人员
                if (AvailablePersonnels == null)
                {
                    System.Diagnostics.Debug.WriteLine("错误: AvailablePersonnels为null");
                    _ = _dialogService.ShowErrorAsync("无法加载可用人员列表");
                    return;
                }

                var availableToAdd = AvailablePersonnels.ToList();

                System.Diagnostics.Debug.WriteLine($"可添加的人员数量: {availableToAdd.Count}");

                // 设置可添加到当前哨位的人员列表
                AvailablePersonnelForPosition = new ObservableCollection<PersonnelDto>(availableToAdd);

                // 设置标志为true，显示添加人员对话框
                IsAddingPersonnelToPosition = true;

                System.Diagnostics.Debug.WriteLine("添加人员对话框已打开");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 开始为哨位添加人员失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                _ = _dialogService.ShowErrorAsync("无法打开添加人员对话框", ex);
            }
        }

        /// <summary>
        /// 提交添加人员到哨位
        /// </summary>
        private async Task SubmitAddPersonnelToPositionAsync()
        {
            try
            {
                if (CurrentEditingPosition == null)
                {
                    System.Diagnostics.Debug.WriteLine("错误: SubmitAddPersonnelToPositionAsync - CurrentEditingPosition is null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"=== 提交添加人员到哨位 ===");
                System.Diagnostics.Debug.WriteLine($"哨位: {CurrentEditingPosition.Name} (ID: {CurrentEditingPosition.Id})");

                // 验证选择的人员
                if (SelectedPersonnelIdsForPosition.Count == 0)
                {
                    await _dialogService.ShowWarningAsync("请至少选择一名人员");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"选择的人员数量: {SelectedPersonnelIdsForPosition.Count}");

                // 调用_positionPersonnelManager.AddPersonnelTemporarily添加人员
                foreach (var personnelId in SelectedPersonnelIdsForPosition)
                {
                    var personnel = AvailablePersonnelForPosition.FirstOrDefault(p => p.Id == personnelId);
                    if (personnel != null)
                    {
                        _positionPersonnelManager.AddPersonnelTemporarily(CurrentEditingPosition.Id, personnelId);
                        System.Diagnostics.Debug.WriteLine($"临时添加人员: {personnel.Name} (ID: {personnelId}) 到哨位 {CurrentEditingPosition.Name}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"警告: 未找到人员 ID: {personnelId}");
                    }
                }

                // 更新UI显示
                // 这里可以触发UI刷新，显示临时添加的人员
                OnPropertyChanged(nameof(SelectedPositions));

                // 清空当前编辑的哨位和选择
                CurrentEditingPosition = null;
                AvailablePersonnelForPosition.Clear();
                SelectedPersonnelIdsForPosition.Clear();

                // 设置标志为false，关闭对话框
                IsAddingPersonnelToPosition = false;

                System.Diagnostics.Debug.WriteLine("人员添加完成，对话框已关闭");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 提交添加人员到哨位失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                await _dialogService.ShowErrorAsync("添加人员失败", ex);
                
                // 确保对话框关闭
                IsAddingPersonnelToPosition = false;
            }
        }

        /// <summary>
        /// 取消添加人员到哨位
        /// </summary>
        private void CancelAddPersonnelToPosition()
        {
            System.Diagnostics.Debug.WriteLine("取消添加人员到哨位");

            // 清空当前编辑的哨位和选择
            CurrentEditingPosition = null;
            AvailablePersonnelForPosition.Clear();
            SelectedPersonnelIdsForPosition.Clear();

            // 设置标志为false，关闭对话框
            IsAddingPersonnelToPosition = false;
        }

        /// <summary>
        /// 临时移除人员从哨位
        /// </summary>
        /// <param name="parameters">哨位ID和人员ID的元组</param>
        private void RemovePersonnelFromPosition((int positionId, int personnelId) parameters)
        {
            try
            {
                var (positionId, personnelId) = parameters;

                System.Diagnostics.Debug.WriteLine($"=== 临时移除人员从哨位 ===");
                System.Diagnostics.Debug.WriteLine($"哨位ID: {positionId}, 人员ID: {personnelId}");

                // 调用_positionPersonnelManager.RemovePersonnelTemporarily
                _positionPersonnelManager.RemovePersonnelTemporarily(positionId, personnelId);

                // 从缓存中获取哨位和人员名称用于日志
                var position = GetPositionFromCache(positionId);
                var personnel = GetPersonnelFromCache(personnelId);

                if (position != null && personnel != null)
                {
                    System.Diagnostics.Debug.WriteLine($"已临时移除人员: {personnel.Name} 从哨位 {position.Name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"警告: 未找到哨位或人员信息 (哨位ID: {positionId}, 人员ID: {personnelId})");
                }

                // 更新UI显示
                // 触发UI刷新，显示临时移除的人员
                OnPropertyChanged(nameof(SelectedPositions));

                System.Diagnostics.Debug.WriteLine("人员移除完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 临时移除人员失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                _ = _dialogService.ShowErrorAsync("移除人员失败", ex);
            }
        }

        /// <summary>
        /// 撤销哨位的临时更改
        /// </summary>
        /// <param name="positionId">哨位ID</param>
        private void RevertPositionChanges(int positionId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== 撤销哨位的临时更改 ===");
                System.Diagnostics.Debug.WriteLine($"哨位ID: {positionId}");

                // 获取哨位名称用于日志
                var position = SelectedPositions.FirstOrDefault(p => p.Id == positionId);
                if (position != null)
                {
                    System.Diagnostics.Debug.WriteLine($"哨位: {position.Name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"警告: 未找到哨位 ID: {positionId}");
                }

                // 调用_positionPersonnelManager.RevertChanges
                _positionPersonnelManager.RevertChanges(positionId);

                // 更新UI显示
                // 触发UI刷新，恢复到原始状态
                OnPropertyChanged(nameof(SelectedPositions));

                System.Diagnostics.Debug.WriteLine("临时更改已撤销");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 撤销哨位临时更改失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                _ = _dialogService.ShowErrorAsync("撤销更改失败", ex);
            }
        }

        /// <summary>
        /// 显示保存确认对话框
        /// </summary>
        /// <param name="changes">哨位人员更改记录</param>
        /// <returns>用户是否确认保存</returns>
        private async Task<bool> ShowSaveConfirmationDialog(PositionPersonnelChanges changes)
        {
            System.Diagnostics.Debug.WriteLine($"=== 显示保存确认对话框 ===");
            System.Diagnostics.Debug.WriteLine($"哨位ID: {changes.PositionId}, 哨位名称: {changes.PositionName}");

            // 构建对话框内容
            var contentPanel = new Microsoft.UI.Xaml.Controls.StackPanel
            {
                Spacing = 12
            };

            // 警告图标和文本
            var warningPanel = new Microsoft.UI.Xaml.Controls.StackPanel
            {
                Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                Spacing = 8
            };
            warningPanel.Children.Add(new Microsoft.UI.Xaml.Controls.SymbolIcon
            {
                Symbol = Microsoft.UI.Xaml.Controls.Symbol.Important,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange)
            });
            warningPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = "以下更改将永久保存到数据库:",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            contentPanel.Children.Add(warningPanel);

            // 哨位信息
            contentPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = $"哨位: {changes.PositionName}",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0)
            });

            // 添加的人员
            if (changes.AddedPersonnelIds.Any())
            {
                contentPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Text = "添加人员:",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 4)
                });

                foreach (var personnelId in changes.AddedPersonnelIds)
                {
                    var personnel = GetPersonnelFromCache(personnelId);
                    var personnelName = personnel?.Name ?? $"人员ID:{personnelId}";
                    
                    var personnelPanel = new Microsoft.UI.Xaml.Controls.StackPanel
                    {
                        Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                        Spacing = 8,
                        Margin = new Microsoft.UI.Xaml.Thickness(16, 0, 0, 4)
                    };
                    personnelPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = "➕",
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green)
                    });
                    personnelPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = personnelName
                    });
                    contentPanel.Children.Add(personnelPanel);
                }
            }

            // 移除的人员
            if (changes.RemovedPersonnelIds.Any())
            {
                contentPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Text = "移除人员:",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 4)
                });

                foreach (var personnelId in changes.RemovedPersonnelIds)
                {
                    var personnel = GetPersonnelFromCache(personnelId);
                    var personnelName = personnel?.Name ?? $"人员ID:{personnelId}";
                    
                    var personnelPanel = new Microsoft.UI.Xaml.Controls.StackPanel
                    {
                        Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                        Spacing = 8,
                        Margin = new Microsoft.UI.Xaml.Thickness(16, 0, 0, 4)
                    };
                    personnelPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = "➖",
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red)
                    });
                    personnelPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = personnelName
                    });
                    contentPanel.Children.Add(personnelPanel);
                }
            }

            // 说明文本
            contentPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = "这些更改将影响后续所有排班。",
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Microsoft.UI.Xaml.Thickness(0, 12, 0, 0)
            });

            // 创建确认对话框
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "确认保存为永久",
                Content = contentPanel,
                PrimaryButtonText = "确认保存",
                SecondaryButtonText = "取消",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Secondary,
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };

            if (dialog.XamlRoot == null) return false;

            try
            {
                var result = await dialog.ShowAsync();
                var confirmed = result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary;
                
                System.Diagnostics.Debug.WriteLine($"用户选择: {(confirmed ? "确认保存" : "取消")}");
                
                return confirmed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示确认对话框失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存哨位的临时更改为永久
        /// </summary>
        /// <param name="positionId">哨位ID</param>
        private async Task SavePositionChangesAsync(int positionId)
        {
            System.Diagnostics.Debug.WriteLine($"=== 开始保存哨位更改为永久 ===");
            System.Diagnostics.Debug.WriteLine($"哨位ID: {positionId}");

            try
            {
                // 获取哨位的临时更改
                var changes = _positionPersonnelManager.GetChanges(positionId);
                
                if (!changes.HasChanges)
                {
                    System.Diagnostics.Debug.WriteLine("没有需要保存的更改");
                    await _dialogService.ShowWarningAsync("没有需要保存的更改");
                    return;
                }

                // 获取哨位
                var position = SelectedPositions.FirstOrDefault(p => p.Id == positionId);
                if (position == null)
                {
                    System.Diagnostics.Debug.WriteLine($"哨位不存在: ID={positionId}");
                    await _dialogService.ShowErrorAsync("哨位不存在");
                    return;
                }

                // 设置哨位名称到changes对象中（用于对话框显示）
                changes.PositionName = position.Name;

                System.Diagnostics.Debug.WriteLine($"哨位: {position.Name}");
                System.Diagnostics.Debug.WriteLine($"添加人员数: {changes.AddedPersonnelIds.Count}");
                System.Diagnostics.Debug.WriteLine($"移除人员数: {changes.RemovedPersonnelIds.Count}");

                // 显示确认对话框
                var confirmed = await ShowSaveConfirmationDialog(changes);
                if (!confirmed)
                {
                    System.Diagnostics.Debug.WriteLine("用户取消保存");
                    return;
                }

                // 获取更新后的可用人员列表
                var updatedPersonnelIds = _positionPersonnelManager.GetAvailablePersonnel(positionId);
                
                System.Diagnostics.Debug.WriteLine($"更新后的可用人员数: {updatedPersonnelIds.Count}");

                // 创建更新DTO
                var updateDto = new UpdatePositionDto
                {
                    Name = position.Name,
                    Location = position.Location,
                    Description = position.Description,
                    Requirements = position.Requirements,
                    RequiredSkillIds = position.RequiredSkillIds,
                    AvailablePersonnelIds = updatedPersonnelIds.ToList()
                };

                // 调用IPositionService.UpdateAsync更新数据库
                System.Diagnostics.Debug.WriteLine("开始调用IPositionService.UpdateAsync");
                await _positionService.UpdateAsync(positionId, updateDto);
                System.Diagnostics.Debug.WriteLine("数据库更新成功");

                // 更新本地PositionDto数据
                position.AvailablePersonnelIds = updatedPersonnelIds.ToList();
                System.Diagnostics.Debug.WriteLine("本地数据已更新");

                // 重新初始化PositionPersonnelManager（清除临时更改标记）
                _positionPersonnelManager.Initialize(SelectedPositions);
                System.Diagnostics.Debug.WriteLine("PositionPersonnelManager已重新初始化");

                // 更新UI显示
                OnPropertyChanged(nameof(SelectedPositions));

                // 显示成功提示
                await _dialogService.ShowSuccessAsync($"哨位 '{position.Name}' 的更改已保存");
                
                System.Diagnostics.Debug.WriteLine("=== 保存哨位更改完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 保存哨位更改失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                await _dialogService.ShowErrorAsync("保存失败", ex);
            }
        }

        #endregion

        #region 手动添加参与人员方法

        /// <summary>
        /// 开始手动添加参与人员（不属于任何哨位）
        /// </summary>
        private void StartManualAddPersonnel()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 开始手动添加参与人员 ===");

                // 验证输入
                if (SelectedPositions == null || AvailablePersonnels == null)
                {
                    System.Diagnostics.Debug.WriteLine("错误: SelectedPositions或AvailablePersonnels为null");
                    _ = _dialogService.ShowErrorAsync("无法加载人员列表");
                    return;
                }

                // 从AvailablePersonnels中筛选不在任何哨位可用人员列表中的人员
                var autoExtractedPersonnelIds = SelectedPositions
                    .SelectMany(p => _positionPersonnelManager.GetAvailablePersonnel(p.Id))
                    .Distinct()
                    .ToHashSet();

                System.Diagnostics.Debug.WriteLine($"自动提取的人员ID数量: {autoExtractedPersonnelIds.Count}");

                // 可以手动添加的人员：不在任何哨位可用人员列表中的人员
                var availableForManualAdd = AvailablePersonnels
                    .Where(p => !autoExtractedPersonnelIds.Contains(p.Id))
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"可手动添加的人员数量: {availableForManualAdd.Count}");

                // 设置可选人员列表
                AvailablePersonnelForManualAdd = new ObservableCollection<PersonnelDto>(availableForManualAdd);

                // 清空之前的选择
                SelectedPersonnelIdsForManualAdd.Clear();

                // 显示手动添加人员对话框
                IsManualAddingPersonnel = true;

                System.Diagnostics.Debug.WriteLine("手动添加人员对话框已打开");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 开始手动添加参与人员失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                _ = _dialogService.ShowErrorAsync("无法打开添加人员对话框", ex);
            }
        }

        /// <summary>
        /// 提交手动添加参与人员
        /// </summary>
        private async Task SubmitManualAddPersonnelAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 提交手动添加参与人员 ===");

                // 验证选择的人员
                if (SelectedPersonnelIdsForManualAdd.Count == 0)
                {
                    await _dialogService.ShowWarningAsync("请至少选择一名人员");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"选择的人员数量: {SelectedPersonnelIdsForManualAdd.Count}");

                // 添加到ManuallyAddedPersonnelIds列表和详细信息列表
                foreach (var personnelId in SelectedPersonnelIdsForManualAdd)
                {
                    // 避免重复添加
                    if (!ManuallyAddedPersonnelIds.Contains(personnelId))
                    {
                        ManuallyAddedPersonnelIds.Add(personnelId);
                        
                        var personnel = AvailablePersonnelForManualAdd.FirstOrDefault(p => p.Id == personnelId);
                        if (personnel != null)
                        {
                            // 添加到详细信息列表
                            ManuallyAddedPersonnelDetails.Add(personnel);
                            
                            // 添加到SelectedPersonnels（如果不存在）
                            if (!SelectedPersonnels.Any(p => p.Id == personnelId))
                            {
                                SelectedPersonnels.Add(personnel);
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"手动添加人员: {personnel.Name} (ID: {personnelId})");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"警告: 未找到人员 ID: {personnelId}");
                        }
                    }
                }

                // 更新ManuallyAddedPersonnelCount
                ManuallyAddedPersonnelCount = ManuallyAddedPersonnelIds.Count;

                System.Diagnostics.Debug.WriteLine($"手动添加人员总数: {ManuallyAddedPersonnelCount}");

                // 清空选择和可选列表
                SelectedPersonnelIdsForManualAdd.Clear();
                AvailablePersonnelForManualAdd.Clear();

                // 关闭对话框
                IsManualAddingPersonnel = false;

                System.Diagnostics.Debug.WriteLine("手动添加人员完成，对话框已关闭");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 提交手动添加参与人员失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                await _dialogService.ShowErrorAsync("添加人员失败", ex);
                
                // 确保对话框关闭
                IsManualAddingPersonnel = false;
            }
        }

        /// <summary>
        /// 取消手动添加参与人员
        /// </summary>
        private void CancelManualAddPersonnel()
        {
            System.Diagnostics.Debug.WriteLine("取消手动添加参与人员");

            // 清空选择和可选列表
            SelectedPersonnelIdsForManualAdd.Clear();
            AvailablePersonnelForManualAdd.Clear();

            // 关闭对话框
            IsManualAddingPersonnel = false;
        }

        /// <summary>
        /// 移除手动添加的人员
        /// </summary>
        /// <param name="personnelId">人员ID</param>
        private void RemoveManualPersonnel(int personnelId)
        {
            System.Diagnostics.Debug.WriteLine($"=== 移除手动添加的人员 ===");
            System.Diagnostics.Debug.WriteLine($"人员ID: {personnelId}");

            // 从缓存中获取人员名称用于日志
            var personnel = GetPersonnelFromCache(personnelId);
            if (personnel != null)
            {
                System.Diagnostics.Debug.WriteLine($"人员: {personnel.Name}");
            }

            // 从ManuallyAddedPersonnelIds列表中移除
            if (ManuallyAddedPersonnelIds.Contains(personnelId))
            {
                ManuallyAddedPersonnelIds.Remove(personnelId);
                System.Diagnostics.Debug.WriteLine("人员已从手动添加列表中移除");
            }

            // 从详细信息列表中移除
            var detailToRemove = ManuallyAddedPersonnelDetails.FirstOrDefault(p => p.Id == personnelId);
            if (detailToRemove != null)
            {
                ManuallyAddedPersonnelDetails.Remove(detailToRemove);
                System.Diagnostics.Debug.WriteLine("人员已从详细信息列表中移除");
            }

            // 从SelectedPersonnels中移除（如果该人员不在任何哨位的可用人员列表中）
            var isInAnyPosition = SelectedPositions.Any(pos => 
                _positionPersonnelManager.GetAvailablePersonnel(pos.Id).Contains(personnelId));
            
            if (!isInAnyPosition)
            {
                var selectedToRemove = SelectedPersonnels.FirstOrDefault(p => p.Id == personnelId);
                if (selectedToRemove != null)
                {
                    SelectedPersonnels.Remove(selectedToRemove);
                    System.Diagnostics.Debug.WriteLine("人员已从选中列表中移除");
                }
            }

            // 更新ManuallyAddedPersonnelCount
            ManuallyAddedPersonnelCount = ManuallyAddedPersonnelIds.Count;

            System.Diagnostics.Debug.WriteLine($"手动添加人员总数: {ManuallyAddedPersonnelCount}");
            System.Diagnostics.Debug.WriteLine("=== 移除完成 ===");
        }

        #endregion

        #region 草稿保存和恢复

        /// <summary>
        /// 创建草稿（保存当前状态）
        /// </summary>
        public async Task CreateDraftAsync()
        {
            if (_draftService == null)
            {
                System.Diagnostics.Debug.WriteLine("草稿服务未初始化，跳过保存");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("=== 开始保存草稿 ===");

                // 收集所有哨位的临时更改
                var positionPersonnelChanges = new Dictionary<int, PositionPersonnelChangeDto>();
                var positionsWithChanges = _positionPersonnelManager.GetPositionsWithChanges();
                
                foreach (var positionId in positionsWithChanges)
                {
                    var changes = _positionPersonnelManager.GetChanges(positionId);
                    positionPersonnelChanges[positionId] = new PositionPersonnelChangeDto
                    {
                        PositionId = changes.PositionId,
                        AddedPersonnelIds = new List<int>(changes.AddedPersonnelIds),
                        RemovedPersonnelIds = new List<int>(changes.RemovedPersonnelIds)
                    };
                }

                System.Diagnostics.Debug.WriteLine($"保存 {positionPersonnelChanges.Count} 个哨位的临时更改");
                System.Diagnostics.Debug.WriteLine($"保存 {ManuallyAddedPersonnelIds.Count} 个手动添加的人员");

                // 获取所有启用的手动指定
                var allEnabledAssignments = _manualAssignmentManager.AllAssignments
                    .Where(a => a.IsEnabled)
                    .ToList();
                
                var enabledManualAssignmentIds = allEnabledAssignments
                    .Where(a => a.Id.HasValue)
                    .Select(a => a.Id.Value)
                    .ToList();
                
                var temporaryManualAssignments = allEnabledAssignments
                    .Where(a => !a.Id.HasValue)
                    .ToList();

                // 创建草稿DTO
                var draft = new SchedulingDraftDto
                {
                    ScheduleTitle = ScheduleTitle,
                    StartDate = StartDate.DateTime,
                    EndDate = EndDate.DateTime,
                    CurrentStep = CurrentStep,
                    TemplateApplied = TemplateApplied,
                    LoadedTemplateId = LoadedTemplateId,
                    SelectedPersonnelIds = SelectedPersonnels.Select(p => p.Id).ToList(),
                    SelectedPositionIds = SelectedPositions.Select(p => p.Id).ToList(),
                    UseActiveHolidayConfig = UseActiveHolidayConfig,
                    SelectedHolidayConfigId = SelectedHolidayConfigId,
                    EnabledFixedRuleIds = FixedPositionRules.Where(r => r.IsEnabled).Select(r => r.Id).ToList(),
                    EnabledManualAssignmentIds = enabledManualAssignmentIds,
                    TemporaryManualAssignments = temporaryManualAssignments
                         .Select(vm => new ManualAssignmentDraftDto
                         {
                             Date = vm.Date,
                             PersonnelId = vm.PersonnelId,
                             PositionId = vm.PositionId,
                             TimeSlot = vm.TimeSlot,
                             Remarks = vm.Remarks,
                             IsEnabled = vm.IsEnabled,
                             TempId = vm.TempId
                         })
                         .ToList(),
                    PositionPersonnelChanges = positionPersonnelChanges,
                    ManuallyAddedPersonnelIds = new List<int>(ManuallyAddedPersonnelIds),
                    SavedAt = DateTime.Now,
                    Version = "1.0"
                };

                await _draftService.SaveDraftAsync(draft);
                System.Diagnostics.Debug.WriteLine("草稿保存成功");
                System.Diagnostics.Debug.WriteLine("=== 草稿保存完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存草稿失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                // 不抛出异常，避免影响主流程
            }
        }

        /// <summary>
        /// 从草稿恢复状态
        /// </summary>
        public async Task<bool> RestoreFromDraftAsync()
        {
            if (_draftService == null)
            {
                System.Diagnostics.Debug.WriteLine("草稿服务未初始化，跳过恢复");
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("=== 开始恢复草稿 ===");

                var draft = await _draftService.LoadDraftAsync();
                if (draft == null)
                {
                    System.Diagnostics.Debug.WriteLine("没有找到草稿");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"找到草稿: {draft.ScheduleTitle}");
                System.Diagnostics.Debug.WriteLine($"草稿保存时间: {draft.SavedAt}");
                System.Diagnostics.Debug.WriteLine($"草稿步骤: {draft.CurrentStep}");

                // 加载基础数据（如果还没有加载）
                if (AvailablePersonnels.Count == 0 || AvailablePositions.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("加载基础数据");
                    await LoadInitialDataAsync();
                }

                // 恢复基本信息
                ScheduleTitle = draft.ScheduleTitle;
                StartDate = new DateTimeOffset(draft.StartDate);
                EndDate = new DateTimeOffset(draft.EndDate);
                TemplateApplied = draft.TemplateApplied;
                LoadedTemplateId = draft.LoadedTemplateId;
                UseActiveHolidayConfig = draft.UseActiveHolidayConfig;
                SelectedHolidayConfigId = draft.SelectedHolidayConfigId;

                // 恢复选中的人员和哨位
                var selectedPers = AvailablePersonnels.Where(p => draft.SelectedPersonnelIds.Contains(p.Id)).ToList();
                var selectedPos = AvailablePositions.Where(p => draft.SelectedPositionIds.Contains(p.Id)).ToList();
                
                SelectedPersonnels = new ObservableCollection<PersonnelDto>(selectedPers);
                SelectedPositions = new ObservableCollection<PositionDto>(selectedPos);

                System.Diagnostics.Debug.WriteLine($"恢复人员: {SelectedPersonnels.Count}, 哨位: {SelectedPositions.Count}");

                // 恢复约束数据（如果需要）
                if (draft.CurrentStep >= 4)
                {
                    System.Diagnostics.Debug.WriteLine("加载约束数据");
                    await LoadConstraintsAsync();

                    // 恢复约束启用状态
                    foreach (var rule in FixedPositionRules)
                    {
                        rule.IsEnabled = draft.EnabledFixedRuleIds.Contains(rule.Id);
                    }

                    // 恢复手动指定
                    _manualAssignmentManager.Clear();
                    
                    // 加载已保存的手动指定（使用缓存提高性能）
                    var savedManualAssignments = ManualAssignments
                        .Where(m => draft.EnabledManualAssignmentIds.Contains(m.Id))
                        .Select(m =>
                        {
                            var personnel = GetPersonnelFromCache(m.PersonalId);
                            var position = GetPositionFromCache(m.PositionId);
                            
                            return new ManualAssignmentDto
                            {
                                Id = m.Id,
                                Date = m.Date,
                                PersonnelId = m.PersonalId,
                                PersonnelName = personnel?.Name ?? "未知人员",
                                PositionId = m.PositionId,
                                PositionName = position?.Name ?? "未知哨位",
                                TimeSlot = m.PeriodIndex,
                                Remarks = m.Remarks,
                                IsEnabled = true,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
                            };
                        })
                        .ToList();
                    
                    _manualAssignmentManager.LoadSaved(savedManualAssignments);
                    
                    // 恢复临时手动指定
                    foreach (var tempAssignment in draft.TemporaryManualAssignments)
                    {
                        var personnel = GetPersonnelFromCache(tempAssignment.PersonnelId);
                        var position = GetPositionFromCache(tempAssignment.PositionId);
                        
                        var dto = new CreateManualAssignmentDto
                        {
                            Date = tempAssignment.Date,
                            PersonnelId = tempAssignment.PersonnelId,
                            PositionId = tempAssignment.PositionId,
                            TimeSlot = tempAssignment.TimeSlot,
                            Remarks = tempAssignment.Remarks,
                            IsEnabled = tempAssignment.IsEnabled
                        };
                        
                        _manualAssignmentManager.AddTemporary(dto, personnel?.Name ?? "未知人员", position?.Name ?? "未知哨位", tempAssignment.TempId);
                    }

                    System.Diagnostics.Debug.WriteLine($"恢复手动指定: 已保存 {savedManualAssignments.Count}, 临时 {draft.TemporaryManualAssignments.Count}");
                }

                // 恢复PositionPersonnelManager状态
                if (draft.PositionPersonnelChanges.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"恢复 {draft.PositionPersonnelChanges.Count} 个哨位的临时更改");
                    
                    // 初始化PositionPersonnelManager
                    _positionPersonnelManager.Initialize(SelectedPositions);
                    
                    // 恢复每个哨位的临时更改
                    foreach (var kvp in draft.PositionPersonnelChanges)
                    {
                        var positionId = kvp.Key;
                        var changes = kvp.Value;
                        
                        // 添加临时添加的人员
                        foreach (var personnelId in changes.AddedPersonnelIds)
                        {
                            _positionPersonnelManager.AddPersonnelTemporarily(positionId, personnelId);
                        }
                        
                        // 移除临时移除的人员
                        foreach (var personnelId in changes.RemovedPersonnelIds)
                        {
                            _positionPersonnelManager.RemovePersonnelTemporarily(positionId, personnelId);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"哨位 {positionId}: 添加 {changes.AddedPersonnelIds.Count}, 移除 {changes.RemovedPersonnelIds.Count}");
                    }
                }

                // 恢复手动添加的人员列表
                ManuallyAddedPersonnelIds.Clear();
                foreach (var personnelId in draft.ManuallyAddedPersonnelIds)
                {
                    ManuallyAddedPersonnelIds.Add(personnelId);
                }
                ManuallyAddedPersonnelCount = ManuallyAddedPersonnelIds.Count;

                System.Diagnostics.Debug.WriteLine($"恢复手动添加的人员: {ManuallyAddedPersonnelCount}");

                // 恢复步骤（最后设置，因为会触发OnCurrentStepChanged）
                CurrentStep = draft.CurrentStep;

                System.Diagnostics.Debug.WriteLine("草稿恢复成功");
                System.Diagnostics.Debug.WriteLine("=== 草稿恢复完成 ===");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"恢复草稿失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                await _dialogService.ShowErrorAsync("恢复草稿失败", ex);
                return false;
            }
        }

        #endregion

        /// <summary>
        /// 从已选哨位自动提取可用人员
        /// </summary>
        private void ExtractPersonnelFromPositions()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            System.Diagnostics.Debug.WriteLine("=== 开始自动提取人员 ===");
            System.Diagnostics.Debug.WriteLine($"已选哨位数量: {SelectedPositions.Count}");

            try
            {
                // 验证输入
                if (SelectedPositions == null)
                {
                    System.Diagnostics.Debug.WriteLine("错误: SelectedPositions为null");
                    AutoExtractedPersonnelCount = 0;
                    ManuallyAddedPersonnelCount = ManuallyAddedPersonnelIds.Count;
                    return;
                }

                // 初始化PositionPersonnelManager
                _positionPersonnelManager.Initialize(SelectedPositions);
                var initTime = stopwatch.ElapsedMilliseconds;
                System.Diagnostics.Debug.WriteLine($"PositionPersonnelManager初始化耗时: {initTime}ms");

                // 使用HashSet提高查找性能，避免重复计算
                var autoExtractedPersonnelIds = new HashSet<int>();
                foreach (var position in SelectedPositions)
                {
                    if (position.AvailablePersonnelIds == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"警告: 哨位 {position.Name} (ID: {position.Id}) 的AvailablePersonnelIds为null");
                        continue;
                    }

                    foreach (var personnelId in position.AvailablePersonnelIds)
                    {
                        autoExtractedPersonnelIds.Add(personnelId);
                    }
                }
                var extractTime = stopwatch.ElapsedMilliseconds - initTime;
                System.Diagnostics.Debug.WriteLine($"人员ID提取耗时: {extractTime}ms");
                System.Diagnostics.Debug.WriteLine($"自动提取的人员ID数量: {autoExtractedPersonnelIds.Count}");

                // 更新自动提取的人员数量
                AutoExtractedPersonnelCount = autoExtractedPersonnelIds.Count;

                // 更新手动添加的人员数量
                ManuallyAddedPersonnelCount = ManuallyAddedPersonnelIds.Count;

                // 更新哨位人员视图模型
                UpdatePositionPersonnelViewModels();

                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine($"自动提取人员数量: {AutoExtractedPersonnelCount}");
                System.Diagnostics.Debug.WriteLine($"手动添加人员数量: {ManuallyAddedPersonnelCount}");
                System.Diagnostics.Debug.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds}ms");
                
                // 性能警告
                if (stopwatch.ElapsedMilliseconds > 500 && SelectedPositions.Count <= 50)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ 性能警告: 50个以下哨位的人员提取超过500ms");
                }
                else if (stopwatch.ElapsedMilliseconds > 2000 && SelectedPositions.Count > 50)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ 性能警告: 50个以上哨位的人员提取超过2秒");
                }
                
                System.Diagnostics.Debug.WriteLine("=== 人员提取完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 自动提取人员失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                // 设置默认值，避免UI显示异常
                AutoExtractedPersonnelCount = 0;
                ManuallyAddedPersonnelCount = ManuallyAddedPersonnelIds.Count;
                
                // 显示错误提示给用户
                _ = _dialogService.ShowErrorAsync("自动提取人员失败", ex);
            }
        }

        /// <summary>
        /// 更新哨位人员视图模型（用于步骤3的UI展示）
        /// </summary>
        private void UpdatePositionPersonnelViewModels()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            System.Diagnostics.Debug.WriteLine("=== 开始更新哨位人员视图模型 ===");

            try
            {
                // 清空现有的视图模型
                PositionPersonnelViewModels.Clear();

                // 为每个选中的哨位创建视图模型
                foreach (var position in SelectedPositions)
                {
                    // 获取该哨位的可用人员ID（包含临时更改）
                    var availablePersonnelIds = _positionPersonnelManager.GetAvailablePersonnel(position.Id);
                    
                    // 创建人员项视图模型列表
                    var personnelItems = new ObservableCollection<PersonnelItemViewModel>();
                    
                    // 获取哨位的临时更改状态（在循环外获取一次）
                    var positionChanges = _positionPersonnelManager.GetChanges(position.Id);
                    
                    foreach (var personnelId in availablePersonnelIds)
                    {
                        var personnel = GetPersonnelFromCache(personnelId);
                        if (personnel == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"警告: 未找到人员 ID: {personnelId}");
                            continue;
                        }

                        // 判断人员来源类型
                        var sourceType = PersonnelSourceType.AutoExtracted;
                        
                        if (positionChanges.AddedPersonnelIds.Contains(personnelId))
                        {
                            sourceType = PersonnelSourceType.TemporarilyAdded;
                        }
                        else if (positionChanges.RemovedPersonnelIds.Contains(personnelId))
                        {
                            // 注意：如果人员被临时移除，它不应该出现在availablePersonnelIds中
                            // 这里只是为了完整性而保留此逻辑
                            sourceType = PersonnelSourceType.TemporarilyRemoved;
                        }

                        // 判断是否为共享人员（在多个哨位中可用）
                        var sharedPositionCount = SelectedPositions.Count(p => 
                            _positionPersonnelManager.GetAvailablePersonnel(p.Id).Contains(personnelId));
                        var isShared = sharedPositionCount > 1;

                        // 格式化技能显示
                        var skillsDisplay = personnel.SkillIds != null && personnel.SkillIds.Count > 0
                            ? $"技能: {personnel.SkillIds.Count} 项"
                            : "无技能";

                        // 创建人员项视图模型
                        var personnelItem = new PersonnelItemViewModel
                        {
                            PersonnelId = personnelId,
                            PersonnelName = personnel.Name,
                            SkillsDisplay = skillsDisplay,
                            SourceType = sourceType,
                            IsShared = isShared,
                            SharedPositionCount = sharedPositionCount,
                            // 自动提取的人员默认选中，其他类型根据SelectedPersonnels判断
                            IsSelected = sourceType == PersonnelSourceType.AutoExtracted || SelectedPersonnels.Any(p => p.Id == personnelId)
                        };

                        // 如果人员被选中但不在SelectedPersonnels中，添加它
                        if (personnelItem.IsSelected && !SelectedPersonnels.Any(p => p.Id == personnelId))
                        {
                            SelectedPersonnels.Add(personnel);
                            System.Diagnostics.Debug.WriteLine($"自动添加选中人员到列表: {personnel.Name}");
                        }

                        // 监听IsSelected属性变化，更新SelectedPersonnels集合
                        personnelItem.PropertyChanged += (s, e) =>
                        {
                            if (e?.PropertyName == nameof(PersonnelItemViewModel.IsSelected))
                            {
                                if (s is not PersonnelItemViewModel item) return;

                                var personnelDto = GetPersonnelFromCache(item.PersonnelId);
                                if (personnelDto == null) return;

                                if (item.IsSelected)
                                {
                                    // 添加到SelectedPersonnels（如果不存在）
                                    if (!SelectedPersonnels.Any(p => p.Id == item.PersonnelId))
                                    {
                                        SelectedPersonnels.Add(personnelDto);
                                        System.Diagnostics.Debug.WriteLine($"添加人员到选中列表: {personnelDto.Name}");
                                    }
                                }
                                else
                                {
                                    // 从SelectedPersonnels移除
                                    var toRemove = SelectedPersonnels.FirstOrDefault(p => p.Id == item.PersonnelId);
                                    if (toRemove != null)
                                    {
                                        SelectedPersonnels.Remove(toRemove);
                                        System.Diagnostics.Debug.WriteLine($"从选中列表移除人员: {personnelDto.Name}");
                                    }
                                }
                            }
                        };

                        personnelItems.Add(personnelItem);
                    }

                    // 获取哨位的临时更改状态
                    var changes = _positionPersonnelManager.GetChanges(position.Id);

                    // 创建哨位人员视图模型
                    var positionViewModel = new PositionPersonnelViewModel
                    {
                        PositionId = position.Id,
                        PositionName = position.Name,
                        Location = position.Location ?? string.Empty,
                        PositionDto = position,
                        AvailablePersonnel = personnelItems,
                        IsExpanded = true,
                        HasChanges = changes.HasChanges,
                        ChangesSummary = changes.HasChanges 
                            ? $"添加 {changes.AddedPersonnelIds.Count} 人，移除 {changes.RemovedPersonnelIds.Count} 人"
                            : string.Empty
                    };

                    PositionPersonnelViewModels.Add(positionViewModel);
                }

                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine($"哨位人员视图模型更新完成: {PositionPersonnelViewModels.Count} 个哨位");
                System.Diagnostics.Debug.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds}ms");
                System.Diagnostics.Debug.WriteLine("=== 哨位人员视图模型更新完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 更新哨位人员视图模型失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                // 清空视图模型，避免显示错误数据
                PositionPersonnelViewModels.Clear();
                
                // 显示错误提示给用户
                _ = _dialogService.ShowErrorAsync("更新哨位人员列表失败", ex);
            }
        }

        // 属性变化回调
        partial void OnCurrentStepChanged(int value)
        {
            RefreshCommandStates();
            
            // 步骤3：自动提取人员
            if (value == 3)
            {
                ExtractPersonnelFromPositions();
            }
            else if (value == 4 && !IsLoadingConstraints && FixedPositionRules.Count == 0)
            {
                _ = LoadConstraintsAsync();
            }
            else if (value == 5)
            {
                BuildSummarySections();
            }
        }

        partial void OnScheduleTitleChanged(string value) => RefreshCommandStates();
        partial void OnStartDateChanged(DateTimeOffset value)
        {
            if (EndDate < value) EndDate = value;
            RefreshCommandStates();
            if (CurrentStep >= 4)
                _ = LoadConstraintsAsync();
            if (CurrentStep == 5) BuildSummarySections();
        }
        partial void OnEndDateChanged(DateTimeOffset value)
        {
            if (value < StartDate) value = StartDate;
            RefreshCommandStates();
            if (CurrentStep >= 4)
                _ = LoadConstraintsAsync();
            if (CurrentStep == 5) BuildSummarySections();
        }
        partial void OnSelectedPersonnelsChanged(ObservableCollection<PersonnelDto> value)
        {
            if (value != null)
            {
                value.CollectionChanged += (s, e) => RefreshCommandStates();
            }
            RefreshCommandStates();
            if (CurrentStep == 5) BuildSummarySections();
        }
        partial void OnSelectedPositionsChanged(ObservableCollection<PositionDto> value)
        {
            if (value != null)
            {
                value.CollectionChanged += (s, e) =>
                {
                    RefreshCommandStates();
                    
                    // 如果在步骤3，重新提取人员
                    if (CurrentStep == 3)
                    {
                        ExtractPersonnelFromPositions();
                    }
                };
            }
            RefreshCommandStates();
            
            // 如果在步骤3，重新提取人员
            if (CurrentStep == 3)
            {
                ExtractPersonnelFromPositions();
            }
            
            if (CurrentStep == 5) BuildSummarySections();
        }
        partial void OnUseActiveHolidayConfigChanged(bool value)
        {
            RefreshCommandStates();
            if (CurrentStep == 5) BuildSummarySections();
        }
        partial void OnSelectedHolidayConfigIdChanged(int? value)
        {
            RefreshCommandStates();
            if (CurrentStep == 5) BuildSummarySections();
        }
    }
}
