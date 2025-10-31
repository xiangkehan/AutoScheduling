using AutoScheduling3.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace AutoScheduling3.Services.Interfaces
{
    public interface IHistoryService
    {
        Task<IEnumerable<HistoryScheduleDto>> GetHistorySchedulesAsync(HistoryQueryOptions options);
        Task<HistoryScheduleDetailDto> GetHistoryScheduleDetailAsync(int scheduleId);
        Task<Tuple<HistoryScheduleDetailDto, HistoryScheduleDetailDto>> GetSchedulesForComparisonAsync(int scheduleId1, int scheduleId2);
    }

    public class HistoryQueryOptions
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Keyword { get; set; }
        public string SortBy { get; set; } = "Time";
        public bool IsAscending { get; set; }
    }
}
