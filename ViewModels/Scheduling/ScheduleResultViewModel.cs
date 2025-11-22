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
    /// �Ű���ҳ�� ViewModel�������Ű����顢ȷ�ϡ����������ص�
    /// </summary>
    public partial class ScheduleResultViewModel : ViewModelBase
    {
        private readonly ISchedulingService _schedulingService;
        private readonly DialogService _dialogService;
        private readonly NavigationService _navigationService;

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
            set => SetProperty(ref _currentViewMode, value);
        }

        private bool _isConflictPaneOpen = true;
        public bool IsConflictPaneOpen
        {
            get => _isConflictPaneOpen;
            set => SetProperty(ref _isConflictPaneOpen, value);
        }

        private ObservableCollection<ConflictDto> _conflicts = new();
        public ObservableCollection<ConflictDto> Conflicts
        {
            get => _conflicts;
            set => SetProperty(ref _conflicts, value);
        }

        public IAsyncRelayCommand<int> LoadScheduleCommand { get; }
        public IAsyncRelayCommand ConfirmCommand { get; }
        public IRelayCommand BackCommand { get; }
        public IAsyncRelayCommand ExportExcelCommand { get; }
        public IRelayCommand RescheduleCommand { get; }
        public IRelayCommand<ViewMode> ChangeViewModeCommand { get; }


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
            ChangeViewModeCommand = new RelayCommand<ViewMode>(viewMode => CurrentViewMode = viewMode);

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
                    await _dialogService.ShowWarningAsync("δ�ҵ��Ű�����");
                    _navigationService.GoBack();
                    return;
                }
                Schedule = dto;
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
                    await _dialogService.ShowSuccessAsync($"�����ɹ����ļ��ѱ��浽: {file.Path}");
                }
            }
            catch (NotImplementedException)
            {
                await _dialogService.ShowWarningAsync("����������δʵ��");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("����ʧ��", ex);
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
    }
}
