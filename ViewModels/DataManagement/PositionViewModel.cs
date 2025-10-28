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
/// 哨位管理 ViewModel
/// </summary>
public partial class PositionViewModel : ListViewModelBase<PositionDto>
{
    private readonly IPositionService _positionService;
    private readonly ISkillService _skillService;
    private readonly DialogService _dialogService;

    private bool _isEditing;
    private CreatePositionDto _newPosition = new();
    private UpdatePositionDto? _editingPosition;

    /// <summary>
    /// 是否正在编辑模式
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    /// <summary>
    /// 新建哨位DTO
    /// </summary>
    public CreatePositionDto NewPosition
    {
        get => _newPosition;
        set => SetProperty(ref _newPosition, value);
    }

    /// <summary>
    /// 编辑中的哨位DTO
    /// </summary>
    public UpdatePositionDto? EditingPosition
    {
        get => _editingPosition;
        set => SetProperty(ref _editingPosition, value);
    }

    /// <summary>
    /// 可选技能列表
    /// </summary>
    public ObservableCollection<SkillDto> AvailableSkills { get; } = new();

    public PositionViewModel(
        IPositionService positionService,
        ISkillService skillService,
        DialogService dialogService)
    {
        _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
        _skillService = skillService ?? throw new ArgumentNullException(nameof(skillService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        CreateCommand = new AsyncRelayCommand(CreatePositionAsync);
        EditCommand = new AsyncRelayCommand(StartEditAsync, () => SelectedItem != null);
        SaveCommand = new AsyncRelayCommand(SavePositionAsync, () => IsEditing);
        CancelCommand = new RelayCommand(CancelEdit, () => IsEditing);
        DeleteCommand = new AsyncRelayCommand(DeletePositionAsync, () => SelectedItem != null);
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
            // 加载哨位列表
            var positions = string.IsNullOrWhiteSpace(SearchKeyword)
                ? await _positionService.GetAllAsync()
                : await _positionService.SearchAsync(SearchKeyword);

            ClearItems();
            AddRange(positions);

            // 加载可选技能
            var skills = await _skillService.GetAllAsync();
            AvailableSkills.Clear();
            foreach (var skill in skills)
            {
                AvailableSkills.Add(skill);
            }
        }, "正在加载哨位数据...");
    }

    /// <summary>
    /// 创建哨位
    /// </summary>
    private async Task CreatePositionAsync()
    {
        await ExecuteAsync(async () =>
        {
            var created = await _positionService.CreateAsync(NewPosition);
            AddItem(created);

            // 重置表单
            NewPosition = new CreatePositionDto();

            await _dialogService.ShowSuccessAsync("哨位创建成功！");
        }, "正在创建哨位...");
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
            EditingPosition = new UpdatePositionDto
            {
                Name = SelectedItem.Name,
                Location = SelectedItem.Location,
                Description = SelectedItem.Description,
                Requirements = SelectedItem.Requirements,
                RequiredSkillIds = new List<int>(SelectedItem.RequiredSkillIds)
            };

            IsEditing = true;
            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// 保存编辑
    /// </summary>
    private async Task SavePositionAsync()
    {
        if (SelectedItem == null || EditingPosition == null)
            return;

        await ExecuteAsync(async () =>
        {
            await _positionService.UpdateAsync(SelectedItem.Id, EditingPosition);
            
            // 重新加载数据
            await LoadDataAsync();

            IsEditing = false;
            EditingPosition = null;

            await _dialogService.ShowSuccessAsync("哨位信息已更新！");
        }, "正在保存...");
    }

    /// <summary>
    /// 取消编辑
    /// </summary>
    private void CancelEdit()
    {
        IsEditing = false;
        EditingPosition = null;
    }

    /// <summary>
    /// 删除哨位
    /// </summary>
    private async Task DeletePositionAsync()
    {
        if (SelectedItem == null)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "确认删除",
            $"确定要删除哨位 '{SelectedItem.Name}' 吗？此操作无法撤销。");

        if (!confirmed)
            return;

        await ExecuteAsync(async () =>
        {
            await _positionService.DeleteAsync(SelectedItem.Id);
            RemoveItem(SelectedItem);
            SelectedItem = null;

            await _dialogService.ShowSuccessAsync("哨位已删除！");
        }, "正在删除...");
    }

    protected override void OnError(Exception exception)
    {
        base.OnError(exception);
        _ = _dialogService.ShowErrorAsync("操作失败", exception);
    }
}
