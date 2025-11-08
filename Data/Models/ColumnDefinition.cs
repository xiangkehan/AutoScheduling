namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Represents the definition of a database column
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>
        /// Name of the column
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Data type of the column (e.g., INTEGER, TEXT, REAL)
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Indicates whether the column can contain NULL values
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Default value for the column
        /// </summary>
        public string DefaultValue { get; set; }
    }
}
