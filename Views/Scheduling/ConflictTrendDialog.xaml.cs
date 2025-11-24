using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;

namespace AutoScheduling3.Views.Scheduling;

/// <summary>
/// 冲突趋势对话框
/// </summary>
public sealed partial class ConflictTrendDialog : ContentDialog
{
    private readonly IConflictReportService _conflictReportService;
    private int _scheduleId;
    private ConflictTrendData? _trendData;
    private List<ConflictDto>? _allConflicts;

    /// <summary>
    /// 趋势图数据项
    /// </summary>
    private class TrendChartItem
    {
        public int Count { get; set; }
        public double BarHeight { get; set; }
        public string DateLabel { get; set; } = string.Empty;
    }

    /// <summary>
    /// 类型分布数据项
    /// </summary>
    private class TypeDistributionItem
    {
        public string TypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public SolidColorBrush BarColor { get; set; } = new SolidColorBrush(Colors.Gray);
    }

    public ConflictTrendDialog(IConflictReportService conflictReportService)
    {
        _conflictReportService = conflictReportService ?? throw new ArgumentNullException(nameof(conflictReportService));
        InitializeComponent();
    }

    /// <summary>
    /// 初始化对话框
    /// </summary>
    /// <param name="scheduleId">排班ID</param>
    /// <param name="allConflicts">所有冲突列表（用于生成趋势数据）</param>
    public async Task InitializeAsync(int scheduleId, List<ConflictDto> allConflicts)
    {
        _scheduleId = scheduleId;
        _allConflicts = allConflicts ?? new List<ConflictDto>();

        // 设置默认日期范围（最近30天）
        var endDate = DateTime.Now.Date;
        var startDate = endDate.AddDays(-29);
        
        StartDatePicker.Date = startDate;
        EndDatePicker.Date = endDate;

        // 加载趋势数据
        await LoadTrendDataAsync(startDate, endDate);
    }

    /// <summary>
    /// 加载趋势数据
    /// </summary>
    private async Task LoadTrendDataAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            // 基于当前冲突列表生成趋势数据
            _trendData = GenerateTrendDataFromConflicts(startDate, endDate);

            // 更新UI
            UpdateStatistics();
            UpdateTrendChart();
            UpdateTypeDistribution();
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // 显示错误提示
            var errorDialog = new ContentDialog
            {
                Title = "加载失败",
                Content = $"无法加载趋势数据：{ex.Message}",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }

    /// <summary>
    /// 从冲突列表生成趋势数据
    /// </summary>
    private ConflictTrendData GenerateTrendDataFromConflicts(DateTime startDate, DateTime endDate)
    {
        var trendData = new ConflictTrendData
        {
            StartDate = startDate,
            EndDate = endDate
        };

        if (_allConflicts == null || _allConflicts.Count == 0)
        {
            return trendData;
        }

        // 筛选日期范围内的冲突
        var conflictsInRange = _allConflicts
            .Where(c => c.StartTime.HasValue && 
                       c.StartTime.Value.Date >= startDate.Date && 
                       c.StartTime.Value.Date <= endDate.Date)
            .ToList();

        // 按日期统计冲突数量
        var currentDate = startDate.Date;
        while (currentDate <= endDate.Date)
        {
            var count = conflictsInRange.Count(c => c.StartTime!.Value.Date == currentDate);
            trendData.ConflictsByDate[currentDate] = count;
            currentDate = currentDate.AddDays(1);
        }

        // 按类型统计冲突数量
        var conflictsByType = conflictsInRange
            .GroupBy(c => c.SubType)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var kvp in conflictsByType)
        {
            trendData.ConflictsByType[kvp.Key] = kvp.Value;
        }

        // 统计总数和状态
        trendData.TotalConflicts = _allConflicts.Count;
        trendData.IgnoredConflicts = _allConflicts.Count(c => c.IsIgnored);
        trendData.ResolvedConflicts = 0; // 目前没有"已解决"状态，可以后续扩展
        trendData.PendingConflicts = trendData.TotalConflicts - trendData.IgnoredConflicts - trendData.ResolvedConflicts;

