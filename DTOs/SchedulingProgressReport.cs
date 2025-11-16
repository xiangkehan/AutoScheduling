namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班进度报告
/// </summary>
public class SchedulingProgressReport
{
    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public double ProgressPercentage { get; set; }

    /// <summary>
    /// 当前执行阶段
    /// </summary>
    public SchedulingStage CurrentStage { get; set; }

    /// <summary>
    /// 阶段描述
    /// </summary>
    public string StageDescription { get; set; } = string.Empty;

    /// <summary>
    /// 已完成的分配数量
    /// </summary>
    public int CompletedAssignments { get; set; }

    /// <summary>
    /// 总共需要分配的时段数
    /// </summary>
    public int TotalSlotsToAssign { get; set; }

    /// <summary>
    /// 剩余未分配的时段数
    /// </summary>
    public int RemainingSlots { get; set; }

    /// <summary>
    /// 当前正在处理的哨位名称
    /// </summary>
    public string? CurrentPositionName { get; set; }

    /// <summary>
    /// 当前正在处理的时段索引
    /// </summary>
    public int CurrentPeriodIndex { get; set; }

    /// <summary>
    /// 当前正在处理的日期
    /// </summary>
    public DateTime CurrentDate { get; set; }

    /// <summary>
    /// 已执行时间
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// 警告信息列表
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 是否有错误
    /// </summary>
    public bool HasErrors { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }
}
