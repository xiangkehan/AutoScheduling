namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班表格单元格数据
/// </summary>
public class ScheduleGridCell
{
    /// <summary>
    /// 行索引
    /// </summary>
    public int RowIndex { get; set; }

    /// <summary>
    /// 列索引
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// 人员ID（如果已分配）
    /// </summary>
    public int? PersonnelId { get; set; }

    /// <summary>
    /// 人员姓名（如果已分配）
    /// </summary>
    public string? PersonnelName { get; set; }

    /// <summary>
    /// 是否已分配
    /// </summary>
    public bool IsAssigned { get; set; }

    /// <summary>
    /// 是否为手动指定的分配
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
