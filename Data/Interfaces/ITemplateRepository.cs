using AutoScheduling3.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Data.Interfaces;

/// <summary>
/// 排班模板仓储接口
/// </summary>
public interface ITemplateRepository
{
    /// <summary>
    /// 获取所有模板
    /// </summary>
    Task<List<SchedulingTemplate>> GetAllAsync();

    /// <summary>
    /// 根据ID获取模板
    /// </summary>
    Task<SchedulingTemplate?> GetByIdAsync(int id);

    /// <summary>
    /// 创建模板
    /// </summary>
    Task<int> CreateAsync(SchedulingTemplate template);

    /// <summary>
    /// 更新模板
    /// </summary>
    Task UpdateAsync(SchedulingTemplate template);

    /// <summary>
    /// 删除模板
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 检查模板是否存在
    /// </summary>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// 获取默认模板
    /// </summary>
    Task<SchedulingTemplate?> GetDefaultAsync();

    /// <summary>
    /// 根据类型获取模板列表
    /// </summary>
    Task<List<SchedulingTemplate>> GetByTypeAsync(string templateType);

    /// <summary>
    /// 更新模板使用记录
    /// </summary>
    Task UpdateUsageAsync(int id);
}
