using System;

namespace AutoScheduling3.DTOs.Comparison
{
    /// <summary>
    /// 班次差异数据传输对象
    /// </summary>
    public class ShiftDiffDto
    {
        /// <summary>
        /// 日期索引（从排班开始日期的天数，0-based）
        /// </summary>
        public int DayIndex { get; set; }

        /// <summary>
        /// 具体日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 哨位ID
        /// </summary>
        public int PositionId { get; set; }

        /// <summary>
        /// 哨位名称
        /// </summary>
        public string PositionName { get; set; } = string.Empty;

        /// <summary>
        /// 时段索引（0-11）
        /// </summary>
        public int PeriodIndex { get; set; }

        /// <summary>
        /// 时段范围描述（如 "00:00-02:00"）
        /// </summary>
        public string TimeRange { get; set; } = string.Empty;

        /// <summary>
        /// 排班表1中的人员ID（可为空）
        /// </summary>
        public int? PersonnelId1 { get; set; }

        /// <summary>
        /// 排班表1中的人员姓名（可为空）
        /// </summary>
        public string? PersonnelName1 { get; set; }

        /// <summary>
        /// 排班表2中的人员ID（可为空）
        /// </summary>
        public int? PersonnelId2 { get; set; }

        /// <summary>
        /// 排班表2中的人员姓名（可为空）
        /// </summary>
        public string? PersonnelName2 { get; set; }

        /// <summary>
        /// 差异类型
        /// </summary>
        public DiffType Type { get; set; }
    }
}
