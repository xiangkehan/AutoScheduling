using System;
using System.Collections.Generic;
using System.Linq;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.SchedulingEngine.Strategies.Mutation;

/// <summary>
/// 交换变异策略 - 对应需求5.1, 5.2, 5.3, 5.4, 5.5, 8.4, 9.4
/// 随机选择班次，从可行人员中选择新人员进行替换
/// 优先填补未分配时段
/// </summary>
public class SwapMutation : IMutationStrategy
{
    private readonly ConstraintValidator _constraintValidator;
    private readonly FeasibilityTensor _feasibilityTensor;
    private readonly int _mutationCount;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="constraintValidator">约束验证器</param>
    /// <param name="feasibilityTensor">可行性张量</param>
    /// <param name="mutationCount">每次变异的班次数量（默认3）</param>
    public SwapMutation(
        ConstraintValidator constraintValidator,
        FeasibilityTensor feasibilityTensor,
        int mutationCount = 3)
    {
        _constraintValidator = constraintValidator ?? throw new ArgumentNullException(nameof(constraintValidator));
        _feasibilityTensor = feasibilityTensor ?? throw new ArgumentNullException(nameof(feasibilityTensor));
        _mutationCount = mutationCount;
    }

    /// <summary>
    /// 对个体执行变异操作
    /// </summary>
    public void Mutate(Individual individual, SchedulingContext context, Random random)
    {
        if (individual == null)
            throw new ArgumentNullException(nameof(individual));

        // 收集所有未分配时段 - 对应需求8.4
        var unassignedSlots = CollectUnassignedSlots(individual);

        // 收集所有已分配时段
        var assignedSlots = CollectAssignedSlots(individual);

        // 优先变异未分配时段 - 对应需求8.4
        int unassignedMutations = Math.Min(_mutationCount, unassignedSlots.Count);
        int assignedMutations = _mutationCount - unassignedMutations;

        // 变异未分配时段
        for (int i = 0; i < unassignedMutations && unassignedSlots.Count > 0; i++)
        {
            int randomIndex = random.Next(unassignedSlots.Count);
            var slot = unassignedSlots[randomIndex];
            unassignedSlots.RemoveAt(randomIndex);

            MutateSlot(individual, slot.date, slot.periodIdx, slot.positionIdx, context, random);
        }

        // 变异已分配时段
        for (int i = 0; i < assignedMutations && assignedSlots.Count > 0; i++)
        {
            int randomIndex = random.Next(assignedSlots.Count);
            var slot = assignedSlots[randomIndex];
            assignedSlots.RemoveAt(randomIndex);

            MutateSlot(individual, slot.date, slot.periodIdx, slot.positionIdx, context, random);
        }

        // 重新识别未分配时段
        individual.IdentifyUnassignedSlots();
    }

    /// <summary>
    /// 收集所有未分配时段
    /// </summary>
    private List<(DateTime date, int periodIdx, int positionIdx)> CollectUnassignedSlots(Individual individual)
    {
        var slots = new List<(DateTime, int, int)>();

        foreach (var (date, assignments) in individual.Genes)
        {
            int periods = assignments.GetLength(0);
            int positions = assignments.GetLength(1);

            for (int periodIdx = 0; periodIdx < periods; periodIdx++)
            {
                for (int positionIdx = 0; positionIdx < positions; positionIdx++)
                {
                    if (assignments[periodIdx, positionIdx] == -1)
                    {
                        slots.Add((date, periodIdx, positionIdx));
                    }
                }
            }
        }

        return slots;
    }

    /// <summary>
    /// 收集所有已分配时段
    /// </summary>
    private List<(DateTime date, int periodIdx, int positionIdx)> CollectAssignedSlots(Individual individual)
    {
        var slots = new List<(DateTime, int, int)>();

        foreach (var (date, assignments) in individual.Genes)
        {
            int periods = assignments.GetLength(0);
            int positions = assignments.GetLength(1);

            for (int periodIdx = 0; periodIdx < periods; periodIdx++)
            {
                for (int positionIdx = 0; positionIdx < positions; positionIdx++)
                {
                    if (assignments[periodIdx, positionIdx] != -1)
                    {
                        slots.Add((date, periodIdx, positionIdx));
                    }
                }
            }
        }

        return slots;
    }

    /// <summary>
    /// 变异单个时段 - 对应需求5.2, 5.3, 5.4, 5.5
    /// </summary>
    private void MutateSlot(
        Individual individual,
        DateTime date,
        int periodIdx,
        int positionIdx,
        SchedulingContext context,
        Random random)
    {
        // 保存原始分配以便回滚
        int originalPersonIdx = individual.Genes[date][periodIdx, positionIdx];

        // 获取该时段的可行人员 - 对应需求5.3
        var feasiblePersons = _feasibilityTensor.GetFeasiblePersons(date, periodIdx, positionIdx);

        if (feasiblePersons == null || feasiblePersons.Length == 0)
        {
            // 无可行人员，设置为未分配
            individual.Genes[date][periodIdx, positionIdx] = -1;
            return;
        }

        // 过滤掉原始人员（如果有）
        var candidates = feasiblePersons.Where(p => p != originalPersonIdx).ToArray();

        if (candidates.Length == 0)
        {
            // 只有原始人员可行，保持不变或设置为未分配
            if (originalPersonIdx == -1)
            {
                // 尝试分配唯一的可行人员
                individual.Genes[date][periodIdx, positionIdx] = feasiblePersons[0];
            }
            return;
        }

        // 随机选择一个新人员 - 对应需求5.2, 5.3
        int newPersonIdx = candidates[random.Next(candidates.Length)];

        // 尝试分配新人员
        individual.Genes[date][periodIdx, positionIdx] = newPersonIdx;

        // 验证约束 - 对应需求5.4
        if (!_constraintValidator.ValidateAllConstraints(newPersonIdx, positionIdx, periodIdx, date))
        {
            // 约束违反，回滚 - 对应需求5.5
            individual.Genes[date][periodIdx, positionIdx] = originalPersonIdx;
        }
    }
}
