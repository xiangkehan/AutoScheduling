using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.Data.Logging;
using AutoScheduling3.DTOs;
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
        
        // 日志记录器 - 对应需求8.4
        private readonly ILogger _logger;

        // 进度报告相关字段
        private Stopwatch? _executionTimer;
        private DateTime _lastProgressReport;
        private int _totalSlotsToAssign;
        private int _completedAssignments;

        public GreedyScheduler(SchedulingContext context, GreedySchedulerConfig? config = null, ILogger? logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _config = config ?? new GreedySchedulerConfig();
            _logger = logger ?? new DebugLogger("GreedyScheduler");
        }

        /// <summary>
        /// 执行排班算法（支持取消） - 对应需求3.1, 3.2, 7.5
        /// 按照先哨位再时段的顺序进行人员分配
        /// </summary>
        public async Task<Schedule> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(null, cancellationToken);
        }

        /// <summary>
        /// 执行排班算法（支持进度报告和取消） - 对应需求1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5
        /// 按照先哨位再时段的顺序进行人员分配
        /// </summary>
        public async Task<Schedule> ExecuteAsync(IProgress<SchedulingProgressReport>? progress, CancellationToken cancellationToken = default)
        {
            // 初始化进度跟踪
            _executionTimer = Stopwatch.StartNew();
            _lastProgressReport = DateTime.UtcNow;
            _completedAssignments = 0;
            
            // 计算总时段数
            int totalDays = (_context.EndDate.Date - _context.StartDate.Date).Days + 1;
            _totalSlotsToAssign = totalDays * 12 * _context.Positions.Count;

            cancellationToken.ThrowIfCancellationRequested();

            // 报告初始化阶段
            ReportProgress(progress, SchedulingStage.Initializing, "正在初始化排班环境...", 0);

            // 预处理阶段
            await PreprocessAsync();
            cancellationToken.ThrowIfCancellationRequested();

            // 报告加载数据阶段
            ReportProgress(progress, SchedulingStage.LoadingData, "正在加载排班数据...", 5);

            // 多天循环处理：每天重建张量与策略，保持跨日评分状态累积
            var currentDate = _context.StartDate.Date;

            for (int day = 0; day < totalDays; day++)
            {
                var date = currentDate.AddDays(day);
                cancellationToken.ThrowIfCancellationRequested();

                // 报告构建上下文阶段
                ReportProgress(progress, SchedulingStage.BuildingContext, 
                    $"正在构建第 {day + 1}/{totalDays} 天的排班上下文...", 
                    10 + (day * 80 / totalDays) * 0.1);

                // 初始化当日排班环境
                InitializeDailyScheduling();

                // 报告初始化张量阶段
                ReportProgress(progress, SchedulingStage.InitializingTensor, 
                    $"正在初始化第 {day + 1}/{totalDays} 天的可行性张量...", 
                    10 + (day * 80 / totalDays) * 0.2, date);

                // 报告应用约束阶段
                ReportProgress(progress, SchedulingStage.ApplyingConstraints, 
                    $"正在应用第 {day + 1}/{totalDays} 天的约束条件...", 
                    10 + (day * 80 / totalDays) * 0.3, date);

                // 应用所有约束条件
                ApplyAllConstraints(date);

                // 报告应用手动指定阶段
                ReportProgress(progress, SchedulingStage.ApplyingManualAssignments, 
                    $"正在应用第 {day + 1}/{totalDays} 天的手动指定...", 
                    10 + (day * 80 / totalDays) * 0.4, date);

                // 预置手动指定分配 - 对应需求5.8
                ApplyManualAssignmentsForDate(date);

                cancellationToken.ThrowIfCancellationRequested();

                // 报告贪心分配阶段
                ReportProgress(progress, SchedulingStage.GreedyAssignment, 
                    $"正在执行第 {day + 1}/{totalDays} 天的贪心分配...", 
                    10 + (day * 80 / totalDays) * 0.5, date);

                // 执行贪心分配算法 - 对应需求7.5
                await PerformGreedyAssignmentsAsync(date, progress, cancellationToken);

                // 注意：不再需要更新评分状态，因为现在是动态计算
            }

            // 报告完成阶段
            ReportProgress(progress, SchedulingStage.Finalizing, "正在生成最终排班结果...", 95);

            var schedule = GenerateSchedule();

            // 报告完成
            ReportProgress(progress, SchedulingStage.Completed, "排班完成", 100);

            _executionTimer?.Stop();

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

            // 初始化约束处理组件
            _constraintValidator = new ConstraintValidator(_context);
            _softConstraintCalculator = new SoftConstraintCalculator(_context);

            // 配置软约束权重
            _softConstraintCalculator.UpdateWeights(
                _config.RestWeight,
                _config.HolidayWeight,
                _config.TimeSlotWeight,
                _config.WorkloadBalanceWeight);

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

            // 使用FeasibilityTensor的新方法初始化
            _tensor.InitializeWithAvailablePersonnel(_context.Positions, _context.PersonIdToIdx);
        }

        /// <summary>
        /// 获取优化的可行人员列表 - 对应需求4.1
        /// 结合张量状态和哨位可用人员列表进行双重筛选
        /// </summary>
        private int[] GetOptimizedFeasiblePersons(int positionIdx, int periodIdx)
        {
            if (_tensor == null) return Array.Empty<int>();

            var position = _context.Positions[positionIdx];
            var feasiblePersons = new List<int>();

            // 仅检查哨位可用人员列表中的人员
            foreach (var personnelId in position.AvailablePersonnelIds)
            {
                if (_context.PersonIdToIdx.TryGetValue(personnelId, out int personIdx))
                {
                    // 检查张量中的可行性状态
                    if (_tensor[positionIdx, periodIdx, personIdx])
                    {
                        feasiblePersons.Add(personIdx);
                    }
                }
            }

            return feasiblePersons.ToArray();
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
        /// 执行贪心分配算法（集成回溯机制） - 对应需求1.1, 1.2, 1.3, 7.5
        /// 按照先哨位再时段的顺序进行人员分配，遇到死胡同时自动回溯
        /// </summary>
        private async Task PerformGreedyAssignmentsAsync(DateTime date, IProgress<SchedulingProgressReport>? progress, CancellationToken cancellationToken)
        {
            if (_tensor == null || _softConstraintCalculator == null) return;

            // 初始化MRV策略
            InitializeStrategies();
            if (_mrvStrategy == null) return;

            // 检查是否启用回溯机制 - 对应需求3.4
            if (!_config.Backtracking.EnableBacktracking)
            {
                // 禁用回溯时使用原有的贪心算法逻辑
                await PerformGreedyAssignmentsWithoutBacktrackingAsync(date, progress, cancellationToken);
                return;
            }

            // 初始化回溯引擎 - 对应需求1.1, 1.2, 8.4
            var backtrackingEngine = new BacktrackingEngine(
                _context,
                _tensor,
                _mrvStrategy,
                _constraintValidator!,
                _softConstraintCalculator,
                _config.Backtracking,
                _logger);

            // 按照先哨位再时段的顺序进行分配 - 对应需求7.5
            int maxSlots = _tensor.PositionCount * 12;
            int processedSlots = 0;
            int dayStartAssignments = _completedAssignments;

            for (int iteration = 0; iteration < maxSlots; iteration++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 检测死胡同 - 对应需求1.1
                if (backtrackingEngine.DetectDeadEnd())
                {
                    // 报告回溯开始 - 对应需求4.1
                    ReportBacktrackingProgress(progress, backtrackingEngine, date, "检测到死胡同，开始回溯...");

                    // 执行回溯 - 对应需求1.2, 1.3
                    bool backtrackSuccess = await backtrackingEngine.Backtrack(date, progress, cancellationToken);

                    if (!backtrackSuccess)
                    {
                        // 回溯失败（深度超限或无解） - 对应需求1.3
                        if (_config.LogUnassignedSlots)
                        {
                            var unassignedSlots = _mrvStrategy.GetUnassignedWithNoCandidates();
                            LogUnassignedSlots(date, unassignedSlots);
                        }

                        // 报告回溯失败 - 对应需求4.2, 4.4
                        ReportBacktrackingComplete(progress, backtrackingEngine, date, false);
                        break;
                    }

                    // 回溯成功，继续分配
                    ReportBacktrackingProgress(progress, backtrackingEngine, date, "回溯成功，继续分配...");
                    continue;
                }

                // 使用MRV策略选择下一个分配位置
                var (posIdx, periodIdx) = _mrvStrategy.SelectNextSlot();
                if (posIdx == -1 || periodIdx == -1)
                {
                    break; // 所有位置已分配或无可行分配
                }

                // 使用回溯引擎尝试分配（支持回溯） - 对应需求1.2, 5.3
                bool assigned = await backtrackingEngine.TryAssignWithBacktracking(
                    posIdx, periodIdx, date, progress, cancellationToken);

                if (assigned)
                {
                    processedSlots++;
                    _completedAssignments++;

                    // 报告进度（每完成10%或每处理10个时段）
                    if (processedSlots % Math.Max(1, maxSlots / 10) == 0 || processedSlots % 10 == 0)
                    {
                        var positionName = _context.Positions[posIdx].Name;
                        ReportProgress(progress, SchedulingStage.GreedyAssignment,
                            $"正在分配: {positionName} - 时段 {periodIdx}",
                            10 + (_completedAssignments * 80.0 / _totalSlotsToAssign),
                            date, positionName, periodIdx);
                    }

                    // 定期让出控制权，避免长时间阻塞
                    if (processedSlots % _config.YieldInterval == 0)
                    {
                        await Task.Yield();
                    }
                }
                else
                {
                    // 分配失败，标记为已处理
                    _mrvStrategy.MarkAsAssigned(posIdx, periodIdx);
                }
            }

            // 报告回溯完成 - 对应需求4.1, 4.2, 4.4, 4.5
            ReportBacktrackingComplete(progress, backtrackingEngine, date, true);

            // 验证约束一致性（如果启用） - 对应需求1.5, 7.1, 7.4
            if (_config.Backtracking.EnableConstraintConsistencyVerification)
            {
                var consistencyReport = backtrackingEngine.VerifyConstraintConsistency(date);
                
                if (!consistencyReport.IsConsistent)
                {
                    _logger.LogWarning($"约束一致性验证失败:\n{consistencyReport}");
                    
                    // 将验证结果添加到进度报告
                    if (progress != null)
                    {
                        var warningReport = new SchedulingProgressReport
                        {
                            CurrentStage = SchedulingStage.Backtracking,
                            StageDescription = "约束一致性验证失败",
                            ProgressPercentage = 10 + (_completedAssignments * 80.0 / _totalSlotsToAssign),
                            CurrentDate = date,
                            Warnings = consistencyReport.Issues,
                            HasErrors = true,
                            ErrorMessage = consistencyReport.Summary
                        };
                        progress.Report(warningReport);
                    }
                }
                else
                {
                    _logger.Log($"约束一致性验证通过: {consistencyReport.Summary}");
                }
            }

            // 检查是否有未分配的位置
            var finalUnassignedSlots = _mrvStrategy.GetUnassignedWithNoCandidates();
            if (finalUnassignedSlots.Count > 0 && _config.LogUnassignedSlots)
            {
                LogUnassignedSlots(date, finalUnassignedSlots);
            }
        }

        /// <summary>
        /// 执行贪心分配算法（不使用回溯） - 对应需求3.4
        /// 保持与原始实现一致的行为
        /// </summary>
        private async Task PerformGreedyAssignmentsWithoutBacktrackingAsync(DateTime date, IProgress<SchedulingProgressReport>? progress, CancellationToken cancellationToken)
        {
            if (_tensor == null || _softConstraintCalculator == null || _mrvStrategy == null) return;

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

                // 获取可行人员列表并进行优化筛选
                var feasiblePersons = GetOptimizedFeasiblePersons(posIdx, periodIdx);
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
                    _completedAssignments++;

                    // 报告进度（每完成10%或每处理10个时段）
                    if (processedSlots % Math.Max(1, maxSlots / 10) == 0 || processedSlots % 10 == 0)
                    {
                        var positionName = _context.Positions[posIdx].Name;
                        ReportProgress(progress, SchedulingStage.GreedyAssignment,
                            $"正在分配: {positionName} - 时段 {periodIdx}",
                            10 + (_completedAssignments * 80.0 / _totalSlotsToAssign),
                            date, positionName, periodIdx);
                    }

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
        /// 报告回溯进度 - 对应需求4.1, 4.2, 4.4
        /// </summary>
        private void ReportBacktrackingProgress(
            IProgress<SchedulingProgressReport>? progress,
            BacktrackingEngine engine,
            DateTime date,
            string message)
        {
            if (progress == null) return;

            var stats = engine.GetStatistics();
            var report = new SchedulingProgressReport
            {
                CurrentStage = SchedulingStage.Backtracking,
                StageDescription = message,
                ProgressPercentage = 10 + (_completedAssignments * 80.0 / _totalSlotsToAssign),
                CurrentDate = date,
                CurrentBacktrackDepth = engine.CurrentDepth,
                BacktrackingStats = stats,
                CompletedAssignments = _completedAssignments,
                TotalSlotsToAssign = _totalSlotsToAssign,
                RemainingSlots = _totalSlotsToAssign - _completedAssignments,
                ElapsedTime = _executionTimer?.Elapsed ?? TimeSpan.Zero,
                Warnings = new List<string>(),
                HasErrors = false
            };

            progress.Report(report);
        }

        /// <summary>
        /// 报告回溯完成 - 对应需求4.1, 4.2, 4.4, 4.5, 8.4
        /// </summary>
        private void ReportBacktrackingComplete(
            IProgress<SchedulingProgressReport>? progress,
            BacktrackingEngine engine,
            DateTime date,
            bool success)
        {
            // 记录回溯摘要日志 - 对应需求8.4
            engine.LogBacktrackingSummary();

            if (progress == null) return;

            var stats = engine.GetStatistics();
            var stage = success ? SchedulingStage.BacktrackingComplete : SchedulingStage.Backtracking;
            var message = success
                ? $"回溯完成 - 总回溯次数: {stats.TotalBacktracks}, 成功: {stats.SuccessfulBacktracks}"
                : $"回溯失败 - 已达最大深度或无解";

            var report = new SchedulingProgressReport
            {
                CurrentStage = stage,
                StageDescription = message,
                ProgressPercentage = 10 + (_completedAssignments * 80.0 / _totalSlotsToAssign),
                CurrentDate = date,
                CurrentBacktrackDepth = engine.CurrentDepth,
                BacktrackingStats = stats,
                CompletedAssignments = _completedAssignments,
                TotalSlotsToAssign = _totalSlotsToAssign,
                RemainingSlots = _totalSlotsToAssign - _completedAssignments,
                ElapsedTime = _executionTimer?.Elapsed ?? TimeSpan.Zero,
                Warnings = success ? new List<string>() : new List<string> { "回溯失败，部分时段未分配" },
                HasErrors = !success
            };

            progress.Report(report);
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

            // 注意：不再需要更新人员评分状态，因为现在是动态计算

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
                            
                            // 判断是否为夜哨
                            bool isNightShift = periodIdx == 11 || periodIdx == 0 || periodIdx == 1 || periodIdx == 2;
                            
                            schedule.Results.Add(new SingleShift
                            {
                                PositionId = positionId,
                                PersonnelId = personalId,
                                StartTime = DateTime.SpecifyKind(startTime, DateTimeKind.Utc),
                                EndTime = DateTime.SpecifyKind(endTime, DateTimeKind.Utc),
                                ScheduleId = schedule.Id,
                                DayIndex = (date.Date - _context.StartDate.Date).Days,
                                TimeSlotIndex = periodIdx,
                                IsNightShift = isNightShift
                            });
                        }
                    }
                }
            }
            return schedule;
        }

        /// <summary>
        /// 报告进度（带节流机制） - 对应需求1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5
        /// </summary>
        private void ReportProgress(
            IProgress<SchedulingProgressReport>? progress,
            SchedulingStage stage,
            string description,
            double progressPercentage,
            DateTime? currentDate = null,
            string? currentPositionName = null,
            int? currentPeriodIndex = null)
        {
            if (progress == null) return;

            // 节流机制：最小间隔100ms（除非是关键阶段）
            var now = DateTime.UtcNow;
            var isKeyStage = stage == SchedulingStage.Initializing ||
                           stage == SchedulingStage.Completed ||
                           stage == SchedulingStage.Failed ||
                           stage == SchedulingStage.Finalizing;

            if (!isKeyStage && (now - _lastProgressReport).TotalMilliseconds < 100)
            {
                return;
            }

            _lastProgressReport = now;

            var report = new SchedulingProgressReport
            {
                ProgressPercentage = Math.Min(100, Math.Max(0, progressPercentage)),
                CurrentStage = stage,
                StageDescription = description,
                CompletedAssignments = _completedAssignments,
                TotalSlotsToAssign = _totalSlotsToAssign,
                RemainingSlots = _totalSlotsToAssign - _completedAssignments,
                CurrentPositionName = currentPositionName,
                CurrentPeriodIndex = currentPeriodIndex ?? -1,
                CurrentDate = currentDate ?? _context.StartDate,
                ElapsedTime = _executionTimer?.Elapsed ?? TimeSpan.Zero,
                Warnings = new List<string>(),
                HasErrors = false,
                ErrorMessage = null
            };

            progress.Report(report);
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
    /// 提高权重以减少休息时间不足的问题
    /// </summary>
    public double RestWeight { get; set; } = 3.0;

    /// <summary>
    /// 休息日平衡得分权重 - 对应需求6.2
    /// </summary>
    public double HolidayWeight { get; set; } = 1.5;

    /// <summary>
    /// 时段平衡得分权重 - 对应需求6.3
    /// </summary>
    public double TimeSlotWeight { get; set; } = 1.0;

    /// <summary>
    /// 工作量平衡得分权重
    /// </summary>
    public double WorkloadBalanceWeight { get; set; } = 2.0;

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

    /// <summary>
    /// 回溯配置 - 对应需求3.1, 3.2, 3.3, 3.5
    /// </summary>
    public BacktrackingConfig Backtracking { get; set; } = new BacktrackingConfig();
}

