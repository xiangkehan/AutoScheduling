using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.Models.Constraints;
using AutoScheduling3.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.Services;

/// <summary>
/// 约束管理服务实现
/// </summary>
public class ConstraintService : IConstraintService
{
    private readonly IConstraintRepository _constraintRepository;
    private readonly IPersonalRepository _personnelRepository;
    private readonly IPositionRepository _positionRepository;

    public ConstraintService(
        IConstraintRepository constraintRepository,
        IPersonalRepository personnelRepository,
        IPositionRepository positionRepository)
    {
        _constraintRepository = constraintRepository ?? throw new ArgumentNullException(nameof(constraintRepository));
        _personnelRepository = personnelRepository ?? throw new ArgumentNullException(nameof(personnelRepository));
        _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
    }

    #region 定岗规则管理

    public async Task<int> CreateFixedPositionRuleAsync(FixedPositionRule rule)
    {
        // 业务规则验证
        await ValidateFixedPositionRuleBusinessLogicAsync(rule);
        
        return await _constraintRepository.AddFixedPositionRuleAsync(rule);
    }

    public async Task<List<FixedPositionRule>> GetAllFixedPositionRulesAsync(bool enabledOnly = true)
    {
        return await _constraintRepository.GetAllFixedPositionRulesAsync(enabledOnly);
    }

    public async Task<List<FixedPositionRule>> GetFixedPositionRulesByPersonAsync(int personalId)
    {
        // 验证人员是否存在
        var personnelExists = await _personnelRepository.ExistsAsync(personalId);
        if (!personnelExists)
            throw new ArgumentException($"人员ID {personalId} 不存在");

        return await _constraintRepository.GetFixedPositionRulesByPersonAsync(personalId);
    }

    public async Task UpdateFixedPositionRuleAsync(FixedPositionRule rule)
    {
        // 业务规则验证
        await ValidateFixedPositionRuleBusinessLogicAsync(rule);
        
        await _constraintRepository.UpdateFixedPositionRuleAsync(rule);
    }

    public async Task DeleteFixedPositionRuleAsync(int id)
    {
        await _constraintRepository.DeleteFixedPositionRuleAsync(id);
    }

    #endregion

    #region 手动指定管理

    public async Task<int> CreateManualAssignmentAsync(ManualAssignment assignment)
    {
        // 业务规则验证
        await ValidateManualAssignmentBusinessLogicAsync(assignment);
        
        return await _constraintRepository.AddManualAssignmentAsync(assignment);
    }

    public async Task<List<ManualAssignment>> GetManualAssignmentsByDateRangeAsync(DateTime startDate, DateTime endDate, bool enabledOnly = true)
    {
        if (startDate > endDate)
            throw new ArgumentException("开始日期不能晚于结束日期");

        return await _constraintRepository.GetManualAssignmentsByDateRangeAsync(startDate, endDate, enabledOnly);
    }

    public async Task UpdateManualAssignmentAsync(ManualAssignment assignment)
    {
        // 业务规则验证
        await ValidateManualAssignmentBusinessLogicAsync(assignment);
        
        await _constraintRepository.UpdateManualAssignmentAsync(assignment);
    }

    public async Task DeleteManualAssignmentAsync(int id)
    {
        await _constraintRepository.DeleteManualAssignmentAsync(id);
    }

    #endregion

    #region 休息日配置管理

    public async Task<int> CreateHolidayConfigAsync(HolidayConfig config)
    {
        // 业务规则验证
        ValidateHolidayConfigBusinessLogic(config);
        
        // 如果设置为活跃配置，需要将其他配置设为非活跃
        if (config.IsActive)
        {
            await DeactivateOtherHolidayConfigsAsync();
        }
        
        return await _constraintRepository.AddHolidayConfigAsync(config);
    }

    public async Task<HolidayConfig?> GetActiveHolidayConfigAsync()
    {
        return await _constraintRepository.GetActiveHolidayConfigAsync();
    }

    public async Task<List<HolidayConfig>> GetAllHolidayConfigsAsync()
    {
        return await _constraintRepository.GetAllHolidayConfigsAsync();
    }

    public async Task UpdateHolidayConfigAsync(HolidayConfig config)
    {
        // 业务规则验证
        ValidateHolidayConfigBusinessLogic(config);
        
        // 如果设置为活跃配置，需要将其他配置设为非活跃
        if (config.IsActive)
        {
            await DeactivateOtherHolidayConfigsAsync(config.Id);
        }
        
        await _constraintRepository.UpdateHolidayConfigAsync(config);
    }

