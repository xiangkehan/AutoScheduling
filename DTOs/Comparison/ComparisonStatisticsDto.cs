using System.Collections.Generic;

namespace AutoScheduling3.DTOs.Comparison
{
    /// <summary>
    /// 对比统计数据传输对象
    /// </summary>
    public class ComparisonStatisticsDto
    {
        /// <summary>
        /// 排班表1的总班次数
        /// </summary>
        public int TotalShifts1 { get; set; }

        /// <summary>
        /// 排班表2的总班次数
        /// </summary>
        public int TotalShifts2 { get; set; }

        /// <summary>
        /// 排班表1的人均班次数
        /// </summary>
        public double AverageShiftsPerPerson1 { get; set; }

        /// <summary>
        /// 排班表2的人均班次数
        /// </summary>
        public double AverageShiftsPerPerson2 { get; set; }

        /// <summary>
        /// 排班表1的夜哨班次数
        /// </summary>
        public int NightShifts1 { get; set; }

        /// <summary>
        /// 排班表2的夜哨班次数
        /// </summary>
        public int NightShifts2 { get; set; }

        /// <summary>
        /// 排班表1的休息日班次数
        /// </summary>
        public int HolidayShifts1 { get; set; }

        /// <summary>
        /// 排班表2的休息日班次数
        /// </summary>
        public int HolidayShifts2 { get; set; }

        /// <summary>
        /// 新增班次数量
        /// </summary>
        public int AddedCount { get; set; }

        /// <summary>
        /// 删除班次数量
        /// </summary>
        public int RemovedCount { get; set; }

        /// <summary>
        /// 变更班次数量
        /// </summary>
        public int ChangedCount { get; set; }

        /// <summary>
        /// 排班表1的哨位覆盖率（哨位ID -> 覆盖率）
        /// </summary>
        public Dictionary<int, double> PositionCoverage1 { get; set; } = new();

        /// <summary>
        /// 排班表2的哨位覆盖率（哨位ID -> 覆盖率）
        /// </summary>
        public Dictionary<int, double> PositionCoverage2 { get; set; } = new();
    }
}
