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
using AutoScheduling3.Helpers;

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
        private string _selectedSortBy = "按时间";

        [ObservableProperty]
        private bool _isSortAscending = false;

        [ObservableProperty]
        private int _currentPage =1;

        [ObservableProperty]
        private int _pageSize =20;

        [ObservableProperty]
        private int _totalPages =1;

        public List<string> SortByOptions { get; } = new List<string> { "按时间", "按名称" };

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
            // 将中文排序选项转换为英文
            var sortBy = SelectedSortBy switch
            {
                "按时间" => "Time",
                "按名称" => "Name",
                _ => "Time"
            };
            
            var options = new HistoryQueryOptions
            {
                StartDate = StartDate?.Date,
                EndDate = EndDate?.Date,
                Keyword = Keyword,
                SortBy = sortBy,
                IsAscending = IsSortAscending
            };

            var result = await _historyService.GetHistorySchedulesAsync(options);
            var list = result.ToList();
            TotalPages = Math.Max(1, (int)Math.Ceiling(list.Count / (double)PageSize));
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            var pageItems = list.Skip((CurrentPage -1) * PageSize).Take(PageSize).ToList();

            Items.Clear();
            foreach (var item in pageItems)
            {
                Items.Add(item);
            }

            GroupData(list);

            IsLoading = false;
            IsLoaded = true;
            IsEmpty = Items.Count ==0;
        }

        private void GroupData(List<HistoryScheduleDto> allItems)
        {
            GroupedHistorySchedules.Clear();
            var grouped = allItems
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
            CurrentPage =1;
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            CurrentPage =1;
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task ApplyDateRangeAsync()
        {
            CurrentPage =1;
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task GoToPageAsync(int page)
        {
            if (page <1 || page > TotalPages) return;
            CurrentPage = page;
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task PrevPageAsync()
        {
            if (CurrentPage >1)
            {
                CurrentPage--;
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private void ViewDetail(int scheduleId)
        {
            var navigationService = (App.Current as App).ServiceProvider.GetRequiredService<NavigationService>();
            // 使用 ScheduleResult 页面展示历史详情
            navigationService.NavigateTo("ScheduleResult", scheduleId);
        }

        [RelayCommand]
        private async Task ExportAsync(int scheduleId)
        {
            var detail = await _historyService.GetHistoryScheduleDetailAsync(scheduleId);
            if (detail == null) return;
            var json = System.Text.Json.JsonSerializer.Serialize(detail);
            try
            {
                var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dp.SetText(json);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
            }
            catch { }
        }

        [RelayCommand]
        private void Compare(int scheduleId)
        {
            var navigationService = (App.Current as App).ServiceProvider.GetRequiredService<NavigationService>();
            navigationService.NavigateTo("Compare", scheduleId);
        }
    }

    public class GroupedHistorySchedule
    {
        public string Key { get; private set; }
        public List<HistoryScheduleDto> Items { get; private set; }
        
        public GroupedHistorySchedule(int year, int month, List<HistoryScheduleDto> items)
        {
            Key = $"{year}年 {month}月";
            Items = items;
        }
    }
}
