using System;
using System.Linq;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine.Strategies.Selection;

/// <summary>
/// 锦标赛选择策略 - 对应需求4.2, 9.2
/// 随机选择若干个体进行竞争，返回其中适应度最高的
/// </summary>
public class TournamentSelection : ISelectionStrategy
{
    private readonly int _tournamentSize;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tournamentSize">锦标赛大小（默认5）</param>
    public TournamentSelection(int tournamentSize = 5)
    {
        if (tournamentSize <= 0)
            throw new ArgumentException("锦标赛大小必须大于0", nameof(tournamentSize));

        _tournamentSize = tournamentSize;
    }

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

        // 确定实际的锦标赛大小（不超过种群大小）
        int actualTournamentSize = Math.Min(_tournamentSize, population.Individuals.Count);

        // 随机选择 tournamentSize 个个体
        var tournament = new Individual[actualTournamentSize];
        for (int i = 0; i < actualTournamentSize; i++)
        {
            int randomIndex = random.Next(population.Individuals.Count);
            tournament[i] = population.Individuals[randomIndex];
        }

        // 返回适应度最高的个体
        // 如果适应度相同，使用稳定的比较规则（按索引）- 对应需求3.5
        return tournament.OrderByDescending(i => i.Fitness).First();
    }
}
