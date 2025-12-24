using System;
using System.Collections.Generic;
using System.Linq;
using AutoScheduling3.Models;

namespace AutoScheduling3.SchedulingEngine.Core;

/// <summary>
/// 种群类，管理遗传算法中的多个个体
/// </summary>
public class Population
{
    /// <summary>
    /// 种群中的所有个体
    /// </summary>
    public List<Individual> Individuals { get; set; } = new();

    /// <summary>
    /// 当前种群中的最优个体
    /// </summary>
    public Individual? BestIndividual { get; private set; }

    /// <summary>
    /// 当前种群的平均适应度
    /// </summary>
    public double AverageFitness { get; private set; }

    /// <summary>
    /// 随机数生成器
    /// </summary>
    private readonly Random _random;

    public Population(Random? random = null)
    {
        _random = random ?? new Random();
    }

    /// <summary>
    /// 初始化种群
    /// </summary>
    /// <param name="initialSolution">初始解（来自贪心算法）</param>
    /// <param name="populationSize">种群大小</param>
    /// <param name="context">调度上下文</param>
    public void Initialize(Schedule? initialSolution, int populationSize, SchedulingContext context)
    {
        Individuals.Clear();

        // 1. 如果有初始解，将其加入种群
        if (initialSolution != null)
        {
            var initialIndividual = Individual.FromSchedule(initialSolution, context);
            Individuals.Add(initialIndividual);
        }

        // 2. 生成随机个体填充剩余种群
        int remainingCount = populationSize - Individuals.Count;
        for (int i = 0; i < remainingCount; i++)
        {
            var randomIndividual = CreateRandomIndividual(context);
            Individuals.Add(randomIndividual);
        }
    }

    /// <summary>
    /// 创建随机个体
    /// </summary>
    /// <param name="context">调度上下文</param>
    /// <returns>随机生成的个体</returns>
    private Individual CreateRandomIndividual(SchedulingContext context)
    {
        var individual = new Individual();

        // 初始化基因编码
        var currentDate = context.StartDate.Date;
        while (currentDate <= context.EndDate.Date)
        {
            individual.Genes[currentDate] = new int[12, context.Positions.Count];

            // 为每个时段和哨位随机分配人员
            for (int periodIdx = 0; periodIdx < 12; periodIdx++)
            {
                for (int positionIdx = 0; positionIdx < context.Positions.Count; positionIdx++)
                {
                    // 随机决定是否分配（80%概率分配）
                    if (_random.NextDouble() < 0.8)
                    {
                        // 随机选择一个人员
                        int personIdx = _random.Next(context.Personals.Count);
                        individual.Genes[currentDate][periodIdx, positionIdx] = personIdx;
                    }
                    else
                    {
                        // 未分配
                        individual.Genes[currentDate][periodIdx, positionIdx] = -1;
                    }
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        // 识别未分配时段
        individual.IdentifyUnassignedSlots();

        return individual;
    }

    /// <summary>
    /// 更新种群统计信息
    /// </summary>
    public void UpdateStatistics()
    {
        if (Individuals.Count == 0)
        {
            BestIndividual = null;
            AverageFitness = 0;
            return;
        }

        // 找到最优个体
        BestIndividual = Individuals.OrderByDescending(i => i.Fitness).First();

        // 计算平均适应度
        AverageFitness = Individuals.Average(i => i.Fitness);
    }

    /// <summary>
    /// 获取精英个体（适应度最高的若干个体）
    /// </summary>
    /// <param name="count">精英个体数量</param>
    /// <returns>精英个体列表</returns>
    public List<Individual> GetElites(int count)
    {
        if (count <= 0 || Individuals.Count == 0)
            return new List<Individual>();

        // 按适应度降序排序，取前 count 个
        return Individuals
            .OrderByDescending(i => i.Fitness)
            .Take(count)
            .Select(i => i.Clone()) // 返回克隆以避免修改原个体
            .ToList();
    }
}
