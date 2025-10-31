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
using AutoScheduling3.Models.Constraints; // 新增约束模型引用

namespace AutoScheduling3.ViewModels.Scheduling
{
 public partial class SchedulingViewModel : ObservableObject
 {
 private readonly ISchedulingService _schedulingService;
 private readonly IPersonnelService _personnelService;
 private readonly IPositionService _positionService;
 private readonly ITemplateService _templateService; // 新增模板服务
 private readonly DialogService _dialogService;
 private readonly NavigationService _navigationService;

 [ObservableProperty]
 private int _currentStep =1;

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

 //约束数据源
 [ObservableProperty] private ObservableCollection<HolidayConfig> _holidayConfigs = new(); // 已声明确保存在
 [ObservableProperty] private ObservableCollection<FixedPositionRule> _fixedPositionRules = new(); // 已声明
 [ObservableProperty] private ObservableCollection<ManualAssignment> _manualAssignments = new(); // 已声明

 [ObservableProperty]
 private ScheduleDto _resultSchedule;

 [ObservableProperty]
 private bool _isExecuting;
 [ObservableProperty]
 private bool _isLoadingInitial;
 [ObservableProperty]
 private bool _isLoadingConstraints; // 补充缺失字段

 // 模板相关
 [ObservableProperty] private int? _loadedTemplateId; // 已加载的模板ID
 [ObservableProperty] private bool _templateApplied; // 是否已应用模板（用于跳步逻辑）

 // 命令
 public IAsyncRelayCommand LoadDataCommand { get; }
 public IRelayCommand NextStepCommand { get; }
 public IRelayCommand PreviousStepCommand { get; }
 public IAsyncRelayCommand ExecuteSchedulingCommand { get; }
 public IRelayCommand CancelCommand { get; }
 public IAsyncRelayCommand LoadTemplateCommand { get; } // 新增
 public IAsyncRelayCommand LoadConstraintsCommand { get; } // 新增
 public IAsyncRelayCommand SaveAsTemplateCommand { get; } // 新增

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
 _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

 LoadDataCommand = new AsyncRelayCommand(LoadInitialDataAsync);
 NextStepCommand = new RelayCommand(NextStep, CanGoNext);
 PreviousStepCommand = new RelayCommand(PreviousStep, () => CurrentStep >1);
 ExecuteSchedulingCommand = new AsyncRelayCommand(ExecuteSchedulingAsync, CanExecuteScheduling);
 CancelCommand = new RelayCommand(CancelWizard);
 LoadTemplateCommand = new AsyncRelayCommand<int>(LoadTemplateAsync); // 带参数的模板加载
 LoadConstraintsCommand = new AsyncRelayCommand(LoadConstraintsAsync);
 SaveAsTemplateCommand = new AsyncRelayCommand(SaveAsTemplateAsync, CanSaveTemplate);
 }

