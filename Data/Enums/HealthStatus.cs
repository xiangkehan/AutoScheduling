namespace AutoScheduling3.Data.Enums
{
    /// <summary>
    /// Represents the overall health status of the database
    /// </summary>
    public enum HealthStatus
    {
        /// <summary>
        /// Database is healthy with no issues
        /// </summary>
        Healthy,

        /// <summary>
        /// Database has non-critical issues
        /// </summary>
        Warning,

        /// <summary>
        /// Database has critical issues
        /// </summary>
        Critical,

        /// <summary>
        /// Health status is unknown
        /// </summary>
        Unknown
    }
}
