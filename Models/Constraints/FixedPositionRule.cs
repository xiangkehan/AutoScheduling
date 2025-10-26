using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.Models.Constraints
{
    /// <summary>
    /// 定岗规则模型：定义某些人员仅能在特定哨位或特定时段上哨
    /// </summary>
    public class FixedPositionRule
    {
        /// <summary>
        /// 数据库主键ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 人员ID
        /// </summary>
        public int PersonalId { get; set; }

        /// <summary>
        /// 允许的哨位ID列表（空表示不限制哨位）
        /// </summary>
        public List<int> AllowedPositionIds { get; set; } = new List<int>();

        /// <summary>
        /// 允许的时段序号列表（0-11，空表示不限制时段）
        /// </summary>
        public List<int> AllowedPeriods { get; set; } = new List<int>();

        /// <summary>
        /// 规则是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 规则描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"FixedPositionRule[{Id}] PersonId={PersonalId} (Enabled={IsEnabled})";
        }
    }
}
