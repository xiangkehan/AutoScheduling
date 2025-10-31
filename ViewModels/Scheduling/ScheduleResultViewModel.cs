using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

namespace AutoScheduling3.ViewModels.Scheduling
{
    public enum ScheduleViewMode { Grid, List, ByPersonnel }

    /// <summary>
    /// 排班结果页面 ViewModel：加载排班详情、确认、导出、返回等
    /// </summary>
    [ObservableObject]
    public partial class ScheduleResultViewModel
    {
        private readonly ISchedulingService _schedulingService;
        private readonly DialogService _dialogService;
        private readonly NavigationService _navigationService;

        [ObservableProperty]
        private ScheduleDto _schedule;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isConfirming;

        [ObservableProperty]
        private ScheduleViewMode _currentViewMode = ScheduleViewMode.Grid;

        [ObservableProperty]
        private bool _isConflictPaneOpen = true;

        [ObservableProperty]
        private ObservableCollection<ConflictDto> _conflicts = new();

        public IAsyncRelayCommand<int> LoadScheduleCommand { get; }
        public IAsyncRelayCommand ConfirmCommand { get; }
        public IRelayCommand BackCommand { get; }
        public IAsyncRelayCommand ExportExcelCommand { get; }
        public IRelayCommand RescheduleCommand { get; }
        public IRelayCommand<ScheduleViewMode> ChangeViewModeCommand { get; }


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
            RescheduleCommand = new RelayCommand(Reschedule, CanReschedule);
            ChangeViewModeCommand = new RelayCommand<ScheduleViewMode>(viewMode => CurrentViewMode = viewMode);

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
                }
                if (e.PropertyName is nameof(IsLoading) or nameof(IsConfirming))
                {
                    ConfirmCommand.NotifyCanExecuteChanged();
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
                    await _dialogService.ShowWarningAsync("未找到排班详情");
                    _navigationService.GoBack();
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
                var bytes = await _schedulingService.ExportScheduleAsync(Schedule.Id, "csv");

                var savePicker = new FileSavePicker();
                InitializeWithWindow.Initialize(savePicker, App.MainWindowHandle);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV (Comma-separated values)", new List<string>() { ".csv" });
                savePicker.SuggestedFileName = $"{Schedule.Title}_{DateTime.Now:yyyyMMdd}";

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteBytesAsync(file, bytes);
                    await _dialogService.ShowSuccessAsync($"导出成功！文件已保存到: {file.Path}");
                }
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

        private void Reschedule()
        {
            if (!CanReschedule()) return;

            var request = new SchedulingRequestDto
            {
                Title = $"{Schedule.Title} (副本)",
                StartDate = Schedule.StartDate,
                EndDate = Schedule.EndDate,
                PersonnelIds = Schedule.PersonnelIds.ToList(),
                PositionIds = Schedule.PositionIds.ToList(),
                // Note: Constraint information is not part of ScheduleDto, so it cannot be carried over.
                // The user will need to re-configure constraints on the creation page.
            };

            _navigationService.NavigateTo("CreateScheduling", request);
        }
    }
}
