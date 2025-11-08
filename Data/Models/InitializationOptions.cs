using System;

namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Configuration options for database initialization
    /// </summary>
    public class InitializationOptions
    {
        /// <summary>
        /// Whether to perform a health check on existing databases
        /// </summary>
        public bool PerformHealthCheck { get; set; } = true;

        /// <summary>
        /// Whether to automatically repair schema issues
        /// </summary>
        public bool AutoRepair { get; set; } = true;

        /// <summary>
        /// Whether to create a backup before attempting repairs
        /// </summary>
        public bool CreateBackupBeforeRepair { get; set; } = true;

        /// <summary>
        /// Number of times to retry connection attempts
        /// </summary>
        public int ConnectionRetryCount { get; set; } = 3;

        /// <summary>
        /// Delay between connection retry attempts
        /// </summary>
        public TimeSpan ConnectionRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }
}
