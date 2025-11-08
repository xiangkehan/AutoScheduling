using System;
using System.Collections.Generic;
using AutoScheduling3.Data.Enums;

namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Contains the result of a database initialization attempt
    /// </summary>
    public class InitializationResult
    {
        /// <summary>
        /// Whether initialization succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The final state after initialization
        /// </summary>
        public InitializationState FinalState { get; set; }

        /// <summary>
        /// The stage where initialization failed (null if successful)
        /// </summary>
        public InitializationStage? FailedStage { get; set; }

        /// <summary>
        /// Error message if initialization failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// How long initialization took
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Non-critical warnings that occurred during initialization
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
