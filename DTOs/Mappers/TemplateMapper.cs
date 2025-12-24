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

        var dto = new SchedulingTemplateDto
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
            StrategyConfig = model.StrategyConfig,
            CreatedAt = model.CreatedAt,
            LastUsedAt = model.LastUsedAt,
            UsageCount = model.UsageCount
        };

        // 从 StrategyConfig 解析算法配置
        if (!string.IsNullOrWhiteSpace(model.StrategyConfig))
        {
            var algorithmConfig = Helpers.OptimizedConfigSerializer.Deserialize<TemplateAlgorithmConfig>(model.StrategyConfig);
            if (algorithmConfig != null)
            {
                dto.SchedulingMode = algorithmConfig.SchedulingMode;
                dto.GeneticAlgorithmConfig = algorithmConfig.GeneticConfig;
            }
        }

        return dto;
    }

    /// <summary>
    /// 创建 DTO 转 Model
    /// </summary>
    public SchedulingTemplate ToModel(CreateTemplateDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // 构建算法配置JSON
        string strategyConfig = string.Empty;
        if (dto.SchedulingMode.HasValue)
        {
            var algorithmConfig = new TemplateAlgorithmConfig
            {
                SchedulingMode = dto.SchedulingMode.Value,
                GeneticConfig = dto.GeneticAlgorithmConfig
            };
            strategyConfig = Helpers.OptimizedConfigSerializer.Serialize(algorithmConfig);
        }

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
            StrategyConfig = strategyConfig,
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

        // 更新算法配置
        if (dto.SchedulingMode.HasValue)
        {
            var algorithmConfig = new TemplateAlgorithmConfig
            {
                SchedulingMode = dto.SchedulingMode.Value,
                GeneticConfig = dto.GeneticAlgorithmConfig
            };
            model.StrategyConfig = Helpers.OptimizedConfigSerializer.Serialize(algorithmConfig);
        }
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
