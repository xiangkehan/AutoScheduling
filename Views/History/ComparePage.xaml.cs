using AutoScheduling3.ViewModels.History;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace AutoScheduling3.Views.History
{
    public sealed partial class ComparePage : Page
    {
        public CompareViewModel ViewModel { get; }

        public ComparePage()
        {
            this.InitializeComponent();
            ViewModel = (App.Current as App).ServiceProvider.GetRequiredService<CompareViewModel>();
            this.DataContext = ViewModel;
        }
    }
}
