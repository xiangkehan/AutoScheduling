using System;

namespace AutoScheduling3.DTOs.ImportExport
{
    /// <summary>
    /// 导出文件的元数据信息
    /// </summary>
    public class ExportMetadata
    {
        public string ExportVersion { get; set; } = "1.0";
        public DateTime ExportedAt { get; set; }
        public int DatabaseVersion { get; set; }
        public string ApplicationVersion { get; set; }
        public DataStatistics Statistics { get; set; }
    }
}
