using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 导航服务 - 管理页面导航
/// </summary>
public class NavigationService
{
    private Frame? _frame;
    private readonly Dictionary<string, Type> _pages = new();

    /// <summary>
    /// 初始化导航服务
    /// </summary>
    public void Initialize(Frame frame)
    {
        _frame = frame ?? throw new ArgumentNullException(nameof(frame));
    }

    /// <summary>
    /// 注册页面类型
    /// </summary>
    public void RegisterPage(string key, Type pageType)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("页面键不能为空", nameof(key));

        if (pageType == null)
            throw new ArgumentNullException(nameof(pageType));

        if (!typeof(Page).IsAssignableFrom(pageType))
            throw new ArgumentException("页面类型必须继承自 Page", nameof(pageType));

        _pages[key] = pageType;
    }

    /// <summary>
    /// 导航到指定页面
    /// </summary>
    public bool NavigateTo(string pageKey, object? parameter = null)
    {
        if (_frame == null)
            throw new InvalidOperationException("导航服务未初始化，请先调用 Initialize 方法");

        if (!_pages.TryGetValue(pageKey, out Type? pageType))
            throw new ArgumentException($"未找到键为 '{pageKey}' 的页面", nameof(pageKey));

        return _frame.Navigate(pageType, parameter);
    }

    /// <summary>
    /// 导航到指定页面类型
    /// </summary>
    public bool Navigate(Type pageType, object? parameter = null)
    {
        if (_frame == null)
            throw new InvalidOperationException("导航服务未初始化，请先调用 Initialize 方法");

        if (pageType == null)
            throw new ArgumentNullException(nameof(pageType));

        return _frame.Navigate(pageType, parameter);
    }

    /// <summary>
    /// 后退
    /// </summary>
    public void GoBack()
    {
        if (_frame == null)
            throw new InvalidOperationException("导航服务未初始化");

        if (_frame.CanGoBack)
        {
            _frame.GoBack();
        }
    }

    /// <summary>
    /// 前进
    /// </summary>
    public void GoForward()
    {
        if (_frame == null)
            throw new InvalidOperationException("导航服务未初始化");

        if (_frame.CanGoForward)
        {
            _frame.GoForward();
        }
    }

    /// <summary>
    /// 能否后退
    /// </summary>
    public bool CanGoBack => _frame?.CanGoBack ?? false;

    /// <summary>
    /// 能否前进
    /// </summary>
    public bool CanGoForward => _frame?.CanGoForward ?? false;

    /// <summary>
    /// 清空导航历史
    /// </summary>
    public void ClearHistory()
    {
        if (_frame == null)
            throw new InvalidOperationException("导航服务未初始化");

        _frame.BackStack.Clear();
        _frame.ForwardStack.Clear();
    }
}
