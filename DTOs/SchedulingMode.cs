namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班模式枚举
/// </summary>
public enum SchedulingMode
{
    /// <summary>
    /// 仅使用贪心算法
    /// </summary>
    GreedyOnly,

    /// <summary>
    /// 混合模式：先使用贪心算法生成初始解，再使用遗传算法优化
    /// </summary>
    Hybrid
}
