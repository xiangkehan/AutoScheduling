namespace AutoScheduling3.Services.ImportExport.Models
{
    /// <summary>
    /// 单个表的导入统计信息
    /// </summary>
    public class TableImportStats
    {
        /// <summary>
        /// 总记录数
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 插入的记录数
        /// </summary>
        public int Inserted { get; set; }

        /// <summary>
        /// 更新的记录数
        /// </summary>
        public int Updated { get; set; }

        /// <summary>
        /// 未变化的记录数
        /// </summary>
        public int Unchanged { get; set; }

        /// <summary>
        /// 跳过的记录数
        /// </summary>
        public int Skipped { get; set; }

        /// <summary>
        /// 导入持续时间
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}
