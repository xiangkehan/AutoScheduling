using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.Models;
using AutoScheduling3.SchedulingEngine.Core;
using AutoScheduling3.SchedulingEngine.Strategies;
using AutoScheduling3.Models.Constraints; // manual assignments

namespace AutoScheduling3.SchedulingEngine
{
    /// <summary>
    /// 贪心调度器：基于MRV启发式策略的排班算法核心（增强版）
    /// </summary>
    public class GreedyScheduler
    {
        private readonly SchedulingContext _context;
        private FeasibilityTensor? _tensor;
        private MRVStrategy? _mrvStrategy;
        private ScoreCalculator? _scoreCalculator;

        public GreedyScheduler(SchedulingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 执行排班算法（支持取消）
        /// </summary>
        public async Task<Schedule> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await PreprocessAsync();
            cancellationToken.ThrowIfCancellationRequested();
            // 多天循环处理：每天重建张量与策略，保持跨日评分状态累积
            var currentDate = _context.StartDate.Date;
            int totalDays = (_context.EndDate.Date - _context.StartDate.Date).Days + 1;

            for (int day = 0; day < totalDays; day++)
            {
                var date = currentDate.AddDays(day);
                cancellationToken.ThrowIfCancellationRequested();
                InitializeTensor();
                ApplyInitialConstraints();
                InitializeStrategies();
                //预置手动指定分配
                ApplyManualAssignmentsForDate(date);
                cancellationToken.ThrowIfCancellationRequested();
                PerformAssignmentsWithMRV(date, cancellationToken);
                // 时段与休息日间隔增量：在一天完成后调用一次 Holiday 增量
                _scoreCalculator?.IncrementHolidayIntervalsIfNeeded(date);
            }

            return GenerateSchedule();
        }

