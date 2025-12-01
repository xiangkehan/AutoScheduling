namespace AutoScheduling3.DTOs;

/// <summary>
/// 人员排班数据
/// </summary>
public class PersonnelScheduleData
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
    /// 工作量统计
    /// </summary>
    public PersonnelWorkload Workload { get; set; } = new();

    /// <summary>
    /// 班次列表
    /// </summary>
    public List<PersonnelShift> Shifts { get; set; } = new();

    /// <summary>
    /// 日历数据（用于日历视图）
    /// </summary>
    public Dictionary<DateTime, List<PersonnelShift>> CalendarData { get; set; } = new();
}
