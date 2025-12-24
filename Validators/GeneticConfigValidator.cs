using System;
using System.Collections.Generic;
using AutoScheduling3.SchedulingEngine.Config;

namespace AutoScheduling3.Validators;

/// <summary>
/// 遗传算法配置验证器
/// 验证遗传算法配置参数的有效性
/// </summary>
public class GeneticConfigValidator
{
    /// <summary>
    /// 验证遗传算法配置
    /// </summary>
    /// <param name="config">要验证的配置对象</param>
    /// <returns>验证结果</returns>
    public ValidationResult Validate(GeneticSchedulerConfig config)
    {
        var result = new ValidationResult { IsValid = true };

        if (config == null)
        {
            result.IsValid = false;
            result.Errors.Add("配置对象不能为空");
            return result;
        }

        // 验证种群大小（10-200）
        if (config.PopulationSize < 10 || config.PopulationSize > 200)
        {
            result.IsValid = false;
            result.Errors.Add($"种群大小必须在 10-200 之间，当前值: {config.PopulationSize}");
        }

        // 验证代数（10-500）
        if (config.MaxGenerations < 10 || config.MaxGenerations > 500)
        {
            result.IsValid = false;
            result.Errors.Add($"最大代数必须在 10-500 之间，当前值: {config.MaxGenerations}");
        }

        // 验证交叉率（0.0-1.0）
        if (config.CrossoverRate < 0.0 || config.CrossoverRate > 1.0)
        {
            result.IsValid = false;
            result.Errors.Add($"交叉率必须在 0.0-1.0 之间，当前值: {config.CrossoverRate}");
        }

        // 验证变异率（0.0-1.0）
        if (config.MutationRate < 0.0 || config.MutationRate > 1.0)
        {
            result.IsValid = false;
            result.Errors.Add($"变异率必须在 0.0-1.0 之间，当前值: {config.MutationRate}");
        }

        // 验证精英保留数量（0-10）
        if (config.EliteCount < 0 || config.EliteCount > 10)
        {
            result.IsValid = false;
            result.Errors.Add($"精英保留数量必须在 0-10 之间，当前值: {config.EliteCount}");
        }

        // 验证精英保留数量不能超过种群大小
        if (config.EliteCount > config.PopulationSize)
        {
            result.IsValid = false;
            result.Errors.Add($"精英保留数量 ({config.EliteCount}) 不能超过种群大小 ({config.PopulationSize})");
        }

        // 验证锦标赛大小（2-20）
        if (config.TournamentSize < 2 || config.TournamentSize > 20)
        {
            result.IsValid = false;
            result.Errors.Add($"锦标赛大小必须在 2-20 之间，当前值: {config.TournamentSize}");
        }

        // 验证锦标赛大小不能超过种群大小
        if (config.TournamentSize > config.PopulationSize)
        {
            result.IsValid = false;
            result.Errors.Add($"锦标赛大小 ({config.TournamentSize}) 不能超过种群大小 ({config.PopulationSize})");
        }

        // 验证权重参数
        if (config.UnassignedPenaltyWeight < 0)
        {
            result.Warnings.Add($"未分配惩罚权重为负数: {config.UnassignedPenaltyWeight}，建议使用正数");
        }

        if (config.HardConstraintPenaltyWeight < 0)
        {
            result.Warnings.Add($"硬约束惩罚权重为负数: {config.HardConstraintPenaltyWeight}，建议使用正数");
        }

        return result;
    }
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误列表
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告列表
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 获取错误消息（用于显示）
    /// </summary>
    public string GetErrorMessage()
    {
        return string.Join("\n", Errors);
    }

    /// <summary>
    /// 获取警告消息（用于显示）
    /// </summary>
    public string GetWarningMessage()
    {
        return string.Join("\n", Warnings);
    }
}
