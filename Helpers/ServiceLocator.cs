using Microsoft.Extensions.DependencyInjection;
using System;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 服务定位器 - 提供全局访问依赖注入容器的功能
/// </summary>
public static class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// 初始化服务提供者
    /// </summary>
    /// <param name="serviceProvider">服务提供者实例</param>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// 获取指定类型的服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    /// <exception cref="InvalidOperationException">当服务提供者未初始化或服务未注册时抛出</exception>
    public static T GetService<T>() where T : class
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("ServiceLocator has not been initialized. Call Initialize() first.");

        var service = _serviceProvider.GetService<T>();
        if (service == null)
            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");

        return service;
    }

    /// <summary>
    /// 获取必需的服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    /// <exception cref="InvalidOperationException">当服务提供者未初始化时抛出</exception>
    public static T GetRequiredService<T>() where T : class
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("ServiceLocator has not been initialized. Call Initialize() first.");

        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// 尝试获取指定类型的服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例，如果未找到则返回null</returns>
    public static T? TryGetService<T>() where T : class
    {
        if (_serviceProvider == null)
            return null;

        return _serviceProvider.GetService<T>();
    }

    /// <summary>
    /// 检查服务提供者是否已初始化
    /// </summary>
    public static bool IsInitialized => _serviceProvider != null;

    /// <summary>
    /// 清理服务定位器
    /// </summary>
    internal static void Clear()
    {
        _serviceProvider = null;
    }
}