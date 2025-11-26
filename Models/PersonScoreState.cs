using System;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.Models
{
    /// <summary>
    /// 人员评分状态模型：在排班过程中动态计算每个人员的评分状态，用于软约束评分计算
    /// </summary>
    public class PersonScoreState
    {
        /// <summary>
        /// 人员ID
        /// </summary>
        public int PersonalId { get; set; }

        public PersonScoreState(int personalId)
        {
            PersonalId = personalId;
        }

        /// <summary>
        /// 计算到最近班次的间隔（时段数）
        /// </summary>
        /// <param name="targetDate">目标日期</param>
        /// <param name="targetPeriod">目标时段</param>
        /// <param name="context">排班上下文</param>
        /// <returns>间隔时段数，如果从未分配则返回 int.MaxValue</returns>
        public int CalculateRecentShiftInterval(DateTime targetDate, int targetPeriod, SchedulingContext context)
        {
            int targetTimestamp = context.CalculateTimestamp(targetDate, targetPeriod);

            if (!context.PersonAssignmentTimestamps.TryGetValue(PersonalId, out var timestamps) || timestamps.Count == 0)
                return int.MaxValue; // 从未分配过

            // 使用 SortedSet 的 GetViewBetween 找到小于目标时间戳的所有时间戳
            var beforeTarget = timestamps.GetViewBetween(int.MinValue, targetTimestamp - 1);

            if (beforeTarget.Count == 0)
                return int.MaxValue; // 在目标时间之前没有分配

            int nearestTimestamp = beforeTarget.Max;
            return targetTimestamp - nearestTimestamp;
        }

        /// <summary>
        /// 计算到最近同时段班次的间隔（时段数）
        /// </summary>
        /// <param name="targetPeriod">目标时段（0-11）</param>
        /// <param name="targetDate">目标日期</param>
        /// <param name="context">排班上下文</param>
        /// <returns>间隔时段数，如果从未在该时段分配则返回 int.MaxValue</returns>
        public int CalculatePeriodInterval(int targetPeriod, DateTime targetDate, SchedulingContext context)
        {
            int targetTimestamp = context.CalculateTimestamp(targetDate, targetPeriod);

            if (!context.PersonAssignmentTimestamps.TryGetValue(PersonalId, out var timestamps) || timestamps.Count == 0)
                return int.MaxValue;

            // 筛选出同时段的时间戳（时间戳 % 12 == targetPeriod）
            int nearestSamePeriodTimestamp = -1;

            foreach (var ts in timestamps)
            {
                if (ts >= targetTimestamp) break; // 已经超过目标时间

                // 检查是否同时段
                if (ts % 12 == targetPeriod)
                {
                    nearestSamePeriodTimestamp = ts;
                }
            }

            if (nearestSamePeriodTimestamp == -1)
                return int.MaxValue;

            return targetTimestamp - nearestSamePeriodTimestamp;
        }

        /// <summary>
        /// 计算到最近休息日班次的间隔（天数）
        /// </summary>
        /// <param name="targetDate">目标日期</param>
        /// <param name="context">排班上下文</param>
        /// <returns>间隔天数，如果从未在休息日分配则返回 int.MaxValue</returns>
        public int CalculateHolidayInterval(DateTime targetDate, SchedulingContext context)
        {
            if (!context.IsHoliday(targetDate))
                return int.MaxValue; // 不是休息日，返回最大值

            int targetTimestamp = context.CalculateTimestamp(targetDate, 0); // 使用当天0时段作为基准

            if (!context.PersonAssignmentTimestamps.TryGetValue(PersonalId, out var timestamps) ||
                !context.PersonAssignmentDetails.TryGetValue(PersonalId, out var details) ||
                timestamps.Count == 0)
                return int.MaxValue;

            // 找到最近的休息日班次
            int nearestHolidayTimestamp = -1;

            foreach (var ts in timestamps)
            {
                if (ts >= targetTimestamp) break;

                if (details.TryGetValue(ts, out var detail))
                {
                    if (context.IsHoliday(detail.date))
                    {
                        nearestHolidayTimestamp = ts;
                    }
                }
            }

            if (nearestHolidayTimestamp == -1)
                return int.MaxValue;

            // 返回天数间隔
            return (targetTimestamp - nearestHolidayTimestamp) / 12;
        }

        public override string ToString()
        {
            return $"PersonScoreState[{PersonalId}]";
        }
    }
}
