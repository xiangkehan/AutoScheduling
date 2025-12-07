using System;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;

namespace AutoScheduling3.Services;

/// <summary>
/// 自动保存节流器 - 控制自动保存的频率和条件
/// 
/// 性能优化策略（方案4 - 混合策略）：
/// 1. 最小进度变化阈值：默认5%，避免微小变化触发保存
/// 2. 最大时间间隔：默认5秒，确保长时间任务定期保存
/// 3. 关键进度点：25%, 50%, 75% 强制保存
/// 4. 异步保存：不阻塞主线程
/// 
/// 预期性能影响：< 2% 性能开销
/// </summary>
public class ThrottledAutoSaver
{
    private readonly ISchedulingService _schedulingService;
    private readonly double _minProgressChangeThreshold;
    private readonly int _maxSaveIntervalSeconds;
    private readonly double[] _keyProgressPoints = { 25.0, 50.0, 75.0 };

    private double _lastSavedProgress = 0.0;
    private DateTime _lastSaveTime = DateTime.MinValue;
    private bool _isSaving = false;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    /// <summary>
    /// 上次保存时间（用于UI显示）
    /// </summary>
    public DateTime? LastSaveTime { get; private set; }

    /// <summary>
    /// 是否正在保存
    /// </summary>
    public bool IsSaving => _isSaving;

    /// <summary>
    /// 初始化自动保存节流器
    /// </summary>
    /// <param name="schedulingService">排班服务</param>
    /// <param name="minProgressChangeThreshold">最小进度变化阈值（百分比），默认5%</param>
    /// <param name="maxSaveIntervalSeconds">最大保存时间间隔（秒），默认5秒</param>
    public ThrottledAutoSaver(
        ISchedulingService schedulingService,
        double minProgressChangeThreshold = 5.0,
        int maxSaveIntervalSeconds = 5)
    {
        _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
        _minProgressChangeThreshold = minProgressChangeThreshold;
        _maxSaveIntervalSeconds = maxSaveIntervalSeconds;
    }

    /// <summary>
    /// 尝试自动保存（如果满足条件）
    /// </summary>
    /// <param name="scheduleDto">当前排班DTO</param>
    /// <param name="progressReport">当前进度报告</param>
    /// <returns>是否执行了保存</returns>
    public async Task<bool> TryAutoSaveAsync(ScheduleDto scheduleDto, SchedulingProgressReport progressReport)
    {
        if (scheduleDto == null || progressReport == null)
        {
            return false;
        }

        // 检查是否正在保存
        if (_isSaving)
        {
            return false;
        }

        var currentProgress = Math.Round(progressReport.ProgressPercentage, 1); // 保留1位小数
        var timeSinceLastSave = _lastSaveTime == DateTime.MinValue ? double.MaxValue : (DateTime.Now - _lastSaveTime).TotalSeconds;

        // 检查是否应该保存
        var (shouldSave, reason) = ShouldSave(currentProgress, timeSinceLastSave);
        if (!shouldSave)
        {
            return false;
        }

        // 使用信号量确保同一时间只有一个保存操作
        if (!await _saveLock.WaitAsync(0))
        {
            return false;
        }

        try
        {
            _isSaving = true;

            // 异步保存，不阻塞主线程
            await Task.Run(async () =>
            {
                try
                {
                    await _schedulingService.SaveProgressAsDraftAsync(scheduleDto, progressReport);
                    
                    LastSaveTime = DateTime.UtcNow;
                    _lastSaveTime = DateTime.Now;
                    _lastSavedProgress = currentProgress;
                }
                catch (Exception)
                {
                    // 不抛出异常，避免影响主流程
                }
            });

            return true;
        }
        finally
        {
            _isSaving = false;
            _saveLock.Release();
        }
    }

    /// <summary>
    /// 判断是否应该保存（方案4 - 混合策略）
    /// </summary>
    /// <param name="currentProgress">当前进度百分比</param>
    /// <param name="timeSinceLastSave">距上次保存的秒数</param>
    /// <returns>(是否应该保存, 保存原因)</returns>
    private (bool shouldSave, string reason) ShouldSave(double currentProgress, double timeSinceLastSave)
    {
        // 策略1：检查是否达到关键进度点
        if (IsKeyProgressPoint(currentProgress))
        {
            return (true, $"关键进度点 ({currentProgress:F1}%)");
        }

        // 策略2：检查进度变化是否达到阈值
        var progressChange = Math.Abs(currentProgress - _lastSavedProgress);
        if (progressChange >= _minProgressChangeThreshold)
        {
            return (true, $"进度变化 ({progressChange:F1}% >= {_minProgressChangeThreshold}%)");
        }

        // 策略3：检查时间间隔是否超过最大值（确保长时间任务定期保存）
        if (timeSinceLastSave >= _maxSaveIntervalSeconds)
        {
            return (true, $"时间间隔 ({timeSinceLastSave:F1}秒 >= {_maxSaveIntervalSeconds}秒)");
        }

        return (false, "不满足任何保存条件");
    }

    /// <summary>
    /// 判断是否为关键进度点
    /// </summary>
    private bool IsKeyProgressPoint(double currentProgress)
    {
        foreach (var keyPoint in _keyProgressPoints)
        {
            // 判断是否跨过关键点（上次保存进度 < 关键点 <= 当前进度）
            if (_lastSavedProgress < keyPoint && currentProgress >= keyPoint)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 重置状态（用于新的排班任务）
    /// </summary>
    public void Reset()
    {
        LastSaveTime = null;
        _lastSaveTime = DateTime.MinValue;
        _lastSavedProgress = 0.0;
        _isSaving = false;
    }
}
