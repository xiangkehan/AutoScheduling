namespace AutoScheduling3.Data.Enums
{
    /// <summary>
    /// Represents the specific stage of database initialization
    /// </summary>
    public enum InitializationStage
    {
        /// <summary>
        /// Creating database directory
        /// </summary>
        DirectoryCreation,

        /// <summary>
        /// Testing database connection
        /// </summary>
        ConnectionTest,

        /// <summary>
        /// Performing health check on existing database
        /// </summary>
        HealthCheck,

        /// <summary>
        /// Validating database schema
        /// </summary>
        SchemaValidation,

        /// <summary>
        /// Repairing database schema issues
        /// </summary>
        SchemaRepair,

        /// <summary>
        /// Creating database tables
        /// </summary>
        TableCreation,

        /// <summary>
        /// Creating database indexes
        /// </summary>
        IndexCreation,

        /// <summary>
        /// Running database migrations
        /// </summary>
        Migration,

        /// <summary>
        /// Finalizing initialization
        /// </summary>
        Finalization
    }
}
