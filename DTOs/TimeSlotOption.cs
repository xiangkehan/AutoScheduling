using System.Collections.Generic;

namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 时段选项，用于手动指定对话框中的时段选择
    /// </summary>
    public class TimeSlotOption
    {
        public int Index { get; set; }
        public string DisplayText { get; set; } = string.Empty;

        public static List<TimeSlotOption> GetAll()
        {
            var options = new List<TimeSlotOption>();
            for (int i = 0; i < 12; i++)
            {
                var startHour = i * 2;
                var endHour = (i + 1) * 2;
                options.Add(new TimeSlotOption
                {
                    Index = i,
                    DisplayText = $"时段 {i} ({startHour:D2}:00-{endHour:D2}:00)"
                });
            }
            return options;
        }
    }
}
