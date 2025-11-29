using System;
using System.Collections.Generic;
using System.Linq;
using AutoScheduling3.Models;

namespace AutoScheduling3.SchedulingEngine.Core;

/// <summary>
/// 适应度评估器 - 对应需求3.1-3.5
/// 计算个体的适应度值，综合硬约束违反、软约束得分和未分配时段惩罚
/// </summary>
public class FitnessEvaluator
{
    private readonly SchedulingContext _context;
    private readonly ConstraintValidator _constraintValidator;
    private readonly SoftConstraintCalculator _softConstraintCalculator;

    /// <summary>
    /// 硬约束违反惩罚权重（默认10000）
    /// </summary>
    public double HardConstraintPenaltyWeight { get; set; } = 10000.0;

    /// <summary>
    /// 未分配时段惩罚权重（默认1000）
    /// </summary>
    public double UnassignedPenaltyWeight { get; set; } = 1000.0;

    public FitnessEvaluator(
        SchedulingContext context,
        ConstraintValidator constraintValidator,
        SoftConstraintCalculator softConstraintCalculator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _constraintValidator = constraintValidator ?? throw new ArgumentNullException(nameof(constraintValidator));
        _softConstraintCalculator = softConstraintCalculator ?? throw new ArgumentNullException(nameof(softConstraintCalculator));
    }

    /// <summary>
    /// 评估个体适应度 - 对应需求3.1, 3.4
    /// </summary>
    /// <param name="individual">待评估的个体</param>
    public void EvaluateFitness(Individual individual)
    {
        if (individual == null)
            throw new ArgumentNullException(nameof(individual));

        // 计算硬约束违反数量 - 对应需求3.1
        int hardViolations = CalculateHardConstraintViolations(individual);
        individual.HardConstraintViolations = hardViolations;

        // 计算软约束得分 - 对应需求3.3
        double softScore = CalculateSoftConstraintScore(individual);
        individual.SoftConstraintScore = softScore;

        // 计算未分配时段惩罚
        double unassignedPenalty = CalculateUnassignedPenalty(individual);

        // 综合计算适应度 - 对应需求3.4
        // 适应度 = 软约束得分 - 硬约束惩罚 - 未分配惩罚
        // 硬约束违反会施加严重惩罚 - 对应需求3.2
        individual.Fitness = softScore 
                           - (hardViolations * HardConstraintPenaltyWeight) 
                           - unassignedPenalty;
    }

    /// <summary>
    /// 计算硬约束违反数量 - 对应需求3.1
    /// </summary>
    /// <param name="individual">个体</param>
    /// <returns>硬约束违反总数</returns>
    private int CalculateHardConstraintViolations(Individual individual)
    {
        int violations = 0;

        // 创建临时上下文以避免修改原始上下文
        // 注意：这里简化处理，直接检查约束而不修改上下文
        // 实际约束验证应该基于个体的基因而不是上下文的分配状态

        // 遍历所有分配，检查硬约束
        foreach (var (date, assignments) in individual.Genes)
        {
            int periods = assignments.GetLength(0);
            int positions = assignments.GetLength(1);

            for (int periodIdx = 0; periodIdx < periods; periodIdx++)
            {
                for (int positionIdx = 0; positionIdx < positions; positionIdx++)
                {
                    int personIdx = assignments[periodIdx, positionIdx];

                    // 跳过未分配的时段
                    if (personIdx == -1)
                        continue;

                    // 检查基本约束（不依赖上下文分配状态的约束）
                    if (!ValidateBasicConstraints(personIdx, positionIdx, periodIdx, date))
                    {
                        violations++;
                    }

                    // 检查与其他分配冲突的约束（基于个体的基因）
                    if (HasConflictWithOtherAssignments(individual, date, periodIdx, positionIdx, personIdx))
                    {
                        violations++;
                    }
                }
            }
        }

        return violations;
    }

    /// <summary>
    /// 验证基本约束（不依赖上下文分配状态）
    /// </summary>
    private bool ValidateBasicConstraints(int personIdx, int positionIdx, int periodIdx, DateTime date)
    {
        // 人员可用性
        if (!_constraintValidator.ValidatePersonnelAvailability(personIdx))
            return false;

        // 人员-哨位可用性
        if (!_constraintValidator.ValidatePersonnelPositionAvailability(personIdx, positionIdx))
            return false;

        // 技能匹配
        if (!_constraintValidator.ValidateSkillMatch(personIdx, positionIdx))
            return false;

        // 定岗要求
        if (!_constraintValidator.ValidateFixedAssignment(personIdx, positionIdx, periodIdx, date))
            return false;

        // 手动指定
        if (!_constraintValidator.ValidateManualAssignment(personIdx, positionIdx, periodIdx, date))
            return false;

        return true;
    }

