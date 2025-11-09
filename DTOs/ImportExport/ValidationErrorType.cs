namespace AutoScheduling3.DTOs.ImportExport
{
    /// <summary>
    /// 验证错误类型枚举
    /// </summary>
    public enum ValidationErrorType
    {
        MissingField,
        InvalidDataType,
        InvalidValue,
        BrokenReference,
        DuplicateKey,
        ConstraintViolation
    }
}
