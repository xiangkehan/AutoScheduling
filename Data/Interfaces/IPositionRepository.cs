using AutoScheduling3.Models;

namespace AutoScheduling3.Data.Interfaces;

/// <summary>
/// 哨位/职位仓储接口
/// </summary>
public interface IPositionRepository
{
    /// <summary>
    /// 获取所有哨位
    /// </summary>
    Task<List<PositionLocation>> GetAllAsync();

    /// <summary>
    /// 根据ID获取哨位
    /// </summary>
    Task<PositionLocation?> GetByIdAsync(int id);

    /// <summary>
    /// 创建哨位
    /// </summary>
    Task<int> CreateAsync(PositionLocation position);

    /// <summary>
    /// 更新哨位
    /// </summary>
    Task UpdateAsync(PositionLocation position);

    /// <summary>
    /// 删除哨位
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 检查哨位是否存在
    /// </summary>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// 按名称搜索哨位
    /// </summary>
    Task<List<PositionLocation>> SearchByNameAsync(string keyword);
}
