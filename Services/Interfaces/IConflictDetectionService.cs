using AutoScheduling3.DTOs;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 冲突检测服务接口
/// </summary>
public interface IConflictDetectionService
{
    /// <summary>
    /// 检测排班中的所有冲突
    /// </summary>
    /// <param name="schedule">排班数据</param>
    /// <returns>冲突列表</returns>
    Task<List<ConflictDto>> DetectConflictsAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测特定班次的冲突
    /// </summary>
    /// <param name="shift">班次数据</param>
    /// <param name="schedule">完整排班数据</param>
    /// <returns>冲突列表</returns>
    Task<List<ConflictDto>> DetectShiftConflictsAsync(object shift, ScheduleDto schedule);

    /// <summary>
    /// 获取冲突统计信息
    /// </summary>
    /// <param name="conflicts">冲突列表</param>
    /// <returns>统计信息</returns>
    ConflictStatistics GetConflictStatistics(List<ConflictDto> conflicts);

    /// <summary>
    /// 检测技能不匹配冲突
    /// </summary>
    /// <param name="schedule">排班数据</param>
    /// <returns>冲突列表</returns>
    Task<List<ConflictDto>> DetectSkillMismatchAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测人员不可用冲突
    /// </summary>
    /// <param name="schedule">排班数据</param>
    /// <returns>冲突列表</returns>
    Task<List<ConflictDto>> DetectPersonnelUnavailableAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测休息时间不足冲突
    /// </summary>
    /// <param name="schedule">排班数据</param>
    /// <returns>冲突列表</returns>
    Task<List<ConflictDto>> DetectInsufficientRestAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测工作量不均衡冲突
    /// </summary>
    /// <param name="schedule">排班数据</param>
    /// <returns>冲突列表</returns>
    Task<List<ConflictDto>> DetectWorkloadImbalanceAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测连续工作超时冲突
    /// </summary>
    /// <param name="schedule">排班数据</param>
    /// <returns>冲突列表</returns>
    Task<List<ConflictDto>> DetectConsecutiveOvertimeAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测未分配时段
    /// </summary>
    /// <param name="schedule">排班数据</param>
    /// <returns>冲突列表</returns>
    Task<List<ConflictDto>> DetectUnassignedSlotsAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测重复分配冲突
    /// </summary>
    /// <param name="schedule">排班数据</param>
    /// <returns>冲突列表</returns>
    Task<List<ConflictDto>> DetectDuplicateAssignmentsAsync(ScheduleDto schedule);
}
