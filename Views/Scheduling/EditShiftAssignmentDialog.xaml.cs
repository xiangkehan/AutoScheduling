using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoScheduling3.Views.Scheduling;

/// <summary>
/// 修改班次分配对话框
/// </summary>
public sealed partial class EditShiftAssignmentDialog : ContentDialog
{
    private ObservableCollection<PersonnelOption> _allPersonnel = new();
    private ObservableCollection<PersonnelOption> _filteredPersonnel = new();

    /// <summary>
    /// 当前分配信息
    /// </summary>
    public CurrentAssignmentInfo? CurrentAssignment { get; set; }

    /// <summary>
    /// 可用人员列表
    /// </summary>
    public List<PersonnelOption>? AvailablePersonnel { get; set; }

    /// <summary>
    /// 选中的人员ID
    /// </summary>
    public int? SelectedPersonnelId { get; private set; }

    public EditShiftAssignmentDialog()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// 对话框打开时的处理
    /// </summary>
    private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        // 设置无障碍属性
        SetAccessibilityProperties();

        // 加载当前分配信息
        if (CurrentAssignment != null)
        {
            LoadCurrentAssignment();
        }

        // 加载人员列表
        if (AvailablePersonnel != null)
        {
            LoadPersonnelList();
        }

        // 初始状态
        IsPrimaryButtonEnabled = false;
    }

    /// <summary>
    /// 设置无障碍属性
    /// </summary>
    private void SetAccessibilityProperties()
    {
        SearchBox.SetValue(AutomationProperties.NameProperty, "搜索人员");
        SearchBox.SetValue(AutomationProperties.HelpTextProperty, "输入人员姓名进行搜索");

        PersonnelListView.SetValue(AutomationProperties.NameProperty, "可用人员列表");
        PersonnelListView.SetValue(AutomationProperties.HelpTextProperty, "选择要分配的人员");
    }

    /// <summary>
    /// 加载当前分配信息
    /// </summary>
    private void LoadCurrentAssignment()
    {
        if (CurrentAssignment == null) return;

        DateTimeText.Text = $"{CurrentAssignment.Date:yyyy-MM-dd} {CurrentAssignment.TimeSlot}";
        PositionText.Text = CurrentAssignment.PositionName;
        CurrentPersonnelText.Text = CurrentAssignment.CurrentPersonnelName ?? "未分配";
    }

    /// <summary>
    /// 加载人员列表
    /// </summary>
    private void LoadPersonnelList()
    {
        if (AvailablePersonnel == null) return;

        _allPersonnel.Clear();
        foreach (var personnel in AvailablePersonnel)
        {
            _allPersonnel.Add(personnel);
        }

        // 初始显示所有人员
        UpdateFilteredPersonnel(string.Empty);

        // 显示推荐人员
        var recommended = AvailablePersonnel.FirstOrDefault(p => p.IsRecommended);
        if (recommended != null)
        {
            RecommendationText.Text = $"推荐人员: {recommended.Name} ({recommended.RecommendationReason})";
            RecommendationPanel.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// 搜索框文本变化
    /// </summary>
    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var searchText = sender.Text?.Trim() ?? string.Empty;
            UpdateFilteredPersonnel(searchText);
        }
    }

    /// <summary>
    /// 更新筛选后的人员列表
    /// </summary>
    private void UpdateFilteredPersonnel(string searchText)
    {
        _filteredPersonnel.Clear();

        var filtered = string.IsNullOrWhiteSpace(searchText)
            ? _allPersonnel
            : _allPersonnel.Where(p => p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        foreach (var personnel in filtered.OrderByDescending(p => p.IsRecommended)
                                           .ThenBy(p => p.Name))
        {
            _filteredPersonnel.Add(personnel);
        }

        PersonnelListView.ItemsSource = _filteredPersonnel;
    }

    /// <summary>
    /// 人员列表选择变化
    /// </summary>
    private void PersonnelListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsPrimaryButtonEnabled = PersonnelListView.SelectedItem != null;
    }

    /// <summary>
    /// 主按钮点击
    /// </summary>
    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();

        try
        {
            if (PersonnelListView.SelectedItem is PersonnelOption selected)
            {
                // 检查是否选择了不可用的人员
                if (!selected.IsAvailable)
                {
                    ShowValidationError($"人员 {selected.Name} 当前不可用: {selected.AvailabilityText}");
                    args.Cancel = true;
                    return;
                }

                SelectedPersonnelId = selected.Id;
            }
            else
            {
                ShowValidationError("请选择一个人员");
                args.Cancel = true;
            }
        }
        catch (Exception ex)
        {
            ShowValidationError($"操作失败: {ex.Message}");
            args.Cancel = true;
        }
        finally
        {
            deferral.Complete();
        }
    }

    /// <summary>
    /// 显示验证错误
    /// </summary>
    private void ShowValidationError(string message)
    {
        ValidationErrorBar.Message = message;
        ValidationErrorBar.IsOpen = true;
    }

    #region 键盘快捷键处理

    /// <summary>
    /// Esc键快捷键处理 - 取消对话框
    /// </summary>
    private void EscapeAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SelectedPersonnelId = null;
        Hide();
        args.Handled = true;
    }

    /// <summary>
    /// Enter键快捷键处理 - 确认选择
    /// </summary>
    private void EnterAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        // 只有在选中了人员时才处理
        if (PersonnelListView.SelectedItem != null && IsPrimaryButtonEnabled)
        {
            // 触发主按钮点击
            var clickArgs = new ContentDialogButtonClickEventArgs();
            ContentDialog_PrimaryButtonClick(this, clickArgs);
            
            if (!clickArgs.Cancel)
            {
                Hide();
            }
            args.Handled = true;
        }
    }

    #endregion
}

