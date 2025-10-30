using System.Collections.ObjectModel;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _currentStep = 1;

        [ObservableProperty]
        private string _scheduleTitle;

        [ObservableProperty]
        private DateTimeOffset _startDate = DateTimeOffset.Now;

        [ObservableProperty]
        private DateTimeOffset _endDate = DateTimeOffset.Now.AddDays(7);

        [ObservableProperty]
        private ObservableCollection<PersonnelDto> _selectedPersonnels = new();

        [ObservableProperty]
        private ObservableCollection<PositionDto> _selectedPositions = new();

        [ObservableProperty]
        private ScheduleDto _resultSchedule;

        [ObservableProperty]
        private bool _isExecuting;

        [RelayCommand]
        private void NextStep()
        {
            if (CurrentStep < 5)
            {
                CurrentStep++;
            }
        }

        [RelayCommand]
        private void PreviousStep()
        {
            if (CurrentStep > 1)
            {
                CurrentStep--;
            }
        }

        [RelayCommand]
        private void ExecuteScheduling()
        {
            // Logic to execute scheduling algorithm
        }

        [RelayCommand]
        private void SaveAsTemplate()
        {
            // Logic to save current configuration as a template
        }
    }
}
