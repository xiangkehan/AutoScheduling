using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;
using AutoScheduling3.Services.Interfaces;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 动画辅助类 - 提供统一的动画效果
/// </summary>
public static class AnimationHelper
{
    private static IThemeService? _themeService;

    /// <summary>
    /// 初始化动画辅助类
    /// </summary>
    public static void Initialize(IThemeService themeService)
    {
        _themeService = themeService;
    }

    /// <summary>
    /// 获取调整后的动画持续时间
    /// </summary>
    private static TimeSpan GetAdjustedDuration(TimeSpan baseDuration)
    {
        if (_themeService?.IsAnimationEnabled != true)
            return TimeSpan.Zero;

        var multiplier = _themeService.AnimationSpeedMultiplier;
        return TimeSpan.FromMilliseconds(baseDuration.TotalMilliseconds / multiplier);
    }

    /// <summary>
    /// 淡入动画
    /// </summary>
    public static Storyboard CreateFadeInAnimation(FrameworkElement element, TimeSpan? duration = null)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = GetAdjustedDuration(duration ?? TimeSpan.FromMilliseconds(300)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Children.Add(animation);

        return storyboard;
    }

    /// <summary>
    /// 淡出动画
    /// </summary>
    public static Storyboard CreateFadeOutAnimation(FrameworkElement element, TimeSpan? duration = null)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = GetAdjustedDuration(duration ?? TimeSpan.FromMilliseconds(300)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Children.Add(animation);

        return storyboard;
    }

    /// <summary>
    /// 滑入动画（从左侧）
    /// </summary>
    public static Storyboard CreateSlideInFromLeftAnimation(FrameworkElement element, TimeSpan? duration = null)
    {
        var storyboard = new Storyboard();
        
        // 透明度动画
        var opacityAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = GetAdjustedDuration(duration ?? TimeSpan.FromMilliseconds(400))
        };

        // 位移动画
        var translateAnimation = new DoubleAnimation
        {
            From = -50,
            To = 0,
            Duration = GetAdjustedDuration(duration ?? TimeSpan.FromMilliseconds(400)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(opacityAnimation, element);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
        
        Storyboard.SetTarget(translateAnimation, element);
        Storyboard.SetTargetProperty(translateAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");

        storyboard.Children.Add(opacityAnimation);
        storyboard.Children.Add(translateAnimation);

        return storyboard;
    }

    /// <summary>
    /// 缩放动画
    /// </summary>
    public static Storyboard CreateScaleAnimation(FrameworkElement element, double fromScale, double toScale, TimeSpan? duration = null)
    {
        var storyboard = new Storyboard();
        
        var scaleXAnimation = new DoubleAnimation
        {
            From = fromScale,
            To = toScale,
            Duration = GetAdjustedDuration(duration ?? TimeSpan.FromMilliseconds(300)),
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
        };

        var scaleYAnimation = new DoubleAnimation
        {
            From = fromScale,
            To = toScale,
            Duration = GetAdjustedDuration(duration ?? TimeSpan.FromMilliseconds(300)),
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
        };

        Storyboard.SetTarget(scaleXAnimation, element);
        Storyboard.SetTargetProperty(scaleXAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");
        
        Storyboard.SetTarget(scaleYAnimation, element);
        Storyboard.SetTargetProperty(scaleYAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");

        storyboard.Children.Add(scaleXAnimation);
        storyboard.Children.Add(scaleYAnimation);

        return storyboard;
    }

    /// <summary>
    /// 弹跳进入动画
    /// </summary>
    public static Storyboard CreateBounceInAnimation(FrameworkElement element, TimeSpan? duration = null)
    {
        var storyboard = new Storyboard();
        
        // 透明度动画
        var opacityAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = GetAdjustedDuration(duration ?? TimeSpan.FromMilliseconds(600))
        };

        // 缩放动画
        var scaleAnimation = new DoubleAnimation
        {
            From = 0.3,
            To = 1,
            Duration = GetAdjustedDuration(duration ?? TimeSpan.FromMilliseconds(600)),
            EasingFunction = new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 2, Bounciness = 2 }
        };

        Storyboard.SetTarget(opacityAnimation, element);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
        
        Storyboard.SetTarget(scaleAnimation, element);
        Storyboard.SetTargetProperty(scaleAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");
        
        var scaleYAnimation = new DoubleAnimation
        {
            From = 0.3,
            To = 1,
            Duration = GetAdjustedDuration(duration ?? TimeSpan.FromMilliseconds(600)),
            EasingFunction = new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 2, Bounciness = 2 }
        };
        
        Storyboard.SetTarget(scaleYAnimation, element);
        Storyboard.SetTargetProperty(scaleYAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");

        storyboard.Children.Add(opacityAnimation);
        storyboard.Children.Add(scaleAnimation);
        storyboard.Children.Add(scaleYAnimation);

        return storyboard;
    }

    /// <summary>
    /// 页面切换动画
    /// </summary>
    public static async Task AnimatePageTransitionAsync(FrameworkElement outgoingPage, FrameworkElement incomingPage)
    {
        if (_themeService?.IsAnimationEnabled != true)
            return;

        var duration = GetAdjustedDuration(TimeSpan.FromMilliseconds(300));
        
        // 准备动画
        incomingPage.Opacity = 0;
        incomingPage.RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform { TranslateX = 50 };

        // 淡出旧页面
        if (outgoingPage != null)
        {
            var fadeOut = CreateFadeOutAnimation(outgoingPage, duration);
            fadeOut.Begin();
        }

        // 淡入新页面
        var fadeIn = CreateFadeInAnimation(incomingPage, duration);
        var slideIn = CreateSlideInFromLeftAnimation(incomingPage, duration);
        
        fadeIn.Begin();
        slideIn.Begin();

        // 等待动画完成
        await Task.Delay(duration);
    }

    /// <summary>
    /// 微交互动画 - 按钮点击效果
    /// </summary>
    public static void AnimateButtonPress(FrameworkElement button)
    {
        if (_themeService?.IsAnimationEnabled != true)
            return;

        var pressAnimation = CreateScaleAnimation(button, 1.0, 0.95, TimeSpan.FromMilliseconds(100));
        var releaseAnimation = CreateScaleAnimation(button, 0.95, 1.0, TimeSpan.FromMilliseconds(100));

        pressAnimation.Completed += (s, e) => releaseAnimation.Begin();
        pressAnimation.Begin();
    }

    /// <summary>
    /// 微交互动画 - 悬停效果
    /// </summary>
    public static void AnimateHover(FrameworkElement element, bool isEntering)
    {
        if (_themeService?.IsAnimationEnabled != true)
            return;

        var targetScale = isEntering ? 1.05 : 1.0;
        var animation = CreateScaleAnimation(element, element.RenderTransform is Microsoft.UI.Xaml.Media.CompositeTransform ct ? ct.ScaleX : 1.0, targetScale, TimeSpan.FromMilliseconds(200));
        animation.Begin();
    }

    /// <summary>
    /// 列表项动画 - 交错显示
    /// </summary>
    public static async Task AnimateListItemsAsync(FrameworkElement[] items, TimeSpan? staggerDelay = null)
    {
        if (_themeService?.IsAnimationEnabled != true || items.Length == 0)
            return;

        var delay = staggerDelay ?? TimeSpan.FromMilliseconds(50);
        
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            item.Opacity = 0;
            item.RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform { TranslateY = 20 };

            await Task.Delay(delay);

            var animation = CreateSlideInFromLeftAnimation(item, TimeSpan.FromMilliseconds(300));
            animation.Begin();
        }
    }
}