        return trendData;
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics()
    {
        if (_trendData == null) return;

        TotalConflictsText.Text = _trendData.TotalConflicts.ToString();
        ResolvedConflictsText.Text = _trendData.ResolvedConflicts.ToString();
        IgnoredConflictsText.Text = _trendData.IgnoredConflicts.ToString();
        PendingConflictsText.Text = _trendData.PendingConflicts.ToString();

        // 计算百分比
        if (_trendData.TotalConflicts > 0)
        {
            var resolvedPercentage = (_trendData.ResolvedConflicts * 100.0 / _trendData.TotalConflicts);
            var ignoredPercentage = (_trendData.IgnoredConflicts * 100.0 / _trendData.TotalConflicts);
            var pendingPercentage = (_trendData.PendingConflicts * 100.0 / _trendData.TotalConflicts);

            ResolvedPercentageText.Text = $"({resolvedPercentage:F1}%)";
            IgnoredPercentageText.Text = $"({ignoredPercentage:F1}%)";
            PendingPercentageText.Text = $"({pendingPercentage:F1}%)";
        }
        else
        {
            ResolvedPercentageText.Text = "(0%)";
            IgnoredPercentageText.Text = "(0%)";
            PendingPercentageText.Text = "(0%)";
        }
    }

    /// <summary>
    /// 更新趋势图
    /// </summary>
    private void UpdateTrendChart()
    {
        if (_trendData == null || _trendData.ConflictsByDate.Count == 0)
        {
            TrendChartPlaceholder.Visibility = Visibility.Visible;
            TrendChartScrollViewer.Visibility = Visibility.Collapsed;
            return;
        }

        TrendChartPlaceholder.Visibility = Visibility.Collapsed;
        TrendChartScrollViewer.Visibility = Visibility.Visible;

        // 准备图表数据
        var maxCount = _trendData.ConflictsByDate.Values.Max();
        var maxBarHeight = 150.0; // 最大柱高度

        var chartItems = _trendData.ConflictsByDate
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new TrendChartItem
            {
                Count = kvp.Value,
                BarHeight = maxCount > 0 ? (kvp.Value * maxBarHeight / maxCount) : 0,
                DateLabel = kvp.Key.ToString("MM/dd")
            })
            .ToList();

