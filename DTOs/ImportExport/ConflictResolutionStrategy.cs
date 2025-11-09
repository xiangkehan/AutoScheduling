namespace AutoScheduling3.DTOs.ImportExport
{
    /// <summary>
    /// 冲突解决策略枚举
    /// </summary>
    public enum ConflictResolutionStrategy
    {
        /// <summary>
        /// 覆盖现有数据
        /// </summary>
        Replace,

        /// <summary>
        /// 跳过冲突数据
        /// </summary>
        Skip,

        /// <summary>
        /// 合并数据（保留现有，添加新数据）
        /// </summary>
        Merge
    }
}
