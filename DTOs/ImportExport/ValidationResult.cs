using System.Collections.Generic;

namespace AutoScheduling3.DTOs.ImportExport
{
    /// <summary>
    /// 数据验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
        public ExportMetadata Metadata { get; set; }
    }
}
