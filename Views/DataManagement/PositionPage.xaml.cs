using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls; // Added for Page
using AutoScheduling3.ViewModels.DataManagement;

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
        _ = ViewModel.LoadDataAsync();
    }
}
