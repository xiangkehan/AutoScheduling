using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 主题服务接口 - 管理应用程序主题和个性化设置
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// 当前主题
    /// </summary>
    ElementTheme CurrentTheme { get; }

    /// <summary>
    /// 主题变更事件
    /// </summary>
    event EventHandler<ElementTheme> ThemeChanged;

    /// <summary>
    /// 初始化主题服务
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 设置主题
    /// </summary>
    /// <param name="theme">要设置的主题</param>
    Task SetThemeAsync(ElementTheme theme);

    /// <summary>
    /// 切换到下一个主题
    /// </summary>
    Task ToggleThemeAsync();

    /// <summary>
    /// 获取系统主题
    /// </summary>
    ElementTheme GetSystemTheme();

    /// <summary>
    /// 是否启用动画
    /// </summary>
    bool IsAnimationEnabled { get; }

    /// <summary>
    /// 设置动画启用状态
    /// </summary>
    Task SetAnimationEnabledAsync(bool enabled);

    /// <summary>
    /// 动画持续时间倍数
    /// </summary>
    double AnimationSpeedMultiplier { get; }

    /// <summary>
    /// 设置动画速度倍数
    /// </summary>
    Task SetAnimationSpeedMultiplierAsync(double multiplier);

    /// <summary>
    /// 是否启用高对比度模式
    /// </summary>
    bool IsHighContrastEnabled { get; }

    /// <summary>
    /// 设置高对比度模式
    /// </summary>
    Task SetHighContrastEnabledAsync(bool enabled);
}