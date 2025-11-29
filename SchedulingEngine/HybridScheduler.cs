using System;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using AutoScheduling3.Models;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine;

/// <summary>
/// 混合调度器 - 对应需求1.1, 1.2, 6.1, 6.4
/// 先执行贪心算法获得初始解，再使用遗传算法优化
/// </summary>
public class HybridScheduler
{
    private readonly GreedyScheduler _greedyScheduler;
    private readonly GeneticScheduler _geneticScheduler;

    public HybridScheduler(
        GreedyScheduler greedyScheduler,
        GeneticScheduler geneticScheduler)
    {
        _greedyScheduler = greedyScheduler ?? throw new ArgumentNullException(nameof(greedyScheduler));
        _geneticScheduler = geneticScheduler ?? throw new ArgumentNullException(nameof(geneticScheduler));
    }

    /// <summary>
    /// 执行混合调度 - 对应需求1.1, 1.2
    /// </summary>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>优化后的排班方案</returns>
    public async Task<Schedule> ExecuteAsync(
        IProgress<SchedulingProgressReport>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            // 阶段1: 执行贪心算法获得初始解 - 对应需求1.1
            ReportProgress(progress, SchedulingStage.Initializing, 0, "正在启动混合调度...");

            // 创建贪心算法进度包装器（将0-50%映射到贪心阶段）
            var greedyProgress = new Progress<SchedulingProgressReport>(report =>
            {
                // 将贪心算法的进度映射到0-50%
                var mappedReport = new SchedulingProgressReport
                {
                    CurrentStage = report.CurrentStage,
                    ProgressPercentage = report.ProgressPercentage * 0.5,
                    StageDescription = $"[贪心阶段] {report.StageDescription}",
                    CompletedAssignments = report.CompletedAssignments,
                    TotalSlotsToAssign = report.TotalSlotsToAssign,
                    RemainingSlots = report.RemainingSlots,
                    CurrentPositionName = report.CurrentPositionName,
                    CurrentPeriodIndex = report.CurrentPeriodIndex,
                    CurrentDate = report.CurrentDate,
                    ElapsedTime = report.ElapsedTime,
                    Warnings = report.Warnings,
                    HasErrors = report.HasErrors,
                    ErrorMessage = report.ErrorMessage
                };
                progress?.Report(mappedReport);
            });

            // 执行贪心算法
            var greedySolution = await _greedyScheduler.ExecuteAsync(greedyProgress, cancellationToken);

            // 检查取消令牌 - 对应需求6.4
            cancellationToken.ThrowIfCancellationRequested();

            // 阶段2: 使用遗传算法优化 - 对应需求1.2
            ReportProgress(progress, SchedulingStage.GeneticOptimizing, 50, "正在启动遗传算法优化...");

            // 创建遗传算法进度包装器（将50-100%映射到遗传阶段）
            var geneticProgress = new Progress<SchedulingProgressReport>(report =>
            {
                // 将遗传算法的进度映射到50-100%
                var mappedReport = new SchedulingProgressReport
                {
                    CurrentStage = report.CurrentStage,
                    ProgressPercentage = 50 + report.ProgressPercentage * 0.5,
                    StageDescription = $"[遗传优化] {report.StageDescription}",
                    CompletedAssignments = report.CompletedAssignments,
                    TotalSlotsToAssign = report.TotalSlotsToAssign,
                    RemainingSlots = report.RemainingSlots,
                    CurrentPositionName = report.CurrentPositionName,
                    CurrentPeriodIndex = report.CurrentPeriodIndex,
                    CurrentDate = report.CurrentDate,
                    ElapsedTime = report.ElapsedTime,
                    Warnings = report.Warnings,
                    HasErrors = report.HasErrors,
                    ErrorMessage = report.ErrorMessage,
                    GeneticProgressInfo = report.GeneticProgressInfo
                };
                progress?.Report(mappedReport);
            });

            // 执行遗传算法优化
            var optimizedSolution = await _geneticScheduler.ExecuteAsync(
                greedySolution,
                geneticProgress,
                cancellationToken);

            // 报告完成 - 对应需求6.1
            ReportProgress(progress, SchedulingStage.Completed, 100, "混合调度完成");

            return optimizedSolution;
        }
        catch (OperationCanceledException)
        {
            // 取消操作，重新抛出
            throw;
        }
        catch (Exception ex)
        {
            // 报告错误
            ReportError(progress, $"混合调度失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 报告进度 - 对应需求6.1
    /// </summary>
    /// <param name="progress">进度报告接口</param>
    /// <param name="stage">当前阶段</param>
    /// <param name="progressPercentage">进度百分比</param>
    /// <param name="description">描述</param>
    private void ReportProgress(
        IProgress<SchedulingProgressReport>? progress,
        SchedulingStage stage,
        double progressPercentage,
        string description)
    {
        if (progress == null)
            return;

        var report = new SchedulingProgressReport
        {
            CurrentStage = stage,
            ProgressPercentage = progressPercentage,
            StageDescription = description
        };

        progress.Report(report);
    }

    /// <summary>
    /// 报告错误
    /// </summary>
    /// <param name="progress">进度报告接口</param>
    /// <param name="errorMessage">错误消息</param>
    private void ReportError(
        IProgress<SchedulingProgressReport>? progress,
        string errorMessage)
    {
        if (progress == null)
            return;

        var report = new SchedulingProgressReport
        {
            CurrentStage = SchedulingStage.Failed,
            ProgressPercentage = 0,
            StageDescription = errorMessage,
            HasErrors = true,
            ErrorMessage = errorMessage
        };

        progress.Report(report);
    }
}
