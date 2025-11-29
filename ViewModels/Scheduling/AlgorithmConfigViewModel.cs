using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using AutoScheduling3.SchedulingEngine.Config;
using AutoScheduling3.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoScheduling3.ViewModels.Scheduling;

/// <summary>
/// 算法配置视图模型
/// </summary>
public partial class AlgorithmConfigViewModel : ObservableObject
{
    private readonly ISchedulingService _schedulingService;

    [ObservableProperty]
    private SchedulingMode _selectedMode = SchedulingMode.Hybrid;

    [ObservableProperty]
    private int _populationSize = 50;

    [ObservableProperty]
    private int _maxGenerations = 100;

    [ObservableProperty]
    private double _crossoverRate = 0.8;

    [ObservableProperty]
    private double _mutationRate = 0.1;

    [ObservableProperty]
    private int _eliteCount = 2;

    [ObservableProperty]
    private SelectionStrategyType _selectionStrategy = SelectionStrategyType.Tournament;

    [ObservableProperty]
    private CrossoverStrategyType _crossoverStrategy = CrossoverStrategyType.Uniform;

    [ObservableProperty]
    private MutationStrategyType _mutationStrategy = MutationStrategyType.Swap;

    [ObservableProperty]
    private int _tournamentSize = 5;

    [ObservableProperty]
    private bool _isHybridMode = true;

    [ObservableProperty]
    private bool _isGreedyOnlyMode = false;

    // 验证错误消息
    [ObservableProperty]
    private string? _populationSizeError;

    [ObservableProperty]
    private string? _maxGenerationsError;

    [ObservableProperty]
    private string? _crossoverRateError;

    [ObservableProperty]
    private string? _mutationRateError;

    [ObservableProperty]
    private string? _eliteCountError;

    [ObservableProperty]
    private string? _tournamentSizeError;

