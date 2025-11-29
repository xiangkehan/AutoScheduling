using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using AutoScheduling3.Models;
using AutoScheduling3.SchedulingEngine.Config;
using AutoScheduling3.SchedulingEngine.Core;
using AutoScheduling3.SchedulingEngine.Strategies.Crossover;
using AutoScheduling3.SchedulingEngine.Strategies.Mutation;
using AutoScheduling3.SchedulingEngine.Strategies.Selection;

namespace AutoScheduling3.SchedulingEngine;

/// <summary>
/// 遗传算法调度器 - 对应需求1.2-1.4, 2.1-2.5, 6.1-6.5
/// 使用遗传算法优化排班方案
/// </summary>
public class GeneticScheduler
{
    private readonly SchedulingContext _context;
    private readonly GeneticSchedulerConfig _config;
    private readonly FitnessEvaluator _fitnessEvaluator;
    private readonly ISelectionStrategy _selectionStrategy;
    private readonly ICrossoverStrategy _crossoverStrategy;
    private readonly IMutationStrategy _mutationStrategy;
    private readonly Random _random;

    public GeneticScheduler(
        SchedulingContext context,
        GeneticSchedulerConfig config,
        FitnessEvaluator fitnessEvaluator,
        ISelectionStrategy selectionStrategy,
        ICrossoverStrategy crossoverStrategy,
        IMutationStrategy mutationStrategy,
        Random? random = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _fitnessEvaluator = fitnessEvaluator ?? throw new ArgumentNullException(nameof(fitnessEvaluator));
        _selectionStrategy = selectionStrategy ?? throw new ArgumentNullException(nameof(selectionStrategy));
        _crossoverStrategy = crossoverStrategy ?? throw new ArgumentNullException(nameof(crossoverStrategy));
        _mutationStrategy = mutationStrategy ?? throw new ArgumentNullException(nameof(mutationStrategy));
        _random = random ?? new Random();

        // 更新适应度评估器的惩罚权重
        _fitnessEvaluator.UpdatePenaltyWeights(
            _config.HardConstraintPenaltyWeight,
            _config.UnassignedPenaltyWeight);
    }

