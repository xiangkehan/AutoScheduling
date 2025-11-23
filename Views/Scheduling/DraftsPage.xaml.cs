using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AutoScheduling3.Views.Scheduling
{
    public sealed partial class DraftsPage : Page
    {
        public DraftsViewModel ViewModel { get; }

        public DraftsPage()
        {
            this.InitializeComponent();
            ViewModel = (App.Current as App).ServiceProvider.GetRequiredService<DraftsViewModel>();
            this.DataContext = ViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _ = ViewModel.LoadDraftsCommand.ExecuteAsync(null);
        }
    }
}
