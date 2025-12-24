using System;
using System.Linq;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine.Strategies.Selection;

/// <summary>
/// 轮盘赌选择策略 - 对应需求4.2, 9.2
/// 根据适应度比例选择个体，适应度越高被选中概率越大
/// </summary>
public class RouletteWheelSelection : ISelectionStrategy
{
    /// <summary>
    /// 从种群中选择一个个体
    /// </summary>
    /// <param name="population">种群</param>
    /// <param name="random">随机数生成器</param>
    /// <returns>被选中的个体</returns>
    public Individual Select(Population population, Random random)
    {
        if (population == null || population.Individuals.Count == 0)
            throw new ArgumentException("种群为空或无个体", nameof(population));

        // 处理所有适应度都为负数的情况
        double minFitness = population.Individuals.Min(i => i.Fitness);
        double offset = minFitness < 0 ? Math.Abs(minFitness) + 1 : 0;

        // 计算调整后的适应度总和
        double totalFitness = population.Individuals.Sum(i => i.Fitness + offset);

        // 如果总适应度为0或负数，随机选择
        if (totalFitness <= 0)
        {
            int randomIndex = random.Next(population.Individuals.Count);
            return population.Individuals[randomIndex];
        }

        // 生成随机数（0到totalFitness之间）
        double randomValue = random.NextDouble() * totalFitness;

        // 累加适应度，找到对应的个体
        double cumulativeFitness = 0;
        foreach (var individual in population.Individuals)
        {
            cumulativeFitness += individual.Fitness + offset;
            if (cumulativeFitness >= randomValue)
            {
                return individual;
            }
        }

        // 理论上不应该到达这里，但为了安全返回最后一个个体
        return population.Individuals[^1];
    }
}
