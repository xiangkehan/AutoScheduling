using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.Models
{
    /// <summary>
    /// 技能数据模型：表示一个技能的基本信息，用于通过 SQLite 存储。
    /// 包含：数据库ID、技能名称、技能描述。
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
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 技能描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否激活/可用
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return $"[{Id}] {Name}";
        }
    }
}
