using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AutoScheduling3.ViewModels.Settings;
using AutoScheduling3.Helpers;

namespace AutoScheduling3.Views.Settings;

/// <summary>
/// 测试数据生成器页面
/// </summary>
public sealed partial class TestDataGeneratorPage : Page
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public TestDataGeneratorViewModel ViewModel { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public TestDataGeneratorPage()
    {
        this.InitializeComponent();

        // 从ServiceLocator获取ViewModel
        ViewModel = ServiceLocator.GetService<TestDataGeneratorViewModel>();
    }

    /// <summary>
    /// 页面导航到时
    /// </summary>
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // 初始化ViewModel
        await ViewModel.InitializeAsync();
    }

    /// <summary>
    /// 页面导航离开时
    /// </summary>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
    }
}
