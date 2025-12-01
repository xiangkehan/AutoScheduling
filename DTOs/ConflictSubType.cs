namespace AutoScheduling3.DTOs;

/// <summary>
/// 冲突子类型
/// </summary>
public enum ConflictSubType
{
    // 硬约束冲突
    /// <summary>
    /// 技能不匹配
    /// </summary>
    SkillMismatch,
    
    /// <summary>
    /// 人员不可用
    /// </summary>
    PersonnelUnavailable,
    
    /// <summary>
    /// 重复分配
    /// </summary>
    DuplicateAssignment,
    
    // 软约束冲突
    /// <summary>
    /// 休息时间不足
    /// </summary>
    InsufficientRest,
    
    /// <summary>
    /// 工作量过大
    /// </summary>
    ExcessiveWorkload,
    
    /// <summary>
    /// 工作量不均衡
    /// </summary>
    WorkloadImbalance,
    
    /// <summary>
    /// 连续工作超时
    /// </summary>
    ConsecutiveOvertime,
    
    // 信息提示
    /// <summary>
    /// 未分配时段
    /// </summary>
    UnassignedSlot,
    
    /// <summary>
    /// 次优分配
    /// </summary>
    SuboptimalAssignment,
    
    // 其他
    /// <summary>
    /// 未知类型
    /// </summary>
    Unknown
}
