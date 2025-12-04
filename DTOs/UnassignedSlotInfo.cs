using System;

namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 未分配时段信息
    /// Information about an unassigned slot
    /// 对应需求: 4.3, 8.3
    /// </summary>
    public class UnassignedSlotInfo
    {
        /// <summary>
        /// 哨位索引
        /// </summary>
        public int PositionIdx { get; set; }

        /// <summary>
        /// 哨位名称
        /// </summary>
        public string PositionName { get; set; } = string.Empty;

        /// <summary>
        /// 时段索引 (0-11)
        /// </summary>
        public int PeriodIdx { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 候选人员数量
        /// </summary>
        public int CandidateCount { get; set; }

        /// <summary>
        /// 失败原因
        /// </summary>
        public string FailureReason { get; set; } = string.Empty;

        /// <summary>
        /// 约束冲突详情
        /// </summary>
        public string? ConstraintDetails { get; set; }

        /// <summary>
        /// 获取时段时间范围描述
        /// </summary>
        public string GetTimeRangeDescription()
        {
            var startHour = PeriodIdx * 2;
            var endHour = (PeriodIdx + 1) * 2;
            return $"{startHour:D2}:00-{endHour:D2}:00";
        }

        public override string ToString()
        {
            return $"哨位: {PositionName}, 时段: {PeriodIdx} ({GetTimeRangeDescription()}), " +
                   $"日期: {Date:yyyy-MM-dd}, 候选人员: {CandidateCount}, 原因: {FailureReason}";
        }
    }
}
