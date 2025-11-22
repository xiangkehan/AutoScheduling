using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using AutoScheduling3.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.Views.Scheduling;

/// <summary>
/// 班次详情对话框
/// </summary>
public sealed partial class ShiftDetailsDialog : ContentDialog
{
    /// <summary>
    /// 班次数据
    /// </summary>
    public ShiftDetailsData? ShiftData { get; set; }

    /// <summary>
    /// 用户选择的操作
    /// </summary>
    public ShiftDetailsAction SelectedAction { get; private set; } = ShiftDetailsAction.None;

    public ShiftDetailsDialog()
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

        // 加载数据
        if (ShiftData != null)
        {
            LoadShiftData();
        }
    }

    /// <summary>
    /// 设置无障碍属性
    /// </summary>
    private void SetAccessibilityProperties()
    {
        EditButton.SetValue(AutomationProperties.NameProperty, "修改分配");
        EditButton.SetValue(AutomationProperties.HelpTextProperty, "打开修改分配对话框，选择新的人员");

        RemoveButton.SetValue(AutomationProperties.NameProperty, "取消分配");
        RemoveButton.SetValue(AutomationProperties.HelpTextProperty, "取消当前班次的人员分配");

        ViewPersonnelButton.SetValue(AutomationProperties.NameProperty, "查看人员详情");
        ViewPersonnelButton.SetValue(AutomationProperties.HelpTextProperty, "查看人员的完整信息");
    }

    /// <summary>
    /// 加载班次数据
    /// </summary>
    private void LoadShiftData()
    {
        if (ShiftData == null) return;

        // 基本信息
        DateTimeText.Text = $"{ShiftData.Date:yyyy-MM-dd} {ShiftData.TimeSlot}";
        PositionText.Text = ShiftData.PositionName;
        PersonnelText.Text = ShiftData.PersonnelName ?? "未分配";

        // 分配方式
        if (ShiftData.IsManualAssignment)
        {
            AssignmentTypePanel.Visibility = Visibility.Visible;
        }

        // 人员信息
        if (ShiftData.PersonnelInfo != null)
        {
            var info = ShiftData.PersonnelInfo;
            
            SkillsText.Text = info.Skills != null && info.Skills.Any()
                ? string.Join(", ", info.Skills)
                : "无";

            WorkloadText.Text = $"{info.TotalShifts} 班次 ({info.TotalHours}小时)";
            WeeklyWorkloadText.Text = $"{info.WeeklyShifts} 班次 ({info.WeeklyHours}小时)";

            PreviousShiftText.Text = info.PreviousShift ?? "无";
            NextShiftText.Text = info.NextShift ?? "无";
        }

        // 约束评估
        LoadConstraintEvaluations();
    }

    /// <summary>
    /// 加载约束评估结果
    /// </summary>
    private void LoadConstraintEvaluations()
    {
        ConstraintsPanel.Children.Clear();

        if (ShiftData?.ConstraintEvaluations == null || !ShiftData.ConstraintEvaluations.Any())
        {
            var noConstraints = new TextBlock
            {
                Text = "无约束评估信息",
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
            ConstraintsPanel.Children.Add(noConstraints);
            return;
        }

        foreach (var evaluation in ShiftData.ConstraintEvaluations)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            // 图标
            var icon = new FontIcon
            {
                FontSize = 16,
                Glyph = evaluation.IsSatisfied ? "\uE73E" : "\uE7BA" // ✓ 或 ⚠
            };

            if (evaluation.IsSatisfied)
            {
                icon.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
            }
            else
            {
                icon.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
            }

            panel.Children.Add(icon);

            // 文本
            var text = new TextBlock
            {
                Text = evaluation.Message,
                TextWrapping = TextWrapping.Wrap
            };

            if (!evaluation.IsSatisfied)
            {
                text.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
            }

            panel.Children.Add(text);

            ConstraintsPanel.Children.Add(panel);
        }
    }

    /// <summary>
    /// 修改分配按钮点击
    /// </summary>
    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedAction = ShiftDetailsAction.Edit;
        Hide();
    }

    /// <summary>
    /// 取消分配按钮点击
    /// </summary>
    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedAction = ShiftDetailsAction.Remove;
        Hide();
    }

    /// <summary>
    /// 查看人员详情按钮点击
    /// </summary>
    private void ViewPersonnelButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedAction = ShiftDetailsAction.ViewPersonnel;
        Hide();
    }

    /// <summary>
    /// Esc键快捷键处理 - 关闭对话框
    /// </summary>
    private void EscapeAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SelectedAction = ShiftDetailsAction.None;
        Hide();
        args.Handled = true;
    }
}

/// <summary>
/// 班次详情数据
/// </summary>
public class ShiftDetailsData
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
    /// 人员名称
    /// </summary>
    public string? PersonnelName { get; set; }

    /// <summary>
    /// 是否手动指定
    /// </summary>
    public bool IsManualAssignment { get; set; }

    /// <summary>
    /// 人员信息
    /// </summary>
    public PersonnelInfo? PersonnelInfo { get; set; }

    /// <summary>
    /// 约束评估结果
    /// </summary>
    public List<ConstraintEvaluation> ConstraintEvaluations { get; set; } = new();
}

/// <summary>
/// 人员信息
/// </summary>
public class PersonnelInfo
{
    /// <summary>
    /// 技能列表
    /// </summary>
    public List<string> Skills { get; set; } = new();

    /// <summary>
    /// 总班次数
    /// </summary>
    public int TotalShifts { get; set; }

    /// <summary>
    /// 总工作时长
    /// </summary>
    public int TotalHours { get; set; }

    /// <summary>
    /// 本周班次数
    /// </summary>
    public int WeeklyShifts { get; set; }

    /// <summary>
    /// 本周工作时长
    /// </summary>
    public int WeeklyHours { get; set; }

    /// <summary>
    /// 上一班次描述
    /// </summary>
    public string? PreviousShift { get; set; }

    /// <summary>
    /// 下一班次描述
    /// </summary>
    public string? NextShift { get; set; }
}

/// <summary>
/// 约束评估结果
/// </summary>
public class ConstraintEvaluation
{
    /// <summary>
    /// 是否满足约束
    /// </summary>
    public bool IsSatisfied { get; set; }

    /// <summary>
    /// 评估消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 班次详情操作
/// </summary>
public enum ShiftDetailsAction
{
    /// <summary>
    /// 无操作
    /// </summary>
    None,

    /// <summary>
    /// 修改分配
    /// </summary>
    Edit,

    /// <summary>
    /// 取消分配
    /// </summary>
    Remove,

    /// <summary>
    /// 查看人员详情
    /// </summary>
    ViewPersonnel
}
