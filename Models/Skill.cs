using System;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.Models
{
    /// <summary>
    /// 技能数据模型：表示一个技能的基本信息，用于通过 SQLite 存储。
    /// 包含：数据库ID、技能名称、技能描述。
    /// 需求: 2.2
    /// </summary>
    public class Skill
    {
        /// <summary>
        /// 数据库主键ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 技能名称
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 技能描述
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否激活/可用
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

        public override string ToString()
        {
            return $"[{Id}] {Name}";
        }
    }
}
