using System.Collections.Generic;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 哨位/职位数据传输对象
/// </summary>
public class PositionDto
{
    /// <summary>
    /// 哨位ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 哨位名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 地点
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// 介绍
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 要求说明
    /// </summary>
    public string? Requirements { get; set; }

    /// <summary>
    /// 所需技能ID列表
    /// </summary>
    public List<int> RequiredSkillIds { get; set; } = new();

    /// <summary>
    /// 所需技能名称列表（冗余字段，便于显示）
    /// </summary>
    public List<string> RequiredSkillNames { get; set; } = new();
}

/// <summary>
/// 创建哨位DTO
/// </summary>
public class CreatePositionDto
{
    /// <summary>
    /// 哨位名称（必填，1-100字符）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 地点（必填，1-200字符）
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// 介绍（可选，最多500字符）
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 要求说明（可选，最多1000字符）
    /// </summary>
    public string? Requirements { get; set; }

    /// <summary>
    /// 所需技能ID列表（至少一项）
    /// </summary>
    public List<int> RequiredSkillIds { get; set; } = new();
}

/// <summary>
/// 更新哨位DTO
/// </summary>
public class UpdatePositionDto
{
    /// <summary>
    /// 哨位名称（必填，1-100字符）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 地点（必填，1-200字符）
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// 介绍（可选，最多500字符）
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 要求说明（可选，最多1000字符）
    /// </summary>
    public string? Requirements { get; set; }

    /// <summary>
    /// 所需技能ID列表（至少一项）
    /// </summary>
    public List<int> RequiredSkillIds { get; set; } = new();
}
