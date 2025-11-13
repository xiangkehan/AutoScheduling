using System;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.Data.Logging;

namespace AutoScheduling3.Services.ImportExport.Locking;

/// <summary>
/// 管理导入操作的并发锁，防止多个导入同时执行
/// </summary>
public class ImportLockManager : IDisposable
{
    private static readonly SemaphoreSlim _importLock = new SemaphoreSlim(1, 1);
    private static readonly TimeSpan _lockTimeout = TimeSpan.FromMinutes(30);
    private readonly ILogger? _logger;
    private bool _hasLock = false;
    private DateTime? _lockAcquiredTime = null;
    private readonly object _stateLock = new object();

    /// <summary>
    /// 初始化 ImportLockManager
    /// </summary>
    /// <param name="logger">日志记录器（可选）</param>
    public ImportLockManager(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取导入锁是否已被持有
    /// </summary>
    public bool IsLocked
    {
        get
        {
            lock (_stateLock)
            {
                return _hasLock;
            }
        }
    }

    /// <summary>
    /// 尝试获取导入锁
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果成功获取锁返回 true，否则返回 false</returns>
    public async Task<bool> TryAcquireLockAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.Log("尝试获取导入锁...");

            // 尝试在超时时间内获取锁
            bool acquired = await _importLock.WaitAsync(_lockTimeout, cancellationToken);

            if (acquired)
            {
                lock (_stateLock)
                {
                    _hasLock = true;
                    _lockAcquiredTime = DateTime.UtcNow;
                }

                _logger?.Log($"成功获取导入锁 (时间: {_lockAcquiredTime:yyyy-MM-dd HH:mm:ss})");
                return true;
            }
            else
            {
                _logger?.LogWarning("获取导入锁超时 - 另一个导入操作可能正在进行中");
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("获取导入锁操作被取消");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"获取导入锁时发生错误: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 释放导入锁
    /// </summary>
    public void ReleaseLock()
    {
        lock (_stateLock)
        {
            if (!_hasLock)
            {
                _logger?.LogWarning("尝试释放未持有的锁");
                return;
            }

            try
            {
                var lockDuration = _lockAcquiredTime.HasValue 
                    ? DateTime.UtcNow - _lockAcquiredTime.Value 
                    : TimeSpan.Zero;

                _importLock.Release();
                _hasLock = false;

                _logger?.Log($"成功释放导入锁 (持有时长: {lockDuration.TotalSeconds:F2} 秒)");

                // 检查是否接近超时
                if (lockDuration > TimeSpan.FromMinutes(25))
                {
                    _logger?.LogWarning($"导入操作持续时间较长 ({lockDuration.TotalMinutes:F1} 分钟)，接近超时限制");
                }

                _lockAcquiredTime = null;
            }
            catch (SemaphoreFullException)
            {
                _logger?.LogError("释放锁失败 - 信号量已满（可能存在重复释放）");
                _hasLock = false;
                _lockAcquiredTime = null;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"释放导入锁时发生错误: {ex.Message}");
                _hasLock = false;
                _lockAcquiredTime = null;
            }
        }
    }

    /// <summary>
    /// 检查锁是否超时
    /// </summary>
    /// <returns>如果锁已超时返回 true</returns>
    public bool IsLockTimedOut()
    {
        lock (_stateLock)
        {
            if (!_hasLock || !_lockAcquiredTime.HasValue)
            {
                return false;
            }

            var lockDuration = DateTime.UtcNow - _lockAcquiredTime.Value;
            return lockDuration > _lockTimeout;
        }
    }

    /// <summary>
    /// 获取锁持有时长
    /// </summary>
    /// <returns>锁持有时长，如果未持有锁则返回 null</returns>
    public TimeSpan? GetLockDuration()
    {
        lock (_stateLock)
        {
            if (!_hasLock || !_lockAcquiredTime.HasValue)
            {
                return null;
            }

            return DateTime.UtcNow - _lockAcquiredTime.Value;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_hasLock)
        {
            _logger?.LogWarning("ImportLockManager 被释放时仍持有锁，自动释放锁");
            ReleaseLock();
        }
    }
}
