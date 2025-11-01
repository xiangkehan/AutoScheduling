using AutoScheduling3.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Data.Interfaces;

/// <summary>
/// 技能仓储接口
/// </summary>
public interface ISkillRepository : IRepository<Skill>
{
    /// <summary>
    /// 批量获取技能
    /// </summary>
    Task<List<Skill>> GetByIdsAsync(List<int> ids); // 新增批量方法以供 Mapper 使用

    /// <summary>
    /// 按名称搜索技能
    /// </summary>
    Task<List<Skill>> SearchByNameAsync(string keyword);

    /// <summary>
    /// 检查技能名称是否已存在
    /// </summary>
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
}
