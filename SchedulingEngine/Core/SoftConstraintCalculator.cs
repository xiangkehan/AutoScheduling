using System;
using System.Collections.Generic;
using System.Linq;
using AutoScheduling3.Models;

namespace AutoScheduling3.SchedulingEngine.Core
{
    /// <summary>
    /// 软约束评分计算器 - 对应需求6.1-6.4
    /// 计算充分休息、休息日平衡、时段平衡得分，实现评分算法和权重配置
    /// </summary>
    public class SoftConstraintCalculator
    {
        private readonly SchedulingContext _context;

        /// <summary>
        /// 充分休息得分权重 - 对应需求6.1
        /// </summary>
        public double RestWeight { get; set; } = 1.0;

        /// <summary>
        /// 休息日平衡得分权重 - 对应需求6.2
        /// </summary>
        public double HolidayBalanceWeight { get; set; } = 1.5;

        /// <summary>
        /// 时段平衡得分权重 - 对应需求6.3
        /// </summary>
        public double TimeSlotBalanceWeight { get; set; } = 1.0;

        /// <summary>
        /// 最大休息间隔天数（用于归一化）
        /// </summary>
        public int MaxRestDays { get; set; } = 7;

        /// <summary>
        /// 最大休息日间隔天数（用于归一化）
        /// </summary>
        public int MaxHolidayDays { get; set; } = 30;

        /// <summary>
        /// 最大时段间隔天数（用于归一化）
        /// </summary>
        public int MaxTimeSlotDays { get; set; } = 14;

