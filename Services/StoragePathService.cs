using AutoScheduling3.Data;
using AutoScheduling3.Models;
using AutoScheduling3.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace AutoScheduling3.Services
{
    /// <summary>
    /// 存储路径服务实现 - 管理应用程序存储文件信息
    /// 
    /// 使用 ApplicationData.Current.LocalFolder 获取所有存储文件的路径信息。
    /// 
    /// 为什么使用 ApplicationData.Current.LocalFolder：
    /// 1. 统一路径管理：所有应用数据都存储在 LocalFolder 下
    /// 2. 路径一致性：确保报告的路径与实际存储位置一致
    /// 3. 诊断支持：便于用户和开发者定位数据文件
    /// 
    /// 功能：
    /// - 获取数据库文件信息（路径、大小、最后修改时间）
    /// - 获取配置文件信息
    /// - 验证所有路径都包含 ApplicationData.Current.LocalFolder
    /// - 提供文件存在性和可访问性检查
    /// 
    /// 需求: 1.1, 1.3, 3.1, 3.2, 3.3, 6.2, 6.3, 8.3
    /// </summary>
    public class StoragePathService : IStoragePathService
    {
        private List<StorageFileInfo> _cachedStorageFiles = new();
        private readonly object _lock = new();

        /// <summary>
        /// 初始化存储路径服务
        /// 
        /// 初始化流程：
        /// 1. 记录 ApplicationData.Current.LocalFolder 路径
        /// 2. 刷新存储文件信息
        /// 
        /// 日志记录：
        /// - 输出 LocalFolder 路径，便于诊断
        /// 
        /// 错误处理：
        /// - UnauthorizedAccessException：权限不足，抛出 InvalidOperationException
        /// - IOException：文件系统访问错误，抛出 InvalidOperationException
        /// 
        /// 需求: 5.1, 5.2, 5.3, 6.3, 8.1, 8.3
        /// </summary>
        /// <exception cref="InvalidOperationException">无法访问应用程序数据文件夹时抛出</exception>
        public async Task InitializeAsync()
        {
            try
            {
                // Log storage paths during initialization
                // Requirements: 6.3, 8.1
                var localFolder = ApplicationData.Current.LocalFolder.Path;
                System.Diagnostics.Debug.WriteLine($"[StoragePathService] Initialized - LocalFolder: {localFolder}");
                
                await RefreshStorageInfoAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                var errorMsg = "权限不足，无法访问应用程序数据文件夹";
                System.Diagnostics.Debug.WriteLine($"[StoragePathService] {errorMsg}: {ex.Message}");
                throw new InvalidOperationException(errorMsg, ex);
            }
            catch (IOException ex)
            {
                var errorMsg = "访问应用程序数据文件夹时发生IO错误";
                System.Diagnostics.Debug.WriteLine($"[StoragePathService] {errorMsg}: {ex.Message}");
                throw new InvalidOperationException(errorMsg, ex);
            }
        }

        public async Task CleanupAsync()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取所有存储文件信息
        /// </summary>
        public async Task<IEnumerable<StorageFileInfo>> GetStorageFilesAsync()
        {
            lock (_lock)
            {
                if (_cachedStorageFiles.Count > 0)
                {
                    return new List<StorageFileInfo>(_cachedStorageFiles);
                }
            }

            await RefreshStorageInfoAsync();

            lock (_lock)
            {
                return new List<StorageFileInfo>(_cachedStorageFiles);
            }
        }

        /// <summary>
        /// 刷新存储文件信息
        /// 
        /// 刷新流程：
        /// 1. 获取数据库文件路径和信息
        /// 2. 获取配置文件路径和信息
        /// 3. 验证所有路径都包含 ApplicationData.Current.LocalFolder
        /// 4. 更新缓存的文件信息列表
        /// 
        /// 路径验证：
        /// - 检查每个文件路径是否包含 LocalFolder
        /// - 如果路径不包含 LocalFolder，记录警告
        /// - 这有助于发现路径配置错误
        /// 
        /// 日志记录：
        /// - 输出数据库文件路径
        /// - 输出配置文件路径
        /// - 记录路径验证警告
        /// - 记录错误情况
        /// 
        /// 错误处理：
        /// - 捕获 UnauthorizedAccessException 和 IOException
        /// - 捕获所有异常，不中断流程
        /// - 即使部分文件信息获取失败，也返回可用的信息
        /// 
        /// 需求: 3.1, 3.2, 3.3, 5.1, 5.2, 6.2, 6.3, 8.3
        /// </summary>
        public async Task RefreshStorageInfoAsync()
        {
            var files = new List<StorageFileInfo>();

            try
            {
                // 获取数据库文件信息
                var dbPath = DatabaseConfiguration.GetDefaultDatabasePath();
                System.Diagnostics.Debug.WriteLine($"[StoragePathService] Database path: {dbPath}");
                files.Add(await CreateStorageFileInfoAsync(
                    "数据库文件",
                    "存储排班数据的SQLite数据库",
                    dbPath,
                    StorageFileType.Database));

                // 获取配置文件信息
                var configPath = GetConfigurationFilePath();
                System.Diagnostics.Debug.WriteLine($"[StoragePathService] Configuration path: {configPath}");
                files.Add(await CreateStorageFileInfoAsync(
                    "配置文件",
                    "应用程序设置和用户偏好",
                    configPath,
                    StorageFileType.Configuration));
                
                // Verify paths contain ApplicationData.Current.LocalFolder
                // Requirements: 6.3, 8.3
                var localFolder = ApplicationData.Current.LocalFolder.Path;
                foreach (var file in files)
                {
                    if (!file.FullPath.Contains(localFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine($"[StoragePathService] WARNING: File path does not contain LocalFolder: {file.FullPath}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // 记录权限错误但不中断流程
                System.Diagnostics.Debug.WriteLine($"[StoragePathService] 权限不足，无法访问存储文件信息: {ex.Message}");
            }
            catch (IOException ex)
            {
                // 记录IO错误但不中断流程
                System.Diagnostics.Debug.WriteLine($"[StoragePathService] 访问存储文件信息时发生IO错误: {ex.Message}");
            }
            catch (Exception ex)
            {
                // 记录其他错误但不中断流程
                System.Diagnostics.Debug.WriteLine($"[StoragePathService] 获取存储文件信息时出错: {ex.Message}");
            }

            lock (_lock)
            {
                _cachedStorageFiles = files;
            }
        }

        /// <summary>
        /// 创建存储文件信息对象
        /// </summary>
        private async Task<StorageFileInfo> CreateStorageFileInfoAsync(
            string name,
            string description,
            string fullPath,
            StorageFileType type)
        {
            var fileInfo = new StorageFileInfo
            {
                Name = name,
                Description = description,
                FullPath = fullPath,
                Type = type
            };

            try
            {
                if (File.Exists(fullPath))
                {
                    var info = new FileInfo(fullPath);
                    fileInfo.Exists = true;
                    fileInfo.FileSize = info.Length;
                    fileInfo.LastModified = info.LastWriteTime;
                    fileInfo.ErrorMessage = null;
                }
                else
                {
                    fileInfo.Exists = false;
                    fileInfo.FileSize = 0;
                    fileInfo.LastModified = DateTime.MinValue;
                    fileInfo.ErrorMessage = "文件不存在";
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // 处理权限不足的异常
                System.Diagnostics.Debug.WriteLine($"访问文件权限不足 ({fullPath}): {ex.Message}");
                fileInfo.Exists = false;
                fileInfo.FileSize = 0;
                fileInfo.LastModified = DateTime.MinValue;
                fileInfo.ErrorMessage = "权限不足，无法访问文件";
            }
            catch (IOException ex)
            {
                // 处理IO异常
                System.Diagnostics.Debug.WriteLine($"文件IO错误 ({fullPath}): {ex.Message}");
                fileInfo.Exists = false;
                fileInfo.FileSize = 0;
                fileInfo.LastModified = DateTime.MinValue;
                fileInfo.ErrorMessage = "文件访问错误";
            }
            catch (Exception ex)
            {
                // 处理其他异常
                System.Diagnostics.Debug.WriteLine($"获取文件信息失败 ({fullPath}): {ex.Message}");
                fileInfo.Exists = false;
                fileInfo.FileSize = 0;
                fileInfo.LastModified = DateTime.MinValue;
                fileInfo.ErrorMessage = "无法获取文件信息";
            }

            return await Task.FromResult(fileInfo);
        }

        /// <summary>
        /// 获取配置文件路径
        /// 
        /// 使用 ApplicationData.Current.LocalFolder 符合 WinUI3 最佳实践。
        /// 
        /// 路径构成：
        /// - 基础路径：ApplicationData.Current.LocalFolder
        /// - 子文件夹：Settings
        /// - 文件名：config.json
        /// - 完整路径：{LocalFolder}\Settings\config.json
        /// 
        /// 技术说明：
        /// - 与 ConfigurationService 使用相同的路径逻辑
        /// - 确保路径一致性
        /// 
        /// 错误处理：
        /// - UnauthorizedAccessException 和 IOException 会向上传播
        /// - 调用方负责处理这些异常
        /// 
        /// 需求: 3.1, 5.1, 5.2, 6.3, 8.3
        /// </summary>
        /// <returns>配置文件的完整路径</returns>
        /// <exception cref="UnauthorizedAccessException">权限不足时抛出</exception>
        /// <exception cref="IOException">文件系统访问错误时抛出</exception>
        private string GetConfigurationFilePath()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder.Path;
                var settingsFolder = Path.Combine(localFolder, "Settings");
                return Path.Combine(settingsFolder, "config.json");
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StoragePathService] 权限不足，无法获取配置文件路径: {ex.Message}");
                throw;
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StoragePathService] 获取配置文件路径时发生IO错误: {ex.Message}");
                throw;
            }
        }
    }
}