    /// <summary>
    /// 执行遗传算法 - 对应需求1.2, 1.3, 1.4
    /// </summary>
    /// <param name="initialSolution">初始解（来自贪心算法）</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>优化后的排班方案</returns>
    public async Task<Schedule> ExecuteAsync(
        Schedule? initialSolution,
        IProgress<SchedulingProgressReport>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            // 报告初始化阶段 - 对应需求6.1
            ReportProgress(progress, SchedulingStage.Initializing, 0, null);

            // 初始化种群 - 对应需求1.3, 2.1
            var population = new Population(_random);
            population.Initialize(initialSolution, _config.PopulationSize, _context);

            // 评估初始种群的适应度
            EvaluatePopulation(population);
            population.UpdateStatistics();

            if (_config.EnableDetailedLogging)
            {
                LogGeneration(0, population);
            }

            // 报告初始化完成
            ReportProgress(progress, SchedulingStage.GeneticOptimizing, 0, population);

            // 遗传算法主循环 - 对应需求2.2
            for (int generation = 1; generation <= _config.MaxGenerations; generation++)
            {
                // 检查取消令牌 - 对应需求6.4
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // 创建新一代种群
                var newPopulation = new Population(_random);

                // 精英保留 - 对应需求2.5
                var elites = population.GetElites(_config.EliteCount);
                newPopulation.Individuals.AddRange(elites);

                // 生成新个体直到达到种群大小
                while (newPopulation.Individuals.Count < _config.PopulationSize)
                {
                    // 检查取消令牌
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // 选择父代 - 对应需求2.3
                    var parent1 = _selectionStrategy.Select(population, _random);
                    var parent2 = _selectionStrategy.Select(population, _random);

                    // 交叉操作 - 对应需求2.4
                    Individual child1, child2;
                    if (_random.NextDouble() < _config.CrossoverRate)
                    {
                        (child1, child2) = _crossoverStrategy.Crossover(parent1, parent2, _context, _random);
                    }
                    else
                    {
                        // 不交叉，直接复制父代
                        child1 = parent1.Clone();
                        child2 = parent2.Clone();
                    }

                    // 变异操作 - 对应需求2.4
                    if (_random.NextDouble() < _config.MutationRate)
                    {
                        _mutationStrategy.Mutate(child1, _context, _random);
                    }

                    if (_random.NextDouble() < _config.MutationRate)
                    {
                        _mutationStrategy.Mutate(child2, _context, _random);
                    }

                    // 添加到新种群
                    newPopulation.Individuals.Add(child1);
                    if (newPopulation.Individuals.Count < _config.PopulationSize)
                    {
                        newPopulation.Individuals.Add(child2);
                    }
                }

                // 评估新种群的适应度
                EvaluatePopulation(newPopulation);
                newPopulation.UpdateStatistics();

                // 替换旧种群
                population = newPopulation;

                // 详细日志
                if (_config.EnableDetailedLogging)
                {
                    LogGeneration(generation, population);
                }

                // 报告进度 - 对应需求6.2, 6.5
                double progressPercentage = (double)generation / _config.MaxGenerations * 100;
                ReportProgress(progress, SchedulingStage.GeneticOptimizing, progressPercentage, population);

                // 检查取消令牌
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            // 返回最优解 - 对应需求1.4
            if (population.BestIndividual == null)
            {
                throw new InvalidOperationException("遗传算法未能生成有效解");
            }

            var bestSchedule = population.BestIndividual.ToSchedule(_context);

            // 报告完成
            ReportProgress(progress, SchedulingStage.Completed, 100, population);

            return bestSchedule;
        }
        catch (OperationCanceledException)
        {
            // 取消操作，返回当前最优解（如果有）
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"遗传算法执行失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 评估种群中所有个体的适应度
    /// </summary>
    /// <param name="population">种群</param>
    private void EvaluatePopulation(Population population)
    {
        // 串行评估（后续可优化为并行）
        foreach (var individual in population.Individuals)
        {
            _fitnessEvaluator.EvaluateFitness(individual);
        }
    }

    /// <summary>
    /// 报告进度 - 对应需求6.1, 6.2, 6.5
    /// </summary>
    /// <param name="progress">进度报告接口</param>
    /// <param name="stage">当前阶段</param>
    /// <param name="progressPercentage">进度百分比</param>
    /// <param name="population">当前种群（可选）</param>
    private void ReportProgress(
        IProgress<SchedulingProgressReport>? progress,
        SchedulingStage stage,
        double progressPercentage,
        Population? population)
    {
        if (progress == null)
            return;

        var report = new SchedulingProgressReport
        {
            CurrentStage = stage,
            ProgressPercentage = progressPercentage,
            StageDescription = GetStageMessage(stage, progressPercentage)
        };

        // 添加遗传算法进度信息
        if (population != null && population.BestIndividual != null)
        {
            report.GeneticProgressInfo = new GeneticProgressInfo
            {
                CurrentGeneration = (int)(progressPercentage / 100 * _config.MaxGenerations),
                MaxGenerations = _config.MaxGenerations,
                BestFitness = population.BestIndividual.Fitness,
                AverageFitness = population.AverageFitness,
                BestHardConstraintViolations = population.BestIndividual.HardConstraintViolations,
                BestUnassignedSlots = population.BestIndividual.UnassignedSlots
            };
        }

        progress.Report(report);
    }

    /// <summary>
    /// 获取阶段消息
    /// </summary>
    /// <param name="stage">阶段</param>
    /// <param name="progressPercentage">进度百分比</param>
    /// <returns>消息文本</returns>
    private string GetStageMessage(SchedulingStage stage, double progressPercentage)
    {
        return stage switch
        {
            SchedulingStage.Initializing => "正在初始化遗传算法...",
            SchedulingStage.GeneticOptimizing => $"正在优化排班方案... {progressPercentage:F1}%",
            SchedulingStage.Completed => "遗传算法优化完成",
            _ => "处理中..."
        };
    }

    /// <summary>
    /// 记录代数日志
    /// </summary>
    /// <param name="generation">代数</param>
    /// <param name="population">种群</param>
    private void LogGeneration(int generation, Population population)
    {
        if (population.BestIndividual == null)
            return;

        var best = population.BestIndividual;
        System.Diagnostics.Debug.WriteLine(
            $"代数 {generation}: " +
            $"最优适应度={best.Fitness:F4}, " +
            $"平均适应度={population.AverageFitness:F4}, " +
            $"硬约束违反={best.HardConstraintViolations}, " +
            $"未分配={best.UnassignedSlots}");
    }
}
