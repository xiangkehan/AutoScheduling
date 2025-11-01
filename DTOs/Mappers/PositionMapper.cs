using AutoScheduling3.DTOs;
using AutoScheduling3.Models;
using AutoScheduling3.Data.Interfaces;
using System; // 添加以支持 ArgumentNullException
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; // 添加以支持 LINQ 扩展

namespace AutoScheduling3.DTOs.Mappers;

/// <summary>
/// 哨位数据映射器 - Model 与 DTO互转
/// </summary>
public class PositionMapper
{
    private readonly ISkillRepository _skillRepository;

    public PositionMapper(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
    }

    /// <summary>
    /// Model 转 DTO（同步版本，不加载技能名称）
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
            RequiredSkillNames = new List<string>() // 空列表，需要异步加载
        };
    }

    /// <summary>
    /// Model 转 DTO（异步版本，加载技能名称）
    /// </summary>
    public async Task<PositionDto> ToDtoAsync(PositionLocation model)
    {
        var dto = ToDto(model);

        // 加载技能名称
        if (model.RequiredSkillIds.Count > 0)
        {
            // 使用接口方法批量获取技能
            var skills = await _skillRepository.GetByIdsAsync(model.RequiredSkillIds);
            dto.RequiredSkillNames = skills.Select(s => s.Name).ToList();
        }

        return dto;
    }

    /// <summary>
    /// 创建 DTO 转 Model
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
            RequiredSkillIds = new List<int>(dto.RequiredSkillIds)
        };
    }

    /// <summary>
    /// DTO 转 Model（完整PositionDto）
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
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 更新 DTO 到现有 Model
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
    }

    /// <summary>
    /// 批量转换 DTO（同步版本）
    /// </summary>
    public List<PositionDto> ToDtoList(IEnumerable<PositionLocation> models)
    {
        if (models == null)
            return new List<PositionDto>();

        return models.Select(ToDto).ToList();
    }

    /// <summary>
    /// 批量转换 DTO（异步版本，加载技能名称）
    /// </summary>
    public async Task<List<PositionDto>> ToDtoListAsync(IEnumerable<PositionLocation> models)
    {
        if (models == null)
            return new List<PositionDto>();

        var tasks = models.Select(m => ToDtoAsync(m));
        return (await Task.WhenAll(tasks)).ToList();
    }

    /// <summary>
    /// 批量转换 Model（从DTO列表）
    /// </summary>
    public List<PositionLocation> ToModelList(IEnumerable<PositionDto> dtos)
    {
        if (dtos == null)
            return new List<PositionLocation>();

        return dtos.Select(ToModel).ToList();
    }
}
