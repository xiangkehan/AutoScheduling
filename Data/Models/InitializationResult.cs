using System;
using System.Collections.Generic;
using AutoScheduling3.Data.Enums;

namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Represents the result of a database initialization operation
    /// </summary>
    public class InitializationResult
    {
        /// <summary>
        /// Indicates whether initialization was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Final state of initialization
        /// </summary>
        public InitializationState FinalState { get; set; }

        /// <summary>
        /// Stage at which initialization failed (null if successful)
        /// </summary>
        public InitializationStage? FailedStage { get; set; }

        /// <summary>
        /// Error message if initialization failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Duration of the initialization process
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// List of warnings generated during initialization
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
