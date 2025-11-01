using AutoScheduling3.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoScheduling3.Services;

/// <summary>
/// 配置服务实现 - 基于JSON文件的配置管理
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly string _configFilePath;
    private Dictionary<string, object> _configuration;
    private readonly object _lock = new();

    public ConfigurationService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "AutoScheduling3");
        Directory.CreateDirectory(appFolder);
        _configFilePath = Path.Combine(appFolder, "config.json");
        _configuration = new Dictionary<string, object>();
    }

    public async Task InitializeAsync()
    {
        await LoadConfigurationAsync();
    }

    public async Task CleanupAsync()
    {
        await SaveConfigurationAsync();
    }

    public async Task LoadConfigurationAsync()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
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
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load configuration: {ex.Message}");
            // 如果加载失败，使用空配置
            lock (_lock)
            {
                _configuration.Clear();
            }
        }
    }

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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save configuration: {ex.Message}");
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