/// <summary>
/// 回溯机制配置类 - 对应需求3.1, 3.2, 3.3, 3.5
/// </summary>
public class BacktrackingConfig
{
    private int _maxBacktrackDepth = 50;
    private int _maxCandidatesPerDecision = 5;
    private int _snapshotInterval = 10;
    private long _memoryThresholdMB = 500;

    /// <summary>
    /// 是否启用回溯机制 - 对应需求3.2
    /// </summary>
    public bool EnableBacktracking { get; set; } = true;

    /// <summary>
    /// 最大回溯深度 - 对应需求3.1
    /// 默认值: 50
    /// </summary>
    public int MaxBacktrackDepth
    {
        get => _maxBacktrackDepth;
        set
        {
            if (value <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[警告] MaxBacktrackDepth 必须大于0，使用默认值50");
                _maxBacktrackDepth = 50;
            }
            else
            {
                _maxBacktrackDepth = value;
            }
        }
    }

    /// <summary>
    /// 每个决策点保留的候选人员数量 - 对应需求3.3
    /// 默认值: 5
    /// </summary>
    public int MaxCandidatesPerDecision
    {
        get => _maxCandidatesPerDecision;
        set
        {
            if (value <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[警告] MaxCandidatesPerDecision 必须大于0，使用默认值5");
                _maxCandidatesPerDecision = 5;
            }
            else
            {
                _maxCandidatesPerDecision = value;
            }
        }
    }

