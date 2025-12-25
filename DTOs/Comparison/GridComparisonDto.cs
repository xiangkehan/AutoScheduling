using System;
using System.Collections.Generic;

namespace AutoScheduling3.DTOs.Comparison
{
    /// <summary>
    /// 网格对比数据 - 用于并排显示两个排班表
    /// </summary>
    public class GridComparisonDto
    {
        /// <summary>
        /// 日期列表（重叠范围内的所有日期）
        /// </summary>
        public List<DateTime> Dates { get; set; } = new();

        /// <summary>
        /// 哨位列表（两个排班表的并集）
        /// </summary>
        public List<GridPositionDto> Positions { get; set; } = new();

        /// <summary>
        /// 网格行数据（按日期+时段组织）
        /// </summary>
        public List<GridRowDto> Rows { get; set; } = new();
    }

    /// <summary>
    /// 网格哨位信息
    /// </summary>
    public class GridPositionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// 网格行数据（一个日期+时段的所有哨位分配）
    /// </summary>
    public class GridRowDto
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 时段索引 (0-11)
        /// </summary>
        public int PeriodIndex { get; set; }

        /// <summary>
        /// 时段显示文本
        /// </summary>
        public string TimeRange { get; set; } = string.Empty;

        /// <summary>
        /// 行标签（日期+时段）
        /// </summary>
        public string RowLabel => $"{Date:MM/dd} {TimeRange}";

        /// <summary>
        /// 各哨位的单元格数据（按哨位ID索引）
        /// </summary>
        public Dictionary<int, GridCellDto> Cells { get; set; } = new();
    }

    /// <summary>
    /// 网格单元格数据
    /// </summary>
    public class GridCellDto
    {
        /// <summary>
        /// 哨位ID
        /// </summary>
        public int PositionId { get; set; }

        /// <summary>
        /// 排班表1的人员名称（null表示未分配）
        /// </summary>
        public string? PersonnelName1 { get; set; }

        /// <summary>
        /// 排班表2的人员名称（null表示未分配）
        /// </summary>
        public string? PersonnelName2 { get; set; }

        /// <summary>
        /// 差异类型（null表示无差异）
        /// </summary>
        public DiffType? DiffType { get; set; }

        /// <summary>
        /// 是否有差异
        /// </summary>
        public bool HasDiff => DiffType.HasValue;

        /// <summary>
        /// 显示文本1（用于UI）
        /// </summary>
        public string DisplayText1 => PersonnelName1 ?? "-";

        /// <summary>
        /// 显示文本2（用于UI）
        /// </summary>
        public string DisplayText2 => PersonnelName2 ?? "-";
    }
}
