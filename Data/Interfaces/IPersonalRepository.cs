using AutoScheduling3.Models;

namespace AutoScheduling3.Data.Interfaces;

/// <summary>
/// 人员仓储接口
/// </summary>
public interface IPersonalRepository
{
    /// <summary>
    /// 获取所有人员
    /// </summary>
    Task<List<Personal>> GetAllAsync();

    /// <summary>
    /// 根据ID获取人员
    /// </summary>
    Task<Personal?> GetByIdAsync(int id);

    /// <summary>
    /// 创建人员
    /// </summary>
    Task<int> CreateAsync(Personal personal);

    /// <summary>
    /// 更新人员
    /// </summary>
    Task UpdateAsync(Personal personal);

    /// <summary>
    /// 删除人员
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 按名称搜索人员
    /// </summary>
    Task<List<Personal>> SearchByNameAsync(string keyword);

    /// <summary>
    /// 更新人员间隔计数
    /// </summary>
    Task UpdateIntervalCountsAsync(int personnelId, int recentShiftInterval, int recentHolidayInterval, int[] periodIntervals);

    /// <summary>
    /// 检查人员是否存在
    /// </summary>
    Task<bool> ExistsAsync(int id);
}
