using System;

namespace AutoScheduling3.DTOs
{
    public class HistoryScheduleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int NumberOfPersonnel { get; set; }
        public int NumberOfPositions { get; set; }
        public DateTime ConfirmTime { get; set; }
    }
}