    /// <summary>
    /// 检查与其他分配的冲突（基于个体的基因）
    /// </summary>
    private bool HasConflictWithOtherAssignments(
        Individual individual,
        DateTime date,
        int periodIdx,
        int positionIdx,
        int personIdx)
    {
        var assignments = individual.Genes[date];
        int periods = assignments.GetLength(0);
        int positions = assignments.GetLength(1);

        // 检查人员时段唯一性（同一时段不能在多个哨位）
        for (int otherPosIdx = 0; otherPosIdx < positions; otherPosIdx++)
        {
            if (otherPosIdx == positionIdx)
                continue;

            if (assignments[periodIdx, otherPosIdx] == personIdx)
                return true; // 冲突
        }

        // 检查夜哨唯一性（同一晚上只能一个夜哨）
        int[] nightPeriods = { 11, 0, 1, 2 };
        if (nightPeriods.Contains(periodIdx))
        {
            foreach (var nightPeriod in nightPeriods)
            {
                if (nightPeriod == periodIdx)
                    continue;

                for (int posIdx = 0; posIdx < positions; posIdx++)
                {
                    if (assignments[nightPeriod, posIdx] == personIdx)
                        return true; // 冲突
                }
            }
        }

        // 检查时段不连续（相邻时段不能连续上哨）
        // 检查前一个时段
        if (periodIdx > 0)
        {
            for (int posIdx = 0; posIdx < positions; posIdx++)
            {
                if (assignments[periodIdx - 1, posIdx] == personIdx)
                    return true; // 冲突
            }
        }

        // 检查后一个时段
        if (periodIdx < periods - 1)
        {
            for (int posIdx = 0; posIdx < positions; posIdx++)
            {
                if (assignments[periodIdx + 1, posIdx] == personIdx)
                    return true; // 冲突
            }
        }

        // 跨日检查
        if (periodIdx == 0 && individual.Genes.ContainsKey(date.AddDays(-1)))
        {
            var prevDayAssignments = individual.Genes[date.AddDays(-1)];
            for (int posIdx = 0; posIdx < positions; posIdx++)
            {
                if (prevDayAssignments[11, posIdx] == personIdx)
                    return true; // 冲突
            }
        }

        if (periodIdx == periods - 1 && individual.Genes.ContainsKey(date.AddDays(1)))
        {
            var nextDayAssignments = individual.Genes[date.AddDays(1)];
            for (int posIdx = 0; posIdx < positions; posIdx++)
            {
                if (nextDayAssignments[0, posIdx] == personIdx)
                    return true; // 冲突
            }
        }

        return false; // 无冲突
    }

    /// <summary>
    /// 计算软约束得分 - 对应需求3.3
    /// </summary>
    /// <param name="individual">个体</param>
    /// <returns>软约束总得分</returns>
    private double CalculateSoftConstraintScore(Individual individual)
    {
        double totalScore = 0.0;
        int assignmentCount = 0;

        // 遍历所有分配，累加软约束得分
        foreach (var (date, assignments) in individual.Genes)
        {
            int periods = assignments.GetLength(0);
            int positions = assignments.GetLength(1);

            for (int periodIdx = 0; periodIdx < periods; periodIdx++)
            {
                for (int positionIdx = 0; positionIdx < positions; positionIdx++)
                {
                    int personIdx = assignments[periodIdx, positionIdx];

                    // 跳过未分配的时段
                    if (personIdx == -1)
                        continue;

                    // 计算该分配的软约束得分
                    double score = _softConstraintCalculator.CalculateTotalScore(personIdx, periodIdx, date);
                    totalScore += score;
                    assignmentCount++;
                }
            }
        }

        // 返回平均得分（避免因分配数量不同导致的不公平比较）
        return assignmentCount > 0 ? totalScore / assignmentCount : 0.0;
    }

    /// <summary>
    /// 计算未分配时段惩罚 - 对应需求8.3
    /// </summary>
    /// <param name="individual">个体</param>
    /// <returns>未分配惩罚值</returns>
    private double CalculateUnassignedPenalty(Individual individual)
    {
        // 未分配时段数量已在 Individual 中计算
        return individual.UnassignedSlots * UnassignedPenaltyWeight;
    }

    /// <summary>
    /// 批量评估多个个体的适应度（支持并行）
    /// </summary>
    /// <param name="individuals">个体列表</param>
    public void EvaluateFitnessBatch(List<Individual> individuals)
    {
        if (individuals == null || individuals.Count == 0)
            return;

        // 串行评估（后续可优化为并行）
        foreach (var individual in individuals)
        {
            EvaluateFitness(individual);
        }
    }

    /// <summary>
    /// 更新惩罚权重
    /// </summary>
    /// <param name="hardConstraintWeight">硬约束惩罚权重</param>
    /// <param name="unassignedWeight">未分配惩罚权重</param>
    public void UpdatePenaltyWeights(double hardConstraintWeight, double unassignedWeight)
    {
        HardConstraintPenaltyWeight = Math.Max(0, hardConstraintWeight);
        UnassignedPenaltyWeight = Math.Max(0, unassignedWeight);
    }

    /// <summary>
    /// 获取适应度评估详情
    /// </summary>
    /// <param name="individual">个体</param>
    /// <returns>详情字符串</returns>
    public string GetFitnessDetails(Individual individual)
    {
        return $"适应度评估详情:\n" +
               $"  硬约束违反数量: {individual.HardConstraintViolations}\n" +
               $"  硬约束惩罚: {individual.HardConstraintViolations * HardConstraintPenaltyWeight:F2}\n" +
               $"  软约束得分: {individual.SoftConstraintScore:F4}\n" +
               $"  未分配时段数量: {individual.UnassignedSlots}\n" +
               $"  未分配惩罚: {individual.UnassignedSlots * UnassignedPenaltyWeight:F2}\n" +
               $"  最终适应度: {individual.Fitness:F4}";
    }
}
