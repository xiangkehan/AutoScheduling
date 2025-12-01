using System;
using System.Collections.Generic;
using AutoScheduling3.Models;

namespace AutoScheduling3.SchedulingEngine.Core
{
    /// <summary>
    /// 软约束评分计算器：计算人员在特定时段和日期的综合得分
    /// </summary>
    public class ScoreCalculator
    {
        private readonly SchedulingContext _context;

        /// <summary>
        /// 充分休息得分权重
        /// </summary>
        public double RestWeight { get; set; } = 1.0;

        /// <summary>
        /// 时段平衡得分权重
        /// </summary>
        public double PeriodBalanceWeight { get; set; } = 1.0;

        /// <summary>
        /// 休息日平衡得分权重
        /// </summary>
        public double HolidayBalanceWeight { get; set; } = 1.5;

        public ScoreCalculator(SchedulingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 计算指定人员在特定时段和日期的综合得分
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="periodIdx">时段索引（0-11）</param>
        /// <param name="date">日期</param>
        /// <returns>综合得分（越高越优先）</returns>
        public double CalculateScore(int personIdx, int periodIdx, DateTime date)
        {
            int personId = _context.PersonIdxToId[personIdx];
            if (!_context.PersonScoreStates.ContainsKey(personId))
                return 0;

            var scoreState = _context.PersonScoreStates[personId];
            bool isHoliday = _context.IsHoliday(date);

            // 动态计算各项间隔
            int recentShiftInterval = scoreState.CalculateRecentShiftInterval(date, periodIdx, _context);
            int periodInterval = scoreState.CalculatePeriodInterval(periodIdx, date, _context);

            // 充分休息得分
            double restScore = recentShiftInterval == int.MaxValue ? 1000 : recentShiftInterval;

            // 时段平衡得分
            double periodScore = periodInterval == int.MaxValue ? 1000 : periodInterval;

            // 如果是休息日，加上休息日平衡得分
            double totalScore = restScore + periodScore;
            if (isHoliday)
            {
                int holidayInterval = scoreState.CalculateHolidayInterval(date, _context);
                double holidayScore = holidayInterval == int.MaxValue ? 1000 : holidayInterval;
                totalScore += holidayScore;
            }

            return totalScore;
        }

        /// <summary>
        /// 计算所有可行人员的得分，并返回排序后的列表
        /// </summary>
        /// <param name="feasiblePersons">可行人员索引数组</param>
        /// <param name="periodIdx">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>按得分降序排列的(人员索引, 得分)列表</returns>
        public List<(int PersonIdx, double Score)> CalculateAndRankScores(
            int[] feasiblePersons, int periodIdx, DateTime date)
        {
            var scores = new List<(int PersonIdx, double Score)>();

            foreach (var personIdx in feasiblePersons)
            {
                double score = CalculateScore(personIdx, periodIdx, date);
                scores.Add((personIdx, score));
            }

            // 按得分降序排序
            scores.Sort((a, b) => b.Score.CompareTo(a.Score));

            return scores;
        }

        /// <summary>
        /// 选择得分最高的人员
        /// </summary>
        /// <param name="feasiblePersons">可行人员索引数组</param>
        /// <param name="periodIdx">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>得分最高的人员索引，如果无可行人员则返回-1</returns>
        public int SelectBestPerson(int[] feasiblePersons, int periodIdx, DateTime date)
        {
            if (feasiblePersons == null || feasiblePersons.Length == 0)
                return -1;

            int bestPersonIdx = feasiblePersons[0];
            double bestScore = CalculateScore(bestPersonIdx, periodIdx, date);

            for (int i = 1; i < feasiblePersons.Length; i++)
            {
                double score = CalculateScore(feasiblePersons[i], periodIdx, date);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPersonIdx = feasiblePersons[i];
                }
            }

            return bestPersonIdx;
        }



        /// <summary>
        /// 获取人员的当前评分状态摘要
        /// </summary>
        public string GetPersonScoreSummary(int personIdx, DateTime date, int periodIdx)
        {
            int personId = _context.PersonIdxToId[personIdx];
            if (!_context.PersonScoreStates.ContainsKey(personId))
                return "未找到评分状态";

            var state = _context.PersonScoreStates[personId];
            int recentShiftInterval = state.CalculateRecentShiftInterval(date, periodIdx, _context);
            int periodInterval = state.CalculatePeriodInterval(periodIdx, date, _context);
            int holidayInterval = state.CalculateHolidayInterval(date, _context);

            return $"人员{personId}: 休息间隔={recentShiftInterval}, 时段{periodIdx}间隔={periodInterval}, " +
                   $"休息日间隔={holidayInterval}";
        }
    }
}
