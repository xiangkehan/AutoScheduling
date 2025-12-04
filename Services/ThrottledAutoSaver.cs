using System;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;

namespace AutoScheduling3.Services;

/// <summary>
/// 自动保存节流器 - 控制自动保存的频率和条件
/// 
/// 性能优化策略：
/// 1. 最小保存间隔：默认2分钟，避免频繁保存
/// 2. 最小进度变化阈值：默认5%，避免微小变化触发保存
/// 3. 关键进度点：25%, 50%, 75% 自动保存
/// 4. 异步保存：不阻塞主线程
/// 
/// 预期性能影响：< 2% 性能开销
/// </summary>
public class ThrottledAutoSaver
{
    private readonly ISchedulingService _schedulingService;
    private readonly TimeSpan _minSaveInterval;
    private readonly double _minProgressChangeThreshold;
    private readonly double[] _keyProgressPoints = { 25.0, 50.0, 75.0 };

    private DateTime _lastSaveTime = DateTime.MinValue;
    private double _lastSavedProgress = 0.0;
    private bool _isSaving = false;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    /// <summary>
    /// 上次保存时间（用于UI显示）
    /// </summary>
    public DateTime LastSaveTime => _lastSaveTime;

    /// <summary>
    /// 是否正在保存
    /// </summary>
    public bool IsSaving => _isSaving;

    /// <summary>
    /// 初始化自动保存节流器
    /// </summary>
    /// <param name="schedulingService">排班服务</param>
    /// <param name="minSaveIntervalMinutes">最小保存间隔（分钟），默认2分钟</param>
    /// <param name="minProgressChangeThreshold">最小进度变化阈值（百分比），默认5%</param>
    public ThrottledAutoSaver(
        ISchedulingService schedulingService,
        int minSaveIntervalMinutes = 2,
        double minProgressChangeThreshold = 5.0)
    {
        _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
        _minSaveInterval = TimeSpan.FromMinutes(minSaveIntervalMinutes);
        _minProgressChangeThreshold = minProgressChangeThreshold;
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
            //System.Diagnostics.Debug.WriteLine("[ThrottledAutoSaver] 已有保存操作正在进行，跳过");
            return false;
        }

        var currentProgress = progressReport.ProgressPercentage;
        var now = DateTime.UtcNow;

        // 检查是否应该保存
        if (!ShouldSave(currentProgress, now))
        {
            return false;
        }

        // 使用信号量确保同一时间只有一个保存操作
        if (!await _saveLock.WaitAsync(0))
        {
            System.Diagnostics.Debug.WriteLine("[ThrottledAutoSaver] 无法获取保存锁，跳过");
            return false;
        }

        try
        {
            _isSaving = true;
            System.Diagnostics.Debug.WriteLine($"[ThrottledAutoSaver] 开始自动保存，进度: {currentProgress:F1}%");

            // 异步保存，不阻塞主线程
            await Task.Run(async () =>
            {
                try
                {
                    await _schedulingService.SaveProgressAsDraftAsync(scheduleDto, progressReport);
                    
                    _lastSaveTime = now;
                    _lastSavedProgress = currentProgress;
                    
                    System.Diagnostics.Debug.WriteLine($"[ThrottledAutoSaver] 自动保存成功，时间: {now:HH:mm:ss}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ThrottledAutoSaver] 自动保存失败: {ex.Message}");
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
    /// 判断是否应该保存
    /// </summary>
    private bool ShouldSave(double currentProgress, DateTime now)
    {
        // 检查是否达到关键进度点
        if (IsKeyProgressPoint(currentProgress))
        {
            System.Diagnostics.Debug.WriteLine($"[ThrottledAutoSaver] 达到关键进度点: {currentProgress:F1}%");
            return true;
        }

        // 检查时间间隔
        var timeSinceLastSave = now - _lastSaveTime;
        if (timeSinceLastSave < _minSaveInterval)
        {
            return false;
        }

        // 检查进度变化
        var progressChange = Math.Abs(currentProgress - _lastSavedProgress);
        if (progressChange < _minProgressChangeThreshold)
        {
            return false;
        }

        System.Diagnostics.Debug.WriteLine($"[ThrottledAutoSaver] 满足保存条件 - 时间间隔: {timeSinceLastSave.TotalMinutes:F1}分钟, 进度变化: {progressChange:F1}%");
        return true;
    }

    /// <summary>
    /// 判断是否为关键进度点
    /// </summary>
    private bool IsKeyProgressPoint(double currentProgress)
    {
        foreach (var keyPoint in _keyProgressPoints)
        {
            // 如果当前进度刚好跨过关键点（在关键点±2%范围内，且上次保存进度小于关键点）
            if (currentProgress >= keyPoint - 2.0 && currentProgress <= keyPoint + 2.0 && _lastSavedProgress < keyPoint)
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
        _lastSaveTime = DateTime.MinValue;
        _lastSavedProgress = 0.0;
        _isSaving = false;
        System.Diagnostics.Debug.WriteLine("[ThrottledAutoSaver] 状态已重置");
    }
}
