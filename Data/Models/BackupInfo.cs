using System;

namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Represents information about a database backup file
    /// </summary>
    public class BackupInfo
    {
        /// <summary>
        /// Full path to the backup file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Name of the backup file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Size of the backup file in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Timestamp when the backup was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Database version at the time of backup
        /// </summary>
        public int DatabaseVersion { get; set; }

        /// <summary>
        /// Indicates whether the backup has been verified for integrity
        /// </summary>
        public bool IsVerified { get; set; }
    }
}
