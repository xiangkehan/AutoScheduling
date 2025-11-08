using System;
using System.Collections.Generic;
using AutoScheduling3.Data.Enums;

namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Tracks the progress of database initialization
    /// </summary>
    public class InitializationProgress
    {
        /// <summary>
        /// The current stage of initialization
        /// </summary>
        public InitializationStage CurrentStage { get; set; }

        /// <summary>
        /// A descriptive message about the current operation
        /// </summary>
        public string CurrentMessage { get; set; } = string.Empty;

        /// <summary>
        /// When initialization started
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// When initialization completed (null if still in progress)
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// List of stages that have been completed
        /// </summary>
        public List<string> CompletedStages { get; set; } = new List<string>();
    }
}
