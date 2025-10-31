using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.UI.Xaml;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Views.Scheduling
{
    public sealed partial class ScheduleResultPage : Page
    {
        public ScheduleResultViewModel ViewModel { get; }

        public ScheduleResultPage()
        {
            this.InitializeComponent();
            ViewModel = ((App)Application.Current).ServiceProvider.GetRequiredService<ScheduleResultViewModel>();
            this.DataContext = ViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is int scheduleId)
            {
                _ = ViewModel.LoadScheduleCommand.ExecuteAsync(scheduleId);
            }
            else if (e.Parameter is SchedulingRequestDto)
            {
                // This case is for when we navigate back from rescheduling.
                // The ViewModel on the Create page should handle this.
                // Here, we just make sure we don't try to load a schedule with a DTO.
            }
        }
    }
}
