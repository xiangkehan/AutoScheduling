using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.ViewModels.Base;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace AutoScheduling3.ViewModels.DataManagement;

/// <summary>
/// 人员管理 ViewModel
/// </summary>
public partial class PersonnelViewModel : ListViewModelBase<PersonnelDto>
{
    private readonly IPersonnelService _personnelService;
    private readonly IPositionService _positionService;
    private readonly ISkillService _skillService;
    private readonly DialogService _dialogService;

    private bool _isEditing;
    private CreatePersonnelDto _newPersonnel = new();
    private UpdatePersonnelDto? _editingPersonnel;

    /// <summary>
    /// 是否正在编辑模式
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    /// <summary>
    /// 新建人员DTO
    /// </summary>
    public CreatePersonnelDto NewPersonnel
    {
        get => _newPersonnel;
        set => SetProperty(ref _newPersonnel, value);
    }

    /// <summary>
    /// 编辑中的人员DTO
    /// </summary>
    public UpdatePersonnelDto? EditingPersonnel
    {
        get => _editingPersonnel;
        set => SetProperty(ref _editingPersonnel, value);
    }

    /// <summary>
    /// 可选哨位列表
    /// </summary>
    public ObservableCollection<PositionDto> AvailablePositions { get; } = new();

    /// <summary>
    /// 可选技能列表
    /// </summary>
    public ObservableCollection<SkillDto> AvailableSkills { get; } = new();

    public PersonnelViewModel(
        IPersonnelService personnelService,
        IPositionService positionService,
        ISkillService skillService,
        DialogService dialogService)
    {
        _personnelService = personnelService ?? throw new ArgumentNullException(nameof(personnelService));
        _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
        _skillService = skillService ?? throw new ArgumentNullException(nameof(skillService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        CreateCommand = new AsyncRelayCommand(CreatePersonnelAsync);
        EditCommand = new AsyncRelayCommand(StartEditAsync, () => SelectedItem != null);
        SaveCommand = new AsyncRelayCommand(SavePersonnelAsync, () => IsEditing);
        CancelCommand = new RelayCommand(CancelEdit, () => IsEditing);
        DeleteCommand = new AsyncRelayCommand(DeletePersonnelAsync, () => SelectedItem != null);
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
    /// 加载数据
    /// </summary>
    public override async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // 加载人员列表
            var personnel = string.IsNullOrWhiteSpace(SearchKeyword)
                ? await _personnelService.GetAllAsync()
                : await _personnelService.SearchAsync(SearchKeyword);

            ClearItems();
            AddRange(personnel);

            // 加载可选项
            var positions = await _positionService.GetAllAsync();
            AvailablePositions.Clear();
            foreach (var pos in positions)
            {
                AvailablePositions.Add(pos);
            }

            var skills = await _skillService.GetAllAsync();
            AvailableSkills.Clear();
            foreach (var skill in skills)
            {
                AvailableSkills.Add(skill);
            }
        }, "正在加载人员数据...");
    }

    /// <summary>
    /// 创建人员
    /// </summary>
    private async Task CreatePersonnelAsync()
    {
        await ExecuteAsync(async () =>
        {
            var created = await _personnelService.CreateAsync(NewPersonnel);
            AddItem(created);

            // 重置表单
            NewPersonnel = new CreatePersonnelDto();

            await _dialogService.ShowSuccessAsync("人员创建成功！");
        }, "正在创建人员...");
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
            // 将选中的人员数据转换为编辑DTO
            EditingPersonnel = new UpdatePersonnelDto
            {
                Name = SelectedItem.Name,
                AvailablePositionIds = new List<int>(SelectedItem.AvailablePositionIds),
                SkillIds = new List<int>(SelectedItem.SkillIds),
                IsAvailable = SelectedItem.IsAvailable,
                IsRetired = SelectedItem.IsRetired
            };

            IsEditing = true;
            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// 保存编辑
    /// </summary>
    private async Task SavePersonnelAsync()
    {
        if (SelectedItem == null || EditingPersonnel == null)
            return;

        await ExecuteAsync(async () =>
        {
            await _personnelService.UpdateAsync(SelectedItem.Id, EditingPersonnel);
            
            // 重新加载数据
            await LoadDataAsync();

            IsEditing = false;
            EditingPersonnel = null;

            await _dialogService.ShowSuccessAsync("人员信息已更新！");
        }, "正在保存...");
    }

    /// <summary>
    /// 取消编辑
    /// </summary>
    private void CancelEdit()
    {
        IsEditing = false;
        EditingPersonnel = null;
    }

    /// <summary>
    /// 删除人员
    /// </summary>
    private async Task DeletePersonnelAsync()
    {
        if (SelectedItem == null)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "确认删除",
            $"确定要删除人员 '{SelectedItem.Name}' 吗？此操作无法撤销。");

        if (!confirmed)
            return;

        await ExecuteAsync(async () =>
        {
            await _personnelService.DeleteAsync(SelectedItem.Id);
            RemoveItem(SelectedItem);
            SelectedItem = null;

            await _dialogService.ShowSuccessAsync("人员已删除！");
        }, "正在删除...");
    }

    protected override void OnError(Exception exception)
    {
        base.OnError(exception);
        _ = _dialogService.ShowErrorAsync("操作失败", exception);
    }
}
