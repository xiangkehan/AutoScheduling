using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 模板算法配置
/// 用于在模板的 StrategyConfig 中存储算法配置
/// </summary>
public class TemplateAlgorithmConfig
{
    /// <summary>
    /// 排班模式
    /// </summary>
    [JsonPropertyName("schedulingMode")]
    public SchedulingMode SchedulingMode { get; set; } = SchedulingMode.GreedyOnly;

    /// <summary>
    /// 遗传算法配置（仅在混合模式下使用）
    /// </summary>
    [JsonPropertyName("geneticConfig")]
    public GeneticAlgorithmConfigDto? GeneticConfig { get; set; }
}
