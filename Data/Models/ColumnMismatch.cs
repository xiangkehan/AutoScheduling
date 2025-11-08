using AutoScheduling3.Data.Enums;

namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Represents a mismatch between expected and actual column definitions
    /// </summary>
    public class ColumnMismatch
    {
        /// <summary>
        /// Name of the table containing the mismatched column
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Name of the mismatched column
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Type of mismatch detected
        /// </summary>
        public MismatchType Type { get; set; }

        /// <summary>
        /// Expected value or definition
        /// </summary>
        public string Expected { get; set; }

        /// <summary>
        /// Actual value or definition found in the database
        /// </summary>
        public string Actual { get; set; }
    }
}
