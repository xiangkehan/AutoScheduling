using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.Models;

/// <summary>
/// 排班模板数据模型 - 保存常用的排班配置以便重复使用
/// </summary>
public class SchedulingTemplate
{
    /// <summary>
    /// 模板ID
    /// </summary>
    [Key]
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
    /// 模板类型：regular(常规), holiday(节假日), special(特殊)
    /// </summary>
    public string TemplateType { get; set; } = "regular";

    /// <summary>
    /// 是否为默认模板
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 参与人员ID列表（JSON数组）
    /// </summary>
    public List<int> PersonnelIds { get; set; } = new();

    /// <summary>
    /// 参与哨位ID列表（JSON数组）
    /// </summary>
    public List<int> PositionIds { get; set; } = new();

    /// <summary>
    /// 休息日配置ID（可选）
    /// </summary>
    public int? HolidayConfigId { get; set; }

    /// <summary>
    /// 是否使用当前活动的休息日配置
    /// </summary>
    public bool UseActiveHolidayConfig { get; set; }

    /// <summary>
    /// 启用的定岗规则ID列表（JSON数组）
    /// </summary>
    public List<int> EnabledFixedRuleIds { get; set; } = new();

    /// <summary>
    /// 启用的手动指定ID列表（JSON数组）
    /// </summary>
    public List<int> EnabledManualAssignmentIds { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; }

    public override string ToString()
    {
        return $"SchedulingTemplate[{Id}] {Name} ({TemplateType})";
    }
}
