using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 模板中的算法配置
/// 用于在模板的 StrategyConfig JSON 字段中存储算法模式和遗传算法配置
/// </summary>
public class TemplateAlgorithmConfig
{
    /// <summary>
    /// 排班模式（GreedyOnly 或 Hybrid）
    /// </summary>
    [JsonPropertyName("schedulingMode")]
    public SchedulingMode SchedulingMode { get; set; } = SchedulingMode.Hybrid;

    /// <summary>
    /// 遗传算法配置（仅在 Hybrid 模式下使用）
    /// </summary>
    [JsonPropertyName("geneticConfig")]
    public GeneticAlgorithmConfigDto? GeneticConfig { get; set; }
}

/// <summary>
/// 遗传算法配置DTO（用于模板存储）
/// </summary>
public class GeneticAlgorithmConfigDto
{
    /// <summary>
    /// 种群大小
    /// </summary>
    [JsonPropertyName("populationSize")]
    public int PopulationSize { get; set; } = 50;

    /// <summary>
    /// 最大代数
    /// </summary>
    [JsonPropertyName("maxGenerations")]
    public int MaxGenerations { get; set; } = 100;

    /// <summary>
    /// 交叉率
    /// </summary>
    [JsonPropertyName("crossoverRate")]
    public double CrossoverRate { get; set; } = 0.8;

    /// <summary>
    /// 变异率
    /// </summary>
    [JsonPropertyName("mutationRate")]
    public double MutationRate { get; set; } = 0.1;

    /// <summary>
    /// 精英保留数量
    /// </summary>
    [JsonPropertyName("eliteCount")]
    public int EliteCount { get; set; } = 2;

    /// <summary>
    /// 选择策略类型
    /// </summary>
    [JsonPropertyName("selectionStrategy")]
    public SelectionStrategyType SelectionStrategy { get; set; } = SelectionStrategyType.Tournament;

    /// <summary>
    /// 交叉策略类型
    /// </summary>
    [JsonPropertyName("crossoverStrategy")]
    public CrossoverStrategyType CrossoverStrategy { get; set; } = CrossoverStrategyType.Uniform;

    /// <summary>
    /// 变异策略类型
    /// </summary>
    [JsonPropertyName("mutationStrategy")]
    public MutationStrategyType MutationStrategy { get; set; } = MutationStrategyType.Swap;

    /// <summary>
    /// 锦标赛大小
    /// </summary>
    [JsonPropertyName("tournamentSize")]
    public int TournamentSize { get; set; } = 5;
}
