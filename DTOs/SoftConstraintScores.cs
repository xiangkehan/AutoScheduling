namespace AutoScheduling3.DTOs;

/// <summary>
/// 软约束评分
/// </summary>
public class SoftConstraintScores
{
    /// <summary>
    /// 总分
    /// </summary>
    public double TotalScore { get; set; }

    /// <summary>
    /// 休息时间评分
    /// </summary>
    public double RestScore { get; set; }

    /// <summary>
    /// 时段平衡评分
    /// </summary>
    public double TimeSlotBalanceScore { get; set; }

    /// <summary>
    /// 节假日平衡评分
    /// </summary>
    public double HolidayBalanceScore { get; set; }
}
