using AutoScheduling3.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Data.Interfaces;

/// <summary>
/// 人员仓储接口
/// </summary>
public interface IPersonalRepository : IRepository<Personal>
{
    /// <summary>
    /// 按名称搜索人员
    /// </summary>
    Task<List<Personal>> SearchByNameAsync(string keyword);

    /// <summary>
    /// 更新人员间隔计数
    /// </summary>
    Task UpdateIntervalCountsAsync(int personnelId, int recentShiftInterval, int recentHolidayInterval, int[] periodIntervals);

    /// <summary>
    /// 根据ID列表获取人员
    /// </summary>
    Task<List<Personal>> GetPersonnelByIdsAsync(IEnumerable<int> ids);
}
