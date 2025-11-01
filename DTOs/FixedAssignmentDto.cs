using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 定岗要求数据传输对象 - 对应需求5.4
/// </summary>
public class FixedAssignmentDto
{
    /// <summary>
    /// 定岗规则ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

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
    /// 允许的哨位ID列表
    /// </summary>
    [Required(ErrorMessage = "允许的哨位列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要指定一个允许的哨位")]
    [JsonPropertyName("allowedPositionIds")]
    public List<int> AllowedPositionIds { get; set; } = new();

    /// <summary>
    /// 允许的哨位名称列表（冗余字段，便于显示）
    /// </summary>
    [JsonPropertyName("allowedPositionNames")]
    public List<string> AllowedPositionNames { get; set; } = new();

    /// <summary>
    /// 允许的时段列表（0-11，对应12个时段）
    /// </summary>
    [Required(ErrorMessage = "允许的时段列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要指定一个允许的时段")]
    [JsonPropertyName("allowedTimeSlots")]
    public List<int> AllowedTimeSlots { get; set; } = new();

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
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 规则名称
    /// </summary>
    [Required(ErrorMessage = "规则名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "规则名称长度必须在1-100字符之间")]
    [JsonPropertyName("ruleName")]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 规则描述
    /// </summary>
    [StringLength(500, ErrorMessage = "规则描述长度不能超过500字符")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

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
}

/// <summary>
/// 创建定岗规则DTO
/// </summary>
public class CreateFixedAssignmentDto
{
    /// <summary>
    /// 人员ID（必填）
    /// </summary>
    [Required(ErrorMessage = "人员ID不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "人员ID必须大于0")]
    [JsonPropertyName("personnelId")]
    public int PersonnelId { get; set; }

    /// <summary>
    /// 允许的哨位ID列表（必填，至少1个）
    /// </summary>
    [Required(ErrorMessage = "允许的哨位列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要指定一个允许的哨位")]
    [JsonPropertyName("allowedPositionIds")]
    public List<int> AllowedPositionIds { get; set; } = new();

    /// <summary>
    /// 允许的时段列表（必填，至少1个，0-11）
    /// </summary>
    [Required(ErrorMessage = "允许的时段列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要指定一个允许的时段")]
    [JsonPropertyName("allowedTimeSlots")]
    public List<int> AllowedTimeSlots { get; set; } = new();

    /// <summary>
    /// 开始日期（必填）
    /// </summary>
    [Required(ErrorMessage = "开始日期不能为空")]
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期（必填，不早于开始日期）
    /// </summary>
    [Required(ErrorMessage = "结束日期不能为空")]
    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 是否启用（默认true）
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 规则名称（必填，1-100字符）
    /// </summary>
    [Required(ErrorMessage = "规则名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "规则名称长度必须在1-100字符之间")]
    [JsonPropertyName("ruleName")]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 规则描述（可选，最多500字符）
    /// </summary>
    [StringLength(500, ErrorMessage = "规则描述长度不能超过500字符")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 自定义验证：结束日期不能早于开始日期
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult(
                "结束日期不能早于开始日期",
                new[] { nameof(EndDate) });
        }

        // 验证时段索引范围
        foreach (var timeSlot in AllowedTimeSlots)
        {
            if (timeSlot < 0 || timeSlot > 11)
            {
                yield return new ValidationResult(
                    "时段索引必须在0-11之间",
                    new[] { nameof(AllowedTimeSlots) });
                break;
            }
        }
    }
}

/// <summary>
/// 更新定岗规则DTO
/// </summary>
public class UpdateFixedAssignmentDto
{
    /// <summary>
    /// 人员ID（必填）
    /// </summary>
    [Required(ErrorMessage = "人员ID不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "人员ID必须大于0")]
    [JsonPropertyName("personnelId")]
    public int PersonnelId { get; set; }

    /// <summary>
    /// 允许的哨位ID列表（必填，至少1个）
    /// </summary>
    [Required(ErrorMessage = "允许的哨位列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要指定一个允许的哨位")]
    [JsonPropertyName("allowedPositionIds")]
    public List<int> AllowedPositionIds { get; set; } = new();

    /// <summary>
    /// 允许的时段列表（必填，至少1个，0-11）
    /// </summary>
    [Required(ErrorMessage = "允许的时段列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要指定一个允许的时段")]
    [JsonPropertyName("allowedTimeSlots")]
    public List<int> AllowedTimeSlots { get; set; } = new();

    /// <summary>
    /// 开始日期（必填）
    /// </summary>
    [Required(ErrorMessage = "开始日期不能为空")]
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期（必填，不早于开始日期）
    /// </summary>
    [Required(ErrorMessage = "结束日期不能为空")]
    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 规则名称（必填，1-100字符）
    /// </summary>
    [Required(ErrorMessage = "规则名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "规则名称长度必须在1-100字符之间")]
    [JsonPropertyName("ruleName")]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 规则描述（可选，最多500字符）
    /// </summary>
    [StringLength(500, ErrorMessage = "规则描述长度不能超过500字符")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 自定义验证：结束日期不能早于开始日期
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult(
                "结束日期不能早于开始日期",
                new[] { nameof(EndDate) });
        }

        // 验证时段索引范围
        foreach (var timeSlot in AllowedTimeSlots)
        {
            if (timeSlot < 0 || timeSlot > 11)
            {
                yield return new ValidationResult(
                    "时段索引必须在0-11之间",
                    new[] { nameof(AllowedTimeSlots) });
                break;
            }
        }
    }
}