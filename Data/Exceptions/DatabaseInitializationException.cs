using AutoScheduling3.Data.Enums;
using System;

namespace AutoScheduling3.Data.Exceptions
{
    /// <summary>
    /// Exception thrown when database initialization fails
    /// </summary>
    public class DatabaseInitializationException : Exception
    {
        public InitializationStage FailedStage { get; }

        public DatabaseInitializationException(
            string message,
            InitializationStage stage,
            Exception innerException = null)
            : base(message, innerException)
        {
            FailedStage = stage;
        }
    }
}
