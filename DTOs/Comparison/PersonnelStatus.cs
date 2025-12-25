namespace AutoScheduling3.DTOs.Comparison
{
    /// <summary>
    /// 人员状态枚举
    /// </summary>
    public enum PersonnelStatus
    {
        /// <summary>
        /// 两个排班表都有该人员
        /// </summary>
        Unchanged,

        /// <summary>
        /// 只在排班表2中出现（新增人员）
        /// </summary>
        Added,

        /// <summary>
        /// 只在排班表1中出现（移除人员）
        /// </summary>
        Removed
    }
}
