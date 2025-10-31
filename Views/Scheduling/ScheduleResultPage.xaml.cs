using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.UI.Xaml;

namespace AutoScheduling3.Views.Scheduling
{
    public sealed partial class ScheduleResultPage : Page
    {
        public ScheduleResultViewModel ViewModel { get; }

        public ScheduleResultPage()
        {
            this.InitializeComponent();
            ViewModel = ((App)Application.Current).ServiceProvider.GetRequiredService<ScheduleResultViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is int scheduleId)
            {
                ViewModel.LoadScheduleCommand.Execute(scheduleId);
            }
        }
    }
}
