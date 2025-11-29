namespace AutoScheduling3.DTOs;

/// <summary>
/// 交叉策略类型枚举
/// </summary>
public enum CrossoverStrategyType
{
    /// <summary>
    /// 均匀交叉：对每个基因位随机选择来自父代1或父代2
    /// </summary>
    Uniform,

    /// <summary>
    /// 单点交叉：随机选择交叉点，交换交叉点后的基因片段
    /// </summary>
    SinglePoint
}
