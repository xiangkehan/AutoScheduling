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
        /// 提高权重以增加区分度
        /// </summary>
        public double RestWeight { get; set; } = 3.0;

        /// <summary>
        /// 休息日平衡得分权重 - 对应需求6.2
        /// 提高权重以增加区分度
        /// </summary>
        public double HolidayBalanceWeight { get; set; } = 4.0;

        /// <summary>
        /// 时段平衡得分权重 - 对应需求6.3
        /// 提高权重以增加区分度
        /// </summary>
        public double TimeSlotBalanceWeight { get; set; } = 3.0;

        /// <summary>
        /// 工作量平衡得分权重
        /// 提高权重以增加区分度
        /// </summary>
        public double WorkloadBalanceWeight { get; set; } = 5.0;

        /// <summary>
        /// 连续夜哨惩罚权重
        /// 惩罚连续多天上夜哨的情况
        /// </summary>
        public double ConsecutiveNightShiftPenaltyWeight { get; set; } = 4.0;

        /// <summary>
        /// 哨位多样性得分权重
        /// 鼓励人员轮换不同哨位
        /// </summary>
        public double PositionDiversityWeight { get; set; } = 2.0;

        /// <summary>
        /// 休息日公平性得分权重
        /// 休息日班次在人员间的公平分布
        /// </summary>
        public double HolidayFairnessWeight { get; set; } = 3.0;

        /// <summary>
        /// 日夜比例平衡得分权重
        /// 夜哨/日哨比例接近理想值
        /// </summary>
        public double DayNightRatioWeight { get; set; } = 3.0;

        /// <summary>
        /// 最大连续夜哨天数（用于归一化）
        /// </summary>
        public int MaxConsecutiveNightDays { get; set; } = 3;

        /// <summary>
        /// 理想夜哨比例（夜哨时段占比 4/12 ≈ 0.33）
        /// </summary>
        public double IdealNightShiftRatio { get; set; } = 0.33;

        /// <summary>
        /// 最大休息间隔天数（用于归一化）
        /// 收紧窗口以提高区分度：3天内的休息差异更敏感
        /// </summary>
        public int MaxRestDays { get; set; } = 3;

        /// <summary>
        /// 最大休息日间隔天数（用于归一化）
        /// 收紧窗口以提高区分度：两周内的休息日分配更有意义
        /// </summary>
        public int MaxHolidayDays { get; set; } = 14;

        /// <summary>
        /// 最大时段间隔天数（用于归一化）
        /// 收紧窗口以提高区分度：一周内的时段平衡更重要
        /// </summary>
        public int MaxTimeSlotDays { get; set; } = 7;

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
        /// 计算工作量平衡得分
        /// 基于人员当前工作量与全局平均工作量的对比
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <returns>工作量平衡得分（0-1之间，工作量越少得分越高）</returns>
        public double CalculateWorkloadBalanceScore(int personIdx)
        {
            int personId = _context.PersonIdxToId[personIdx];
            if (!_context.PersonScoreStates.TryGetValue(personId, out var scoreState))
                return 0.5;

            double avgWorkload = _context.AverageWorkload;

            // 如果还没有任何分配，所有人得分相同
            if (avgWorkload < 0.01)
                return 0.5;

            // 计算工作量偏差（负数表示低于平均，正数表示高于平均）
            double deviation = scoreState.WorkloadScore - avgWorkload;

            // 工作量越少，得分越高
            // 使用 sigmoid 函数进行归一化，使得得分在 0-1 之间
            // 斜率调整为 2.0（原 0.5），让工作量差异更敏感
            double normalizedScore = 1.0 / (1.0 + Math.Exp(deviation * 2.0));

            return normalizedScore;
        }

        /// <summary>
        /// 计算连续夜哨惩罚得分
        /// 惩罚连续多天上夜哨的情况，连续天数越多得分越低
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="timeSlot">时段索引</param>
        /// <param name="date">当前日期</param>
        /// <returns>连续夜哨得分（0-1之间，连续天数越少得分越高）</returns>
        public double CalculateConsecutiveNightShiftScore(int personIdx, int timeSlot, DateTime date)
        {
            // 非夜哨时段，返回满分（不惩罚）
            if (!Constants.SchedulingConstants.NightShiftPeriods.Contains(timeSlot))
                return 1.0;

            int personId = _context.PersonIdxToId[personIdx];
            if (!_context.PersonScoreStates.TryGetValue(personId, out var scoreState))
                return 0.5;

            // 计算最近连续夜哨天数
            int consecutiveDays = CalculateConsecutiveNightDays(personId, date);

            // 连续天数越多，得分越低
            // 0天 -> 1.0, 1天 -> 0.67, 2天 -> 0.33, 3天+ -> 0.0
            double normalizedScore = Math.Max(0, 1.0 - (double)consecutiveDays / MaxConsecutiveNightDays);

            return normalizedScore;
        }

        /// <summary>
        /// 计算最近连续夜哨天数
        /// </summary>
        private int CalculateConsecutiveNightDays(int personId, DateTime targetDate)
        {
            if (!_context.PersonAssignmentDetails.TryGetValue(personId, out var details))
                return 0;

            int consecutiveDays = 0;
            var checkDate = targetDate.AddDays(-1);

            // 向前检查连续的夜哨天数
            while (consecutiveDays < MaxConsecutiveNightDays + 1)
            {
                bool hasNightShiftOnDate = false;

                foreach (var kvp in details)
                {
                    var (assignDate, period, _) = kvp.Value;
                    if (assignDate.Date == checkDate.Date && 
                        Constants.SchedulingConstants.NightShiftPeriods.Contains(period))
                    {
                        hasNightShiftOnDate = true;
                        break;
                    }
                }

                if (!hasNightShiftOnDate)
                    break;

                consecutiveDays++;
                checkDate = checkDate.AddDays(-1);
            }

            return consecutiveDays;
        }

        /// <summary>
        /// 计算哨位多样性得分
        /// 鼓励人员轮换不同哨位，避免总在同一哨位
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="positionIdx">目标哨位索引</param>
        /// <returns>哨位多样性得分（0-1之间，该哨位分配越少得分越高）</returns>
        public double CalculatePositionDiversityScore(int personIdx, int positionIdx)
        {
            int personId = _context.PersonIdxToId[personIdx];
            if (!_context.PersonAssignmentDetails.TryGetValue(personId, out var details) ||
                !_context.PersonScoreStates.TryGetValue(personId, out var scoreState))
                return 0.8; // 无历史记录，给予较高分

            // 只统计有效哨位的班次（排除已删除哨位 positionIdx = -1）
            int validAssignments = details.Values.Count(d => d.positionIdx >= 0);
            if (validAssignments == 0)
                return 0.8;

            // 统计该人员在目标哨位的分配次数
            int positionCount = details.Values.Count(d => d.positionIdx == positionIdx);

            // 计算该哨位的分配比例（使用有效班次作为分母）
            double positionRatio = (double)positionCount / validAssignments;

            // 理想比例 = 1 / 哨位总数（均匀分布）
            double idealRatio = 1.0 / Math.Max(1, _context.Positions.Count);

            // 如果比例低于理想值，给高分；高于理想值，给低分
            if (positionRatio <= idealRatio)
                return 1.0;

            // 超出理想比例的部分进行惩罚
            double excessRatio = positionRatio - idealRatio;
            double normalizedScore = Math.Max(0, 1.0 - excessRatio * 3.0); // 放大惩罚

            return normalizedScore;
        }

        /// <summary>
        /// 计算休息日公平性得分
        /// 休息日班次在人员间的公平分布，休息日班次少的人得分高
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="date">当前日期</param>
        /// <returns>休息日公平性得分（0-1之间，休息日班次越少得分越高）</returns>
        public double CalculateHolidayFairnessScore(int personIdx, DateTime date)
        {
            // 非休息日，返回中等分数
            if (!_context.IsHoliday(date))
                return 0.5;

            int personId = _context.PersonIdxToId[personIdx];
            if (!_context.PersonAssignmentDetails.TryGetValue(personId, out var details))
                return 0.8; // 无历史记录，给予较高分

            // 统计该人员的休息日班次数
            int holidayShiftCount = details.Values.Count(d => _context.IsHoliday(d.date));

            // 计算全局平均休息日班次数
            double avgHolidayShifts = CalculateAverageHolidayShifts();

            if (avgHolidayShifts < 0.01)
                return 0.5; // 还没有休息日班次

            // 休息日班次越少，得分越高
            double deviation = holidayShiftCount - avgHolidayShifts;
            double normalizedScore = 1.0 / (1.0 + Math.Exp(deviation * 1.5));

            return normalizedScore;
        }

        /// <summary>
        /// 计算全局平均休息日班次数
        /// </summary>
        private double CalculateAverageHolidayShifts()
        {
            if (_context.PersonAssignmentDetails.Count == 0)
                return 0;

            int totalHolidayShifts = 0;
            foreach (var personDetails in _context.PersonAssignmentDetails.Values)
            {
                totalHolidayShifts += personDetails.Values.Count(d => _context.IsHoliday(d.date));
            }

            return (double)totalHolidayShifts / _context.PersonAssignmentDetails.Count;
        }

        /// <summary>
        /// 计算日夜比例平衡得分
        /// 夜哨/日哨比例接近理想值（约0.33）的人得分高
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="timeSlot">时段索引</param>
        /// <returns>日夜比例平衡得分（0-1之间，比例越接近理想值得分越高）</returns>
        public double CalculateDayNightRatioScore(int personIdx, int timeSlot)
        {
            int personId = _context.PersonIdxToId[personIdx];
            if (!_context.PersonScoreStates.TryGetValue(personId, out var scoreState))
                return 0.5;

            // 分配次数太少时，不做比例判断
            if (scoreState.TotalAssignments < 3)
                return 0.5;

            // 计算当前夜哨比例
            double currentNightRatio = (double)scoreState.NightShiftCount / scoreState.TotalAssignments;

            // 判断当前时段是否为夜哨
            bool isNightSlot = Constants.SchedulingConstants.NightShiftPeriods.Contains(timeSlot);

            // 如果当前夜哨比例低于理想值，分配夜哨得高分
            // 如果当前夜哨比例高于理想值，分配日哨得高分
            double deviation = currentNightRatio - IdealNightShiftRatio;

            if (isNightSlot)
            {
                // 夜哨时段：比例低于理想值时得高分
                // deviation < 0 表示夜哨不足，应该多分配夜哨
                double normalizedScore = 1.0 / (1.0 + Math.Exp(deviation * 6.0));
                return normalizedScore;
            }
            else
            {
                // 日哨时段：比例高于理想值时得高分
                // deviation > 0 表示夜哨过多，应该多分配日哨
                double normalizedScore = 1.0 / (1.0 + Math.Exp(-deviation * 6.0));
                return normalizedScore;
            }
        }

        /// <summary>
        /// 计算综合得分 - 对应需求6.4
        /// 根据软约束得分选择最优人员分配
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="timeSlot">时段索引</param>
        /// <param name="date">日期</param>
        /// <param name="positionIdx">哨位索引（可选，用于哨位多样性计算）</param>
        /// <returns>综合得分（越高越优先）</returns>
        public double CalculateTotalScore(int personIdx, int timeSlot, DateTime date, int positionIdx = -1)
        {
            // 原有指标
            double restScore = CalculateRestScore(personIdx, timeSlot, date);
            double holidayScore = CalculateHolidayBalanceScore(personIdx, date);
            double timeSlotScore = CalculateTimeSlotBalanceScore(personIdx, timeSlot, date);
            double workloadScore = CalculateWorkloadBalanceScore(personIdx);

            // 新增差异化指标
            double consecutiveNightScore = CalculateConsecutiveNightShiftScore(personIdx, timeSlot, date);
            double positionDiversityScore = positionIdx >= 0 
                ? CalculatePositionDiversityScore(personIdx, positionIdx) 
                : 0.5;
            double holidayFairnessScore = CalculateHolidayFairnessScore(personIdx, date);
            double dayNightRatioScore = CalculateDayNightRatioScore(personIdx, timeSlot);

            // 加权计算总分
            double totalScore = (restScore * RestWeight) + 
                               (holidayScore * HolidayBalanceWeight) + 
                               (timeSlotScore * TimeSlotBalanceWeight) +
                               (workloadScore * WorkloadBalanceWeight) +
                               (consecutiveNightScore * ConsecutiveNightShiftPenaltyWeight) +
                               (positionDiversityScore * PositionDiversityWeight) +
                               (holidayFairnessScore * HolidayFairnessWeight) +
                               (dayNightRatioScore * DayNightRatioWeight);

            return totalScore;
        }

        /// <summary>
        /// 为可行人员列表计算并排序得分
        /// </summary>
        /// <param name="feasiblePersons">可行人员索引数组</param>
        /// <param name="timeSlot">时段索引</param>
        /// <param name="date">日期</param>
        /// <param name="positionIdx">哨位索引（可选，用于哨位多样性计算）</param>
        /// <returns>按得分降序排列的(人员索引, 得分)列表</returns>
        public List<(int PersonIdx, double Score)> CalculateAndRankScores(
            int[] feasiblePersons, int timeSlot, DateTime date, int positionIdx = -1)
        {
            var scores = new List<(int PersonIdx, double Score)>();

            foreach (var personIdx in feasiblePersons)
            {
                double score = CalculateTotalScore(personIdx, timeSlot, date, positionIdx);
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
        /// <param name="positionIdx">哨位索引（可选，用于哨位多样性计算）</param>
        /// <returns>得分最高的人员索引，如果无可行人员则返回-1</returns>
        public int SelectBestPerson(int[] feasiblePersons, int timeSlot, DateTime date, int positionIdx = -1)
        {
            if (feasiblePersons == null || feasiblePersons.Length == 0)
                return -1;

            var rankedScores = CalculateAndRankScores(feasiblePersons, timeSlot, date, positionIdx);
            return rankedScores.Count > 0 ? rankedScores[0].PersonIdx : -1;
        }

        /// <summary>
        /// 获取人员在特定时段和日期的得分详情
        /// </summary>
        /// <param name="personIdx">人员索引</param>
        /// <param name="timeSlot">时段索引</param>
        /// <param name="date">日期</param>
        /// <param name="positionIdx">哨位索引（可选）</param>
        /// <returns>得分详情字符串</returns>
        public string GetScoreDetails(int personIdx, int timeSlot, DateTime date, int positionIdx = -1)
        {
            // 原有指标
            double restScore = CalculateRestScore(personIdx, timeSlot, date);
            double holidayScore = CalculateHolidayBalanceScore(personIdx, date);
            double timeSlotScore = CalculateTimeSlotBalanceScore(personIdx, timeSlot, date);
            double workloadScore = CalculateWorkloadBalanceScore(personIdx);

            // 新增指标
            double consecutiveNightScore = CalculateConsecutiveNightShiftScore(personIdx, timeSlot, date);
            double positionDiversityScore = positionIdx >= 0 
                ? CalculatePositionDiversityScore(personIdx, positionIdx) 
                : 0.5;
            double holidayFairnessScore = CalculateHolidayFairnessScore(personIdx, date);
            double dayNightRatioScore = CalculateDayNightRatioScore(personIdx, timeSlot);

            double totalScore = CalculateTotalScore(personIdx, timeSlot, date, positionIdx);

            int personId = _context.PersonIdxToId[personIdx];
            string personName = _context.Personals[personIdx].Name;

            // 获取工作量统计
            string workloadStats = "";
            if (_context.PersonScoreStates.TryGetValue(personId, out var state))
            {
                double nightRatio = state.TotalAssignments > 0 
                    ? (double)state.NightShiftCount / state.TotalAssignments 
                    : 0;
                workloadStats = $" [总:{state.TotalAssignments} 夜:{state.NightShiftCount} 日:{state.DayShiftCount} 夜比:{nightRatio:P0}]";
            }

            return $"人员{personName}(ID:{personId}) 时段{timeSlot} {date:yyyy-MM-dd}{workloadStats}\n" +
                   $"  充分休息得分: {restScore:F3} (权重: {RestWeight})\n" +
                   $"  休息日平衡得分: {holidayScore:F3} (权重: {HolidayBalanceWeight})\n" +
                   $"  时段平衡得分: {timeSlotScore:F3} (权重: {TimeSlotBalanceWeight})\n" +
                   $"  工作量平衡得分: {workloadScore:F3} (权重: {WorkloadBalanceWeight})\n" +
                   $"  连续夜哨得分: {consecutiveNightScore:F3} (权重: {ConsecutiveNightShiftPenaltyWeight})\n" +
                   $"  哨位多样性得分: {positionDiversityScore:F3} (权重: {PositionDiversityWeight})\n" +
                   $"  休息日公平性得分: {holidayFairnessScore:F3} (权重: {HolidayFairnessWeight})\n" +
                   $"  日夜比例得分: {dayNightRatioScore:F3} (权重: {DayNightRatioWeight})\n" +
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
        /// <param name="workloadWeight">工作量平衡权重</param>
        public void UpdateWeights(double restWeight, double holidayWeight, double timeSlotWeight, double workloadWeight = 2.0)
        {
            RestWeight = Math.Max(0, restWeight);
            HolidayBalanceWeight = Math.Max(0, holidayWeight);
            TimeSlotBalanceWeight = Math.Max(0, timeSlotWeight);
            WorkloadBalanceWeight = Math.Max(0, workloadWeight);
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
                   $"  时段平衡权重: {TimeSlotBalanceWeight} (最大间隔: {MaxTimeSlotDays}天)\n" +
                   $"  工作量平衡权重: {WorkloadBalanceWeight}\n" +
                   $"  连续夜哨惩罚权重: {ConsecutiveNightShiftPenaltyWeight} (最大连续: {MaxConsecutiveNightDays}天)\n" +
                   $"  哨位多样性权重: {PositionDiversityWeight}\n" +
                   $"  休息日公平性权重: {HolidayFairnessWeight}\n" +
                   $"  日夜比例平衡权重: {DayNightRatioWeight} (理想夜哨比例: {IdealNightShiftRatio:P0})";
        }
    }
}