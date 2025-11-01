using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 哨位/职位数据传输对象
/// </summary>
public class PositionDto
{
    /// <summary>
    /// 哨位ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 哨位名称
    /// </summary>
    [Required(ErrorMessage = "哨位名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "哨位名称长度必须在1-100字符之间")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 地点
    /// </summary>
    [Required(ErrorMessage = "地点不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "地点长度必须在1-200字符之间")]
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// 介绍
    /// </summary>
    [StringLength(500, ErrorMessage = "介绍长度不能超过500字符")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 要求说明
    /// </summary>
    [StringLength(1000, ErrorMessage = "要求说明长度不能超过1000字符")]
    [JsonPropertyName("requirements")]
    public string? Requirements { get; set; }

    /// <summary>
    /// 所需技能ID列表
    /// </summary>
    [Required(ErrorMessage = "技能列表不能为空")]
    [JsonPropertyName("requiredSkillIds")]
    public List<int> RequiredSkillIds { get; set; } = new();

    /// <summary>
    /// 所需技能名称列表（冗余字段，便于显示）
    /// </summary>
    [JsonPropertyName("requiredSkillNames")]
    public List<string> RequiredSkillNames { get; set; } = new();
}

/// <summary>
/// 创建哨位DTO
/// </summary>
public class CreatePositionDto
{
    /// <summary>
    /// 哨位名称（必填，1-100字符）
    /// </summary>
    [Required(ErrorMessage = "哨位名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "哨位名称长度必须在1-100字符之间")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 地点（必填，1-200字符）
    /// </summary>
    [Required(ErrorMessage = "地点不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "地点长度必须在1-200字符之间")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// 介绍（可选，最多500字符）
    /// </summary>
    [StringLength(500, ErrorMessage = "介绍长度不能超过500字符")]
    public string? Description { get; set; }

    /// <summary>
    /// 要求说明（可选，最多1000字符）
    /// </summary>
    [StringLength(1000, ErrorMessage = "要求说明长度不能超过1000字符")]
    public string? Requirements { get; set; }

    /// <summary>
    /// 所需技能ID列表（至少一项）
    /// </summary>
    [Required(ErrorMessage = "技能列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一项技能")]
    public List<int> RequiredSkillIds { get; set; } = new();
}

/// <summary>
/// 更新哨位DTO
/// </summary>
public class UpdatePositionDto
{
    /// <summary>
    /// 哨位名称（必填，1-100字符）
    /// </summary>
    [Required(ErrorMessage = "哨位名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "哨位名称长度必须在1-100字符之间")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 地点（必填，1-200字符）
    /// </summary>
    [Required(ErrorMessage = "地点不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "地点长度必须在1-200字符之间")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// 介绍（可选，最多500字符）
    /// </summary>
    [StringLength(500, ErrorMessage = "介绍长度不能超过500字符")]
    public string? Description { get; set; }

    /// <summary>
    /// 要求说明（可选，最多1000字符）
    /// </summary>
    [StringLength(1000, ErrorMessage = "要求说明长度不能超过1000字符")]
    public string? Requirements { get; set; }

    /// <summary>
    /// 所需技能ID列表（至少一项）
    /// </summary>
    [Required(ErrorMessage = "技能列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一项技能")]
    public List<int> RequiredSkillIds { get; set; } = new();
}
