namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Options for controlling database repair operations
    /// </summary>
    public class RepairOptions
    {
        /// <summary>
        /// Whether to create missing tables during repair
        /// </summary>
        public bool CreateMissingTables { get; set; } = true;

        /// <summary>
        /// Whether to add missing columns to existing tables during repair
        /// </summary>
        public bool AddMissingColumns { get; set; } = true;

        /// <summary>
        /// Whether to create missing indexes during repair
        /// </summary>
        public bool CreateMissingIndexes { get; set; } = true;

        /// <summary>
        /// Whether to create a backup before performing repair operations
        /// </summary>
        public bool BackupBeforeRepair { get; set; } = true;
    }
}