        public SoftConstraintCalculator(SchedulingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 计算充分休息得分 - 对应需求6.1
        /// 基于人员距离上次分配的时段间隔
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="timeSlot">时段索引</param>
        /// <param name="date">当前日期</param>
        /// <returns>充分休息得分（0-1之间，越高越优先）</returns>
        public double CalculateRestScore(int personIdx, int timeSlot, DateTime date)
        {
            int personId = _context.PersonIdxToId[personIdx];
            if (!_context.PersonScoreStates.TryGetValue(personId, out var scoreState))
                return 0.5; // 默认中等分

            // 动态计算到最近班次的间隔
            int interval = scoreState.CalculateRecentShiftInterval(date, timeSlot, _context);

            // 如果从未分配过，给予较高分数
            if (interval == int.MaxValue)
                return 0.8;

            // 基于间隔计算得分
            double intervalDays = interval / 12.0; // 12个时段 = 1天
            double normalizedScore = Math.Min(intervalDays / MaxRestDays, 1.0);

            return normalizedScore;
        }

        /// <summary>
        /// 计算休息日平衡得分 - 对应需求6.2
        /// 处理休息日排班时的平衡得分
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="date">当前日期</param>
        /// <returns>休息日平衡得分（0-1之间，越高越优先）</returns>
        public double CalculateHolidayBalanceScore(int personIdx, DateTime date)
        {
            // 如果不是休息日，返回中等分数
            if (!_context.IsHoliday(date))
                return 0.5;

            int personId = _context.PersonIdxToId[personIdx];
            if (!_context.PersonScoreStates.TryGetValue(personId, out var scoreState))
                return 0.5;

            // 动态计算到最近休息日班次的间隔
            int intervalDays = scoreState.CalculateHolidayInterval(date, _context);

            // 如果从未在休息日分配过，给予较高分数
            if (intervalDays == int.MaxValue)
                return 0.8;

            // 基于休息日间隔计算得分
            double normalizedScore = Math.Min((double)intervalDays / MaxHolidayDays, 1.0);

            return normalizedScore;
        }

        /// <summary>
        /// 计算时段平衡得分 - 对应需求6.3
        /// 基于人员在12个时段的分配历史
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="timeSlot">时段索引（0-11）</param>
        /// <param name="date">当前日期</param>
        /// <returns>时段平衡得分（0-1之间，越高越优先）</returns>
        public double CalculateTimeSlotBalanceScore(int personIdx, int timeSlot, DateTime date)
        {
            if (timeSlot < 0 || timeSlot > 11)
                return 0;

            int personId = _context.PersonIdxToId[personIdx];
            if (!_context.PersonScoreStates.TryGetValue(personId, out var scoreState))
                return 0.5;

            // 动态计算到最近同时段班次的间隔
            int interval = scoreState.CalculatePeriodInterval(timeSlot, date, _context);

            // 如果从未在该时段分配过，给予较高分数
            if (interval == int.MaxValue)
                return 0.8;

            // 基于该时段的间隔计算得分
            double intervalDays = interval / 12.0; // 转换为天数
            double normalizedScore = Math.Min(intervalDays / MaxTimeSlotDays, 1.0);

            return normalizedScore;
        }

        /// <summary>
        /// 计算综合得分 - 对应需求6.4
        /// 根据软约束得分选择最优人员分配
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="timeSlot">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>综合得分（越高越优先）</returns>
        public double CalculateTotalScore(int personIdx, int timeSlot, DateTime date)
        {
            double restScore = CalculateRestScore(personIdx, timeSlot, date);
            double holidayScore = CalculateHolidayBalanceScore(personIdx, date);
            double timeSlotScore = CalculateTimeSlotBalanceScore(personIdx, timeSlot, date);

            // 加权计算总分
            double totalScore = (restScore * RestWeight) + 
                               (holidayScore * HolidayBalanceWeight) + 
                               (timeSlotScore * TimeSlotBalanceWeight);

            return totalScore;
        }

        /// <summary>
        /// 为可行人员列表计算并排序得分
        /// </summary>
        /// <param name="feasiblePersons">可行人员索引数组</param>
        /// <param name="timeSlot">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>按得分降序排列的(人员索引, 得分)列表</returns>
        public List<(int PersonIdx, double Score)> CalculateAndRankScores(
            int[] feasiblePersons, int timeSlot, DateTime date)
        {
            var scores = new List<(int PersonIdx, double Score)>();

            foreach (var personIdx in feasiblePersons)
            {
                double score = CalculateTotalScore(personIdx, timeSlot, date);
                scores.Add((personIdx, score));
            }

            // 按得分降序排序，得分相同时按人员索引升序
            scores.Sort((a, b) => 
            {
                int scoreComparison = b.Score.CompareTo(a.Score);
                return scoreComparison != 0 ? scoreComparison : a.PersonIdx.CompareTo(b.PersonIdx);
            });

            return scores;
        }

        /// <summary>
        /// 选择得分最高的人员
        /// </summary>
        /// <param name="feasiblePersons">可行人员索引数组</param>
        /// <param name="timeSlot">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>得分最高的人员索引，如果无可行人员则返回-1</returns>
        public int SelectBestPerson(int[] feasiblePersons, int timeSlot, DateTime date)
        {
            if (feasiblePersons == null || feasiblePersons.Length == 0)
                return -1;

            var rankedScores = CalculateAndRankScores(feasiblePersons, timeSlot, date);
            return rankedScores.Count > 0 ? rankedScores[0].PersonIdx : -1;
        }

        /// <summary>
        /// 获取人员在特定时段和日期的得分详情
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="timeSlot">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>得分详情字符串</returns>
        public string GetScoreDetails(int personIdx, int timeSlot, DateTime date)
        {
            double restScore = CalculateRestScore(personIdx, timeSlot, date);
            double holidayScore = CalculateHolidayBalanceScore(personIdx, date);
            double timeSlotScore = CalculateTimeSlotBalanceScore(personIdx, timeSlot, date);
            double totalScore = CalculateTotalScore(personIdx, timeSlot, date);

            int personId = _context.PersonIdxToId[personIdx];
            string personName = _context.Personals[personIdx].Name;

            return $"人员{personName}(ID:{personId}) 时段{timeSlot} {date:yyyy-MM-dd}\n" +
                   $"  充分休息得分: {restScore:F3} (权重: {RestWeight})\n" +
                   $"  休息日平衡得分: {holidayScore:F3} (权重: {HolidayBalanceWeight})\n" +
                   $"  时段平衡得分: {timeSlotScore:F3} (权重: {TimeSlotBalanceWeight})\n" +
                   $"  综合得分: {totalScore:F3}";
        }

        /// <summary>
        /// 批量计算多个人员的得分详情
        /// </summary>
        /// <param name="feasiblePersons">可行人员索引数组</param>
        /// <param name="timeSlot">时段索引</param>
        /// <param name="date">日期</param>
        /// <returns>得分详情列表</returns>
        public List<string> GetBatchScoreDetails(int[] feasiblePersons, int timeSlot, DateTime date)
        {
            var details = new List<string>();
            var rankedScores = CalculateAndRankScores(feasiblePersons, timeSlot, date);

            for (int i = 0; i < rankedScores.Count; i++)
            {
                var (personIdx, score) = rankedScores[i];
                string detail = $"排名{i + 1}: {GetScoreDetails(personIdx, timeSlot, date)}";
                details.Add(detail);
            }

            return details;
        }

        /// <summary>
        /// 更新权重配置
        /// </summary>
        /// <param name="restWeight">充分休息权重</param>
        /// <param name="holidayWeight">休息日平衡权重</param>
        /// <param name="timeSlotWeight">时段平衡权重</param>
        public void UpdateWeights(double restWeight, double holidayWeight, double timeSlotWeight)
        {
            RestWeight = Math.Max(0, restWeight);
            HolidayBalanceWeight = Math.Max(0, holidayWeight);
            TimeSlotBalanceWeight = Math.Max(0, timeSlotWeight);
        }

        /// <summary>
        /// 更新归一化参数
        /// </summary>
        /// <param name="maxRestDays">最大休息间隔天数</param>
        /// <param name="maxHolidayDays">最大休息日间隔天数</param>
        /// <param name="maxTimeSlotDays">最大时段间隔天数</param>
        public void UpdateNormalizationParameters(int maxRestDays, int maxHolidayDays, int maxTimeSlotDays)
        {
            MaxRestDays = Math.Max(1, maxRestDays);
            MaxHolidayDays = Math.Max(1, maxHolidayDays);
            MaxTimeSlotDays = Math.Max(1, maxTimeSlotDays);
        }

        /// <summary>
        /// 获取当前配置摘要
        /// </summary>
        /// <returns>配置摘要字符串</returns>
        public string GetConfigurationSummary()
        {
            return $"软约束评分配置:\n" +
                   $"  充分休息权重: {RestWeight} (最大间隔: {MaxRestDays}天)\n" +
                   $"  休息日平衡权重: {HolidayBalanceWeight} (最大间隔: {MaxHolidayDays}天)\n" +
                   $"  时段平衡权重: {TimeSlotBalanceWeight} (最大间隔: {MaxTimeSlotDays}天)";
        }
    }
}