using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 响应式布局辅助类 - 提供自适应布局支持
/// </summary>
public static class ResponsiveHelper
{
    /// <summary>
    /// 断点定义
    /// </summary>
    public enum Breakpoint
    {
        XSmall = 0,    // < 600px
        Small = 600,   // 600px - 960px
        Medium = 960,  // 960px - 1280px
        Large = 1280,  // 1280px - 1920px
        XLarge = 1920  // > 1920px
    }

    /// <summary>
    /// 获取当前断点
    /// </summary>
    public static Breakpoint GetCurrentBreakpoint(double width)
    {
        return width switch
        {
            < 600 => Breakpoint.XSmall,
            < 960 => Breakpoint.Small,
            < 1280 => Breakpoint.Medium,
            < 1920 => Breakpoint.Large,
            _ => Breakpoint.XLarge
        };
    }

    /// <summary>
    /// 根据断点调整网格列数
    /// </summary>
    public static int GetGridColumns(Breakpoint breakpoint, int maxColumns = 4)
    {
        return breakpoint switch
        {
            Breakpoint.XSmall => 1,
            Breakpoint.Small => Math.Min(2, maxColumns),
            Breakpoint.Medium => Math.Min(3, maxColumns),
            Breakpoint.Large => Math.Min(4, maxColumns),
            Breakpoint.XLarge => maxColumns,
            _ => 1
        };
    }

    /// <summary>
    /// 根据断点调整边距
    /// </summary>
    public static Thickness GetResponsiveMargin(Breakpoint breakpoint)
    {
        return breakpoint switch
        {
            Breakpoint.XSmall => new Thickness(8),
            Breakpoint.Small => new Thickness(12),
            Breakpoint.Medium => new Thickness(16),
            Breakpoint.Large => new Thickness(20),
            Breakpoint.XLarge => new Thickness(24),
            _ => new Thickness(16)
        };
    }

    /// <summary>
    /// 根据断点调整字体大小
    /// </summary>
    public static double GetResponsiveFontSize(Breakpoint breakpoint, double baseFontSize = 14)
    {
        var multiplier = breakpoint switch
        {
            Breakpoint.XSmall => 0.9,
            Breakpoint.Small => 1.0,
            Breakpoint.Medium => 1.0,
            Breakpoint.Large => 1.1,
            Breakpoint.XLarge => 1.2,
            _ => 1.0
        };

        return baseFontSize * multiplier;
    }

    /// <summary>
    /// 应用响应式布局到网格
    /// </summary>
    public static void ApplyResponsiveLayout(Grid grid, double availableWidth, int maxColumns = 4)
    {
        var breakpoint = GetCurrentBreakpoint(availableWidth);
        var columns = GetGridColumns(breakpoint, maxColumns);

        // 清除现有列定义
        grid.ColumnDefinitions.Clear();

        // 添加新的列定义
        for (int i = 0; i < columns; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        // 重新排列子元素
        var children = new FrameworkElement[grid.Children.Count];
        for (int i = 0; i < grid.Children.Count; i++)
        {
            children[i] = (FrameworkElement)grid.Children[i];
        }

        for (int i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var column = i % columns;
            var row = i / columns;

            Grid.SetColumn(child, column);
            Grid.SetRow(child, row);

            // 确保有足够的行
            while (grid.RowDefinitions.Count <= row)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
        }

        // 应用响应式边距
        var margin = GetResponsiveMargin(breakpoint);
        grid.Margin = margin;
    }

    /// <summary>
    /// 应用响应式字体大小
    /// </summary>
    public static void ApplyResponsiveFontSize(FrameworkElement element, double availableWidth, double baseFontSize = 14)
    {
        var breakpoint = GetCurrentBreakpoint(availableWidth);
        var fontSize = GetResponsiveFontSize(breakpoint, baseFontSize);

        if (element is TextBlock textBlock)
        {
            textBlock.FontSize = fontSize;
        }
        else if (element is Control control)
        {
            control.FontSize = fontSize;
        }
    }

    /// <summary>
    /// 获取导航面板显示模式
    /// </summary>
    public static NavigationViewPaneDisplayMode GetNavigationPaneDisplayMode(Breakpoint breakpoint)
    {
        return breakpoint switch
        {
            Breakpoint.XSmall => NavigationViewPaneDisplayMode.LeftMinimal,
            Breakpoint.Small => NavigationViewPaneDisplayMode.LeftCompact,
            Breakpoint.Medium => NavigationViewPaneDisplayMode.Left,
            Breakpoint.Large => NavigationViewPaneDisplayMode.Left,
            Breakpoint.XLarge => NavigationViewPaneDisplayMode.Left,
            _ => NavigationViewPaneDisplayMode.Left
        };
    }

    /// <summary>
    /// 检查是否为移动设备尺寸
    /// </summary>
    public static bool IsMobileSize(double width)
    {
        return GetCurrentBreakpoint(width) == Breakpoint.XSmall;
    }

    /// <summary>
    /// 检查是否为平板设备尺寸
    /// </summary>
    public static bool IsTabletSize(double width)
    {
        var breakpoint = GetCurrentBreakpoint(width);
        return breakpoint == Breakpoint.Small || breakpoint == Breakpoint.Medium;
    }

    /// <summary>
    /// 检查是否为桌面设备尺寸
    /// </summary>
    public static bool IsDesktopSize(double width)
    {
        var breakpoint = GetCurrentBreakpoint(width);
        return breakpoint == Breakpoint.Large || breakpoint == Breakpoint.XLarge;
    }
}