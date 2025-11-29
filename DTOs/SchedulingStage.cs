namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班算法执行的各个阶段
/// </summary>
public enum SchedulingStage
{
    /// <summary>
    /// 初始化
    /// </summary>
    Initializing,

    /// <summary>
    /// 加载数据
    /// </summary>
    LoadingData,

    /// <summary>
    /// 构建上下文
    /// </summary>
    BuildingContext,

    /// <summary>
    /// 初始化可行性张量
    /// </summary>
    InitializingTensor,

    /// <summary>
    /// 应用约束
    /// </summary>
    ApplyingConstraints,

    /// <summary>
    /// 应用手动指定
    /// </summary>
    ApplyingManualAssignments,

    /// <summary>
    /// 贪心分配
    /// </summary>
    GreedyAssignment,

    /// <summary>
    /// 更新评分
    /// </summary>
    UpdatingScores,

    /// <summary>
    /// 完成处理
    /// </summary>
    Finalizing,

    /// <summary>
    /// 遗传算法优化
    /// </summary>
    GeneticOptimizing,

    /// <summary>
    /// 完成
    /// </summary>
    Completed,

    /// <summary>
    /// 失败
    /// </summary>
    Failed
}
