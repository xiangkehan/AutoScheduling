using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AutoScheduling3.ViewModels.DataManagement;
using AutoScheduling3.Helpers;

namespace AutoScheduling3.Views.DataManagement;

/// <summary>
/// 哨位管理页面
/// </summary>
public sealed partial class PositionPage : Page
{
    public PositionViewModel ViewModel { get; }

    public PositionPage()
    {
        this.InitializeComponent();
        ViewModel = ((App)Application.Current).ServiceProvider.GetRequiredService<PositionViewModel>();
        this.Loaded += PositionPage_Loaded;
    }

    private async void PositionPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDataAsync();
        
        // 初始化响应式布局
        if (MainContentGrid.ActualWidth > 0)
        {
            ApplyResponsiveLayout(MainContentGrid.ActualWidth);
        }
    }

    private void MainContentGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveLayout(e.NewSize.Width);
    }

    private void ApplyResponsiveLayout(double availableWidth)
    {
        var breakpoint = ResponsiveHelper.GetCurrentBreakpoint(availableWidth);
        
        // 根据断点调整布局
        switch (breakpoint)
        {
            case ResponsiveHelper.Breakpoint.XSmall:
            case ResponsiveHelper.Breakpoint.Small:
                // 小屏幕：垂直布局
                ApplyVerticalLayout();
                break;
                
            case ResponsiveHelper.Breakpoint.Medium:
            case ResponsiveHelper.Breakpoint.Large:
            case ResponsiveHelper.Breakpoint.XLarge:
                // 大屏幕：水平布局
                ApplyHorizontalLayout();
                break;
        }

        // 应用响应式边距
        var margin = ResponsiveHelper.GetResponsiveMargin(breakpoint);
        MainContentGrid.Margin = new Thickness(margin.Left / 2, margin.Top / 2, margin.Right / 2, margin.Bottom / 2);
    }

    private void ApplyVerticalLayout()
    {
        // 清除列定义，使用行布局
        MainContentGrid.ColumnDefinitions.Clear();
        MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // 设置行定义
        MainContentGrid.RowDefinitions.Clear();
        MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) }); // 间距
        MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // 重新定位元素
        Grid.SetColumn(PositionListGrid, 0);
        Grid.SetRow(PositionListGrid, 0);
        
        Grid.SetColumn(DetailScrollViewer, 0);
        Grid.SetRow(DetailScrollViewer, 2);

        // 隐藏分隔线
        SeparatorBorder.Visibility = Visibility.Collapsed;
    }

    private void ApplyHorizontalLayout()
    {
        // 设置列定义
        MainContentGrid.ColumnDefinitions.Clear();
        MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) }); // 间距
        MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.5, GridUnitType.Star), MinWidth = 350, MaxWidth = 600 });

        // 清除行定义
        MainContentGrid.RowDefinitions.Clear();
        MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // 重新定位元素
        Grid.SetColumn(PositionListGrid, 0);
        Grid.SetRow(PositionListGrid, 0);
        
        Grid.SetColumn(DetailScrollViewer, 2);
        Grid.SetRow(DetailScrollViewer, 0);

        Grid.SetColumn(SeparatorBorder, 1);
        Grid.SetRow(SeparatorBorder, 0);

        // 显示分隔线
        SeparatorBorder.Visibility = Visibility.Visible;
    }
}
