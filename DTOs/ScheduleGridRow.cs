namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班表格行数据（日期+时段行）
/// </summary>
public class ScheduleGridRow
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 时段索引
    /// </summary>
    public int PeriodIndex { get; set; }

    /// <summary>
    /// 时间范围（如 "08:00-10:00"）
    /// </summary>
    public string TimeRange { get; set; } = string.Empty;

    /// <summary>
    /// 显示文本（如 "2025-01-15 08:00-10:00"）
    /// </summary>
    public string DisplayText { get; set; } = string.Empty;

    /// <summary>
    /// 行索引
    /// </summary>
    public int RowIndex { get; set; }
}
