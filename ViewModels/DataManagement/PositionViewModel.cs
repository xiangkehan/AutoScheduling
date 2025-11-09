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
    private readonly IPersonnelService _personnelService;
    private readonly ISkillService _skillService;
    private readonly DialogService _dialogService;

    private bool _isEditing;
    private CreatePositionDto _newPosition = new();
    private UpdatePositionDto? _editingPosition;

    /// <summary>
    /// 对话框服务（公开以供代码后置使用）
    /// </summary>
    public DialogService DialogService => _dialogService;

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

    /// <summary>
    /// 所有人员列表（用于人员管理）
    /// </summary>
    public ObservableCollection<PersonnelDto> AllPersonnel { get; } = new();

    /// <summary>
    /// 当前哨位的可用人员列表
    /// </summary>
    public ObservableCollection<PersonnelDto> AvailablePersonnel { get; } = new();

    /// <summary>
    /// 选中的人员（用于添加/移除操作）
    /// </summary>
    private PersonnelDto? _selectedPersonnel;
    public PersonnelDto? SelectedPersonnel
    {
        get => _selectedPersonnel;
        set => SetProperty(ref _selectedPersonnel, value);
    }

    public PositionViewModel(
        IPositionService positionService,
        IPersonnelService personnelService,
        ISkillService skillService,
        DialogService dialogService)
    {
        _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
        _personnelService = personnelService ?? throw new ArgumentNullException(nameof(personnelService));
        _skillService = skillService ?? throw new ArgumentNullException(nameof(skillService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        CreateCommand = new AsyncRelayCommand(CreatePositionAsync);
        EditCommand = new AsyncRelayCommand(StartEditAsync, () => SelectedItem != null);
        SaveCommand = new AsyncRelayCommand(SavePositionAsync, () => IsEditing);
        CancelCommand = new RelayCommand(CancelEdit, () => IsEditing);
        DeleteCommand = new AsyncRelayCommand(DeletePositionAsync, () => SelectedItem != null);
        AddPersonnelCommand = new AsyncRelayCommand(AddPersonnelAsync, () => SelectedItem != null && SelectedPersonnel != null);
        RemovePersonnelCommand = new AsyncRelayCommand(RemovePersonnelAsync, () => SelectedItem != null && SelectedPersonnel != null);
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
    /// 添加人员命令
    /// </summary>
    public IAsyncRelayCommand AddPersonnelCommand { get; }

    /// <summary>
    /// 移除人员命令
    /// </summary>
    public IAsyncRelayCommand RemovePersonnelCommand { get; }

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

            // 加载所有人员
            var personnel = await _personnelService.GetAllAsync();
            AllPersonnel.Clear();
            foreach (var person in personnel)
            {
                AllPersonnel.Add(person);
            }

            // 更新当前选中哨位的可用人员
            UpdateAvailablePersonnel();
        }, "正在加载哨位数据...");
    }

    /// <summary>
    /// 创建哨位
    /// </summary>
    private async Task CreatePositionAsync()
    {
        try
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(NewPosition.Name))
            {
                await _dialogService.ShowErrorAsync("哨位名称不能为空");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPosition.Location))
            {
                await _dialogService.ShowErrorAsync("哨位地点不能为空");
                return;
            }

            if (NewPosition.RequiredSkillIds == null || NewPosition.RequiredSkillIds.Count == 0)
            {
                await _dialogService.ShowErrorAsync("至少需要选择一项所需技能");
                return;
            }

            await ExecuteAsync(async () =>
            {
                var created = await _positionService.CreateAsync(NewPosition);
                AddItem(created);

                // 自动选中新创建的项
                SelectedItem = created;

                // 重置表单
                NewPosition = new CreatePositionDto();

                await _dialogService.ShowSuccessAsync("哨位创建成功！");
            }, "正在创建哨位...");
        }
        catch (ArgumentException argEx)
        {
            // 验证错误
            await _dialogService.ShowErrorAsync("输入验证失败", argEx);
        }
        catch (InvalidOperationException invEx)
        {
            // 业务逻辑错误
            await _dialogService.ShowErrorAsync("操作失败：该哨位可能已存在或所选技能无效", invEx);
        }
        catch (Exception ex)
        {
            // 数据库或网络错误
            await _dialogService.ShowErrorAsync("创建哨位失败，请检查网络连接或稍后重试", ex);
        }
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
                RequiredSkillIds = new List<int>(SelectedItem.RequiredSkillIds),
                AvailablePersonnelIds = new List<int>(SelectedItem.AvailablePersonnelIds)
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

    /// <summary>
    /// 添加人员到当前哨位
    /// </summary>
    private async Task AddPersonnelAsync()
    {
        if (SelectedItem == null || SelectedPersonnel == null)
            return;

        await ExecuteAsync(async () =>
        {
            await _positionService.AddAvailablePersonnelAsync(SelectedItem.Id, SelectedPersonnel.Id);
            
            // 重新加载数据
            await LoadDataAsync();

            await _dialogService.ShowSuccessAsync($"已将人员 '{SelectedPersonnel.Name}' 添加到哨位 '{SelectedItem.Name}'");
        }, "正在添加人员...");
    }

    /// <summary>
    /// 从当前哨位移除人员
    /// </summary>
    private async Task RemovePersonnelAsync()
    {
        if (SelectedItem == null || SelectedPersonnel == null)
            return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "确认移除",
            $"确定要将人员 '{SelectedPersonnel.Name}' 从哨位 '{SelectedItem.Name}' 中移除吗？");

        if (!confirmed)
            return;

        await ExecuteAsync(async () =>
        {
            await _positionService.RemoveAvailablePersonnelAsync(SelectedItem.Id, SelectedPersonnel.Id);
            
            // 重新加载数据
            await LoadDataAsync();

            await _dialogService.ShowSuccessAsync($"已将人员 '{SelectedPersonnel.Name}' 从哨位 '{SelectedItem.Name}' 中移除");
        }, "正在移除人员...");
    }

    /// <summary>
    /// 更新当前哨位的可用人员列表
    /// </summary>
    private void UpdateAvailablePersonnel()
    {
        AvailablePersonnel.Clear();
        
        if (SelectedItem != null)
        {
            foreach (var person in AllPersonnel)
            {
                if (SelectedItem.AvailablePersonnelIds.Contains(person.Id))
                {
                    AvailablePersonnel.Add(person);
                }
            }
        }
    }

    /// <summary>
    /// 重写选中项变更处理，更新可用人员列表
    /// </summary>
    protected override void OnSelectedItemChanged(PositionDto? newItem)
    {
        base.OnSelectedItemChanged(newItem);
        UpdateAvailablePersonnel();
    }

    protected override void OnError(Exception exception)
    {
        base.OnError(exception);
        _ = _dialogService.ShowErrorAsync("操作失败", exception);
    }
}
