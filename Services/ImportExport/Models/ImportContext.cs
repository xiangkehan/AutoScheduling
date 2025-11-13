using Microsoft.Data.Sqlite;
using AutoScheduling3.DTOs.ImportExport;
using AutoScheduling3.Services.ImportExport.Monitoring;

namespace AutoScheduling3.Services.ImportExport.Models
{
    /// <summary>
    /// 导入操作的上下文对象，在整个导入过程中传递
    /// </summary>
    public class ImportContext
    {
        /// <summary>
        /// 数据库连接
        /// </summary>
        public SqliteConnection Connection { get; set; }

        /// <summary>
        /// 数据库事务
        /// </summary>
        public SqliteTransaction Transaction { get; set; }

        /// <summary>
        /// 导入选项
        /// </summary>
        public ImportOptions Options { get; set; }

        /// <summary>
        /// 性能监控器
        /// </summary>
        public PerformanceMonitor PerformanceMonitor { get; set; }

        /// <summary>
        /// 导入统计信息
        /// </summary>
        public ImportStatistics Statistics { get; set; }

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; set; }

        /// <summary>
        /// 取消令牌
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        public ImportContext()
        {
            Warnings = new List<string>();
            CancellationToken = CancellationToken.None;
        }
    }
}
