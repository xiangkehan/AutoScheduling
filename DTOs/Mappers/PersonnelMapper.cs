using AutoScheduling3.DTOs;
using AutoScheduling3.Models;
using AutoScheduling3.Data.Interfaces; // 修正命名空间引用
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.DTOs.Mappers;

/// <summary>
/// 人员数据映射器
/// </summary>
public class PersonnelMapper
{
    private readonly IPositionRepository _positionRepo;
    private readonly ISkillRepository _skillRepo;

    public PersonnelMapper(IPositionRepository positionRepo, ISkillRepository skillRepo)
    {
        _positionRepo = positionRepo ?? throw new ArgumentNullException(nameof(positionRepo));
        _skillRepo = skillRepo ?? throw new ArgumentNullException(nameof(skillRepo));
    }

    /// <summary>
    /// Model转DTO
    /// </summary>
    public async Task<PersonnelDto> ToDtoAsync(Personal model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        var dto = new PersonnelDto
        {
            Id = model.Id,
            Name = model.Name,
            PositionId = model.PositionId,
            SkillIds = model.SkillIds?.ToList() ?? new List<int>(),
            IsAvailable = model.IsAvailable,
            IsRetired = model.IsRetired,
            RecentShiftIntervalCount = model.RecentShiftIntervalCount,
            RecentHolidayShiftIntervalCount = model.RecentHolidayShiftIntervalCount,
            RecentPeriodShiftIntervals = model.RecentPeriodShiftIntervals?.ToArray() ?? new int[12]
        };

        // 加载职位名称
        var position = await _positionRepo.GetByIdAsync(model.PositionId);
        dto.PositionName = position?.Name ?? "未知职位";

        // 加载技能名称
        if (model.SkillIds != null && model.SkillIds.Count > 0)
        {
            var allSkills = await _skillRepo.GetAllAsync();
            dto.SkillNames = allSkills
                .Where(s => model.SkillIds.Contains(s.Id))
                .Select(s => s.Name)
                .ToList();
        }

        return dto;
    }

    // Added sync wrapper for LINQ Select usage scenarios
    /// <summary>
    /// Model转DTO（同步）
    /// </summary>
    public PersonnelDto ToDto(Personal model) => ToDtoAsync(model).GetAwaiter().GetResult();

    /// <summary>
    /// DTO转Model（创建）
    /// </summary>
    public Personal ToModel(CreatePersonnelDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new Personal
        {
            Name = dto.Name,
            PositionId = dto.PositionId,
            SkillIds = dto.SkillIds ?? new List<int>(),
            IsAvailable = dto.IsAvailable,
            IsRetired = false,
            RecentShiftIntervalCount = dto.RecentShiftIntervalCount,
            RecentHolidayShiftIntervalCount = dto.RecentHolidayShiftIntervalCount,
            RecentPeriodShiftIntervals = dto.RecentPeriodShiftIntervals ?? new int[12]
        };
    }

    /// <summary>
    /// 更新Model（从UpdateDTO）
    /// </summary>
    public void UpdateModel(Personal model, UpdatePersonnelDto dto)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        model.Name = dto.Name;
        model.PositionId = dto.PositionId;
        model.SkillIds = dto.SkillIds;
        model.IsAvailable = dto.IsAvailable;
        model.IsRetired = dto.IsRetired;
        model.RecentShiftIntervalCount = dto.RecentShiftIntervalCount;
        model.RecentHolidayShiftIntervalCount = dto.RecentHolidayShiftIntervalCount;
        model.RecentPeriodShiftIntervals = dto.RecentPeriodShiftIntervals ?? new int[12];
    }

    /// <summary>
    /// 批量转换
    /// </summary>
    public async Task<List<PersonnelDto>> ToDtoListAsync(List<Personal> models)
    {
        var dtos = new List<PersonnelDto>();
        foreach (var model in models)
        {
            dtos.Add(await ToDtoAsync(model));
        }
        return dtos;
    }
}
