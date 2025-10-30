using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls; // Added for Page
using AutoScheduling3.ViewModels.DataManagement;

namespace AutoScheduling3.Views.DataManagement
{
    public sealed partial class PersonnelPage : Page
    {
        public PersonnelViewModel ViewModel { get; }

        public PersonnelPage()
        {
            this.InitializeComponent();
            ViewModel = ((App)Application.Current).ServiceProvider.GetRequiredService<PersonnelViewModel>();
            _ = ViewModel.LoadDataAsync();
        }
    }
}
