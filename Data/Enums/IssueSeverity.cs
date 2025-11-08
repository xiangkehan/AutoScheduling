namespace AutoScheduling3.Data.Enums
{
    /// <summary>
    /// Represents the severity level of a database health issue
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>
        /// Critical issue that requires immediate attention
        /// </summary>
        Critical,

        /// <summary>
        /// Warning that should be addressed
        /// </summary>
        Warning,

        /// <summary>
        /// Informational message
        /// </summary>
        Info
    }
}
