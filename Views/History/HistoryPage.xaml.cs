using Microsoft.UI.Xaml.Controls;
using AutoScheduling3.ViewModels.History;
using Microsoft.Extensions.DependencyInjection;

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
            _ = ViewModel.LoadDataAsync();
        }
    }
}
