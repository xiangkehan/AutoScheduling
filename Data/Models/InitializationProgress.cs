using System;
using System.Collections.Generic;
using AutoScheduling3.Data.Enums;

namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Represents the progress of database initialization
    /// </summary>
    public class InitializationProgress
    {
        /// <summary>
        /// Current stage of initialization
        /// </summary>
        public InitializationStage CurrentStage { get; set; }

        /// <summary>
        /// Current status message
        /// </summary>
        public string CurrentMessage { get; set; }

        /// <summary>
        /// Timestamp when initialization started
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Timestamp when initialization completed (null if still in progress)
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// List of stages that have been completed
        /// </summary>
        public List<string> CompletedStages { get; set; } = new List<string>();
    }
}
