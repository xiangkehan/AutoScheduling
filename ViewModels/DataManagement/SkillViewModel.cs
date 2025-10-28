using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.ViewModels.Base;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;

namespace AutoScheduling3.ViewModels.DataManagement;

/// <summary>
/// 技能管理 ViewModel
/// </summary>
public partial class SkillViewModel : ListViewModelBase<SkillDto>
{
    private readonly ISkillService _skillService;
    private readonly DialogService _dialogService;

    private bool _isEditing;
    private CreateSkillDto _newSkill = new();
    private UpdateSkillDto? _editingSkill;

    /// <summary>
    /// 是否正在编辑模式
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    /// <summary>
    /// 新建技能DTO
    /// </summary>
    public CreateSkillDto NewSkill
    {
        get => _newSkill;
        set => SetProperty(ref _newSkill, value);
    }

    /// <summary>
    /// 编辑中的技能DTO
    /// </summary>
    public UpdateSkillDto? EditingSkill
    {
        get => _editingSkill;
        set => SetProperty(ref _editingSkill, value);
    }

    public SkillViewModel(
        ISkillService skillService,
        DialogService dialogService)
    {
        _skillService = skillService ?? throw new ArgumentNullException(nameof(skillService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        CreateCommand = new AsyncRelayCommand(CreateSkillAsync);
        EditCommand = new AsyncRelayCommand(StartEditAsync, () => SelectedItem != null);
        SaveCommand = new AsyncRelayCommand(SaveSkillAsync, () => IsEditing);
        CancelCommand = new RelayCommand(CancelEdit, () => IsEditing);
        DeleteCommand = new AsyncRelayCommand(DeleteSkillAsync, () => SelectedItem != null);
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
            var skills = string.IsNullOrWhiteSpace(SearchKeyword)
                ? await _skillService.GetAllAsync()
                : await _skillService.SearchAsync(SearchKeyword);

            ClearItems();
            AddRange(skills);
        }, "正在加载技能数据...");
    }

    /// <summary>
    /// 创建技能
    /// </summary>
    private async Task CreateSkillAsync()
    {
        await ExecuteAsync(async () =>
        {
            var created = await _skillService.CreateAsync(NewSkill);
            AddItem(created);

            // 重置表单
            NewSkill = new CreateSkillDto();

            await _dialogService.ShowSuccessAsync("技能创建成功！");
        }, "正在创建技能...");
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
            EditingSkill = new UpdateSkillDto
            {
                Name = SelectedItem.Name,
                Description = SelectedItem.Description,
                IsActive = SelectedItem.IsActive
            };

            IsEditing = true;
            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// 保存编辑
    /// </summary>
    private async Task SaveSkillAsync()
    {
        if (SelectedItem == null || EditingSkill == null)
            return;

        await ExecuteAsync(async () =>
        {
            await _skillService.UpdateAsync(SelectedItem.Id, EditingSkill);
            
            // 重新加载数据
            await LoadDataAsync();

            IsEditing = false;
            EditingSkill = null;

            await _dialogService.ShowSuccessAsync("技能信息已更新！");
        }, "正在保存...");
    }

    /// <summary>
    /// 取消编辑
    /// </summary>
    private void CancelEdit()
    {
        IsEditing = false;
        EditingSkill = null;
    }

    /// <summary>
    /// 删除技能
    /// </summary>
    private async Task DeleteSkillAsync()
    {
        if (SelectedItem == null)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "确认删除",
            $"确定要删除技能 '{SelectedItem.Name}' 吗？此操作无法撤销。");

        if (!confirmed)
            return;

        await ExecuteAsync(async () =>
        {
            await _skillService.DeleteAsync(SelectedItem.Id);
            RemoveItem(SelectedItem);
            SelectedItem = null;

            await _dialogService.ShowSuccessAsync("技能已删除！");
        }, "正在删除...");
    }

    protected override void OnError(Exception exception)
    {
        base.OnError(exception);
        _ = _dialogService.ShowErrorAsync("操作失败", exception);
    }
}
