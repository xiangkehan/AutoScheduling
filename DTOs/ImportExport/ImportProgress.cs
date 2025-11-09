namespace AutoScheduling3.DTOs.ImportExport
{
    /// <summary>
    /// 导入进度信息
    /// </summary>
    public class ImportProgress
    {
        public string CurrentTable { get; set; }
        public string CurrentOperation { get; set; } // "Validating", "Importing", "Verifying"
        public int ProcessedRecords { get; set; }
        public int TotalRecords { get; set; }
        public double PercentComplete { get; set; }
    }
}
