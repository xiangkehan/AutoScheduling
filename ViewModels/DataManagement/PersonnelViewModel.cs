using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.ViewModels.Base;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.ViewModels.DataManagement;

/// <summary>
/// 人员管理 ViewModel
/// </summary>
public partial class PersonnelViewModel : ListViewModelBase<PersonnelDto>
{
    private readonly IPersonnelService _personnelService;
    private readonly ISkillService _skillService;
    private readonly DialogService _dialogService;

    private bool _isEditing;
    private CreatePersonnelDto _newPersonnel = new();
    private UpdatePersonnelDto? _editingPersonnel;
    private UpdatePersonnelDto? _originalPersonnel; // 保存原始数据用于取消操作
    private bool _isNameValid = false;
    private bool _areSkillsValid = false;
    private string _nameValidationMessage = string.Empty;
    private string _skillsValidationMessage = string.Empty;

    /// <summary>
    /// 是否正在编辑模式
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    /// <summary>
    /// 姓名是否有效
    /// </summary>
    public bool IsNameValid
    {
        get => _isNameValid;
        set => SetProperty(ref _isNameValid, value);
    }

    /// <summary>
    /// 技能选择是否有效
    /// </summary>
    public bool AreSkillsValid
    {
        get => _areSkillsValid;
        set => SetProperty(ref _areSkillsValid, value);
    }

    /// <summary>
    /// 姓名验证消息
    /// </summary>
    public string NameValidationMessage
    {
        get => _nameValidationMessage;
        set => SetProperty(ref _nameValidationMessage, value);
    }

    /// <summary>
    /// 技能验证消息
    /// </summary>
    public string SkillsValidationMessage
    {
        get => _skillsValidationMessage;
        set => SetProperty(ref _skillsValidationMessage, value);
    }

    /// <summary>
    /// 是否可以创建（所有验证通过）
    /// </summary>
    public bool CanCreate => IsNameValid && AreSkillsValid;

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
    /// 可选技能列表
    /// </summary>
    public ObservableCollection<SkillDto> AvailableSkills { get; } = new();

    public PersonnelViewModel(
        IPersonnelService personnelService,
        ISkillService skillService,
        DialogService dialogService)
    {
        _personnelService = personnelService ?? throw new ArgumentNullException(nameof(personnelService));
        _skillService = skillService ?? throw new ArgumentNullException(nameof(skillService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        CreateCommand = new AsyncRelayCommand(CreatePersonnelAsync, () => CanCreate);
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

            // 加载可选技能
            var skills = await _skillService.GetAllAsync();
            AvailableSkills.Clear();
            foreach (var skill in skills)
            {
                AvailableSkills.Add(skill);
            }
        }, "正在加载人员数据...");
    }

    /// <summary>
    /// 验证姓名
    /// </summary>
    public void ValidateName()
    {
        if (string.IsNullOrWhiteSpace(NewPersonnel.Name))
        {
            IsNameValid = false;
            NameValidationMessage = "姓名不能为空";
        }
        else if (NewPersonnel.Name.Length > 50)
        {
            IsNameValid = false;
            NameValidationMessage = "姓名长度不能超过50字符";
        }
        else
        {
            IsNameValid = true;
            NameValidationMessage = string.Empty;
        }

        CreateCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 验证技能选择
    /// </summary>
    public void ValidateSkills()
    {
        if (NewPersonnel.SkillIds == null || NewPersonnel.SkillIds.Count == 0)
        {
            AreSkillsValid = false;
            SkillsValidationMessage = "至少需要选择一项技能";
        }
        else
        {
            AreSkillsValid = true;
            SkillsValidationMessage = string.Empty;
        }

        CreateCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 重置创建表单
    /// </summary>
    public void ResetCreateForm()
    {
        // 创建新的DTO实例
        NewPersonnel = new CreatePersonnelDto
        {
            IsAvailable = true,
            RecentPeriodShiftIntervals = new int[12]
        };

        // 重置验证状态
        IsNameValid = false;
        AreSkillsValid = false;
        NameValidationMessage = string.Empty;
        SkillsValidationMessage = string.Empty;

        // 通知命令状态更新
        CreateCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 创建人员
    /// </summary>
    private async Task CreatePersonnelAsync()
    {
        try
        {
            // 验证输入
            if (!IsNameValid || !AreSkillsValid)
            {
                await _dialogService.ShowErrorAsync("请修正表单中的验证错误后再提交");
                return;
            }

            await ExecuteAsync(async () =>
            {
                var created = await _personnelService.CreateAsync(NewPersonnel);
                AddItem(created);

                // 自动选中新创建的项
                SelectedItem = created;

                // 重置表单
                ResetCreateForm();

                // 根据需求8.5，成功操作不显示提示消息
            }, "正在创建人员...");
        }
        catch (ArgumentException argEx)
        {
            // 验证错误
            await _dialogService.ShowErrorAsync("输入验证失败", argEx);
        }
        catch (InvalidOperationException invEx)
        {
            // 业务逻辑错误
            await _dialogService.ShowErrorAsync("操作失败：该人员可能已存在或数据不一致", invEx);
        }
        catch (Exception ex)
        {
            // 数据库或网络错误
            await _dialogService.ShowErrorAsync("创建人员失败，请检查网络连接或稍后重试", ex);
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
            // 将选中的人员数据转换为编辑DTO
            EditingPersonnel = new UpdatePersonnelDto
            {
                Name = SelectedItem.Name,
                SkillIds = new List<int>(SelectedItem.SkillIds),
                IsAvailable = SelectedItem.IsAvailable,
                IsRetired = SelectedItem.IsRetired
            };

            // 保存原始数据的副本用于取消操作
            _originalPersonnel = new UpdatePersonnelDto
            {
                Name = SelectedItem.Name,
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

        // 验证编辑的数据
        if (string.IsNullOrWhiteSpace(EditingPersonnel.Name))
        {
            await _dialogService.ShowErrorAsync("姓名不能为空");
            return;
        }

        if (EditingPersonnel.SkillIds == null || EditingPersonnel.SkillIds.Count == 0)
        {
            await _dialogService.ShowErrorAsync("至少需要选择一项技能");
            return;
        }

        var selectedId = SelectedItem.Id;

        await ExecuteAsync(async () =>
        {
            await _personnelService.UpdateAsync(selectedId, EditingPersonnel);
            
            // 重新加载数据
            await LoadDataAsync();

            // 重新选中更新后的项
            SelectedItem = Items.FirstOrDefault(p => p.Id == selectedId);

            IsEditing = false;
            EditingPersonnel = null;
            _originalPersonnel = null;

            // 根据需求8.5，成功操作不显示提示消息
            // await _dialogService.ShowSuccessAsync("人员信息已更新！");
        }, "正在保存...");
    }

    /// <summary>
    /// 取消编辑
    /// </summary>
    private void CancelEdit()
    {
        // 恢复原始数据
        if (_originalPersonnel != null && EditingPersonnel != null)
        {
            EditingPersonnel.Name = _originalPersonnel.Name;
            EditingPersonnel.SkillIds = new List<int>(_originalPersonnel.SkillIds);
            EditingPersonnel.IsAvailable = _originalPersonnel.IsAvailable;
            EditingPersonnel.IsRetired = _originalPersonnel.IsRetired;
        }

        IsEditing = false;
        EditingPersonnel = null;
        _originalPersonnel = null;
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

            // 根据需求8.5，成功操作不显示提示消息
        }, "正在删除...");
    }

    protected override void OnError(Exception exception)
    {
        base.OnError(exception);
        _ = _dialogService.ShowErrorAsync("操作失败", exception);
    }
}
