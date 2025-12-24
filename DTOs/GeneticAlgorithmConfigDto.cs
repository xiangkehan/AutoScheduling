using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 遗传算法配置数据传输对象
/// </summary>
public class GeneticAlgorithmConfigDto
{
    /// <summary>
    /// 种群大小（10-200）
    /// </summary>
    [Range(10, 200, ErrorMessage = "种群大小必须在 10-200 之间")]
    [JsonPropertyName("populationSize")]
    public int PopulationSize { get; set; } = 50;

    /// <summary>
    /// 最大代数（10-500）
    /// </summary>
    [Range(10, 500, ErrorMessage = "最大代数必须在 10-500 之间")]
    [JsonPropertyName("maxGenerations")]
    public int MaxGenerations { get; set; } = 100;

    /// <summary>
    /// 交叉率（0.0-1.0）
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "交叉率必须在 0.0-1.0 之间")]
    [JsonPropertyName("crossoverRate")]
    public double CrossoverRate { get; set; } = 0.8;

    /// <summary>
    /// 变异率（0.0-1.0）
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "变异率必须在 0.0-1.0 之间")]
    [JsonPropertyName("mutationRate")]
    public double MutationRate { get; set; } = 0.1;

    /// <summary>
    /// 精英保留数量（0-10）
    /// </summary>
    [Range(0, 10, ErrorMessage = "精英保留数量必须在 0-10 之间")]
    [JsonPropertyName("eliteCount")]
    public int EliteCount { get; set; } = 2;

    /// <summary>
    /// 选择策略
    /// </summary>
    [JsonPropertyName("selectionStrategy")]
    public SelectionStrategyType SelectionStrategy { get; set; } = SelectionStrategyType.Tournament;

    /// <summary>
    /// 交叉策略
    /// </summary>
    [JsonPropertyName("crossoverStrategy")]
    public CrossoverStrategyType CrossoverStrategy { get; set; } = CrossoverStrategyType.Uniform;

    /// <summary>
    /// 变异策略
    /// </summary>
    [JsonPropertyName("mutationStrategy")]
    public MutationStrategyType MutationStrategy { get; set; } = MutationStrategyType.Swap;

    /// <summary>
    /// 锦标赛大小（仅在使用锦标赛选择时有效，2-20）
    /// </summary>
    [Range(2, 20, ErrorMessage = "锦标赛大小必须在 2-20 之间")]
    [JsonPropertyName("tournamentSize")]
    public int TournamentSize { get; set; } = 5;
}
