namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班表格数据模型，包含表格的列、行和单元格数据
/// </summary>
public class ScheduleGridData
{
    /// <summary>
    /// 表格列数据（哨位列）
    /// </summary>
    public List<ScheduleGridColumn> Columns { get; set; } = new();

    /// <summary>
    /// 表格行数据（日期+时段行）
    /// </summary>
    public List<ScheduleGridRow> Rows { get; set; } = new();

    /// <summary>
    /// 单元格数据字典，键为 "row_col" 格式
    /// </summary>
    public Dictionary<string, ScheduleGridCell> Cells { get; set; } = new();

    /// <summary>
    /// 排班开始日期
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 排班结束日期
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 哨位ID列表
    /// </summary>
    public List<int> PositionIds { get; set; } = new();

    /// <summary>
    /// 总天数
    /// </summary>
    public int TotalDays { get; set; }

    /// <summary>
    /// 总时段数（每天的时段数）
    /// </summary>
    public int TotalPeriods { get; set; }
}
