namespace AutoScheduling3.Data.Enums
{
    /// <summary>
    /// Represents the current state of database initialization
    /// </summary>
    public enum InitializationState
    {
        /// <summary>
        /// Initialization has not started
        /// </summary>
        NotStarted,

        /// <summary>
        /// Initialization is currently in progress
        /// </summary>
        InProgress,

        /// <summary>
        /// Initialization completed successfully
        /// </summary>
        Completed,

        /// <summary>
        /// Initialization failed
        /// </summary>
        Failed
    }
}
