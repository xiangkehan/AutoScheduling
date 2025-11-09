namespace AutoScheduling3.DTOs.ImportExport
{
    /// <summary>
    /// 导出进度信息
    /// </summary>
    public class ExportProgress
    {
        public string CurrentTable { get; set; }
        public int ProcessedRecords { get; set; }
        public int TotalRecords { get; set; }
        public double PercentComplete { get; set; }
    }
}
