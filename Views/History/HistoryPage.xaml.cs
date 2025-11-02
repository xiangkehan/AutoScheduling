using Microsoft.UI.Xaml.Controls;
using AutoScheduling3.ViewModels.History;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

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
            this.Loaded += HistoryPage_Loaded;
        }

        private async void HistoryPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadDataAsync();
        }
    }
}