        TrendChartItemsControl.ItemsSource = chartItems;
    }

    /// <summary>
    /// 更新类型分布
    /// </summary>
    private void UpdateTypeDistribution()
    {
        if (_trendData == null || _trendData.ConflictsByType.Count == 0)
        {
            TypeDistributionItemsControl.Visibility = Visibility.Collapsed;
            TypeDistributionPlaceholder.Visibility = Visibility.Visible;
            return;
        }

        TypeDistributionItemsControl.Visibility = Visibility.Visible;
        TypeDistributionPlaceholder.Visibility = Visibility.Collapsed;

        var totalCount = _trendData.ConflictsByType.Values.Sum();
        
        var distributionItems = _trendData.ConflictsByType
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => new TypeDistributionItem
            {
                TypeName = GetConflictTypeDisplayName(kvp.Key),
                Count = kvp.Value,
                Percentage = totalCount > 0 ? (kvp.Value * 100.0 / totalCount) : 0,
                BarColor = GetConflictTypeColor(kvp.Key)
            })
            .ToList();

        TypeDistributionItemsControl.ItemsSource = distributionItems;
    }

    /// <summary>
    /// 获取冲突类型显示名称
    /// </summary>
    private string GetConflictTypeDisplayName(ConflictSubType subType)
    {
        return subType switch
        {
            ConflictSubType.SkillMismatch => "技能不匹配",
            ConflictSubType.PersonnelUnavailable => "人员不可用",
            ConflictSubType.DuplicateAssignment => "重复分配",
            ConflictSubType.InsufficientRest => "休息时间不足",
            ConflictSubType.ExcessiveWorkload => "工作量过大",
            ConflictSubType.WorkloadImbalance => "工作量不均衡",
            ConflictSubType.ConsecutiveOvertime => "连续工作超时",
            ConflictSubType.UnassignedSlot => "未分配时段",
            ConflictSubType.SuboptimalAssignment => "次优分配",
            _ => "其他"
        };
    }

    /// <summary>
    /// 获取冲突类型颜色
    /// </summary>
    private SolidColorBrush GetConflictTypeColor(ConflictSubType subType)
    {
        return subType switch
        {
            ConflictSubType.SkillMismatch => new SolidColorBrush(Color.FromArgb(255, 197, 15, 31)), // 红色
            ConflictSubType.PersonnelUnavailable => new SolidColorBrush(Color.FromArgb(255, 247, 99, 12)), // 橙色
            ConflictSubType.DuplicateAssignment => new SolidColorBrush(Color.FromArgb(255, 202, 80, 16)), // 深橙色
            ConflictSubType.InsufficientRest => new SolidColorBrush(Color.FromArgb(255, 255, 185, 0)), // 黄色
            ConflictSubType.ExcessiveWorkload => new SolidColorBrush(Color.FromArgb(255, 255, 140, 0)), // 琥珀色
            ConflictSubType.WorkloadImbalance => new SolidColorBrush(Color.FromArgb(255, 16, 137, 62)), // 绿色
            ConflictSubType.ConsecutiveOvertime => new SolidColorBrush(Color.FromArgb(255, 0, 120, 212)), // 蓝色
            ConflictSubType.UnassignedSlot => new SolidColorBrush(Color.FromArgb(255, 118, 118, 118)), // 灰色
            ConflictSubType.SuboptimalAssignment => new SolidColorBrush(Color.FromArgb(255, 135, 100, 184)), // 紫色
            _ => new SolidColorBrush(Color.FromArgb(255, 150, 150, 150)) // 默认灰色
        };
    }

    /// <summary>
    /// 应用日期范围按钮点击事件
    /// </summary>
    private async void OnApplyDateRangeClick(object sender, RoutedEventArgs e)
    {
        var startDate = StartDatePicker.Date.Date;
        var endDate = EndDatePicker.Date.Date;

        // 验证日期范围
        if (startDate > endDate)
        {
            var errorDialog = new ContentDialog
            {
                Title = "日期错误",
                Content = "开始日期不能晚于结束日期",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
            return;
        }

        // 重新加载数据
        await LoadTrendDataAsync(startDate, endDate);
    }

    /// <summary>
    /// 导出报告按钮点击事件
    /// </summary>
    private async void OnExportReportClick(object sender, RoutedEventArgs e)
    {
        if (_allConflicts == null || _allConflicts.Count == 0)
        {
            var noDataDialog = new ContentDialog
            {
                Title = "无数据",
                Content = "没有可导出的冲突数据",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await noDataDialog.ShowAsync();
            return;
        }

        try
        {
            // 创建文件保存选择器
            var savePicker = new FileSavePicker();
            
            // 获取窗口句柄
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.SuggestedFileName = $"冲突报告_{DateTime.Now:yyyyMMdd_HHmmss}";
            savePicker.FileTypeChoices.Add("Excel 文件", new List<string> { ".xlsx" });

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // 生成报告（这里需要传入排班数据，暂时使用空对象）
                var reportBytes = await _conflictReportService.ExportConflictReportAsync(
                    _allConflicts, 
                    new ScheduleDto(), // TODO: 传入实际的排班数据
                    "excel");

                // 保存文件
                await FileIO.WriteBytesAsync(file, reportBytes);

                // 显示成功提示
                var successDialog = new ContentDialog
                {
                    Title = "导出成功",
                    Content = $"报告已保存到：{file.Path}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new ContentDialog
            {
                Title = "导出失败",
                Content = $"无法导出报告：{ex.Message}",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }
}
