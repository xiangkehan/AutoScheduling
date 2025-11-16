namespace AutoScheduling3.DTOs;

/// <summary>
/// 表格导出选项
/// </summary>
public class ExportOptions
{
    /// <summary>
    /// 是否包含表头
    /// </summary>
    public bool IncludeHeader { get; set; } = true;

    /// <summary>
    /// 是否包含空单元格
    /// </summary>
    public bool IncludeEmptyCells { get; set; } = true;

    /// <summary>
    /// 是否高亮显示冲突
    /// </summary>
    public bool HighlightConflicts { get; set; } = true;

    /// <summary>
    /// 是否高亮显示手动指定的分配
    /// </summary>
    public bool HighlightManualAssignments { get; set; } = true;

    /// <summary>
    /// 导出标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 自定义选项字典
    /// </summary>
    public Dictionary<string, object>? CustomOptions { get; set; }
}
