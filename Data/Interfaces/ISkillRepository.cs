using AutoScheduling3.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Data.Interfaces;

/// <summary>
/// 技能仓储接口
/// </summary>
public interface ISkillRepository
{
    /// <summary>
    /// 获取所有技能
    /// </summary>
    Task<List<Skill>> GetAllAsync();

    /// <summary>
    /// 根据ID获取技能
    /// </summary>
    Task<Skill?> GetByIdAsync(int id);

    /// <summary>
    /// 创建技能
    /// </summary>
    Task<int> CreateAsync(Skill skill);

    /// <summary>
    /// 更新技能
    /// </summary>
    Task UpdateAsync(Skill skill);

    /// <summary>
    /// 删除技能
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 检查技能是否存在
    /// </summary>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// 按名称搜索技能
    /// </summary>
    Task<List<Skill>> SearchByNameAsync(string keyword);

    /// <summary>
    /// 检查技能名称是否已存在
    /// </summary>
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
}
