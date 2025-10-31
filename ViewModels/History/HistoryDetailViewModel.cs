using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.ViewModels.History
{
    public partial class HistoryDetailViewModel : ViewModelBase
    {
        private readonly IHistoryService _historyService;

        [ObservableProperty]
        private HistoryScheduleDetailDto? _scheduleDetail;

        [ObservableProperty]
        private IEnumerable<KeyValuePair<string, int>> _shiftsByTimeOfDay = Enumerable.Empty<KeyValuePair<string, int>>();

        [ObservableProperty]
        private IEnumerable<KeyValuePair<string, int>> _shiftsPerPerson = Enumerable.Empty<KeyValuePair<string, int>>();

        public HistoryDetailViewModel(IHistoryService historyService)
        {
            _historyService = historyService;
            Title = "¿˙ ∑œÍ«È";
        }

        public async Task LoadDetailAsync(int scheduleId)
        {
            IsLoading = true;
            ScheduleDetail = await _historyService.GetHistoryScheduleDetailAsync(scheduleId);

            if (ScheduleDetail?.Statistics != null)
            {
                ShiftsByTimeOfDay = ScheduleDetail.Statistics.ShiftsByTimeOfDay.AsEnumerable();
                ShiftsPerPerson = ScheduleDetail.Statistics.ShiftsPerPerson.AsEnumerable();
            }
            else
            {
                ShiftsByTimeOfDay = Enumerable.Empty<KeyValuePair<string, int>>();
                ShiftsPerPerson = Enumerable.Empty<KeyValuePair<string, int>>();
            }

            IsLoading = false;
            IsLoaded = ScheduleDetail != null;
        }
    }
}
