using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.ViewModels.Base;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoScheduling3.ViewModels.Scheduling;

/// <summary>
/// 排班模板管理 ViewModel
/// </summary>
public partial class TemplateViewModel : ObservableObject
{
    private readonly ITemplateService _templateService;
    private readonly IPersonnelService _personnelService;
    private readonly IPositionService _positionService;
    private readonly DialogService _dialogService;
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<SchedulingTemplateDto> _templates = new();

    [ObservableProperty]
    private SchedulingTemplateDto _selectedTemplate;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLoadingDetails;

    [ObservableProperty]
    private bool _isDetailPaneOpen;

    [ObservableProperty]
    private string _searchKeyword;

    [ObservableProperty]
    private ObservableCollection<string> _searchSuggestions = new();

    [ObservableProperty]
    private ObservableCollection<PersonnelDto> _availablePersonnel = new();

    [ObservableProperty]
    private ObservableCollection<PositionDto> _availablePositions = new();

    [ObservableProperty]
    private ObservableCollection<PersonnelDto> _selectedPersonnel = new();

    [ObservableProperty]
    private ObservableCollection<PositionDto> _selectedPositions = new();

    public List<string> TemplateTypes { get; } = new List<string> { "regular", "holiday", "special" };

    public TemplateViewModel(
        ITemplateService templateService,
        IPersonnelService personnelService,
        IPositionService positionService,
        DialogService dialogService,
        NavigationService navigationService)
    {
        _templateService = templateService;
        _personnelService = personnelService;
        _positionService = positionService;
        _dialogService = dialogService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private async Task LoadTemplatesAsync()
    {
        IsLoading = true;
        try
        {
            var templatesTask = _templateService.GetAllTemplatesAsync();
            var personnelTask = _personnelService.GetAllAsync();
            var positionsTask = _positionService.GetAllAsync();

            await Task.WhenAll(templatesTask, personnelTask, positionsTask);

            Templates = new ObservableCollection<SchedulingTemplateDto>(templatesTask.Result);
            AvailablePersonnel = new ObservableCollection<PersonnelDto>(personnelTask.Result);
            AvailablePositions = new ObservableCollection<PositionDto>(positionsTask.Result);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Failed to load templates.", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CreateTemplate()
    {
        SelectedTemplate = new SchedulingTemplateDto 
        { 
            Id = -1, // Indicates a new template
            Name = "New Template",
            PersonnelIds = new List<int>(),
            PositionIds = new List<int>(),
            EnabledFixedRuleIds = new List<int>(),
            EnabledManualAssignmentIds = new List<int>(),
            CreatedAt = DateTime.Now
        };
        IsDetailPaneOpen = true;
    }

    [RelayCommand]
    private async Task SaveTemplateAsync()
    {
        if (SelectedTemplate == null) return;

        SelectedTemplate.PersonnelIds = SelectedPersonnel.Select(p => p.Id).ToList();
        SelectedTemplate.PositionIds = SelectedPositions.Select(p => p.Id).ToList();

        IsLoadingDetails = true;
        try
        {
            if (SelectedTemplate.Id == -1) // New template
            {
                var createDto = new CreateTemplateDto
                {
                    Name = SelectedTemplate.Name,
                    Description = SelectedTemplate.Description,
                    TemplateType = SelectedTemplate.TemplateType,
                    IsDefault = SelectedTemplate.IsDefault,
                    PersonnelIds = SelectedTemplate.PersonnelIds,
                    PositionIds = SelectedTemplate.PositionIds,
                    HolidayConfigId = SelectedTemplate.HolidayConfigId,
                    UseActiveHolidayConfig = SelectedTemplate.UseActiveHolidayConfig,
                    EnabledFixedRuleIds = SelectedTemplate.EnabledFixedRuleIds,
                    EnabledManualAssignmentIds = SelectedTemplate.EnabledManualAssignmentIds
                };
                var newTemplate = await _templateService.CreateTemplateAsync(createDto);
                Templates.Add(newTemplate);
                SelectedTemplate = newTemplate;
            }
            else // Existing template
            {
                var updateDto = new UpdateTemplateDto
                {
                    Name = SelectedTemplate.Name,
                    Description = SelectedTemplate.Description,
                    TemplateType = SelectedTemplate.TemplateType,
                    IsDefault = SelectedTemplate.IsDefault,
                    PersonnelIds = SelectedTemplate.PersonnelIds,
                    PositionIds = SelectedTemplate.PositionIds,
                    HolidayConfigId = SelectedTemplate.HolidayConfigId,
                    UseActiveHolidayConfig = SelectedTemplate.UseActiveHolidayConfig,
                    EnabledFixedRuleIds = SelectedTemplate.EnabledFixedRuleIds,
                    EnabledManualAssignmentIds = SelectedTemplate.EnabledManualAssignmentIds
                };
                await _templateService.UpdateTemplateAsync(SelectedTemplate.Id, updateDto);
                // Refresh the list to show changes
                var index = Templates.ToList().FindIndex(t => t.Id == SelectedTemplate.Id);
                if (index != -1)
                {
                    Templates[index] = await _templateService.GetTemplateByIdAsync(SelectedTemplate.Id);
                }
            }
            await _dialogService.ShowSuccessAsync("Template saved successfully.");
            IsDetailPaneOpen = false;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Failed to save template.", ex.Message);
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    [RelayCommand]
    private async Task DeleteTemplateAsync()
    {
        if (SelectedTemplate == null) return;

        var confirm = await _dialogService.ShowConfirmAsync("Delete Template", $"Are you sure you want to delete '{SelectedTemplate.Name}'?");
        if (!confirm) return;

        IsLoading = true;
        try
        {
            await _templateService.DeleteTemplateAsync(SelectedTemplate.Id);
            Templates.Remove(SelectedTemplate);
            SelectedTemplate = null;
            IsDetailPaneOpen = false;
            await _dialogService.ShowSuccessAsync("Template deleted.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Failed to delete template.", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DuplicateTemplateAsync()
    {
        if (SelectedTemplate == null) return;
        
        var newName = await _dialogService.ShowInputDialogAsync("Duplicate Template", "Enter a name for the new template:", $"{SelectedTemplate.Name} (Copy)");
        if (string.IsNullOrWhiteSpace(newName)) return;

        IsLoading = true;
        try
        {
            var duplicatedTemplate = await _templateService.DuplicateTemplateAsync(SelectedTemplate.Id, newName);
            Templates.Add(duplicatedTemplate);
            SelectedTemplate = duplicatedTemplate;
            IsDetailPaneOpen = true;
            await _dialogService.ShowSuccessAsync("Template duplicated successfully.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Failed to duplicate template.", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void UseTemplate()
    {
        if (SelectedTemplate == null) return;
        _navigationService.NavigateTo("CreateScheduling", SelectedTemplate.Id);
    }

    partial void OnSelectedTemplateChanged(SchedulingTemplateDto value)
    {
        IsDetailPaneOpen = value != null;
        if (value != null)
        {
            UpdateSelectedPersonnelAndPositions();
        }
    }

    private void UpdateSelectedPersonnelAndPositions()
    {
        if (SelectedTemplate == null)
        {
            SelectedPersonnel.Clear();
            SelectedPositions.Clear();
            return;
        }

        var personnel = AvailablePersonnel.Where(p => SelectedTemplate.PersonnelIds.Contains(p.Id));
        SelectedPersonnel = new ObservableCollection<PersonnelDto>(personnel);

        var positions = AvailablePositions.Where(p => SelectedTemplate.PositionIds.Contains(p.Id));
        SelectedPositions = new ObservableCollection<PositionDto>(positions);
    }

    partial void OnSearchKeywordChanged(string value)
    {
        // This is a simple implementation. A more robust solution would use debouncing.
        if (string.IsNullOrWhiteSpace(value))
        {
            // Reload all templates or use a cached full list
            _ = LoadTemplatesAsync();
        }
        else
        {
            var filtered = Templates.Where(t => t.Name.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList();
            Templates = new ObservableCollection<SchedulingTemplateDto>(filtered);
            // In a real app, you'd fetch from the service:
            // var searchResult = await _templateService.SearchByNameAsync(value);
            // Templates = new ObservableCollection<SchedulingTemplateDto>(searchResult);
        }
    }
}
