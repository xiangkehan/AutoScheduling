using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.Views.Scheduling;

/// <summary>
/// 排班比较视图
/// </summary>
public sealed partial class ScheduleComparisonView : ContentDialog
{
    /// <summary>
    /// 当前排班数据
    /// </summary>
    public ScheduleComparisonData? CurrentSchedule { get; set; }

    /// <summary>
    /// 比较排班数据
    /// </summary>
    public ScheduleComparisonData? CompareSchedule { get; set; }

    /// <summary>
    /// 差异单元格集合
    /// </summary>
    public HashSet<string> DifferenceCells { get; set; } = new();

    public ScheduleComparisonView()
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
        LoadComparisonData();
    }

    /// <summary>
    /// 设置无障碍属性
    /// </summary>
    private void SetAccessibilityProperties()
    {
        CurrentScheduleGrid.SetValue(AutomationProperties.NameProperty, "当前排班表格");
        CompareScheduleGrid.SetValue(AutomationProperties.NameProperty, "比较排班表格");
    }

    /// <summary>
    /// 加载比较数据
    /// </summary>
    private void LoadComparisonData()
    {
        if (CurrentSchedule == null || CompareSchedule == null)
            return;

        // 加载基本信息
        CurrentScheduleNameText.Text = CurrentSchedule.Name;
        CurrentScheduleDateText.Text = CurrentSchedule.DateRange;
        CompareScheduleNameText.Text = CompareSchedule.Name;
        CompareScheduleDateText.Text = CompareSchedule.DateRange;

        // 加载统计信息
        LoadStatistics();

        // 构建表格（简化版本，实际应该根据数据动态生成）
        BuildComparisonGrids();
    }

    /// <summary>
    /// 加载统计信息
    /// </summary>
    private void LoadStatistics()
    {
        if (CurrentSchedule == null || CompareSchedule == null)
            return;

        // 当前排班统计
        CurrentTotalAssignmentsText.Text = CurrentSchedule.TotalAssignments.ToString();
        CurrentPersonnelCountText.Text = CurrentSchedule.PersonnelCount.ToString();
        CurrentScoreText.Text = CurrentSchedule.Score.ToString("F1");

        // 比较排班统计
        CompareTotalAssignmentsText.Text = CompareSchedule.TotalAssignments.ToString();
        ComparePersonnelCountText.Text = CompareSchedule.PersonnelCount.ToString();
        CompareScoreText.Text = CompareSchedule.Score.ToString("F1");
    }

    /// <summary>
    /// 构建比较表格
    /// </summary>
    private void BuildComparisonGrids()
    {
        // 这里是简化版本，实际应该根据数据动态生成完整的表格
        // 包括行头、列头和单元格，并高亮差异的单元格

        // 示例：创建一个简单的提示
        var currentInfo = new TextBlock
        {
            Text = "表格内容将根据实际数据动态生成",
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 40, 0, 0)
        };

        var compareInfo = new TextBlock
        {
            Text = "表格内容将根据实际数据动态生成",
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 40, 0, 0)
        };

        CurrentScheduleGrid.Children.Add(currentInfo);
        CompareScheduleGrid.Children.Add(compareInfo);

        // TODO: 实际实现应该：
        // 1. 遍历所有日期和时段
        // 2. 为每个单元格创建 Border 和 TextBlock
        // 3. 如果单元格在 DifferenceCells 中，应用差异样式
        // 4. 使用 Grid 的 RowDefinitions 和 ColumnDefinitions 布局
    }

    /// <summary>
    /// 检查单元格是否有差异
    /// </summary>
    private bool IsDifferenceCell(DateTime date, int periodIndex, int positionId)
    {
        var cellKey = $"{date:yyyy-MM-dd}_{periodIndex}_{positionId}";
        return DifferenceCells.Contains(cellKey);
    }

    /// <summary>
    /// 应用差异样式到单元格
    /// </summary>
    private void ApplyDifferenceStyle(Border cell)
    {
        cell.Background = (Brush)Application.Current.Resources["SystemFillColorCautionBackgroundBrush"];
        cell.BorderBrush = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
        cell.BorderThickness = new Thickness(2);
    }
}

/// <summary>
/// 排班比较数据
/// </summary>
public class ScheduleComparisonData
{
    /// <summary>
    /// 排班ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 排班名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 日期范围
    /// </summary>
    public string DateRange { get; set; } = string.Empty;

    /// <summary>
    /// 总分配数
    /// </summary>
    public int TotalAssignments { get; set; }

    /// <summary>
    /// 参与人员数
    /// </summary>
    public int PersonnelCount { get; set; }

    /// <summary>
    /// 参与哨位数
    /// </summary>
    public int PositionCount { get; set; }

    /// <summary>
    /// 软约束评分
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 班次数据（用于构建表格）
    /// </summary>
    public Dictionary<string, string> Shifts { get; set; } = new();
}