    /// <summary>
    /// 是否启用智能回溯点选择 - 对应需求5.1, 5.2
    /// </summary>
    public bool EnableSmartBacktrackSelection { get; set; } = true;

    /// <summary>
    /// 是否启用路径记忆（避免重复尝试） - 对应需求5.5
    /// </summary>
    public bool EnablePathMemory { get; set; } = true;

    /// <summary>
    /// 状态快照保存间隔（每N次分配保存一次完整快照） - 对应需求6.3
    /// 默认值: 10
    /// </summary>
    public int SnapshotInterval
    {
        get => _snapshotInterval;
        set
        {
            if (value <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[警告] SnapshotInterval 必须大于0，使用默认值10");
                _snapshotInterval = 10;
            }
            else
            {
                _snapshotInterval = value;
            }
        }
    }

    /// <summary>
    /// 内存使用阈值（MB），超过后降低回溯深度 - 对应需求6.5
    /// 默认值: 500MB
    /// </summary>
    public long MemoryThresholdMB
    {
        get => _memoryThresholdMB;
        set
        {
            if (value <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[警告] MemoryThresholdMB 必须大于0，使用默认值500");
                _memoryThresholdMB = 500;
            }
            else
            {
                _memoryThresholdMB = value;
            }
        }
    }

    /// <summary>
    /// 是否记录回溯日志 - 对应需求8.4
    /// </summary>
    public bool LogBacktracking { get; set; } = true;

