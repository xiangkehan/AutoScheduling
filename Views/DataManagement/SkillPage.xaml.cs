using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AutoScheduling3.ViewModels.DataManagement;
using AutoScheduling3.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace AutoScheduling3.Views.DataManagement;

/// <summary>
/// 技能管理页面
/// </summary>
public sealed partial class SkillPage : Page
{
    public SkillViewModel ViewModel { get; }

    public SkillPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SkillViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadDataAsync();
    }
}
