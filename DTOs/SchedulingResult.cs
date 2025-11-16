namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班结果
/// </summary>
public class SchedulingResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 排班数据（成功时不为空）
    /// </summary>
    public ScheduleDto? Schedule { get; set; }

    /// <summary>
    /// 统计信息（成功时不为空）
    /// </summary>
    public SchedulingStatistics? Statistics { get; set; }

    /// <summary>
    /// 冲突列表
    /// </summary>
    public List<ConflictInfo> Conflicts { get; set; } = new();

    /// <summary>
    /// 错误消息（失败时不为空）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 总执行时长
    /// </summary>
    public TimeSpan TotalDuration { get; set; }
}
