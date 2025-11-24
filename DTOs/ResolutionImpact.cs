namespace AutoScheduling3.DTOs;

/// <summary>
/// 修复方案影响评估
/// </summary>
public class ResolutionImpact
{
    /// <summary>
    /// 将解决的冲突数量
    /// </summary>
    public int ResolvedConflicts { get; set; }

    /// <summary>
    /// 可能产生的新冲突数量
    /// </summary>
    public int NewConflicts { get; set; }

    /// <summary>
    /// 影响的人员ID列表
    /// </summary>
    public List<int> AffectedPersonnelIds { get; set; } = new();

    /// <summary>
    /// 影响的哨位ID列表
    /// </summary>
    public List<int> AffectedPositionIds { get; set; } = new();

    /// <summary>
    /// 影响描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
