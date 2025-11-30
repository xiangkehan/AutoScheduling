using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

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
    private readonly ThrottledAutoSaver? _autoSaver;

    #endregion

    #region 私有字段

    private CancellationTokenSource? _cancellationTokenSource;
    private Stopwatch? _stopwatch;
    private DispatcherQueueTimer? _elapsedTimeTimer;
    private readonly DispatcherQueue _dispatcherQueue;
    private SchedulingRequestDto? _currentRequest;
    private SchedulingProgressReport? _latestProgressReport;

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

    /// <summary>
    /// 阶段历史列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<StageHistoryItem> _stageHistory = new();

    /// <summary>
    /// 警告信息列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _warnings = new();

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

    /// <summary>
    /// 上次自动保存时间
    /// </summary>
    [ObservableProperty]
    private string _lastAutoSaveTime = "从未保存";

    /// <summary>
    /// 是否正在保存草稿
    /// </summary>
    [ObservableProperty]
    private bool _isSavingDraft;

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
            await _dialogService.ShowErrorAsync("参数错误", "排班请求参数不能为空");
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
            await HandleCancellationAsync();
        }
        catch (ArgumentException ex)
        {
            // 参数验证错误
            await HandleArgumentExceptionAsync(ex);
        }
        catch (InvalidOperationException ex)
        {
            // 业务逻辑错误
            await HandleInvalidOperationExceptionAsync(ex);
        }
        catch (Exception ex)
        {
            // 未预期的系统错误
            await HandleUnexpectedExceptionAsync(ex);
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

            // 更新阶段历史
            UpdateStageHistory(report.CurrentStage);

            // 更新警告信息
            if (report.Warnings != null && report.Warnings.Count > 0)
            {
                foreach (var warning in report.Warnings)
                {
                    if (!Warnings.Contains(warning))
                    {
                        Warnings.Add(warning);
                    }
                }
            }
        });
    }

    /// <summary>
    /// 更新阶段历史
    /// </summary>
    /// <param name="currentStage">当前阶段</param>
    private void UpdateStageHistory(SchedulingStage currentStage)
    {
        var stageName = GetStageDisplayName(currentStage);
        
        // 查找是否已存在该阶段
        var existingStage = StageHistory.FirstOrDefault(s => s.StageName == stageName);
        
        if (existingStage != null)
        {
            // 更新现有阶段状态
            if (currentStage == SchedulingStage.Failed)
            {
                existingStage.Status = "Failed";
            }
            else if (currentStage == SchedulingStage.Completed)
            {
                existingStage.Status = "Completed";
                existingStage.CompletedTime = DateTime.Now;
            }
            else
            {
                existingStage.Status = "InProgress";
            }
        }
        else
        {
            // 将之前的阶段标记为已完成
            foreach (var stage in StageHistory)
            {
                if (stage.Status == "InProgress")
                {
                    stage.Status = "Completed";
                    stage.CompletedTime = DateTime.Now;
                }
            }

            // 添加新阶段
            var newStage = new StageHistoryItem
            {
                StageName = stageName,
                Status = currentStage == SchedulingStage.Failed ? "Failed" : 
                         currentStage == SchedulingStage.Completed ? "Completed" : "InProgress"
            };

            if (newStage.Status == "Completed")
            {
                newStage.CompletedTime = DateTime.Now;
            }

            StageHistory.Add(newStage);
        }
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
            CurrentStage = "已完成";
            StageDescription = "排班任务已成功完成";

            // 更新阶段历史
            UpdateStageHistory(SchedulingStage.Completed);

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

            // 显示成功消息
            await _dialogService.ShowSuccessAsync(
                $"排班成功完成！\n\n总分配数：{result.Statistics?.TotalAssignments ?? 0}\n执行时间：{ElapsedTime}");
        }
        else
        {
            // 排班失败
            IsCompleted = false;
            IsFailed = true;
            CurrentStage = "排班失败";
            StageDescription = result.ErrorMessage ?? "排班执行失败";

            // 更新阶段历史
            UpdateStageHistory(SchedulingStage.Failed);

            // 填充冲突信息
            if (result.Conflicts != null && result.Conflicts.Count > 0)
            {
                Conflicts.Clear();
                foreach (var conflict in result.Conflicts)
                {
                    Conflicts.Add(conflict);
                }
            }

            // 显示失败消息和建议
            await ShowFailureMessageAsync(result);
        }

        // 通知命令状态变化
        SaveScheduleCommand.NotifyCanExecuteChanged();
        DiscardScheduleCommand.NotifyCanExecuteChanged();
        ViewDetailedResultCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 显示失败消息和建议
    /// </summary>
    /// <param name="result">排班结果</param>
    private async Task ShowFailureMessageAsync(SchedulingResult result)
    {
        var errorMessage = "排班失败\n\n";
        
        // 添加失败原因
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            errorMessage += $"失败原因：{result.ErrorMessage}\n\n";
        }

        // 添加统计信息
        errorMessage += $"已完成分配：{CompletedAssignments}/{TotalSlotsToAssign}\n";
        errorMessage += $"执行时间：{ElapsedTime}\n";

        // 添加冲突信息摘要
        if (Conflicts.Count > 0)
        {
            errorMessage += $"\n发现 {Conflicts.Count} 个冲突\n";
            
            // 按冲突类型分组统计
            var conflictGroups = Conflicts.GroupBy(c => c.ConflictType).ToList();
            foreach (var group in conflictGroups)
            {
                errorMessage += $"• {group.Key}：{group.Count()} 个\n";
            }
        }

        // 添加建议
        errorMessage += "\n建议：\n";
        errorMessage += "• 点击下方查看冲突详情\n";
        errorMessage += "• 调整约束条件后重新排班\n";
        errorMessage += "• 增加可用人员或减少排班需求\n";
        errorMessage += "• 点击\"返回修改\"按钮返回配置页面";

        await _dialogService.ShowErrorAsync("排班失败", errorMessage);

        // 如果有冲突，询问是否查看详情
        if (Conflicts.Count > 0)
        {
            var viewDetails = await _dialogService.ShowConfirmAsync(
                "查看冲突详情",
                "是否查看详细的冲突信息？",
                "查看",
                "稍后");

            if (viewDetails)
            {
                await ShowConflictDetailsAsync();
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
    private async Task HandleCancellationAsync()
    {
        IsCancelled = true;
        IsCompleted = false;
        IsFailed = false;
        CurrentStage = "已取消";
        StageDescription = "排班任务已被用户取消";

        // 更新阶段历史
        UpdateStageHistory(SchedulingStage.Failed);

        // 通知命令状态变化
        CancelSchedulingCommand.NotifyCanExecuteChanged();

        // 显示取消消息
        await _dialogService.ShowMessageAsync(
            "排班已取消",
            $"排班任务已被取消。\n\n已完成分配：{CompletedAssignments}/{TotalSlotsToAssign}\n执行时间：{ElapsedTime}");
    }

    /// <summary>
    /// 处理参数异常
    /// </summary>
    /// <param name="ex">参数异常对象</param>
    private async Task HandleArgumentExceptionAsync(ArgumentException ex)
    {
        IsFailed = true;
        IsCompleted = false;
        IsCancelled = false;
        CurrentStage = "参数错误";
        StageDescription = "排班参数验证失败";

        // 更新阶段历史
        UpdateStageHistory(SchedulingStage.Failed);

        // 构建详细错误消息
        var errorMessage = $"排班参数验证失败，请检查以下内容：\n\n{ex.Message}\n\n建议：\n";
        errorMessage += "• 检查人员和哨位配置是否完整\n";
        errorMessage += "• 确认日期范围设置正确\n";
        errorMessage += "• 验证约束条件是否合理\n";
        errorMessage += "• 确保所有必填字段已填写";

        // 显示错误对话框
        await _dialogService.ShowErrorAsync("参数验证失败", errorMessage);
    }

    /// <summary>
    /// 处理业务逻辑异常
    /// </summary>
    /// <param name="ex">业务逻辑异常对象</param>
    private async Task HandleInvalidOperationExceptionAsync(InvalidOperationException ex)
    {
        IsFailed = true;
        IsCompleted = false;
        IsCancelled = false;
        CurrentStage = "业务逻辑错误";
        StageDescription = "排班执行过程中遇到业务逻辑错误";

        // 更新阶段历史
        UpdateStageHistory(SchedulingStage.Failed);

        // 构建详细错误消息
        var errorMessage = $"排班执行失败：\n\n{ex.Message}\n\n";
        
        // 根据错误消息内容提供具体建议
        if (ex.Message.Contains("无可行解") || ex.Message.Contains("无法分配"))
        {
            errorMessage += "可能的原因：\n";
            errorMessage += "• 约束条件过于严格，导致无法找到可行的排班方案\n";
            errorMessage += "• 人员数量不足以覆盖所有哨位和时段\n";
            errorMessage += "• 技能匹配要求过高\n\n";
            errorMessage += "建议：\n";
            errorMessage += "• 适当放宽约束条件\n";
            errorMessage += "• 增加可用人员数量\n";
            errorMessage += "• 调整技能要求或培训更多人员\n";
            errorMessage += "• 减少需要覆盖的哨位或时段";
        }
        else if (ex.Message.Contains("约束冲突") || ex.Message.Contains("冲突"))
        {
            errorMessage += "可能的原因：\n";
            errorMessage += "• 存在相互矛盾的约束条件\n";
            errorMessage += "• 手动指定的分配违反了约束规则\n";
            errorMessage += "• 人员休假或不可用时间与排班需求冲突\n\n";
            errorMessage += "建议：\n";
            errorMessage += "• 检查并解决冲突的约束条件\n";
            errorMessage += "• 调整手动指定的分配\n";
            errorMessage += "• 更新人员可用性信息";
        }
        else
        {
            errorMessage += "建议：\n";
            errorMessage += "• 检查排班配置是否正确\n";
            errorMessage += "• 确认数据完整性\n";
            errorMessage += "• 尝试调整约束条件后重新排班";
        }

        errorMessage += $"\n\n已完成分配：{CompletedAssignments}/{TotalSlotsToAssign}";

        // 显示错误对话框
        await _dialogService.ShowErrorAsync("排班执行失败", errorMessage);

        // 如果有冲突信息，显示冲突详情
        if (Conflicts.Count > 0)
        {
            await ShowConflictDetailsAsync();
        }
    }

    /// <summary>
    /// 处理未预期的系统异常
    /// </summary>
    /// <param name="ex">异常对象</param>
    private async Task HandleUnexpectedExceptionAsync(Exception ex)
    {
        IsFailed = true;
        IsCompleted = false;
        IsCancelled = false;
        CurrentStage = "系统错误";
        StageDescription = "排班执行过程中发生系统错误";

        // 更新阶段历史
        UpdateStageHistory(SchedulingStage.Failed);

        // 构建详细错误消息
        var errorMessage = $"排班执行过程中发生系统错误：\n\n{ex.Message}\n\n";
        errorMessage += "错误类型：" + ex.GetType().Name + "\n\n";
        errorMessage += "建议：\n";
        errorMessage += "• 请稍后重试\n";
        errorMessage += "• 如果问题持续存在，请联系技术支持\n";
        errorMessage += "• 检查系统日志以获取更多详细信息";

        if (ex.InnerException != null)
        {
            errorMessage += $"\n\n内部错误：{ex.InnerException.Message}";
        }

        errorMessage += $"\n\n已完成分配：{CompletedAssignments}/{TotalSlotsToAssign}";
        errorMessage += $"\n执行时间：{ElapsedTime}";

        // 显示错误对话框
        await _dialogService.ShowErrorAsync("系统错误", errorMessage);

        // 记录到日志（如果有日志系统）
        System.Diagnostics.Debug.WriteLine($"Scheduling execution failed with exception: {ex}");
    }

    /// <summary>
    /// 显示冲突详情对话框
    /// </summary>
    private async Task ShowConflictDetailsAsync()
    {
        if (Conflicts.Count == 0)
        {
            return;
        }

        var conflictMessage = $"发现 {Conflicts.Count} 个冲突：\n\n";
        
        // 限制显示前10个冲突，避免消息过长
        var displayConflicts = Conflicts.Take(10).ToList();
        
        foreach (var conflict in displayConflicts)
        {
            conflictMessage += $"• {conflict.ConflictType}";
            
            if (!string.IsNullOrEmpty(conflict.PositionName))
            {
                conflictMessage += $" - 哨位：{conflict.PositionName}";
            }
            
            if (conflict.Date.HasValue)
            {
                conflictMessage += $" - 日期：{conflict.Date.Value:yyyy-MM-dd}";
            }
            
            if (conflict.PeriodIndex.HasValue)
            {
                conflictMessage += $" - 时段：{conflict.PeriodIndex.Value}";
            }
            
            if (!string.IsNullOrEmpty(conflict.Description))
            {
                conflictMessage += $"\n  {conflict.Description}";
            }
            
            conflictMessage += "\n\n";
        }

        if (Conflicts.Count > 10)
        {
            conflictMessage += $"... 还有 {Conflicts.Count - 10} 个冲突未显示\n\n";
        }

        conflictMessage += "请返回修改配置后重新排班。";

        await _dialogService.ShowErrorAsync("冲突详情", conflictMessage);
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
        StageHistory.Clear();
        Warnings.Clear();
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
            // 显示加载对话框
            var loadingDialog = _dialogService.ShowLoadingDialog("正在保存排班结果...");

            try
            {
                // 将排班结果转换为 Schedule 模型
                var schedule = ConvertToScheduleModel(Result.Schedule);

                // 添加到缓冲区（草稿）
                var bufferId = await _historyManagement.AddToBufferAsync(schedule);

                // 确认缓冲区排班（转为历史记录）
                await _historyManagement.ConfirmBufferScheduleAsync(bufferId);

                // 关闭加载对话框
                loadingDialog.Hide();

                // 显示成功消息
                await _dialogService.ShowSuccessAsync(
                    $"排班已成功保存到历史记录\n\n排班标题：{Result.Schedule.Title}\n总分配数：{Result.Statistics?.TotalAssignments ?? 0}");

                // 导航到排班结果页面，传递排班ID
                _navigationService.NavigateTo("ScheduleResult", Result.Schedule.Id);
            }
            catch
            {
                // 确保关闭加载对话框
                loadingDialog.Hide();
                throw;
            }
        }
        catch (ArgumentException ex)
        {
            await _dialogService.ShowErrorAsync(
                "保存失败",
                $"排班数据验证失败：\n\n{ex.Message}\n\n请检查排班数据的完整性。");
        }
        catch (InvalidOperationException ex)
        {
            await _dialogService.ShowErrorAsync(
                "保存失败",
                $"保存操作失败：\n\n{ex.Message}\n\n可能的原因：\n• 数据库连接失败\n• 数据已存在\n• 权限不足");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "保存失败",
                $"保存排班时发生错误：\n\n{ex.Message}\n\n请稍后重试或联系技术支持。");
            
            // 记录详细错误信息
            System.Diagnostics.Debug.WriteLine($"Failed to save schedule: {ex}");
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
        try
        {
            // 构建确认消息
            var confirmMessage = "确定要放弃当前的排班结果吗？此操作无法撤销。\n\n";
            
            if (Result?.IsSuccess == true && Result.Statistics != null)
            {
                confirmMessage += $"当前排班信息：\n";
                confirmMessage += $"• 总分配数：{Result.Statistics.TotalAssignments}\n";
                confirmMessage += $"• 执行时间：{ElapsedTime}\n";
            }
            else if (IsFailed)
            {
                confirmMessage += $"当前状态：排班失败\n";
                confirmMessage += $"• 已完成分配：{CompletedAssignments}/{TotalSlotsToAssign}\n";
            }
            else if (IsCancelled)
            {
                confirmMessage += $"当前状态：已取消\n";
                confirmMessage += $"• 已完成分配：{CompletedAssignments}/{TotalSlotsToAssign}\n";
            }

            // 显示确认对话框
            var confirmed = await _dialogService.ShowConfirmAsync(
                "放弃排班",
                confirmMessage,
                "确定放弃",
                "取消");

            if (confirmed)
            {
                // 清除结果数据
                ResetState();

                // 显示提示消息
                await _dialogService.ShowMessageAsync("已放弃", "排班结果已清除，即将返回配置页面");

                // 返回到创建排班页面
                _navigationService.NavigateTo("CreateScheduling");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "操作失败",
                $"放弃排班时发生错误：\n\n{ex.Message}");
            
            System.Diagnostics.Debug.WriteLine($"Failed to discard schedule: {ex}");
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
        try
        {
            if (GridData == null)
            {
                await _dialogService.ShowWarningAsync("没有可显示的排班表格数据");
                return;
            }

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
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "显示失败",
                $"切换全屏模式时发生错误：\n\n{ex.Message}");
            
            System.Diagnostics.Debug.WriteLine($"Failed to toggle grid full screen: {ex}");
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
            // 准备全屏视图参数
            var parameter = new FullScreenViewParameter
            {
                ViewMode = ViewMode.Grid,
                GridData = GridData,
                Title = "排班表格 - 全屏视图"
            };

            // 导航到全屏视图页面
            IsGridFullScreen = true;
            _navigationService.NavigateTo("ScheduleGridFullScreen", parameter);
        }
        catch (Exception ex)
        {
            IsGridFullScreen = false;
            await _dialogService.ShowErrorAsync("显示全屏表格失败", $"无法显示全屏表格：{ex.Message}");
        }
    }

    /// <summary>
    /// 执行导出表格命令
    /// </summary>
    /// <param name="format">导出格式（如 "excel", "csv", "pdf"）</param>
    private async Task ExecuteExportGridAsync(string? format)
    {
        try
        {
            if (GridData == null)
            {
                await _dialogService.ShowWarningAsync("没有可导出的排班表格数据");
                return;
            }

            // 验证格式
            if (!string.IsNullOrEmpty(format) && !_gridExporter.IsFormatSupported(format))
            {
                var supportedFormats = _gridExporter.GetSupportedFormats();
                var formatsText = string.Join("、", supportedFormats);
                
                await _dialogService.ShowErrorAsync(
                    "不支持的格式",
                    $"不支持的导出格式：{format}\n\n支持的格式：{formatsText}");
                return;
            }

            // 获取支持的导出格式
            var exportFormats = _gridExporter.GetSupportedFormats();
            var exportFormatsText = string.Join("、", exportFormats.Select(f => f.ToUpper()));

            // 显示格式选择对话框
            var dialog = new ContentDialog
            {
                Title = "导出排班表格",
                CloseButtonText = "关闭",
                PrimaryButtonText = "确定",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            var formatComboBox = new ComboBox
            {
                ItemsSource = exportFormats,
                SelectedIndex = 0,
                Margin = new Thickness(0, 12, 0, 0)
            };
            dialog.Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = $"请选择导出格式 ({exportFormatsText}):" },
                    formatComboBox
                }
            };
            dialog.XamlRoot = App.MainWindow.Content.XamlRoot;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var selectedFormat = formatComboBox.SelectedItem as string;
                if (!string.IsNullOrEmpty(selectedFormat))
                {
                    // 执行导出
                    await PerformExportAsync(selectedFormat);
                }
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "导出失败",
                $"导出表格时发生错误：\n\n{ex.Message}");
            
            System.Diagnostics.Debug.WriteLine($"Failed to export grid: {ex}");
        }
    }

    /// <summary>
    /// 判断是否可以导出表格
    /// </summary>
    private bool CanExportGrid(string? format)
    {
        return GridData != null;
    }

    /// <summary>
    /// 执行导出操作
    /// </summary>
    /// <param name="format">导出格式</param>
    private async Task PerformExportAsync(string format)
    {
        if (GridData == null)
        {
            await _dialogService.ShowWarningAsync("没有可导出的排班表格数据");
            return;
        }

        try
        {
            // 显示加载对话框
            var loadingDialog = _dialogService.ShowLoadingDialog($"正在导出为 {format.ToUpper()} 格式...");

            try
            {
                // 调用导出服务
                var exportData = await _gridExporter.ExportAsync(GridData, format);

                // 关闭加载对话框
                loadingDialog.Hide();

                // 保存文件
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                var hwnd = App.MainWindowHandle;
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                // 设置文件类型
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.SuggestedFileName = $"排班表_{DateTime.Now:yyyyMMdd_HHmmss}";

                switch (format.ToLower())
                {
                    case "excel":
                        savePicker.FileTypeChoices.Add("Excel 文件", new List<string> { ".xlsx" });
                        break;
                    case "csv":
                        savePicker.FileTypeChoices.Add("CSV 文件", new List<string> { ".csv" });
                        break;
                    case "pdf":
                        savePicker.FileTypeChoices.Add("PDF 文件", new List<string> { ".pdf" });
                        break;
                    default:
                        savePicker.FileTypeChoices.Add("文件", new List<string> { $".{format}" });
                        break;
                }

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await Windows.Storage.FileIO.WriteBytesAsync(file, exportData);
                    await _dialogService.ShowSuccessAsync($"排班表已成功导出到：\n{file.Path}");
                }
            }
            catch
            {
                // 确保关闭加载对话框
                loadingDialog.Hide();
                throw;
            }
        }
        catch (NotImplementedException)
        {
            await _dialogService.ShowWarningAsync(
                $"功能开发中\n\n{format.ToUpper()} 格式导出功能正在开发中，敬请期待。");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "导出失败",
                $"导出排班表时发生错误：\n\n{ex.Message}");

            System.Diagnostics.Debug.WriteLine($"Failed to perform export: {ex}");
        }
    }

    #endregion
}


