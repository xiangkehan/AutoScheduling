using System;
using System.Collections.Generic;

namespace AutoScheduling3.Data.Exceptions
{
    /// <summary>
    /// Exception thrown when database corruption is detected
    /// </summary>
    public class DatabaseCorruptionException : Exception
    {
        public List<string> CorruptionDetails { get; }

        public DatabaseCorruptionException(
            string message,
            List<string> details)
            : base(message)
        {
            CorruptionDetails = details ?? new List<string>();
        }
    }
}
