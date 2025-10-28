namespace AutoScheduling3.DTOs;

/// <summary>
/// 技能数据传输对象
/// </summary>
public class SkillDto
{
    /// <summary>
    /// 技能ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 技能名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 技能描述
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 创建技能DTO
/// </summary>
public class CreateSkillDto
{
    /// <summary>
    /// 技能名称（必填，1-50字符，唯一）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 技能描述（可选，最多200字符）
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 更新技能DTO
/// </summary>
public class UpdateSkillDto
{
    /// <summary>
    /// 技能名称（必填，1-50字符，唯一）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 技能描述（可选，最多200字符）
    /// </summary>
    public string? Description { get; set; }
}
