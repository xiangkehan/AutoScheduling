using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班表数据传输对象
/// </summary>
public class ScheduleDto
{
    /// <summary>
    /// 排班表ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 排班表名称
    /// </summary>
    [Required(ErrorMessage = "排班表名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "排班表名称长度必须在1-100字符之间")]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 参与人员ID列表
    /// </summary>
    [Required(ErrorMessage = "人员列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一名人员")]
    [JsonPropertyName("personnelIds")]
    public List<int> PersonnelIds { get; set; } = new();

    /// <summary>
    /// 参与哨位ID列表
    /// </summary>
    [Required(ErrorMessage = "哨位列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一个哨位")]
    [JsonPropertyName("positionIds")]
    public List<int> PositionIds { get; set; } = new();

    /// <summary>
    /// 单次排班列表
    /// </summary>
    [Required(ErrorMessage = "班次列表不能为空")]
    [JsonPropertyName("shifts")]
    public List<ShiftDto> Shifts { get; set; } = new();

    /// <summary>
    /// 冲突/约束提示集合（前端面板展示）
    /// </summary>
    [JsonPropertyName("conflicts")]
    public List<ConflictDto> Conflicts { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required(ErrorMessage = "创建时间不能为空")]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 确认时间（草稿为null）
    /// </summary>
    [JsonPropertyName("confirmedAt")]
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// 开始日期
    /// </summary>
    [Required(ErrorMessage = "开始日期不能为空")]
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    [Required(ErrorMessage = "结束日期不能为空")]
    [JsonPropertyName("endDate")]
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
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 所属排班表ID
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "排班表ID必须大于0")]
    [JsonPropertyName("scheduleId")]
    public int ScheduleId { get; set; }

    /// <summary>
    /// 哨位ID
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "哨位ID必须大于0")]
    [JsonPropertyName("positionId")]
    public int PositionId { get; set; }

    /// <summary>
    /// 哨位名称（冗余字段，便于显示）
    /// </summary>
    [JsonPropertyName("positionName")]
    public string PositionName { get; set; } = string.Empty;

    /// <summary>
    /// 人员ID
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "人员ID必须大于0")]
    [JsonPropertyName("personnelId")]
    public int PersonnelId { get; set; }

    /// <summary>
    /// 人员姓名（冗余字段，便于显示）
    /// </summary>
    [JsonPropertyName("personnelName")]
    public string PersonnelName { get; set; } = string.Empty;

    /// <summary>
    /// 开始时间
    /// </summary>
    [Required(ErrorMessage = "开始时间不能为空")]
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [Required(ErrorMessage = "结束时间不能为空")]
    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 时段索引（0-11，对应12个时段）
    /// </summary>
    [Range(0, 11, ErrorMessage = "时段索引必须在0-11之间")]
    [JsonPropertyName("periodIndex")]
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
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 排班表名称
    /// </summary>
    [Required(ErrorMessage = "排班表名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "排班表名称长度必须在1-100字符之间")]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 开始日期
    /// </summary>
    [Required(ErrorMessage = "开始日期不能为空")]
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    [Required(ErrorMessage = "结束日期不能为空")]
    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 人员数量
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "人员数量不能为负数")]
    [JsonPropertyName("personnelCount")]
    public int PersonnelCount { get; set; }

    /// <summary>
    /// 哨位数量
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "哨位数量不能为负数")]
    [JsonPropertyName("positionCount")]
    public int PositionCount { get; set; }

    /// <summary>
    /// 班次数量
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "班次数量不能为负数")]
    [JsonPropertyName("shiftCount")]
    public int ShiftCount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required(ErrorMessage = "创建时间不能为空")]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 确认时间（草稿为null）
    /// </summary>
    [JsonPropertyName("confirmedAt")]
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
    [Required(ErrorMessage = "排班表名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "排班表名称长度必须在1-100字符之间")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 开始日期（必填，不早于今天）
    /// </summary>
    [Required(ErrorMessage = "开始日期不能为空")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期（必填，不早于开始日期）
    /// </summary>
    [Required(ErrorMessage = "结束日期不能为空")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 参与人员ID列表（必填，至少1人）
    /// </summary>
    [Required(ErrorMessage = "人员列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一名人员")]
    public List<int> PersonnelIds { get; set; } = new();

    /// <summary>
    /// 参与哨位ID列表（必填，至少1个）
    /// </summary>
    [Required(ErrorMessage = "哨位列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一个哨位")]
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

/// <summary>
/// 冲突/约束提示 DTO
/// </summary>
public class ConflictDto
{
    /// <summary>
    /// 冲突类型：hard / soft / info / unassigned
    /// </summary>
    [Required(ErrorMessage = "冲突类型不能为空")]
    [RegularExpression("^(hard|soft|info|unassigned)$", ErrorMessage = "冲突类型必须是 hard、soft、info 或 unassigned")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 冲突描述
    /// </summary>
    [Required(ErrorMessage = "冲突描述不能为空")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "冲突描述长度必须在1-500字符之间")]
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 相关哨位ID
    /// </summary>
    [JsonPropertyName("positionId")]
    public int? PositionId { get; set; }

    /// <summary>
    /// 相关人员ID
    /// </summary>
    [JsonPropertyName("personnelId")]
    public int? PersonnelId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 时段索引
    /// </summary>
    [Range(0, 11, ErrorMessage = "时段索引必须在0-11之间")]
    [JsonPropertyName("periodIndex")]
    public int? PeriodIndex { get; set; }
}
