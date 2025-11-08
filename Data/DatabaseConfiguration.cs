using System;
using System.IO;
using Windows.Storage;

namespace AutoScheduling3.Data
{
    /// <summary>
    /// 数据库配置：管理数据库路径和连接设置
    /// 
    /// 使用 WinUI3 标准的 ApplicationData API 来管理应用程序数据存储。
    /// 
    /// 为什么使用 ApplicationData.Current.LocalFolder：
    /// 1. WinUI3 最佳实践：这是 WinUI3 应用程序推荐的数据存储方式
    /// 2. 应用商店兼容性：确保应用程序符合 Windows 应用商店的要求
    /// 3. 沙箱安全性：应用程序在沙箱环境中正确运行，自动拥有读写权限
    /// 4. 系统管理：Windows 系统可以正确管理应用数据的备份和清理
    /// 5. 跨设备同步：为未来支持漫游数据提供基础
    /// 
    /// 需求: 1.2, 2.1, 8.3
    /// </summary>
    public static class DatabaseConfiguration
    {
        /// <summary>
        /// 默认数据库文件名
        /// </summary>
        public const string DefaultDatabaseFileName = "GuardDutyScheduling.db";

        /// <summary>
        /// 获取默认数据库路径
        /// 
        /// 使用 ApplicationData.Current.LocalFolder 而不是 Environment.SpecialFolder.LocalApplicationData。
        /// 
        /// 技术说明：
        /// - ApplicationData.Current.LocalFolder 是 WinUI3 应用程序的标准数据存储位置
        /// - 路径通常位于：%LocalAppData%\Packages\{PackageFamilyName}\LocalState
        /// - 应用程序自动拥有此位置的完整读写权限
        /// - 数据库文件直接存储在 LocalFolder 根目录，无需额外的应用名称子文件夹
        /// 
        /// 日志记录：
        /// - 输出 LocalFolder 路径和完整数据库路径，便于诊断和调试
        /// 
        /// 错误处理：
        /// - 捕获 UnauthorizedAccessException：权限不足
        /// - 捕获 IOException：文件系统访问错误
        /// 
        /// 需求: 1.1, 1.2, 8.1, 8.3
        /// </summary>
        /// <returns>数据库文件的完整路径</returns>
        /// <exception cref="InvalidOperationException">无法访问应用程序数据文件夹时抛出</exception>
        public static string GetDefaultDatabasePath()
        {
            try
            {
                // 使用 WinUI3 标准的 ApplicationData API
                var localFolderPath = ApplicationData.Current.LocalFolder.Path;
                var databasePath = Path.Combine(localFolderPath, DefaultDatabaseFileName);
                
                // 记录使用的路径
                System.Diagnostics.Debug.WriteLine($"[DatabaseConfiguration] LocalFolder path: {localFolderPath}");
                System.Diagnostics.Debug.WriteLine($"[DatabaseConfiguration] Database path: {databasePath}");
                
                return databasePath;
            }
            catch (UnauthorizedAccessException ex)
            {
                var errorMsg = "权限不足，无法访问应用程序数据文件夹";
                System.Diagnostics.Debug.WriteLine($"[DatabaseConfiguration] {errorMsg}: {ex.Message}");
                throw new InvalidOperationException(errorMsg, ex);
            }
            catch (IOException ex)
            {
                var errorMsg = "访问应用程序数据文件夹时发生IO错误";
                System.Diagnostics.Debug.WriteLine($"[DatabaseConfiguration] {errorMsg}: {ex.Message}");
                throw new InvalidOperationException(errorMsg, ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseConfiguration] Error getting database path: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取测试数据库路径
        /// </summary>
        public static string GetTestDatabasePath()
        {
            return ":memory:"; // SQLite 内存数据库
        }

        /// <summary>
        /// 获取备份数据库路径
        /// 
        /// 备份文件存储在 ApplicationData.Current.LocalFolder 下的 backups 子文件夹中。
        /// 
        /// 技术说明：
        /// - 备份路径：{LocalFolder}\backups\{原文件名}_backup_{时间戳}.db
        /// - 时间戳格式：yyyyMMdd_HHmmss（例如：20241108_143025）
        /// - 自动创建 backups 文件夹（如果不存在）
        /// 
        /// 日志记录：
        /// - 记录备份文件夹创建操作
        /// - 输出生成的备份文件路径
        /// 
        /// 错误处理：
        /// - 捕获权限和IO错误，提供清晰的错误消息
        /// 
        /// 需求: 1.3, 4.1, 8.1, 8.3
        /// </summary>
        /// <param name="originalPath">原始数据库文件路径</param>
        /// <returns>备份文件的完整路径</returns>
        /// <exception cref="InvalidOperationException">无法创建备份文件夹时抛出</exception>
        public static string GetBackupDatabasePath(string originalPath)
        {
            try
            {
                // 使用 ApplicationData.Current.LocalFolder 下的 backups 子文件夹
                var localFolderPath = ApplicationData.Current.LocalFolder.Path;
                var backupsFolder = Path.Combine(localFolderPath, "backups");
                
                // 确保备份文件夹存在
                if (!Directory.Exists(backupsFolder))
                {
                    Directory.CreateDirectory(backupsFolder);
                    System.Diagnostics.Debug.WriteLine($"[DatabaseConfiguration] Created backups folder: {backupsFolder}");
                }
                
                var fileName = Path.GetFileNameWithoutExtension(originalPath);
                var extension = Path.GetExtension(originalPath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(backupsFolder, $"{fileName}_backup_{timestamp}{extension}");
                
                // 记录备份路径
                System.Diagnostics.Debug.WriteLine($"[DatabaseConfiguration] Backup path: {backupPath}");
                
                return backupPath;
            }
            catch (UnauthorizedAccessException ex)
            {
                var errorMsg = "权限不足，无法创建备份文件夹";
                System.Diagnostics.Debug.WriteLine($"[DatabaseConfiguration] {errorMsg}: {ex.Message}");
                throw new InvalidOperationException(errorMsg, ex);
            }
            catch (IOException ex)
            {
                var errorMsg = "创建备份文件夹时发生IO错误";
                System.Diagnostics.Debug.WriteLine($"[DatabaseConfiguration] {errorMsg}: {ex.Message}");
                throw new InvalidOperationException(errorMsg, ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseConfiguration] Error getting backup path: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 验证数据库路径的有效性
        /// 
        /// 验证逻辑：
        /// - 检查路径是否为空或仅包含空白字符
        /// - 内存数据库（":memory:"）始终有效
        /// - 检查目录是否存在，不存在则尝试创建
        /// 
        /// 错误处理：
        /// - 捕获所有异常并返回 false，不抛出异常
        /// - 记录详细的错误日志
        /// 
        /// 需求: 5.1, 8.3
        /// </summary>
        /// <param name="path">要验证的数据库路径</param>
        /// <returns>路径有效返回 true，否则返回 false</returns>
        public static bool ValidateDatabasePath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return false;

                // 内存数据库总是有效的
                if (path == ":memory:")
                    return true;

                // 检查目录是否存在或可以创建
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseConfiguration] 权限不足，无法验证路径: {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseConfiguration] 验证路径时发生IO错误: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseConfiguration] 验证路径时发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取连接字符串
        /// </summary>
        public static string GetConnectionString(string databasePath)
        {
            return $"Data Source={databasePath};Cache=Shared;";
        }

        /// <summary>
        /// 获取带有性能优化的连接字符串
        /// </summary>
        public static string GetOptimizedConnectionString(string databasePath)
        {
            return $"Data Source={databasePath};Cache=Shared;Journal Mode=WAL;Synchronous=Normal;";
        }

        /// <summary>
        /// 最大备份数量
        /// 需求: 5.5
        /// </summary>
        public static int MaxBackupCount { get; set; } = 5;

        /// <summary>
        /// 是否启用自动修复
        /// 需求: 6.1
        /// </summary>
        public static bool AutoRepairEnabled { get; set; } = true;

        /// <summary>
        /// 是否在启动时执行健康检查
        /// 需求: 6.1
        /// </summary>
        public static bool HealthCheckOnStartup { get; set; } = true;

        /// <summary>
        /// 连接重试次数
        /// 需求: 6.1
        /// </summary>
        public static int ConnectionRetryCount { get; set; } = 3;

        /// <summary>
        /// 连接重试延迟
        /// 需求: 6.1
        /// </summary>
        public static TimeSpan ConnectionRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }
}