using System;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 手动指定草稿数据传输对象
/// 用于序列化临时手动指定（未保存到数据库的）
/// </summary>
public class ManualAssignmentDraftDto
{
    /// <summary>
    /// 指定日期
    /// </summary>
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    /// <summary>
    /// 人员ID
    /// </summary>
    [JsonPropertyName("personnelId")]
    public int PersonnelId { get; set; }

    /// <summary>
    /// 岗位ID
    /// </summary>
    [JsonPropertyName("positionId")]
    public int PositionId { get; set; }

    /// <summary>
    /// 时段索引（0-11，对应12个时段）
    /// </summary>
    [JsonPropertyName("timeSlot")]
    public int TimeSlot { get; set; }

    /// <summary>
    /// 备注说明
    /// </summary>
    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 临时ID（用于临时手动指定的唯一标识）
    /// </summary>
    [JsonPropertyName("tempId")]
    public Guid TempId { get; set; }
}
