using System.Collections.Generic;

namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Represents the schema definition for a database table
    /// </summary>
    public class TableSchema
    {
        /// <summary>
        /// Name of the table
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of column definitions for the table
        /// </summary>
        public List<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();

        /// <summary>
        /// List of index names for the table
        /// </summary>
        public List<string> Indexes { get; set; } = new List<string>();

        /// <summary>
        /// Indicates whether this table is critical for application functionality
        /// </summary>
        public bool IsCritical { get; set; }
    }
}
