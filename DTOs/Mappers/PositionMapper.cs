using AutoScheduling3.DTOs;
using AutoScheduling3.Models;
using AutoScheduling3.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.DTOs.Mappers;

/// <summary>
/// 哨位数据映射器 - Model 与 DTO 互转
/// 需求: 1.1, 2.2
/// </summary>
public class PositionMapper
{
    private readonly ISkillRepository _skillRepository;
    private readonly IPersonalRepository _personalRepository;

    public PositionMapper(ISkillRepository skillRepository, IPersonalRepository personalRepository)
    {
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _personalRepository = personalRepository ?? throw new ArgumentNullException(nameof(personalRepository));
    }

    /// <summary>
    /// Model 转 DTO（同步版本，不加载关联名称）
    /// </summary>
    public PositionDto ToDto(PositionLocation model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new PositionDto
        {
            Id = model.Id,
            Name = model.Name,
            Location = model.Location,
            Description = model.Description,
            Requirements = model.Requirements,
            RequiredSkillIds = new List<int>(model.RequiredSkillIds),
            RequiredSkillNames = new List<string>(), // 需要异步加载
            AvailablePersonnelIds = new List<int>(model.AvailablePersonnelIds),
            AvailablePersonnelNames = new List<string>() // 需要异步加载
        };
    }

    /// <summary>
    /// Model 转 DTO（异步版本，加载关联名称）
    /// </summary>
    public async Task<PositionDto> ToDtoAsync(PositionLocation model)
    {
        var dto = ToDto(model);

        // 加载技能名称
        if (model.RequiredSkillIds != null && model.RequiredSkillIds.Count > 0)
        {
            var skills = await _skillRepository.GetByIdsAsync(model.RequiredSkillIds);
            dto.RequiredSkillNames = skills.Select(s => s.Name).ToList();
        }

        // 加载可用人员名称
        if (model.AvailablePersonnelIds != null && model.AvailablePersonnelIds.Count > 0)
        {
            var personnel = await _personalRepository.GetByIdsAsync(model.AvailablePersonnelIds);
            dto.AvailablePersonnelNames = personnel.Select(p => p.Name).ToList();
        }

        return dto;
    }

    /// <summary>
    /// 批量转换 PositionDto（异步版本，加载关联名称）
    /// </summary>
    public async Task<List<PositionDto>> ToDtoListAsync(IEnumerable<PositionLocation> models)
    {
        if (models == null)
            return new List<PositionDto>();

        var tasks = models.Select(m => ToDtoAsync(m));
        return (await Task.WhenAll(tasks)).ToList();
    }

    /// <summary>
    /// 批量转换 PositionDto（同步版本）
    /// </summary>
    public List<PositionDto> ToDtoList(IEnumerable<PositionLocation> models)
    {
        if (models == null)
            return new List<PositionDto>();

        return models.Select(ToDto).ToList();
    }

    /// <summary>
    /// CreatePositionDto 转 PositionLocation Model
    /// </summary>
    public PositionLocation ToModel(CreatePositionDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new PositionLocation
        {
            Name = dto.Name,
            Location = dto.Location,
            Description = dto.Description ?? string.Empty,
            Requirements = dto.Requirements ?? string.Empty,
            RequiredSkillIds = new List<int>(dto.RequiredSkillIds),
            AvailablePersonnelIds = new List<int>(dto.AvailablePersonnelIds),
            IsActive = true, // 新创建的哨位默认激活
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// PositionDto 转 PositionLocation Model（用于更新现有模型）
    /// </summary>
    public PositionLocation ToModel(PositionDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new PositionLocation
        {
            Id = dto.Id,
            Name = dto.Name,
            Location = dto.Location,
            Description = dto.Description ?? string.Empty,
            Requirements = dto.Requirements ?? string.Empty,
            RequiredSkillIds = new List<int>(dto.RequiredSkillIds),
            AvailablePersonnelIds = new List<int>(dto.AvailablePersonnelIds),
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// UpdatePositionDto 转 PositionLocation Model（更新现有模型）
    /// </summary>
    public void UpdateModel(PositionLocation model, UpdatePositionDto dto)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        model.Name = dto.Name;
        model.Location = dto.Location;
        model.Description = dto.Description ?? string.Empty;
        model.Requirements = dto.Requirements ?? string.Empty;
        model.RequiredSkillIds = new List<int>(dto.RequiredSkillIds);
        model.AvailablePersonnelIds = new List<int>(dto.AvailablePersonnelIds);
        model.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// PositionDto 转 UpdatePositionDto
    /// </summary>
    public UpdatePositionDto ToUpdateDto(PositionDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new UpdatePositionDto
        {
            Name = dto.Name,
            Location = dto.Location,
            Description = dto.Description,
            Requirements = dto.Requirements,
            RequiredSkillIds = new List<int>(dto.RequiredSkillIds),
            AvailablePersonnelIds = new List<int>(dto.AvailablePersonnelIds)
        };
    }

    /// <summary>
    /// PositionDto 转 CreatePositionDto
    /// </summary>
    public CreatePositionDto ToCreateDto(PositionDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new CreatePositionDto
        {
            Name = dto.Name,
            Location = dto.Location,
            Description = dto.Description,
            Requirements = dto.Requirements,
            RequiredSkillIds = new List<int>(dto.RequiredSkillIds),
            AvailablePersonnelIds = new List<int>(dto.AvailablePersonnelIds)
        };
    }
}