using AutoScheduling3.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Services.Interfaces
{
    public interface IHistoryService
    {
        Task<IEnumerable<HistoryScheduleDto>> GetHistorySchedulesAsync(HistoryQueryOptions options);
        Task<HistoryScheduleDetailDto> GetHistoryScheduleDetailAsync(int scheduleId);
    }

    public class HistoryQueryOptions
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Keyword { get; set; }
        public string SortBy { get; set; } // "Time" or "Name"
        public bool IsAscending { get; set; }
    }
}
