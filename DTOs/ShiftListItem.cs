namespace AutoScheduling3.DTOs;

/// <summary>
/// 班次列表项（用于 List View）
/// </summary>
public class ShiftListItem
{
    /// <summary>
    /// 班次ID
    /// </summary>
    public int ShiftId { get; set; }

    /// <summary>
    /// 日期时间描述
    /// </summary>
    public string DateTimeDescription { get; set; } = string.Empty;

    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 时段索引
    /// </summary>
    public int PeriodIndex { get; set; }

    /// <summary>
    /// 时段描述
    /// </summary>
    public string TimeSlot { get; set; } = string.Empty;

    /// <summary>
    /// 哨位ID
    /// </summary>
    public int PositionId { get; set; }

    /// <summary>
    /// 哨位名称
    /// </summary>
    public string PositionName { get; set; } = string.Empty;

    /// <summary>
    /// 人员ID
    /// </summary>
    public int PersonnelId { get; set; }

    /// <summary>
    /// 人员姓名
    /// </summary>
    public string PersonnelName { get; set; } = string.Empty;

    /// <summary>
    /// 是否手动指定
    /// </summary>
    public bool IsManualAssignment { get; set; }

    /// <summary>
    /// 是否有冲突
    /// </summary>
    public bool HasConflict { get; set; }

    /// <summary>
    /// 冲突消息
    /// </summary>
    public string? ConflictMessage { get; set; }
}
