namespace AutoScheduling3.DTOs;

/// <summary>
/// 修复方案类型
/// </summary>
public enum ResolutionType
{
    /// <summary>
    /// 替换人员
    /// </summary>
    ReplacePersonnel,
    
    /// <summary>
    /// 取消分配
    /// </summary>
    RemoveAssignment,
    
    /// <summary>
    /// 调整时间
    /// </summary>
    AdjustTime,
    
    /// <summary>
    /// 重新分配班次
    /// </summary>
    ReassignShifts,
    
    /// <summary>
    /// 忽略冲突
    /// </summary>
    IgnoreConflict,
    
    /// <summary>
    /// 手动修复
    /// </summary>
    ManualFix
}
