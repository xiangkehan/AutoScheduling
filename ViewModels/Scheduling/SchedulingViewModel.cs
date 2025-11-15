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
using AutoScheduling3.Models.Constraints; // ����Լ��ģ������
using CommunityToolkit.Mvvm.Input.Internals;

namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel : ObservableObject
    {
        private readonly ISchedulingService _schedulingService;
        private readonly IPersonnelService _personnelService;
        private readonly IPositionService _positionService;
        private readonly ITemplateService _templateService; // ����ģ�����
        private readonly DialogService _dialogService;
        private readonly NavigationService _navigation_service; // ����ԭ���������ֶ���
        private Microsoft.UI.Xaml.Controls.ContentDialog? _progressDialog; //���ȶԻ�������

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

        //Լ������Դ��ʹ�������Ա� UI��)
        [ObservableProperty] private ObservableCollection<HolidayConfig> _holidayConfigs = new();
        [ObservableProperty] private ObservableCollection<FixedPositionRule> _fixedPositionRules = new();
        [ObservableProperty] private ObservableCollection<ManualAssignment> _manualAssignments = new();

        [ObservableProperty]
        private ScheduleDto _resultSchedule;

        [ObservableProperty]
        private bool _isExecuting;
        [ObservableProperty]
        private bool _isLoadingInitial;
        [ObservableProperty]
        private bool _isLoadingConstraints; // ʹ�����Թ� UI ��ʾ����״̬

        // ģ����أ�ʹ�����Զ����ֶΣ����ڰ����߼�ͳһ��
        [ObservableProperty] private int? _loadedTemplateId;
        [ObservableProperty] private bool _templateApplied;

        // ��5������չʾ����
        [ObservableProperty] private ObservableCollection<SummarySection> _summarySections = new();

        // ����
        public IAsyncRelayCommand LoadDataCommand { get; }
        public IRelayCommand NextStepCommand { get; }
        public IRelayCommand PreviousStepCommand { get; }
        public IAsyncRelayCommand ExecuteSchedulingCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IAsyncRelayCommand<int> LoadTemplateCommand { get; }
        public IAsyncRelayCommand LoadConstraintsCommand { get; }
        public IAsyncRelayCommand SaveAsTemplateCommand { get; }

        public SchedulingViewModel(
        ISchedulingService schedulingService,
        IPersonnelService personnelService,
        IPositionService positionService,
        ITemplateService templateService,
        DialogService dialogService,
        NavigationService navigationService)
        {
            _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
            _personnelService = personnelService ?? throw new ArgumentNullException(nameof(personnelService));
            _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _navigation_service = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            LoadDataCommand = new AsyncRelayCommand(LoadInitialDataAsync);
            NextStepCommand = new RelayCommand(NextStep, CanGoNext);
            PreviousStepCommand = new RelayCommand(PreviousStep, () => CurrentStep > 1);
            ExecuteSchedulingCommand = new AsyncRelayCommand(ExecuteSchedulingAsync, CanExecuteScheduling);
            CancelCommand = new RelayCommand(CancelWizard);
            LoadTemplateCommand = new AsyncRelayCommand<int>(LoadTemplateAsync);
            LoadConstraintsCommand = new AsyncRelayCommand(LoadConstraintsAsync);
            SaveAsTemplateCommand = new AsyncRelayCommand(SaveAsTemplateAsync, CanSaveTemplate);

            // Listen to property changes to refresh command states
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(CurrentStep) or nameof(ScheduleTitle) or nameof(StartDate) or nameof(EndDate) or nameof(IsExecuting))
                {
                    RefreshCommandStates();
                }
            };
            SelectedPersonnels.CollectionChanged += (s, e) => RefreshCommandStates();
            SelectedPositions.CollectionChanged += (s, e) => RefreshCommandStates();
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

        private bool ValidateStep1(out string error)
        {
            if (string.IsNullOrWhiteSpace(ScheduleTitle)) { error = "排班标题不能为空"; return false; }
            if (ScheduleTitle.Length > 100) { error = "排班标题长度不能超过100字符"; return false; }
            if (StartDate.Date < DateTimeOffset.Now.Date) { error = "开始日期不能早于今天"; return false; }
            if (EndDate.Date < StartDate.Date) { error = "结束日期不能早于开始日期"; return false; }
            if ((EndDate.Date - StartDate.Date).TotalDays + 1 > 365) { error = "排班周期不能超过365天"; return false; }
            error = string.Empty; return true;
        }
        private bool ValidateStep2(out string error)
        {
            if (SelectedPersonnels == null || SelectedPersonnels.Count == 0) { error = "请至少选择一名人员"; return false; }
            if (SelectedPersonnels.Any(p => !p.IsAvailable || p.IsRetired)) { error = "选择的人员有不可用人员，请移除"; return false; }
            error = string.Empty; return true;
        }
        private bool ValidateStep3(out string error)
        {
            if (SelectedPositions == null || SelectedPositions.Count == 0) { error = "请至少选择一个岗位"; return false; }
            error = string.Empty; return true;
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
            
            var request = BuildSchedulingRequest();
            
            // 显示进度对话框
            _progressDialog = _dialogService.ShowLoadingDialog("正在生成排班，请稍候...");
            IsExecuting = true;
            RefreshCommandStates();
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"开始执行排班: {request.Title}");
                System.Diagnostics.Debug.WriteLine($"人员数: {request.PersonnelIds.Count}, 哨位数: {request.PositionIds.Count}");
                
                var schedule = await _schedulingService.ExecuteSchedulingAsync(request);
                
                System.Diagnostics.Debug.WriteLine($"排班执行成功，生成 {schedule.Shifts.Count} 个班次");
                
                ResultSchedule = schedule;
                await _dialogService.ShowSuccessAsync("排班生成成功");
                
                try 
                { 
                    _navigation_service.NavigateTo("ScheduleResult", schedule.Id); 
                } 
                catch (Exception navEx)
                {
                    System.Diagnostics.Debug.WriteLine($"导航失败: {navEx.Message}");
                }
            }
            catch (ArgumentException argEx)
            {
                // 业务规则验证失败
                System.Diagnostics.Debug.WriteLine($"业务规则验证失败: {argEx.Message}");
                await _dialogService.ShowErrorAsync($"排班参数验证失败：{argEx.Message}");
            }
            catch (InvalidOperationException invEx)
            {
                // 操作无效（如数据不存在）
                System.Diagnostics.Debug.WriteLine($"操作无效: {invEx.Message}");
                await _dialogService.ShowErrorAsync($"排班操作失败：{invEx.Message}");
            }
            catch (Exception ex)
            {
                // 其他未知异常
                System.Diagnostics.Debug.WriteLine($"排班执行异常: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"异常消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                await _dialogService.ShowErrorAsync($"执行排班失败：{ex.Message}\n\n详细信息：{ex.GetType().Name}");
            }
            finally
            {
                IsExecuting = false;
                if (_progressDialog != null)
                {
                    try { _progressDialog.Hide(); } catch { }
                    _progressDialog = null;
                }
                RefreshCommandStates();
            }
        }
        private SchedulingRequestDto BuildSchedulingRequest() => new()
        {
            Title = ScheduleTitle.Trim(),
            StartDate = StartDate.DateTime.Date,
            EndDate = EndDate.DateTime.Date,
            PersonnelIds = SelectedPersonnels.Select(p => p.Id).ToList(),
            PositionIds = SelectedPositions.Select(p => p.Id).ToList(),
            UseActiveHolidayConfig = UseActiveHolidayConfig,
            HolidayConfigId = UseActiveHolidayConfig ? null : SelectedHolidayConfigId,
            EnabledFixedRuleIds = FixedPositionRules.Where(r => r.IsEnabled).Select(r => r.Id).ToList(),
            EnabledManualAssignmentIds = ManualAssignments.Where(a => a.IsEnabled).Select(a => a.Id).ToList()
        };

        private void CancelWizard()
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
            RefreshCommandStates();
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
            var result = await dialog.ShowAsync();
            if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary) return;
            var name = nameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name)) { await _dialogService.ShowWarningAsync("名称不能为空"); return; }
            var type = typeBox.SelectedItem?.ToString() ?? "regular";
            if (name.Length > 100) { await _dialogService.ShowWarningAsync("名称不能超过100字符"); return; }
            var createDto = new CreateTemplateDto
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
                EnabledManualAssignmentIds = ManualAssignments.Where(a => a.IsEnabled).Select(a => a.Id).ToList()
            };
            try
            {
                var tpl = await _templateService.CreateAsync(createDto);
                await _dialogService.ShowSuccessAsync($"模板 '{tpl.Name}' 已保存");
            }
            catch (Exception ex)
            {
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
            // 模板信息
            if (TemplateApplied && LoadedTemplateId.HasValue)
            {
                sections.Add(new SummarySection { Header = "模板来源", Lines = { $"来源模板ID: {LoadedTemplateId}" } });
            }
            // 在 BuildSummarySections 方法中
            SummarySections = new ObservableCollection<SummarySection>(sections);
        }

        // 属性变化回调
        partial void OnCurrentStepChanged(int value)
        {
            RefreshCommandStates();
            if (value == 4 && !IsLoadingConstraints && FixedPositionRules.Count == 0)
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
                value.CollectionChanged += (s, e) => RefreshCommandStates();
            }
            RefreshCommandStates();
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
