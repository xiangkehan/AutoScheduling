using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AutoScheduling3.Views.Scheduling
{
    public sealed partial class TemplatePage : Page
    {
        public TemplateViewModel ViewModel { get; }

        public TemplatePage()
        {
            this.InitializeComponent();
            ViewModel = (App.Current as App).ServiceProvider.GetService<TemplateViewModel>();
            // The DataContext is set in XAML to enable x:Bind compilation.
            // If you set it here, it will override the XAML setting at runtime.
            // DataContext = ViewModel; 
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Load data when the page is navigated to.
            ViewModel.LoadTemplatesCommand.Execute(null);
        }
    }
}
