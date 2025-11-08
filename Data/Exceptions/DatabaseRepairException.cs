using System;
using AutoScheduling3.Data.Models;

namespace AutoScheduling3.Data.Exceptions
{
    /// <summary>
    /// Exception thrown when database repair fails
    /// </summary>
    public class DatabaseRepairException : Exception
    {
        public RepairResult PartialResult { get; }

        public DatabaseRepairException(
            string message,
            RepairResult partialResult)
            : base(message)
        {
            PartialResult = partialResult;
        }
    }
}
