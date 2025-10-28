using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AutoScheduling3.ViewModels.DataManagement;
using AutoScheduling3.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace AutoScheduling3.Views.DataManagement;

/// <summary>
/// 人员管理页面
/// </summary>
public sealed partial class PersonnelPage : Page
{
    public PersonnelViewModel ViewModel { get; }

    public PersonnelPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<PersonnelViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadDataAsync();
    }
}
