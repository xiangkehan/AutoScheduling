using System;

namespace AutoScheduling3.DTOs.ImportExport
{
    /// <summary>
    /// 导出操作结果
    /// </summary>
    public class ExportResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DataStatistics Statistics { get; set; }
        public TimeSpan Duration { get; set; }
        public string ErrorMessage { get; set; }
    }
}
