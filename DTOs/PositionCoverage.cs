namespace AutoScheduling3.DTOs;

/// <summary>
/// 哨位覆盖率统计
/// </summary>
public class PositionCoverage
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
    /// 已分配的时段数
    /// </summary>
    public int AssignedSlots { get; set; }

    /// <summary>
    /// 总时段数
    /// </summary>
    public int TotalSlots { get; set; }

    /// <summary>
    /// 覆盖率 (0-1)
    /// </summary>
    public double CoverageRate { get; set; }

    /// <summary>
    /// 覆盖率展示文本
    /// </summary>
    public string CoverageRateDisplay => CoverageRate.ToString("P0");

    /// <summary>
    /// 包值（有效人员数，考虑人员共享）
    /// </summary>
    public double PackValue { get; set; }

    /// <summary>
    /// 包值展示文本
    /// </summary>
    public string PackValueDisplay => PackValue.ToString("F1");
}
