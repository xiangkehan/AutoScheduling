using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.Models
{
    /// <summary>
    /// 排班表数据模型：包含数据库ID、人员组、哨位ID和表头的排班表
    /// 需求: 3.1, 3.2
    /// </summary>
    public class Schedule
    {
        /// <summary>
        /// 数据库主键ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 表头（排班名称或描述）
        /// </summary>
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Header { get; set; } = string.Empty;

        /// <summary>
        /// 人员ID集合
        /// </summary>
        public List<int> PersonnelIds { get; set; } = new List<int>();

        /// <summary>
        /// 哨位ID集合
        /// </summary>
        public List<int> PositionIds { get; set; } = new List<int>();

        /// <summary>
        /// 排班结果（单次排班集合）
        /// </summary>
        public List<SingleShift> Results { get; set; } = new List<SingleShift>();

        /// <summary>
        /// 排班开始日期（日期部分，UTC）
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 排班结束日期（日期部分，UTC）
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// 是否已确认实施
        /// </summary>
        public bool IsConfirmed { get; set; } = false;

        /// <summary>
        /// 创建时间（UTC）
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间（UTC）
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 休息日配置ID（可选）
        /// </summary>
        public int? HolidayConfigId { get; set; }

        /// <summary>
        /// 是否使用活动的休息日配置
        /// </summary>
        public bool UseActiveHolidayConfig { get; set; } = true;

        /// <summary>
        /// 启用的定岗规则ID列表
        /// </summary>
        public List<int> EnabledFixedRuleIds { get; set; } = new List<int>();

        /// <summary>
        /// 启用的手动指定ID列表
        /// </summary>
        public List<int> EnabledManualAssignmentIds { get; set; } = new List<int>();

        /// <summary>
        /// 进度百分比（0-100），用于标识部分完成的排班
        /// </summary>
        public double? ProgressPercentage { get; set; }

        /// <summary>
        /// 当前阶段，用于标识排班执行到哪个阶段
        /// </summary>
        public string? CurrentStage { get; set; }

        /// <summary>
        /// 是否为部分结果（未完成的排班）
        /// </summary>
        public bool IsPartialResult { get; set; }

        /// <summary>
        /// 排班模式（仅贪心或混合模式）
        /// </summary>
        public int SchedulingMode { get; set; } = 0; // 0 = GreedyOnly, 1 = Hybrid

        public override string ToString() => $"Schedule[{Id}] {Header} Results={Results.Count}";
    }
}
