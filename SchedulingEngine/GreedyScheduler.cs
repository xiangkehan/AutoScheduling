using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.Models;
using AutoScheduling3.SchedulingEngine.Core;
using AutoScheduling3.SchedulingEngine.Strategies;

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
            // 第1步：预处理阶段
            await PreprocessAsync();
            cancellationToken.ThrowIfCancellationRequested();

            // 第2步：初始化可行性张量
            InitializeTensor();
            cancellationToken.ThrowIfCancellationRequested();

            // 第3步：应用初始约束
            ApplyInitialConstraints();
            cancellationToken.ThrowIfCancellationRequested();

            // 第4步：初始化MRV策略和评分计算器
            InitializeStrategies();
            cancellationToken.ThrowIfCancellationRequested();

            // 第5步：执行分配循环（MRV策略）
            PerformAssignmentsWithMRV(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 第6步：生成结果
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
        /// 执行分配循环（使用MRV策略 - 增强版）
        /// </summary>
        private void PerformAssignmentsWithMRV(CancellationToken cancellationToken)
        {
            if (_tensor == null || _mrvStrategy == null || _scoreCalculator == null) return;
            var currentDate = _context.StartDate.Date;
            int totalDays = (_context.EndDate.Date - _context.StartDate.Date).Days + 1;

            // 遍历每一天
            for (int day = 0; day < totalDays; day++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var date = currentDate.AddDays(day);
                // 每天处理12个时段 × N个哨位
                for (int slotIdx = 0; slotIdx < _tensor.PositionCount * 12; slotIdx++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // 使用MRV策略选择下一个要分配的位置
                    var (posIdx, periodIdx) = _mrvStrategy.SelectNextSlot();

                    if (posIdx == -1 || periodIdx == -1)
                    {
                        // 当天所有位置已分配或无法分配
                        break;
                    }

                    // 获取可行人员
                    var feasiblePersons = _tensor.GetFeasiblePersons(posIdx, periodIdx);

                    if (feasiblePersons.Length == 0)
                    {
                        // 无可行人员，标记为已分配（跳过）
                        _mrvStrategy.MarkAsAssigned(posIdx, periodIdx);
                        Console.WriteLine($"警告：{date:yyyy-MM-dd} 时段{periodIdx} 哨位{posIdx} 无可行人员");
                        continue;
                    }

                    // 使用评分计算器选择最佳人员
                    int bestPersonIdx = _scoreCalculator.SelectBestPerson(feasiblePersons, periodIdx, date);

                    if (bestPersonIdx >= 0)
                    {
                        // 执行分配
                        AssignPersonEnhanced(posIdx, periodIdx, bestPersonIdx, date);
                    }
                }
            }
        }

        /// <summary>
        /// 执行分配并更新约束（增强版）
        /// </summary>
        private void AssignPersonEnhanced(int positionIdx, int periodIdx, int personIdx, DateTime date)
        {
            if (_tensor == null || _mrvStrategy == null || _scoreCalculator == null) return;
            // 记录分配
            _context.RecordAssignment(date, periodIdx, positionIdx, personIdx);

            // 标记为已分配
            _mrvStrategy.MarkAsAssigned(positionIdx, periodIdx);

            // 更新张量约束
            // 单人上哨：同一哨位时段只能一个人
            _tensor.SetOthersInfeasibleForSlot(positionIdx, periodIdx, personIdx);

            // 一人一哨：同一人员同一时段只能一个哨位
            _tensor.SetOtherPositionsInfeasibleForPersonPeriod(personIdx, periodIdx, positionIdx);

            // 时段不连续：相邻时段不能连续上哨
            if (periodIdx > 0) _tensor.SetPersonInfeasibleForPeriod(personIdx, periodIdx - 1);
            if (periodIdx < 11) _tensor.SetPersonInfeasibleForPeriod(personIdx, periodIdx + 1);

            // 夜哨唯一：同一晚上只能上一个夜哨
            int[] nightPeriods = { 11, 0, 1, 2 };
            if (nightPeriods.Contains(periodIdx))
            {
                foreach (var np in nightPeriods)
                    if (np != periodIdx) _tensor.SetPersonInfeasibleForPeriod(personIdx, np);
            }

            // 更新MRV候选人员数缓存
            _mrvStrategy.UpdateCandidateCountsAfterAssignment(positionIdx, periodIdx, personIdx);

            // 更新人员评分状态
            _scoreCalculator.UpdatePersonScoreState(personIdx, periodIdx, date);
        }

        /// <summary>
        /// 生成排班结果
        /// </summary>
        private Schedule GenerateSchedule()
        {
            var schedule = new Schedule
            {
                Title = $"排班表 {_context.StartDate:yyyy-MM-dd} 至 {_context.EndDate:yyyy-MM-dd}",
                PersonalIds = _context.Personals.Select(p => p.Id).ToList(),
                PositionIds = _context.Positions.Select(p => p.Id).ToList(),
                Shifts = new List<SingleShift>()
            };
            // 遍历所有分配记录，生成SingleShift
            foreach (var kvp in _context.Assignments)
            {
                var date = kvp.Key;
                var assignments = kvp.Value;
                for (int periodIdx = 0; periodIdx < 12; periodIdx++)
                    for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                    {
                        int personIdx = assignments[periodIdx, posIdx];
                        if (personIdx >= 0)
                        {
                            int positionId = _context.PositionIdxToId[posIdx];
                            int personalId = _context.PersonIdxToId[personIdx];
                            var startTime = date.AddHours(periodIdx * 2);
                            var endTime = startTime.AddHours(2);
                            schedule.Shifts.Add(new SingleShift
                            {
                                PositionId = positionId,
                                PersonalId = personalId,
                                StartTime = startTime,
                                EndTime = endTime
                            });
                        }
                    }
            }

            return schedule;
        }
    }
}
