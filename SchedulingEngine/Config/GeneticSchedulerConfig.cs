using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.SchedulingEngine.Config;

/// <summary>
/// 遗传算法调度器配置类
/// </summary>
public class GeneticSchedulerConfig
{
    /// <summary>
    /// 种群大小（默认50）
    /// </summary>
    public int PopulationSize { get; set; } = 50;

    /// <summary>
    /// 最大代数（默认100）
    /// </summary>
    public int MaxGenerations { get; set; } = 100;

    /// <summary>
    /// 交叉率（默认0.8）
    /// </summary>
    public double CrossoverRate { get; set; } = 0.8;

    /// <summary>
    /// 变异率（默认0.1）
    /// </summary>
    public double MutationRate { get; set; } = 0.1;

    /// <summary>
    /// 精英保留数量（默认2）
    /// </summary>
    public int EliteCount { get; set; } = 2;

    /// <summary>
    /// 选择策略类型（默认锦标赛选择）
    /// </summary>
    public SelectionStrategyType SelectionStrategy { get; set; } = SelectionStrategyType.Tournament;

    /// <summary>
    /// 交叉策略类型（默认均匀交叉）
    /// </summary>
    public CrossoverStrategyType CrossoverStrategy { get; set; } = CrossoverStrategyType.Uniform;

    /// <summary>
    /// 变异策略类型（默认交换变异）
    /// </summary>
    public MutationStrategyType MutationStrategy { get; set; } = MutationStrategyType.Swap;

    /// <summary>
    /// 锦标赛大小（用于锦标赛选择，默认5）
    /// </summary>
    public int TournamentSize { get; set; } = 5;

    /// <summary>
    /// 未分配时段惩罚权重（默认1000.0）
    /// </summary>
    public double UnassignedPenaltyWeight { get; set; } = 1000.0;

    /// <summary>
    /// 硬约束违反惩罚权重（默认10000.0）
    /// </summary>
    public double HardConstraintPenaltyWeight { get; set; } = 10000.0;

    /// <summary>
    /// 是否启用详细日志（默认false）
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public void SaveToFile(string filePath)
    {
        try
        {
            // 验证配置
            ValidateConfig();

            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 序列化配置
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"保存配置文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从文件加载配置
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>配置对象</returns>
    public static GeneticSchedulerConfig LoadFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                // 文件不存在，返回默认配置
                return GetDefault();
            }

            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            var config = JsonSerializer.Deserialize<GeneticSchedulerConfig>(json, options);
            
            if (config == null)
            {
                // 反序列化失败，返回默认配置
                return GetDefault();
            }

            // 验证配置
            config.ValidateConfig();

            return config;
        }
        catch (JsonException ex)
        {
            // JSON格式错误，返回默认配置并记录警告
            System.Diagnostics.Debug.WriteLine($"配置文件格式错误，使用默认配置: {ex.Message}");
            return GetDefault();
        }
        catch (Exception ex)
        {
            // 其他错误，返回默认配置
            System.Diagnostics.Debug.WriteLine($"加载配置文件失败，使用默认配置: {ex.Message}");
            return GetDefault();
        }
    }

    /// <summary>
    /// 获取默认配置
    /// </summary>
    /// <returns>默认配置对象</returns>
    public static GeneticSchedulerConfig GetDefault()
    {
        return new GeneticSchedulerConfig();
    }

    /// <summary>
    /// 验证配置参数的有效性
    /// </summary>
    private void ValidateConfig()
    {
        // 验证种群大小
        if (PopulationSize < 10)
        {
            System.Diagnostics.Debug.WriteLine($"种群大小 {PopulationSize} 过小，使用最小值 10");
            PopulationSize = 10;
        }
        else if (PopulationSize > 200)
        {
            System.Diagnostics.Debug.WriteLine($"种群大小 {PopulationSize} 过大，使用最大值 200");
            PopulationSize = 200;
        }

        // 验证最大代数
        if (MaxGenerations < 10)
        {
            System.Diagnostics.Debug.WriteLine($"最大代数 {MaxGenerations} 过小，使用最小值 10");
            MaxGenerations = 10;
        }
        else if (MaxGenerations > 500)
        {
            System.Diagnostics.Debug.WriteLine($"最大代数 {MaxGenerations} 过大，使用最大值 500");
            MaxGenerations = 500;
        }

        // 验证交叉率
        if (CrossoverRate < 0.0 || CrossoverRate > 1.0)
        {
            System.Diagnostics.Debug.WriteLine($"交叉率 {CrossoverRate} 无效，使用默认值 0.8");
            CrossoverRate = 0.8;
        }

        // 验证变异率
        if (MutationRate < 0.0 || MutationRate > 1.0)
        {
            System.Diagnostics.Debug.WriteLine($"变异率 {MutationRate} 无效，使用默认值 0.1");
            MutationRate = 0.1;
        }

        // 验证精英保留数量
        if (EliteCount < 0)
        {
            System.Diagnostics.Debug.WriteLine($"精英保留数量 {EliteCount} 无效，使用默认值 2");
            EliteCount = 2;
        }
        else if (EliteCount > 10)
        {
            System.Diagnostics.Debug.WriteLine($"精英保留数量 {EliteCount} 过大，使用最大值 10");
            EliteCount = 10;
        }

        // 验证锦标赛大小
        if (TournamentSize < 2)
        {
            System.Diagnostics.Debug.WriteLine($"锦标赛大小 {TournamentSize} 过小，使用最小值 2");
            TournamentSize = 2;
        }
        else if (TournamentSize > PopulationSize)
        {
            System.Diagnostics.Debug.WriteLine($"锦标赛大小 {TournamentSize} 超过种群大小，使用种群大小 {PopulationSize}");
            TournamentSize = PopulationSize;
        }

        // 验证惩罚权重
        if (UnassignedPenaltyWeight < 0)
        {
            System.Diagnostics.Debug.WriteLine($"未分配时段惩罚权重 {UnassignedPenaltyWeight} 无效，使用默认值 1000.0");
            UnassignedPenaltyWeight = 1000.0;
        }

        if (HardConstraintPenaltyWeight < 0)
        {
            System.Diagnostics.Debug.WriteLine($"硬约束违反惩罚权重 {HardConstraintPenaltyWeight} 无效，使用默认值 10000.0");
            HardConstraintPenaltyWeight = 10000.0;
        }
    }

    /// <summary>
    /// 创建配置的深拷贝
    /// </summary>
    /// <returns>配置副本</returns>
    public GeneticSchedulerConfig Clone()
    {
        return new GeneticSchedulerConfig
        {
            PopulationSize = this.PopulationSize,
            MaxGenerations = this.MaxGenerations,
            CrossoverRate = this.CrossoverRate,
            MutationRate = this.MutationRate,
            EliteCount = this.EliteCount,
            SelectionStrategy = this.SelectionStrategy,
            CrossoverStrategy = this.CrossoverStrategy,
            MutationStrategy = this.MutationStrategy,
            TournamentSize = this.TournamentSize,
            UnassignedPenaltyWeight = this.UnassignedPenaltyWeight,
            HardConstraintPenaltyWeight = this.HardConstraintPenaltyWeight,
            EnableDetailedLogging = this.EnableDetailedLogging
        };
    }
}
