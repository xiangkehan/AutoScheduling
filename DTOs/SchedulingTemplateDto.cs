namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班模板数据传输对象
/// </summary>
public class SchedulingTemplateDto
{
    /// <summary>
    /// 模板ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 模板类型（regular/holiday/special）
    /// </summary>
    public string TemplateType { get; set; } = "regular";

    /// <summary>
    /// 是否为默认模板
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
    /// 休息日配置ID（可选）
    /// </summary>
    public int? HolidayConfigId { get; set; }

    /// <summary>
    /// 是否使用当前活动配置
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
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; }
}

/// <summary>
/// 创建模板DTO
/// </summary>
public class CreateTemplateDto
{
    /// <summary>
    /// 模板名称（必填，1-100字符）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述（可选，最多500字符）
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 模板类型（必选：regular/holiday/special）
    /// </summary>
    public string TemplateType { get; set; } = "regular";

    /// <summary>
    /// 是否设为默认
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 参与人员ID列表（至少1人）
    /// </summary>
    public List<int> PersonnelIds { get; set; } = new();

    /// <summary>
    /// 参与哨位ID列表（至少1个）
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
