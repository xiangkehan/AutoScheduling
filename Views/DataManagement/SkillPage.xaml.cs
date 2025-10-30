using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls; // Added for Page
using AutoScheduling3.ViewModels.DataManagement;

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
        ViewModel = ((App)Application.Current).ServiceProvider.GetRequiredService<SkillViewModel>();
        _ = ViewModel.LoadDataAsync();
    }
}
