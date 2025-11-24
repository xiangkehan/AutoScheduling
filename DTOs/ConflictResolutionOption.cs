namespace AutoScheduling3.DTOs;

/// <summary>
/// 冲突修复方案
/// </summary>
public class ConflictResolutionOption
{
    /// <summary>
    /// 方案ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 方案标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 方案描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 方案类型
    /// </summary>
    public ResolutionType Type { get; set; }

    /// <summary>
    /// 是否推荐
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// 优点列表
    /// </summary>
    public List<string> Pros { get; set; } = new();

    /// <summary>
    /// 缺点/警告列表
    /// </summary>
    public List<string> Cons { get; set; } = new();

    /// <summary>
    /// 影响描述
    /// </summary>
    public string Impact { get; set; } = string.Empty;

    /// <summary>
    /// 预期产生的新冲突数量
    /// </summary>
    public int ExpectedNewConflicts { get; set; }

    /// <summary>
    /// 方案数据（用于应用修复）
    /// </summary>
    public object? ResolutionData { get; set; }
}
