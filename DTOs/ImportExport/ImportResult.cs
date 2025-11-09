using System;
using System.Collections.Generic;

namespace AutoScheduling3.DTOs.ImportExport
{
    /// <summary>
    /// 导入操作结果
    /// </summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public ImportStatistics Statistics { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public string ErrorMessage { get; set; }
    }
}
