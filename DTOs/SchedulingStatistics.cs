namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班统计信息
/// </summary>
public class SchedulingStatistics
{
    /// <summary>
    /// 总分配数
    /// </summary>
    public int TotalAssignments { get; set; }

    /// <summary>
    /// 人员工作量统计（键为人员ID）
    /// </summary>
    public Dictionary<int, PersonnelWorkload> PersonnelWorkloads { get; set; } = new();

    /// <summary>
    /// 哨位覆盖率统计（键为哨位ID）
    /// </summary>
    public Dictionary<int, PositionCoverage> PositionCoverages { get; set; } = new();

    /// <summary>
    /// 软约束评分
    /// </summary>
    public SoftConstraintScores SoftScores { get; set; } = new();
}