    /// <summary>
    /// 是否启用约束一致性验证（用于调试和测试） - 对应需求1.5, 7.1, 7.4
    /// 注意：启用此选项会增加性能开销，建议仅在开发和测试环境使用
    /// </summary>
    public bool EnableConstraintConsistencyVerification { get; set; } = false;

    /// <summary>
    /// 验证配置参数的有效性 - 对应需求3.5
    /// </summary>
    /// <returns>配置是否有效</returns>
    public bool Validate()
    {
        bool isValid = true;

        if (MaxBacktrackDepth <= 0)
        {
            System.Diagnostics.Debug.WriteLine($"[配置错误] MaxBacktrackDepth 必须大于0");
            isValid = false;
        }

        if (MaxCandidatesPerDecision <= 0)
        {
            System.Diagnostics.Debug.WriteLine($"[配置错误] MaxCandidatesPerDecision 必须大于0");
            isValid = false;
        }

        if (SnapshotInterval <= 0)
        {
            System.Diagnostics.Debug.WriteLine($"[配置错误] SnapshotInterval 必须大于0");
            isValid = false;
        }

        if (MemoryThresholdMB <= 0)
        {
            System.Diagnostics.Debug.WriteLine($"[配置错误] MemoryThresholdMB 必须大于0");
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// 获取配置的描述信息（用于调试）
    /// </summary>
    public override string ToString()
    {
        return $"BacktrackingConfig [Enabled={EnableBacktracking}, " +
               $"MaxDepth={MaxBacktrackDepth}, " +
               $"MaxCandidates={MaxCandidatesPerDecision}, " +
               $"SmartSelection={EnableSmartBacktrackSelection}, " +
               $"PathMemory={EnablePathMemory}, " +
               $"SnapshotInterval={SnapshotInterval}, " +
               $"MemoryThreshold={MemoryThresholdMB}MB, " +
               $"Logging={LogBacktracking}]";
    }
}
