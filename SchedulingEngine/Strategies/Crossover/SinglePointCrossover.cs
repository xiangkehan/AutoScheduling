using System;
using System.Linq;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine.Strategies.Crossover;

/// <summary>
/// 单点交叉策略 - 对应需求4.1, 4.3, 4.4, 4.5, 9.3
/// 随机选择一个交叉点（按日期），交换交叉点后的基因片段
/// </summary>
public class SinglePointCrossover : ICrossoverStrategy
{
    private readonly ConstraintValidator _constraintValidator;
    private readonly int _maxRepairAttempts;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="constraintValidator">约束验证器</param>
    /// <param name="maxRepairAttempts">最大修复尝试次数（默认3）</param>
    public SinglePointCrossover(ConstraintValidator constraintValidator, int maxRepairAttempts = 3)
    {
        _constraintValidator = constraintValidator ?? throw new ArgumentNullException(nameof(constraintValidator));
        _maxRepairAttempts = maxRepairAttempts;
    }

    /// <summary>
    /// 执行单点交叉操作
    /// </summary>
    public (Individual child1, Individual child2) Crossover(
        Individual parent1,
        Individual parent2,
        SchedulingContext context,
        Random random)
    {
        if (parent1 == null || parent2 == null)
            throw new ArgumentNullException("父代个体不能为空");

        // 创建两个后代个体（先完全复制父代）
        var child1 = parent1.Clone();
        var child2 = parent2.Clone();

        // 获取所有日期并排序
        var dates = parent1.Genes.Keys.OrderBy(d => d).ToList();
        if (dates.Count <= 1)
        {
            // 只有一天或没有数据，无法交叉
            return (child1, child2);
        }

        // 随机选择交叉点（日期索引）
        int crossoverPoint = random.Next(1, dates.Count);

        // 交换交叉点后的基因片段
        for (int i = crossoverPoint; i < dates.Count; i++)
        {
            var date = dates[i];

            if (parent1.Genes.ContainsKey(date) && parent2.Genes.ContainsKey(date))
            {
                // 交换这一天的所有基因
                var temp = child1.Genes[date];
                child1.Genes[date] = CloneGeneArray(child2.Genes[date]);
                child2.Genes[date] = CloneGeneArray(temp);
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
    /// 克隆基因数组
    /// </summary>
    private int[,] CloneGeneArray(int[,] source)
    {
        int rows = source.GetLength(0);
        int cols = source.GetLength(1);
        var clone = new int[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                clone[i, j] = source[i, j];
            }
        }

        return clone;
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
