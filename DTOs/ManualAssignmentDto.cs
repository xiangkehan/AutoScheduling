using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 手动指定数据传输对象 - 对应需求5.8
/// </summary>
public class ManualAssignmentDto
{
    /// <summary>
    /// 手动指定ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

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
    /// 时段索引（0-11，对应12个时段）
    /// </summary>
    [Range(0, 11, ErrorMessage = "时段索引必须在0-11之间")]
    [JsonPropertyName("timeSlot")]
    public int TimeSlot { get; set; }

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
    /// 指定日期
    /// </summary>
    [Required(ErrorMessage = "指定日期不能为空")]
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 备注说明
    /// </summary>
    [StringLength(200, ErrorMessage = "备注说明长度不能超过200字符")]
    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }

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
/// 创建手动指定DTO
/// </summary>
public class CreateManualAssignmentDto
{
    /// <summary>
    /// 哨位ID（必填）
    /// </summary>
    [Required(ErrorMessage = "哨位ID不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "哨位ID必须大于0")]
    [JsonPropertyName("positionId")]
    public int PositionId { get; set; }

    /// <summary>
    /// 时段索引（必填，0-11）
    /// </summary>
    [Required(ErrorMessage = "时段索引不能为空")]
    [Range(0, 11, ErrorMessage = "时段索引必须在0-11之间")]
    [JsonPropertyName("timeSlot")]
    public int TimeSlot { get; set; }

    /// <summary>
    /// 人员ID（必填）
    /// </summary>
    [Required(ErrorMessage = "人员ID不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "人员ID必须大于0")]
    [JsonPropertyName("personnelId")]
    public int PersonnelId { get; set; }

    /// <summary>
    /// 指定日期（必填）
    /// </summary>
    [Required(ErrorMessage = "指定日期不能为空")]
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    /// <summary>
    /// 是否启用（默认true）
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 备注说明（可选，最多200字符）
    /// </summary>
    [StringLength(200, ErrorMessage = "备注说明长度不能超过200字符")]
    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}

/// <summary>
/// 更新手动指定DTO
/// </summary>
public class UpdateManualAssignmentDto
{
    /// <summary>
    /// 哨位ID（必填）
    /// </summary>
    [Required(ErrorMessage = "哨位ID不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "哨位ID必须大于0")]
    [JsonPropertyName("positionId")]
    public int PositionId { get; set; }

    /// <summary>
    /// 时段索引（必填，0-11）
    /// </summary>
    [Required(ErrorMessage = "时段索引不能为空")]
    [Range(0, 11, ErrorMessage = "时段索引必须在0-11之间")]
    [JsonPropertyName("timeSlot")]
    public int TimeSlot { get; set; }

    /// <summary>
    /// 人员ID（必填）
    /// </summary>
    [Required(ErrorMessage = "人员ID不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "人员ID必须大于0")]
    [JsonPropertyName("personnelId")]
    public int PersonnelId { get; set; }

    /// <summary>
    /// 指定日期（必填）
    /// </summary>
    [Required(ErrorMessage = "指定日期不能为空")]
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 备注说明（可选，最多200字符）
    /// </summary>
    [StringLength(200, ErrorMessage = "备注说明长度不能超过200字符")]
    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}