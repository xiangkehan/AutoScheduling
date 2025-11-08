using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace AutoScheduling3.Models
{
    /// <summary>
    /// 存储文件类型枚举
    /// </summary>
    public enum StorageFileType
    {
        /// <summary>
        /// 数据库文件
        /// </summary>
        Database,

        /// <summary>
        /// 配置文件
        /// </summary>
        Configuration,

        /// <summary>
        /// 日志文件
        /// </summary>
        Log,

        /// <summary>
        /// 缓存文件
        /// </summary>
        Cache,

        /// <summary>
        /// 备份文件
        /// </summary>
        Backup
    }

    /// <summary>
    /// 存储文件信息数据模型
    /// 需求: 1.1, 1.3
    /// </summary>
    public partial class StorageFileInfo : ObservableObject
    {
        /// <summary>
        /// 文件名称
        /// </summary>
        [ObservableProperty]
        private string name = string.Empty;

        /// <summary>
        /// 文件描述
        /// </summary>
        [ObservableProperty]
        private string description = string.Empty;

        /// <summary>
        /// 文件完整路径
        /// </summary>
        [ObservableProperty]
        private string fullPath = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        [ObservableProperty]
        private long fileSize;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        [ObservableProperty]
        private DateTime lastModified;

        /// <summary>
        /// 文件是否存在
        /// </summary>
        [ObservableProperty]
        private bool exists;

        /// <summary>
        /// 文件类型
        /// </summary>
        [ObservableProperty]
        private StorageFileType type;

        /// <summary>
        /// 错误消息（如果有）
        /// </summary>
        [ObservableProperty]
        private string? errorMessage;

        /// <summary>
        /// 格式化的文件大小
        /// </summary>
        public string FormattedFileSize => FormatFileSize(FileSize);

        /// <summary>
        /// 格式化的最后修改时间
        /// </summary>
        public string FormattedLastModified => LastModified.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>
        /// 状态文本
        /// </summary>
        public string StatusText => !string.IsNullOrEmpty(ErrorMessage) ? ErrorMessage : (Exists ? "存在" : "不存在");

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化后的文件大小字符串</returns>
        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }
}
