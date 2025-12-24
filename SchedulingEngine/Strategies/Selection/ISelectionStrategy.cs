using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine.Strategies.Selection;

/// <summary>
/// 选择策略接口 - 对应需求4.2, 9.1, 9.2
/// 定义从种群中选择个体的策略
/// </summary>
public interface ISelectionStrategy
{
    /// <summary>
    /// 从种群中选择一个个体
    /// </summary>
    /// <param name="population">种群</param>
    /// <param name="random">随机数生成器</param>
    /// <returns>被选中的个体</returns>
    Individual Select(Population population, Random random);
}
