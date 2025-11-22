namespace AutoScheduling3.DTOs;

/// <summary>
/// 周数据
/// </summary>
public class WeekData
{
    /// <summary>
    /// 周次（第几周）
    /// </summary>
    public int WeekNumber { get; set; }

    /// <summary>
    /// 周开始日期
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 周结束日期
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 单元格数据（键为 "periodIndex_dayOfWeek"）
    /// periodIndex: 0-11（12个时段）
    /// dayOfWeek: 0-6（周一到周日）
    /// </summary>
    public Dictionary<string, PositionScheduleCell> Cells { get; set; } = new();
}
