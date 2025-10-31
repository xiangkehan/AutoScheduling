using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace AutoScheduling3.ViewModels.History
{
    public partial class HistoryViewModel : ListViewModelBase<HistoryScheduleDto>
    {
        private readonly IHistoryService _historyService;

        [ObservableProperty]
        private ObservableCollection<GroupedHistorySchedule> _groupedHistorySchedules;

        [ObservableProperty]
        private DateTimeOffset? _startDate;

        [ObservableProperty]
        private DateTimeOffset? _endDate;

        [ObservableProperty]
        private string _keyword;

        [ObservableProperty]
        private bool _isTimelineView = true;

        [ObservableProperty]
        private bool _isListView = false;

        [ObservableProperty]
        private string _selectedSortBy = "Time";

        [ObservableProperty]
        private bool _isSortAscending = false;

        public List<string> SortByOptions { get; } = new List<string> { "Time", "Name" };

        public HistoryViewModel(IHistoryService historyService)
        {
            _historyService = historyService;
            Title = "历史记录";
            GroupedHistorySchedules = new ObservableCollection<GroupedHistorySchedule>();
        }

        [RelayCommand]
        private void SwitchView(string view)
        {
            if (view == "Timeline")
            {
                IsTimelineView = true;
                IsListView = false;
            }
            else
            {
                IsTimelineView = false;
                IsListView = true;
            }
        }

        public override async Task LoadDataAsync()
        {
            IsLoading = true;
            var options = new HistoryQueryOptions
            {
                StartDate = StartDate?.Date,
                EndDate = EndDate?.Date,
                Keyword = Keyword,
                SortBy = SelectedSortBy,
                IsAscending = IsSortAscending
            };

            var result = await _historyService.GetHistorySchedulesAsync(options);

            // Clear existing items and add new ones
            Items.Clear();
            foreach (var item in result)
            {
                Items.Add(item);
            }

            GroupData();

            IsLoading = false;
            IsLoaded = true;
            IsEmpty = Items.Count == 0;
        }

        private void GroupData()
        {
            GroupedHistorySchedules.Clear();
            var grouped = Items
                .GroupBy(h => new { h.ConfirmTime.Year, h.ConfirmTime.Month })
                .OrderByDescending(g => g.Key.Year)
                .ThenByDescending(g => g.Key.Month)
                .Select(g => new GroupedHistorySchedule(g.Key.Year, g.Key.Month, g.ToList()));

            foreach (var group in grouped)
            {
                GroupedHistorySchedules.Add(group);
            }
        }

        [RelayCommand]
        private async Task SortAsync(string sortBy)
        {
            if (SelectedSortBy == sortBy)
            {
                IsSortAscending = !IsSortAscending;
            }
            else
            {
                SelectedSortBy = sortBy;
                IsSortAscending = false;
            }
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private void ViewDetail(int scheduleId)
        {
            var navigationService = (App.Current as App).ServiceProvider.GetRequiredService<Helpers.NavigationService>();
            navigationService.NavigateTo("HistoryDetail", scheduleId);
        }
    }

    public class GroupedHistorySchedule : List<HistoryScheduleDto>
    {
        public string Key { get; private set; }
        public GroupedHistorySchedule(int year, int month, List<HistoryScheduleDto> items) : base(items)
        {
            Key = $"{year}年 {month}月";
        }
    }
}
