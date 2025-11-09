using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 技能数据传输对象
/// </summary>
public class SkillDto : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>
    /// 技能ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 技能名称
    /// </summary>
    [Required(ErrorMessage = "技能名称不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "技能名称长度必须在1-50字符之间")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 技能描述
    /// </summary>
    [StringLength(200, ErrorMessage = "技能描述长度不能超过200字符")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 是否激活
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
/// 创建技能DTO
/// </summary>
public class CreateSkillDto
{
    /// <summary>
    /// 技能名称（必填，1-50字符，唯一）
    /// </summary>
    [Required(ErrorMessage = "技能名称不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "技能名称长度必须在1-50字符之间")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 技能描述（可选，最多200字符）
    /// </summary>
    [StringLength(200, ErrorMessage = "技能描述长度不能超过200字符")]
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
    [Required(ErrorMessage = "技能名称不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "技能名称长度必须在1-50字符之间")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 技能描述（可选，最多200字符）
    /// </summary>
    [StringLength(200, ErrorMessage = "技能描述长度不能超过200字符")]
    public string? Description { get; set; }
    
    public bool IsActive { get; internal set; }
}
