using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoScheduling3.SchedulingEngine.Config;

namespace AutoScheduling3.Validators;

/// <summary>
/// 带缓存的遗传算法配置验证器
/// 使用配置哈希作为缓存键，避免重复验证相同的配置
/// 性能优化：缓存命中时可提升 90%+ 验证速度
/// </summary>
public class CachedConfigValidator
{
    private readonly GeneticConfigValidator _validator;
    private readonly ConcurrentDictionary<string, ValidationResult> _validationCache;
    private readonly ConcurrentDictionary<string, DateTime> _cacheTimestamps;
    private readonly TimeSpan _cacheExpiration;

    // 统计信息
    private long _totalValidations = 0;
    private long _cacheHits = 0;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="cacheExpirationMinutes">缓存过期时间（分钟），默认10分钟</param>
    public CachedConfigValidator(int cacheExpirationMinutes = 10)
    {
        _validator = new GeneticConfigValidator();
        _validationCache = new ConcurrentDictionary<string, ValidationResult>();
        _cacheTimestamps = new ConcurrentDictionary<string, DateTime>();
        _cacheExpiration = TimeSpan.FromMinutes(cacheExpirationMinutes);
    }

    /// <summary>
    /// 验证遗传算法配置（带缓存）
    /// </summary>
    /// <param name="config">要验证的配置对象</param>
    /// <returns>验证结果</returns>
    public ValidationResult Validate(GeneticSchedulerConfig config)
    {
        if (config == null)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new System.Collections.Generic.List<string> { "配置对象不能为空" }
            };
        }

        System.Threading.Interlocked.Increment(ref _totalValidations);

        // 计算配置哈希
        var configHash = ComputeConfigHash(config);

        // 检查缓存是否存在且未过期
        if (_validationCache.TryGetValue(configHash, out var cachedResult))
        {
            if (_cacheTimestamps.TryGetValue(configHash, out var timestamp))
            {
                if (DateTime.UtcNow - timestamp < _cacheExpiration)
                {
                    // 缓存命中且未过期
                    System.Threading.Interlocked.Increment(ref _cacheHits);
                    System.Diagnostics.Debug.WriteLine($"[CachedConfigValidator] 缓存命中，哈希: {configHash.Substring(0, 8)}...");
                    return cachedResult;
                }
                else
                {
                    // 缓存已过期，移除
                    _validationCache.TryRemove(configHash, out _);
                    _cacheTimestamps.TryRemove(configHash, out _);
                    System.Diagnostics.Debug.WriteLine($"[CachedConfigValidator] 缓存已过期，哈希: {configHash.Substring(0, 8)}...");
                }
            }
        }

        // 缓存未命中或已过期，执行验证
        System.Diagnostics.Debug.WriteLine($"[CachedConfigValidator] 执行验证，哈希: {configHash.Substring(0, 8)}...");
        var result = _validator.Validate(config);

        // 将结果添加到缓存
        _validationCache.TryAdd(configHash, result);
        _cacheTimestamps.TryAdd(configHash, DateTime.UtcNow);

        return result;
    }

    /// <summary>
    /// 快速验证配置是否有效（仅返回布尔值）
    /// </summary>
    /// <param name="config">要验证的配置对象</param>
    /// <returns>配置是否有效</returns>
    public bool IsValid(GeneticSchedulerConfig config)
    {
        return Validate(config).IsValid;
    }

    /// <summary>
    /// 计算配置的哈希值
    /// 使用 SHA256 算法确保哈希的唯一性
    /// </summary>
    /// <param name="config">配置对象</param>
    /// <returns>配置的哈希字符串</returns>
    private string ComputeConfigHash(GeneticSchedulerConfig config)
    {
        try
        {
            // 序列化配置为 JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(config, options);

            // 计算 SHA256 哈希
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            // 如果序列化失败，使用配置的字符串表示作为后备方案
            System.Diagnostics.Debug.WriteLine($"[CachedConfigValidator] 计算哈希失败: {ex.Message}");
            var fallbackString = $"{config.PopulationSize}_{config.MaxGenerations}_{config.CrossoverRate}_{config.MutationRate}_{config.EliteCount}";
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fallbackString));
            return Convert.ToBase64String(hashBytes);
        }
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void ClearCache()
    {
        _validationCache.Clear();
        _cacheTimestamps.Clear();
        System.Diagnostics.Debug.WriteLine("[CachedConfigValidator] 缓存已清除");
    }

    /// <summary>
    /// 清除过期的缓存条目
    /// </summary>
    public void CleanupExpiredCache()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = new System.Collections.Generic.List<string>();

        foreach (var kvp in _cacheTimestamps)
        {
            if (now - kvp.Value >= _cacheExpiration)
            {
                expiredKeys.Add(kvp.Key);
            }
        }

        foreach (var key in expiredKeys)
        {
            _validationCache.TryRemove(key, out _);
            _cacheTimestamps.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[CachedConfigValidator] 清理了 {expiredKeys.Count} 个过期缓存条目");
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>缓存统计信息字符串</returns>
    public string GetCacheStatistics()
    {
        var hitRate = _totalValidations > 0 ? (_cacheHits * 100.0 / _totalValidations) : 0;
        return $"总验证次数: {_totalValidations}, 缓存命中: {_cacheHits}, 命中率: {hitRate:F2}%, 缓存大小: {_validationCache.Count}";
    }

    /// <summary>
    /// 获取缓存命中率
    /// </summary>
    /// <returns>缓存命中率（0.0-1.0）</returns>
    public double GetCacheHitRate()
    {
        return _totalValidations > 0 ? (_cacheHits / (double)_totalValidations) : 0;
    }

    /// <summary>
    /// 获取缓存大小
    /// </summary>
    /// <returns>缓存中的条目数量</returns>
    public int GetCacheSize()
    {
        return _validationCache.Count;
    }
}
