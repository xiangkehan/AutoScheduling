using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoScheduling3.ViewModels.Scheduling;

/// <summary>
/// 人员项视图模型
/// 用于在哨位人员列表中展示单个人员
/// </summary>
public partial class PersonnelItemViewModel : ObservableObject
{
    /// <summary>
    /// 人员ID
    /// </summary>
    public int PersonnelId { get; set; }

    /// <summary>
    /// 人员姓名
    /// </summary>
    public string PersonnelName { get; set; } = string.Empty;

    /// <summary>
    /// 技能显示文本
    /// </summary>
    public string SkillsDisplay { get; set; } = string.Empty;

    /// <summary>
    /// 是否被选中
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// 人员来源类型
    /// </summary>
    [ObservableProperty]
    private PersonnelSourceType _sourceType;

    /// <summary>
    /// 是否被多个哨位共享
    /// </summary>
    [ObservableProperty]
    private bool _isShared;

    /// <summary>
    /// 共享的哨位数量
    /// </summary>
    [ObservableProperty]
    private int _sharedPositionCount;
}
