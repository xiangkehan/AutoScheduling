using System.Collections.Generic;

namespace AutoScheduling3.DTOs
{
    public class HistoryScheduleDetailDto : HistoryScheduleDto
    {
        public List<PersonnelDto> Personnel { get; set; } = new();
        public List<PositionDto> Positions { get; set; } = new();
        public List<List<string>> ScheduleGrid { get; set; } = new();
        public ScheduleStatisticsDto Statistics { get; set; } = new();
    }
}
