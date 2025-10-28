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
}

/// <summary>
/// 创建人员DTO
/// </summary>
public class CreatePersonnelDto
{
    /// <summary>
    /// 姓名（必填，1-50字符）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 职位ID（必填）
    /// </summary>
    public int PositionId { get; set; }

    /// <summary>
    /// 技能ID列表（至少一项）
    /// </summary>
    public List<int> SkillIds { get; set; } = new();

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// 最近班次间隔计数（0-999）
    /// </summary>
    public int RecentShiftIntervalCount { get; set; }

    /// <summary>
    /// 最近节假日班次间隔计数（0-999）
    /// </summary>
    public int RecentHolidayShiftIntervalCount { get; set; }

    /// <summary>
    /// 各时段班次间隔计数（12个时段，每项0-999）
    /// </summary>
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
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 职位ID（必填）
    /// </summary>
    public int PositionId { get; set; }

    /// <summary>
    /// 技能ID列表（至少一项）
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
}
