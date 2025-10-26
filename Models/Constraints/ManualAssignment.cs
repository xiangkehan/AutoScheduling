using System;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.Models.Constraints
{
    /// <summary>
    /// 手动指定分配模型：用户预先指定某些哨位-时段-人员的固定分配
    /// </summary>
    public class ManualAssignment
    {
        /// <summary>
        /// 数据库主键ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 哨位ID
        /// </summary>
        public int PositionId { get; set; }

        /// <summary>
        /// 时段序号（0-11）
        /// </summary>
        public int PeriodIndex { get; set; }

        /// <summary>
        /// 指定人员ID
        /// </summary>
        public int PersonalId { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"ManualAssignment[{Id}] Pos={PositionId} Period={PeriodIndex} Person={PersonalId} Date={Date:yyyy-MM-dd}";
        }
    }
}
