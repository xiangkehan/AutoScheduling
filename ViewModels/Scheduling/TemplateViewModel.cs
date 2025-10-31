using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using AutoScheduling3.Models.Constraints;
using System.Collections.Generic;

namespace AutoScheduling3.ViewModels.Scheduling
{
 /// <summary>
 /// 排班模板管理 ViewModel：模板验证 / 类型筛选 / 分页 / 搜索 /复制 / 删除 / 使用
 /// 手动属性实现，避免源生成冲突。
 /// </summary>
 public class TemplateViewModel : ObservableObject
 {
 private readonly ITemplateService _templateService;
 private readonly IPersonnelService _personnelService;
 private readonly IPositionService _positionService;
 private readonly ISchedulingService _schedulingService;
 private readonly DialogService _dialogService;
 private readonly NavigationService _navigationService;

 private List<SchedulingTemplateDto> _allTemplates = new();
 private ObservableCollection<SchedulingTemplateDto> _templates = new();
 public ObservableCollection<SchedulingTemplateDto> Templates
 {
 get => _templates;
 private set => SetProperty(ref _templates, value);
 }

 private SchedulingTemplateDto _selectedTemplate;
 public SchedulingTemplateDto SelectedTemplate
 {
 get => _selectedTemplate;
 set
 {
 if (SetProperty(ref _selectedTemplate, value))
 {
 IsDetailPaneOpen = value != null;
 if (value != null)
 {
 LoadDetailsForSelectedTemplate();
 }
 RefreshCommandStates();
 }
 }
 }

 private ObservableCollection<PersonnelDto> _availablePersonnel = new();
 public ObservableCollection<PersonnelDto> AvailablePersonnel
 {
 get => _availablePersonnel;
 private set => SetProperty(ref _availablePersonnel, value);
 }

 private ObservableCollection<PositionDto> _availablePositions = new();
 public ObservableCollection<PositionDto> AvailablePositions
 {
 get => _availablePositions;
 private set => SetProperty(ref _availablePositions, value);
 }

 private ObservableCollection<PersonnelDto> _selectedPersonnel = new();
 public ObservableCollection<PersonnelDto> SelectedPersonnel
 {
 get => _selectedPersonnel;
 set => SetProperty(ref _selectedPersonnel, value);
 }

 private ObservableCollection<PositionDto> _selectedPositions = new();
 public ObservableCollection<PositionDto> SelectedPositions
 {
 get => _selectedPositions;
 set => SetProperty(ref _selectedPositions, value);
 }

 // Constraint Properties
 private ObservableCollection<HolidayConfig> _holidayConfigs = new();
 public ObservableCollection<HolidayConfig> HolidayConfigs { get => _holidayConfigs; set => SetProperty(ref _holidayConfigs, value); }

 private ObservableCollection<FixedPositionRule> _fixedPositionRules = new();
 public ObservableCollection<FixedPositionRule> FixedPositionRules { get => _fixedPositionRules; set => SetProperty(ref _fixedPositionRules, value); }


 private bool _isLoading;
 public bool IsLoading { get => _isLoading; set { if (SetProperty(ref _isLoading, value)) RefreshCommandStates(); } }
 private bool _isLoadingDetails;
 public bool IsLoadingDetails { get => _isLoadingDetails; set { if (SetProperty(ref _isLoadingDetails, value)) RefreshCommandStates(); } }
 private bool _isDetailPaneOpen;
 public bool IsDetailPaneOpen { get => _isDetailPaneOpen; set => SetProperty(ref _isDetailPaneOpen, value); }
 private bool _isValidating;
 public bool IsValidating { get => _isValidating; set { if (SetProperty(ref _isValidating, value)) RefreshCommandStates(); } }
 private string _searchKeyword = string.Empty;
 public string SearchKeyword
 {
 get => _searchKeyword;
 set
 {
 if (SetProperty(ref _searchKeyword, value))
 {
 _currentPage =1;
 RefreshPagedView();
 }
 }
 }
 private string _typeFilter; // null/regular/holiday/special
 public string TypeFilter
 {
 get => _typeFilter;
 set
 {
 if (SetProperty(ref _typeFilter, value))
 {
 _currentPage =1;
 RefreshPagedView();
 }
 }
 }
 private int _pageSize =10;
 public int PageSize
 {
 get => _pageSize;
 set
 {
 if (value <=0) value =10;
 if (SetProperty(ref _pageSize, value))
 {
 _currentPage =1;
 RefreshPagedView();
 }
 }
 }
 private int _currentPage =1;
 public int CurrentPage
 {
 get => _currentPage;
 set
 {
 if (SetProperty(ref _currentPage, value))
 {
 RefreshPagedView();
 }
 }
 }
 private int _totalPages =1;
 public int TotalPages
 {
 get => _totalPages;
 private set => SetProperty(ref _totalPages, value);
 }
 private TemplateValidationResult _validationResult;
 public TemplateValidationResult ValidationResult
 {
 get => _validationResult;
 private set => SetProperty(ref _validationResult, value);
 }

