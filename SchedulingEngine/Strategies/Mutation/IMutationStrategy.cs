using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine.Strategies.Mutation;

/// <summary>
/// 变异策略接口 - 对应需求5.1, 5.2, 9.1, 9.4
/// 定义个体变异的策略
/// </summary>
public interface IMutationStrategy
{
    /// <summary>
    /// 对个体执行变异操作
    /// </summary>
    /// <param name="individual">待变异的个体</param>
    /// <param name="context">调度上下文</param>
    /// <param name="random">随机数生成器</param>
    void Mutate(
        Individual individual,
        SchedulingContext context,
        Random random);
}
