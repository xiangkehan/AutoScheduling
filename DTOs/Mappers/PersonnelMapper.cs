using AutoScheduling3.DTOs;
using AutoScheduling3.Models;
using AutoScheduling3.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.DTOs.Mappers;

/// <summary>
/// 人员数据映射器 - Model 与 DTO 互转
/// 需求: 2.1, 2.2
/// </summary>
public class PersonnelMapper
{
    private readonly ISkillRepository _skillRepository;
    private readonly IPositionRepository _positionRepository;

    public PersonnelMapper(ISkillRepository skillRepository, IPositionRepository positionRepository)
    {
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
    }

    /// <summary>
    /// Model 转 DTO（同步版本，不加载关联名称）
    /// </summary>
    public PersonnelDto ToDto(Personal model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new PersonnelDto
        {
            Id = model.Id,
            Name = model.Name,
            PositionId = model.PositionId,
            PositionName = string.Empty, // 需要异步加载
            SkillIds = new List<int>(model.SkillIds),
            SkillNames = new List<string>(), // 需要异步加载
            IsAvailable = model.IsAvailable,
            IsRetired = model.IsRetired,
            RecentShiftIntervalCount = model.RecentShiftIntervalCount,
            RecentHolidayShiftIntervalCount = model.RecentHolidayShiftIntervalCount,
            RecentPeriodShiftIntervals = (int[])model.RecentPeriodShiftIntervals.Clone()
        };
    }

    /// <summary>
    /// Model 转 DTO（异步版本，加载关联名称）
    /// </summary>
    public async Task<PersonnelDto> ToDtoAsync(Personal model)
    {
        var dto = ToDto(model);

        // 加载职位名称
        if (model.PositionId > 0)
        {
            var position = await _positionRepository.GetByIdAsync(model.PositionId);
            dto.PositionName = position?.Name ?? "未知职位";
        }

        // 加载技能名称
        if (model.SkillIds != null && model.SkillIds.Count > 0)
        {
            var skills = await _skillRepository.GetByIdsAsync(model.SkillIds);
            dto.SkillNames = skills.Select(s => s.Name).ToList();
        }

        return dto;
    }

    /// <summary>
    /// 批量转换 PersonnelDto（异步版本，加载关联名称）
    /// </summary>
    public async Task<List<PersonnelDto>> ToDtoListAsync(IEnumerable<Personal> models)
    {
        if (models == null)
            return new List<PersonnelDto>();

        var tasks = models.Select(m => ToDtoAsync(m));
        return (await Task.WhenAll(tasks)).ToList();
    }

    /// <summary>
    /// 批量转换 PersonnelDto（同步版本）
    /// </summary>
    public List<PersonnelDto> ToDtoList(IEnumerable<Personal> models)
    {
        if (models == null)
            return new List<PersonnelDto>();

        return models.Select(ToDto).ToList();
    }

    /// <summary>
    /// CreatePersonnelDto 转 Personal Model
    /// </summary>
    public Personal ToModel(CreatePersonnelDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new Personal
        {
            Name = dto.Name,
            PositionId = dto.PositionId,
            SkillIds = new List<int>(dto.SkillIds),
            IsAvailable = dto.IsAvailable,
            IsRetired = false, // 新创建的人员默认未退役
            RecentShiftIntervalCount = dto.RecentShiftIntervalCount,
            RecentHolidayShiftIntervalCount = dto.RecentHolidayShiftIntervalCount,
            RecentPeriodShiftIntervals = (int[])dto.RecentPeriodShiftIntervals.Clone(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// PersonnelDto 转 Personal Model（用于更新现有模型）
    /// </summary>
    public Personal ToModel(PersonnelDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new Personal
        {
            Id = dto.Id,
            Name = dto.Name,
            PositionId = dto.PositionId,
            SkillIds = new List<int>(dto.SkillIds),
            IsAvailable = dto.IsAvailable,
            IsRetired = dto.IsRetired,
            RecentShiftIntervalCount = dto.RecentShiftIntervalCount,
            RecentHolidayShiftIntervalCount = dto.RecentHolidayShiftIntervalCount,
            RecentPeriodShiftIntervals = (int[])dto.RecentPeriodShiftIntervals.Clone(),
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// UpdatePersonnelDto 转 Personal Model（更新现有模型）
    /// </summary>
    public void UpdateModel(Personal model, UpdatePersonnelDto dto)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        model.Name = dto.Name;
        model.PositionId = dto.PositionId;
        model.SkillIds = new List<int>(dto.SkillIds);
        model.IsAvailable = dto.IsAvailable;
        model.IsRetired = dto.IsRetired;
        model.RecentShiftIntervalCount = dto.RecentShiftIntervalCount;
        model.RecentHolidayShiftIntervalCount = dto.RecentHolidayShiftIntervalCount;
        model.RecentPeriodShiftIntervals = (int[])dto.RecentPeriodShiftIntervals.Clone();
        model.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// PersonnelDto 转 UpdatePersonnelDto
    /// </summary>
    public UpdatePersonnelDto ToUpdateDto(PersonnelDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new UpdatePersonnelDto
        {
            Name = dto.Name,
            PositionId = dto.PositionId,
            SkillIds = new List<int>(dto.SkillIds),
            IsAvailable = dto.IsAvailable,
            IsRetired = dto.IsRetired,
            RecentShiftIntervalCount = dto.RecentShiftIntervalCount,
            RecentHolidayShiftIntervalCount = dto.RecentHolidayShiftIntervalCount,
            RecentPeriodShiftIntervals = (int[])dto.RecentPeriodShiftIntervals.Clone()
        };
    }

    /// <summary>
    /// PersonnelDto 转 CreatePersonnelDto
    /// </summary>
    public CreatePersonnelDto ToCreateDto(PersonnelDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new CreatePersonnelDto
        {
            Name = dto.Name,
            PositionId = dto.PositionId,
            SkillIds = new List<int>(dto.SkillIds),
            IsAvailable = dto.IsAvailable,
            RecentShiftIntervalCount = dto.RecentShiftIntervalCount,
            RecentHolidayShiftIntervalCount = dto.RecentHolidayShiftIntervalCount,
            RecentPeriodShiftIntervals = (int[])dto.RecentPeriodShiftIntervals.Clone()
        };
    }
}