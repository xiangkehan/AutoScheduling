using AutoScheduling3.DTOs;
using AutoScheduling3.Models.Constraints;
using AutoScheduling3.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.DTOs.Mappers;

/// <summary>
/// 约束相关映射器
/// </summary>
public class ConstraintMapper
{
    private readonly IPersonalRepository _personnelRepository;
    private readonly IPositionRepository _positionRepository;

    public ConstraintMapper(
        IPersonalRepository personnelRepository,
        IPositionRepository positionRepository)
    {
        _personnelRepository = personnelRepository ?? throw new ArgumentNullException(nameof(personnelRepository));
        _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
    }

    #region FixedAssignmentDto 映射

    /// <summary>
    /// 将定岗规则模型转换为DTO
    /// </summary>
    public async Task<FixedAssignmentDto> ToFixedAssignmentDtoAsync(FixedPositionRule rule)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));

        var personnel = await _personnelRepository.GetByIdAsync(rule.PersonalId);
        var positions = await _positionRepository.GetPositionsByIdsAsync(rule.AllowedPositionIds);

        return new FixedAssignmentDto
        {
            Id = rule.Id,
            PersonnelId = rule.PersonalId,
            PersonnelName = personnel?.Name ?? "未知人员",
            AllowedPositionIds = rule.AllowedPositionIds,
            AllowedPositionNames = positions.Select(p => p.Name).ToList(),
            AllowedTimeSlots = rule.AllowedPeriods,
            StartDate = DateTime.Today, // 默认值，实际应从业务逻辑获取
            EndDate = DateTime.Today.AddDays(30), // 默认值，实际应从业务逻辑获取
            IsEnabled = rule.IsEnabled,
            RuleName = $"定岗规则-{personnel?.Name ?? rule.PersonalId.ToString()}",
            Description = rule.Description,
            CreatedAt = DateTime.Now, // 默认值，实际应从数据库获取
            UpdatedAt = DateTime.Now // 默认值，实际应从数据库获取
        };
    }

    /// <summary>
    /// 将定岗规则DTO转换为模型
    /// </summary>
    public FixedPositionRule ToFixedPositionRule(FixedAssignmentDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new FixedPositionRule
        {
            Id = dto.Id,
            PersonalId = dto.PersonnelId,
            AllowedPositionIds = dto.AllowedPositionIds,
            AllowedPeriods = dto.AllowedTimeSlots,
            IsEnabled = dto.IsEnabled,
            Description = dto.Description ?? string.Empty
        };
    }

    /// <summary>
    /// 将创建定岗规则DTO转换为模型
    /// </summary>
    public FixedPositionRule ToFixedPositionRule(CreateFixedAssignmentDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new FixedPositionRule
        {
            PersonalId = dto.PersonnelId,
            AllowedPositionIds = dto.AllowedPositionIds,
            AllowedPeriods = dto.AllowedTimeSlots,
            IsEnabled = dto.IsEnabled,
            Description = dto.Description ?? string.Empty
        };
    }

    /// <summary>
    /// 将更新定岗规则DTO转换为模型
    /// </summary>
    public FixedPositionRule ToFixedPositionRule(UpdateFixedAssignmentDto dto, int id)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new FixedPositionRule
        {
            Id = id,
            PersonalId = dto.PersonnelId,
            AllowedPositionIds = dto.AllowedPositionIds,
            AllowedPeriods = dto.AllowedTimeSlots,
            IsEnabled = dto.IsEnabled,
            Description = dto.Description ?? string.Empty
        };
    }

    #endregion

    #region ManualAssignmentDto 映射

    /// <summary>
    /// 将手动指定模型转换为DTO
    /// </summary>
    public async Task<ManualAssignmentDto> ToManualAssignmentDtoAsync(ManualAssignment assignment)
    {
        if (assignment == null) throw new ArgumentNullException(nameof(assignment));

        var personnel = await _personnelRepository.GetByIdAsync(assignment.PersonalId);
        var position = await _positionRepository.GetByIdAsync(assignment.PositionId);

        return new ManualAssignmentDto
        {
            Id = assignment.Id,
            PositionId = assignment.PositionId,
            PositionName = position?.Name ?? "未知哨位",
            TimeSlot = assignment.PeriodIndex,
            PersonnelId = assignment.PersonalId,
            PersonnelName = personnel?.Name ?? "未知人员",
            Date = assignment.Date,
            IsEnabled = assignment.IsEnabled,
            Remarks = assignment.Remarks,
            CreatedAt = DateTime.Now, // 默认值，实际应从数据库获取
            UpdatedAt = DateTime.Now // 默认值，实际应从数据库获取
        };
    }

    /// <summary>
    /// 将手动指定DTO转换为模型
    /// </summary>
    public ManualAssignment ToManualAssignment(ManualAssignmentDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new ManualAssignment
        {
            Id = dto.Id,
            PositionId = dto.PositionId,
            PeriodIndex = dto.TimeSlot,
            PersonalId = dto.PersonnelId,
            Date = dto.Date,
            IsEnabled = dto.IsEnabled,
            Remarks = dto.Remarks ?? string.Empty
        };
    }

    /// <summary>
    /// 将创建手动指定DTO转换为模型
    /// </summary>
    public ManualAssignment ToManualAssignment(CreateManualAssignmentDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new ManualAssignment
        {
            PositionId = dto.PositionId,
            PeriodIndex = dto.TimeSlot,
            PersonalId = dto.PersonnelId,
            Date = dto.Date,
            IsEnabled = dto.IsEnabled,
            Remarks = dto.Remarks ?? string.Empty
        };
    }

    /// <summary>
    /// 将更新手动指定DTO转换为模型
    /// </summary>
    public ManualAssignment ToManualAssignment(UpdateManualAssignmentDto dto, int id)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new ManualAssignment
        {
            Id = id,
            PositionId = dto.PositionId,
            PeriodIndex = dto.TimeSlot,
            PersonalId = dto.PersonnelId,
            Date = dto.Date,
            IsEnabled = dto.IsEnabled,
            Remarks = dto.Remarks ?? string.Empty
        };
    }

    #endregion

    #region HolidayConfigDto 映射

    /// <summary>
    /// 将休息日配置模型转换为DTO
    /// </summary>
    public HolidayConfigDto ToHolidayConfigDto(HolidayConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        return new HolidayConfigDto
        {
            Id = config.Id,
            ConfigName = config.ConfigName,
            EnableWeekendRule = config.EnableWeekendRule,
            WeekendDays = config.WeekendDays,
            LegalHolidays = config.LegalHolidays,
            CustomHolidays = config.CustomHolidays,
            ExcludedDates = config.ExcludedDates,
            IsActive = config.IsActive,
            CreatedAt = DateTime.Now, // 默认值，实际应从数据库获取
            UpdatedAt = DateTime.Now // 默认值，实际应从数据库获取
        };
    }

    /// <summary>
    /// 将休息日配置DTO转换为模型
    /// </summary>
    public HolidayConfig ToHolidayConfig(HolidayConfigDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new HolidayConfig
        {
            Id = dto.Id,
            ConfigName = dto.ConfigName,
            EnableWeekendRule = dto.EnableWeekendRule,
            WeekendDays = dto.WeekendDays,
            LegalHolidays = dto.LegalHolidays,
            CustomHolidays = dto.CustomHolidays,
            ExcludedDates = dto.ExcludedDates,
            IsActive = dto.IsActive
        };
    }

    /// <summary>
    /// 将创建休息日配置DTO转换为模型
    /// </summary>
    public HolidayConfig ToHolidayConfig(CreateHolidayConfigDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new HolidayConfig
        {
            ConfigName = dto.ConfigName,
            EnableWeekendRule = dto.EnableWeekendRule,
            WeekendDays = dto.WeekendDays,
            LegalHolidays = dto.LegalHolidays,
            CustomHolidays = dto.CustomHolidays,
            ExcludedDates = dto.ExcludedDates,
            IsActive = dto.IsActive
        };
    }

    /// <summary>
    /// 将更新休息日配置DTO转换为模型
    /// </summary>
    public HolidayConfig ToHolidayConfig(UpdateHolidayConfigDto dto, int id)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new HolidayConfig
        {
            Id = id,
            ConfigName = dto.ConfigName,
            EnableWeekendRule = dto.EnableWeekendRule,
            WeekendDays = dto.WeekendDays,
            LegalHolidays = dto.LegalHolidays,
            CustomHolidays = dto.CustomHolidays,
            ExcludedDates = dto.ExcludedDates,
            IsActive = dto.IsActive
        };
    }

    #endregion

    #region 批量映射方法

    /// <summary>
    /// 批量将定岗规则模型转换为DTO
    /// </summary>
    public async Task<List<FixedAssignmentDto>> ToFixedAssignmentDtoListAsync(IEnumerable<FixedPositionRule> rules)
    {
        if (rules == null) return new List<FixedAssignmentDto>();

        var result = new List<FixedAssignmentDto>();
        foreach (var rule in rules)
        {
            result.Add(await ToFixedAssignmentDtoAsync(rule));
        }
        return result;
    }

    /// <summary>
    /// 批量将手动指定模型转换为DTO
    /// </summary>
    public async Task<List<ManualAssignmentDto>> ToManualAssignmentDtoListAsync(IEnumerable<ManualAssignment> assignments)
    {
        if (assignments == null) return new List<ManualAssignmentDto>();

        var result = new List<ManualAssignmentDto>();
        foreach (var assignment in assignments)
        {
            result.Add(await ToManualAssignmentDtoAsync(assignment));
        }
        return result;
    }

    /// <summary>
    /// 批量将休息日配置模型转换为DTO
    /// </summary>
    public List<HolidayConfigDto> ToHolidayConfigDtoList(IEnumerable<HolidayConfig> configs)
    {
        if (configs == null) return new List<HolidayConfigDto>();

        return configs.Select(ToHolidayConfigDto).ToList();
    }

    #endregion
}