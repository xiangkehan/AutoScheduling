using AutoScheduling3.DTOs;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 冲突修复服务接口
/// </summary>
public interface IConflictResolutionService
{
    /// <summary>
    /// 生成冲突修复方案
    /// </summary>
    /// <param name="conflict">冲突信息</param>
    /// <param name="schedule">排班数据</param>
    /// <returns>修复方案列表</returns>
    Task<List<ConflictResolutionOption>> GenerateResolutionOptionsAsync(
        ConflictDto conflict,
        ScheduleDto schedule);

    /// <summary>
    /// 应用修复方案
    /// </summary>
    /// <param name="option">选定的修复方案</param>
    /// <param name="schedule">排班数据</param>
    /// <returns>更新后的排班数据</returns>
    Task<ScheduleDto> ApplyResolutionAsync(
        ConflictResolutionOption option,
        ScheduleDto schedule);

    /// <summary>
    /// 验证修复方案的有效性
    /// </summary>
    /// <param name="option">修复方案</param>
    /// <param name="schedule">排班数据</param>
    /// <returns>是否有效及原因</returns>
    Task<(bool IsValid, string? Reason)> ValidateResolutionAsync(
        ConflictResolutionOption option,
        ScheduleDto schedule);

    /// <summary>
    /// 评估修复方案的影响
    /// </summary>
    /// <param name="option">修复方案</param>
    /// <param name="schedule">排班数据</param>
    /// <returns>影响评估结果</returns>
    Task<ResolutionImpact> EvaluateImpactAsync(
        ConflictResolutionOption option,
        ScheduleDto schedule);
}
