using System;
using System.IO;

namespace AutoScheduling3.Data
{
    /// <summary>
    /// 数据库配置：管理数据库路径和连接设置
    /// 需求: 1.2, 2.1
    /// </summary>
    public static class DatabaseConfiguration
    {
        /// <summary>
        /// 默认数据库文件名
        /// </summary>
        public const string DefaultDatabaseFileName = "GuardDutyScheduling.db";

        /// <summary>
        /// 获取默认数据库路径
        /// </summary>
        public static string GetDefaultDatabasePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "AutoScheduling3");
            
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            return Path.Combine(appFolder, DefaultDatabaseFileName);
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
        /// </summary>
        public static string GetBackupDatabasePath(string originalPath)
        {
            var directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
            var fileName = Path.GetFileNameWithoutExtension(originalPath);
            var extension = Path.GetExtension(originalPath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            return Path.Combine(directory, $"{fileName}_backup_{timestamp}{extension}");
        }

        /// <summary>
        /// 验证数据库路径
        /// </summary>
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
            catch
            {
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