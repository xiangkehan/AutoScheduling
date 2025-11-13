using System.Collections.Generic;
using AutoScheduling3.Services.ImportExport.Models;

namespace AutoScheduling3.DTOs.ImportExport
{
    /// <summary>
    /// 导入统计信息
    /// </summary>
    public class ImportStatistics
    {
        public int TotalRecords { get; set; }
        public int ImportedRecords { get; set; }
        public int SkippedRecords { get; set; }
        public int FailedRecords { get; set; }
        public Dictionary<string, int> RecordsByTable { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// 插入的记录总数
        /// </summary>
        public int InsertedRecords { get; set; }

        /// <summary>
        /// 更新的记录总数
        /// </summary>
        public int UpdatedRecords { get; set; }

        /// <summary>
        /// 未变化的记录总数
        /// </summary>
        public int UnchangedRecords { get; set; }

        /// <summary>
        /// 按表分类的详细统计信息
        /// </summary>
        public Dictionary<string, TableImportStats> DetailsByTable { get; set; } = new Dictionary<string, TableImportStats>();
    }
}
