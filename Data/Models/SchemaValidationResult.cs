using System.Collections.Generic;

namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Represents the result of a database schema validation
    /// </summary>
    public class SchemaValidationResult
    {
        /// <summary>
        /// Indicates whether the schema is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of tables that are missing from the database
        /// </summary>
        public List<string> MissingTables { get; set; } = new List<string>();

        /// <summary>
        /// List of column mismatches found in the database
        /// </summary>
        public List<ColumnMismatch> ColumnMismatches { get; set; } = new List<ColumnMismatch>();

        /// <summary>
        /// List of indexes that are missing from the database
        /// </summary>
        public List<string> MissingIndexes { get; set; } = new List<string>();

        /// <summary>
        /// List of unexpected objects (tables/columns) found in the database
        /// </summary>
        public List<string> ExtraObjects { get; set; } = new List<string>();
    }
}
