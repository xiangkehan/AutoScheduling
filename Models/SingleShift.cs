using System;
using System.ComponentModel.DataAnnotations;

namespace AutoScheduling3.Models
{
    /// <summary>
    /// 单次排班：包含数据库ID、所属排班表ID、哨位ID、人员ID、班次开始/结束时间。
    /// </summary>
    public class SingleShift
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 所属排班表ID (Schedule.Id)
        /// </summary>
        public int ScheduleId { get; set; }

        /// <summary>
        /// 哨位ID (PositionLocation.Id)
        /// </summary>
        public int PositionId { get; set; }

        /// <summary>
        /// 人员ID (Personal.Id)
        /// </summary>
        public int PersonalId { get; set; }

        /// <summary>
        /// 班次开始时间（UTC）
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 班次结束时间（UTC）
        /// </summary>
        public DateTime EndTime { get; set; }

        public override string ToString() => $"Shift[{Id}] Pos={PositionId} Person={PersonalId} {StartTime:o} -> {EndTime:o}";
    }
}
