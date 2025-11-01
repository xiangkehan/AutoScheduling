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
}
