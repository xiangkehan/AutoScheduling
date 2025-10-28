using AutoScheduling3.DTOs;
using AutoScheduling3.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.DTOs.Mappers;

/// <summary>
/// 排班模板映射器 - Model 与 DTO 互转
/// </summary>
public class TemplateMapper
{
    /// <summary>
    /// Model 转 DTO
    /// </summary>
    public SchedulingTemplateDto ToDto(SchedulingTemplate model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new SchedulingTemplateDto
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            TemplateType = model.TemplateType,
            IsDefault = model.IsDefault,
            PersonnelIds = new List<int>(model.PersonnelIds),
            PositionIds = new List<int>(model.PositionIds),
            HolidayConfigId = model.HolidayConfigId,
            UseActiveHolidayConfig = model.UseActiveHolidayConfig,
            EnabledFixedRuleIds = new List<int>(model.EnabledFixedRuleIds),
            EnabledManualAssignmentIds = new List<int>(model.EnabledManualAssignmentIds),
            CreatedAt = model.CreatedAt,
            LastUsedAt = model.LastUsedAt,
            UsageCount = model.UsageCount
        };
    }

    /// <summary>
    /// 创建 DTO 转 Model
    /// </summary>
    public SchedulingTemplate ToModel(CreateTemplateDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new SchedulingTemplate
        {
            Name = dto.Name,
            Description = dto.Description,
            TemplateType = dto.TemplateType,
            IsDefault = dto.IsDefault,
            PersonnelIds = new List<int>(dto.PersonnelIds),
            PositionIds = new List<int>(dto.PositionIds),
            HolidayConfigId = dto.HolidayConfigId,
            UseActiveHolidayConfig = dto.UseActiveHolidayConfig,
            EnabledFixedRuleIds = new List<int>(dto.EnabledFixedRuleIds),
            EnabledManualAssignmentIds = new List<int>(dto.EnabledManualAssignmentIds),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            LastUsedAt = null,
            UsageCount = 0
        };
    }

    /// <summary>
    /// 更新 DTO 到现有 Model
    /// </summary>
    public void UpdateModel(SchedulingTemplate model, UpdateTemplateDto dto)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        model.Name = dto.Name;
        model.Description = dto.Description;
        model.TemplateType = dto.TemplateType;
        model.IsDefault = dto.IsDefault;
        model.PersonnelIds = new List<int>(dto.PersonnelIds);
        model.PositionIds = new List<int>(dto.PositionIds);
        model.HolidayConfigId = dto.HolidayConfigId;
        model.UseActiveHolidayConfig = dto.UseActiveHolidayConfig;
        model.EnabledFixedRuleIds = new List<int>(dto.EnabledFixedRuleIds);
        model.EnabledManualAssignmentIds = new List<int>(dto.EnabledManualAssignmentIds);
        model.UpdatedAt = DateTime.Now;
    }

    /// <summary>
    /// 批量转换 DTO
    /// </summary>
    public List<SchedulingTemplateDto> ToDtoList(IEnumerable<SchedulingTemplate> models)
    {
        if (models == null)
            return new List<SchedulingTemplateDto>();

        return models.Select(ToDto).ToList();
    }
}
