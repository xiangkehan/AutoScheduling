using System.Threading.Tasks;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 排班创建草稿服务接口 - 管理排班创建进度的保存和恢复
/// </summary>
public interface ISchedulingDraftService : IApplicationService
{
    /// <summary>
    /// 保存排班创建草稿
    /// </summary>
    /// <param name="draft">草稿数据</param>
    Task SaveDraftAsync(SchedulingDraftDto draft);

    /// <summary>
    /// 加载最近的排班创建草稿
    /// </summary>
    /// <returns>草稿数据，如果不存在则返回null</returns>
    Task<SchedulingDraftDto?> LoadDraftAsync();

    /// <summary>
    /// 删除排班创建草稿
    /// </summary>
    Task DeleteDraftAsync();

    /// <summary>
    /// 检查是否存在草稿
    /// </summary>
    /// <returns>是否存在草稿</returns>
    Task<bool> HasDraftAsync();

    /// <summary>
    /// 清理过期草稿（超过7天）
    /// </summary>
    Task CleanupExpiredDraftsAsync();
}