    public async Task DeleteHolidayConfigAsync(int id)
    {
        await _constraintRepository.DeleteHolidayConfigAsync(id);
    }

    #endregion

    #region 业务规则验证

    public async Task<bool> ValidateFixedPositionRuleAsync(FixedPositionRule rule)
    {
        try
        {
            await ValidateFixedPositionRuleBusinessLogicAsync(rule);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ValidateManualAssignmentAsync(ManualAssignment assignment)
    {
        try
        {
            await ValidateManualAssignmentBusinessLogicAsync(assignment);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsHolidayAsync(DateTime date)
    {
        var activeConfig = await GetActiveHolidayConfigAsync();
        return activeConfig?.IsHoliday(date) ?? false;
    }

    #endregion

    #region 私有验证方法

    private async Task ValidateFixedPositionRuleBusinessLogicAsync(FixedPositionRule rule)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        // 验证人员是否存在
        var personnelExists = await _personnelRepository.ExistsAsync(rule.PersonalId);
        if (!personnelExists)
            throw new ArgumentException($"人员ID {rule.PersonalId} 不存在");

        // 验证哨位ID是否有效
        if (rule.AllowedPositionIds?.Any() == true)
        {
            foreach (var positionId in rule.AllowedPositionIds)
            {
                var positionExists = await _positionRepository.ExistsAsync(positionId);
                if (!positionExists)
                    throw new ArgumentException($"哨位ID {positionId} 不存在");
            }
        }

        // 验证时段序号是否有效（0-11）
        if (rule.AllowedPeriods?.Any() == true)
        {
            var invalidPeriods = rule.AllowedPeriods.Where(p => p < 0 || p > 11).ToList();
            if (invalidPeriods.Any())
                throw new ArgumentException($"时段序号无效: {string.Join(", ", invalidPeriods)}，有效范围为0-11");
        }
    }

    private async Task ValidateManualAssignmentBusinessLogicAsync(ManualAssignment assignment)
    {
        if (assignment == null)
            throw new ArgumentNullException(nameof(assignment));

        // 验证人员是否存在
        var personnelExists = await _personnelRepository.ExistsAsync(assignment.PersonalId);
        if (!personnelExists)
            throw new ArgumentException($"人员ID {assignment.PersonalId} 不存在");

        // 验证哨位是否存在
        var positionExists = await _positionRepository.ExistsAsync(assignment.PositionId);
        if (!positionExists)
            throw new ArgumentException($"哨位ID {assignment.PositionId} 不存在");

        // 验证时段序号是否有效（0-11）
        if (assignment.PeriodIndex < 0 || assignment.PeriodIndex > 11)
            throw new ArgumentException($"时段序号 {assignment.PeriodIndex} 无效，有效范围为0-11");

        // 验证日期不能是过去的日期
        if (assignment.Date.Date < DateTime.Today)
            throw new ArgumentException("不能为过去的日期创建手动指定");

        // 验证人员技能是否匹配哨位要求
        var personnel = await _personnelRepository.GetByIdAsync(assignment.PersonalId);
        var position = await _positionRepository.GetByIdAsync(assignment.PositionId);
        
        if (personnel != null && position != null)
        {
            // 检查人员是否可用
            if (!personnel.IsAvailable || personnel.IsRetired)
                throw new ArgumentException($"人员 {personnel.Name} 当前不可用");

            // 检查技能匹配
            if (position.RequiredSkillIds?.Any() == true && personnel.SkillIds?.Any() == true)
            {
                var missingSkills = position.RequiredSkillIds.Except(personnel.SkillIds).ToList();
                if (missingSkills.Any())
                    throw new ArgumentException($"人员 {personnel.Name} 缺少哨位所需技能");
            }
        }
    }

    private void ValidateHolidayConfigBusinessLogic(HolidayConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(config.ConfigName))
            throw new ArgumentException("配置名称不能为空");

        if (config.ConfigName.Length > 100)
            throw new ArgumentException("配置名称长度不能超过100字符");

        // 验证周末日期设置
        if (config.EnableWeekendRule && (config.WeekendDays == null || !config.WeekendDays.Any()))
            throw new ArgumentException("启用周末规则时必须指定至少一个周末日期");
    }

    private async Task DeactivateOtherHolidayConfigsAsync(int? excludeId = null)
    {
        var allConfigs = await GetAllHolidayConfigsAsync();
        var configsToDeactivate = allConfigs.Where(c => c.IsActive && c.Id != excludeId).ToList();
        
        foreach (var config in configsToDeactivate)
        {
            config.IsActive = false;
            await _constraintRepository.UpdateHolidayConfigAsync(config);
        }
    }

    #endregion
}