 public List<string> TemplateTypes { get; } = new() { "regular", "holiday", "special" };
 public bool HasData => Templates.Count >0;

 // Commands
 public IAsyncRelayCommand LoadTemplatesCommand { get; }
 public IRelayCommand CreateTemplateCommand { get; }
 public IAsyncRelayCommand SaveTemplateCommand { get; }
 public IAsyncRelayCommand DeleteTemplateCommand { get; }
 public IAsyncRelayCommand DuplicateTemplateCommand { get; }
 public IRelayCommand UseTemplateCommand { get; }
 public IRelayCommand ClearFilterCommand { get; }
 public IRelayCommand<string> ApplyTypeFilterCommand { get; }
 public IRelayCommand NextPageCommand { get; }
 public IRelayCommand PrevPageCommand { get; }
 public IAsyncRelayCommand ValidateTemplateCommand { get; }

 public TemplateViewModel(
 ITemplateService templateService,
 IPersonnelService personnelService,
 IPositionService positionService,
 ISchedulingService schedulingService,
 DialogService dialogService,
 NavigationService navigationService)
 {
 _templateService = templateService;
 _personnelService = personnelService;
 _positionService = positionService;
 _schedulingService = schedulingService;
 _dialogService = dialogService;
 _navigationService = navigationService;

 LoadTemplatesCommand = new AsyncRelayCommand(LoadTemplatesAsync);
 CreateTemplateCommand = new RelayCommand(CreateTemplate);
 SaveTemplateCommand = new AsyncRelayCommand(SaveTemplateAsync, () => CanSaveSelected());
 DeleteTemplateCommand = new AsyncRelayCommand(DeleteTemplateAsync, () => CanModifySelected());
 DuplicateTemplateCommand = new AsyncRelayCommand(DuplicateTemplateAsync, () => CanModifySelected());
 UseTemplateCommand = new RelayCommand(UseTemplate, () => CanModifySelected());
 ClearFilterCommand = new RelayCommand(() => { TypeFilter = null; CurrentPage =1; RefreshPagedView(); });
 ApplyTypeFilterCommand = new RelayCommand<string>(t => { TypeFilter = t; CurrentPage =1; RefreshPagedView(); });
 NextPageCommand = new RelayCommand(NextPage, () => CurrentPage < TotalPages);
 PrevPageCommand = new RelayCommand(PrevPage, () => CurrentPage >1);
 ValidateTemplateCommand = new AsyncRelayCommand(ValidateTemplateAsync, () => CanModifySelected());
 }

 private async Task LoadTemplatesAsync()
 {
 if (IsLoading) return;
 IsLoading = true;
 try
 {
 var tplTask = _templateService.GetAllAsync();
 var perTask = _personnelService.GetAllAsync();
 var posTask = _positionService.GetAllAsync();
 var holidayConfigsTask = _schedulingService.GetHolidayConfigsAsync();
 var fixedRulesTask = _schedulingService.GetFixedPositionRulesAsync(false); // Load all rules

 await Task.WhenAll(tplTask, perTask, posTask, holidayConfigsTask, fixedRulesTask);

 _allTemplates = tplTask.Result.OrderByDescending(t => t.IsDefault).ThenByDescending(t => t.UsageCount).ToList();
 AvailablePersonnel = new ObservableCollection<PersonnelDto>(perTask.Result);
 AvailablePositions = new ObservableCollection<PositionDto>(posTask.Result);
 HolidayConfigs = new ObservableCollection<HolidayConfig>(holidayConfigsTask.Result);
 FixedPositionRules = new ObservableCollection<FixedPositionRule>(fixedRulesTask.Result);

 CurrentPage = 1;
 RefreshPagedView();
 }
 catch (Exception ex)
 {
 await _dialogService.ShowErrorAsync("加载模板或约束失败", ex);
 }
 finally
 {
 IsLoading = false;
 RefreshCommandStates();
 }
 }

