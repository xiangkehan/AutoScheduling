using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoScheduling3.Views.Scheduling;

/// <summary>
/// 排班比较对话框
/// </summary>
public sealed partial class CompareSchedulesDialog : ContentDialog
{
    private ObservableCollection<HistoryScheduleItem> _allSchedules = new();
    private ObservableCollection<HistoryScheduleItem> _filteredSchedules = new();

    /// <summary>
    /// 历史排班列表
    /// </summary>
    public List<HistoryScheduleItem>? HistorySchedules { get; set; }

    /// <summary>
    /// 选中的排班ID
    /// </summary>
    public int? SelectedScheduleId { get; private set; }

    public CompareSchedulesDialog()
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

        // 加载历史排班列表
        LoadHistorySchedules();

        // 初始状态
        IsPrimaryButtonEnabled = false;
    }

    /// <summary>
    /// 设置无障碍属性
    /// </summary>
    private void SetAccessibilityProperties()
    {
        SearchBox.SetValue(AutomationProperties.NameProperty, "搜索排班");
        SearchBox.SetValue(AutomationProperties.HelpTextProperty, "输入排班名称进行搜索");

        ScheduleListView.SetValue(AutomationProperties.NameProperty, "历史排班列表");
        ScheduleListView.SetValue(AutomationProperties.HelpTextProperty, "选择要比较的历史排班");
    }

    /// <summary>
    /// 加载历史排班列表
    /// </summary>
    private void LoadHistorySchedules()
    {
        if (HistorySchedules == null || !HistorySchedules.Any())
        {
            ShowEmptyState();
            return;
        }

        _allSchedules.Clear();
        foreach (var schedule in HistorySchedules)
        {
            _allSchedules.Add(schedule);
        }

        // 初始显示所有排班
        UpdateFilteredSchedules(string.Empty);
    }

    /// <summary>
    /// 搜索框文本变化
    /// </summary>
    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var searchText = sender.Text?.Trim() ?? string.Empty;
            UpdateFilteredSchedules(searchText);
        }
    }

    /// <summary>
    /// 更新筛选后的排班列表
    /// </summary>
    private void UpdateFilteredSchedules(string searchText)
    {
        _filteredSchedules.Clear();

        var filtered = string.IsNullOrWhiteSpace(searchText)
            ? _allSchedules
            : _allSchedules.Where(s => s.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        var hasResults = false;
        foreach (var schedule in filtered.OrderByDescending(s => s.CreatedDate))
        {
            _filteredSchedules.Add(schedule);
            hasResults = true;
        }

        ScheduleListView.ItemsSource = _filteredSchedules;

        // 显示或隐藏空状态
        if (hasResults)
        {
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            ScheduleListView.Visibility = Visibility.Visible;
        }
        else
        {
            ShowEmptyState();
        }
    }

    /// <summary>
    /// 显示空状态
    /// </summary>
    private void ShowEmptyState()
    {
        EmptyStatePanel.Visibility = Visibility.Visible;
        ScheduleListView.Visibility = Visibility.Collapsed;
        IsPrimaryButtonEnabled = false;
    }

    /// <summary>
    /// 排班列表选择变化
    /// </summary>
    private void ScheduleListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsPrimaryButtonEnabled = ScheduleListView.SelectedItem != null;
    }

    /// <summary>
    /// 主按钮点击
    /// </summary>
    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();

        try
        {
            if (ScheduleListView.SelectedItem is HistoryScheduleItem selected)
            {
                SelectedScheduleId = selected.Id;
            }
            else
            {
                ShowValidationError("请选择一个排班");
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
}

/// <summary>
/// 历史排班项
/// </summary>
public class HistoryScheduleItem
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
    /// 创建时间
    /// </summary>
    public string CreatedTime { get; set; } = string.Empty;

    /// <summary>
    /// 创建日期（用于排序）
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// 人员数量
    /// </summary>
    public int PersonnelCount { get; set; }

    /// <summary>
    /// 哨位数量
    /// </summary>
    public int PositionCount { get; set; }

    /// <summary>
    /// 班次数量
    /// </summary>
    public int ShiftCount { get; set; }

    /// <summary>
    /// 评分
    /// </summary>
    public string Score { get; set; } = "0";
}
