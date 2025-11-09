namespace AutoScheduling3.DTOs.ImportExport
{
    /// <summary>
    /// 导入操作的配置选项
    /// </summary>
    public class ImportOptions
    {
        public ConflictResolutionStrategy Strategy { get; set; } = ConflictResolutionStrategy.Skip;
        public bool CreateBackupBeforeImport { get; set; } = true;
        public bool ValidateReferences { get; set; } = true;
        public bool ContinueOnError { get; set; } = false;
    }
}
