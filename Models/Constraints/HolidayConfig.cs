using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.Models.Constraints
{
    /// <summary>
    /// 休息日配置模型：定义如何判定休息日的规则配置
    /// </summary>
    public class HolidayConfig
    {
        /// <summary>
        /// 数据库主键ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 配置名称（如"2024年休息日配置"）
        /// </summary>
        public string ConfigName { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用周末规则
        /// </summary>
        public bool EnableWeekendRule { get; set; } = true;

        /// <summary>
        /// 周末日期列表（如 [Saturday, Sunday]）
        /// </summary>
        public List<DayOfWeek> WeekendDays { get; set; } = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };

        /// <summary>
        /// 法定节假日日期列表
        /// </summary>
        public List<DateTime> LegalHolidays { get; set; } = new List<DateTime>();

        /// <summary>
        /// 自定义休息日日期列表
        /// </summary>
        public List<DateTime> CustomHolidays { get; set; } = new List<DateTime>();

        /// <summary>
        /// 排除日期列表（强制为工作日，优先级最高）
        /// </summary>
        public List<DateTime> ExcludedDates { get; set; } = new List<DateTime>();

        /// <summary>
        /// 是否为当前启用配置
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 判定指定日期是否为休息日
        /// </summary>
        /// <param name="date">要判定的日期</param>
        /// <returns>是否为休息日</returns>
        public bool IsHoliday(DateTime date)
        {
            // 标准化为日期部分（忽略时间）
            var dateOnly = date.Date;

            // 第一优先级：检查排除列表（强制为工作日）
            if (ExcludedDates.Exists(d => d.Date == dateOnly))
                return false;

            // 第二优先级：检查自定义休息日
            if (CustomHolidays.Exists(d => d.Date == dateOnly))
                return true;

            // 第三优先级：检查法定节假日
            if (LegalHolidays.Exists(d => d.Date == dateOnly))
                return true;

            // 第四优先级：检查周末规则
            if (EnableWeekendRule && WeekendDays.Contains(date.DayOfWeek))
                return true;

            // 默认为工作日
            return false;
        }

        public override string ToString()
        {
            return $"HolidayConfig[{Id}] {ConfigName} (Active={IsActive})";
        }
    }
}
