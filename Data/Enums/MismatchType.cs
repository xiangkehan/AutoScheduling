namespace AutoScheduling3.Data.Enums
{
    /// <summary>
    /// Represents the type of schema mismatch detected
    /// </summary>
    public enum MismatchType
    {
        /// <summary>
        /// Column is missing from the table
        /// </summary>
        Missing,

        /// <summary>
        /// Column data type does not match expected type
        /// </summary>
        TypeMismatch,

        /// <summary>
        /// Column nullability does not match expected setting
        /// </summary>
        NullabilityMismatch,

        /// <summary>
        /// Column exists but was not expected
        /// </summary>
        ExtraColumn
    }
}
