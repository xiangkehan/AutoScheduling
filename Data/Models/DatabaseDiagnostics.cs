using System.Collections.Generic;

namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Comprehensive diagnostics information about the database
    /// </summary>
    public class DatabaseDiagnostics
    {
        /// <summary>
        /// Basic database information
        /// </summary>
        public DatabaseInfo BasicInfo { get; set; }

        /// <summary>
        /// Health report from the most recent health check
        /// </summary>
        public DatabaseHealthReport HealthReport { get; set; }

        /// <summary>
        /// Schema validation results
        /// </summary>
        public SchemaValidationResult SchemaValidation { get; set; }

        /// <summary>
        /// Row counts for each table
        /// </summary>
        public Dictionary<string, int> TableRowCounts { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Index usage statistics
        /// </summary>
        public List<IndexUsageStats> IndexStats { get; set; } = new List<IndexUsageStats>();

        /// <summary>
        /// Performance metrics
        /// </summary>
        public DatabasePerformanceMetrics Performance { get; set; }
    }

    /// <summary>
    /// Statistics about index usage
    /// </summary>
    public class IndexUsageStats
    {
        /// <summary>
        /// Name of the index
        /// </summary>
        public string IndexName { get; set; }

        /// <summary>
        /// Table the index belongs to
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Whether the index exists
        /// </summary>
        public bool Exists { get; set; }
    }

    /// <summary>
    /// Performance metrics for the database
    /// </summary>
    public class DatabasePerformanceMetrics
    {
        /// <summary>
        /// Time taken for the last initialization
        /// </summary>
        public System.TimeSpan LastInitializationTime { get; set; }

        /// <summary>
        /// Average query execution time
        /// </summary>
        public System.TimeSpan AverageQueryTime { get; set; }

        /// <summary>
        /// Current connection pool size
        /// </summary>
        public int ConnectionPoolSize { get; set; }
    }
}
