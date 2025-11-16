using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using AutoScheduling3.History;
using AutoScheduling3.Models;

namespace AutoScheduling3.ViewModels.Scheduling;

/// <summary>
/// 排班进度可视化页面的视图模型
/// </summary>
public partial class SchedulingProgressViewModel : ObservableObject
{
    #region 依赖注入

    private readonly ISchedulingService _schedulingService;
    private readonly NavigationService _navigationService;
    private readonly IScheduleGridExporter _gridExporter;
    private readonly DialogService _dialogService;
    private readonly IHistoryManagement _historyManagement;

    #endregion

    #region 私有字段

    private CancellationTokenSource? _cancellationTokenSource;
    private Stopwatch? _stopwatch;
    private DispatcherQueueTimer? _elapsedTimeTimer;
    private readonly DispatcherQueue _dispatcherQueue;

    #endregion

    #region 进度状态属性

    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    [ObservableProperty]
    private double _progressPercentage;

    /// <summary>
    /// 当前执行阶段
    /// </summary>
    [ObservableProperty]
    private string _currentStage = string.Empty;

    /// <summary>
    /// 阶段描述
    /// </summary>
    [ObservableProperty]
    private string _stageDescription = string.Empty;

    /// <summary>
    /// 是否正在执行
    /// </summary>
    [ObservableProperty]
    private bool _isExecuting;

    /// <summary>
    /// 是否已完成
    /// </summary>
    [ObservableProperty]
    private bool _isCompleted;

    /// <summary>
    /// 是否失败
    /// </summary>
    [ObservableProperty]
    private bool _isFailed;

    /// <summary>
    /// 是否已取消
    /// </summary>
    [ObservableProperty]
    private bool _isCancelled;

    #endregion

    #region 统计信息属性

    /// <summary>
    /// 已完成的分配数量
    /// </summary>
    [ObservableProperty]
    private int _completedAssignments;

    /// <summary>
    /// 总共需要分配的时段数
    /// </summary>
    [ObservableProperty]
    private int _totalSlotsToAssign;

    /// <summary>
    /// 当前正在处理的哨位名称
    /// </summary>
    [ObservableProperty]
    private string _currentPositionName = string.Empty;

    /// <summary>
    /// 当前正在处理的时段信息
    /// </summary>
    [ObservableProperty]
    private string _currentTimeSlot = string.Empty;

    /// <summary>
    /// 已执行时间（格式化字符串）
    /// </summary>
    [ObservableProperty]
    private string _elapsedTime = "00:00:00";

    #endregion

    #region 结果数据属性

    /// <summary>
    /// 排班结果
    /// </summary>
    [ObservableProperty]
    private SchedulingResult? _result;

    /// <summary>
    /// 人员工作量统计列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PersonnelWorkload> _personnelWorkloads = new();

    /// <summary>
    /// 哨位覆盖率统计列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PositionCoverage> _positionCoverages = new();

    /// <summary>
    /// 冲突信息列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ConflictInfo> _conflicts = new();

    #endregion

    #region 表格数据属性

    /// <summary>
    /// 排班表格数据
    /// </summary>
    [ObservableProperty]
    private ScheduleGridData? _gridData;

    /// <summary>
    /// 表格是否全屏显示
    /// </summary>
    [ObservableProperty]
    private bool _isGridFullScreen;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化排班进度视图模型
    /// </summary>
    /// <param name="schedulingService">排班服务</param>
    /// <param name="navigationService">导航服务</param>
    /// <param name="gridExporter">表格导出服务</param>
    /// <param name="dialogService">对话框服务</param>
    /// <param name="historyManagement">历史管理服务</param>
    public SchedulingProgressViewModel(
        ISchedulingService schedulingService,
        NavigationService navigationService,
        IScheduleGridExporter gridExporter,
        DialogService dialogService,
        IHistoryManagement historyManagement)
    {
        _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _gridExporter = gridExporter ?? throw new ArgumentNullException(nameof(gridExporter));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _historyManagement = historyManagement ?? throw new ArgumentNullException(nameof(historyManagement));
        
        // 获取当前线程的 DispatcherQueue
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        
        // 初始化命令
        StartSchedulingCommand = new AsyncRelayCommand<SchedulingRequestDto>(ExecuteStartSchedulingAsync);
        CancelSchedulingCommand = new AsyncRelayCommand(ExecuteCancelSchedulingAsync, CanCancelScheduling);
        SaveScheduleCommand = new AsyncRelayCommand(ExecuteSaveScheduleAsync, CanSaveSchedule);
        DiscardScheduleCommand = new AsyncRelayCommand(ExecuteDiscardScheduleAsync, CanDiscardSchedule);
        ViewDetailedResultCommand = new RelayCommand(ExecuteViewDetailedResult, CanViewDetailedResult);
        ReturnToConfigCommand = new RelayCommand(ExecuteReturnToConfig);
        ToggleGridFullScreenCommand = new AsyncRelayCommand(ExecuteToggleGridFullScreenAsync, CanToggleGridFullScreen);
        ExportGridCommand = new AsyncRelayCommand<string>(ExecuteExportGridAsync, CanExportGrid);
    }

