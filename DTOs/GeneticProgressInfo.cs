namespace AutoScheduling3.DTOs;

/// <summary>
/// 遗传算法进度信息
/// </summary>
public class GeneticProgressInfo
{
    /// <summary>
    /// 当前代数
    /// </summary>
    public int CurrentGeneration { get; set; }

    /// <summary>
    /// 最大代数
    /// </summary>
    public int MaxGenerations { get; set; }

    /// <summary>
    /// 当前最优适应度
    /// </summary>
    public double BestFitness { get; set; }

    /// <summary>
    /// 当前平均适应度
    /// </summary>
    public double AverageFitness { get; set; }

    /// <summary>
    /// 最优个体的硬约束违反数量
    /// </summary>
    public int BestHardConstraintViolations { get; set; }

    /// <summary>
    /// 最优个体的未分配时段数量
    /// </summary>
    public int BestUnassignedSlots { get; set; }
}
