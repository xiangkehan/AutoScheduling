using System.Collections.Generic;

namespace AutoScheduling3.DTOs
{
    public class ScheduleStatisticsDto
    {
        public int TotalShifts { get; set; }
        public double AverageShiftsPerPerson { get; set; }
        public int WeekendShifts { get; set; }
        public Dictionary<string, int> ShiftsByTimeOfDay { get; set; } = new();
        public Dictionary<string, int> ShiftsPerPerson { get; set; } = new();
        public double PositionCoverage { get; set; }
    }
}
