namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Represents metrics collected about the database
    /// </summary>
    public class DatabaseMetrics
    {
        /// <summary>
        /// Size of the database file in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Number of tables in the database
        /// </summary>
        public int TableCount { get; set; }

        /// <summary>
        /// Number of indexes in the database
        /// </summary>
        public int IndexCount { get; set; }

        /// <summary>
        /// Total number of rows across all tables
        /// </summary>
        public int TotalRowCount { get; set; }

        /// <summary>
        /// Percentage of database fragmentation
        /// </summary>
        public double FragmentationPercentage { get; set; }
    }
}
