using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.ViewModels.History
{
    public partial class CompareViewModel : ViewModelBase
    {
        private readonly IHistoryService _historyService;

        [ObservableProperty]
        private ObservableCollection<HistoryScheduleDto> _schedules;

        [ObservableProperty]
        private HistoryScheduleDto? _selectedSchedule1;

        [ObservableProperty]
        private HistoryScheduleDto? _selectedSchedule2;

        [ObservableProperty]
        private HistoryScheduleDetailDto? _scheduleDetail1;

        [ObservableProperty]
        private HistoryScheduleDetailDto? _scheduleDetail2;

        // Add properties for comparison results here

        public CompareViewModel(IHistoryService historyService)
        {
            _historyService = historyService;
            Title = "≈≈∞‡∂‘±»";
            Schedules = new ObservableCollection<HistoryScheduleDto>();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            var result = await _historyService.GetHistorySchedulesAsync(new HistoryQueryOptions());
            Schedules.Clear();
            foreach (var item in result.OrderByDescending(i => i.ConfirmTime))
            {
                Schedules.Add(item);
            }
            IsLoading = false;
        }

        [RelayCommand]
        private async Task CompareAsync()
        {
            if (SelectedSchedule1 == null || SelectedSchedule2 == null)
            {
                // Show error/message
                return;
            }

            IsLoading = true;

            var result = await _historyService.GetSchedulesForComparisonAsync(SelectedSchedule1.Id, SelectedSchedule2.Id);

            ScheduleDetail1 = result.Item1;
            ScheduleDetail2 = result.Item2;

            // TODO: Implement comparison logic here

            IsLoading = false;
        }
    }
}
