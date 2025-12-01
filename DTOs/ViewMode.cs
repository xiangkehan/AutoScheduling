namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 排班结果页面的视图模式
    /// </summary>
    public enum ViewMode
    {
        /// <summary>
        /// 网格视图 - 以表格形式显示所有哨位和时段
        /// </summary>
        Grid,

        /// <summary>
        /// 列表视图 - 以列表形式显示所有班次
        /// </summary>
        List,

        /// <summary>
        /// 人员视图 - 按人员查看排班情况
        /// </summary>
        ByPersonnel,

        /// <summary>
        /// 哨位视图 - 按哨位查看排班情况（按周显示）
        /// </summary>
        ByPosition
    }
}
