namespace AutoScheduling3.DTOs;

/// <summary>
/// 哨位排班数据（按周显示）
/// </summary>
public class PositionScheduleData
{
    /// <summary>
    /// 哨位ID
    /// </summary>
    public int PositionId { get; set; }

    /// <summary>
    /// 哨位名称
    /// </summary>
    public string PositionName { get; set; } = string.Empty;

    /// <summary>
    /// 周次列表
    /// </summary>
    public List<WeekData> Weeks { get; set; } = new();

    /// <summary>
    /// 当前选中的周次索引
    /// </summary>
    public int CurrentWeekIndex { get; set; }
}
