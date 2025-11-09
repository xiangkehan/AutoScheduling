using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 人员数据传输对象
/// </summary>
public class PersonnelDto : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>
    /// 人员ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 姓名
    /// </summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "姓名长度必须在1-50字符之间")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 技能ID列表
    /// </summary>
    [JsonPropertyName("skillIds")]
    public List<int> SkillIds { get; set; } = new();

    /// <summary>
    /// 技能名称列表（冗余字段，便于显示）
    /// </summary>
    [JsonPropertyName("skillNames")]
    public List<string> SkillNames { get; set; } = new();

    /// <summary>
    /// 是否可用
    /// </summary>
    [JsonPropertyName("isAvailable")]
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// 是否已退役
    /// </summary>
    [JsonPropertyName("isRetired")]
    public bool IsRetired { get; set; }

    /// <summary>
    /// 最近班次间隔计数
    /// </summary>
    [Range(0, 999, ErrorMessage = "班次间隔计数必须在0-999之间")]
    [JsonPropertyName("recentShiftIntervalCount")]
    public int RecentShiftIntervalCount { get; set; }

    /// <summary>
    /// 最近节假日班次间隔计数
    /// </summary>
    [Range(0, 999, ErrorMessage = "节假日班次间隔计数必须在0-999之间")]
    [JsonPropertyName("recentHolidayShiftIntervalCount")]
    public int RecentHolidayShiftIntervalCount { get; set; }

    /// <summary>
    /// 各时段班次间隔计数（12个时段）
    /// </summary>
    [Required(ErrorMessage = "时段间隔数组不能为空")]
    [MinLength(12, ErrorMessage = "时段间隔数组必须包含12个元素")]
    [MaxLength(12, ErrorMessage = "时段间隔数组必须包含12个元素")]
    [JsonPropertyName("recentPeriodShiftIntervals")]
    public int[] RecentPeriodShiftIntervals { get; set; } = new int[12];

    // 新增:兼容旧 XAML绑定的 IsActive 属性（在职且可用）
    public bool IsActive => IsAvailable && !IsRetired;

    /// <summary>
    /// 触发属性变更通知
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 创建人员DTO
/// </summary>
public class CreatePersonnelDto
{
    /// <summary>
    /// 姓名（必填，1-50字符）
    /// </summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "姓名长度必须在1-50字符之间")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 技能ID列表（可选）
    /// </summary>
    public List<int> SkillIds { get; set; } = new();

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// 最近班次间隔计数（0-999）
    /// </summary>
    [Range(0, 999, ErrorMessage = "班次间隔计数必须在0-999之间")]
    public int RecentShiftIntervalCount { get; set; }

    /// <summary>
    /// 最近节假日班次间隔计数（0-999）
    /// </summary>
    [Range(0, 999, ErrorMessage = "节假日班次间隔计数必须在0-999之间")]
    public int RecentHolidayShiftIntervalCount { get; set; }

    /// <summary>
    /// 各时段班次间隔计数（12个时段，每项0-999）
    /// </summary>
    [Required(ErrorMessage = "时段间隔数组不能为空")]
    [MinLength(12, ErrorMessage = "时段间隔数组必须包含12个元素")]
    [MaxLength(12, ErrorMessage = "时段间隔数组必须包含12个元素")]
    public int[] RecentPeriodShiftIntervals { get; set; } = new int[12];
}

/// <summary>
/// 更新人员DTO
/// </summary>
public class UpdatePersonnelDto
{
    /// <summary>
    /// 姓名（必填，1-50字符）
    /// </summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "姓名长度必须在1-50字符之间")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 技能ID列表（可选）
    /// </summary>
    public List<int> SkillIds { get; set; } = new();

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 最近班次间隔计数
    /// </summary>
    public int RecentShiftIntervalCount { get; set; }

    /// <summary>
    /// 最近节假日班次间隔计数
    /// </summary>
    public int RecentHolidayShiftIntervalCount { get; set; }

    /// <summary>
    /// 各时段班次间隔计数
    /// </summary>
    public int[] RecentPeriodShiftIntervals { get; set; } = new int[12];
    public bool IsRetired { get; internal set; }
}