 private void RefreshPagedView()
 {
 var filtered = string.IsNullOrWhiteSpace(TypeFilter)
 ? _allTemplates
 : _allTemplates.Where(t => t.TemplateType.Equals(TypeFilter, StringComparison.OrdinalIgnoreCase)).ToList();
 if (!string.IsNullOrWhiteSpace(SearchKeyword))
 filtered = filtered.Where(t => t.Name.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase)).ToList();
 TotalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
 if (CurrentPage > TotalPages) CurrentPage = TotalPages;
 var pageItems = filtered.Skip((CurrentPage -1) * PageSize).Take(PageSize).ToList();
 Templates = new ObservableCollection<SchedulingTemplateDto>(pageItems);
 RefreshCommandStates();
 }

 private void CreateTemplate()
 {
 SelectedTemplate = new SchedulingTemplateDto
 {
 Id = 0, // Use 0 for new unsaved item
 Name = "新模板",
 TemplateType = string.IsNullOrWhiteSpace(TypeFilter) ? "regular" : TypeFilter,
 PersonnelIds = new List<int>(),
 PositionIds = new List<int>(),
 EnabledFixedRuleIds = new List<int>(),
 EnabledManualAssignmentIds = new List<int>(),
 UseActiveHolidayConfig = true,
 CreatedAt = DateTime.Now
 };
 }

