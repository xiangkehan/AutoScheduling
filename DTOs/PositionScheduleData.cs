using System.Linq;

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

    /// <summary>
    /// 班次总数（统计所有周次的已分配班次）
    /// </summary>
    public int TotalShifts => Weeks
        .SelectMany(w => w.Cells.Values)
        .Count(c => c.IsAssigned);
}
