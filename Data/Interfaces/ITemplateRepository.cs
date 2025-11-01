using AutoScheduling3.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Data.Interfaces;

/// <summary>
/// 排班模板仓储接口
/// </summary>
public interface ITemplateRepository : IRepository<SchedulingTemplate>
{
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

    /// <summary>
    /// 检查名称是否已存在（排除指定ID用于更新）
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);

    /// <summary>
    /// 清除同类型的默认模板标记
    /// </summary>
    Task ClearDefaultForTypeAsync(string templateType);
}
