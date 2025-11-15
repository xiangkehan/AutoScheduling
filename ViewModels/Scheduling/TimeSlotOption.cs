using System.Collections.Generic;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// 时段选项 - 用于时段选择下拉列表
    /// </summary>
    public class TimeSlotOption
    {
        /// <summary>
        /// 时段索引（0-11）
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 显示文本（例如："时段 0 (00:00-02:00)"）
        /// </summary>
        public string DisplayText { get; set; } = string.Empty;

        /// <summary>
        /// 获取所有时段选项
        /// </summary>
        /// <returns>12个时段选项的列表</returns>
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
