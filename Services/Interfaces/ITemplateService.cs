using AutoScheduling3.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 排班模板服务接口
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// 获取所有模板
    /// </summary>
    Task<List<SchedulingTemplateDto>> GetAllAsync();

    /// <summary>
    /// 根据ID获取模板
    /// </summary>
    Task<SchedulingTemplateDto?> GetByIdAsync(int id);

    /// <summary>
    /// 创建模板
    /// </summary>
    Task<SchedulingTemplateDto> CreateAsync(CreateTemplateDto dto);

    /// <summary>
    /// 更新模板
    /// </summary>
    Task UpdateAsync(int id, UpdateTemplateDto dto);

    /// <summary>
    /// 删除模板
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 获取默认模板
    /// </summary>
    Task<SchedulingTemplateDto?> GetDefaultAsync();

    /// <summary>
    /// 根据类型获取模板
    /// </summary>
    Task<List<SchedulingTemplateDto>> GetByTypeAsync(string templateType);

    /// <summary>
    /// 验证模板配置
    /// </summary>
    Task<TemplateValidationResult> ValidateAsync(int templateId);

    /// <summary>
    /// 使用模板创建排班
    /// </summary>
    Task<ScheduleDto> UseTemplateAsync(UseTemplateDto dto);
}
