namespace AutoScheduling3.DTOs;

/// <summary>
/// 冲突趋势数据
/// </summary>
public class ConflictTrendData
{
    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 按日期统计的冲突数量
    /// </summary>
    public Dictionary<DateTime, int> ConflictsByDate { get; set; } = new();

    /// <summary>
    /// 按类型统计的冲突数量
    /// </summary>
    public Dictionary<ConflictSubType, int> ConflictsByType { get; set; } = new();

    /// <summary>
    /// 总冲突数
    /// </summary>
    public int TotalConflicts { get; set; }

    /// <summary>
    /// 已解决冲突数
    /// </summary>
    public int ResolvedConflicts { get; set; }

    /// <summary>
    /// 已忽略冲突数
    /// </summary>
    public int IgnoredConflicts { get; set; }

    /// <summary>
    /// 待处理冲突数
    /// </summary>
    public int PendingConflicts { get; set; }

    /// <summary>
    /// 解决率
    /// </summary>
    public double ResolutionRate => TotalConflicts > 0 
        ? (double)ResolvedConflicts / TotalConflicts 
        : 0;
}
