using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 休息日配置数据传输对象
/// </summary>
public class HolidayConfigDto
{
    /// <summary>
    /// 配置ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 配置名称
    /// </summary>
    [Required(ErrorMessage = "配置名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "配置名称长度必须在1-100字符之间")]
    [JsonPropertyName("configName")]
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用周末规则
    /// </summary>
    [JsonPropertyName("enableWeekendRule")]
    public bool EnableWeekendRule { get; set; } = true;

    /// <summary>
    /// 周末日期列表
    /// </summary>
    [JsonPropertyName("weekendDays")]
    public List<DayOfWeek> WeekendDays { get; set; } = new() { DayOfWeek.Saturday, DayOfWeek.Sunday };

    /// <summary>
    /// 法定节假日日期列表
    /// </summary>
    [JsonPropertyName("legalHolidays")]
    public List<DateTime> LegalHolidays { get; set; } = new();

    /// <summary>
    /// 自定义休息日日期列表
    /// </summary>
    [JsonPropertyName("customHolidays")]
    public List<DateTime> CustomHolidays { get; set; } = new();

    /// <summary>
    /// 排除日期列表（强制为工作日）
    /// </summary>
    [JsonPropertyName("excludedDates")]
    public List<DateTime> ExcludedDates { get; set; } = new();

    /// <summary>
    /// 是否为当前启用配置
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 创建休息日配置DTO
/// </summary>
public class CreateHolidayConfigDto
{
    /// <summary>
    /// 配置名称（必填）
    /// </summary>
    [Required(ErrorMessage = "配置名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "配置名称长度必须在1-100字符之间")]
    [JsonPropertyName("configName")]
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用周末规则（默认true）
    /// </summary>
    [JsonPropertyName("enableWeekendRule")]
    public bool EnableWeekendRule { get; set; } = true;

    /// <summary>
    /// 周末日期列表（默认周六周日）
    /// </summary>
    [JsonPropertyName("weekendDays")]
    public List<DayOfWeek> WeekendDays { get; set; } = new() { DayOfWeek.Saturday, DayOfWeek.Sunday };

    /// <summary>
    /// 法定节假日日期列表（可选）
    /// </summary>
    [JsonPropertyName("legalHolidays")]
    public List<DateTime> LegalHolidays { get; set; } = new();

    /// <summary>
    /// 自定义休息日日期列表（可选）
    /// </summary>
    [JsonPropertyName("customHolidays")]
    public List<DateTime> CustomHolidays { get; set; } = new();

    /// <summary>
    /// 排除日期列表（可选）
    /// </summary>
    [JsonPropertyName("excludedDates")]
    public List<DateTime> ExcludedDates { get; set; } = new();

    /// <summary>
    /// 是否为当前启用配置（默认true）
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 自定义验证：启用周末规则时必须指定周末日期
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EnableWeekendRule && (WeekendDays == null || !WeekendDays.Any()))
        {
            yield return new ValidationResult(
                "启用周末规则时必须指定至少一个周末日期",
                new[] { nameof(WeekendDays) });
        }
    }
}

/// <summary>
/// 更新休息日配置DTO
/// </summary>
public class UpdateHolidayConfigDto
{
    /// <summary>
    /// 配置名称（必填）
    /// </summary>
    [Required(ErrorMessage = "配置名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "配置名称长度必须在1-100字符之间")]
    [JsonPropertyName("configName")]
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用周末规则
    /// </summary>
    [JsonPropertyName("enableWeekendRule")]
    public bool EnableWeekendRule { get; set; }

    /// <summary>
    /// 周末日期列表
    /// </summary>
    [JsonPropertyName("weekendDays")]
    public List<DayOfWeek> WeekendDays { get; set; } = new();

    /// <summary>
    /// 法定节假日日期列表
    /// </summary>
    [JsonPropertyName("legalHolidays")]
    public List<DateTime> LegalHolidays { get; set; } = new();

    /// <summary>
    /// 自定义休息日日期列表
    /// </summary>
    [JsonPropertyName("customHolidays")]
    public List<DateTime> CustomHolidays { get; set; } = new();

    /// <summary>
    /// 排除日期列表
    /// </summary>
    [JsonPropertyName("excludedDates")]
    public List<DateTime> ExcludedDates { get; set; } = new();

    /// <summary>
    /// 是否为当前启用配置
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    /// <summary>
    /// 自定义验证：启用周末规则时必须指定周末日期
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EnableWeekendRule && (WeekendDays == null || !WeekendDays.Any()))
        {
            yield return new ValidationResult(
                "启用周末规则时必须指定至少一个周末日期",
                new[] { nameof(WeekendDays) });
        }
    }
}