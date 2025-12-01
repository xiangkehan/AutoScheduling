namespace AutoScheduling3.DTOs;

/// <summary>
/// 冲突统计信息
/// </summary>
public class ConflictStatistics
{
    /// <summary>
    /// 硬约束冲突数量
    /// </summary>
    public int HardConflictCount { get; set; }

    /// <summary>
    /// 软约束冲突数量
    /// </summary>
    public int SoftConflictCount { get; set; }

    /// <summary>
    /// 未分配时段数量
    /// </summary>
    public int UnassignedSlotCount { get; set; }

    /// <summary>
    /// 已忽略冲突数量
    /// </summary>
    public int IgnoredConflictCount { get; set; }

    /// <summary>
    /// 总冲突数量
    /// </summary>
    public int TotalConflictCount => HardConflictCount + SoftConflictCount + UnassignedSlotCount;

    /// <summary>
    /// 按类型分组的冲突数量
    /// </summary>
    public Dictionary<ConflictSubType, int> ConflictsByType { get; set; } = new();
}
