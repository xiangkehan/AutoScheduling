namespace AutoScheduling3.ViewModels.Scheduling;

/// <summary>
/// 人员来源类型
/// </summary>
public enum PersonnelSourceType
{
    /// <summary>
    /// 自动提取（从哨位的原始可用人员列表）
    /// </summary>
    AutoExtracted,

    /// <summary>
    /// 临时添加（在本次排班中添加到哨位）
    /// </summary>
    TemporarilyAdded,

    /// <summary>
    /// 临时移除（在本次排班中从哨位移除）
    /// </summary>
    TemporarilyRemoved,

    /// <summary>
    /// 手动添加（不属于任何哨位，手动添加到参与人员）
    /// </summary>
    ManuallyAdded
}
