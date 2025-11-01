using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.ViewModels.Base;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AutoScheduling3.ViewModels.History
{
    public enum CompareMode
    {
        SideBySide,
        DiffHighlight,
        Statistics
    }

    public class ShiftDiff
    {
        public int DayIndex { get; set; }
        public int PositionId { get; set; }
        public int? PersonLeft { get; set; }
        public int? PersonRight { get; set; }
        public string DiffType { get; set; } = string.Empty; // 新增班次 / 删除班次 / 人员变更 / 时间调整
    }

    public partial class CompareViewModel : ViewModelBase
    {
        private readonly IHistoryService _historyService;

        private ObservableCollection<HistoryScheduleDto> _schedules = new();
        public ObservableCollection<HistoryScheduleDto> Schedules
        {
            get => _schedules;
            set => SetProperty(ref _schedules, value);
        }

        private HistoryScheduleDto? _selectedSchedule1;
        public HistoryScheduleDto? SelectedSchedule1
        {
            get => _selectedSchedule1;
            set => SetProperty(ref _selectedSchedule1, value);
        }

        private HistoryScheduleDto? _selectedSchedule2;
        public HistoryScheduleDto? SelectedSchedule2
        {
            get => _selectedSchedule2;
            set => SetProperty(ref _selectedSchedule2, value);
        }

        private HistoryScheduleDetailDto? _scheduleDetail1;
        public HistoryScheduleDetailDto? ScheduleDetail1
        {
            get => _scheduleDetail1;
            set => SetProperty(ref _scheduleDetail1, value);
        }

        private HistoryScheduleDetailDto? _scheduleDetail2;
        public HistoryScheduleDetailDto? ScheduleDetail2
        {
            get => _scheduleDetail2;
            set => SetProperty(ref _scheduleDetail2, value);
        }

        private ObservableCollection<ShiftDiff> _differences = new();
        public ObservableCollection<ShiftDiff> Differences
        {
            get => _differences;
            set => SetProperty(ref _differences, value);
        }

        private CompareMode _mode = CompareMode.SideBySide;
        public CompareMode Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        private bool _hasCompared;
        public bool HasCompared
        {
            get => _hasCompared;
            set => SetProperty(ref _hasCompared, value);
        }

        public IReadOnlyList<CompareMode> Modes { get; } = new List<CompareMode> { CompareMode.SideBySide, CompareMode.DiffHighlight, CompareMode.Statistics };

        public CompareViewModel(IHistoryService historyService)
        {
            _historyService = historyService;
            Title = "排班对比";
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
            if (SelectedSchedule1 == null || SelectedSchedule2 == null || SelectedSchedule1.Id == SelectedSchedule2.Id)
            {
                return;
            }

            IsLoading = true;
            var result = await _historyService.GetSchedulesForComparisonAsync(SelectedSchedule1.Id, SelectedSchedule2.Id);
            ScheduleDetail1 = result.Item1;
            ScheduleDetail2 = result.Item2;

            Differences.Clear();
            if (ScheduleDetail1 != null && ScheduleDetail2 != null)
            {
                var leftShifts = ScheduleDetail1.Shifts;
                var rightShifts = ScheduleDetail2.Shifts;

                var leftMap = leftShifts.GroupBy(s => (s.DayIndex, s.PositionId)).ToDictionary(g => g.Key, g => g.ToList());
                var rightMap = rightShifts.GroupBy(s => (s.DayIndex, s.PositionId)).ToDictionary(g => g.Key, g => g.ToList());
                var allKeys = leftMap.Keys.Union(rightMap.Keys).ToList();

                foreach (var key in allKeys)
                {
                    leftMap.TryGetValue(key, out var leftList);
                    rightMap.TryGetValue(key, out var rightList);

                    var leftPersons = leftList?.Select(s => s.PersonnelId).OrderBy(i => i).ToList() ?? new List<int>();
                    var rightPersons = rightList?.Select(s => s.PersonnelId).OrderBy(i => i).ToList() ?? new List<int>();

                    if (!leftPersons.Any() && rightPersons.Any())
                    {
                        Differences.Add(new ShiftDiff { DayIndex = key.DayIndex, PositionId = key.PositionId, PersonLeft = null, PersonRight = rightPersons.First(), DiffType = "新增班次" });
                    }
                    else if (leftPersons.Any() && !rightPersons.Any())
                    {
                        Differences.Add(new ShiftDiff { DayIndex = key.DayIndex, PositionId = key.PositionId, PersonLeft = leftPersons.First(), PersonRight = null, DiffType = "删除班次" });
                    }
                    else if (leftPersons.Any() && rightPersons.Any())
                    {
                        if (!leftPersons.SequenceEqual(rightPersons))
                        {
                            Differences.Add(new ShiftDiff { DayIndex = key.DayIndex, PositionId = key.PositionId, PersonLeft = leftPersons.First(), PersonRight = rightPersons.First(), DiffType = "人员变更" });
                        }
                    }
                }
            }

            HasCompared = Differences.Any();
            IsLoading = false;
        }

        [RelayCommand]
        private void ChangeMode(CompareMode mode)
        {
            Mode = mode;
        }
    }
}