/// <summary>
/// 当前分配信息
/// </summary>
public class CurrentAssignmentInfo
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 时段描述
    /// </summary>
    public string TimeSlot { get; set; } = string.Empty;

    /// <summary>
    /// 哨位名称
    /// </summary>
    public string PositionName { get; set; } = string.Empty;

    /// <summary>
    /// 当前人员名称
    /// </summary>
    public string? CurrentPersonnelName { get; set; }

    /// <summary>
    /// 当前人员ID
    /// </summary>
    public int? CurrentPersonnelId { get; set; }
}

/// <summary>
/// 人员选项
/// </summary>
public class PersonnelOption
{
    /// <summary>
    /// 人员ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 人员姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 技能是否匹配
    /// </summary>
    public bool SkillMatched { get; set; }

    /// <summary>
    /// 技能匹配图标
    /// </summary>
    public string SkillMatchIcon => SkillMatched ? "\uE73E" : "\uE7BA";

    /// <summary>
    /// 技能匹配文本
    /// </summary>
    public string SkillMatchText => SkillMatched ? "技能匹配" : "技能不足";

    /// <summary>
    /// 技能匹配颜色
    /// </summary>
    public Brush SkillMatchColor => SkillMatched
        ? (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"]
        : (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 可用性图标
    /// </summary>
    public string AvailabilityIcon => IsAvailable ? "\uE73E" : "\uE711";

    /// <summary>
    /// 可用性文本
    /// </summary>
    public string AvailabilityText { get; set; } = "可用";

    /// <summary>
    /// 可用性颜色
    /// </summary>
    public Brush AvailabilityColor => IsAvailable
        ? (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"]
        : (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];

    /// <summary>
    /// 工作量
    /// </summary>
    public int Workload { get; set; }

    /// <summary>
    /// 工作量文本
    /// </summary>
    public string WorkloadText => $"工作量: {Workload}班次";

    /// <summary>
    /// 是否推荐
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// 推荐原因
    /// </summary>
    public string RecommendationReason { get; set; } = string.Empty;
}
