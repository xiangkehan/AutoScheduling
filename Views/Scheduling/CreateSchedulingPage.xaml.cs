using AutoScheduling3.DTOs;
using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Linq;

namespace AutoScheduling3.Views.Scheduling
{
    public sealed partial class CreateSchedulingPage : Page
    {
        public SchedulingViewModel ViewModel { get; }

        public CreateSchedulingPage()
        {
            this.InitializeComponent();
            ViewModel = (App.Current as App).ServiceProvider.GetRequiredService<SchedulingViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.CancelCommand.Execute(null); // Reset state on new navigation

            if (e.Parameter is int templateId && templateId > 0)
            {
                // A template is passed, load it.
                _ = ViewModel.LoadTemplateCommand.ExecuteAsync(templateId);
            }
            else
            {
                // Load initial data for a new schedule from scratch.
                _ = ViewModel.LoadDataCommand.ExecuteAsync(null);
            }
            // Always load constraints, this is now called from ViewModel when needed.
            // _ = ViewModel.LoadConstraintsCommand.ExecuteAsync(null);
        }

        private void AddPersonnel_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = AvailablePersonnelList?.SelectedItems.Cast<PersonnelDto>().ToList();
            if (selectedItems != null && selectedItems.Any())
            {
                foreach (var item in selectedItems)
                {
                    if (!ViewModel.SelectedPersonnels.Any(p => p.Id == item.Id))
                    {
                        ViewModel.SelectedPersonnels.Add(item);
                    }
                }
            }
        }

        private void RemovePersonnel_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = SelectedPersonnelList?.SelectedItems.Cast<PersonnelDto>().ToList();
            if (selectedItems != null && selectedItems.Any())
            {
                foreach (var item in selectedItems)
                {
                    ViewModel.SelectedPersonnels.Remove(item);
                }
            }
        }

        private void AddPosition_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = AvailablePositionsList?.SelectedItems.Cast<PositionDto>().ToList();
            if (selectedItems != null && selectedItems.Any())
            {
                foreach (var item in selectedItems)
                {
                    if (!ViewModel.SelectedPositions.Any(p => p.Id == item.Id))
                    {
                        ViewModel.SelectedPositions.Add(item);
                    }
                }
            }
        }

        private void RemovePosition_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = SelectedPositionsList?.SelectedItems.Cast<PositionDto>().ToList();
            if (selectedItems != null && selectedItems.Any())
            {
                foreach (var item in selectedItems)
                {
                    ViewModel.SelectedPositions.Remove(item);
                }
            }
        }
    }
}