 private async Task SaveTemplateAsync()
 {
 if (SelectedTemplate == null) return;

 // Update DTO from UI state before saving
 SelectedTemplate.PersonnelIds = SelectedPersonnel.Select(p => p.Id).ToList();
 SelectedTemplate.PositionIds = SelectedPositions.Select(p => p.Id).ToList();
 SelectedTemplate.EnabledFixedRuleIds = FixedPositionRules.Where(r => r.IsEnabled).Select(r => r.Id).ToList();
 // Manual assignments are not edited here. Keep existing values.

 IsLoadingDetails = true;
 try
 {
 if (SelectedTemplate.Id == 0) // New template
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
 EnabledManualAssignmentIds = SelectedTemplate.EnabledManualAssignmentIds // Keep original
 };
 var newTpl = await _templateService.CreateAsync(createDto);
 _allTemplates.Add(newTpl);
 RefreshPagedView(); // Refresh list
 SelectedTemplate = newTpl; // Select the newly created item
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
 EnabledManualAssignmentIds = SelectedTemplate.EnabledManualAssignmentIds // Keep original
 };
 await _templateService.UpdateAsync(SelectedTemplate.Id, updateDto);
 var latest = await _templateService.GetByIdAsync(SelectedTemplate.Id);
 if (latest != null)
 {
 var idx = _allTemplates.FindIndex(t => t.Id == latest.Id);
 if (idx >= 0) _allTemplates[idx] = latest;
 RefreshPagedView(); // Refresh list
 SelectedTemplate = latest; // Reselect with updated data
 }
 }
 await _dialogService.ShowSuccessAsync("模板已保存");
 IsDetailPaneOpen = false;
 }
 catch (Exception ex)
 {
 await _dialogService.ShowErrorAsync("保存模板失败", ex);
 }
 finally
 {
 IsLoadingDetails = false;
 RefreshCommandStates();
 }
 }

 private async Task DeleteTemplateAsync()
 {
 if (!CanModifySelected()) return;
 var confirm = await _dialogService.ShowConfirmAsync("删除模板", $"确定删除模板 '{SelectedTemplate.Name}'?", "删除", "取消");
 if (!confirm) return;
 IsLoading = true;
 try
 {
 await _templateService.DeleteAsync(SelectedTemplate.Id);
 _allTemplates.RemoveAll(t => t.Id == SelectedTemplate.Id);
 SelectedTemplate = null;
 IsDetailPaneOpen = false;
 RefreshPagedView();
 await _dialogService.ShowSuccessAsync("模板已删除");
 }
 catch (Exception ex)
 {
 await _dialogService.ShowErrorAsync("删除失败", ex);
 }
 finally
 {
 IsLoading = false;
 RefreshCommandStates();
 }
 }

 private async Task DuplicateTemplateAsync()
 {
 if (!CanModifySelected()) return;
 var newName = await _dialogService.ShowInputDialogAsync("复制模板", "输入副本名称", $"{SelectedTemplate.Name} (副本)");
 if (string.IsNullOrWhiteSpace(newName)) return;
 IsLoading = true;
 try
 {
 var createDto = new CreateTemplateDto
 {
 Name = newName.Trim(),
 Description = SelectedTemplate.Description,
 TemplateType = SelectedTemplate.TemplateType,
 IsDefault = false,
 PersonnelIds = SelectedTemplate.PersonnelIds.ToList(),
 PositionIds = SelectedTemplate.PositionIds.ToList(),
 HolidayConfigId = SelectedTemplate.HolidayConfigId,
 UseActiveHolidayConfig = SelectedTemplate.UseActiveHolidayConfig,
 EnabledFixedRuleIds = SelectedTemplate.EnabledFixedRuleIds.ToList(),
 EnabledManualAssignmentIds = SelectedTemplate.EnabledManualAssignmentIds.ToList()
 };
 var dup = await _templateService.CreateAsync(createDto);
 _allTemplates.Add(dup);
 SelectedTemplate = dup;
 IsDetailPaneOpen = true;
 RefreshPagedView();
 await _dialogService.ShowSuccessAsync("模板复制成功");
 }
 catch (Exception ex)
 {
 await _dialogService.ShowErrorAsync("复制失败", ex);
 }
 finally
 {
 IsLoading = false;
 RefreshCommandStates();
 }
 }

 private async Task ValidateTemplateAsync()
 {
 if (!CanModifySelected()) return;
 IsValidating = true;
 try
 {
 ValidationResult = await _templateService.ValidateAsync(SelectedTemplate.Id);
 if (ValidationResult != null)
 {
 var msg = string.Join("\n", new[]
 {
 $"有效: {ValidationResult.IsValid}",
 ValidationResult.Errors.Count >0 ? "错误:\n - " + string.Join("\n - ", ValidationResult.Errors.Select(e => e.Message)) : "无错误",
 ValidationResult.Warnings.Count >0 ? "警告:\n - " + string.Join("\n - ", ValidationResult.Warnings.Select(e => e.Message)) : "无警告"
 });
 await _dialogService.ShowMessageAsync("模板验证结果", msg);
 }
 }
 catch (Exception ex)
 {
 await _dialogService.ShowErrorAsync("验证失败", ex);
 }
 finally
 {
 IsValidating = false;
 RefreshCommandStates();
 }
 }

 private void UseTemplate()
 {
 if (!CanModifySelected()) return;
 _navigationService.NavigateTo("CreateScheduling", SelectedTemplate.Id);
 }

 private void NextPage()
 {
 if (CurrentPage < TotalPages)
 {
 CurrentPage++;
 RefreshPagedView();
 }
 }
 private void PrevPage()
 {
 if (CurrentPage >1)
 {
 CurrentPage--;
 RefreshPagedView();
 }
 }

 private bool CanModifySelected() => SelectedTemplate != null && SelectedTemplate.Id != 0;
 private bool CanSaveSelected() => SelectedTemplate != null && !string.IsNullOrWhiteSpace(SelectedTemplate.Name) && SelectedPersonnel.Count > 0 && SelectedPositions.Count > 0;

 private void LoadDetailsForSelectedTemplate()
 {
 if (SelectedTemplate == null)
 {
 SelectedPersonnel.Clear();
 SelectedPositions.Clear();
 foreach (var rule in FixedPositionRules) rule.IsEnabled = false;
 return;
 }

 // Update personnel and positions
 SelectedPersonnel = new ObservableCollection<PersonnelDto>(AvailablePersonnel.Where(p => SelectedTemplate.PersonnelIds.Contains(p.Id)));
 SelectedPositions = new ObservableCollection<PositionDto>(AvailablePositions.Where(p => SelectedTemplate.PositionIds.Contains(p.Id)));

 // Update constraints
 foreach (var rule in FixedPositionRules)
 {
 rule.IsEnabled = SelectedTemplate.EnabledFixedRuleIds.Contains(rule.Id);
 }
 // Note: HolidayConfigId and UseActiveHolidayConfig are bound directly to SelectedTemplate properties.
 }

 private void RefreshCommandStates()
 {
 SaveTemplateCommand.NotifyCanExecuteChanged();
 DeleteTemplateCommand.NotifyCanExecuteChanged();
 DuplicateTemplateCommand.NotifyCanExecuteChanged();
 UseTemplateCommand.NotifyCanExecuteChanged();
 ValidateTemplateCommand.NotifyCanExecuteChanged();
 NextPageCommand.NotifyCanExecuteChanged();
 PrevPageCommand.NotifyCanExecuteChanged();
 }
 }
}
