namespace AutoScheduling3.SchedulingEngine.Strategies;

/// <summary>
/// 调度策略接口：定义选择下一个分配槽位的策略
/// 支持单日模式（MRVStrategy）和全局模式（GlobalMRVStrategy）
/// </summary>
public interface ISchedulingStrategy
{
    /// <summary>
    /// 选择下一个要分配的槽位
    /// </summary>
    /// <returns>
    /// (positionIdx, periodIdx) 元组
    /// - positionIdx: 哨位索引
    /// - periodIdx: 时段索引（单日模式为局部时段0-11，全局模式为全局时段0-totalPeriods）
    /// 如果所有位置已分配，返回 (-1, -1)
    /// </returns>
    (int positionIdx, int periodIdx) SelectNextSlot();

    /// <summary>
    /// 分配后更新候选人员计数
    /// </summary>
    /// <param name="positionIdx">哨位索引</param>
    /// <param name="periodIdx">时段索引（语义由实现决定）</param>
    /// <param name="personIdx">人员索引</param>
    void UpdateCandidateCountsAfterAssignment(int positionIdx, int periodIdx, int personIdx);

    /// <summary>
    /// 标记某个槽位为已分配
    /// </summary>
    /// <param name="positionIdx">哨位索引</param>
    /// <param name="periodIdx">时段索引（语义由实现决定）</param>
    void MarkAsAssigned(int positionIdx, int periodIdx);

    /// <summary>
    /// 初始化候选人员计数
    /// </summary>
    void InitializeCandidateCounts();

    /// <summary>
    /// 获取指定槽位的候选人员数量
    /// </summary>
    /// <param name="positionIdx">哨位索引</param>
    /// <param name="periodIdx">时段索引（语义由实现决定）</param>
    /// <returns>候选人员数量</returns>
    int GetCandidateCount(int positionIdx, int periodIdx);

    /// <summary>
    /// 获取所有未分配且无候选人员的位置（无解检测）
    /// </summary>
    /// <returns>未分配且无候选的位置列表</returns>
    List<(int positionIdx, int periodIdx)> GetUnassignedWithNoCandidates();

    /// <summary>
    /// 获取所有未分配的位置
    /// </summary>
    /// <returns>未分配的位置列表</returns>
    List<(int positionIdx, int periodIdx)> GetUnassignedSlots();

    /// <summary>
    /// 获取候选计数数组的副本（用于状态快照）
    /// </summary>
    /// <returns>候选计数数组副本</returns>
    int[,] GetCandidateCountsCopy();

    /// <summary>
    /// 获取分配标记数组的副本（用于状态快照）
    /// </summary>
    /// <returns>分配标记数组副本</returns>
    bool[,] GetAssignedFlagsCopy();

    /// <summary>
    /// 获取候选计数数组的引用（用于状态恢复）
    /// </summary>
    /// <returns>候选计数数组引用</returns>
    int[,] GetCandidateCountsReference();

    /// <summary>
    /// 获取分配标记数组的引用（用于状态恢复）
    /// </summary>
    /// <returns>分配标记数组引用</returns>
    bool[,] GetAssignedFlagsReference();
}
