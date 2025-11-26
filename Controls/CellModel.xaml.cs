using AutoScheduling3.DTOs;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Automation;
using Windows.UI;

namespace AutoScheduling3.Controls;

/// <summary>
/// 排班表格单元格控件
/// 
/// 使用示例：
/// <code>
/// var cell = new CellModel
/// {
///     CellData = new ScheduleGridCell
///     {
///         PersonnelName = "张三",
///         IsAssigned = true,
///         IsManualAssignment = true,  // 蓝色边框
///         HasConflict = false
///     },
///     PersonnelSkills = "技能A, 技能B",
///     PersonnelWorkload = "15 班次"
/// };
/// 
/// cell.CellClicked += (sender, cellData) =>
/// {
///     // 处理单元格点击事件
/// };
/// </code>
/// 
/// 样式说明：
/// - 普通单元格：默认边框和背景
/// - 手动指定单元格：蓝色边框（BorderThickness=2）
/// - 冲突单元格：红色边框（BorderThickness=2）+ 浅红色背景
/// </summary>
public sealed partial class CellModel : UserControl
{
    /// <summary>
    /// 单元格数据依赖属性
    /// </summary>
    public static readonly DependencyProperty CellDataProperty =
        DependencyProperty.Register(
            nameof(CellData),
            typeof(ScheduleGridCell),
            typeof(CellModel),
            new PropertyMetadata(null, OnCellDataChanged));

    /// <summary>
    /// 人员技能依赖属性（用于ToolTip显示）
    /// </summary>
    public static readonly DependencyProperty PersonnelSkillsProperty =
        DependencyProperty.Register(
            nameof(PersonnelSkills),
            typeof(string),
            typeof(CellModel),
            new PropertyMetadata("无"));

    /// <summary>
    /// 人员工作量依赖属性（用于ToolTip显示）
    /// </summary>
    public static readonly DependencyProperty PersonnelWorkloadProperty =
        DependencyProperty.Register(
            nameof(PersonnelWorkload),
            typeof(string),
            typeof(CellModel),
            new PropertyMetadata("0 班次"));

    /// <summary>
    /// 是否高亮显示依赖属性
    /// </summary>
    public static readonly DependencyProperty IsHighlightedProperty =
        DependencyProperty.Register(
            nameof(IsHighlighted),
            typeof(bool),
            typeof(CellModel),
            new PropertyMetadata(false, OnIsHighlightedChanged));

    /// <summary>
    /// 是否为焦点高亮依赖属性（比普通高亮更明显）
    /// </summary>
    public static readonly DependencyProperty IsFocusedHighlightProperty =
        DependencyProperty.Register(
            nameof(IsFocusedHighlight),
            typeof(bool),
            typeof(CellModel),
            new PropertyMetadata(false, OnIsFocusedHighlightChanged));

    /// <summary>
    /// 单元格点击事件
    /// </summary>
    public event EventHandler<ScheduleGridCell>? CellClicked;

    public CellModel()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// 单元格数据
    /// </summary>
    public ScheduleGridCell? CellData
    {
        get => (ScheduleGridCell?)GetValue(CellDataProperty);
        set => SetValue(CellDataProperty, value);
    }

    /// <summary>
    /// 人员技能（用于ToolTip）
    /// </summary>
    public string PersonnelSkills
    {
        get => (string)GetValue(PersonnelSkillsProperty);
        set => SetValue(PersonnelSkillsProperty, value);
    }

    /// <summary>
    /// 人员工作量（用于ToolTip）
    /// </summary>
    public string PersonnelWorkload
    {
        get => (string)GetValue(PersonnelWorkloadProperty);
        set => SetValue(PersonnelWorkloadProperty, value);
    }

    /// <summary>
    /// 是否高亮显示
    /// </summary>
    public bool IsHighlighted
    {
        get => (bool)GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
    }

    /// <summary>
    /// 是否为焦点高亮（比普通高亮更明显）
    /// </summary>
    public bool IsFocusedHighlight
    {
        get => (bool)GetValue(IsFocusedHighlightProperty);
        set => SetValue(IsFocusedHighlightProperty, value);
    }

