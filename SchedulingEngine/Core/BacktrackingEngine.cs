using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.Data.Logging;
using AutoScheduling3.DTOs;
using AutoScheduling3.SchedulingEngine.Strategies;

namespace AutoScheduling3.SchedulingEngine.Core
{
    /// <summary>
    /// 回溯引擎：管理回溯逻辑，支持智能回溯和路径记忆
    /// 对应需求: 1.1, 1.2, 1.3, 1.4, 1.5, 5.1-5.5
    /// </summary>
    public class BacktrackingEngine
    {
        private readonly SchedulingContext _context;
        private readonly FeasibilityTensor _tensor;
        private readonly MRVStrategy _mrvStrategy;
        private readonly ConstraintValidator _constraintValidator;
        private readonly SoftConstraintCalculator _softConstraintCalculator;
        private readonly AssignmentStack _assignmentStack;
        private readonly BacktrackingConfig _config;
        private readonly ILogger _logger;

        // 回溯统计
        private readonly BacktrackingStatistics _statistics;
        private readonly Stopwatch _backtrackingTimer;

        // 路径记忆（避免重复尝试失败的路径）
        private readonly HashSet<string> _failedPaths;

        // 内存监控 - 对应需求6.5
        private long _lastMemoryCheck;
        private const long MemoryCheckInterval = 10; // 每10次分配检查一次内存
        private bool _memoryThresholdExceeded;
        private int _memoryWarningCount;

        /// <summary>
        /// 构造函数：初始化回溯引擎
        /// 对应需求: 1.1, 1.2, 8.4
        /// </summary>
        public BacktrackingEngine(
            SchedulingContext context,
            FeasibilityTensor tensor,
            MRVStrategy mrvStrategy,
            ConstraintValidator constraintValidator,
            SoftConstraintCalculator softConstraintCalculator,
            BacktrackingConfig config,
            ILogger? logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _tensor = tensor ?? throw new ArgumentNullException(nameof(tensor));
            _mrvStrategy = mrvStrategy ?? throw new ArgumentNullException(nameof(mrvStrategy));
            _constraintValidator = constraintValidator ?? throw new ArgumentNullException(nameof(constraintValidator));
            _softConstraintCalculator = softConstraintCalculator ?? throw new ArgumentNullException(nameof(softConstraintCalculator));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? new DebugLogger("Backtracking");

            // 初始化分配栈
            _assignmentStack = new AssignmentStack(_config.MaxBacktrackDepth);

            // 初始化统计信息
            _statistics = new BacktrackingStatistics();
            _backtrackingTimer = new Stopwatch();

            // 初始化路径记忆
            _failedPaths = _config.EnablePathMemory ? new HashSet<string>() : new HashSet<string>();

            _lastMemoryCheck = 0;
            _memoryThresholdExceeded = false;
            _memoryWarningCount = 0;

            // 记录初始化日志
            if (_config.LogBacktracking)
            {
                _logger.Log($"回溯引擎已初始化 - 最大深度: {_config.MaxBacktrackDepth}, " +
                           $"每决策点候选数: {_config.MaxCandidatesPerDecision}, " +
                           $"智能回溯: {_config.EnableSmartBacktrackSelection}, " +
                           $"路径记忆: {_config.EnablePathMemory}");
            }
        }

        /// <summary>
        /// 获取回溯统计信息
        /// 对应需求: 4.2, 4.5
        /// </summary>
        public BacktrackingStatistics GetStatistics()
        {
            return _statistics;
        }

        /// <summary>
        /// 记录回溯完成的摘要日志
        /// 对应需求: 8.4
        /// </summary>
        public void LogBacktrackingSummary()
        {
            if (!_config.LogBacktracking) return;

            var summary = new System.Text.StringBuilder();
            summary.AppendLine("========== 回溯机制执行摘要 ==========");
            summary.AppendLine($"总回溯次数: {_statistics.TotalBacktracks}");
            summary.AppendLine($"成功回溯: {_statistics.SuccessfulBacktracks}");
            summary.AppendLine($"失败回溯: {_statistics.FailedBacktracks}");
            summary.AppendLine($"最大回溯深度: {_statistics.MaxDepthReached}");
            summary.AppendLine($"平均回溯深度: {_statistics.AverageBacktrackDepth:F2}");
            summary.AppendLine($"死胡同检测次数: {_statistics.DeadEndDetections}");
            summary.AppendLine($"状态快照创建次数: {_statistics.SnapshotsCreated}");
            summary.AppendLine($"状态恢复次数: {_statistics.StateRestores}");
            summary.AppendLine($"回溯总耗时: {_statistics.BacktrackingTimeMs}ms");
            
            if (_statistics.AvoidedDuplicatePaths > 0)
            {
                summary.AppendLine($"避免重复路径: {_statistics.AvoidedDuplicatePaths} 次");
            }
            
            if (_statistics.MemoryPressureEvents > 0)
            {
                summary.AppendLine($"内存压力事件: {_statistics.MemoryPressureEvents} 次");
                summary.AppendLine($"峰值内存使用: {_statistics.PeakMemoryUsageMB:F2}MB");
            }
            
            summary.AppendLine("=====================================");
            
            _logger.Log(summary.ToString());
        }

