using System;
using System.Collections.Generic;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine.Strategies.Crossover;

/// <summary>
/// 均匀交叉策略 - 对应需求4.1, 4.3, 4.4, 4.5, 9.3
/// 对每个基因位，随机选择来自父代1或父代2
/// </summary>
public class UniformCrossover : ICrossoverStrategy
{
    private readonly ConstraintValidator _constraintValidator;
    private readonly int _maxRepairAttempts;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="constraintValidator">约束验证器</param>
    /// <param name="maxRepairAttempts">最大修复尝试次数（默认3）</param>
    public UniformCrossover(ConstraintValidator constraintValidator, int maxRepairAttempts = 3)
    {
        _constraintValidator = constraintValidator ?? throw new ArgumentNullException(nameof(constraintValidator));
        _maxRepairAttempts = maxRepairAttempts;
    }

    /// <summary>
    /// 执行均匀交叉操作
    /// </summary>
    public (Individual child1, Individual child2) Crossover(
        Individual parent1,
        Individual parent2,
        SchedulingContext context,
        Random random)
    {
        if (parent1 == null || parent2 == null)
            throw new ArgumentNullException("父代个体不能为空");

        // 创建两个后代个体
        var child1 = new Individual();
        var child2 = new Individual();

        // 对每个日期的基因进行交叉
        foreach (var date in parent1.Genes.Keys)
        {
            if (!parent2.Genes.ContainsKey(date))
                continue;

            var parent1Genes = parent1.Genes[date];
            var parent2Genes = parent2.Genes[date];

            int periods = parent1Genes.GetLength(0);
            int positions = parent1Genes.GetLength(1);

            child1.Genes[date] = new int[periods, positions];
            child2.Genes[date] = new int[periods, positions];

            // 对每个基因位（时段-哨位组合）随机选择父代
            for (int periodIdx = 0; periodIdx < periods; periodIdx++)
            {
                for (int positionIdx = 0; positionIdx < positions; positionIdx++)
                {
                    // 随机决定基因来源
                    if (random.NextDouble() < 0.5)
                    {
                        // 来自父代1
                        child1.Genes[date][periodIdx, positionIdx] = parent1Genes[periodIdx, positionIdx];
                        child2.Genes[date][periodIdx, positionIdx] = parent2Genes[periodIdx, positionIdx];
                    }
                    else
                    {
                        // 来自父代2
                        child1.Genes[date][periodIdx, positionIdx] = parent2Genes[periodIdx, positionIdx];
                        child2.Genes[date][periodIdx, positionIdx] = parent1Genes[periodIdx, positionIdx];
                    }
                }
            }
        }

        // 识别未分配时段
        child1.IdentifyUnassignedSlots();
        child2.IdentifyUnassignedSlots();

        // 尝试修复约束违反 - 对应需求4.4, 4.5
        TryRepairConstraintViolations(child1, context, random);
        TryRepairConstraintViolations(child2, context, random);

        return (child1, child2);
    }

    /// <summary>
    /// 尝试修复约束违反 - 对应需求4.5
    /// </summary>
    private void TryRepairConstraintViolations(Individual individual, SchedulingContext context, Random random)
    {
        int attempts = 0;

        while (attempts < _maxRepairAttempts)
        {
            bool hasViolation = false;

            // 检查所有分配
            foreach (var (date, assignments) in individual.Genes)
            {
                int periods = assignments.GetLength(0);
                int positions = assignments.GetLength(1);

                for (int periodIdx = 0; periodIdx < periods; periodIdx++)
                {
                    for (int positionIdx = 0; positionIdx < positions; positionIdx++)
                    {
                        int personIdx = assignments[periodIdx, positionIdx];

                        // 跳过未分配
                        if (personIdx == -1)
                            continue;

                        // 检查约束
                        if (!_constraintValidator.ValidateAllConstraints(personIdx, positionIdx, periodIdx, date))
                        {
                            hasViolation = true;

                            // 尝试修复：设置为未分配
                            individual.Genes[date][periodIdx, positionIdx] = -1;
                        }
                    }
                }
            }

            // 如果没有违反，退出
            if (!hasViolation)
                break;

            attempts++;
        }

        // 重新识别未分配时段
        individual.IdentifyUnassignedSlots();
    }
}
