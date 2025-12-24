using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Services;

/// <summary>
/// 模板配置缓存服务
/// 性能优化：使用 ConcurrentDictionary 存储缓存，10分钟过期
/// </summary>
public class TemplateConfigCache
{
    private readonly ConcurrentDictionary<int, CacheEntry> _cache = new();
    private readonly TimeSpan _expirationTime = TimeSpan.FromMinutes(10);
    private readonly Timer _cleanupTimer;

    public TemplateConfigCache()
    {
        // 每5分钟清理一次过期缓存
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// 获取缓存的模板
    /// </summary>
    public bool TryGet(int templateId, out SchedulingTemplateDto? template)
    {
        if (_cache.TryGetValue(templateId, out var entry))
        {
            if (DateTime.UtcNow - entry.Timestamp < _expirationTime)
            {
                template = entry.Template;
                return true;
            }
            else
            {
                // 过期，移除
                _cache.TryRemove(templateId, out _);
            }
        }

        template = null;
        return false;
    }

    /// <summary>
    /// 添加或更新缓存
    /// </summary>
    public void Set(int templateId, SchedulingTemplateDto template)
    {
        var entry = new CacheEntry
        {
            Template = template,
            Timestamp = DateTime.UtcNow
        };

        _cache.AddOrUpdate(templateId, entry, (key, oldValue) => entry);
    }

    /// <summary>
    /// 移除缓存
    /// </summary>
    public void Remove(int templateId)
    {
        _cache.TryRemove(templateId, out _);
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// 清理过期的缓存条目
    /// </summary>
    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = new System.Collections.Generic.List<int>();

        foreach (var kvp in _cache)
        {
            if (now - kvp.Value.Timestamp >= _expirationTime)
            {
                expiredKeys.Add(kvp.Key);
            }
        }

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine($"清理了 {expiredKeys.Count} 个过期的模板缓存条目");
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            TotalEntries = _cache.Count,
            ExpirationTime = _expirationTime
        };
    }

    private class CacheEntry
    {
        public SchedulingTemplateDto Template { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }
}

/// <summary>
/// 缓存统计信息
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public TimeSpan ExpirationTime { get; set; }
}
