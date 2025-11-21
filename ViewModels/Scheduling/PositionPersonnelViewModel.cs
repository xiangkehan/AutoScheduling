using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoScheduling3.ViewModels.Scheduling;

/// <summary>
/// 哨位人员视图模型
/// 用于在UI中展示哨位及其可用人员
/// </summary>
public partial class PositionPersonnelViewModel : ObservableObject
{
    /// <summary>
    /// 哨位ID
    /// </summary>
    public int PositionId { get; set; }

    /// <summary>
    /// 哨位名称
    /// </summary>
    public string PositionName { get; set; } = string.Empty;

    /// <summary>
    /// 哨位地点
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// 可用人员列表（包含临时更改）
    /// </summary>
    public ObservableCollection<PersonnelItemViewModel> AvailablePersonnel { get; set; } = new();

    /// <summary>
    /// 是否展开
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded = true;

    /// <summary>
    /// 是否有临时更改
    /// </summary>
    [ObservableProperty]
    private bool _hasChanges;

    /// <summary>
    /// 临时更改摘要
    /// </summary>
    [ObservableProperty]
    private string _changesSummary = string.Empty;
}
