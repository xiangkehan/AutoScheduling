using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班模板数据传输对象
/// </summary>
public class SchedulingTemplateDto
{
    /// <summary>
    /// 模板ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    [Required(ErrorMessage = "模板名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "模板名称长度必须在1-100字符之间")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    [StringLength(500, ErrorMessage = "模板描述长度不能超过500字符")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 模板类型（regular/holiday/special）
    /// </summary>
    [Required(ErrorMessage = "模板类型不能为空")]
    [RegularExpression("^(regular|holiday|special)$", ErrorMessage = "模板类型必须是 regular、holiday 或 special")]
    [JsonPropertyName("templateType")]
    public string TemplateType { get; set; } = "regular";

    /// <summary>
    /// 是否为默认模板
    /// </summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

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
    /// 休息日配置ID（可选）
    /// </summary>
    [JsonPropertyName("holidayConfigId")]
    public int? HolidayConfigId { get; set; }

    /// <summary>
    /// 是否使用当前活动配置
    /// </summary>
    [JsonPropertyName("useActiveHolidayConfig")]
    public bool UseActiveHolidayConfig { get; set; }

    /// <summary>
    /// 启用的定岗规则ID
    /// </summary>
    [JsonPropertyName("enabledFixedRuleIds")]
    public List<int> EnabledFixedRuleIds { get; set; } = new();

    /// <summary>
    /// 启用的手动指定ID
    /// </summary>
    [JsonPropertyName("enabledManualAssignmentIds")]
    public List<int> EnabledManualAssignmentIds { get; set; } = new();

    /// <summary>
    /// 排班天数
    /// </summary>
    [Range(1, 365, ErrorMessage = "排班天数必须在1-365之间")]
    [JsonPropertyName("durationDays")]
    public int DurationDays { get; set; } = 1;

    /// <summary>
    /// 排班策略配置（JSON格式）
    /// </summary>
    [JsonPropertyName("strategyConfig")]
    public string StrategyConfig { get; set; } = string.Empty;

    /// <summary>
    /// 使用次数
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "使用次数不能为负数")]
    [JsonPropertyName("usageCount")]
    public int UsageCount { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Required(ErrorMessage = "创建时间不能为空")]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 最后使用时间
    /// </summary>
    [JsonPropertyName("lastUsedAt")]
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// 创建模板DTO
/// </summary>
public class CreateTemplateDto
{
    /// <summary>
    /// 模板名称（必填，1-100字符）
    /// </summary>
    [Required(ErrorMessage = "模板名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "模板名称长度必须在1-100字符之间")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述（可选，最多500字符）
    /// </summary>
    [StringLength(500, ErrorMessage = "模板描述长度不能超过500字符")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 模板类型（必选：regular/holiday/special）
    /// </summary>
    [Required(ErrorMessage = "模板类型不能为空")]
    [RegularExpression("^(regular|holiday|special)$", ErrorMessage = "模板类型必须是 regular、holiday 或 special")]
    [JsonPropertyName("templateType")]
    public string TemplateType { get; set; } = "regular";

    /// <summary>
    /// 是否设为默认
    /// </summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 参与人员ID列表（至少1人）
    /// </summary>
    [Required(ErrorMessage = "人员列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一名人员")]
    [JsonPropertyName("personnelIds")]
    public List<int> PersonnelIds { get; set; } = new();

    /// <summary>
    /// 参与哨位ID列表（至少1个）
    /// </summary>
    [Required(ErrorMessage = "哨位列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一个哨位")]
    [JsonPropertyName("positionIds")]
    public List<int> PositionIds { get; set; } = new();

    /// <summary>
    /// 休息日配置ID
    /// </summary>
    [JsonPropertyName("holidayConfigId")]
    public int? HolidayConfigId { get; set; }

    /// <summary>
    /// 是否使用活动配置
    /// </summary>
    [JsonPropertyName("useActiveHolidayConfig")]
    public bool UseActiveHolidayConfig { get; set; }

    /// <summary>
    /// 启用的定岗规则ID
    /// </summary>
    [JsonPropertyName("enabledFixedRuleIds")]
    public List<int> EnabledFixedRuleIds { get; set; } = new();

    /// <summary>
    /// 启用的手动指定ID
    /// </summary>
    [JsonPropertyName("enabledManualAssignmentIds")]
    public List<int> EnabledManualAssignmentIds { get; set; } = new();

    /// <summary>
    /// 排班模式（可选）
    /// </summary>
    [JsonPropertyName("schedulingMode")]
    public SchedulingMode? SchedulingMode { get; set; }

    /// <summary>
    /// 遗传算法配置（可选，仅在 Hybrid 模式下使用）
    /// </summary>
    [JsonPropertyName("geneticAlgorithmConfig")]
    public GeneticAlgorithmConfigDto? GeneticAlgorithmConfig { get; set; }
}

/// <summary>
/// 更新模板DTO
/// </summary>
public class UpdateTemplateDto
{
    /// <summary>
    /// 模板名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 模板类型
    /// </summary>
    public string TemplateType { get; set; } = "regular";

    /// <summary>
    /// 是否设为默认
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 参与人员ID列表
    /// </summary>
    public List<int> PersonnelIds { get; set; } = new();

    /// <summary>
    /// 参与哨位ID列表
    /// </summary>
    public List<int> PositionIds { get; set; } = new();

    /// <summary>
    /// 休息日配置ID
    /// </summary>
    public int? HolidayConfigId { get; set; }

    /// <summary>
    /// 是否使用活动配置
    /// </summary>
    public bool UseActiveHolidayConfig { get; set; }

    /// <summary>
    /// 启用的定岗规则ID
    /// </summary>
    public List<int> EnabledFixedRuleIds { get; set; } = new();

    /// <summary>
    /// 启用的手动指定ID
    /// </summary>
    public List<int> EnabledManualAssignmentIds { get; set; } = new();

    /// <summary>
    /// 排班模式（可选）
    /// </summary>
    public SchedulingMode? SchedulingMode { get; set; }

    /// <summary>
    /// 遗传算法配置（可选，仅在 Hybrid 模式下使用）
    /// </summary>
    public GeneticAlgorithmConfigDto? GeneticAlgorithmConfig { get; set; }
}

/// <summary>
/// 使用模板DTO
/// </summary>
public class UseTemplateDto
{
    /// <summary>
    /// 模板ID
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 排班表名称
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 覆盖人员列表（为空则使用模板配置）
    /// </summary>
    public List<int>? OverridePersonnelIds { get; set; }

    /// <summary>
    /// 覆盖哨位列表
    /// </summary>
    public List<int>? OverridePositionIds { get; set; }
}

/// <summary>
/// 模板验证结果
/// </summary>
public class TemplateValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误消息列表
    /// </summary>
    public List<ValidationMessage> Errors { get; set; } = new();

    /// <summary>
    /// 警告消息列表
    /// </summary>
    public List<ValidationMessage> Warnings { get; set; } = new();

    /// <summary>
    /// 信息消息列表
    /// </summary>
    public List<ValidationMessage> Infos { get; set; } = new();
}

/// <summary>
/// 验证消息
/// </summary>
public class ValidationMessage
{
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 相关属性
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// 相关资源ID
    /// </summary>
    public int? ResourceId { get; set; }
}
