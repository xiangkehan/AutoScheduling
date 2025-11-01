using AutoScheduling3.DTOs;
using AutoScheduling3.Models.Constraints;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 约束管理服务接口
/// </summary>
public interface IConstraintService
{
    // 定岗规则管理
    /// <summary>
    /// 创建定岗规则
    /// </summary>
    Task<int> CreateFixedPositionRuleAsync(FixedPositionRule rule);

    /// <summary>
    /// 获取所有定岗规则
    /// </summary>
    Task<List<FixedPositionRule>> GetAllFixedPositionRulesAsync(bool enabledOnly = true);

    /// <summary>
    /// 根据人员ID获取定岗规则
    /// </summary>
    Task<List<FixedPositionRule>> GetFixedPositionRulesByPersonAsync(int personalId);

    /// <summary>
    /// 更新定岗规则
    /// </summary>
    Task UpdateFixedPositionRuleAsync(FixedPositionRule rule);

    /// <summary>
    /// 删除定岗规则
    /// </summary>
    Task DeleteFixedPositionRuleAsync(int id);

    // 手动指定管理
    /// <summary>
    /// 创建手动指定
    /// </summary>
    Task<int> CreateManualAssignmentAsync(ManualAssignment assignment);

    /// <summary>
    /// 根据日期范围获取手动指定
    /// </summary>
    Task<List<ManualAssignment>> GetManualAssignmentsByDateRangeAsync(DateTime startDate, DateTime endDate, bool enabledOnly = true);

    /// <summary>
    /// 更新手动指定
    /// </summary>
    Task UpdateManualAssignmentAsync(ManualAssignment assignment);

    /// <summary>
    /// 删除手动指定
    /// </summary>
    Task DeleteManualAssignmentAsync(int id);

    // 休息日配置管理
    /// <summary>
    /// 创建休息日配置
    /// </summary>
    Task<int> CreateHolidayConfigAsync(HolidayConfig config);

    /// <summary>
    /// 获取当前活跃的休息日配置
    /// </summary>
    Task<HolidayConfig?> GetActiveHolidayConfigAsync();

    /// <summary>
    /// 获取所有休息日配置
    /// </summary>
    Task<List<HolidayConfig>> GetAllHolidayConfigsAsync();

    /// <summary>
    /// 更新休息日配置
    /// </summary>
    Task UpdateHolidayConfigAsync(HolidayConfig config);

    /// <summary>
    /// 删除休息日配置
    /// </summary>
    Task DeleteHolidayConfigAsync(int id);

    // 业务规则验证
    /// <summary>
    /// 验证定岗规则是否有效
    /// </summary>
    Task<bool> ValidateFixedPositionRuleAsync(FixedPositionRule rule);

    /// <summary>
    /// 验证手动指定是否有效
    /// </summary>
    Task<bool> ValidateManualAssignmentAsync(ManualAssignment assignment);

    /// <summary>
    /// 检查指定日期是否为休息日
    /// </summary>
    Task<bool> IsHolidayAsync(DateTime date);

    // DTO-based methods for UI integration
    /// <summary>
    /// 获取所有定岗规则DTO
    /// </summary>
    Task<List<FixedAssignmentDto>> GetAllFixedAssignmentDtosAsync(bool enabledOnly = true);

    /// <summary>
    /// 根据人员ID获取定岗规则DTO
    /// </summary>
    Task<List<FixedAssignmentDto>> GetFixedAssignmentDtosByPersonAsync(int personalId);

    /// <summary>
    /// 创建定岗规则（使用DTO）
    /// </summary>
    Task<int> CreateFixedAssignmentAsync(CreateFixedAssignmentDto dto);

    /// <summary>
    /// 更新定岗规则（使用DTO）
    /// </summary>
    Task UpdateFixedAssignmentAsync(int id, UpdateFixedAssignmentDto dto);

    /// <summary>
    /// 根据日期范围获取手动指定DTO
    /// </summary>
    Task<List<ManualAssignmentDto>> GetManualAssignmentDtosByDateRangeAsync(DateTime startDate, DateTime endDate, bool enabledOnly = true);

    /// <summary>
    /// 创建手动指定（使用DTO）
    /// </summary>
    Task<int> CreateManualAssignmentAsync(CreateManualAssignmentDto dto);

    /// <summary>
    /// 更新手动指定（使用DTO）
    /// </summary>
    Task UpdateManualAssignmentAsync(int id, UpdateManualAssignmentDto dto);

    /// <summary>
    /// 获取所有休息日配置DTO
    /// </summary>
    Task<List<HolidayConfigDto>> GetAllHolidayConfigDtosAsync();

    /// <summary>
    /// 获取当前活跃的休息日配置DTO
    /// </summary>
    Task<HolidayConfigDto?> GetActiveHolidayConfigDtoAsync();

    /// <summary>
    /// 创建休息日配置（使用DTO）
    /// </summary>
    Task<int> CreateHolidayConfigAsync(CreateHolidayConfigDto dto);

    /// <summary>
    /// 更新休息日配置（使用DTO）
    /// </summary>
    Task UpdateHolidayConfigAsync(int id, UpdateHolidayConfigDto dto);
}