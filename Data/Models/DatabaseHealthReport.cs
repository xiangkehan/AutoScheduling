using System;
using System.Collections.Generic;
using AutoScheduling3.Data.Enums;

namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Represents a comprehensive health report for the database
    /// </summary>
    public class DatabaseHealthReport
    {
        /// <summary>
        /// Overall health status of the database
        /// </summary>
        public HealthStatus OverallStatus { get; set; }

        /// <summary>
        /// List of health issues found during the check
        /// </summary>
        public List<HealthIssue> Issues { get; set; } = new List<HealthIssue>();

        /// <summary>
        /// Database metrics collected during the health check
        /// </summary>
        public DatabaseMetrics Metrics { get; set; }

        /// <summary>
        /// Timestamp when the health check was performed
        /// </summary>
        public DateTime CheckedAt { get; set; }
    }
}
