using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AutoScheduling3.DTOs;
using System.Linq;

namespace AutoScheduling3.Views.Scheduling
{
    public sealed partial class TemplatePage : Page
    {
        public TemplateViewModel ViewModel { get; }

        public TemplatePage()
        {
            this.InitializeComponent();
            ViewModel = (App.Current as App).ServiceProvider.GetRequiredService<TemplateViewModel>();
            // The DataContext is set in XAML to enable x:Bind compilation.
            // If you set it here, it will override the XAML setting at runtime.
            this.DataContext = ViewModel; 
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Load data when the page is navigated to.
            _ = ViewModel.LoadTemplatesCommand.ExecuteAsync(null);
        }

        private void AddPersonnel_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = AvailablePersonnelList.SelectedItems.Cast<PersonnelDto>().ToList();
            if (selectedItems.Any())
            {
                foreach (var item in selectedItems)
                {
                    if (!ViewModel.SelectedPersonnel.Any(p => p.Id == item.Id))
                    {
                        ViewModel.SelectedPersonnel.Add(item);
                    }
                }
            }
        }

        private void RemovePersonnel_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = SelectedPersonnelList.SelectedItems.Cast<PersonnelDto>().ToList();
            if (selectedItems.Any())
            {
                foreach (var item in selectedItems)
                {
                    ViewModel.SelectedPersonnel.Remove(item);
                }
            }
        }

        private void AddPosition_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = AvailablePositionsList.SelectedItems.Cast<PositionDto>().ToList();
            if (selectedItems.Any())
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
            var selectedItems = SelectedPositionsList.SelectedItems.Cast<PositionDto>().ToList();
            if (selectedItems.Any())
            {
                foreach (var item in selectedItems)
                {
                    ViewModel.SelectedPositions.Remove(item);
                }
            }
        }
    }
}
