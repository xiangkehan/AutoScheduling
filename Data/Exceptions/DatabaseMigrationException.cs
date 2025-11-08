using System;

namespace AutoScheduling3.Data.Exceptions
{
    /// <summary>
    /// Exception thrown when database migration fails
    /// </summary>
    public class DatabaseMigrationException : Exception
    {
        public int TargetVersion { get; }

        public DatabaseMigrationException(
            string message,
            Exception innerException = null)
            : base(message, innerException)
        {
        }

        public DatabaseMigrationException(
            string message,
            int targetVersion,
            Exception innerException = null)
            : base(message, innerException)
        {
            TargetVersion = targetVersion;
        }
    }
}
