using System.Collections.Generic;

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
    }
}
