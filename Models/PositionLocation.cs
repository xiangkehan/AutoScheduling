using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.Models
{
    /// <summary>
    /// 哨位数据模型：表示一个哨位的基本信息，用于通过 SQLite 存储。
    /// 包含：数据库ID、哨位名称、哨位地点、哨位介绍、哨位要求、哨位所需技能ID集合。
    /// </summary>
    public class PositionLocation
    {
        /// <summary>
        /// 数据库主键ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 哨位名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 哨位地点
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 哨位介绍
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 哨位要求（文本描述，用于显示）
        /// </summary>
        public string Requirements { get; set; } = string.Empty;

        /// <summary>
        /// 哨位所需技能ID集合（用于算法快速匹配）
        /// </summary>
        public List<int> RequiredSkillIds { get; set; } = new List<int>();

        public override string ToString()
        {
            return $"[{Id}] {Name} @ {Location} - {Description}";
        }
    }
}
