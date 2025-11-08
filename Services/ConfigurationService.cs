using AutoScheduling3.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace AutoScheduling3.Services;

/// <summary>
/// 配置服务实现 - 基于JSON文件的配置管理
/// 
/// 使用 ApplicationData.Current.LocalFolder 符合 WinUI3 最佳实践。
/// 
/// 为什么使用 ApplicationData.Current.LocalFolder：
/// 1. WinUI3 标准：这是 WinUI3 应用程序推荐的配置存储方式
/// 2. 权限管理：应用程序自动拥有读写权限，无需额外配置
/// 3. 数据隔离：配置文件与其他应用程序隔离，提高安全性
/// 4. 系统集成：Windows 系统可以正确管理应用配置的备份和清理
/// 
/// 配置文件位置：
/// - 路径：{LocalFolder}\Settings\config.json
/// - Settings 子文件夹用于组织配置文件，符合 Windows 应用规范
/// - 便于用户识别和管理配置文件
/// 
/// 需求: 2.1, 2.2, 2.3, 8.2, 8.3
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly string _configFilePath;
    private Dictionary<string, object> _configuration;
    private readonly object _lock = new();

    /// <summary>
    /// 初始化配置服务
    /// 
    /// 初始化流程：
    /// 1. 获取 ApplicationData.Current.LocalFolder 路径
    /// 2. 创建 Settings 子文件夹（如果不存在）
    /// 3. 设置配置文件路径为 {LocalFolder}\Settings\config.json
    /// 
    /// 日志记录：
    /// - 记录 Settings 文件夹创建操作
    /// - 输出配置文件的完整路径
    /// 
    /// 错误处理：
    /// - UnauthorizedAccessException：权限不足，无法访问应用程序数据文件夹
    /// - IOException：文件系统访问错误
    /// 
    /// 需求: 2.1, 2.2, 2.3, 5.1, 5.2, 8.2, 8.3
    /// </summary>
    /// <exception cref="InvalidOperationException">无法访问或创建配置文件夹时抛出</exception>
    public ConfigurationService()
    {
        try
        {
            // 使用 ApplicationData.Current.LocalFolder 符合 WinUI3 最佳实践
            var localFolder = ApplicationData.Current.LocalFolder.Path;
            var settingsFolder = Path.Combine(localFolder, "Settings");
            
            // 确保 Settings 文件夹存在
            if (!Directory.Exists(settingsFolder))
            {
                Directory.CreateDirectory(settingsFolder);
                System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Created Settings folder at: {settingsFolder}");
            }
            
            _configFilePath = Path.Combine(settingsFolder, "config.json");
            _configuration = new Dictionary<string, object>();
            
            // 记录配置文件路径，便于诊断
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Initialized with path: {_configFilePath}");
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] 无法访问应用程序数据文件夹: {ex.Message}");
            throw new InvalidOperationException("权限不足，无法访问应用程序数据文件夹", ex);
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] 访问应用程序数据文件夹时发生IO错误: {ex.Message}");
            throw new InvalidOperationException("访问应用程序数据文件夹时发生错误", ex);
        }
    }

    public async Task InitializeAsync()
    {
        await LoadConfigurationAsync();
        
        // 如果配置文件不存在，创建一个空的配置文件
        if (!File.Exists(_configFilePath))
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Configuration file does not exist, creating empty configuration file");
            await SaveConfigurationAsync();
        }
    }

    public async Task CleanupAsync()
    {
        await SaveConfigurationAsync();
    }

    /// <summary>
    /// 从文件加载配置
    /// 
    /// 加载流程：
    /// 1. 检查配置文件是否存在
    /// 2. 读取 JSON 文件内容
    /// 3. 反序列化为配置字典
    /// 4. 线程安全地更新内存中的配置
    /// 
    /// 日志记录：
    /// - 记录配置文件路径
    /// - 输出加载的配置项数量
    /// - 记录文件不存在的情况
    /// 
    /// 错误处理：
    /// - 如果文件不存在，使用空配置（不抛出异常）
    /// - 如果加载失败，清空配置并使用空配置
    /// - 捕获权限、IO 和反序列化错误
    /// 
    /// 需求: 2.2, 5.1, 5.2, 8.2, 8.3
    /// </summary>
    public async Task LoadConfigurationAsync()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Loading configuration from: {_configFilePath}");
                var json = await File.ReadAllTextAsync(_configFilePath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                
                lock (_lock)
                {
                    _configuration.Clear();
                    if (config != null)
                    {
                        foreach (var kvp in config)
                        {
                            _configuration[kvp.Key] = kvp.Value;
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Configuration loaded successfully with {_configuration.Count} entries");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Configuration file does not exist at: {_configFilePath}");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] 无法访问配置文件 (权限不足): {ex.Message}");
            // 如果加载失败，使用空配置
            lock (_lock)
            {
                _configuration.Clear();
            }
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] 读取配置文件时发生IO错误: {ex.Message}");
            // 如果加载失败，使用空配置
            lock (_lock)
            {
                _configuration.Clear();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Failed to load configuration: {ex.Message}");
            // 如果加载失败，使用空配置
            lock (_lock)
            {
                _configuration.Clear();
            }
        }
    }

    /// <summary>
    /// 保存配置到文件
    /// 
    /// 保存流程：
    /// 1. 线程安全地复制当前配置
    /// 2. 序列化为格式化的 JSON（便于人工阅读和编辑）
    /// 3. 写入配置文件
    /// 
    /// 日志记录：
    /// - 记录保存成功的消息和文件路径
    /// - 记录所有错误情况
    /// 
    /// 错误处理：
    /// - UnauthorizedAccessException：权限不足，抛出 InvalidOperationException
    /// - IOException：文件系统错误，抛出 InvalidOperationException
    /// - 其他异常：直接抛出
    /// 
    /// 需求: 2.2, 5.1, 5.2, 8.2, 8.3
    /// </summary>
    /// <exception cref="InvalidOperationException">无法保存配置文件时抛出</exception>
    public async Task SaveConfigurationAsync()
    {
        try
        {
            Dictionary<string, object> configCopy;
            lock (_lock)
            {
                configCopy = new Dictionary<string, object>(_configuration);
            }

            var json = JsonSerializer.Serialize(configCopy, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            await File.WriteAllTextAsync(_configFilePath, json);
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Configuration saved successfully to: {_configFilePath}");
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] 无法保存配置文件 (权限不足): {ex.Message}");
            throw new InvalidOperationException("权限不足，无法保存配置文件", ex);
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] 保存配置文件时发生IO错误: {ex.Message}");
            throw new InvalidOperationException("保存配置文件时发生错误", ex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Failed to save configuration: {ex.Message}");
            throw;
        }
    }

    public T GetValue<T>(string key, T defaultValue = default!)
    {
        lock (_lock)
        {
            if (!_configuration.TryGetValue(key, out var value))
                return defaultValue;

            try
            {
                if (value is JsonElement jsonElement)
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;
                }
                
                if (value is T directValue)
                    return directValue;

                // 尝试转换类型
                return (T)Convert.ChangeType(value, typeof(T)) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
    }

    public async Task SetValueAsync<T>(string key, T value)
    {
        lock (_lock)
        {
            _configuration[key] = value!;
        }
        
        // 异步保存配置
        await SaveConfigurationAsync();
    }

    public bool ContainsKey(string key)
    {
        lock (_lock)
        {
            return _configuration.ContainsKey(key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        lock (_lock)
        {
            _configuration.Remove(key);
        }
        
        await SaveConfigurationAsync();
    }

    public async Task ClearAsync()
    {
        lock (_lock)
        {
            _configuration.Clear();
        }
        
        await SaveConfigurationAsync();
    }
}