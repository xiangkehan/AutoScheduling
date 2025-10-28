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
}
