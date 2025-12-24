namespace AutoScheduling3.Constants;

/// <summary>
/// 排班相关常量
/// </summary>
public static class SchedulingConstants
{
    /// <summary>
    /// 每天的时段数
    /// </summary>
    public const int PeriodsPerDay = 12;

    /// <summary>
    /// 夜哨时段索引（时段11, 0, 1, 2）
    /// 对应时间：22:00-00:00, 00:00-02:00, 02:00-04:00, 04:00-06:00
    /// </summary>
    public static readonly int[] NightShiftPeriods = { 11, 0, 1, 2 };

    /// <summary>
    /// 日哨时段索引（时段3-10）
    /// 对应时间：06:00-22:00
    /// </summary>
    public static readonly int[] DayShiftPeriods = { 3, 4, 5, 6, 7, 8, 9, 10 };

    /// <summary>
    /// 每个时段的小时数
    /// </summary>
    public const int HoursPerPeriod = 2;

    /// <summary>
    /// 最小时段索引
    /// </summary>
    public const int MinPeriodIndex = 0;

    /// <summary>
    /// 最大时段索引
    /// </summary>
    public const int MaxPeriodIndex = PeriodsPerDay - 1;
}
