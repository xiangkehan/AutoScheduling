using System;
using System.Collections.Generic;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班表数据传输对象
/// </summary>
public class ScheduleDto
{
    /// <summary>
    /// 排班表ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 排班表名称
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 参与人员ID列表
    /// </summary>
    public List<int> PersonnelIds { get; set; } = new();

    /// <summary>
    /// 参与哨位ID列表
    /// </summary>
    public List<int> PositionIds { get; set; } = new();

    /// <summary>
    /// 单次排班列表
    /// </summary>
    public List<ShiftDto> Shifts { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 确认时间（草稿为null）
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime EndDate { get; set; }
}

/// <summary>
/// 单次班次数据传输对象
/// </summary>
public class ShiftDto
{
    /// <summary>
    /// 班次ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 所属排班表ID
    /// </summary>
    public int ScheduleId { get; set; }

    /// <summary>
    /// 哨位ID
    /// </summary>
    public int PositionId { get; set; }

    /// <summary>
    /// 哨位名称（冗余字段，便于显示）
    /// </summary>
    public string PositionName { get; set; } = string.Empty;

    /// <summary>
    /// 人员ID
    /// </summary>
    public int PersonnelId { get; set; }

    /// <summary>
    /// 人员姓名（冗余字段，便于显示）
    /// </summary>
    public string PersonnelName { get; set; } = string.Empty;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 时段索引（0-11，对应12个时段）
    /// </summary>
    public int PeriodIndex { get; set; }
}

/// <summary>
/// 排班表摘要数据传输对象
/// </summary>
public class ScheduleSummaryDto
{
    /// <summary>
    /// 排班表ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 排班表名称
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 人员数量
    /// </summary>
    public int PersonnelCount { get; set; }

    /// <summary>
    /// 哨位数量
    /// </summary>
    public int PositionCount { get; set; }

    /// <summary>
    /// 班次数量
    /// </summary>
    public int ShiftCount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 确认时间（草稿为null）
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }
}

/// <summary>
/// 排班请求数据传输对象
/// </summary>
public class SchedulingRequestDto
{
    /// <summary>
    /// 排班表名称（必填，1-100字符）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 开始日期（必填，不早于今天）
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期（必填，不早于开始日期）
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 参与人员ID列表（必填，至少1人）
    /// </summary>
    public List<int> PersonnelIds { get; set; } = new();

    /// <summary>
    /// 参与哨位ID列表（必填，至少1个）
    /// </summary>
    public List<int> PositionIds { get; set; } = new();

    /// <summary>
    /// 是否使用活动的休息日配置（默认true）
    /// </summary>
    public bool UseActiveHolidayConfig { get; set; } = true;

    /// <summary>
    /// 启用的定岗规则ID列表（可选）
    /// </summary>
    public List<int>? EnabledFixedRuleIds { get; set; }

    /// <summary>
    /// 启用的手动指定ID列表（可选，将根据日期范围过滤）
    /// </summary>
    public List<int>? EnabledManualAssignmentIds { get; set; }
    public int? HolidayConfigId { get; set; }
}
