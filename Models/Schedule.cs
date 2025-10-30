using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.Models
{
    /// <summary>
    /// 排班表：包含数据库ID、表头（名称/描述）、排班结果（单次排班集合）。
    /// 人员组与哨位列表通过引用集合的方式存储：人员ID集合、哨位ID集合。
    /// </summary>
    public class Schedule
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 表头（名称或描述）
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 人员ID集合
        /// </summary>
        public List<int> PersonalIds { get; set; } = new();

        /// <summary>
        /// 哨位ID集合
        /// </summary>
        public List<int> PositionIds { get; set; } = new();

        /// <summary>
        /// 排班结果（单次排班集合）
        /// </summary>
        public List<SingleShift> Shifts { get; set; } = new();

        /// <summary>
        /// 排班开始日期（日期部分，UTC）
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 排班结束日期（日期部分，UTC）
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// 创建时间（UTC）
        /// </summary>
        public DateTime CreatedAt { get; set; }

        public override string ToString() => $"Schedule[{Id}] {Title} Shifts={Shifts.Count}";
    }
}
