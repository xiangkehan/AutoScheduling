namespace AutoScheduling3.DTOs.ImportExport
{
    /// <summary>
    /// 验证错误信息
    /// </summary>
    public class ValidationError
    {
        public string Table { get; set; }
        public int? RecordId { get; set; }
        public string Field { get; set; }
        public string Message { get; set; }
        public ValidationErrorType Type { get; set; }
    }
}
