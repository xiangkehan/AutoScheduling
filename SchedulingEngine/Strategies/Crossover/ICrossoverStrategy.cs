using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine.Strategies.Crossover;

/// <summary>
/// 交叉策略接口 - 对应需求4.1, 4.3, 9.1, 9.3
/// 定义两个父代个体交叉生成后代的策略
/// </summary>
public interface ICrossoverStrategy
{
    /// <summary>
    /// 执行交叉操作，生成两个后代个体
    /// </summary>
    /// <param name="parent1">父代个体1</param>
    /// <param name="parent2">父代个体2</param>
    /// <param name="context">调度上下文</param>
    /// <param name="random">随机数生成器</param>
    /// <returns>两个后代个体</returns>
    (Individual child1, Individual child2) Crossover(
        Individual parent1,
        Individual parent2,
        SchedulingContext context,
        Random random);
}
