using AutoScheduling3.ViewModels.History;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AutoScheduling3.Views.History
{
    public sealed partial class HistoryPage : Page
    {
        public HistoryViewModel ViewModel { get; }

        public HistoryPage()
        {
            this.InitializeComponent();
            ViewModel = (App.Current as App).ServiceProvider.GetRequiredService<HistoryViewModel>();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!ViewModel.IsLoaded)
            {
                await ViewModel.LoadAsync();
            }
        }
    }
}