    public AlgorithmConfigViewModel(ISchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    /// <summary>
    /// 可用的排班模式列表
    /// </summary>
    public List<SchedulingModeOption> AvailableModes { get; } = new()
    {
        new SchedulingModeOption
        {
            Mode = SchedulingMode.GreedyOnly,
            DisplayName = "仅贪心算法",
            Description = "快速生成排班方案，适合低性能设备"
        },
        new SchedulingModeOption
        {
            Mode = SchedulingMode.Hybrid,
            DisplayName = "混合模式（推荐）",
            Description = "先使用贪心算法生成初始解，再使用遗传算法优化，质量更优"
        }
    };

    /// <summary>
    /// 可用的选择策略列表
    /// </summary>
    public List<SelectionStrategyOption> AvailableSelectionStrategies { get; } = new()
    {
        new SelectionStrategyOption
        {
            Strategy = SelectionStrategyType.Tournament,
            DisplayName = "锦标赛选择",
            Description = "随机选择若干个体，返回其中最优的"
        },
        new SelectionStrategyOption
        {
            Strategy = SelectionStrategyType.RouletteWheel,
            DisplayName = "轮盘赌选择",
            Description = "根据适应度比例随机选择"
        }
    };

    /// <summary>
    /// 可用的交叉策略列表
    /// </summary>
    public List<CrossoverStrategyOption> AvailableCrossoverStrategies { get; } = new()
    {
        new CrossoverStrategyOption
        {
            Strategy = CrossoverStrategyType.Uniform,
            DisplayName = "均匀交叉",
            Description = "对每个基因位随机选择来自父代1或父代2"
        },
        new CrossoverStrategyOption
        {
            Strategy = CrossoverStrategyType.SinglePoint,
            DisplayName = "单点交叉",
            Description = "随机选择交叉点，交换交叉点后的基因片段"
        }
    };

    /// <summary>
    /// 可用的变异策略列表
    /// </summary>
    public List<MutationStrategyOption> AvailableMutationStrategies { get; } = new()
    {
        new MutationStrategyOption
        {
            Strategy = MutationStrategyType.Swap,
            DisplayName = "交换变异",
            Description = "随机选择班次，从可行人员中选择新人员"
        }
    };

    partial void OnSelectedModeChanged(SchedulingMode value)
    {
        IsHybridMode = value == SchedulingMode.Hybrid;
        IsGreedyOnlyMode = value == SchedulingMode.GreedyOnly;
    }

    partial void OnIsHybridModeChanged(bool value)
    {
        if (value)
        {
            SelectedMode = SchedulingMode.Hybrid;
            IsGreedyOnlyMode = false;
        }
    }

    partial void OnIsGreedyOnlyModeChanged(bool value)
    {
        if (value)
        {
            SelectedMode = SchedulingMode.GreedyOnly;
            IsHybridMode = false;
        }
    }

    partial void OnPopulationSizeChanged(int value)
    {
        ValidatePopulationSize();
    }

    partial void OnMaxGenerationsChanged(int value)
    {
        ValidateMaxGenerations();
    }

    partial void OnCrossoverRateChanged(double value)
    {
        ValidateCrossoverRate();
    }

    partial void OnMutationRateChanged(double value)
    {
        ValidateMutationRate();
    }

    partial void OnEliteCountChanged(int value)
    {
        ValidateEliteCount();
    }

    partial void OnTournamentSizeChanged(int value)
    {
        ValidateTournamentSize();
    }

    /// <summary>
    /// 验证种群大小
    /// </summary>
    private void ValidatePopulationSize()
    {
        if (PopulationSize < 10)
        {
            PopulationSizeError = "种群大小不能小于10";
        }
        else if (PopulationSize > 200)
        {
            PopulationSizeError = "种群大小不能大于200";
        }
        else
        {
            PopulationSizeError = null;
        }
    }

    /// <summary>
    /// 验证最大代数
    /// </summary>
    private void ValidateMaxGenerations()
    {
        if (MaxGenerations < 10)
        {
            MaxGenerationsError = "最大代数不能小于10";
        }
        else if (MaxGenerations > 500)
        {
            MaxGenerationsError = "最大代数不能大于500";
        }
        else
        {
            MaxGenerationsError = null;
        }
    }

    /// <summary>
    /// 验证交叉率
    /// </summary>
    private void ValidateCrossoverRate()
    {
        if (CrossoverRate < 0.0 || CrossoverRate > 1.0)
        {
            CrossoverRateError = "交叉率必须在0.0到1.0之间";
        }
        else
        {
            CrossoverRateError = null;
        }
    }

    /// <summary>
    /// 验证变异率
    /// </summary>
    private void ValidateMutationRate()
    {
        if (MutationRate < 0.0 || MutationRate > 1.0)
        {
            MutationRateError = "变异率必须在0.0到1.0之间";
        }
        else
        {
            MutationRateError = null;
        }
    }

    /// <summary>
    /// 验证精英保留数量
    /// </summary>
    private void ValidateEliteCount()
    {
        if (EliteCount < 0)
        {
            EliteCountError = "精英保留数量不能小于0";
        }
        else if (EliteCount > 10)
        {
            EliteCountError = "精英保留数量不能大于10";
        }
        else
        {
            EliteCountError = null;
        }
    }

    /// <summary>
    /// 验证锦标赛大小
    /// </summary>
    private void ValidateTournamentSize()
    {
        if (TournamentSize < 2)
        {
            TournamentSizeError = "锦标赛大小不能小于2";
        }
        else if (TournamentSize > PopulationSize)
        {
            TournamentSizeError = $"锦标赛大小不能大于种群大小({PopulationSize})";
        }
        else
        {
            TournamentSizeError = null;
        }
    }

    /// <summary>
    /// 验证所有配置
    /// </summary>
    /// <returns>是否所有配置都有效</returns>
    public bool ValidateAll()
    {
        ValidatePopulationSize();
        ValidateMaxGenerations();
        ValidateCrossoverRate();
        ValidateMutationRate();
        ValidateEliteCount();
        ValidateTournamentSize();

        return string.IsNullOrEmpty(PopulationSizeError) &&
               string.IsNullOrEmpty(MaxGenerationsError) &&
               string.IsNullOrEmpty(CrossoverRateError) &&
               string.IsNullOrEmpty(MutationRateError) &&
               string.IsNullOrEmpty(EliteCountError) &&
               string.IsNullOrEmpty(TournamentSizeError);
    }

    /// <summary>
    /// 恢复默认值命令
    /// </summary>
    [RelayCommand]
    private void RestoreDefaults()
    {
        var defaultConfig = GeneticSchedulerConfig.GetDefault();
        LoadFromConfig(defaultConfig);
    }

    /// <summary>
    /// 从配置对象加载
    /// </summary>
    public void LoadFromConfig(GeneticSchedulerConfig config)
    {
        PopulationSize = config.PopulationSize;
        MaxGenerations = config.MaxGenerations;
        CrossoverRate = config.CrossoverRate;
        MutationRate = config.MutationRate;
        EliteCount = config.EliteCount;
        SelectionStrategy = config.SelectionStrategy;
        CrossoverStrategy = config.CrossoverStrategy;
        MutationStrategy = config.MutationStrategy;
        TournamentSize = config.TournamentSize;
    }

    /// <summary>
    /// 转换为配置对象
    /// </summary>
    public GeneticSchedulerConfig ToConfig()
    {
        return new GeneticSchedulerConfig
        {
            PopulationSize = PopulationSize,
            MaxGenerations = MaxGenerations,
            CrossoverRate = CrossoverRate,
            MutationRate = MutationRate,
            EliteCount = EliteCount,
            SelectionStrategy = SelectionStrategy,
            CrossoverStrategy = CrossoverStrategy,
            MutationStrategy = MutationStrategy,
            TournamentSize = TournamentSize
        };
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public async Task LoadConfigAsync()
    {
        try
        {
            var config = await _schedulingService.GetGeneticSchedulerConfigAsync();
            LoadFromConfig(config);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
            // 加载失败时使用默认配置
            RestoreDefaults();
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public async Task SaveConfigAsync()
    {
        if (!ValidateAll())
        {
            throw new InvalidOperationException("配置验证失败，无法保存");
        }

        try
        {
            var config = ToConfig();
            await _schedulingService.SaveGeneticSchedulerConfigAsync(config);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// 排班模式选项
/// </summary>
public class SchedulingModeOption
{
    public SchedulingMode Mode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 选择策略选项
/// </summary>
public class SelectionStrategyOption
{
    public SelectionStrategyType Strategy { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 交叉策略选项
/// </summary>
public class CrossoverStrategyOption
{
    public CrossoverStrategyType Strategy { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 变异策略选项
/// </summary>
public class MutationStrategyOption
{
    public MutationStrategyType Strategy { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
