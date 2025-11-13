using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Services.ImportExport.Mappers;

/// <summary>
/// Maps DTO objects to database field dictionaries for batch operations
/// </summary>
public static class FieldMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    /// <summary>
    /// Maps SkillDto to database fields
    /// </summary>
    public static Dictionary<string, object> MapSkillToFields(SkillDto skill)
    {
        return new Dictionary<string, object>
        {
            ["Id"] = skill.Id,
            ["Name"] = skill.Name,
            ["Description"] = skill.Description ?? string.Empty,
            ["IsActive"] = skill.IsActive ? 1 : 0,
            ["CreatedAt"] = skill.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            ["UpdatedAt"] = skill.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    /// <summary>
    /// Maps PersonnelDto to database fields
    /// </summary>
    public static Dictionary<string, object> MapPersonnelToFields(PersonnelDto personnel)
    {
        return new Dictionary<string, object>
        {
            ["Id"] = personnel.Id,
            ["Name"] = personnel.Name,
            ["Position"] = string.Empty, // Legacy field, kept for compatibility
            ["SkillIds"] = JsonSerializer.Serialize(personnel.SkillIds, JsonOptions),
            ["IsAvailable"] = personnel.IsAvailable ? 1 : 0,
            ["IsRetired"] = personnel.IsRetired ? 1 : 0,
            ["RecentShiftInterval"] = personnel.RecentShiftIntervalCount,
            ["RecentHolidayShiftInterval"] = personnel.RecentHolidayShiftIntervalCount,
            ["RecentTimeSlotIntervals"] = JsonSerializer.Serialize(personnel.RecentPeriodShiftIntervals, JsonOptions),
            ["CreatedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            ["UpdatedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    /// <summary>
    /// Maps PositionDto to database fields
    /// </summary>
    public static Dictionary<string, object> MapPositionToFields(PositionDto position)
    {
        return new Dictionary<string, object>
        {
            ["Id"] = position.Id,
            ["Name"] = position.Name,
            ["Location"] = position.Location,
            ["Description"] = position.Description ?? string.Empty,
            ["Requirements"] = position.Requirements ?? string.Empty,
            ["RequiredSkillIds"] = JsonSerializer.Serialize(position.RequiredSkillIds, JsonOptions),
            ["AvailablePersonnelIds"] = JsonSerializer.Serialize(position.AvailablePersonnelIds, JsonOptions),
            ["IsActive"] = position.IsActive ? 1 : 0,
            ["CreatedAt"] = position.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            ["UpdatedAt"] = position.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    /// <summary>
    /// Maps SchedulingTemplateDto to database fields
    /// </summary>
    public static Dictionary<string, object> MapTemplateToFields(SchedulingTemplateDto template)
    {
        return new Dictionary<string, object>
        {
            ["Id"] = template.Id,
            ["Name"] = template.Name,
            ["Description"] = template.Description ?? string.Empty,
            ["TemplateType"] = template.TemplateType,
            ["IsDefault"] = template.IsDefault ? 1 : 0,
            ["PersonnelIds"] = JsonSerializer.Serialize(template.PersonnelIds, JsonOptions),
            ["PositionIds"] = JsonSerializer.Serialize(template.PositionIds, JsonOptions),
            ["HolidayConfigId"] = template.HolidayConfigId.HasValue ? (object)template.HolidayConfigId.Value : DBNull.Value,
            ["UseActiveHolidayConfig"] = template.UseActiveHolidayConfig ? 1 : 0,
            ["EnabledFixedRuleIds"] = JsonSerializer.Serialize(template.EnabledFixedRuleIds, JsonOptions),
            ["EnabledManualAssignmentIds"] = JsonSerializer.Serialize(template.EnabledManualAssignmentIds, JsonOptions),
            ["DurationDays"] = template.DurationDays,
            ["StrategyConfig"] = template.StrategyConfig,
            ["UsageCount"] = template.UsageCount,
            ["IsActive"] = template.IsActive ? 1 : 0,
            ["CreatedAt"] = template.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            ["UpdatedAt"] = template.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            ["LastUsedAt"] = template.LastUsedAt.HasValue 
                ? template.LastUsedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") 
                : (object)DBNull.Value
        };
    }

    /// <summary>
    /// Maps FixedAssignmentDto to database fields
    /// Note: Database uses PersonalId and AllowedPeriods instead of PersonnelId and AllowedTimeSlots
    /// </summary>
    public static Dictionary<string, object> MapFixedAssignmentToFields(FixedAssignmentDto fixedAssignment)
    {
        return new Dictionary<string, object>
        {
            ["Id"] = fixedAssignment.Id,
            ["PersonalId"] = fixedAssignment.PersonnelId,
            ["AllowedPositionIds"] = JsonSerializer.Serialize(fixedAssignment.AllowedPositionIds, JsonOptions),
            ["AllowedPeriods"] = JsonSerializer.Serialize(fixedAssignment.AllowedTimeSlots, JsonOptions),
            ["IsEnabled"] = fixedAssignment.IsEnabled ? 1 : 0,
            ["Description"] = fixedAssignment.Description ?? string.Empty
        };
    }

    /// <summary>
    /// Maps ManualAssignmentDto to database fields
    /// Note: Database uses PersonalId and PeriodIndex instead of PersonnelId and TimeSlot
    /// </summary>
    public static Dictionary<string, object> MapManualAssignmentToFields(ManualAssignmentDto manualAssignment)
    {
        return new Dictionary<string, object>
        {
            ["Id"] = manualAssignment.Id,
            ["PositionId"] = manualAssignment.PositionId,
            ["PeriodIndex"] = manualAssignment.TimeSlot,
            ["PersonalId"] = manualAssignment.PersonnelId,
            ["Date"] = manualAssignment.Date.ToString("yyyy-MM-dd"),
            ["IsEnabled"] = manualAssignment.IsEnabled ? 1 : 0,
            ["Remarks"] = manualAssignment.Remarks ?? string.Empty
        };
    }

    /// <summary>
    /// Maps HolidayConfigDto to database fields
    /// </summary>
    public static Dictionary<string, object> MapHolidayConfigToFields(HolidayConfigDto holidayConfig)
    {
        // Convert DayOfWeek enum values to integers (Sunday=0, Monday=1, etc.)
        var weekendDayInts = holidayConfig.WeekendDays.Select(d => (int)d).ToList();
        
        // Convert DateTime lists to ISO date strings
        var legalHolidayStrings = holidayConfig.LegalHolidays.Select(d => d.ToString("yyyy-MM-dd")).ToList();
        var customHolidayStrings = holidayConfig.CustomHolidays.Select(d => d.ToString("yyyy-MM-dd")).ToList();
        var excludedDateStrings = holidayConfig.ExcludedDates.Select(d => d.ToString("yyyy-MM-dd")).ToList();

        return new Dictionary<string, object>
        {
            ["Id"] = holidayConfig.Id,
            ["ConfigName"] = holidayConfig.ConfigName,
            ["EnableWeekendRule"] = holidayConfig.EnableWeekendRule ? 1 : 0,
            ["WeekendDays"] = JsonSerializer.Serialize(weekendDayInts, JsonOptions),
            ["LegalHolidays"] = JsonSerializer.Serialize(legalHolidayStrings, JsonOptions),
            ["CustomHolidays"] = JsonSerializer.Serialize(customHolidayStrings, JsonOptions),
            ["ExcludedDates"] = JsonSerializer.Serialize(excludedDateStrings, JsonOptions),
            ["IsActive"] = holidayConfig.IsActive ? 1 : 0
        };
    }
}
