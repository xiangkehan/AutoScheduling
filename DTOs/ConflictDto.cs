using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 冲突/约束提示 DTO（扩展）
/// </summary>
public class ConflictDto
{
    /// <summary>
    /// 冲突ID（用于跟踪和忽略状态）
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 冲突类型：hard / soft / info / unassigned
    /// </summary>
    [Required(ErrorMessage = "冲突类型不能为空")]
    [RegularExpression("^(hard|soft|info|unassigned)$", ErrorMessage = "冲突类型必须是 hard、soft、info 或 unassigned")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 具体冲突子类型
    /// </summary>
    [JsonPropertyName("subType")]
    public ConflictSubType SubType { get; set; }

    /// <summary>
    /// 冲突描述
    /// </summary>
    [Required(ErrorMessage = "冲突描述不能为空")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "冲突描述长度必须在1-500字符之间")]
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 详细描述（用于对话框显示）
    /// </summary>
    [JsonPropertyName("detailedMessage")]
    public string? DetailedMessage { get; set; }

    /// <summary>
    /// 相关哨位ID
    /// </summary>
    [JsonPropertyName("positionId")]
    public int? PositionId { get; set; }

    /// <summary>
    /// 相关哨位名称
    /// </summary>
    [JsonPropertyName("positionName")]
    public string? PositionName { get; set; }

    /// <summary>
    /// 相关人员ID
    /// </summary>
    [JsonPropertyName("personnelId")]
    public int? PersonnelId { get; set; }

    /// <summary>
    /// 相关人员姓名
    /// </summary>
    [JsonPropertyName("personnelName")]
    public string? PersonnelName { get; set; }

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

    /// <summary>
    /// 相关班次ID列表（可能涉及多个班次）
    /// </summary>
    [JsonPropertyName("relatedShiftIds")]
    public List<int> RelatedShiftIds { get; set; } = new();

    /// <summary>
    /// 严重程度（1-5，5最严重）
    /// </summary>
    [Range(1, 5, ErrorMessage = "严重程度必须在1-5之间")]
    [JsonPropertyName("severity")]
    public int Severity { get; set; } = 3;

    /// <summary>
    /// 是否已忽略
    /// </summary>
    [JsonPropertyName("isIgnored")]
    public bool IsIgnored { get; set; }

    /// <summary>
    /// 是否可修复
    /// </summary>
    [JsonPropertyName("isFixable")]
    public bool IsFixable { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
