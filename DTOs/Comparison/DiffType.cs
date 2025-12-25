namespace AutoScheduling3.DTOs.Comparison
{
    /// <summary>
    /// 差异类型枚举
    /// </summary>
    public enum DiffType
    {
        /// <summary>
        /// 新增班次（只在排班表2中存在）
        /// </summary>
        Added,

        /// <summary>
        /// 删除班次（只在排班表1中存在）
        /// </summary>
        Removed,

        /// <summary>
        /// 人员变更（两边都存在但人员不同）
        /// </summary>
        Changed
    }
}
