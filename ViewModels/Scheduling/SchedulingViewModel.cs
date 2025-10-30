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

namespace AutoScheduling3.ViewModels.Scheduling
{
 /// <summary>
 /// 排班向导 ViewModel（步骤1-5）
 ///仅实现当前已有 DTO/Service 支持的核心字段；约束/模板等后续扩展。
 /// </summary>
 public partial class SchedulingViewModel : ObservableObject
 {
 private readonly ISchedulingService _schedulingService;
 private readonly IPersonnelService _personnelService;
 private readonly IPositionService _positionService;
 private readonly DialogService _dialogService;
 private readonly NavigationService _navigationService;

 // 步骤范围：1~5
 [ObservableProperty]
 private int _currentStep =1;

 [ObservableProperty]
 private string _scheduleTitle = string.Empty;

 [ObservableProperty]
 private DateTimeOffset _startDate = DateTimeOffset.Now;

 [ObservableProperty]
 private DateTimeOffset _endDate = DateTimeOffset.Now.AddDays(7);

 // 可选数据源
 [ObservableProperty]
 private ObservableCollection<PersonnelDto> _availablePersonnels = new();

 [ObservableProperty]
 private ObservableCollection<PositionDto> _availablePositions = new();

 // 已选集合
 [ObservableProperty]
 private ObservableCollection<PersonnelDto> _selectedPersonnels = new();

 [ObservableProperty]
 private ObservableCollection<PositionDto> _selectedPositions = new();

 //约束相关（先用 ID 列表占位，可在后续扩展为具体 DTO）
 [ObservableProperty]
 private List<int> _enabledFixedRules = new();

 [ObservableProperty]
 private List<int> _enabledManualAssignments = new();

 //结果
 [ObservableProperty]
 private ScheduleDto _resultSchedule;

 // 状态
 [ObservableProperty]
 private bool _isExecuting;

 [ObservableProperty]
 private bool _isLoadingInitial;

 // 命令
 public IAsyncRelayCommand LoadDataCommand { get; }
 public IRelayCommand NextStepCommand { get; }
 public IRelayCommand PreviousStepCommand { get; }
 public IAsyncRelayCommand ExecuteSchedulingCommand { get; }
 public IRelayCommand CancelCommand { get; }

 public SchedulingViewModel(
 ISchedulingService schedulingService,
 IPersonnelService personnelService,
 IPositionService positionService,
 DialogService dialogService,
 NavigationService navigationService)
 {
 _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
 _personnelService = personnelService ?? throw new ArgumentNullException(nameof(personnelService));
 _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
 _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
 _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

 LoadDataCommand = new AsyncRelayCommand(LoadInitialDataAsync);
 NextStepCommand = new RelayCommand(NextStep, CanGoNext);
 PreviousStepCommand = new RelayCommand(PreviousStep, () => CurrentStep >1);
 ExecuteSchedulingCommand = new AsyncRelayCommand(ExecuteSchedulingAsync, CanExecuteScheduling);
 CancelCommand = new RelayCommand(CancelWizard);
 }

 #region 步骤导航
 private void NextStep()
 {
 if (CurrentStep <5 && CanGoNext())
 {
 CurrentStep++;
 RefreshCommandStates();
 }
 }

 private void PreviousStep()
 {
 if (CurrentStep >1)
 {
 CurrentStep--;
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
4 => true, //约束配置暂未实现详细验证
 _ => false
 };
 }

 private bool CanExecuteScheduling()
 {
 if (CurrentStep !=5) return false;
 return ValidateStep1(out _) && ValidateStep2(out _) && ValidateStep3(out _);
 }

 private void RefreshCommandStates()
 {
 NextStepCommand.NotifyCanExecuteChanged();
 PreviousStepCommand.NotifyCanExecuteChanged();
 ExecuteSchedulingCommand.NotifyCanExecuteChanged();
 }
 #endregion

 #region 验证
 private bool ValidateStep1(out string error)
 {
 if (string.IsNullOrWhiteSpace(ScheduleTitle))
 {
 error = "排班表名称不能为空";
 return false;
 }
 if (ScheduleTitle.Length >100)
 {
 error = "排班表名称长度不能超过100字符";
 return false;
 }
 if (StartDate.Date < DateTimeOffset.Now.Date)
 {
 error = "开始日期不能早于今天";
 return false;
 }
 if (EndDate.Date < StartDate.Date)
 {
 error = "结束日期必须大于或等于开始日期";
 return false;
 }
 if ((EndDate.Date - StartDate.Date).TotalDays +1 >365)
 {
 error = "排班周期不能超过365天";
 return false;
 }
 error = string.Empty;
 return true;
 }

 private bool ValidateStep2(out string error)
 {
 if (SelectedPersonnels == null || SelectedPersonnels.Count ==0)
 {
 error = "至少选择一名人员";
 return false;
 }
 error = string.Empty;
 return true;
 }

 private bool ValidateStep3(out string error)
 {
 if (SelectedPositions == null || SelectedPositions.Count ==0)
 {
 error = "至少选择一个哨位";
 return false;
 }
 error = string.Empty;
 return true;
 }
 #endregion

 #region 数据加载
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
 }
 catch (Exception ex)
 {
 await _dialogService.ShowErrorAsync("加载人员/哨位数据失败", ex);
 }
 finally
 {
 IsLoadingInitial = false;
 RefreshCommandStates();
 }
 }
 #endregion

 #region 执行排班
 private async Task ExecuteSchedulingAsync()
 {
 if (!CanExecuteScheduling())
 {
 // 找出第一个失败原因
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
 // 若已注册结果页面，可导航
 try { _navigationService.NavigateTo("ScheduleResult", schedule.Id); } catch { /* 忽略导航错误 */ }
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
 UseActiveHolidayConfig = true, //先固定；后续可由 UI 控制
 EnabledFixedRuleIds = _enabledFixedRules?.Count >0 ? new List<int>(_enabledFixedRules) : null,
 EnabledManualAssignmentIds = _enabledManualAssignments?.Count >0 ? new List<int>(_enabledManualAssignments) : null
 };
 }
 #endregion

 #region Cancel
 private void CancelWizard()
 {
 // 简单回到第一步并清空选择
 CurrentStep =1;
 ScheduleTitle = string.Empty;
 SelectedPersonnels.Clear();
 SelectedPositions.Clear();
 EnabledFixedRules.Clear();
 EnabledManualAssignments.Clear();
 ResultSchedule = null;
 RefreshCommandStates();
 }
 #endregion

 #region 属性变化联动
 partial void OnScheduleTitleChanged(string value) => RefreshCommandStates();
 partial void OnStartDateChanged(DateTimeOffset value) => RefreshCommandStates();
 partial void OnEndDateChanged(DateTimeOffset value) => RefreshCommandStates();
 partial void OnSelectedPersonnelsChanged(ObservableCollection<PersonnelDto> value) => RefreshCommandStates();
 partial void OnSelectedPositionsChanged(ObservableCollection<PositionDto> value) => RefreshCommandStates();
 #endregion
 }
}
