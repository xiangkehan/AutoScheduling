using System;
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
    public SchedulingProgressViewModel(
        ISchedulingService schedulingService,
        NavigationService navigationService,
        IScheduleGridExporter gridExporter)
    {
        _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _gridExporter = gridExporter ?? throw new ArgumentNullException(nameof(gridExporter));
        
        // 获取当前线程的 DispatcherQueue
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        
        // 初始化命令
        StartSchedulingCommand = new AsyncRelayCommand<SchedulingRequestDto>(ExecuteStartSchedulingAsync);
    }

    #endregion

    #region 命令

    /// <summary>
    /// 开始排班命令
    /// </summary>
    public IAsyncRelayCommand<SchedulingRequestDto> StartSchedulingCommand { get; }

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
}
