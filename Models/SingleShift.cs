using System;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.Models
{
    /// <summary>
    /// 单次排班：包含数据库ID、哨位ID、班次时间和人员ID
    /// 需求: 3.1, 3.2
    /// </summary>
    public class SingleShift
    {
        /// <summary>
        /// 数据库主键ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 所属排班表ID (Schedule.Id)
        /// </summary>
        public int ScheduleId { get; set; }

        /// <summary>
        /// 哨位ID (PositionLocation.Id)
        /// </summary>
        [Required]
        public int PositionId { get; set; }

        /// <summary>
        /// 人员ID (Personal.Id)
        /// </summary>
        [Required]
        public int PersonnelId { get; set; }

        /// <summary>
        /// 班次开始时间（UTC）
        /// </summary>
        [Required]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 班次结束时间（UTC）
        /// </summary>
        [Required]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 从排班开始日期的天数索引 (0-based)
        /// </summary>
        [Range(0, int.MaxValue)]
        public int DayIndex { get; set; }

        /// <summary>
        /// 时段索引 (0-11)
        /// </summary>
        [Range(0, 11)]
        public int TimeSlotIndex { get; set; }

        /// <summary>
        /// 是否为夜哨
        /// </summary>
        public bool IsNightShift { get; set; } = false;

        /// <summary>
        /// 是否为手动分配（用于标记用户手动编辑的班次）
        /// </summary>
        public bool IsManualAssignment { get; set; } = false;

        public override string ToString() => $"Shift[{Id}] Pos={PositionId} Person={PersonnelId} {StartTime:o} -> {EndTime:o}";
    }
}
