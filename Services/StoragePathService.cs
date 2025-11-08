using AutoScheduling3.Data;
using AutoScheduling3.Models;
using AutoScheduling3.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AutoScheduling3.Services
{
    /// <summary>
    /// 存储路径服务实现 - 管理应用程序存储文件信息
    /// 需求: 1.1, 1.3
    /// </summary>
    public class StoragePathService : IStoragePathService
    {
        private List<StorageFileInfo> _cachedStorageFiles = new();
        private readonly object _lock = new();

        public async Task InitializeAsync()
        {
            await RefreshStorageInfoAsync();
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
        /// </summary>
        public async Task RefreshStorageInfoAsync()
        {
            var files = new List<StorageFileInfo>();

            try
            {
                // 获取数据库文件信息
                var dbPath = DatabaseConfiguration.GetDefaultDatabasePath();
                files.Add(await CreateStorageFileInfoAsync(
                    "数据库文件",
                    "存储排班数据的SQLite数据库",
                    dbPath,
                    StorageFileType.Database));

                // 获取配置文件信息
                var configPath = GetConfigurationFilePath();
                files.Add(await CreateStorageFileInfoAsync(
                    "配置文件",
                    "应用程序设置和用户偏好",
                    configPath,
                    StorageFileType.Configuration));
            }
            catch (Exception ex)
            {
                // 记录错误但不中断流程
                System.Diagnostics.Debug.WriteLine($"获取存储文件信息时出错: {ex.Message}");
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
        /// </summary>
        private string GetConfigurationFilePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "AutoScheduling3");
            return Path.Combine(appFolder, "config.json");
        }
    }
}
