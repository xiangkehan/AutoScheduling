using System;
using System.Collections.Generic;

namespace AutoScheduling3.DTOs.Comparison
{
    /// <summary>
    /// 对比结果导出数据传输对象（用于 JSON 导出）
    /// </summary>
    public class ComparisonExportDto
    {
        /// <summary>
        /// 导出时间
        /// </summary>
        public DateTime ExportTime { get; set; }

        /// <summary>
        /// 排班表1信息
        /// </summary>
        public ScheduleInfoDto Schedule1Info { get; set; } = null!;

        /// <summary>
        /// 排班表2信息
        /// </summary>
        public ScheduleInfoDto Schedule2Info { get; set; } = null!;

        /// <summary>
        /// 重叠日期范围
        /// </summary>
        public DateRange? OverlappingDateRange { get; set; }

        /// <summary>
        /// 对比统计
        /// </summary>
        public ComparisonStatisticsDto Statistics { get; set; } = new();

        /// <summary>
        /// 差异列表
        /// </summary>
        public List<ShiftDiffDto> Differences { get; set; } = new();

        /// <summary>
        /// 人员变更分析（简化版，不含详细变更列表）
        /// </summary>
        public List<PersonnelChangeExportDto> PersonnelChanges { get; set; } = new();
    }

    /// <summary>
    /// 排班表信息（用于导出）
    /// </summary>
    public class ScheduleInfoDto
    {
        /// <summary>
        /// 排班表ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 排班表标题
        /// </summary>
        public string Header { get; set; } = string.Empty;

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// 确认时间
        /// </summary>
        public DateTime ConfirmTime { get; set; }
    }

    /// <summary>
    /// 人员变更导出数据传输对象（简化版）
    /// </summary>
    public class PersonnelChangeExportDto
    {
        /// <summary>
        /// 人员ID
        /// </summary>
        public int PersonnelId { get; set; }

        /// <summary>
        /// 人员姓名
        /// </summary>
        public string PersonnelName { get; set; } = string.Empty;

        /// <summary>
        /// 在排班表1中的班次数
        /// </summary>
        public int ShiftsInSchedule1 { get; set; }

        /// <summary>
        /// 在排班表2中的班次数
        /// </summary>
        public int ShiftsInSchedule2 { get; set; }

        /// <summary>
        /// 新增班次数
        /// </summary>
        public int AddedShifts { get; set; }

        /// <summary>
        /// 减少班次数
        /// </summary>
        public int RemovedShifts { get; set; }

        /// <summary>
        /// 净变化
        /// </summary>
        public int NetChange { get; set; }

        /// <summary>
        /// 人员状态
        /// </summary>
        public PersonnelStatus Status { get; set; }
    }
}
