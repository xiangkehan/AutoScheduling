using System;

namespace AutoScheduling3.Models
{
    /// <summary>
    /// 人员评分状态模型：在排班过程中维护每个人员的实时评分状态，用于软约束评分计算
    /// </summary>
    public class PersonScoreState
    {
        /// <summary>
        /// 人员ID
        /// </summary>
        public int PersonalId { get; set; }

        /// <summary>
        /// 距离上次班次的间隔（时段数）
        /// 用于计算充分休息得分
        /// </summary>
        public int RecentShiftInterval { get; set; }

        /// <summary>
        /// 距离上次休息日班次的间隔（天数）
        /// 用于计算休息日平衡得分
        /// </summary>
        public int RecentHolidayInterval { get; set; }

        /// <summary>
        /// 各时段的间隔数组（12个时段，索引0-11）
        /// 用于计算时段平衡得分
        /// </summary>
        public int[] PeriodIntervals { get; set; }

        /// <summary>
        /// 最后分配的时段序号（-1表示未分配）
        /// </summary>
        public int LastAssignedPeriod { get; set; }

        /// <summary>
        /// 最后分配的日期
        /// </summary>
        public DateTime? LastAssignedDate { get; set; }

        // 替换原有的 PeriodIntervals 长度检查和 Resize 逻辑
        public PersonScoreState(int personalId, int recentShiftInterval = 0, int recentHolidayInterval = 0, int[]? periodIntervals = null)
        {
            PersonalId = personalId;
            RecentShiftInterval = recentShiftInterval;
            RecentHolidayInterval = recentHolidayInterval;
            // 如果 periodIntervals 不为 null 且长度为 12，则直接赋值，否则新建长度为 12 的数组并复制已有内容
            if (periodIntervals != null && periodIntervals.Length == 12)
            {
                PeriodIntervals = periodIntervals;
            }
            else
            {
                PeriodIntervals = new int[12];
                if (periodIntervals != null)
                {
                    Array.Copy(periodIntervals, PeriodIntervals, Math.Min(periodIntervals.Length, 12));
                }
            }
            LastAssignedPeriod = -1;
            LastAssignedDate = null;
        }

        /// <summary>
        /// 计算该人员在指定时段和日期的得分
        /// </summary>
        /// <param name="periodIndex">时段序号（0-11）</param>
        /// <param name="date">日期</param>
        /// <param name="isHoliday">是否为休息日</param>
        /// <returns>总得分</returns>
        public double CalculateScore(int periodIndex, DateTime date, bool isHoliday)
        {
            // 充分休息得分
            double restScore = RecentShiftInterval;

            // 时段平衡得分
            double periodScore = PeriodIntervals[periodIndex];

            // 如果是休息日，加上休息日平衡得分
            double totalScore = restScore + periodScore;
            if (isHoliday)
            {
                double holidayScore = RecentHolidayInterval;
                totalScore += holidayScore;
            }

            return totalScore;
        }

        /// <summary>
        /// 更新状态：在分配后调用
        /// </summary>
        /// <param name="periodIndex">分配的时段序号</param>
        /// <param name="date">分配的日期</param>
        /// <param name="isHoliday">是否为休息日</param>
        public void UpdateAfterAssignment(int periodIndex, DateTime date, bool isHoliday)
        {
            // 重置充分休息间隔
            RecentShiftInterval = 0;

            // 重置当前时段间隔
            PeriodIntervals[periodIndex] = 0;

            // 如果是休息日，重置休息日间隔
            if (isHoliday)
            {
                RecentHolidayInterval = 0;
            }

            // 记录最后分配信息
            LastAssignedPeriod = periodIndex;
            LastAssignedDate = date;
        }

        /// <summary>
        /// 增量更新：在时段推进时调用
        /// </summary>
        public void IncrementIntervals()
        {
            // 充分休息间隔+1
            RecentShiftInterval++;

            // 各时段间隔+1
            for (int i = 0; i < 12; i++)
            {
                PeriodIntervals[i]++;
            }
        }

        /// <summary>
        /// 增量更新休息日间隔：在休息日推进时调用
        /// </summary>
        public void IncrementHolidayInterval()
        {
            RecentHolidayInterval++;
        }

        public override string ToString()
        {
            return $"PersonScoreState[{PersonalId}] Rest={RecentShiftInterval} Holiday={RecentHolidayInterval} LastPeriod={LastAssignedPeriod}";
        }
    }
}
