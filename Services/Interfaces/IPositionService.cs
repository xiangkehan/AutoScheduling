using AutoScheduling3.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 哨位服务接口
/// </summary>
public interface IPositionService
{
    /// <summary>
    /// 获取所有哨位
    /// </summary>
    Task<List<PositionDto>> GetAllAsync();

    /// <summary>
    /// 根据ID获取哨位
    /// </summary>
    Task<PositionDto?> GetByIdAsync(int id);

    /// <summary>
    /// 创建哨位
    /// </summary>
    Task<PositionDto> CreateAsync(CreatePositionDto dto);

    /// <summary>
    /// 更新哨位
    /// </summary>
    Task UpdateAsync(int id, UpdatePositionDto dto);

    /// <summary>
    /// 删除哨位
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 搜索哨位
    /// </summary>
    Task<List<PositionDto>> SearchAsync(string keyword);

    /// <summary>
    /// 验证哨位数据完整性
    /// </summary>
    Task<bool> ValidatePositionDataAsync(PositionDto position);

    /// <summary>
    /// 添加可用人员到哨位
    /// </summary>
    Task AddAvailablePersonnelAsync(int positionId, int personnelId);

    /// <summary>
    /// 从哨位移除可用人员
    /// </summary>
    Task RemoveAvailablePersonnelAsync(int positionId, int personnelId);

    /// <summary>
    /// 获取哨位的可用人员列表
    /// </summary>
    Task<List<PersonnelDto>> GetAvailablePersonnelAsync(int positionId);

    /// <summary>
    /// 验证人员技能是否满足哨位要求
    /// </summary>
    Task<bool> ValidatePersonnelSkillsAsync(int personnelId, int positionId);
}
