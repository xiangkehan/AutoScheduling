namespace AutoScheduling3.Data.Enums
{
    /// <summary>
    /// Represents the type of repair action performed on the database
    /// </summary>
    public enum RepairActionType
    {
        /// <summary>
        /// Create a missing table
        /// </summary>
        CreateTable,

        /// <summary>
        /// Add a missing column to an existing table
        /// </summary>
        AddColumn,

        /// <summary>
        /// Create a missing index
        /// </summary>
        CreateIndex,

        /// <summary>
        /// Update a constraint
        /// </summary>
        UpdateConstraint
    }
}
