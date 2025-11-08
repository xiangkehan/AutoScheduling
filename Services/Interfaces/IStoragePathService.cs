using AutoScheduling3.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Services.Interfaces
{
    /// <summary>
    /// 存储路径服务接口 - 管理应用程序存储文件信息
    /// 需求: 1.1
    /// </summary>
    public interface IStoragePathService : IApplicationService
    {
        /// <summary>
        /// 获取所有存储文件信息
        /// </summary>
        /// <returns>存储文件信息集合</returns>
        Task<IEnumerable<StorageFileInfo>> GetStorageFilesAsync();

        /// <summary>
        /// 刷新存储文件信息
        /// </summary>
        Task RefreshStorageInfoAsync();
    }
}
