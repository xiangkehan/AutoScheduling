using System.Threading.Tasks;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 配置服务接口 - 管理应用程序配置
/// </summary>
public interface IConfigurationService : IApplicationService
{
    /// <summary>
    /// 获取配置值
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>配置值</returns>
    T GetValue<T>(string key, T defaultValue = default!);

    /// <summary>
    /// 设置配置值
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="value">配置值</param>
    Task SetValueAsync<T>(string key, T value);

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>是否存在</returns>
    bool ContainsKey(string key);

    /// <summary>
    /// 移除配置项
    /// </summary>
    /// <param name="key">配置键</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// 清空所有配置
    /// </summary>
    Task ClearAsync();
}