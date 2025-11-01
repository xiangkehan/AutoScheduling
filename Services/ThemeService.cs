using AutoScheduling3.Services.Interfaces;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

namespace AutoScheduling3.Services;

/// <summary>
/// 主题服务实现 - 管理应用程序主题和个性化设置
/// </summary>
public class ThemeService : IThemeService
{
    private readonly IConfigurationService _configurationService;
    private ElementTheme _currentTheme;
    private bool _isAnimationEnabled;
    private double _animationSpeedMultiplier;
    private bool _isHighContrastEnabled;

    private const string ThemeKey = "AppTheme";
    private const string AnimationEnabledKey = "AnimationEnabled";
    private const string AnimationSpeedKey = "AnimationSpeed";
    private const string HighContrastKey = "HighContrast";

    public ElementTheme CurrentTheme => _currentTheme;
    public bool IsAnimationEnabled => _isAnimationEnabled;
    public double AnimationSpeedMultiplier => _animationSpeedMultiplier;
    public bool IsHighContrastEnabled => _isHighContrastEnabled;

    public event EventHandler<ElementTheme>? ThemeChanged;

    public ThemeService(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
        _currentTheme = ElementTheme.Default;
        _isAnimationEnabled = true;
        _animationSpeedMultiplier = 1.0;
        _isHighContrastEnabled = false;
    }

    public async Task InitializeAsync()
    {
        // 加载保存的主题设置
        var savedTheme = _configurationService.GetValue(ThemeKey, ElementTheme.Default);
        _isAnimationEnabled = _configurationService.GetValue(AnimationEnabledKey, true);
        _animationSpeedMultiplier = _configurationService.GetValue(AnimationSpeedKey, 1.0);
        _isHighContrastEnabled = _configurationService.GetValue(HighContrastKey, false);

        // 应用主题
        await SetThemeAsync(savedTheme);
    }

    public async Task SetThemeAsync(ElementTheme theme)
    {
        if (_currentTheme == theme) return;

        _currentTheme = theme;

        // 应用到主窗口
        if (App.MainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }

        // 保存设置
        await _configurationService.SetValueAsync(ThemeKey, theme);

        // 触发事件
        ThemeChanged?.Invoke(this, theme);
    }

    public async Task ToggleThemeAsync()
    {
        var nextTheme = _currentTheme switch
        {
            ElementTheme.Light => ElementTheme.Dark,
            ElementTheme.Dark => ElementTheme.Default,
            ElementTheme.Default => ElementTheme.Light,
            _ => ElementTheme.Light
        };

        await SetThemeAsync(nextTheme);
    }

    public ElementTheme GetSystemTheme()
    {
        try
        {
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            var color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
            
            // 判断系统是否为深色主题
            var isDark = (color.R + color.G + color.B) < 384; // 128 * 3
            return isDark ? ElementTheme.Dark : ElementTheme.Light;
        }
        catch
        {
            return ElementTheme.Light;
        }
    }

    public async Task SetAnimationEnabledAsync(bool enabled)
    {
        if (_isAnimationEnabled == enabled) return;

        _isAnimationEnabled = enabled;
        await _configurationService.SetValueAsync(AnimationEnabledKey, enabled);
    }

    public async Task SetAnimationSpeedMultiplierAsync(double multiplier)
    {
        if (Math.Abs(_animationSpeedMultiplier - multiplier) < 0.01) return;

        _animationSpeedMultiplier = Math.Max(0.1, Math.Min(3.0, multiplier));
        await _configurationService.SetValueAsync(AnimationSpeedKey, _animationSpeedMultiplier);
    }

    public async Task SetHighContrastEnabledAsync(bool enabled)
    {
        if (_isHighContrastEnabled == enabled) return;

        _isHighContrastEnabled = enabled;
        await _configurationService.SetValueAsync(HighContrastKey, enabled);
    }
}