namespace AutoScheduling3.DTOs;

/// <summary>
/// 冲突信息
/// </summary>
public class ConflictInfo
{
    /// <summary>
    /// 冲突类型
    /// </summary>
    public string ConflictType { get; set; } = string.Empty;

    /// <summary>
    /// 冲突描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 哨位ID（可选）
    /// </summary>
    public int? PositionId { get; set; }

    /// <summary>
    /// 哨位名称（可选）
    /// </summary>
    public string? PositionName { get; set; }

    /// <summary>
    /// 时段索引（可选）
    /// </summary>
    public int? PeriodIndex { get; set; }

    /// <summary>
    /// 日期（可选）
    /// </summary>
    public DateTime? Date { get; set; }
}
