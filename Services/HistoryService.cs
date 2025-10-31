using AutoScheduling3.Data;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.Services
{
    public class HistoryService : IHistoryService
    {
        private readonly HistoryManagement _historyManagement;
        private readonly SchedulingRepository _schedulingRepository;
        private readonly IPersonalRepository _personalRepository;
        private readonly IPositionRepository _positionRepository;


        public HistoryService(HistoryManagement historyManagement, SchedulingRepository schedulingRepository, IPersonalRepository personalRepository, IPositionRepository positionRepository)
        {
            _historyManagement = historyManagement;
            _schedulingRepository = schedulingRepository;
            _personalRepository = personalRepository;
            _positionRepository = positionRepository;
        }

        public async Task<IEnumerable<HistoryScheduleDto>> GetHistorySchedulesAsync(HistoryQueryOptions options)
        {
            var historySchedules = await _historyManagement.GetAllHistorySchedulesAsync();

            var query = historySchedules.Select(h => new HistoryScheduleDto
            {
                Id = h.Schedule.Id,
                Name = h.Schedule.Name,
                StartDate = h.Schedule.StartDate,
                EndDate = h.Schedule.EndDate,
                NumberOfPersonnel = h.Schedule.PersonnelIds.Count,
                NumberOfPositions = h.Schedule.PositionIds.Count,
                ConfirmTime = h.ConfirmTime
            });

            if (options.StartDate.HasValue)
            {
                query = query.Where(h => h.ConfirmTime.Date >= options.StartDate.Value.Date);
            }

            if (options.EndDate.HasValue)
            {
                query = query.Where(h => h.ConfirmTime.Date <= options.EndDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(options.Keyword))
            {
                query = query.Where(h => h.Name.Contains(options.Keyword, System.StringComparison.OrdinalIgnoreCase));
            }

            if (options.SortBy == "Name")
            {
                query = options.IsAscending ? query.OrderBy(h => h.Name) : query.OrderByDescending(h => h.Name);
            }
            else // Default sort by time
            {
                query = options.IsAscending ? query.OrderBy(h => h.ConfirmTime) : query.OrderByDescending(h => h.ConfirmTime);
            }

            return query.ToList();
        }

        public async Task<HistoryScheduleDetailDto> GetHistoryScheduleDetailAsync(int scheduleId)
        {
            var schedule = await _schedulingRepository.GetScheduleAsync(scheduleId);
            if (schedule == null)
            {
                return null;
            }

            var historySchedules = await _historyManagement.GetAllHistorySchedulesAsync();
            var confirmTime = historySchedules.FirstOrDefault(h => h.Schedule.Id == scheduleId).ConfirmTime;

            var personnel = await _personalRepository.GetPersonnelByIdsAsync(schedule.PersonnelIds);
            var positions = await _positionRepository.GetPositionsByIdsAsync(schedule.PositionIds);

            var detailDto = new HistoryScheduleDetailDto
            {
                Id = schedule.Id,
                Name = schedule.Name,
                StartDate = schedule.StartDate,
                EndDate = schedule.EndDate,
                NumberOfPersonnel = schedule.PersonnelIds.Count,
                NumberOfPositions = schedule.PositionIds.Count,
                ConfirmTime = confirmTime,
                PersonnelNames = personnel.Select(p => p.Name).ToList(),
                PositionNames = positions.Select(p => p.Name).ToList(),
                Statistics = new ScheduleStatisticsDto()
            };

            // Populate statistics
            detailDto.Statistics.TotalShifts = schedule.Shifts.Count;
            detailDto.Statistics.AverageShiftsPerPerson = schedule.PersonnelIds.Any() ? (double)schedule.Shifts.Count / schedule.PersonnelIds.Count : 0;
            
            // Calculate ShiftsPerPerson
            var personnelDict = personnel.ToDictionary(p => p.Id, p => p.Name);
            detailDto.Statistics.ShiftsPerPerson = schedule.Shifts
                .GroupBy(s => s.PersonId)
                .ToDictionary(g => personnelDict.GetValueOrDefault(g.Key, "Î´ÖªÈËÔ±"), g => g.Count());

            // Calculate ShiftsPerTimeSlot
            var shiftsPerTimeSlot = new Dictionary<string, int>();
            for (int i = 0; i < 12; i++)
            {
                shiftsPerTimeSlot.Add($"{i * 2:D2}-{(i + 1) * 2:D2}", 0);
            }

            foreach (var shift in schedule.Shifts)
            {
                int slotIndex = shift.StartTime.Hour / 2;
                string slotKey = $"{slotIndex * 2:D2}-{(slotIndex + 1) * 2:D2}";
                if (shiftsPerTimeSlot.ContainsKey(slotKey))
                {
                    shiftsPerTimeSlot[slotKey]++;
                }
            }
            detailDto.Statistics.ShiftsPerTimeSlot = shiftsPerTimeSlot;

            // TODO: Implement other statistics calculation
            // detailDto.Statistics.HolidayShifts = ...
            // detailDto.Statistics.PositionCoverage = ...

            return detailDto;
        }
    }
}