    #endregion

    #region 命令

    /// <summary>
    /// 开始排班命令
    /// </summary>
    public IAsyncRelayCommand<SchedulingRequestDto> StartSchedulingCommand { get; }

    /// <summary>
    /// 取消排班命令
    /// </summary>
    public IAsyncRelayCommand CancelSchedulingCommand { get; }

    /// <summary>
    /// 保存排班命令
    /// </summary>
    public IAsyncRelayCommand SaveScheduleCommand { get; }

    /// <summary>
    /// 放弃排班命令
    /// </summary>
    public IAsyncRelayCommand DiscardScheduleCommand { get; }

    /// <summary>
    /// 查看详细结果命令
    /// </summary>
    public IRelayCommand ViewDetailedResultCommand { get; }

    /// <summary>
    /// 返回配置页面命令
    /// </summary>
    public IRelayCommand ReturnToConfigCommand { get; }

    /// <summary>
    /// 切换表格全屏命令
    /// </summary>
    public IAsyncRelayCommand ToggleGridFullScreenCommand { get; }

    /// <summary>
    /// 导出表格命令
    /// </summary>
    public IAsyncRelayCommand<string> ExportGridCommand { get; }

    #endregion

    #region 排班执行方法

    /// <summary>
    /// 执行排班任务
    /// </summary>
    /// <param name="request">排班请求</param>
    private async Task ExecuteStartSchedulingAsync(SchedulingRequestDto? request)
    {
        if (request == null)
        {
            return;
        }

        try
        {
            // 重置状态
            ResetState();

            // 设置执行状态
            IsExecuting = true;
            IsCompleted = false;
            IsFailed = false;
            IsCancelled = false;

            // 通知命令状态变化
            CancelSchedulingCommand.NotifyCanExecuteChanged();

            // 创建取消令牌源
            _cancellationTokenSource = new CancellationTokenSource();

            // 启动计时器
            StartElapsedTimeTracking();

            // 创建进度报告器
            var progress = new Progress<SchedulingProgressReport>(OnProgressReported);

            // 执行排班
            var result = await _schedulingService.ExecuteSchedulingAsync(
                request,
                progress,
                _cancellationTokenSource.Token);

            // 处理结果
            await ProcessSchedulingResultAsync(result);
        }
        catch (OperationCanceledException)
        {
            // 用户取消
            HandleCancellation();
        }
        catch (Exception ex)
        {
            // 处理异常
            HandleException(ex);
        }
        finally
        {
            // 停止计时器
            StopElapsedTimeTracking();

            // 清理资源
            IsExecuting = false;

            // 通知命令状态变化
            CancelSchedulingCommand.NotifyCanExecuteChanged();
            SaveScheduleCommand.NotifyCanExecuteChanged();
            DiscardScheduleCommand.NotifyCanExecuteChanged();
            ViewDetailedResultCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// 进度报告回调方法
    /// </summary>
    /// <param name="report">进度报告</param>
    private void OnProgressReported(SchedulingProgressReport report)
    {
        // 确保在UI线程上更新
        _dispatcherQueue.TryEnqueue(() =>
        {
            // 更新进度百分比
            ProgressPercentage = report.ProgressPercentage;

            // 更新当前阶段
            CurrentStage = GetStageDisplayName(report.CurrentStage);
            StageDescription = report.StageDescription;

            // 更新统计信息
            CompletedAssignments = report.CompletedAssignments;
            TotalSlotsToAssign = report.TotalSlotsToAssign;

            // 更新当前处理信息
            CurrentPositionName = report.CurrentPositionName ?? string.Empty;
            
            if (report.CurrentPeriodIndex >= 0)
            {
                CurrentTimeSlot = $"{report.CurrentDate:yyyy-MM-dd} 时段 {report.CurrentPeriodIndex}";
            }
            else
            {
                CurrentTimeSlot = string.Empty;
            }
        });
    }

    /// <summary>
    /// 处理排班结果
    /// </summary>
    /// <param name="result">排班结果</param>
    private async Task ProcessSchedulingResultAsync(SchedulingResult result)
    {
        Result = result;

        if (result.IsSuccess)
        {
            // 排班成功
            IsCompleted = true;
            IsFailed = false;

            // 填充统计数据
            if (result.Statistics != null)
            {
                PopulateStatistics(result.Statistics);
            }

            // 构建表格数据
            if (result.Schedule != null)
            {
                GridData = await _schedulingService.BuildScheduleGridData(result.Schedule);
            }
        }
        else
        {
            // 排班失败
            IsCompleted = false;
            IsFailed = true;

            // 填充冲突信息
            if (result.Conflicts != null && result.Conflicts.Count > 0)
            {
                Conflicts.Clear();
                foreach (var conflict in result.Conflicts)
                {
                    Conflicts.Add(conflict);
                }
            }
        }

        // 通知命令状态变化
        SaveScheduleCommand.NotifyCanExecuteChanged();
        DiscardScheduleCommand.NotifyCanExecuteChanged();
        ViewDetailedResultCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 填充统计信息
    /// </summary>
    /// <param name="statistics">统计数据</param>
    private void PopulateStatistics(SchedulingStatistics statistics)
    {
        // 填充人员工作量
        PersonnelWorkloads.Clear();
        if (statistics.PersonnelWorkloads != null)
        {
            foreach (var workload in statistics.PersonnelWorkloads.Values)
            {
                PersonnelWorkloads.Add(workload);
            }
        }

        // 填充哨位覆盖率
        PositionCoverages.Clear();
        if (statistics.PositionCoverages != null)
        {
            foreach (var coverage in statistics.PositionCoverages.Values)
            {
                PositionCoverages.Add(coverage);
            }
        }
    }

    /// <summary>
    /// 处理取消操作
    /// </summary>
    private void HandleCancellation()
    {
        IsCancelled = true;
        IsCompleted = false;
        IsFailed = false;
        CurrentStage = "已取消";
        StageDescription = "排班任务已被用户取消";

        // 通知命令状态变化
        CancelSchedulingCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 处理异常
    /// </summary>
    /// <param name="ex">异常对象</param>
    private void HandleException(Exception ex)
    {
        IsFailed = true;
        IsCompleted = false;
        IsCancelled = false;
        CurrentStage = "执行失败";
        StageDescription = $"排班执行过程中发生错误: {ex.Message}";
    }

    /// <summary>
    /// 重置状态
    /// </summary>
    private void ResetState()
    {
        ProgressPercentage = 0;
        CurrentStage = string.Empty;
        StageDescription = string.Empty;
        CompletedAssignments = 0;
        TotalSlotsToAssign = 0;
        CurrentPositionName = string.Empty;
        CurrentTimeSlot = string.Empty;
        ElapsedTime = "00:00:00";
        
        Result = null;
        GridData = null;
        
        PersonnelWorkloads.Clear();
        PositionCoverages.Clear();
        Conflicts.Clear();
    }

    /// <summary>
    /// 启动执行时间跟踪
    /// </summary>
    private void StartElapsedTimeTracking()
    {
        // 创建并启动 Stopwatch
        _stopwatch = Stopwatch.StartNew();

        // 创建定时器，每秒更新一次显示
        _elapsedTimeTimer = _dispatcherQueue.CreateTimer();
        _elapsedTimeTimer.Interval = TimeSpan.FromSeconds(1);
        _elapsedTimeTimer.Tick += (sender, args) =>
        {
            if (_stopwatch != null && _stopwatch.IsRunning)
            {
                ElapsedTime = _stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
            }
        };
        _elapsedTimeTimer.Start();
    }

    /// <summary>
    /// 停止执行时间跟踪
    /// </summary>
    private void StopElapsedTimeTracking()
    {
        // 停止 Stopwatch
        _stopwatch?.Stop();

        // 停止并释放定时器
        if (_elapsedTimeTimer != null)
        {
            _elapsedTimeTimer.Stop();
            _elapsedTimeTimer = null;
        }

        // 更新最终时间
        if (_stopwatch != null)
        {
            ElapsedTime = _stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
        }
    }

    /// <summary>
    /// 获取阶段显示名称
    /// </summary>
    /// <param name="stage">阶段枚举</param>
    /// <returns>显示名称</returns>
    private string GetStageDisplayName(SchedulingStage stage)
    {
        return stage switch
        {
            SchedulingStage.Initializing => "初始化",
            SchedulingStage.LoadingData => "加载数据",
            SchedulingStage.BuildingContext => "构建上下文",
            SchedulingStage.InitializingTensor => "初始化可行性张量",
            SchedulingStage.ApplyingConstraints => "应用约束",
            SchedulingStage.ApplyingManualAssignments => "应用手动指定",
            SchedulingStage.GreedyAssignment => "贪心分配",
            SchedulingStage.UpdatingScores => "更新评分",
            SchedulingStage.Finalizing => "完成处理",
            SchedulingStage.Completed => "已完成",
            SchedulingStage.Failed => "失败",
            _ => "未知阶段"
        };
    }

    #endregion

    #region 命令实现

    /// <summary>
    /// 执行取消排班命令
    /// </summary>
    private async Task ExecuteCancelSchedulingAsync()
    {
        // 显示确认对话框
        var confirmed = await _dialogService.ShowConfirmAsync(
            "取消排班",
            "确定要取消正在执行的排班任务吗？",
            "确定",
            "取消");

        if (confirmed)
        {
            // 调用取消令牌
            _cancellationTokenSource?.Cancel();
        }
    }

    /// <summary>
    /// 判断是否可以取消排班
    /// </summary>
    private bool CanCancelScheduling()
    {
        return IsExecuting && !IsCancelled;
    }

    /// <summary>
    /// 执行保存排班命令
    /// </summary>
    private async Task ExecuteSaveScheduleAsync()
    {
        if (Result?.Schedule == null)
        {
            await _dialogService.ShowErrorAsync("保存失败", "没有可保存的排班结果");
            return;
        }

        try
        {
            // 将排班结果转换为 Schedule 模型
            var schedule = ConvertToScheduleModel(Result.Schedule);

            // 添加到缓冲区（草稿）
            var bufferId = await _historyManagement.AddToBufferAsync(schedule);

            // 确认缓冲区排班（转为历史记录）
            await _historyManagement.ConfirmBufferScheduleAsync(bufferId);

            // 显示成功消息
            await _dialogService.ShowSuccessAsync("排班已成功保存到历史记录");

            // 导航到排班结果页面，传递排班ID
            _navigationService.NavigateTo("ScheduleResult", Result.Schedule.Id);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("保存失败", $"保存排班时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 判断是否可以保存排班
    /// </summary>
    private bool CanSaveSchedule()
    {
        return IsCompleted && Result?.IsSuccess == true && Result?.Schedule != null;
    }

    /// <summary>
    /// 执行放弃排班命令
    /// </summary>
    private async Task ExecuteDiscardScheduleAsync()
    {
        // 显示确认对话框
        var confirmed = await _dialogService.ShowConfirmAsync(
            "放弃排班",
            "确定要放弃当前的排班结果吗？此操作无法撤销。",
            "确定",
            "取消");

        if (confirmed)
        {
            // 清除结果数据
            ResetState();

            // 显示提示消息
            await _dialogService.ShowMessageAsync("已放弃", "排班结果已清除");

            // 返回到创建排班页面
            _navigationService.NavigateTo("CreateScheduling");
        }
    }

    /// <summary>
    /// 判断是否可以放弃排班
    /// </summary>
    private bool CanDiscardSchedule()
    {
        return IsCompleted && Result != null;
    }

    /// <summary>
    /// 执行查看详细结果命令
    /// </summary>
    private void ExecuteViewDetailedResult()
    {
        if (Result?.Schedule != null)
        {
            // 导航到排班结果页面，传递排班ID
            _navigationService.NavigateTo("ScheduleResult", Result.Schedule.Id);
        }
    }

    /// <summary>
    /// 判断是否可以查看详细结果
    /// </summary>
    private bool CanViewDetailedResult()
    {
        return IsCompleted && Result?.IsSuccess == true && Result?.Schedule != null;
    }

    /// <summary>
    /// 执行返回配置页面命令
    /// </summary>
    private void ExecuteReturnToConfig()
    {
        // 导航回创建排班页面
        _navigationService.NavigateTo("CreateScheduling");
    }

    /// <summary>
    /// 将 ScheduleDto 转换为 Schedule 模型
    /// </summary>
    private Schedule ConvertToScheduleModel(ScheduleDto dto)
    {
        var schedule = new Schedule
        {
            Id = dto.Id,
            Header = dto.Title,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            CreatedAt = dto.CreatedAt,
            PersonnelIds = dto.PersonnelIds,
            PositionIds = dto.PositionIds,
            Results = new List<SingleShift>()
        };

        // 转换班次数据
        if (dto.Shifts != null)
        {
            foreach (var shift in dto.Shifts)
            {
                schedule.Results.Add(new SingleShift
                {
                    ScheduleId = dto.Id,
                    PersonnelId = shift.PersonnelId,
                    PositionId = shift.PositionId,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime,
                    TimeSlotIndex = shift.PeriodIndex
                });
            }
        }

        return schedule;
    }

    #endregion

    #region 表格相关命令实现

    /// <summary>
    /// 执行切换表格全屏命令
    /// </summary>
    private async Task ExecuteToggleGridFullScreenAsync()
    {
        if (IsGridFullScreen)
        {
            // 如果已经是全屏，则退出全屏
            IsGridFullScreen = false;
        }
        else
        {
            // 显示全屏对话框
            await ShowGridFullScreenAsync();
        }
    }

    /// <summary>
    /// 判断是否可以切换表格全屏
    /// </summary>
    private bool CanToggleGridFullScreen()
    {
        return GridData != null;
    }

    /// <summary>
    /// 显示全屏表格对话框
    /// </summary>
    public async Task ShowGridFullScreenAsync()
    {
        if (GridData == null)
        {
            await _dialogService.ShowWarningAsync("没有可显示的排班表格数据");
            return;
        }

        try
        {
            // 创建全屏对话框
            var dialog = new ContentDialog
            {
                Title = "排班结果表格",
                Content = new Microsoft.UI.Xaml.Controls.ScrollViewer
                {
                    HorizontalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto,
                    Content = new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = $"排班表格全屏视图\n\n开始日期: {GridData.StartDate:yyyy-MM-dd}\n结束日期: {GridData.EndDate:yyyy-MM-dd}\n总天数: {GridData.TotalDays}\n总时段数: {GridData.TotalPeriods}\n哨位数: {GridData.Columns?.Count ?? 0}\n\n注意：完整的表格控件将在后续任务中实现。",
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                        Margin = new Microsoft.UI.Xaml.Thickness(20)
                    }
                },
                CloseButtonText = "关闭",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };

            // 设置对话框样式为全屏（如果有自定义样式）
            // dialog.Style = (Style)Application.Current.Resources["FullScreenDialogStyle"];

            IsGridFullScreen = true;
            await dialog.ShowAsync();
            IsGridFullScreen = false;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("显示全屏表格失败", $"无法显示全屏表格：{ex.Message}");
        }
    }

    /// <summary>
    /// 执行导出表格命令
    /// </summary>
    /// <param name="format">导出格式（如 "excel", "csv", "pdf"）</param>
    private async Task ExecuteExportGridAsync(string? format)
    {
        if (GridData == null)
        {
            await _dialogService.ShowWarningAsync("没有可导出的排班表格数据");
            return;
        }

        // 当前版本显示"功能开发中"对话框
        var supportedFormats = _gridExporter.GetSupportedFormats();
        var formatsText = string.Join("、", supportedFormats);

        await _dialogService.ShowMessageAsync(
            "功能开发中",
            $"表格导出功能正在开发中。\n\n计划支持的格式：{formatsText}\n\n此功能将在后续版本中提供。");
    }

    /// <summary>
    /// 判断是否可以导出表格
    /// </summary>
    private bool CanExportGrid(string? format)
    {
        return GridData != null;
    }

    #endregion
}
