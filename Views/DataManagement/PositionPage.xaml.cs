using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AutoScheduling3.ViewModels.DataManagement;
using AutoScheduling3.DTOs;
using Microsoft.Extensions.DependencyInjection;

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
        ViewModel = App.Services.GetRequiredService<PositionViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadDataAsync();
    }
}
