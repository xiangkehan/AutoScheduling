namespace AutoScheduling3.DTOs;

/// <summary>
/// 人员工作量统计
/// </summary>
public class PersonnelWorkload
{
    /// <summary>
    /// 人员ID
    /// </summary>
    public int PersonnelId { get; set; }

    /// <summary>
    /// 人员姓名
    /// </summary>
    public string PersonnelName { get; set; } = string.Empty;

    /// <summary>
    /// 总班次数
    /// </summary>
    public int TotalShifts { get; set; }

    /// <summary>
    /// 日哨数
    /// </summary>
    public int DayShifts { get; set; }

    /// <summary>
    /// 夜哨数
    /// </summary>
    public int NightShifts { get; set; }
}
