using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace AutoScheduling3.ViewModels.History
{
    public partial class HistoryDetailViewModel : ViewModelBase
    {
        private readonly IHistoryService _historyService;

        [ObservableProperty]
        private HistoryScheduleDetailDto _scheduleDetail;

        public HistoryDetailViewModel(IHistoryService historyService)
        {
            _historyService = historyService;
            Title = "¿˙ ∑œÍ«È";
        }

        public async Task LoadScheduleDetailAsync(int scheduleId)
        {
            IsLoading = true;
            ScheduleDetail = await _historyService.GetHistoryScheduleDetailAsync(scheduleId);
            IsLoading = false;
            IsLoaded = ScheduleDetail != null;
        }
    }
}