/// <summary>
/// 阶段历史项
/// </summary>
public partial class StageHistoryItem : ObservableObject
{
    /// <summary>
    /// 阶段名称
    /// </summary>
    [ObservableProperty]
    private string _stageName = string.Empty;

    /// <summary>
    /// 阶段状态（Completed, InProgress, Pending, Failed）
    /// </summary>
    [ObservableProperty]
    private string _status = "Pending";

    /// <summary>
    /// 完成时间
    /// </summary>
    [ObservableProperty]
    private DateTime? _completedTime;

    /// <summary>
    /// 状态图标（使用 Segoe MDL2 Assets 字体）
    /// </summary>
    public string StatusIcon => Status switch
    {
        "Completed" => "\uE73E", // CheckMark
        "InProgress" => "\uE768", // Sync
        "Failed" => "\uE711", // ErrorBadge
        _ => "\uE91F" // CircleRing
    };

    /// <summary>
    /// 状态颜色
    /// </summary>
    public string StatusColor => Status switch
    {
        "Completed" => "#107C10", // Green
        "InProgress" => "#0078D4", // Blue
        "Failed" => "#D13438", // Red
        _ => "#8A8A8A" // Gray
    };

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(StatusIcon));
        OnPropertyChanged(nameof(StatusColor));
    }
}
