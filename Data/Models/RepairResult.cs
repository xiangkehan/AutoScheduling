using System.Collections.Generic;

namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Represents the result of a database repair operation
    /// </summary>
    public class RepairResult
    {
        /// <summary>
        /// Indicates whether the repair was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// List of repair actions that were successfully performed
        /// </summary>
        public List<RepairAction> ActionsPerformed { get; set; } = new List<RepairAction>();

        /// <summary>
        /// List of repair actions that failed
        /// </summary>
        public List<RepairAction> FailedActions { get; set; } = new List<RepairAction>();

        /// <summary>
        /// Error message if the repair failed
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
