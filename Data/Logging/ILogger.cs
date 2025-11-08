namespace AutoScheduling3.Data.Logging
{
    /// <summary>
    /// Simple logging interface for database operations
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Log an informational message
        /// </summary>
        void Log(string message);

        /// <summary>
        /// Log a warning message
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        /// Log an error message
        /// </summary>
        void LogError(string message);
    }
}
