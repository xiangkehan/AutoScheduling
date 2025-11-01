using AutoScheduling3.DTOs;
using AutoScheduling3.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.DTOs.Mappers;

/// <summary>
/// 技能数据映射器 - Model 与 DTO 互转
/// 需求: 2.2
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
            Description = model.Description,
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }

    /// <summary>
    /// 批量转换 SkillDto
    /// </summary>
    public List<SkillDto> ToDtoList(IEnumerable<Skill> models)
    {
        if (models == null)
            return new List<SkillDto>();

        return models.Select(ToDto).ToList();
    }

    /// <summary>
    /// CreateSkillDto 转 Skill Model
    /// </summary>
    public Skill ToModel(CreateSkillDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new Skill
        {
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            IsActive = true, // 新创建的技能默认激活
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// SkillDto 转 Skill Model（完整转换）
    /// </summary>
    public Skill ToModel(SkillDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new Skill
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
    /// UpdateSkillDto 转 Skill Model（更新现有模型）
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
        model.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// SkillDto 转 UpdateSkillDto
    /// </summary>
    public UpdateSkillDto ToUpdateDto(SkillDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new UpdateSkillDto
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive
        };
    }

    /// <summary>
    /// SkillDto 转 CreateSkillDto
    /// </summary>
    public CreateSkillDto ToCreateDto(SkillDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new CreateSkillDto
        {
            Name = dto.Name,
            Description = dto.Description
        };
    }
}