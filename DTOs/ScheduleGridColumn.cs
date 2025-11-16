namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班表格列数据（哨位列）
/// </summary>
public class ScheduleGridColumn
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
    /// 列索引
    /// </summary>
    public int ColumnIndex { get; set; }
}
