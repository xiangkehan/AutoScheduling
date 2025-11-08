namespace AutoScheduling3.Data.Models
{
    /// <summary>
    /// Represents a single repair action performed on the database
    /// </summary>
    public class RepairAction
    {
        /// <summary>
        /// Type of repair action
        /// </summary>
        public RepairActionType Type { get; set; }

        /// <summary>
        /// Target of the repair (table/column/index name)
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Description of the repair action
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Indicates whether the action was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if the action failed
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
