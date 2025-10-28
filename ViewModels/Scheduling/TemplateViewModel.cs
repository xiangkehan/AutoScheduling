using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.ViewModels.Base;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;

namespace AutoScheduling3.ViewModels.Scheduling;

/// <summary>
/// 排班模板管理 ViewModel
/// </summary>
public partial class TemplateViewModel : ListViewModelBase<SchedulingTemplateDto>
{
    private readonly ITemplateService _templateService;
    private readonly IPersonnelService _personnelService;
    private readonly IPositionService _positionService;
    private readonly DialogService _dialogService;

    private bool _isEditing;
    private CreateTemplateDto _newTemplate = new();
    private UpdateTemplateDto? _editingTemplate;

    /// <summary>
    /// 是否正在编辑模式
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    /// <summary>
    /// 新建模板DTO
    /// </summary>
    public CreateTemplateDto NewTemplate
    {
        get => _newTemplate;
        set => SetProperty(ref _newTemplate, value);
    }

    /// <summary>
    /// 编辑中的模板DTO
    /// </summary>
    public UpdateTemplateDto? EditingTemplate
    {
        get => _editingTemplate;
        set => SetProperty(ref _editingTemplate, value);
    }

    /// <summary>
    /// 可选人员列表
    /// </summary>
    public ObservableCollection<PersonnelDto> AvailablePersonnel { get; } = new();

    /// <summary>
    /// 可选哨位列表
    /// </summary>
    public ObservableCollection<PositionDto> AvailablePositions { get; } = new();

    public TemplateViewModel(
        ITemplateService templateService,
        IPersonnelService personnelService,
        IPositionService positionService,
        DialogService dialogService)
    {
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _personnelService = personnelService ?? throw new ArgumentNullException(nameof(personnelService));
        _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        CreateCommand = new AsyncRelayCommand(CreateTemplateAsync);
        EditCommand = new AsyncRelayCommand(StartEditAsync, () => SelectedItem != null);
        SaveCommand = new AsyncRelayCommand(SaveTemplateAsync, () => IsEditing);
        CancelCommand = new RelayCommand(CancelEdit, () => IsEditing);
        DeleteCommand = new AsyncRelayCommand(DeleteTemplateAsync, () => SelectedItem != null);
        UseTemplateCommand = new AsyncRelayCommand(UseTemplateAsync, () => SelectedItem != null);
    }

    /// <summary>
    /// 创建命令
    /// </summary>
    public IAsyncRelayCommand CreateCommand { get; }

    /// <summary>
    /// 编辑命令
    /// </summary>
    public IAsyncRelayCommand EditCommand { get; }

    /// <summary>
    /// 保存命令
    /// </summary>
    public IAsyncRelayCommand SaveCommand { get; }

    /// <summary>
    /// 取消命令
    /// </summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>
    /// 删除命令
    /// </summary>
    public IAsyncRelayCommand DeleteCommand { get; }

    /// <summary>
    /// 使用模板命令
    /// </summary>
    public IAsyncRelayCommand UseTemplateCommand { get; }

    /// <summary>
    /// 加载数据
    /// </summary>
    public override async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // 加载模板列表
            var templates = await _templateService.GetAllAsync();

            ClearItems();
            AddRange(templates);

            // 加载可选项
            var personnel = await _personnelService.GetAllAsync();
            AvailablePersonnel.Clear();
            foreach (var p in personnel)
            {
                AvailablePersonnel.Add(p);
            }

            var positions = await _positionService.GetAllAsync();
            AvailablePositions.Clear();
            foreach (var pos in positions)
            {
                AvailablePositions.Add(pos);
            }
        }, "正在加载模板数据...");
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    private async Task CreateTemplateAsync()
    {
        await ExecuteAsync(async () =>
        {
            var created = await _templateService.CreateAsync(NewTemplate);
            AddItem(created);

            // 重置表单
            NewTemplate = new CreateTemplateDto();

            await _dialogService.ShowSuccessAsync("模板创建成功！");
        }, "正在创建模板...");
    }

    /// <summary>
    /// 开始编辑
    /// </summary>
    private async Task StartEditAsync()
    {
        if (SelectedItem == null)
            return;

        await ExecuteAsync(async () =>
        {
            EditingTemplate = new UpdateTemplateDto
            {
                Name = SelectedItem.Name,
                Description = SelectedItem.Description,
                TemplateType = SelectedItem.TemplateType,
                IsDefault = SelectedItem.IsDefault,
                PersonnelIds = new List<int>(SelectedItem.PersonnelIds),
                PositionIds = new List<int>(SelectedItem.PositionIds),
                HolidayConfigId = SelectedItem.HolidayConfigId,
                UseActiveHolidayConfig = SelectedItem.UseActiveHolidayConfig,
                EnabledFixedRuleIds = new List<int>(SelectedItem.EnabledFixedRuleIds),
                EnabledManualAssignmentIds = new List<int>(SelectedItem.EnabledManualAssignmentIds)
            };

            IsEditing = true;
            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// 保存编辑
    /// </summary>
    private async Task SaveTemplateAsync()
    {
        if (SelectedItem == null || EditingTemplate == null)
            return;

        await ExecuteAsync(async () =>
        {
            await _templateService.UpdateAsync(SelectedItem.Id, EditingTemplate);
            
            // 重新加载数据
            await LoadDataAsync();

            IsEditing = false;
            EditingTemplate = null;

            await _dialogService.ShowSuccessAsync("模板信息已更新！");
        }, "正在保存...");
    }

    /// <summary>
    /// 取消编辑
    /// </summary>
    private void CancelEdit()
    {
        IsEditing = false;
        EditingTemplate = null;
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    private async Task DeleteTemplateAsync()
    {
        if (SelectedItem == null)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "确认删除",
            $"确定要删除模板 '{SelectedItem.Name}' 吗？此操作无法撤销。");

        if (!confirmed)
            return;

        await ExecuteAsync(async () =>
        {
            await _templateService.DeleteAsync(SelectedItem.Id);
            RemoveItem(SelectedItem);
            SelectedItem = null;

            await _dialogService.ShowSuccessAsync("模板已删除！");
        }, "正在删除...");
    }

    /// <summary>
    /// 使用模板创建排班
    /// </summary>
    private async Task UseTemplateAsync()
    {
        if (SelectedItem == null)
            return;

        // 此处应该打开一个对话框让用户输入排班参数
        // 简化实现：直接使用默认参数
        var useDto = new UseTemplateDto
        {
            TemplateId = SelectedItem.Id,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            Title = $"使用模板'{SelectedItem.Name}'创建的排班"
        };

        await ExecuteAsync(async () =>
        {
            var schedule = await _templateService.UseTemplateAsync(useDto);
            await _dialogService.ShowSuccessAsync($"已使用模板创建排班，排班ID: {schedule.Id}");
        }, "正在创建排班...");
    }

    protected override void OnError(Exception exception)
    {
        base.OnError(exception);
        _ = _dialogService.ShowErrorAsync("操作失败", exception);
    }
}
