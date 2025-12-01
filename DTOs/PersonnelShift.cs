namespace AutoScheduling3.DTOs;

/// <summary>
/// 人员班次
/// </summary>
public class PersonnelShift
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 时段索引
    /// </summary>
    public int PeriodIndex { get; set; }

    /// <summary>
    /// 时段描述（如 "08:00-10:00"）
    /// </summary>
    public string TimeSlot { get; set; } = string.Empty;

    /// <summary>
    /// 哨位ID
    /// </summary>
    public int PositionId { get; set; }

    /// <summary>
    /// 哨位名称
    /// </summary>
    public string PositionName { get; set; } = string.Empty;

    /// <summary>
    /// 是否手动指定
    /// </summary>
    public bool IsManualAssignment { get; set; }

    /// <summary>
    /// 是否夜哨
    /// </summary>
    public bool IsNightShift { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
}