    private static void OnCellDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CellModel cell)
        {
            cell.UpdateCellAppearance();
        }
    }

    private static void OnIsHighlightedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CellModel cell)
        {
            cell.UpdateHighlightState();
        }
    }

    private static void OnIsFocusedHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CellModel cell)
        {
            cell.UpdateHighlightState();
        }
    }

    /// <summary>
    /// 更新高亮状态
    /// </summary>
    private void UpdateHighlightState()
    {
        if (IsFocusedHighlight)
        {
            ApplyFocusedHighlightStyle();
        }
        else if (IsHighlighted)
        {
            ApplyHighlightStyle();
        }
        else
        {
            UpdateCellAppearance();
        }
    }

    /// <summary>
    /// 应用普通高亮样式
    /// </summary>
    private void ApplyHighlightStyle()
    {
        // 使用资源字典中定义的高亮颜色
        try
        {
            CellBorder.BorderBrush = (Brush)Application.Current.Resources["SearchHighlightBrush"];
            CellBorder.BorderThickness = (Thickness)Application.Current.Resources["SearchHighlightBorderThickness"];
            CellBorder.Background = (Brush)Application.Current.Resources["SearchHighlightBackgroundBrush"];
        }
        catch
        {
            // 回退到硬编码颜色
            CellBorder.BorderBrush = new SolidColorBrush(Colors.Orange);
            CellBorder.BorderThickness = new Thickness(3);
            CellBorder.Background = new SolidColorBrush(Color.FromArgb(50, 255, 165, 0));
        }
        
        PersonnelNameText.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
        PersonnelNameText.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
        
        // 清除阴影效果
        CellBorder.Shadow = null;
        CellBorder.Translation = new System.Numerics.Vector3(0, 0, 0);
    }

    /// <summary>
    /// 应用焦点高亮样式（更明显的样式）
    /// </summary>
    private void ApplyFocusedHighlightStyle()
    {
        // 使用资源字典中定义的焦点高亮颜色
        try
        {
            CellBorder.BorderBrush = (Brush)Application.Current.Resources["FocusedHighlightBrush"];
            CellBorder.BorderThickness = (Thickness)Application.Current.Resources["FocusedHighlightBorderThickness"];
            CellBorder.Background = (Brush)Application.Current.Resources["FocusedHighlightBackgroundBrush"];
        }
        catch
        {
            // 回退到硬编码颜色
            CellBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 140, 0));
            CellBorder.BorderThickness = new Thickness(4);
            CellBorder.Background = new SolidColorBrush(Color.FromArgb(100, 255, 140, 0));
        }
        
        PersonnelNameText.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
        PersonnelNameText.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
        
        // 添加阴影效果（如果支持）
        try
        {
            CellBorder.Shadow = new Microsoft.UI.Xaml.Media.ThemeShadow();
            CellBorder.Translation = new System.Numerics.Vector3(0, 0, 8);
        }
        catch
        {
            // 如果不支持阴影，静默忽略
        }
    }

    /// <summary>
    /// 更新单元格外观
    /// </summary>
    private void UpdateCellAppearance()
    {
        if (CellData == null)
        {
            ResetCellAppearance();
            return;
        }

        // 更新人员名称
        PersonnelNameText.Text = CellData.PersonnelName ?? "-";

        // 设置自动化属性
        string automationName;
        string automationHelpText;
        
        if (CellData.IsAssigned && !string.IsNullOrEmpty(CellData.PersonnelName))
        {
            automationName = $"已分配: {CellData.PersonnelName}";
            automationHelpText = $"人员 {CellData.PersonnelName} 已分配到此时段";
            
            if (CellData.IsManualAssignment)
            {
                automationHelpText += "，手动指定";
            }
            
            if (CellData.HasConflict)
            {
                automationHelpText += $"，存在冲突: {CellData.ConflictMessage}";
            }
        }
        else
        {
            automationName = "未分配";
            automationHelpText = "此时段尚未分配人员";
        }
        
        AutomationProperties.SetName(CellBorder, automationName);
        AutomationProperties.SetHelpText(CellBorder, automationHelpText);

        // 应用样式：优先级 冲突 > 手动指定 > 普通
        if (CellData.HasConflict)
        {
            ApplyConflictStyle();
        }
        else if (CellData.IsManualAssignment)
        {
            ApplyManualAssignmentStyle();
        }
        else if (CellData.IsAssigned)
        {
            ApplyAssignedStyle();
        }
        else
        {
            ApplyNormalStyle();
        }

        // 更新ToolTip
        UpdateToolTip();
    }

    /// <summary>
    /// 应用普通单元格样式
    /// </summary>
    private void ApplyNormalStyle()
    {
        CellBorder.BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
        CellBorder.BorderThickness = new Thickness(1);
        CellBorder.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
        PersonnelNameText.Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
    }

    /// <summary>
    /// 应用已分配单元格样式
    /// </summary>
    private void ApplyAssignedStyle()
    {
        CellBorder.BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
        CellBorder.BorderThickness = new Thickness(1);
        CellBorder.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
        PersonnelNameText.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
    }

    /// <summary>
    /// 应用手动指定单元格样式（蓝色边框）
    /// </summary>
    private void ApplyManualAssignmentStyle()
    {
        CellBorder.BorderBrush = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
        CellBorder.BorderThickness = new Thickness(2);
        CellBorder.Background = (Brush)Application.Current.Resources["AccentFillColorTertiaryBrush"];
        PersonnelNameText.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
    }

    /// <summary>
    /// 应用冲突单元格样式（红色边框）
    /// </summary>
    private void ApplyConflictStyle()
    {
        CellBorder.BorderBrush = new SolidColorBrush((Color)Application.Current.Resources["SystemErrorTextColor"]);
        CellBorder.BorderThickness = new Thickness(2);
        
        // 尝试使用系统错误背景色，如果不存在则使用浅红色
        try
        {
            CellBorder.Background = (Brush)Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"];
        }
        catch
        {
            CellBorder.Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(30, 255, 0, 0));
        }
        
        PersonnelNameText.Foreground = new SolidColorBrush((Color)Application.Current.Resources["SystemErrorTextColor"]);
    }

    /// <summary>
    /// 重置单元格外观
    /// </summary>
    private void ResetCellAppearance()
    {
        PersonnelNameText.Text = "-";
        ApplyNormalStyle();
        CellToolTip.Visibility = Visibility.Collapsed;
        AutomationProperties.SetName(CellBorder, "空单元格");
    }

    /// <summary>
    /// 更新ToolTip内容
    /// </summary>
    private void UpdateToolTip()
    {
        if (CellData == null || !CellData.IsAssigned)
        {
            CellToolTip.Visibility = Visibility.Collapsed;
            return;
        }

        CellToolTip.Visibility = Visibility.Visible;

        // 更新人员姓名
        ToolTipPersonnelName.Text = CellData.PersonnelName ?? "未知";

        // 更新技能
        ToolTipSkills.Text = $"技能：{PersonnelSkills}";

        // 更新工作量
        ToolTipWorkload.Text = $"工作量：{PersonnelWorkload}";

        // 显示/隐藏手动指定标识
        ToolTipManualAssignment.Visibility = CellData.IsManualAssignment 
            ? Visibility.Visible 
            : Visibility.Collapsed;

        // 显示/隐藏冲突信息
        if (CellData.HasConflict && !string.IsNullOrEmpty(CellData.ConflictMessage))
        {
            ToolTipConflict.Text = $"冲突：{CellData.ConflictMessage}";
            ToolTipConflict.Visibility = Visibility.Visible;
        }
        else
        {
            ToolTipConflict.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// 鼠标进入事件
    /// </summary>
    private void CellBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (CellData?.IsAssigned == true)
        {
            // 添加悬停效果
            CellBorder.Opacity = 0.8;
        }
    }

    /// <summary>
    /// 鼠标离开事件
    /// </summary>
    private void CellBorder_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        // 恢复正常透明度
        CellBorder.Opacity = 1.0;
    }

    /// <summary>
    /// 单元格点击事件
    /// </summary>
    private void CellBorder_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (CellData != null)
        {
            CellClicked?.Invoke(this, CellData);
        }
    }
}
