using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 人员数据传输对象
/// </summary>
public class PersonnelDto
{
    /// <summary>
    /// 人员ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 职位ID
    /// </summary>
    public int PositionId { get; set; }

    /// <summary>
    /// 职位名称（冗余字段，便于显示）
    /// </summary>
    public string PositionName { get; set; } = string.Empty;

    /// <summary>
    /// 技能ID列表
    /// </summary>
    public List<int> SkillIds { get; set; } = new();

    /// <summary>
    /// 技能名称列表（冗余字段，便于显示）
    /// </summary>
    public List<string> SkillNames { get; set; } = new();

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// 是否已退役
    /// </summary>
    public bool IsRetired { get; set; }

    /// <summary>
    /// 最近班次间隔计数
    /// </summary>
    public int RecentShiftIntervalCount { get; set; }

    /// <summary>
    /// 最近节假日班次间隔计数
    /// </summary>
    public int RecentHolidayShiftIntervalCount { get; set; }

    /// <summary>
    /// 各时段班次间隔计数（12个时段）
    /// </summary>
    public int[] RecentPeriodShiftIntervals { get; set; } = new int[12];

    // 新增:兼容旧 XAML绑定的 IsActive 属性（在职且可用）
    public bool IsActive => IsAvailable && !IsRetired;
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
    /// 职位ID（必填）
    /// </summary>
    [Required(ErrorMessage = "职位ID不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "职位ID必须大于0")]
    public int PositionId { get; set; }

    /// <summary>
    /// 技能ID列表（至少一项）
    /// </summary>
    [Required(ErrorMessage = "技能列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一项技能")]
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
    /// 职位ID（必填）
    /// </summary>
    [Required(ErrorMessage = "职位ID不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "职位ID必须大于0")]
    public int PositionId { get; set; }

    /// <summary>
    /// 技能ID列表（至少一项）
    /// </summary>
    [Required(ErrorMessage = "技能列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一项技能")]
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
