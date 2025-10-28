using AutoScheduling3.DTOs;
using AutoScheduling3.Models;

namespace AutoScheduling3.DTOs.Mappers;

/// <summary>
/// 技能数据映射器 - Model 与 DTO 互转
/// </summary>
public class SkillMapper
{
    /// <summary>
    /// Model 转 DTO
    /// </summary>
    public SkillDto ToDto(Skill model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new SkillDto
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description ?? string.Empty,
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }

    /// <summary>
    /// 创建 DTO 转 Model
    /// </summary>
    public Skill ToModel(CreateSkillDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new Skill
        {
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            IsActive = true, // 新建默认激活
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 更新 DTO 到现有 Model
    /// </summary>
    public void UpdateModel(Skill model, UpdateSkillDto dto)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        model.Name = dto.Name;
        model.Description = dto.Description ?? string.Empty;
        model.IsActive = dto.IsActive;
        model.UpdatedAt = DateTime.Now;
    }

    /// <summary>
    /// 批量转换 DTO
    /// </summary>
    public List<SkillDto> ToDtoList(IEnumerable<Skill> models)
    {
        if (models == null)
            return new List<SkillDto>();

        return models.Select(ToDto).ToList();
    }
}
