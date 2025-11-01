using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.Models
{
    /// <summary>
    /// 排班模板数据模型：用于保存和重用排班配置
    /// 需求: 3.1, 3.2
    /// </summary>
    public class SchedulingTemplate
    {
        /// <summary>
        /// 数据库主键ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 模板描述
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 模板类型（regular/holiday/special）
        /// </summary>
        [StringLength(50)]
        public string TemplateType { get; set; } = "regular";

        /// <summary>
        /// 是否为默认模板
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// 人员ID集合
        /// </summary>
        public List<int> PersonnelIds { get; set; } = new List<int>();

        /// <summary>
        /// 哨位ID集合
        /// </summary>
        public List<int> PositionIds { get; set; } = new List<int>();

        /// <summary>
        /// 休息日配置ID（可选）
        /// </summary>
        public int? HolidayConfigId { get; set; }

        /// <summary>
        /// 是否使用当前活动配置
        /// </summary>
        public bool UseActiveHolidayConfig { get; set; } = false;

        /// <summary>
        /// 启用的定岗规则ID
        /// </summary>
        public List<int> EnabledFixedRuleIds { get; set; } = new List<int>();

        /// <summary>
        /// 启用的手动指定ID
        /// </summary>
        public List<int> EnabledManualAssignmentIds { get; set; } = new List<int>();

        /// <summary>
        /// 排班天数
        /// </summary>
        [Range(1, 365)]
        public int DurationDays { get; set; } = 1;

        /// <summary>
        /// 排班策略配置（JSON格式）
        /// </summary>
        public string StrategyConfig { get; set; } = string.Empty;

        /// <summary>
        /// 使用次数统计
        /// </summary>
        [Range(0, int.MaxValue)]
        public int UsageCount { get; set; } = 0;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后使用时间
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        public override string ToString()
        {
            return $"Template[{Id}] {Name} (Used={UsageCount})";
        }
    }
}