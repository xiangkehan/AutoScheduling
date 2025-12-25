using System;
using System.Collections.Generic;

namespace AutoScheduling3.DTOs.Comparison
{
    /// <summary>
    /// 对比结果数据传输对象
    /// </summary>
    public class ComparisonResultDto
    {
        /// <summary>
        /// 排班表1详情
        /// </summary>
        public HistoryScheduleDetailDto Schedule1 { get; set; } = null!;

        /// <summary>
        /// 排班表2详情
        /// </summary>
        public HistoryScheduleDetailDto Schedule2 { get; set; } = null!;

        /// <summary>
        /// 差异列表
        /// </summary>
        public List<ShiftDiffDto> Differences { get; set; } = new();

        /// <summary>
        /// 对比统计
        /// </summary>
        public ComparisonStatisticsDto Statistics { get; set; } = new();

        /// <summary>
        /// 人员变更分析
        /// </summary>
        public List<PersonnelChangeDto> PersonnelChanges { get; set; } = new();

        /// <summary>
        /// 重叠日期范围
        /// </summary>
        public DateRange? OverlappingDateRange { get; set; }
    }

    /// <summary>
    /// 日期范围
    /// </summary>
    public class DateRange
    {
        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// 是否有效（开始日期 <= 结束日期）
        /// </summary>
        public bool IsValid => StartDate <= EndDate;

        /// <summary>
        /// 天数
        /// </summary>
        public int Days => IsValid ? (EndDate - StartDate).Days + 1 : 0;
    }
}
