using System.Collections.Generic;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Services.ImportExport;

/// <summary>
/// 数据映射服务实现 - 负责DTO与模型之间的双向转换
/// </summary>
public class DataMappingService : IDataMappingService
{
    #region DTO to Model Mapping
    
    /// <summary>
    /// 将 SkillDto 映射到 Skill 模型
    /// </summary>
    public AutoScheduling3.Models.Skill MapToSkill(SkillDto dto)
    {
        return new AutoScheduling3.Models.Skill
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            IsActive = dto.IsActive,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }
    
    /// <summary>
    /// 将 PersonnelDto 映射到 Personal 模型
    /// </summary>
    public AutoScheduling3.Models.Personal MapToPersonnel(PersonnelDto dto)
    {
        return new AutoScheduling3.Models.Personal
        {
            Id = dto.Id,
            Name = dto.Name,
            SkillIds = dto.SkillIds,
            IsAvailable = dto.IsAvailable,
            IsRetired = dto.IsRetired,
            RecentShiftIntervalCount = dto.RecentShiftIntervalCount,
            RecentHolidayShiftIntervalCount = dto.RecentHolidayShiftIntervalCount,
            RecentPeriodShiftIntervals = dto.RecentPeriodShiftIntervals
        };
    }
    
    /// <summary>
    /// 将 PositionDto 映射到 PositionLocation 模型
    /// </summary>
    public AutoScheduling3.Models.PositionLocation MapToPosition(PositionDto dto)
    {
        return new AutoScheduling3.Models.PositionLocation
        {
            Id = dto.Id,
            Name = dto.Name,
            Location = dto.Location,
            Description = dto.Description ?? string.Empty,
            Requirements = dto.Requirements ?? string.Empty,
            RequiredSkillIds = dto.RequiredSkillIds,
            AvailablePersonnelIds = dto.AvailablePersonnelIds,
            IsActive = dto.IsActive,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }
    
    /// <summary>
    /// 将 HolidayConfigDto 映射到 HolidayConfig 模型
    /// </summary>
    public AutoScheduling3.Models.Constraints.HolidayConfig MapToHolidayConfig(HolidayConfigDto dto)
    {
        return new AutoScheduling3.Models.Constraints.HolidayConfig
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
    /// 将 SchedulingTemplateDto 映射到 SchedulingTemplate 模型
    /// </summary>
    public AutoScheduling3.Models.SchedulingTemplate MapToTemplate(SchedulingTemplateDto dto)
    {
        return new AutoScheduling3.Models.SchedulingTemplate
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            TemplateType = dto.TemplateType,
            IsDefault = dto.IsDefault,
            PersonnelIds = dto.PersonnelIds,
            PositionIds = dto.PositionIds,
            HolidayConfigId = dto.HolidayConfigId,
            UseActiveHolidayConfig = dto.UseActiveHolidayConfig,
            EnabledFixedRuleIds = dto.EnabledFixedRuleIds,
            EnabledManualAssignmentIds = dto.EnabledManualAssignmentIds,
            DurationDays = dto.DurationDays,
            StrategyConfig = dto.StrategyConfig,
            UsageCount = dto.UsageCount,
            IsActive = dto.IsActive,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            LastUsedAt = dto.LastUsedAt
        };
    }
    
    /// <summary>
    /// 将 FixedAssignmentDto 映射到 FixedPositionRule 模型
    /// </summary>
    public AutoScheduling3.Models.Constraints.FixedPositionRule MapToFixedPositionRule(FixedAssignmentDto dto)
    {
        return new AutoScheduling3.Models.Constraints.FixedPositionRule
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
    /// 将 ManualAssignmentDto 映射到 ManualAssignment 模型
    /// </summary>
    public AutoScheduling3.Models.Constraints.ManualAssignment MapToManualAssignment(ManualAssignmentDto dto)
    {
        return new AutoScheduling3.Models.Constraints.ManualAssignment
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
    
    #endregion
    
    #region Model to DTO Mapping
    
    /// <summary>
    /// 将 Skill 模型映射到 SkillDto
    /// </summary>
    public SkillDto MapToSkillDto(AutoScheduling3.Models.Skill model)
    {
        return new SkillDto
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }
    
    /// <summary>
    /// 将 Personal 模型映射到 PersonnelDto
    /// </summary>
    public PersonnelDto MapToPersonnelDto(AutoScheduling3.Models.Personal model)
    {
        return new PersonnelDto
        {
            Id = model.Id,
            Name = model.Name,
            SkillIds = new List<int>(model.SkillIds),
            IsAvailable = model.IsAvailable,
            IsRetired = model.IsRetired,
            RecentShiftIntervalCount = model.RecentShiftIntervalCount,
            RecentHolidayShiftIntervalCount = model.RecentHolidayShiftIntervalCount,
            RecentPeriodShiftIntervals = (int[])model.RecentPeriodShiftIntervals.Clone()
        };
    }
    
    /// <summary>
    /// 将 PositionLocation 模型映射到 PositionDto
    /// </summary>
    public PositionDto MapToPositionDto(AutoScheduling3.Models.PositionLocation model)
    {
        return new PositionDto
        {
            Id = model.Id,
            Name = model.Name,
            Location = model.Location,
            Description = model.Description,
            Requirements = model.Requirements,
            RequiredSkillIds = new List<int>(model.RequiredSkillIds),
            AvailablePersonnelIds = new List<int>(model.AvailablePersonnelIds),
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }
    
    /// <summary>
    /// 将 HolidayConfig 模型映射到 HolidayConfigDto
    /// </summary>
    public HolidayConfigDto MapToHolidayConfigDto(AutoScheduling3.Models.Constraints.HolidayConfig model)
    {
        return new HolidayConfigDto
        {
            Id = model.Id,
            ConfigName = model.ConfigName,
            EnableWeekendRule = model.EnableWeekendRule,
            WeekendDays = new List<System.DayOfWeek>(model.WeekendDays),
            LegalHolidays = new List<System.DateTime>(model.LegalHolidays),
            CustomHolidays = new List<System.DateTime>(model.CustomHolidays),
            ExcludedDates = new List<System.DateTime>(model.ExcludedDates),
            IsActive = model.IsActive,
            CreatedAt = System.DateTime.UtcNow, // Not stored in database
            UpdatedAt = System.DateTime.UtcNow // Not stored in database
        };
    }
    
    /// <summary>
    /// 将 SchedulingTemplate 模型映射到 SchedulingTemplateDto
    /// </summary>
    public SchedulingTemplateDto MapToTemplateDto(AutoScheduling3.Models.SchedulingTemplate model)
    {
        return new SchedulingTemplateDto
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            TemplateType = model.TemplateType,
            IsDefault = model.IsDefault,
            PersonnelIds = new List<int>(model.PersonnelIds),
            PositionIds = new List<int>(model.PositionIds),
            HolidayConfigId = model.HolidayConfigId,
            UseActiveHolidayConfig = model.UseActiveHolidayConfig,
            EnabledFixedRuleIds = new List<int>(model.EnabledFixedRuleIds),
            EnabledManualAssignmentIds = new List<int>(model.EnabledManualAssignmentIds),
            DurationDays = model.DurationDays,
            StrategyConfig = model.StrategyConfig,
            UsageCount = model.UsageCount,
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            LastUsedAt = model.LastUsedAt
        };
    }
    
    /// <summary>
    /// 将 FixedPositionRule 模型映射到 FixedAssignmentDto
    /// </summary>
    public FixedAssignmentDto MapToFixedAssignmentDto(AutoScheduling3.Models.Constraints.FixedPositionRule model)
    {
        return new FixedAssignmentDto
        {
            Id = model.Id,
            PersonnelId = model.PersonalId,
            AllowedPositionIds = new List<int>(model.AllowedPositionIds),
            AllowedTimeSlots = new List<int>(model.AllowedPeriods),
            StartDate = System.DateTime.MinValue, // Not stored in database
            EndDate = System.DateTime.MaxValue, // Not stored in database
            IsEnabled = model.IsEnabled,
            RuleName = $"Rule_{model.Id}",
            Description = model.Description,
            CreatedAt = System.DateTime.UtcNow, // Not stored in database
            UpdatedAt = System.DateTime.UtcNow // Not stored in database
        };
    }
    
    /// <summary>
    /// 将 ManualAssignment 模型映射到 ManualAssignmentDto
    /// </summary>
    public ManualAssignmentDto MapToManualAssignmentDto(AutoScheduling3.Models.Constraints.ManualAssignment model)
    {
        return new ManualAssignmentDto
        {
            Id = model.Id,
            PositionId = model.PositionId,
            TimeSlot = model.PeriodIndex,
            PersonnelId = model.PersonalId,
            Date = model.Date,
            IsEnabled = model.IsEnabled,
            Remarks = model.Remarks,
            CreatedAt = System.DateTime.UtcNow, // Not stored in database
            UpdatedAt = System.DateTime.UtcNow // Not stored in database
        };
    }
    
    #endregion
}
