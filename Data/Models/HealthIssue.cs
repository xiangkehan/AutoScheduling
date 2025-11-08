using AutoScheduling3.Data.Enums;

namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Represents a single health issue found in the database
    /// </summary>
    public class HealthIssue
    {
        /// <summary>
        /// Severity level of the issue
        /// </summary>
        public IssueSeverity Severity { get; set; }

        /// <summary>
        /// Category of the issue (e.g., Integrity, Schema, Performance)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Detailed description of the issue
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Recommended action to resolve the issue
        /// </summary>
        public string Recommendation { get; set; }
    }
}
