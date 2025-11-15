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
    private readonly IConstraintService _constraintService;
    private readonly IPositionService _positionService;
    private readonly DialogService _dialogService;

    private bool _isEditing;
    private CreatePersonnelDto _newPersonnel = new();
    private UpdatePersonnelDto? _editingPersonnel;
    private UpdatePersonnelDto? _originalPersonnel; // 保存原始数据用于取消操作
    private bool _isNameValid = false;
    private bool _areSkillsValid = true; // 允许没有技能，初始为true
    private string _nameValidationMessage = string.Empty;
    private string _skillsValidationMessage = string.Empty;
    private bool _isCreatingRule = false;
    private CreateFixedAssignmentDto _newFixedPositionRule = new();
    private bool _isEditingRule = false;
    private UpdateFixedAssignmentDto? _editingFixedPositionRule;
    private FixedAssignmentDto? _selectedRule;
    private FixedAssignmentDto? _originalRuleData; // 保存原始规则数据用于取消操作
    
    // 定岗规则验证相关字段
    private string _ruleValidationMessage = string.Empty;
    private bool _isRuleValid = false;

    /// <summary>
    /// 是否正在编辑模式
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value))
            {
                // 通知依赖于 IsEditing 的命令更新状态
                SaveCommand?.NotifyCanExecuteChanged();
                CancelCommand?.NotifyCanExecuteChanged();
            }
        }
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

    /// <summary>
    /// 定岗规则集合
    /// </summary>
    public ObservableCollection<FixedAssignmentDto> FixedPositionRules { get; } = new();

    /// <summary>
    /// 可选哨位列表
    /// </summary>
    public ObservableCollection<PositionDto> AvailablePositions { get; } = new();

    /// <summary>
    /// 是否正在创建规则
    /// </summary>
    public bool IsCreatingRule
    {
        get => _isCreatingRule;
        set => SetProperty(ref _isCreatingRule, value);
    }

    /// <summary>
    /// 新建定岗规则DTO
    /// </summary>
    public CreateFixedAssignmentDto NewFixedPositionRule
    {
        get => _newFixedPositionRule;
        set => SetProperty(ref _newFixedPositionRule, value);
    }

    /// <summary>
    /// 是否正在编辑规则
    /// </summary>
    public bool IsEditingRule
    {
        get => _isEditingRule;
        set => SetProperty(ref _isEditingRule, value);
    }

    /// <summary>
    /// 编辑中的定岗规则DTO
    /// </summary>
    public UpdateFixedAssignmentDto? EditingFixedPositionRule
    {
        get => _editingFixedPositionRule;
        set => SetProperty(ref _editingFixedPositionRule, value);
    }

    /// <summary>
    /// 选中的规则
    /// </summary>
    public FixedAssignmentDto? SelectedRule
    {
        get => _selectedRule;
        set => SetProperty(ref _selectedRule, value);
    }

    /// <summary>
    /// 规则验证消息
    /// </summary>
    public string RuleValidationMessage
    {
        get => _ruleValidationMessage;
        set => SetProperty(ref _ruleValidationMessage, value);
    }

    /// <summary>
    /// 规则是否有效
    /// </summary>
    public bool IsRuleValid
    {
        get => _isRuleValid;
        set => SetProperty(ref _isRuleValid, value);
    }

    public PersonnelViewModel(
        IPersonnelService personnelService,
        ISkillService skillService,
        IConstraintService constraintService,
        IPositionService positionService,
        DialogService dialogService)
    {
        _personnelService = personnelService ?? throw new ArgumentNullException(nameof(personnelService));
        _skillService = skillService ?? throw new ArgumentNullException(nameof(skillService));
        _constraintService = constraintService ?? throw new ArgumentNullException(nameof(constraintService));
        _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        CreateCommand = new AsyncRelayCommand(CreatePersonnelAsync, () => CanCreate);
        EditCommand = new AsyncRelayCommand(StartEditAsync, () => SelectedItem != null);
        SaveCommand = new AsyncRelayCommand(SavePersonnelAsync, () => IsEditing);
        CancelCommand = new RelayCommand(CancelEdit, () => IsEditing);
        DeleteCommand = new AsyncRelayCommand(DeletePersonnelAsync, () => SelectedItem != null);
        LoadFixedPositionRulesCommand = new AsyncRelayCommand(LoadFixedPositionRulesAsync);
        CreateFixedPositionRuleCommand = new AsyncRelayCommand(CreateFixedPositionRuleAsync);
        StartEditRuleCommand = new AsyncRelayCommand<FixedAssignmentDto>(StartEditRuleAsync);
        SaveFixedPositionRuleCommand = new AsyncRelayCommand(SaveFixedPositionRuleAsync);
        CancelRuleEditCommand = new RelayCommand(CancelRuleEdit);
        DeleteFixedPositionRuleCommand = new AsyncRelayCommand<FixedAssignmentDto>(DeleteFixedPositionRuleAsync);
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
    /// 加载定岗规则命令
    /// </summary>
    public IAsyncRelayCommand LoadFixedPositionRulesCommand { get; }

    /// <summary>
    /// 创建定岗规则命令
    /// </summary>
    public IAsyncRelayCommand CreateFixedPositionRuleCommand { get; }

    /// <summary>
    /// 开始编辑规则命令
    /// </summary>
    public IAsyncRelayCommand<FixedAssignmentDto> StartEditRuleCommand { get; }

    /// <summary>
    /// 保存定岗规则编辑命令
    /// </summary>
    public IAsyncRelayCommand SaveFixedPositionRuleCommand { get; }

    /// <summary>
    /// 取消规则编辑命令
    /// </summary>
    public IRelayCommand CancelRuleEditCommand { get; }

    /// <summary>
    /// 删除定岗规则命令
    /// </summary>
    public IAsyncRelayCommand<FixedAssignmentDto> DeleteFixedPositionRuleCommand { get; }

    /// <summary>
    /// 重写选中项变更处理，通知命令状态更新
    /// </summary>
    protected override void OnSelectedItemChanged(PersonnelDto? newItem)
    {
        base.OnSelectedItemChanged(newItem);
        
        // 通知所有依赖于SelectedItem的命令状态更新
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();

        // 加载选中人员的定岗规则
        _ = LoadFixedPositionRulesCommand.ExecuteAsync(null);
    }

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

            // 加载可选哨位
            var positions = await _positionService.GetAllAsync();
            AvailablePositions.Clear();
            foreach (var position in positions)
            {
                AvailablePositions.Add(position);
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
        // 允许人员没有技能
        AreSkillsValid = true;
        SkillsValidationMessage = string.Empty;

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
        AreSkillsValid = true; // 允许没有技能
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

        // 允许人员没有技能

        var selectedId = SelectedItem.Id;

        await ExecuteAsync(async () =>
        {
            await _personnelService.UpdateAsync(selectedId, EditingPersonnel);
            
            // 增量更新UI - 直接更新SelectedItem的属性
            SelectedItem.Name = EditingPersonnel.Name;
            SelectedItem.IsAvailable = EditingPersonnel.IsAvailable;
            SelectedItem.IsRetired = EditingPersonnel.IsRetired;
            // IsActive 是计算属性，会自动更新
            
            // 更新技能ID列表
            SelectedItem.SkillIds.Clear();
            foreach (var skillId in EditingPersonnel.SkillIds)
            {
                SelectedItem.SkillIds.Add(skillId);
            }
            
            // 更新技能名称列表
            SelectedItem.SkillNames.Clear();
            foreach (var skillId in EditingPersonnel.SkillIds)
            {
                var skill = AvailableSkills.FirstOrDefault(s => s.Id == skillId);
                if (skill != null)
                {
                    SelectedItem.SkillNames.Add(skill.Name);
                }
            }
            
            // 触发属性变更通知以更新UI
            SelectedItem.OnPropertyChanged(nameof(SelectedItem.Name));
            SelectedItem.OnPropertyChanged(nameof(SelectedItem.IsAvailable));
            SelectedItem.OnPropertyChanged(nameof(SelectedItem.IsRetired));
            SelectedItem.OnPropertyChanged(nameof(SelectedItem.IsActive));
            SelectedItem.OnPropertyChanged(nameof(SelectedItem.SkillNames));

            IsEditing = false;
            EditingPersonnel = null;
            _originalPersonnel = null;

            // 根据需求8.5，成功操作不显示提示消息
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

    /// <summary>
    /// 加载定岗规则列表
    /// </summary>
    private async Task LoadFixedPositionRulesAsync()
    {
        // 如果没有选中人员，清空规则列表
        if (SelectedItem == null)
        {
            FixedPositionRules.Clear();
            return;
        }

        try
        {
            // 获取选中人员的定岗规则
            var rules = await _constraintService.GetFixedAssignmentDtosByPersonAsync(SelectedItem.Id);
            
            // 更新规则集合
            FixedPositionRules.Clear();
            foreach (var rule in rules)
            {
                FixedPositionRules.Add(rule);
            }
        }
        catch (ArgumentException argEx)
        {
            // 验证错误 - 人员ID无效
            await _dialogService.ShowErrorAsync("加载定岗规则失败：人员ID无效", argEx);
        }
        catch (InvalidOperationException invEx)
        {
            // 业务逻辑错误 - 数据不一致
            await _dialogService.ShowErrorAsync("加载定岗规则失败：数据不一致或人员不存在", invEx);
        }
        catch (Exception ex)
        {
            // 数据库或网络错误
            await _dialogService.ShowErrorAsync("加载定岗规则失败，请检查网络连接或稍后重试", ex);
        }
    }

    /// <summary>
    /// 创建定岗规则
    /// </summary>
    private async Task CreateFixedPositionRuleAsync()
    {
        // 验证是否选中了人员
        if (SelectedItem == null)
        {
            await _dialogService.ShowErrorAsync("请先选择一个人员");
            return;
        }

        // 如果已经在创建模式，则提交表单
        if (IsCreatingRule)
        {
            try
            {
                // 验证表单输入
                if (!ValidateCreateRuleForm())
                {
                    await _dialogService.ShowErrorAsync(RuleValidationMessage);
                    return;
                }

                // 设置人员ID
                NewFixedPositionRule.PersonnelId = SelectedItem.Id;

                await ExecuteAsync(async () =>
                {
                    // 调用ConstraintService创建规则
                    await _constraintService.CreateFixedAssignmentAsync(NewFixedPositionRule);

                    // 创建成功后刷新规则列表
                    await LoadFixedPositionRulesAsync();

                    // 重置表单并关闭创建界面
                    ResetFixedPositionRuleForm();
                    IsCreatingRule = false;

                }, "正在创建定岗规则...");
            }
            catch (ArgumentException argEx)
            {
                // 验证错误
                await _dialogService.ShowErrorAsync("输入验证失败", argEx);
            }
            catch (InvalidOperationException invEx)
            {
                // 业务逻辑错误
                await _dialogService.ShowErrorAsync("操作失败：规则可能已存在或数据不一致", invEx);
            }
            catch (Exception ex)
            {
                // 数据库或网络错误
                await _dialogService.ShowErrorAsync("创建定岗规则失败，请检查网络连接或稍后重试", ex);
            }
        }
        else
        {
            // 进入创建模式，显示表单
            ResetFixedPositionRuleForm();
            IsCreatingRule = true;
        }
    }

    /// <summary>
    /// 重置定岗规则创建表单
    /// </summary>
    public void ResetFixedPositionRuleForm()
    {
        NewFixedPositionRule = new CreateFixedAssignmentDto
        {
            PersonnelId = SelectedItem?.Id ?? 0,
            AllowedPositionIds = new List<int>(),
            AllowedTimeSlots = new List<int>(),
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddYears(1),
            IsEnabled = true,
            RuleName = string.Empty,
            Description = string.Empty
        };
        
        // 重置验证状态
        RuleValidationMessage = string.Empty;
        IsRuleValid = false;
    }

    /// <summary>
    /// 验证定岗规则表单（创建）
    /// </summary>
    public bool ValidateCreateRuleForm()
    {
        // 验证至少选择一个哨位或时段
        if ((NewFixedPositionRule.AllowedPositionIds == null || NewFixedPositionRule.AllowedPositionIds.Count == 0) &&
            (NewFixedPositionRule.AllowedTimeSlots == null || NewFixedPositionRule.AllowedTimeSlots.Count == 0))
        {
            RuleValidationMessage = "请至少选择一个哨位或一个时段";
            IsRuleValid = false;
            return false;
        }

        // 验证规则描述长度
        if (!string.IsNullOrEmpty(NewFixedPositionRule.Description) && NewFixedPositionRule.Description.Length > 500)
        {
            RuleValidationMessage = "规则描述长度不能超过500字符";
            IsRuleValid = false;
            return false;
        }

        // 验证时段索引范围
        if (NewFixedPositionRule.AllowedTimeSlots != null)
        {
            foreach (var timeSlot in NewFixedPositionRule.AllowedTimeSlots)
            {
                if (timeSlot < 0 || timeSlot > 11)
                {
                    RuleValidationMessage = "时段索引必须在0-11范围内";
                    IsRuleValid = false;
                    return false;
                }
            }
        }

        // 所有验证通过
        RuleValidationMessage = string.Empty;
        IsRuleValid = true;
        return true;
    }

    /// <summary>
    /// 验证定岗规则表单（编辑）
    /// </summary>
    public bool ValidateEditRuleForm()
    {
        if (EditingFixedPositionRule == null)
        {
            RuleValidationMessage = "编辑数据无效";
            IsRuleValid = false;
            return false;
        }

        // 验证至少选择一个哨位或时段
        if ((EditingFixedPositionRule.AllowedPositionIds == null || EditingFixedPositionRule.AllowedPositionIds.Count == 0) &&
            (EditingFixedPositionRule.AllowedTimeSlots == null || EditingFixedPositionRule.AllowedTimeSlots.Count == 0))
        {
            RuleValidationMessage = "请至少选择一个哨位或一个时段";
            IsRuleValid = false;
            return false;
        }

        // 验证规则名称
        if (string.IsNullOrWhiteSpace(EditingFixedPositionRule.RuleName))
        {
            RuleValidationMessage = "规则名称不能为空";
            IsRuleValid = false;
            return false;
        }

        // 验证规则描述长度
        if (!string.IsNullOrEmpty(EditingFixedPositionRule.Description) && EditingFixedPositionRule.Description.Length > 500)
        {
            RuleValidationMessage = "规则描述长度不能超过500字符";
            IsRuleValid = false;
            return false;
        }

        // 验证时段索引范围
        if (EditingFixedPositionRule.AllowedTimeSlots != null)
        {
            foreach (var timeSlot in EditingFixedPositionRule.AllowedTimeSlots)
            {
                if (timeSlot < 0 || timeSlot > 11)
                {
                    RuleValidationMessage = "时段索引必须在0-11范围内";
                    IsRuleValid = false;
                    return false;
                }
            }
        }

        // 所有验证通过
        RuleValidationMessage = string.Empty;
        IsRuleValid = true;
        return true;
    }

    /// <summary>
    /// 开始编辑规则
    /// </summary>
    private async Task StartEditRuleAsync(FixedAssignmentDto? rule)
    {
        if (rule == null)
            return;

        await ExecuteAsync(async () =>
        {
            // 保存选中的规则
            SelectedRule = rule;

            // 保存原始数据副本用于取消操作
            _originalRuleData = new FixedAssignmentDto
            {
                Id = rule.Id,
                PersonnelId = rule.PersonnelId,
                PersonnelName = rule.PersonnelName,
                AllowedPositionIds = new List<int>(rule.AllowedPositionIds),
                AllowedPositionNames = new List<string>(rule.AllowedPositionNames),
                AllowedTimeSlots = new List<int>(rule.AllowedTimeSlots),
                StartDate = rule.StartDate,
                EndDate = rule.EndDate,
                IsEnabled = rule.IsEnabled,
                RuleName = rule.RuleName,
                Description = rule.Description,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt
            };

            // 将选中规则数据复制到EditingFixedPositionRule
            EditingFixedPositionRule = new UpdateFixedAssignmentDto
            {
                PersonnelId = rule.PersonnelId,
                AllowedPositionIds = new List<int>(rule.AllowedPositionIds),
                AllowedTimeSlots = new List<int>(rule.AllowedTimeSlots),
                StartDate = rule.StartDate,
                EndDate = rule.EndDate,
                IsEnabled = rule.IsEnabled,
                RuleName = rule.RuleName,
                Description = rule.Description
            };

            IsEditingRule = true;
            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// 保存定岗规则编辑
    /// </summary>
    private async Task SaveFixedPositionRuleAsync()
    {
        if (SelectedRule == null || EditingFixedPositionRule == null)
            return;

        try
        {
            // 验证表单输入
            if (!ValidateEditRuleForm())
            {
                await _dialogService.ShowErrorAsync(RuleValidationMessage);
                return;
            }

            var ruleId = SelectedRule.Id;

            await ExecuteAsync(async () =>
            {
                // 调用ConstraintService更新规则
                await _constraintService.UpdateFixedAssignmentAsync(ruleId, EditingFixedPositionRule);

                // 更新成功后刷新规则列表
                await LoadFixedPositionRulesAsync();

                // 关闭编辑界面
                IsEditingRule = false;
                EditingFixedPositionRule = null;
                SelectedRule = null;
                _originalRuleData = null;

            }, "正在保存定岗规则...");
        }
        catch (ArgumentException argEx)
        {
            // 验证错误
            await _dialogService.ShowErrorAsync("输入验证失败", argEx);
        }
        catch (InvalidOperationException invEx)
        {
            // 业务逻辑错误
            await _dialogService.ShowErrorAsync("操作失败：规则可能不存在或数据不一致", invEx);
        }
        catch (Exception ex)
        {
            // 数据库或网络错误
            await _dialogService.ShowErrorAsync("更新定岗规则失败，请检查网络连接或稍后重试", ex);
        }
    }

    /// <summary>
    /// 取消规则编辑
    /// </summary>
    private void CancelRuleEdit()
    {
        // 恢复原始数据
        if (_originalRuleData != null && SelectedRule != null)
        {
            SelectedRule.AllowedPositionIds = new List<int>(_originalRuleData.AllowedPositionIds);
            SelectedRule.AllowedPositionNames = new List<string>(_originalRuleData.AllowedPositionNames);
            SelectedRule.AllowedTimeSlots = new List<int>(_originalRuleData.AllowedTimeSlots);
            SelectedRule.StartDate = _originalRuleData.StartDate;
            SelectedRule.EndDate = _originalRuleData.EndDate;
            SelectedRule.IsEnabled = _originalRuleData.IsEnabled;
            SelectedRule.RuleName = _originalRuleData.RuleName;
            SelectedRule.Description = _originalRuleData.Description;
        }

        IsEditingRule = false;
        EditingFixedPositionRule = null;
        SelectedRule = null;
        _originalRuleData = null;
    }

    /// <summary>
    /// 删除定岗规则
    /// </summary>
    private async Task DeleteFixedPositionRuleAsync(FixedAssignmentDto? rule)
    {
        if (rule == null)
            return;

        // 显示确认对话框
        var confirmed = await _dialogService.ShowConfirmAsync(
            "确认删除",
            "确定要删除此定岗规则吗？此操作无法撤销。");

        if (!confirmed)
            return;

        try
        {
            await ExecuteAsync(async () =>
            {
                // 调用ConstraintService删除规则
                await _constraintService.DeleteFixedPositionRuleAsync(rule.Id);

                // 删除成功后从FixedPositionRules集合中移除该规则
                FixedPositionRules.Remove(rule);

            }, "正在删除定岗规则...");
        }
        catch (ArgumentException argEx)
        {
            // 验证错误
            await _dialogService.ShowErrorAsync("删除失败：规则ID无效", argEx);
        }
        catch (InvalidOperationException invEx)
        {
            // 业务逻辑错误
            await _dialogService.ShowErrorAsync("删除失败：规则可能不存在或已被删除", invEx);
        }
        catch (Exception ex)
        {
            // 数据库或网络错误
            await _dialogService.ShowErrorAsync("删除定岗规则失败，请检查网络连接或稍后重试", ex);
        }
    }

    protected override void OnError(Exception exception)
    {
        base.OnError(exception);
        _ = _dialogService.ShowErrorAsync("操作失败", exception);
    }
}
