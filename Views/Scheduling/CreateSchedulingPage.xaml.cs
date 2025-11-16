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
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
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

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.IsCreatingManualAssignment))
            {
                if (ManualAssignmentDialog == null)
                    return;

                if (ViewModel.IsCreatingManualAssignment)
                {
                    // 确保对话框有 XamlRoot
                    if (ManualAssignmentDialog.XamlRoot == null)
                    {
                        ManualAssignmentDialog.XamlRoot = this.XamlRoot;
                    }
                    await ManualAssignmentDialog.ShowAsync();
                }
                else
                {
                    ManualAssignmentDialog.Hide();
                }
            }
            else if (e.PropertyName == nameof(ViewModel.IsEditingManualAssignment))
            {
                if (ManualAssignmentEditDialog == null)
                    return;

                if (ViewModel.IsEditingManualAssignment)
                {
                    // 确保对话框有 XamlRoot
                    if (ManualAssignmentEditDialog.XamlRoot == null)
                    {
                        ManualAssignmentEditDialog.XamlRoot = this.XamlRoot;
                    }
                    await ManualAssignmentEditDialog.ShowAsync();
                }
                else
                {
                    ManualAssignmentEditDialog.Hide();
                }
            }
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
