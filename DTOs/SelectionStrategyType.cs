namespace AutoScheduling3.DTOs;

/// <summary>
/// 选择策略类型枚举
/// </summary>
public enum SelectionStrategyType
{
    /// <summary>
    /// 轮盘赌选择：根据适应度比例选择个体
    /// </summary>
    RouletteWheel,

    /// <summary>
    /// 锦标赛选择：随机选择若干个体，返回其中最优的
    /// </summary>
    Tournament
}
