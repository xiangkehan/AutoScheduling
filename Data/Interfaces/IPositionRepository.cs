using AutoScheduling3.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Data.Interfaces;

/// <summary>
/// 哨位/职位仓储接口
/// </summary>
public interface IPositionRepository : IRepository<PositionLocation>
{
    /// <summary>
    /// 按名称搜索哨位
    /// </summary>
    Task<List<PositionLocation>> SearchByNameAsync(string keyword);

    /// <summary>
    /// 根据ID列表获取哨位
    /// </summary>
    Task<List<PositionLocation>> GetPositionsByIdsAsync(IEnumerable<int> ids);

    /// <summary>
    /// 为哨位添加可用人员
    /// </summary>
    Task AddAvailablePersonnelAsync(int positionId, int personnelId);

    /// <summary>
    /// 从哨位移除可用人员
    /// </summary>
    Task RemoveAvailablePersonnelAsync(int positionId, int personnelId);

    /// <summary>
    /// 更新哨位的可用人员列表
    /// </summary>
    Task UpdateAvailablePersonnelAsync(int positionId, List<int> personnelIds);

    /// <summary>
    /// 获取哨位的可用人员ID列表
    /// </summary>
    Task<List<int>> GetAvailablePersonnelIdsAsync(int positionId);

    /// <summary>
    /// 根据人员ID获取其可用的哨位列表
    /// </summary>
    Task<List<PositionLocation>> GetPositionsByPersonnelAsync(int personnelId);
}