        /// <summary>
        /// 预处理阶段：初始化映射和状态
        /// </summary>
        private Task PreprocessAsync()
        {
            _context.InitializeMappings();
            _context.InitializePersonScoreStates();
            _context.InitializeAssignments();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 初始化可行性张量
        /// </summary>
        private void InitializeTensor()
        {
            _tensor = new FeasibilityTensor(_context.Positions.Count, 12, _context.Personals.Count);
        }

        /// <summary>
        /// 应用初始约束
        /// </summary>
        private void ApplyInitialConstraints()
        {
            if (_tensor == null) return;

            // 人员可用性
            for (int z = 0; z < _context.Personals.Count; z++)
            {
                var person = _context.Personals[z];
                if (!person.IsAvailable || person.IsRetired)
                    _tensor.SetPersonInfeasible(z);
            }

            // 技能匹配
            for (int x = 0; x < _context.Positions.Count; x++)
            {
                var position = _context.Positions[x];
                var requiredSkills = position.RequiredSkillIds;
                for (int z = 0; z < _context.Personals.Count; z++)
                {
                    var person = _context.Personals[z];
                    bool hasAllSkills = requiredSkills.All(skillId => person.SkillIds.Contains(skillId));
                    if (!hasAllSkills)
                        _tensor.SetPersonInfeasibleForPosition(z, x);
                }
            }

            // 定岗规则
            foreach (var rule in _context.FixedPositionRules.Where(r => r.IsEnabled))
            {
                if (!_context.PersonIdToIdx.ContainsKey(rule.PersonalId)) continue;
                int personIdx = _context.PersonIdToIdx[rule.PersonalId];
                if (rule.AllowedPositionIds.Count > 0)
                {
                    for (int x = 0; x < _context.Positions.Count; x++)
                    {
                        int posId = _context.PositionIdxToId[x];
                        if (!rule.AllowedPositionIds.Contains(posId))
                            _tensor.SetPersonInfeasibleForPosition(personIdx, x);
                    }
                }
                if (rule.AllowedPeriods.Count > 0)
                {
                    for (int y = 0; y < 12; y++)
                        if (!rule.AllowedPeriods.Contains(y))
                            _tensor.SetPersonInfeasibleForPeriod(personIdx, y);
                }
            }
        }

        /// <summary>
        /// 初始化MRV策略和评分计算器
        /// </summary>
        private void InitializeStrategies()
        {
            if (_tensor == null) return;
            _mrvStrategy = new MRVStrategy(_tensor, _context);
            _scoreCalculator = new ScoreCalculator(_context);
        }

        /// <summary>
        /// 应用当日的手动指定分配，先占位、再锁定张量与策略。
        /// </summary>
        private void ApplyManualAssignmentsForDate(DateTime date)
        {
            if (_tensor == null || _mrvStrategy == null || _scoreCalculator == null) return;
            var todaysManuals = _context.ManualAssignments
            .Where(m => m.IsEnabled && m.Date.Date == date.Date)
            .ToList();
            foreach (var manual in todaysManuals)
            {
                // 验证基础索引
                if (!_context.PositionIdToIdx.TryGetValue(manual.PositionId, out int posIdx)) continue;
                if (!_context.PersonIdToIdx.TryGetValue(manual.PersonalId, out int personIdx)) continue;
                int periodIdx = manual.PeriodIndex;
                if (periodIdx < 0 || periodIdx > 11) continue;

                // 冲突检测：是否已分配或该人员该时段已有其他哨位
                int existing = _context.GetAssignment(date, periodIdx, posIdx);
                if (existing >= 0) continue; // 已存在分配
                                             // 检查一人一哨冲突
                for (int x = 0; x < _context.Positions.Count; x++)
                {
                    if (_context.GetAssignment(date, periodIdx, x) == personIdx)
                    {
                        // 冲突：该人员此时段已有其他哨位，跳过手动指定
                        goto SkipManual;
                    }
                }

                // 在张量中如果此 slot 对该人员不可行（技能/可用性等），跳过
                var feasiblePersons = _tensor.GetFeasiblePersons(posIdx, periodIdx);
                if (!feasiblePersons.Contains(personIdx)) goto SkipManual;

                // 执行分配并更新约束
                AssignPersonEnhanced(posIdx, periodIdx, personIdx, date, isManual: true);
                continue;
            SkipManual:
                ;
            }
        }

        private void PerformAssignmentsWithMRV(DateTime date, CancellationToken cancellationToken)
        {
            if (_tensor == null || _mrvStrategy == null || _scoreCalculator == null) return;
            // 每天处理12个时段 × N个哨位
            // 策略根据 _mrvStrategy 的已分配标记决定结束
            int maxSlots = _tensor.PositionCount * 12;
            for (int iteration = 0; iteration < maxSlots; iteration++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (posIdx, periodIdx) = _mrvStrategy.SelectNextSlot();
                if (posIdx == -1 || periodIdx == -1)
                {
                    break; // 当天结束
                }

                // 时段推进前，更新休息间隔（仅在进入新时段前一次）
                _scoreCalculator.IncrementAllIntervalsForPeriod();

                var feasiblePersons = _tensor.GetFeasiblePersons(posIdx, periodIdx);
                if (feasiblePersons.Length == 0)
                {
                    _mrvStrategy.MarkAsAssigned(posIdx, periodIdx);
                    continue;
                }
                int bestPersonIdx = _scoreCalculator.SelectBestPerson(feasiblePersons, periodIdx, date);
                if (bestPersonIdx >= 0)
                {
                    AssignPersonEnhanced(posIdx, periodIdx, bestPersonIdx, date);
                }
            }
        }

        /// <summary>
        /// 执行分配并更新约束（增强版）
        /// </summary>
        private void AssignPersonEnhanced(int positionIdx, int periodIdx, int personIdx, DateTime date, bool isManual = false)
        {
            if (_tensor == null || _mrvStrategy == null || _scoreCalculator == null) return;
            _context.RecordAssignment(date, periodIdx, positionIdx, personIdx);
            _mrvStrategy.MarkAsAssigned(positionIdx, periodIdx);
            _tensor.SetOthersInfeasibleForSlot(positionIdx, periodIdx, personIdx);
            _tensor.SetOtherPositionsInfeasibleForPersonPeriod(personIdx, periodIdx, positionIdx);
            // 时段不连续：相邻时段不能连续上哨（仅自动分配时应用，手动指定可覆盖）
            if (!isManual)
            {
                if (periodIdx > 0) _tensor.SetPersonInfeasibleForPeriod(personIdx, periodIdx - 1);
                if (periodIdx < 11) _tensor.SetPersonInfeasibleForPeriod(personIdx, periodIdx + 1);
            }
            // 夜哨唯一：同一晚上只能上一个夜哨
            int[] nightPeriods = { 11, 0, 1, 2 };
            if (nightPeriods.Contains(periodIdx))
            {
                foreach (var np in nightPeriods)
                    if (np != periodIdx) _tensor.SetPersonInfeasibleForPeriod(personIdx, np);
            }
            _mrvStrategy.UpdateCandidateCountsAfterAssignment(positionIdx, periodIdx, personIdx);
            _scoreCalculator.UpdatePersonScoreState(personIdx, periodIdx, date);
        }

        private Schedule GenerateSchedule()
        {
            var schedule = new Schedule
            {
                Header = $"排班表 {_context.StartDate:yyyy-MM-dd} 至 {_context.EndDate:yyyy-MM-dd}",
                PersonnelIds = _context.Personals.Select(p => p.Id).ToList(),
                PositionIds = _context.Positions.Select(p => p.Id).ToList(),
                Results = new List<SingleShift>(),
                StartDate = _context.StartDate.Date,
                EndDate = _context.EndDate.Date,
                CreatedAt = DateTime.UtcNow
            };
            foreach (var kvp in _context.Assignments)
            {
                var date = kvp.Key;
                var assignments = kvp.Value;
                for (int periodIdx = 0; periodIdx < 12; periodIdx++)
                {
                    for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                    {
                        int personIdx = assignments[periodIdx, posIdx];
                        if (personIdx >= 0)
                        {
                            int positionId = _context.PositionIdxToId[posIdx];
                            int personalId = _context.PersonIdxToId[personIdx];
                            var startTime = date.AddHours(periodIdx * 2);
                            var endTime = startTime.AddHours(2);
                            schedule.Results.Add(new SingleShift
                            {
                                PositionId = positionId,
                                PersonalId = personalId,
                                StartTime = DateTime.SpecifyKind(startTime, DateTimeKind.Utc),
                                EndTime = DateTime.SpecifyKind(endTime, DateTimeKind.Utc),
                                ScheduleId = schedule.Id,
                                DayIndex = (date.Date - _context.StartDate.Date).Days
                            });
                        }
                    }
                }
            }
            return schedule;
        }
    }
}
