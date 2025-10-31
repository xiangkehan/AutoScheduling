using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;

namespace AutoScheduling3.ViewModels.Scheduling
{
 /// <summary>
 /// 排班结果页面 ViewModel：加载排班详情、确认、导出、返回等
 /// </summary>
 public class ScheduleResultViewModel : ObservableObject
 {
 private readonly ISchedulingService _schedulingService;
 private readonly DialogService _dialogService;
 private readonly NavigationService _navigationService;

 private ScheduleDto _schedule;
 public ScheduleDto Schedule
 {
 get => _schedule;
 private set
 {
 if (SetProperty(ref _schedule, value))
 {
 ConfirmCommand.NotifyCanExecuteChanged();
 ExportExcelCommand.NotifyCanExecuteChanged();
 }
 }
 }

 private bool _isLoading;
 public bool IsLoading
 {
 get => _isLoading;
 private set
 {
 if (SetProperty(ref _isLoading, value))
 {
 ConfirmCommand.NotifyCanExecuteChanged();
 }
 }
 }

 private bool _isConfirming;
 public bool IsConfirming
 {
 get => _isConfirming;
 private set
 {
 if (SetProperty(ref _isConfirming, value))
 {
 ConfirmCommand.NotifyCanExecuteChanged();
 }
 }
 }

 public IAsyncRelayCommand<int> LoadScheduleCommand { get; }
 public IAsyncRelayCommand ConfirmCommand { get; }
 public IRelayCommand BackCommand { get; }
 public IAsyncRelayCommand ExportExcelCommand { get; }

 public ScheduleResultViewModel(ISchedulingService schedulingService,
 DialogService dialogService,
 NavigationService navigationService)
 {
 _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
 _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
 _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

 LoadScheduleCommand = new AsyncRelayCommand<int>(LoadScheduleAsync);
 ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, CanConfirm);
 BackCommand = new RelayCommand(() => _navigationService.GoBack());
 ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync, CanExport);
 }

 private bool CanConfirm() => Schedule != null && Schedule.ConfirmedAt == null && !IsLoading && !IsConfirming;
 private bool CanExport() => Schedule != null;

 private async Task LoadScheduleAsync(int id)
 {
 if (IsLoading) return;
 IsLoading = true;
 try
 {
 var dto = await _schedulingService.GetScheduleByIdAsync(id);
 if (dto == null)
 {
 await _dialogService.ShowWarningAsync("未找到排班详情");
 return;
 }
 Schedule = dto;
 }
 catch (Exception ex)
 {
 await _dialogService.ShowErrorAsync("加载排班详情失败", ex);
 }
 finally
 {
 IsLoading = false;
 }
 }

 private async Task ConfirmAsync()
 {
 if (!CanConfirm()) return;
 var ok = await _dialogService.ShowConfirmAsync("确认排班", "确认后将无法修改，是否继续?", "确认", "取消");
 if (!ok) return;
 IsConfirming = true;
 try
 {
 await _schedulingService.ConfirmScheduleAsync(Schedule.Id);
 var dto = await _schedulingService.GetScheduleByIdAsync(Schedule.Id);
 Schedule = dto;
 await _dialogService.ShowSuccessAsync("排班已确认");
 }
 catch (Exception ex)
 {
 await _dialogService.ShowErrorAsync("确认排班失败", ex);
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
 var bytes = await _schedulingService.ExportScheduleAsync(Schedule.Id, "excel");
 await _dialogService.ShowSuccessAsync("导出完成 (示例占位，尚未实现保存)");
 }
 catch (NotImplementedException)
 {
 await _dialogService.ShowWarningAsync("导出功能尚未实现");
 }
 catch (Exception ex)
 {
 await _dialogService.ShowErrorAsync("导出失败", ex);
 }
 }
 }
}
