using AutoScheduling3.Data;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.Mappers;
using AutoScheduling3.History;
using AutoScheduling3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.Services
{
    public class HistoryService : IHistoryService
    {
        private readonly HistoryManagement _historyManagement;
        private readonly IPersonalRepository _personalRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly PersonnelMapper _personnelMapper;
        private readonly PositionMapper _positionMapper;

        public HistoryService(HistoryManagement historyManagement, IPersonalRepository personalRepository, IPositionRepository positionRepository, PersonnelMapper personnelMapper, PositionMapper positionMapper)
        {
            _historyManagement = historyManagement;
            _personalRepository = personalRepository;
            _positionRepository = positionRepository;
            _personnelMapper = personnelMapper;
            _positionMapper = positionMapper;
        }

        public async Task<IEnumerable<HistoryScheduleDto>> GetHistorySchedulesAsync(HistoryQueryOptions options)
        {
            var historyItems = await _historyManagement.GetAllHistorySchedulesAsync();

            var query = historyItems.AsQueryable();

            if (options.StartDate.HasValue)
            {
                query = query.Where(h => h.ConfirmTime.Date >= options.StartDate.Value.Date);
            }
            if (options.EndDate.HasValue)
            {
                query = query.Where(h => h.ConfirmTime.Date <= options.EndDate.Value.Date);
            }
            if (!string.IsNullOrEmpty(options.Keyword))
            {
                query = query.Where(h => h.Schedule.Name.Contains(options.Keyword, StringComparison.OrdinalIgnoreCase));
            }

            query = (options.SortBy, options.IsAscending) switch
            {
                ("Name", true) => query.OrderBy(h => h.Schedule.Name),
                ("Name", false) => query.OrderByDescending(h => h.Schedule.Name),
                _ => query.OrderByDescending(h => h.ConfirmTime)
            };

            return query.Select(h => new HistoryScheduleDto
            {
                Id = h.Schedule.Id,
                Name = h.Schedule.Name,
                StartDate = h.Schedule.StartDate,
                EndDate = h.Schedule.EndDate,
                ConfirmTime = h.ConfirmTime,
                NumberOfPersonnel = h.Schedule.PersonnelIds.Count,
                NumberOfPositions = h.Schedule.PositionIds.Count
            }).ToList();
        }

        public async Task<HistoryScheduleDetailDto> GetHistoryScheduleDetailAsync(int scheduleId)
        {
            var historyItem = await _historyManagement.GetHistoryScheduleByScheduleIdAsync(scheduleId);
            if (historyItem == null)
            {
                return null;
            }

            var schedule = historyItem.Schedule;
            var personnel = await _personalRepository.GetPersonnelByIdsAsync(schedule.PersonnelIds);
            var positions = await _positionRepository.GetPositionsByIdsAsync(schedule.PositionIds);

            var detailDto = new HistoryScheduleDetailDto
            {
                Id = schedule.Id,
                Name = schedule.Name,
                StartDate = schedule.StartDate,
                EndDate = schedule.EndDate,
                ConfirmTime = historyItem.ConfirmTime,
                NumberOfPersonnel = schedule.PersonnelIds.Count,
                NumberOfPositions = schedule.PositionIds.Count,
                Personnel = personnel.Select(_personnelMapper.ToDto).ToList(),
                Positions = positions.Select(_positionMapper.ToDto).ToList(),
                Statistics = new ScheduleStatisticsDto()
            };

            // Populate statistics
            detailDto.Statistics.TotalShifts = schedule.Shifts.Count;
            detailDto.Statistics.AverageShiftsPerPerson = schedule.PersonnelIds.Any() ? (double)schedule.Shifts.Count / schedule.PersonnelIds.Count : 0;
            
            // This is a placeholder for weekend shifts logic
            detailDto.Statistics.WeekendShifts = 0; 

            detailDto.Statistics.ShiftsPerPerson = schedule.Shifts
                .GroupBy(s => s.PersonId)
                .ToDictionary(g => personnel.FirstOrDefault(p => p.Id == g.Key)?.Name ?? "Unknown", g => g.Count());

            return detailDto;
        }
    }
}
