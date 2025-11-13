using System;

namespace AutoScheduling3.Services.ImportExport.Models
{
    /// <summary>
    /// 导入错误上下文，用于记录详细的错误信息
    /// </summary>
    public class ImportErrorContext
    {
        /// <summary>
        /// 当前正在处理的表名
        /// </summary>
        public string? CurrentTable { get; set; }

        /// <summary>
        /// 当前正在处理的记录 ID
        /// </summary>
        public int? CurrentRecordId { get; set; }

        /// <summary>
        /// 当前正在执行的操作类型
        /// </summary>
        public string? CurrentOperation { get; set; }

        /// <summary>
        /// 已处理的记录数
        /// </summary>
        public int ProcessedRecords { get; set; }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// 操作开始时间
        /// </summary>
        public DateTime OperationStartTime { get; set; }

        /// <summary>
        /// 获取格式化的错误上下文信息
        /// </summary>
        public string GetFormattedContext()
        {
            var parts = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(CurrentTable))
            {
                parts.Add($"Table: {CurrentTable}");
            }

            if (CurrentRecordId.HasValue)
            {
                parts.Add($"Record ID: {CurrentRecordId.Value}");
            }

            if (!string.IsNullOrEmpty(CurrentOperation))
            {
                parts.Add($"Operation: {CurrentOperation}");
            }

            if (TotalRecords > 0)
            {
                parts.Add($"Progress: {ProcessedRecords}/{TotalRecords} records");
            }

            if (OperationStartTime != default)
            {
                var elapsed = DateTime.UtcNow - OperationStartTime;
                parts.Add($"Elapsed Time: {elapsed.TotalSeconds:F2}s");
            }

            return string.Join(", ", parts);
        }

        /// <summary>
        /// 重置错误上下文
        /// </summary>
        public void Reset()
        {
            CurrentTable = null;
            CurrentRecordId = null;
            CurrentOperation = null;
            ProcessedRecords = 0;
            TotalRecords = 0;
            OperationStartTime = default;
        }
    }
}
