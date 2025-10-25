using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.Models
{
    /// <summary>
    /// 人员数据模型：表示一个人员的基本信息，用于通过 SQLite 存储。
    /// 包含：数据库ID、人员名称、人员职位、人员技能（技能数据id集合）、人员可用性、是否退役、最近班次间隔数、最近节假日班次间隔数、最近某一时段班次间隔数（12个时段）
    /// </summary>
    public class Personal
    {
        /// <summary>
        /// 数据库主键ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 人员名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 人员职位ID（可用于关联到 PositionLocation.Id）
        /// </summary>
        public int PositionId { get; set; }

        /// <summary>
        /// 人员拥有的技能ID集合（对应 Skill.Id）
        /// </summary>
        public List<int> SkillIds { get; set; } = new List<int>();

        /// <summary>
        /// 人员是否可用
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// 人员是否退役/离职
        /// </summary>
        public bool IsRetired { get; set; } = false;

        /// <summary>
        /// 最近班次间隔数（整数）
        /// </summary>
        public int RecentShiftIntervalCount { get; set; } = 0;

        /// <summary>
        /// 最近节假日班次间隔数（整数）
        /// </summary>
        public int RecentHolidayShiftIntervalCount { get; set; } = 0;

        /// <summary>
        /// 最近某一时段班次间隔数，对应12个时段（索引0-11）
        /// 使用数组保证长度为12，默认值为0。
        /// </summary>
        public int[] RecentPeriodShiftIntervals { get; set; } = new int[12];

        public override string ToString()
        {
            return $"[{Id}] {Name} (PositionId={PositionId})";
        }
    }
}
