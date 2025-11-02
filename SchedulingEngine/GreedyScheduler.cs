using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.Models;
using AutoScheduling3.SchedulingEngine.Core;
using AutoScheduling3.SchedulingEngine.Strategies;
using AutoScheduling3.Models.Constraints;

namespace AutoScheduling3.SchedulingEngine
{
    /// <summary>
    /// 贪心调度器：基于MRV启发式策略的排班算法核心 - 对应需求3.1, 3.2, 5.1-5.8, 6.1-6.4, 7.1-7.5
    /// 按照先哨位再时段的顺序进行人员分配，集成硬约束验证和软约束评分
    /// </summary>
    public class GreedyScheduler
    {
        private readonly SchedulingContext _context;
        private FeasibilityTensor? _tensor;
        private MRVStrategy? _mrvStrategy;
        private ScoreCalculator? _scoreCalculator;

        // 新增的约束处理组件 - 对应需求5.1-5.8, 6.1-6.4
        private ConstraintValidator? _constraintValidator;
        private SoftConstraintCalculator? _softConstraintCalculator;

        // 算法配置参数
        private readonly GreedySchedulerConfig _config;

        public GreedyScheduler(SchedulingContext context, GreedySchedulerConfig? config = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _config = config ?? new GreedySchedulerConfig();
        }

        /// <summary>
        /// 执行排班算法（支持取消） - 对应需求3.1, 3.2, 7.5
        /// 按照先哨位再时段的顺序进行人员分配
        /// </summary>
        public async Task<Schedule> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 预处理阶段
            await PreprocessAsync();
            cancellationToken.ThrowIfCancellationRequested();

            // 多天循环处理：每天重建张量与策略，保持跨日评分状态累积
            var currentDate = _context.StartDate.Date;
            int totalDays = (_context.EndDate.Date - _context.StartDate.Date).Days + 1;

