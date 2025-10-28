using AutoScheduling3.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 技能服务接口
/// </summary>
public interface ISkillService
{
    /// <summary>
    /// 获取所有技能
    /// </summary>
    Task<List<SkillDto>> GetAllAsync();

    /// <summary>
    /// 根据ID获取技能
    /// </summary>
    Task<SkillDto?> GetByIdAsync(int id);

    /// <summary>
    /// 创建技能
    /// </summary>
    Task<SkillDto> CreateAsync(CreateSkillDto dto);

    /// <summary>
    /// 更新技能
    /// </summary>
    Task UpdateAsync(int id, UpdateSkillDto dto);

    /// <summary>
    /// 删除技能
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 搜索技能
    /// </summary>
    Task<List<SkillDto>> SearchAsync(string keyword);

    /// <summary>
    /// 根据ID列表批量获取技能
    /// </summary>
    Task<List<SkillDto>> GetByIdsAsync(List<int> ids);
}
