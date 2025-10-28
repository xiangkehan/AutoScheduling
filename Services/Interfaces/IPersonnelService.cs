using AutoScheduling3.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 人员服务接口
/// </summary>
public interface IPersonnelService
{
    /// <summary>
    /// 获取所有人员
    /// </summary>
    Task<List<PersonnelDto>> GetAllAsync();

    /// <summary>
    /// 根据ID获取人员
    /// </summary>
    Task<PersonnelDto?> GetByIdAsync(int id);

    /// <summary>
    /// 创建人员
    /// </summary>
    Task<PersonnelDto> CreateAsync(CreatePersonnelDto dto);

    /// <summary>
    /// 更新人员
    /// </summary>
    Task UpdateAsync(int id, UpdatePersonnelDto dto);

    /// <summary>
    /// 删除人员
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 搜索人员
    /// </summary>
    Task<List<PersonnelDto>> SearchAsync(string keyword);
}
