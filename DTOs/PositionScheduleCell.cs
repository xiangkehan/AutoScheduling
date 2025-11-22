namespace AutoScheduling3.DTOs;

/// <summary>
/// 哨位排班单元格
/// </summary>
public class PositionScheduleCell
{
    /// <summary>
    /// 时段索引（0-11）
    /// </summary>
    public int PeriodIndex { get; set; }

    /// <summary>
    /// 星期（0=周一, 6=周日）
    /// </summary>
    public int DayOfWeek { get; set; }

    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 人员ID
    /// </summary>
    public int? PersonnelId { get; set; }

    /// <summary>
    /// 人员姓名
    /// </summary>
    public string? PersonnelName { get; set; }

    /// <summary>
    /// 是否已分配
    /// </summary>
    public bool IsAssigned { get; set; }

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
