using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.Models;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine
{
    /// <summary>
    /// 贪心调度器：基于MRV启发式策略的排班算法核心
    /// </summary>
    public class GreedyScheduler
    {
        private readonly SchedulingContext _context;
        private FeasibilityTensor? _tensor;

        public GreedyScheduler(SchedulingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 执行排班算法
        /// </summary>
        public async Task<Schedule> ExecuteAsync()
        {
            // 第1步：预处理阶段
            await PreprocessAsync();

            // 第2步：初始化可行性张量
            InitializeTensor();

            // 第3步：应用初始约束
            ApplyInitialConstraints();

            // 第4步：执行分配循环（MRV策略）
            PerformAssignments();

            // 第5步：生成结果
            var schedule = GenerateSchedule();

            return schedule;
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
            int positionCount = _context.Positions.Count;
            int personCount = _context.Personals.Count;
            _tensor = new FeasibilityTensor(positionCount, 12, personCount);
        }

        /// <summary>
        /// 应用初始约束
        /// </summary>
        private void ApplyInitialConstraints()
        {
            if (_tensor == null) return;

            // 应用人员可用性约束
            for (int z = 0; z < _context.Personals.Count; z++)
            {
                var person = _context.Personals[z];
                if (!person.IsAvailable || person.IsRetired)
                {
                    _tensor.SetPersonInfeasible(z);
                }
            }

            // 应用技能匹配约束
            for (int x = 0; x < _context.Positions.Count; x++)
            {
                var position = _context.Positions[x];
                var requiredSkills = position.RequiredSkillIds;

                for (int z = 0; z < _context.Personals.Count; z++)
                {
                    var person = _context.Personals[z];
                    // 检查人员是否拥有所有必需技能
                    bool hasAllSkills = requiredSkills.All(skillId => person.SkillIds.Contains(skillId));
                    if (!hasAllSkills)
                    {
                        _tensor.SetPersonInfeasibleForPosition(z, x);
                    }
                }
            }

            // 应用定岗规则约束
            foreach (var rule in _context.FixedPositionRules.Where(r => r.IsEnabled))
            {
                if (!_context.PersonIdToIdx.ContainsKey(rule.PersonalId))
                    continue;

                int personIdx = _context.PersonIdToIdx[rule.PersonalId];

                // 如果限定了哨位
                if (rule.AllowedPositionIds.Count > 0)
                {
                    for (int x = 0; x < _context.Positions.Count; x++)
                    {
                        int posId = _context.PositionIdxToId[x];
                        if (!rule.AllowedPositionIds.Contains(posId))
                        {
                            _tensor.SetPersonInfeasibleForPosition(personIdx, x);
                        }
                    }
                }

                // 如果限定了时段
                if (rule.AllowedPeriods.Count > 0)
                {
                    for (int y = 0; y < 12; y++)
                    {
                        if (!rule.AllowedPeriods.Contains(y))
                        {
                            _tensor.SetPersonInfeasibleForPeriod(personIdx, y);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 执行分配循环（简化版MRV策略）
        /// </summary>
        private void PerformAssignments()
        {
            if (_tensor == null) return;

            var currentDate = _context.StartDate.Date;
            while (currentDate <= _context.EndDate.Date)
            {
                // 遍历每个时段
                for (int periodIdx = 0; periodIdx < 12; periodIdx++)
                {
                    // 遍历每个哨位
                    for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                    {
                        // 获取可行人员
                        var feasiblePersons = _tensor.GetFeasiblePersons(posIdx, periodIdx);
                        
                        if (feasiblePersons.Length == 0)
                        {
                            // 无解：跳过或记录错误
                            continue;
                        }

                        // 计算每个可行人员的得分
                        int bestPersonIdx = SelectBestPerson(feasiblePersons, periodIdx, currentDate);

                        if (bestPersonIdx >= 0)
                        {
                            // 执行分配
                            AssignPerson(posIdx, periodIdx, bestPersonIdx, currentDate);
                        }
                    }
                }

                currentDate = currentDate.AddDays(1);
            }
        }

        /// <summary>
        /// 选择最佳人员（基于软约束评分）
        /// </summary>
        private int SelectBestPerson(int[] feasiblePersons, int periodIdx, DateTime date)
        {
            if (feasiblePersons.Length == 0) return -1;

            int bestPersonIdx = feasiblePersons[0];
            double bestScore = -1;

            bool isHoliday = _context.IsHoliday(date);

            foreach (var personIdx in feasiblePersons)
            {
                int personId = _context.PersonIdxToId[personIdx];
                if (!_context.PersonScoreStates.ContainsKey(personId))
                    continue;

                var scoreState = _context.PersonScoreStates[personId];
                double score = scoreState.CalculateScore(periodIdx, date, isHoliday);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPersonIdx = personIdx;
                }
            }

            return bestPersonIdx;
        }

        /// <summary>
        /// 执行分配并更新约束
        /// </summary>
        private void AssignPerson(int positionIdx, int periodIdx, int personIdx, DateTime date)
        {
            if (_tensor == null) return;

            // 记录分配
            _context.RecordAssignment(date, periodIdx, positionIdx, personIdx);

            // 更新张量约束
            // 单人上哨：同一哨位时段只能一个人
            _tensor.SetOthersInfeasibleForSlot(positionIdx, periodIdx, personIdx);

            // 一人一哨：同一人员同一时段只能一个哨位
            _tensor.SetOtherPositionsInfeasibleForPersonPeriod(personIdx, periodIdx, positionIdx);

            // 时段不连续：相邻时段不能连续上哨
            if (periodIdx > 0)
            {
                _tensor.SetPersonInfeasibleForPeriod(personIdx, periodIdx - 1);
            }
            if (periodIdx < 11)
            {
                _tensor.SetPersonInfeasibleForPeriod(personIdx, periodIdx + 1);
            }

            // 夜哨唯一：同一晚上只能上一个夜哨
            int[] nightPeriods = { 11, 0, 1, 2 };
            if (nightPeriods.Contains(periodIdx))
            {
                foreach (var np in nightPeriods)
                {
                    if (np != periodIdx)
                    {
                        _tensor.SetPersonInfeasibleForPeriod(personIdx, np);
                    }
                }
            }

            // 更新人员评分状态
            int personId = _context.PersonIdxToId[personIdx];
            if (_context.PersonScoreStates.ContainsKey(personId))
            {
                var scoreState = _context.PersonScoreStates[personId];
                bool isHoliday = _context.IsHoliday(date);
                scoreState.UpdateAfterAssignment(periodIdx, date, isHoliday);
            }
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
            }

            return schedule;
        }
    }
}
