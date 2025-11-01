using AutoScheduling3.Data;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.Mappers;
using AutoScheduling3.History;
using AutoScheduling3.Models;
using AutoScheduling3.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace AutoScheduling3.Services
{
    public class HistoryService : IHistoryService
    {
        private readonly IHistoryManagement _historyManagement;
        private readonly IPersonalRepository _personalRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly PersonnelMapper _personnelMapper;
        private readonly PositionMapper _positionMapper;
        private readonly IConstraintRepository _constraintRepository;

        public HistoryService(IHistoryManagement historyManagement, IPersonalRepository personalRepository, IPositionRepository positionRepository, PersonnelMapper personnelMapper, PositionMapper positionMapper, IConstraintRepository constraintRepository)
        {
            _historyManagement = historyManagement;
            _personalRepository = personalRepository;
            _positionRepository = positionRepository;
            _personnelMapper = personnelMapper;
            _positionMapper = positionMapper;
            _constraintRepository = constraintRepository;
        }

        public async Task<IEnumerable<HistoryScheduleDto>> GetHistorySchedulesAsync(HistoryQueryOptions options)
        {
            var historyItems = await _historyManagement.GetAllHistorySchedulesAsync();
            var query = historyItems.AsQueryable();

            if (options.StartDate.HasValue)
                query = query.Where(h => h.ConfirmTime.Date >= options.StartDate.Value.Date);
            if (options.EndDate.HasValue)
                query = query.Where(h => h.ConfirmTime.Date <= options.EndDate.Value.Date);
            if (!string.IsNullOrEmpty(options.Keyword))
                query = query.Where(h => h.Schedule.Header.Contains(options.Keyword, StringComparison.OrdinalIgnoreCase));

            // �����߼���Time �� Name
            if (string.Equals(options.SortBy, "Time", StringComparison.OrdinalIgnoreCase))
            {
                query = options.IsAscending
                    ? query.OrderBy(h => h.ConfirmTime)
                    : query.OrderByDescending(h => h.ConfirmTime);
            }
            else if (string.Equals(options.SortBy, "Name", StringComparison.OrdinalIgnoreCase) || string.Equals(options.SortBy, "Title", StringComparison.OrdinalIgnoreCase))
            {
                query = options.IsAscending
                    ? query.OrderBy(h => h.Schedule.Header)
                    : query.OrderByDescending(h => h.Schedule.Header);
            }
            else
            {
                // Ĭ�ϰ�ʱ�䵹��
                query = query.OrderByDescending(h => h.ConfirmTime);
            }

            return query.Select(h => new HistoryScheduleDto
            {
                Id = h.Schedule.Id,
                Name = h.Schedule.Header,
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
                return null;
            var schedule = historyItem.Value.Schedule;
            var confirmTime = historyItem.Value.ConfirmTime;
            var personnel = await _personalRepository.GetPersonnelByIdsAsync(schedule.PersonnelIds);
            var positions = await _positionRepository.GetPositionsByIdsAsync(schedule.PositionIds);
            var holidayConfig = await _constraintRepository.GetActiveHolidayConfigAsync();

            var detailDto = new HistoryScheduleDetailDto
            {
                Id = schedule.Id,
                Name = schedule.Header,
                StartDate = schedule.StartDate,
                EndDate = schedule.EndDate,
                ConfirmTime = confirmTime,
                NumberOfPersonnel = schedule.PersonnelIds.Count,
                NumberOfPositions = schedule.PositionIds.Count,
                Personnel = (await _personnelMapper.ToDtoListAsync(personnel)).ToList(),
                Positions = positions.Select(p => _positionMapper.ToDto(p)).ToList(),
                Statistics = new ScheduleStatisticsDto(),
                Shifts = schedule.Results.ToList()
            };

            detailDto.Statistics.TotalShifts = schedule.Results.Count;
            detailDto.Statistics.AverageShiftsPerPerson = schedule.PersonnelIds.Any() ? (double)schedule.Results.Count / schedule.PersonnelIds.Count : 0;
            detailDto.Statistics.WeekendShifts = holidayConfig != null
                ? schedule.Results.Count(s => holidayConfig.IsHoliday(schedule.StartDate.AddDays(s.DayIndex)))
                : 0;
            detailDto.Statistics.ShiftsPerPerson = schedule.Results
                .GroupBy(s => s.PersonnelId)
                .ToDictionary(g => personnel.FirstOrDefault(p => p.Id == g.Key)?.Name ?? "Unknown", g => g.Count());

            return detailDto;
        }

        public async Task<Tuple<HistoryScheduleDetailDto, HistoryScheduleDetailDto>> GetSchedulesForComparisonAsync(int scheduleId1, int scheduleId2)
        {
            var detail1 = await GetHistoryScheduleDetailAsync(scheduleId1);
            var detail2 = await GetHistoryScheduleDetailAsync(scheduleId2);
            return new Tuple<HistoryScheduleDetailDto, HistoryScheduleDetailDto>(detail1, detail2);
        }
    }
}