        /// <summary>
        /// 获取当前回溯深度
        /// </summary>
        public int CurrentDepth => _assignmentStack.Depth;

        /// <summary>
        /// 检查是否已达最大回溯深度
        /// </summary>
        public bool IsAtMaxDepth => _assignmentStack.IsAtMaxDepth;

        /// <summary>
        /// 清空回溯状态（用于新的一天）
        /// </summary>
        public void Clear()
        {
            _assignmentStack.Clear();
            _failedPaths.Clear();
            _lastMemoryCheck = 0;
            _memoryThresholdExceeded = false;
            _memoryWarningCount = 0;
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStatistics()
        {
            _statistics.Reset();
            _backtrackingTimer.Reset();
        }

        /// <summary>
        /// 检测是否遇到死胡同
        /// 对应需求: 1.1, 5.1
        /// </summary>
        public bool DetectDeadEnd()
        {
            // 使用MRV策略检测无候选时段
            var unassignedWithNoCandidates = _mrvStrategy.GetUnassignedWithNoCandidates();
            
            if (unassignedWithNoCandidates.Count > 0)
            {
                _statistics.RecordDeadEnd();
                
                if (_config.LogBacktracking)
                {
                    LogDeadEnd(unassignedWithNoCandidates);
                }
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 记录死胡同日志
        /// 对应需求: 8.4
        /// </summary>
        private void LogDeadEnd(List<(int PositionIdx, int PeriodIdx)> unassignedSlots)
        {
            if (!_config.LogBacktracking) return;

            var message = $"检测到死胡同: {unassignedSlots.Count} 个时段无候选人员";
            _logger.LogWarning(message);

            // 记录详细的未分配时段信息
            var details = new System.Text.StringBuilder();
            details.AppendLine("未分配时段详情:");
            foreach (var (posIdx, periodIdx) in unassignedSlots.Take(5))
            {
                var positionName = _context.Positions[posIdx].Name;
                var feasibleCount = _tensor.GetFeasiblePersons(posIdx, periodIdx).Length;
                details.AppendLine($"  - 哨位: {positionName}, 时段: {periodIdx}, 可行候选数: {feasibleCount}");
            }
            if (unassignedSlots.Count > 5)
            {
                details.AppendLine($"  ... 还有 {unassignedSlots.Count - 5} 个未分配时段");
            }
            _logger.Log(details.ToString());
        }

        /// <summary>
        /// 尝试分配人员到指定位置，支持回溯
        /// 对应需求: 1.2, 5.3
        /// </summary>
        public async Task<bool> TryAssignWithBacktracking(
            int positionIdx,
            int periodIdx,
            DateTime date,
            IProgress<SchedulingProgressReport>? progress,
            CancellationToken cancellationToken)
        {
            // 检查是否已达最大深度
            if (IsAtMaxDepth)
            {
                if (_config.LogBacktracking)
                {
                    _logger.LogWarning($"已达最大回溯深度 {_config.MaxBacktrackDepth}，无法继续分配");
                }
                return false;
            }

            // 获取可行候选人员列表
            var feasiblePersons = _tensor.GetFeasiblePersons(positionIdx, periodIdx);
            if (feasiblePersons.Length == 0)
            {
                return false; // 无可行人员
            }

            // 使用软约束计算器对候选人员评分排序
            var rankedScores = _softConstraintCalculator.CalculateAndRankScores(feasiblePersons, periodIdx, date);
            var sortedCandidates = rankedScores
                .Take(_config.MaxCandidatesPerDecision)
                .Select(x => x.PersonIdx)
                .ToList();

            if (sortedCandidates.Count == 0)
            {
                return false;
            }

            // 创建状态快照
            var snapshot = CreateSnapshot(date, _assignmentStack.Depth);
            _statistics.RecordSnapshotCreated();

            // 创建分配记录
            var record = AssignmentRecord.Create(
                positionIdx,
                periodIdx,
                sortedCandidates[0],
                date,
                sortedCandidates,
                snapshot,
                _assignmentStack.Depth,
                isManual: false);

            // 尝试分配第一个候选人员
            bool assigned = await TryAssignPerson(positionIdx, periodIdx, sortedCandidates[0], date);
            
            if (assigned)
            {
                // 分配成功，压入栈
                _assignmentStack.Push(record);
                _statistics.RecordCandidatesTried(1);
                
                if (_config.LogBacktracking)
                {
                    var positionName = _context.Positions[positionIdx].Name;
                    var personName = _context.Personals[sortedCandidates[0]].Name;
                    _logger.Log($"分配成功 - 哨位: {positionName}, 时段: {periodIdx}, " +
                               $"人员: {personName}, 深度: {_assignmentStack.Depth}, " +
                               $"候选数: {sortedCandidates.Count}");
                }
                
                // 定期检查内存
                CheckMemoryUsage();
                
                return true;
            }

            // 第一个候选失败，尝试其他候选
            for (int i = 1; i < sortedCandidates.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                assigned = await TryAssignPerson(positionIdx, periodIdx, sortedCandidates[i], date);
                
                if (assigned)
                {
                    record.CurrentCandidateIndex = i;
                    record.PersonIdx = sortedCandidates[i];
                    _assignmentStack.Push(record);
                    _statistics.RecordCandidatesTried(i + 1);
                    
                    if (_config.LogBacktracking)
                    {
                        var positionName = _context.Positions[positionIdx].Name;
                        var personName = _context.Personals[sortedCandidates[i]].Name;
                        _logger.Log($"分配成功（第{i + 1}个候选）- 哨位: {positionName}, 时段: {periodIdx}, " +
                                   $"人员: {personName}, 深度: {_assignmentStack.Depth}");
                    }
                    
                    CheckMemoryUsage();
                    return true;
                }
            }

            // 所有候选都失败
            _statistics.RecordCandidatesTried(sortedCandidates.Count);
            
            if (_config.LogBacktracking)
            {
                var positionName = _context.Positions[positionIdx].Name;
                _logger.LogWarning($"所有候选人员分配失败 - 哨位: {positionName}, 时段: {periodIdx}, " +
                                  $"尝试候选数: {sortedCandidates.Count}");
            }
            
            return false;
        }

        /// <summary>
        /// 执行回溯操作
        /// 对应需求: 1.2, 1.3, 5.4, 6.5
        /// </summary>
        public async Task<bool> Backtrack(
            DateTime date,
            IProgress<SchedulingProgressReport>? progress,
            CancellationToken cancellationToken = default)
        {
            _backtrackingTimer.Start();

            try
            {
                // 检查栈是否为空
                if (_assignmentStack.IsEmpty)
                {
                    if (_config.LogBacktracking)
                    {
                        _logger.LogWarning("分配栈为空，无法回溯");
                    }
                    _statistics.RecordBacktrack(_assignmentStack.Depth, false);
                    return false;
                }

                // 检查回溯深度是否超限
                if (_assignmentStack.Depth >= _config.MaxBacktrackDepth)
                {
                    if (_config.LogBacktracking)
                    {
                        _logger.LogWarning($"回溯深度超限: {_assignmentStack.Depth}/{_config.MaxBacktrackDepth}");
                    }
                    _statistics.RecordBacktrack(_assignmentStack.Depth, false);
                    return false;
                }

                // 检查内存压力
                if (ShouldStopDueToMemoryPressure())
                {
                    if (_config.LogBacktracking)
                    {
                        _logger.LogWarning($"因内存压力停止回溯 (当前内存: {_statistics.CurrentMemoryUsageMB:F2}MB)");
                    }
                    _statistics.RecordBacktrack(_assignmentStack.Depth, false);
                    return false;
                }

                // 弹出最近的分配记录
                var record = _assignmentStack.Pop();
                if (record == null)
                {
                    return false;
                }

                if (_config.LogBacktracking)
                {
                    var positionName = _context.Positions[record.PositionIdx].Name;
                    var currentPersonName = _context.Personals[record.PersonIdx].Name;
                    _logger.Log($"开始回溯 - 深度: {record.Depth}, 哨位: {positionName}, 时段: {record.PeriodIdx}, " +
                               $"当前人员: {currentPersonName}, " +
                               $"候选进度: {record.CurrentCandidateIndex + 1}/{record.Candidates.Count}, " +
                               $"时间: {DateTime.Now:HH:mm:ss.fff}");
                }

                // 恢复状态快照
                RestoreState(record.Snapshot, date);
                _statistics.RecordStateRestore();

                // 获取下一个候选人员
                var nextCandidate = record.GetNextCandidate();
                
                if (nextCandidate.HasValue)
                {
                    // 有下一个候选，尝试分配
                    bool assigned = await TryAssignPerson(
                        record.PositionIdx,
                        record.PeriodIdx,
                        nextCandidate.Value,
                        date);

                    if (assigned)
                    {
                        // 分配成功，更新记录并重新压入栈
                        record.PersonIdx = nextCandidate.Value;
                        _assignmentStack.Push(record);
                        
                        _statistics.RecordBacktrack(record.Depth, true);
                        
                        if (_config.LogBacktracking)
                        {
                            var positionName = _context.Positions[record.PositionIdx].Name;
                            var newPersonName = _context.Personals[nextCandidate.Value].Name;
                            _logger.Log($"回溯成功 - 哨位: {positionName}, 时段: {record.PeriodIdx}, " +
                                       $"新人员: {newPersonName}, " +
                                       $"候选序号: {record.CurrentCandidateIndex + 1}/{record.Candidates.Count}, " +
                                       $"深度: {record.Depth}, " +
                                       $"总回溯次数: {_statistics.TotalBacktracks}");
                        }
                        
                        // 报告回溯进度
                        ReportBacktrackProgress(progress, record.Depth, true);
                        
                        return true;
                    }
                    else
                    {
                        // 当前候选失败，递归回溯
                        if (_config.LogBacktracking)
                        {
                            var positionName = _context.Positions[record.PositionIdx].Name;
                            var failedPersonName = _context.Personals[nextCandidate.Value].Name;
                            _logger.Log($"候选人员分配失败 - 哨位: {positionName}, 时段: {record.PeriodIdx}, " +
                                       $"人员: {failedPersonName}, 继续回溯");
                        }
                        return await Backtrack(date, progress, cancellationToken);
                    }
                }
                else
                {
                    // 没有更多候选，继续回溯到更早的决策点
                    if (_config.LogBacktracking)
                    {
                        var positionName = _context.Positions[record.PositionIdx].Name;
                        _logger.Log($"所有候选已尝试 - 哨位: {positionName}, 时段: {record.PeriodIdx}, " +
                                   $"候选总数: {record.Candidates.Count}, 继续回溯到更早决策点");
                    }
                    
                    _statistics.RecordBacktrack(record.Depth, false);
                    
                    // 递归回溯
                    return await Backtrack(date, progress, cancellationToken);
                }
            }
            finally
            {
                _backtrackingTimer.Stop();
                _statistics.BacktrackingTimeMs = _backtrackingTimer.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// 创建状态快照
        /// </summary>
        private StateSnapshot CreateSnapshot(DateTime date, int depth)
        {
            // 获取MRV策略的内部状态（需要通过反射或添加公共访问器）
            // 这里假设MRVStrategy提供了获取状态的方法
            var candidateCounts = GetMRVCandidateCounts();
            var assignedFlags = GetMRVAssignedFlags();

            return StateSnapshot.CreateFromTensor(
                _tensor,
                candidateCounts,
                assignedFlags,
                _context.Assignments,
                date,
                depth);
        }

        /// <summary>
        /// 恢复状态快照
        /// 对应需求: 2.4
        /// </summary>
        private void RestoreState(StateSnapshot snapshot, DateTime date)
        {
            var candidateCounts = _mrvStrategy.GetCandidateCountsReference();
            var assignedFlags = _mrvStrategy.GetAssignedFlagsReference();

            snapshot.RestoreToTensor(
                _tensor,
                candidateCounts,
                assignedFlags,
                _context.Assignments,
                date);
        }

        /// <summary>
        /// 尝试分配人员（内部方法）
        /// </summary>
        private async Task<bool> TryAssignPerson(int positionIdx, int periodIdx, int personIdx, DateTime date)
        {
            // 验证约束
            if (!_constraintValidator.ValidateAllConstraints(personIdx, positionIdx, periodIdx, date))
            {
                return false;
            }

            // 执行分配
            _context.RecordAssignment(date, periodIdx, positionIdx, personIdx);
            _mrvStrategy.MarkAsAssigned(positionIdx, periodIdx);

            // 更新张量约束
            _tensor.SetOthersInfeasibleForSlot(positionIdx, periodIdx, personIdx);
            _tensor.SetOtherPositionsInfeasibleForPersonPeriod(personIdx, periodIdx, positionIdx);

            // 应用时段不连续约束
            ApplyNonConsecutiveConstraint(personIdx, periodIdx);

            // 应用夜哨唯一约束
            ApplyNightShiftUniquenessConstraint(personIdx, periodIdx);

            // 更新MRV策略的候选计数
            _mrvStrategy.UpdateCandidateCountsAfterAssignment(positionIdx, periodIdx, personIdx);

            return true;
        }

        /// <summary>
        /// 应用时段不连续约束
        /// </summary>
        private void ApplyNonConsecutiveConstraint(int personIdx, int periodIdx)
        {
            // 相邻时段不能连续上哨
            if (periodIdx > 0)
                _tensor.SetPersonInfeasibleForPeriod(personIdx, periodIdx - 1);
            if (periodIdx < 11)
                _tensor.SetPersonInfeasibleForPeriod(personIdx, periodIdx + 1);
        }

        /// <summary>
        /// 应用夜哨唯一约束
        /// </summary>
        private void ApplyNightShiftUniquenessConstraint(int personIdx, int periodIdx)
        {
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

        /// <summary>
        /// 检查内存使用并触发自适应调整
        /// 对应需求: 6.5
        /// </summary>
        private void CheckMemoryUsage()
        {
            _lastMemoryCheck++;
            
            if (_lastMemoryCheck % MemoryCheckInterval == 0)
            {
                var currentMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
                _statistics.UpdateMemoryUsage(currentMemoryMB);

                if (currentMemoryMB > _config.MemoryThresholdMB)
                {
                    _memoryWarningCount++;
                    
                    if (_config.LogBacktracking)
                    {
                        _logger.LogWarning($"内存使用超过阈值: {currentMemoryMB:F2}MB > {_config.MemoryThresholdMB}MB " +
                                          $"(警告次数: {_memoryWarningCount})");
                    }
                    
                    // 第一次超过阈值：触发内存清理
                    if (_memoryWarningCount == 1)
                    {
                        if (_config.LogBacktracking)
                        {
                            _logger.Log("触发垃圾回收以释放内存");
                        }
                        GC.Collect(1, GCCollectionMode.Optimized);
                        GC.WaitForPendingFinalizers();
                        
                        // 重新检查内存
                        var afterGCMemoryMB = GC.GetTotalMemory(true) / (1024.0 * 1024.0);
                        _statistics.UpdateMemoryUsage(afterGCMemoryMB);
                        
                        if (_config.LogBacktracking)
                        {
                            _logger.Log($"GC后内存使用: {afterGCMemoryMB:F2}MB (释放: {currentMemoryMB - afterGCMemoryMB:F2}MB)");
                        }
                    }
                    
                    // 持续超过阈值：标记内存压力状态
                    if (_memoryWarningCount >= 3)
                    {
                        _memoryThresholdExceeded = true;
                        
                        if (_config.LogBacktracking)
                        {
                            _logger.LogWarning($"内存压力持续，建议降低回溯深度或禁用回溯 " +
                                             $"(当前深度: {_assignmentStack.Depth}, 最大深度: {_config.MaxBacktrackDepth})");
                        }
                        
                        _statistics.RecordMemoryPressure();
                    }
                }
                else
                {
                    // 内存使用正常，重置警告计数
                    if (_memoryWarningCount > 0)
                    {
                        if (_config.LogBacktracking)
                        {
                            _logger.Log($"内存使用恢复正常: {currentMemoryMB:F2}MB");
                        }
                        _memoryWarningCount = 0;
                        _memoryThresholdExceeded = false;
                    }
                }
            }
        }

        /// <summary>
        /// 检查是否应该因内存压力而停止回溯
        /// </summary>
        public bool ShouldStopDueToMemoryPressure()
        {
            return _memoryThresholdExceeded;
        }

        /// <summary>
        /// 报告回溯进度
        /// </summary>
        private void ReportBacktrackProgress(IProgress<SchedulingProgressReport>? progress, int depth, bool success)
        {
            if (progress == null) return;

            var report = new SchedulingProgressReport
            {
                CurrentStage = SchedulingStage.Backtracking,
                StageDescription = success ? $"回溯成功 (深度={depth})" : $"回溯中 (深度={depth})",
                CurrentBacktrackDepth = depth,
                BacktrackingStats = _statistics
            };

            progress.Report(report);
        }

        /// <summary>
        /// 获取MRV策略的候选计数（用于创建快照）
        /// </summary>
        private int[,] GetMRVCandidateCounts()
        {
            return _mrvStrategy.GetCandidateCountsCopy();
        }

        /// <summary>
        /// 获取MRV策略的分配标记（用于创建快照）
        /// </summary>
        private bool[,] GetMRVAssignedFlags()
        {
            return _mrvStrategy.GetAssignedFlagsCopy();
        }

        /// <summary>
        /// 判定是否无解
        /// 对应需求: 2.5
        /// </summary>
        public bool IsUnsolvable()
        {
            // 当分配栈为空且仍存在未分配时段时，判定为无解
            if (_assignmentStack.IsEmpty)
            {
                var unassignedSlots = _mrvStrategy.GetUnassignedSlots();
                return unassignedSlots.Count > 0;
            }
            
            return false;
        }

        /// <summary>
        /// 生成无解诊断报告
        /// 对应需求: 4.3, 8.3
        /// </summary>
        public BacktrackingDiagnosticReport GenerateDiagnosticReport(DateTime startDate, DateTime endDate)
        {
            var report = new BacktrackingDiagnosticReport
            {
                HasCompleteSolution = !IsUnsolvable(),
                Statistics = _statistics,
                GeneratedAt = DateTime.Now
            };

            // 计算总时段数和已分配时段数
            var totalDays = (endDate - startDate).Days + 1;
            report.TotalSlots = _context.Positions.Count * 12 * totalDays;
            report.AssignedSlots = _context.Assignments.Count;

            // 收集未分配时段信息
            var unassignedSlots = _mrvStrategy.GetUnassignedSlots();
            foreach (var (posIdx, periodIdx) in unassignedSlots)
            {
                var position = _context.Positions[posIdx];
                var feasiblePersons = _tensor.GetFeasiblePersons(posIdx, periodIdx);
                
                var slotInfo = new UnassignedSlotInfo
                {
                    PositionIdx = posIdx,
                    PositionName = position.Name,
                    PeriodIdx = periodIdx,
                    Date = startDate, // 简化处理，实际应该根据时段计算具体日期
                    CandidateCount = feasiblePersons.Length,
                    FailureReason = DetermineFailureReason(feasiblePersons.Length, posIdx, periodIdx)
                };

                // 添加约束冲突详情
                if (feasiblePersons.Length == 0)
                {
                    slotInfo.ConstraintDetails = AnalyzeConstraintConflicts(posIdx, periodIdx);
                }

                report.AddUnassignedSlot(slotInfo);
            }

            // 生成失败原因分析
            report.GenerateFailureAnalysis();

            // 生成建议措施
            report.GenerateRecommendations();

            // 添加回溯历史（从日志或统计中提取）
            AddBacktrackHistoryToReport(report);

            return report;
        }

        /// <summary>
        /// 确定失败原因
        /// </summary>
        private string DetermineFailureReason(int candidateCount, int posIdx, int periodIdx)
        {
            if (candidateCount == 0)
            {
                return "无可用候选人员";
            }
            else if (candidateCount <= 2)
            {
                return "候选人员不足，回溯失败";
            }
            else
            {
                return "所有候选人员分配失败";
            }
        }

        /// <summary>
        /// 分析约束冲突
        /// </summary>
        private string AnalyzeConstraintConflicts(int posIdx, int periodIdx)
        {
            var conflicts = new List<string>();
            var position = _context.Positions[posIdx];

            // 检查技能要求
            if (position.RequiredSkillIds != null && position.RequiredSkillIds.Any())
            {
                var skillNames = position.RequiredSkillIds
                    .Select(skillId => _context.Skills.FirstOrDefault(s => s.Id == skillId)?.Name ?? "未知技能")
                    .ToList();
                conflicts.Add($"需要技能: {string.Join(", ", skillNames)}");
            }

            // 检查可用人员列表
            if (position.AvailablePersonnelIds != null && position.AvailablePersonnelIds.Any())
            {
                conflicts.Add($"限定人员数: {position.AvailablePersonnelIds.Count}");
            }

            // 检查时段类型
            int[] nightPeriods = { 11, 0, 1, 2 };
            if (nightPeriods.Contains(periodIdx))
            {
                conflicts.Add("夜哨时段（可能受夜哨唯一约束影响）");
            }

            return conflicts.Any() ? string.Join("; ", conflicts) : "约束冲突详情未知";
        }

        /// <summary>
        /// 添加回溯历史到报告
        /// </summary>
        private void AddBacktrackHistoryToReport(BacktrackingDiagnosticReport report)
        {
            // 从统计信息生成回溯历史摘要
            if (_statistics.TotalBacktracks > 0)
            {
                report.AddBacktrackHistory($"总回溯次数: {_statistics.TotalBacktracks}");
                report.AddBacktrackHistory($"成功回溯: {_statistics.SuccessfulBacktracks}");
                report.AddBacktrackHistory($"失败回溯: {_statistics.FailedBacktracks}");
                report.AddBacktrackHistory($"最大深度: {_statistics.MaxDepthReached}");
                report.AddBacktrackHistory($"平均深度: {_statistics.AverageBacktrackDepth:F2}");
                
                if (_statistics.DeadEndDetections > 0)
                {
                    report.AddBacktrackHistory($"死胡同检测: {_statistics.DeadEndDetections} 次");
                }
                
                if (_statistics.SmartBacktrackSelections > 0)
                {
                    report.AddBacktrackHistory($"智能回溯选择: {_statistics.SmartBacktrackSelections} 次");
                }
                
                if (_statistics.AvoidedDuplicatePaths > 0)
                {
                    report.AddBacktrackHistory($"避免重复路径: {_statistics.AvoidedDuplicatePaths} 次");
                }
            }
            else
            {
                report.AddBacktrackHistory("未发生回溯");
            }
        }

        /// <summary>
        /// 获取部分结果的诊断报告
        /// 对应需求: 2.5, 4.3
        /// </summary>
        public BacktrackingDiagnosticReport GetPartialResultDiagnostics(DateTime startDate, DateTime endDate)
        {
            var report = GenerateDiagnosticReport(startDate, endDate);
            
            if (!report.HasCompleteSolution)
            {
                report.FailureAnalysis += "\n\n无法找到完整解的原因:\n";
                
                if (_assignmentStack.IsEmpty)
                {
                    report.FailureAnalysis += "  - 分配栈已空，所有可能的分配路径都已尝试\n";
                }
                
                if (_statistics.MaxDepthReached >= _config.MaxBacktrackDepth)
                {
                    report.FailureAnalysis += $"  - 回溯深度达到上限 ({_config.MaxBacktrackDepth})\n";
                }
                
                if (_memoryThresholdExceeded)
                {
                    report.FailureAnalysis += $"  - 内存使用超过阈值 ({_config.MemoryThresholdMB}MB)\n";
                }
                
                if (_statistics.DeadEndDetections > _statistics.TotalBacktracks * 2)
                {
                    report.FailureAnalysis += "  - 频繁遇到死胡同，可能存在结构性约束冲突\n";
                }
            }
            
            return report;
        }

        /// <summary>
        /// 验证约束一致性 - 对应需求1.5, 7.1, 7.4
        /// 确保回溯机制使用相同的约束验证器，并且约束验证逻辑未被修改
        /// </summary>
        /// <param name="date">验证日期</param>
        /// <returns>约束一致性验证结果</returns>
        public ConstraintConsistencyReport VerifyConstraintConsistency(DateTime date)
        {
            var report = new ConstraintConsistencyReport
            {
                VerificationDate = DateTime.Now,
                TargetDate = date,
                IsConsistent = true,
                Issues = new List<string>()
            };

            // 验证1: 确认使用相同的约束验证器实例
            if (_constraintValidator == null)
            {
                report.IsConsistent = false;
                report.Issues.Add("约束验证器未初始化");
                return report;
            }

            // 验证2: 检查所有已分配的时段是否满足约束
            int violationCount = 0;
            int checkedAssignments = 0;

            foreach (var (checkDate, assignments) in _context.Assignments)
            {
                if (checkDate.Date != date.Date) continue;

                for (int periodIdx = 0; periodIdx < 12; periodIdx++)
                {
                    for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                    {
                        int personIdx = assignments[periodIdx, posIdx];
                        if (personIdx >= 0)
                        {
                            checkedAssignments++;

                            // 使用相同的约束验证器验证
                            if (!_constraintValidator.ValidateAllConstraints(personIdx, posIdx, periodIdx, checkDate))
                            {
                                violationCount++;
                                var positionName = _context.Positions[posIdx].Name;
                                var personName = _context.Personals[personIdx].Name;
                                var violations = _constraintValidator.GetConstraintViolations(personIdx, posIdx, periodIdx, checkDate);
                                
                                report.IsConsistent = false;
                                report.Issues.Add($"约束违反: {positionName} 时段{periodIdx} 人员{personName} - {string.Join(", ", violations)}");
                            }
                        }
                    }
                }
            }

            report.TotalAssignmentsChecked = checkedAssignments;
            report.ConstraintViolationsFound = violationCount;

            // 验证3: 检查夜哨唯一约束
            var nightShiftViolations = VerifyNightShiftUniqueness(date);
            if (nightShiftViolations.Count > 0)
            {
                report.IsConsistent = false;
                report.Issues.AddRange(nightShiftViolations);
            }

            // 验证4: 检查时段不连续约束
            var consecutiveViolations = VerifyNonConsecutiveConstraint(date);
            if (consecutiveViolations.Count > 0)
            {
                report.IsConsistent = false;
                report.Issues.AddRange(consecutiveViolations);
            }

            // 验证5: 检查人员时段唯一性
            var uniquenessViolations = VerifyPersonTimeSlotUniqueness(date);
            if (uniquenessViolations.Count > 0)
            {
                report.IsConsistent = false;
                report.Issues.AddRange(uniquenessViolations);
            }

            if (report.IsConsistent)
            {
                report.Summary = $"约束一致性验证通过: 检查了{checkedAssignments}个分配，未发现约束违反";
            }
            else
            {
                report.Summary = $"约束一致性验证失败: 检查了{checkedAssignments}个分配，发现{report.Issues.Count}个问题";
            }

            return report;
        }

        /// <summary>
        /// 验证夜哨唯一约束
        /// </summary>
        private List<string> VerifyNightShiftUniqueness(DateTime date)
        {
            var violations = new List<string>();
            int[] nightPeriods = { 11, 0, 1, 2 };

            if (!_context.Assignments.TryGetValue(date, out var assignments))
                return violations;

            // 统计每个人员在夜哨时段的分配次数
            var nightShiftCounts = new Dictionary<int, List<int>>();

            foreach (var nightPeriod in nightPeriods)
            {
                for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                {
                    int personIdx = assignments[nightPeriod, posIdx];
                    if (personIdx >= 0)
                    {
                        if (!nightShiftCounts.ContainsKey(personIdx))
                        {
                            nightShiftCounts[personIdx] = new List<int>();
                        }
                        nightShiftCounts[personIdx].Add(nightPeriod);
                    }
                }
            }

            // 检查是否有人员被分配到多个夜哨时段
            foreach (var (personIdx, periods) in nightShiftCounts)
            {
                if (periods.Count > 1)
                {
                    var personName = _context.Personals[personIdx].Name;
                    violations.Add($"夜哨唯一约束违反: 人员{personName}被分配到{periods.Count}个夜哨时段 ({string.Join(", ", periods)})");
                }
            }

            return violations;
        }

        /// <summary>
        /// 验证时段不连续约束
        /// </summary>
        private List<string> VerifyNonConsecutiveConstraint(DateTime date)
        {
            var violations = new List<string>();

            if (!_context.Assignments.TryGetValue(date, out var assignments))
                return violations;

            // 检查每个人员的分配是否有连续时段
            var personAssignments = new Dictionary<int, List<int>>();

            for (int periodIdx = 0; periodIdx < 12; periodIdx++)
            {
                for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                {
                    int personIdx = assignments[periodIdx, posIdx];
                    if (personIdx >= 0)
                    {
                        if (!personAssignments.ContainsKey(personIdx))
                        {
                            personAssignments[personIdx] = new List<int>();
                        }
                        personAssignments[personIdx].Add(periodIdx);
                    }
                }
            }

            // 检查是否有连续时段
            foreach (var (personIdx, periods) in personAssignments)
            {
                var sortedPeriods = periods.OrderBy(p => p).ToList();
                for (int i = 0; i < sortedPeriods.Count - 1; i++)
                {
                    if (sortedPeriods[i + 1] - sortedPeriods[i] == 1)
                    {
                        var personName = _context.Personals[personIdx].Name;
                        violations.Add($"时段不连续约束违反: 人员{personName}在时段{sortedPeriods[i]}和{sortedPeriods[i + 1]}连续分配");
                    }
                }
            }

            // 检查跨日连续（时段11和次日时段0）
            var previousDate = date.AddDays(-1);
            if (_context.Assignments.TryGetValue(previousDate, out var prevAssignments))
            {
                for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                {
                    int prevPersonIdx = prevAssignments[11, posIdx];
                    if (prevPersonIdx >= 0)
                    {
                        // 检查该人员是否在当天时段0也有分配
                        for (int currPosIdx = 0; currPosIdx < _context.Positions.Count; currPosIdx++)
                        {
                            int currPersonIdx = assignments[0, currPosIdx];
                            if (currPersonIdx == prevPersonIdx)
                            {
                                var personName = _context.Personals[prevPersonIdx].Name;
                                violations.Add($"跨日时段不连续约束违反: 人员{personName}在{previousDate:yyyy-MM-dd}时段11和{date:yyyy-MM-dd}时段0连续分配");
                            }
                        }
                    }
                }
            }

            return violations;
        }

        /// <summary>
        /// 验证人员时段唯一性约束
        /// </summary>
        private List<string> VerifyPersonTimeSlotUniqueness(DateTime date)
        {
            var violations = new List<string>();

            if (!_context.Assignments.TryGetValue(date, out var assignments))
                return violations;

            // 检查每个时段每个人员是否只在一个哨位
            for (int periodIdx = 0; periodIdx < 12; periodIdx++)
            {
                var personPositions = new Dictionary<int, List<int>>();

                for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
                {
                    int personIdx = assignments[periodIdx, posIdx];
                    if (personIdx >= 0)
                    {
                        if (!personPositions.ContainsKey(personIdx))
                        {
                            personPositions[personIdx] = new List<int>();
                        }
                        personPositions[personIdx].Add(posIdx);
                    }
                }

                // 检查是否有人员在多个哨位
                foreach (var (personIdx, positions) in personPositions)
                {
                    if (positions.Count > 1)
                    {
                        var personName = _context.Personals[personIdx].Name;
                        var positionNames = positions.Select(p => _context.Positions[p].Name);
                        violations.Add($"人员时段唯一性约束违反: 人员{personName}在时段{periodIdx}被分配到{positions.Count}个哨位 ({string.Join(", ", positionNames)})");
                    }
                }
            }

            return violations;
        }
    }

    /// <summary>
    /// 约束一致性验证报告 - 对应需求1.5, 7.1, 7.4
    /// </summary>
    public class ConstraintConsistencyReport
    {
        /// <summary>
        /// 验证时间
        /// </summary>
        public DateTime VerificationDate { get; set; }

        /// <summary>
        /// 目标日期
        /// </summary>
        public DateTime TargetDate { get; set; }

        /// <summary>
        /// 是否一致
        /// </summary>
        public bool IsConsistent { get; set; }

        /// <summary>
        /// 检查的分配总数
        /// </summary>
        public int TotalAssignmentsChecked { get; set; }

        /// <summary>
        /// 发现的约束违反数量
        /// </summary>
        public int ConstraintViolationsFound { get; set; }

        /// <summary>
        /// 问题列表
        /// </summary>
        public List<string> Issues { get; set; } = new List<string>();

        /// <summary>
        /// 摘要
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// 生成报告文本
        /// </summary>
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("========== 约束一致性验证报告 ==========");
            sb.AppendLine($"验证时间: {VerificationDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"目标日期: {TargetDate:yyyy-MM-dd}");
            sb.AppendLine($"验证结果: {(IsConsistent ? "通过 ✅" : "失败 ❌")}");
            sb.AppendLine($"检查分配数: {TotalAssignmentsChecked}");
            sb.AppendLine($"约束违反数: {ConstraintViolationsFound}");
            sb.AppendLine();
            sb.AppendLine($"摘要: {Summary}");
            
            if (Issues.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("问题详情:");
                foreach (var issue in Issues)
                {
                    sb.AppendLine($"  - {issue}");
                }
            }
            
            sb.AppendLine("=====================================");
            return sb.ToString();
        }
    }
}