 private void NextStep()
 {
 if (CurrentStep <5 && CanGoNext())
 {
 // 如果模板已应用并处于第1步，直接跳到第5步
 if (_templateApplied && CurrentStep ==1)
 {
 CurrentStep =5;
 }
 else
 {
 CurrentStep++;
 }
 RefreshCommandStates();
 }
 }
 private void PreviousStep()
 {
 if (CurrentStep >1)
 {
 // 如果模板已应用，阻止回到人员/哨位/约束步骤，只能回到第1步
 if (_templateApplied && CurrentStep >1 && CurrentStep <=5)
 {
 CurrentStep =1;
 }
 else
 {
 CurrentStep--;
 }
 RefreshCommandStates();
 }
 }
 private bool CanGoNext()
 {
 return CurrentStep switch
 {
1 => ValidateStep1(out _),
2 => ValidateStep2(out _),
3 => ValidateStep3(out _),
4 => true,
 _ => false
 };
 }
 private bool CanExecuteScheduling()
 {
 if (CurrentStep !=5 || IsExecuting) return false;
 return ValidateStep1(out _) && ValidateStep2(out _) && ValidateStep3(out _);
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
 if (string.IsNullOrWhiteSpace(ScheduleTitle)) { error = "排班名称不能为空"; return false; }
 if (ScheduleTitle.Length >100) { error = "排班名称长度不能超过100字符"; return false; }
 if (StartDate.Date < DateTimeOffset.Now.Date) { error = "开始日期不能早于今天"; return false; }
 if (EndDate.Date < StartDate.Date) { error = "结束日期不能早于开始日期"; return false; }
 if ((EndDate.Date - StartDate.Date).TotalDays +1 >365) { error = "排班周期不能超过365天"; return false; }
 error = string.Empty; return true;
 }
 private bool ValidateStep2(out string error)
 {
 if (SelectedPersonnels == null || SelectedPersonnels.Count ==0) { error = "请至少选择一名人员"; return false; }
 if (SelectedPersonnels.Any(p => !p.IsAvailable || p.IsRetired)) { error = "包含不可用或退役人员，请移除"; return false; }
 error = string.Empty; return true;
 }
 private bool ValidateStep3(out string error)
 {
 if (SelectedPositions == null || SelectedPositions.Count ==0) { error = "请至少选择一个哨位"; return false; }
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
 ScheduleTitle = $"排班表_{DateTime.Now:yyyyMMdd}";
 }
 catch (Exception ex)
 {
 await _dialogService.ShowErrorAsync("加载人员/哨位失败", ex);
 }
 finally
 {
 IsLoadingInitial = false;
 RefreshCommandStates();
 }
 }

 private async Task LoadConstraintsAsync()
 {
 if (_isLoadingConstraints) return; // 改为字段
 _isLoadingConstraints = true; // 改为字段
 try
 {
 var configsTask = _schedulingService.GetHolidayConfigsAsync();
 var rulesTask = _schedulingService.GetFixedPositionRulesAsync(true);
 var manualTask = _schedulingService.GetManualAssignmentsAsync(StartDate.Date, EndDate.Date, true);
 await Task.WhenAll(configsTask, rulesTask, manualTask);
 _holidayConfigs = new ObservableCollection<HolidayConfig>(configsTask.Result); // 改为字段
 _fixedPositionRules = new ObservableCollection<FixedPositionRule>(rulesTask.Result); // 改为字段
 _manualAssignments = new ObservableCollection<ManualAssignment>(manualTask.Result); // 改为字段
 }
 catch (Exception ex)
 {
 await _dialogService.ShowErrorAsync("加载约束配置失败", ex);
 }
 finally
 {
 _isLoadingConstraints = false; // 改为字段
 RefreshCommandStates();
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
 IsExecuting = true;
 try
 {
 var schedule = await _schedulingService.ExecuteSchedulingAsync(request);
 ResultSchedule = schedule;
 await _dialogService.ShowSuccessAsync("排班生成成功");
 try { _navigationService.NavigateTo("ScheduleResult", schedule.Id); } catch { }
 }
 catch (Exception ex)
 {
 await _dialogService.ShowErrorAsync("执行排班失败", ex);
 }
 finally
 {
 IsExecuting = false;
 }
 }
 private SchedulingRequestDto BuildSchedulingRequest()
 {
 return new SchedulingRequestDto
 {
 Title = ScheduleTitle.Trim(),
 StartDate = StartDate.DateTime.Date,
 EndDate = EndDate.DateTime.Date,
 PersonnelIds = SelectedPersonnels.Select(p => p.Id).ToList(),
 PositionIds = SelectedPositions.Select(p => p.Id).ToList(),
 UseActiveHolidayConfig = UseActiveHolidayConfig,
 HolidayConfigId = UseActiveHolidayConfig ? null : SelectedHolidayConfigId,
 EnabledFixedRuleIds = _enabledFixedRules?.Count >0 ? new List<int>(_enabledFixedRules) : null,
 EnabledManualAssignmentIds = _enabledManualAssignments?.Count >0 ? new List<int>(_enabledManualAssignments) : null
 };
 }

 private void CancelWizard()
 {
 CurrentStep =1;
 ScheduleTitle = string.Empty;
 SelectedPersonnels.Clear();
 SelectedPositions.Clear();
 EnabledFixedRules.Clear();
 EnabledManualAssignments.Clear();
 ResultSchedule = null;
 SelectedHolidayConfigId = null;
 UseActiveHolidayConfig = true;
 _loadedTemplateId = null;
 _templateApplied = false;
 RefreshCommandStates();
 }

 private async Task LoadTemplateAsync(int templateId)
 {
 try
 {
 var template = await _templateService.GetByIdAsync(templateId);
 if (template == null)
 {
 await _dialogService.ShowWarningAsync("模板不存在");
 return;
 }
 _loadedTemplateId = template.Id;
 //预填人员/哨位
 if (AvailablePersonnels.Count ==0 || AvailablePositions.Count ==0)
 await LoadInitialDataAsync();
 var selectedPers = AvailablePersonnels.Where(p => template.PersonnelIds.Contains(p.Id)).ToList();
 var selectedPos = AvailablePositions.Where(p => template.PositionIds.Contains(p.Id)).ToList();
 SelectedPersonnels = new ObservableCollection<PersonnelDto>(selectedPers);
 SelectedPositions = new ObservableCollection<PositionDto>(selectedPos);
 //约束
 UseActiveHolidayConfig = template.UseActiveHolidayConfig;
 SelectedHolidayConfigId = template.HolidayConfigId;
 _enabledFixedRules = template.EnabledFixedRuleIds?.ToList() ?? new();
 _enabledManualAssignments = template.EnabledManualAssignmentIds?.ToList() ?? new();
 _templateApplied = true;
 CurrentStep =1; // 保持在第1步仅需填日期和标题
 RefreshCommandStates();
 await _dialogService.ShowSuccessAsync("模板已加载，请选择日期范围后直接执行排班"); // 替换 ShowInfoAsync
 }
 catch (Exception ex)
 {
 await _dialogService.ShowErrorAsync("加载模板失败", ex);
 }
 }

 private bool CanSaveTemplate() => SelectedPersonnels.Count >0 && SelectedPositions.Count >0 && ValidateStep1(out _);
 private async Task SaveAsTemplateAsync()
 {
 if (!CanSaveTemplate()) { await _dialogService.ShowWarningAsync("当前配置不完整，无法保存为模板"); return; }
 // 构建自定义输入对话框 (名称、类型、描述、默认开关)
 var nameBox = new Microsoft.UI.Xaml.Controls.TextBox { PlaceholderText = "模板名称", Text = $"模板_{DateTime.Now:yyyyMMdd}" };
 var typeBox = new Microsoft.UI.Xaml.Controls.ComboBox { ItemsSource = new string[] { "regular", "holiday", "special" }, SelectedIndex =0, MinWidth =160 };
 var descBox = new Microsoft.UI.Xaml.Controls.TextBox { AcceptsReturn = true, Height =80, PlaceholderText = "描述(可选)" };
 var defaultSwitch = new Microsoft.UI.Xaml.Controls.ToggleSwitch { Header = "设为默认", IsOn = false };
 var panel = new Microsoft.UI.Xaml.Controls.StackPanel { Spacing =8 };
 panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "名称:" });
 panel.Children.Add(nameBox);
 panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "类型:" });
 panel.Children.Add(typeBox);
 panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "描述:" });
 panel.Children.Add(descBox);
 panel.Children.Add(defaultSwitch);
 var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
 {
 Title = "保存为模板",
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
 if (name.Length >100) { await _dialogService.ShowWarningAsync("名称不能超过100字符"); return; }
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
 EnabledFixedRuleIds = _enabledFixedRules?.ToList() ?? new List<int>(),
 EnabledManualAssignmentIds = _enabledManualAssignments?.ToList() ?? new List<int>()
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

 // 属性变更回调
 partial void OnScheduleTitleChanged(string value) => RefreshCommandStates();
 partial void OnStartDateChanged(DateTimeOffset value)
 {
 RefreshCommandStates();
 // 日期改变后刷新手动指定列表
 if (_templateApplied || CurrentStep >=4)
 {
 _ = LoadConstraintsAsync();
 }
 }
 partial void OnEndDateChanged(DateTimeOffset value)
 {
 RefreshCommandStates();
 if (_templateApplied || CurrentStep >=4)
 {
 _ = LoadConstraintsAsync();
 }
 }
 partial void OnSelectedPersonnelsChanged(ObservableCollection<PersonnelDto> value) => RefreshCommandStates();
 partial void OnSelectedPositionsChanged(ObservableCollection<PositionDto> value) => RefreshCommandStates();
 partial void OnUseActiveHolidayConfigChanged(bool value) => RefreshCommandStates();
 partial void OnSelectedHolidayConfigIdChanged(int? value) => RefreshCommandStates();
 }
}
