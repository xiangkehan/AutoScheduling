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

            return scoreState.CalculateScore(periodIdx, date, isHoliday);
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
        /// 更新人员评分状态（在分配后调用）
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="periodIdx">时段索引</param>
        /// <param name="date">日期</param>
        public void UpdatePersonScoreState(int personIdx, int periodIdx, DateTime date)
        {
            int personId = _context.PersonIdxToId[personIdx];
            if (!_context.PersonScoreStates.ContainsKey(personId))
                return;

            var scoreState = _context.PersonScoreStates[personId];
            bool isHoliday = _context.IsHoliday(date);
            
            scoreState.UpdateAfterAssignment(periodIdx, date, isHoliday);
        }

        /// <summary>
        /// 增量更新所有人员的间隔数（在时段推进时调用）
        /// </summary>
        public void IncrementAllIntervalsForPeriod()
        {
            foreach (var state in _context.PersonScoreStates.Values)
            {
                state.IncrementIntervals();
            }
        }

        /// <summary>
        /// 增量更新所有人员的休息日间隔数（在休息日推进时调用）
        /// </summary>
        /// <param name="date">当前日期</param>
        public void IncrementHolidayIntervalsIfNeeded(DateTime date)
        {
            if (_context.IsHoliday(date))
            {
                foreach (var state in _context.PersonScoreStates.Values)
                {
                    state.IncrementHolidayInterval();
                }
            }
        }

        /// <summary>
        /// 获取人员的当前评分状态摘要
        /// </summary>
        public string GetPersonScoreSummary(int personIdx)
        {
            int personId = _context.PersonIdxToId[personIdx];
            if (!_context.PersonScoreStates.ContainsKey(personId))
                return "未找到评分状态";

            var state = _context.PersonScoreStates[personId];
            return $"人员{personId}: 休息间隔={state.RecentShiftInterval}, 休息日间隔={state.RecentHolidayInterval}, " +
                   $"最后分配时段={state.LastAssignedPeriod}, 最后分配日期={state.LastAssignedDate:yyyy-MM-dd}";
        }
    }
}