            for (int day = 0; day < totalDays; day++)
            {
                var date = currentDate.AddDays(day);
                cancellationToken.ThrowIfCancellationRequested();

                // 初始化当日排班环境
                InitializeDailyScheduling();

                // 应用所有约束条件
                ApplyAllConstraints(date);

                // 预置手动指定分配 - 对应需求5.8
                ApplyManualAssignmentsForDate(date);

                cancellationToken.ThrowIfCancellationRequested();

                // 执行贪心分配算法 - 对应需求7.5
                await PerformGreedyAssignmentsAsync(date, cancellationToken);

                // 更新评分状态
                UpdateDailyScoreStates(date);
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

            // 初始化约束处理组件
            _constraintValidator = new ConstraintValidator(_context);
            _softConstraintCalculator = new SoftConstraintCalculator(_context);

            // 配置软约束权重
            _softConstraintCalculator.UpdateWeights(
                _config.RestWeight,
                _config.HolidayWeight,
                _config.TimeSlotWeight);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 初始化每日排班环境 - 对应需求7.1, 7.2
        /// </summary>
        private void InitializeDailyScheduling()
        {
            // 初始化可行性张量，使用优化操作
            _tensor = new FeasibilityTensor(_context.Positions.Count, 12, _context.Personals.Count,
                _config.UseOptimizedTensor);
            
            // 使用新数据模型初始化可行性张量
            InitializeFeasibilityTensorAsync();
        }

        /// <summary>
        /// 使用新数据模型初始化可行性张量 - 对应需求4.1
        /// 仅将哨位可用人员列表中的人员设为可行
        /// </summary>
        private void InitializeFeasibilityTensorAsync()
        {
            if (_tensor == null) return;

            // 首先将所有人员设为不可行
            for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
            {
                for (int periodIdx = 0; periodIdx < 12; periodIdx++)
                {
                    for (int personIdx = 0; personIdx < _context.Personals.Count; personIdx++)
                    {
                        _tensor[posIdx, periodIdx, personIdx] = false;
                    }
                }
            }

            // 然后仅将哨位可用人员设为可行
            for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
            {
                var position = _context.Positions[posIdx];
                
                foreach (var personnelId in position.AvailablePersonnelIds)
                {
                    if (_context.PersonIdToIdx.TryGetValue(personnelId, out int personIdx))
                    {
                        // 将该人员在此哨位的所有时段设为可行
                        for (int periodIdx = 0; periodIdx < 12; periodIdx++)
                        {
                            _tensor[posIdx, periodIdx, personIdx] = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 应用所有约束条件 - 对应需求5.1-5.8
        /// 使用新数据模型：仅对哨位可用人员进行约束检查
        /// </summary>
        private void ApplyAllConstraints(DateTime date)
        {
            if (_tensor == null || _constraintValidator == null) return;

            // 批量收集约束违反信息
            var constraintViolations = new List<(int positionIdx, int periodIdx, int[] infeasiblePersons)>();

            for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
            {
                var position = _context.Positions[posIdx];
                
                for (int periodIdx = 0; periodIdx < 12; periodIdx++)
                {
                    var infeasiblePersons = new List<int>();

                    // 首先将所有不在可用人员列表中的人员设为不可行
                    for (int personIdx = 0; personIdx < _context.Personals.Count; personIdx++)
                    {
                        int personId = _context.PersonIdxToId[personIdx];
                        
                        // 检查人员是否在哨位的可用人员列表中
                        if (!position.AvailablePersonnelIds.Contains(personId))
                        {
                            infeasiblePersons.Add(personIdx);
                            continue;
                        }

                        // 对可用人员进行约束验证
                        if (!_constraintValidator.ValidateAllConstraints(personIdx, posIdx, periodIdx, date))
                        {
                            infeasiblePersons.Add(personIdx);
                        }
                    }

                    if (infeasiblePersons.Count > 0)
                    {
                        constraintViolations.Add((posIdx, periodIdx, infeasiblePersons.ToArray()));
                    }
                }
            }

            // 批量应用约束 - 对应需求7.4
            if (constraintViolations.Count > 0)
            {
                _tensor.ApplyBatchConstraints(constraintViolations);
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
        /// 应用当日的手动指定分配 - 对应需求5.8
        /// </summary>
        private async void ApplyManualAssignmentsForDate(DateTime date)
        {
            if (_tensor == null || _constraintValidator == null) return;

            var todaysManuals = _context.ManualAssignments
                .Where(m => m.IsEnabled && m.Date.Date == date.Date)
                .OrderBy(m => m.PeriodIndex) // 按时段顺序处理
                .ToList();

            foreach (var manual in todaysManuals)
            {
                // 验证基础索引
                if (!_context.PositionIdToIdx.TryGetValue(manual.PositionId, out int posIdx)) continue;
                if (!_context.PersonIdToIdx.TryGetValue(manual.PersonalId, out int personIdx)) continue;
                int periodIdx = manual.PeriodIndex;
                if (periodIdx < 0 || periodIdx > 11) continue;

                // 使用约束验证器检查手动指定的有效性
                if (!_constraintValidator.ValidateManualAssignment(personIdx, posIdx, periodIdx, date))
                {
                    if (_config.LogConstraintViolations)
                    {
                        var violations = _constraintValidator.GetConstraintViolations(personIdx, posIdx, periodIdx, date);
                        await LogConstraintViolationsAsync(manual, violations);
                    }
                    continue;
                }

                // 检查冲突
                if (_context.GetAssignment(date, periodIdx, posIdx) >= 0)
                    continue; // 已存在分配

                // 检查人员时段唯一性
                if (!_constraintValidator.ValidatePersonTimeSlotUniqueness(personIdx, periodIdx, posIdx, date))
                    continue;

                // 执行手动分配
                await AssignPersonAsync(posIdx, periodIdx, personIdx, date, isManual: true);
            }
        }

        /// <summary>
        /// 更新每日评分状态
        /// </summary>
        private void UpdateDailyScoreStates(DateTime date)
        {
            if (_scoreCalculator == null) return;

            // 时段与休息日间隔增量：在一天完成后调用
            _scoreCalculator.IncrementHolidayIntervalsIfNeeded(date);
        }

        /// <summary>
        /// 记录分配日志
        /// </summary>
        private async Task LogAssignmentAsync(int positionIdx, int periodIdx, int personIdx, DateTime date, bool isManual)
        {
            if (!_config.EnableAssignmentLogging) return;

            var positionName = _context.Positions[positionIdx].Name;
            var personName = _context.Personals[personIdx].Name;
            var assignmentType = isManual ? "手动" : "自动";

            var logMessage = $"{date:yyyy-MM-dd} {periodIdx * 2:D2}:00-{(periodIdx * 2 + 2):D2}:00 " +
                           $"{positionName} -> {personName} ({assignmentType})";

            // 这里可以集成实际的日志系统
            await Task.Run(() => System.Diagnostics.Debug.WriteLine($"[排班分配] {logMessage}"));
        }

        /// <summary>
        /// 记录约束违反日志
        /// </summary>
        private async Task LogConstraintViolationsAsync(ManualAssignment manual, List<string> violations)
        {
            var logMessage = $"手动指定违反约束: {manual} - 违反项: {string.Join(", ", violations)}";
            await Task.Run(() => System.Diagnostics.Debug.WriteLine($"[约束违反] {logMessage}"));
        }

        /// <summary>
        /// 记录未分配位置
        /// </summary>
        private void LogUnassignedSlots(DateTime date, List<(int PositionIdx, int PeriodIdx)> unassignedSlots)
        {
            foreach (var (posIdx, periodIdx) in unassignedSlots)
            {
                var positionName = _context.Positions[posIdx].Name;
                var timeRange = $"{periodIdx * 2:D2}:00-{(periodIdx * 2 + 2):D2}:00";
                System.Diagnostics.Debug.WriteLine($"[未分配] {date:yyyy-MM-dd} {timeRange} {positionName}");
            }
        }

        /// <summary>
        /// 执行贪心分配算法 - 对应需求7.5
        /// 按照先哨位再时段的顺序进行人员分配
        /// </summary>
        private async Task PerformGreedyAssignmentsAsync(DateTime date, CancellationToken cancellationToken)
        {
            if (_tensor == null || _softConstraintCalculator == null) return;

            // 初始化MRV策略
            InitializeStrategies();
            if (_mrvStrategy == null) return;

            // 按照先哨位再时段的顺序进行分配 - 对应需求7.5
            int maxSlots = _tensor.PositionCount * 12;
            int processedSlots = 0;

            for (int iteration = 0; iteration < maxSlots; iteration++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 使用MRV策略选择下一个分配位置
                var (posIdx, periodIdx) = _mrvStrategy.SelectNextSlot();
                if (posIdx == -1 || periodIdx == -1)
                {
                    break; // 所有位置已分配或无可行分配
                }

                // 获取可行人员列表
                var feasiblePersons = _tensor.GetFeasiblePersons(posIdx, periodIdx);
                if (feasiblePersons.Length == 0)
                {
                    _mrvStrategy.MarkAsAssigned(posIdx, periodIdx);
                    continue; // 无可行人员，跳过此位置
                }

                // 使用软约束评分选择最优人员 - 对应需求6.1-6.4
                int bestPersonIdx = _softConstraintCalculator.SelectBestPerson(feasiblePersons, periodIdx, date);
                if (bestPersonIdx >= 0)
                {
                    // 执行分配并更新约束
                    await AssignPersonAsync(posIdx, periodIdx, bestPersonIdx, date);
                    processedSlots++;

                    // 定期让出控制权，避免长时间阻塞
                    if (processedSlots % _config.YieldInterval == 0)
                    {
                        await Task.Yield();
                    }
                }
                else
                {
                    _mrvStrategy.MarkAsAssigned(posIdx, periodIdx);
                }
            }

            // 检查是否有未分配的位置
            var unassignedSlots = _mrvStrategy.GetUnassignedWithNoCandidates();
            if (unassignedSlots.Count > 0 && _config.LogUnassignedSlots)
            {
                LogUnassignedSlots(date, unassignedSlots);
            }
        }

        /// <summary>
        /// 执行分配并更新约束（异步增强版）
        /// </summary>
        private async Task AssignPersonAsync(int positionIdx, int periodIdx, int personIdx, DateTime date, bool isManual = false)
        {
            if (_tensor == null || _mrvStrategy == null || _scoreCalculator == null) return;

            // 记录分配
            _context.RecordAssignment(date, periodIdx, positionIdx, personIdx);
            _mrvStrategy.MarkAsAssigned(positionIdx, periodIdx);

            // 更新张量约束
            _tensor.SetOthersInfeasibleForSlot(positionIdx, periodIdx, personIdx);
            _tensor.SetOtherPositionsInfeasibleForPersonPeriod(personIdx, periodIdx, positionIdx);

            // 应用时段不连续约束 - 对应需求5.2（仅自动分配时应用）
            if (!isManual)
            {
                ApplyNonConsecutiveConstraint(personIdx, periodIdx);
            }

            // 应用夜哨唯一约束 - 对应需求5.1
            ApplyNightShiftUniquenessConstraint(personIdx, periodIdx);

            // 更新MRV策略的候选计数
            _mrvStrategy.UpdateCandidateCountsAfterAssignment(positionIdx, periodIdx, personIdx);

            // 更新人员评分状态
            _scoreCalculator.UpdatePersonScoreState(personIdx, periodIdx, date);

            // 异步操作：记录分配日志（如果启用）
            if (_config.EnableAssignmentLogging)
            {
                await LogAssignmentAsync(positionIdx, periodIdx, personIdx, date, isManual);
            }
        }

        /// <summary>
        /// 应用时段不连续约束 - 对应需求5.2
        /// </summary>
        private void ApplyNonConsecutiveConstraint(int personIdx, int periodIdx)
        {
            if (_tensor == null) return;

            // 相邻时段不能连续上哨
            if (periodIdx > 0)
                _tensor.SetPersonInfeasibleForPeriod(personIdx, periodIdx - 1);
            if (periodIdx < 11)
                _tensor.SetPersonInfeasibleForPeriod(personIdx, periodIdx + 1);
        }

        /// <summary>
        /// 应用夜哨唯一约束 - 对应需求5.1
        /// </summary>
        private void ApplyNightShiftUniquenessConstraint(int personIdx, int periodIdx)
        {
            if (_tensor == null) return;

            // 夜哨时段：23:00-01:00, 01:00-03:00, 03:00-05:00, 05:00-07:00
            int[] nightPeriods = { 11, 0, 1, 2 };

            if (nightPeriods.Contains(periodIdx))
            {
                foreach (var np in nightPeriods)
                {
                    if (np != periodIdx)
                        _tensor.SetPersonInfeasibleForPeriod(personIdx, np);
                }
            }
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
                                PersonnelId = personalId, // 修正属性名
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


/// <summary>
/// 贪心调度器配置类
/// </summary>
public class GreedySchedulerConfig
{
    /// <summary>
    /// 充分休息得分权重 - 对应需求6.1
    /// </summary>
    public double RestWeight { get; set; } = 1.0;

    /// <summary>
    /// 休息日平衡得分权重 - 对应需求6.2
    /// </summary>
    public double HolidayWeight { get; set; } = 1.5;

    /// <summary>
    /// 时段平衡得分权重 - 对应需求6.3
    /// </summary>
    public double TimeSlotWeight { get; set; } = 1.0;

    /// <summary>
    /// 是否使用优化的张量操作 - 对应需求7.2, 7.4
    /// </summary>
    public bool UseOptimizedTensor { get; set; } = true;

    /// <summary>
    /// 是否启用分配日志记录
    /// </summary>
    public bool EnableAssignmentLogging { get; set; } = false;

    /// <summary>
    /// 是否记录约束违反日志
    /// </summary>
    public bool LogConstraintViolations { get; set; } = true;

    /// <summary>
    /// 是否记录未分配位置
    /// </summary>
    public bool LogUnassignedSlots { get; set; } = true;

    /// <summary>
    /// 异步操作让出控制权的间隔（处理多少个位置后让出一次）
    /// </summary>
    public int YieldInterval { get; set; } = 10;

    /// <summary>
    /// 最大重试次数（当出现无解时）
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// 是否启用性能监控
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = false;
}
