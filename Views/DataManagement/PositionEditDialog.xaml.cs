using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using AutoScheduling3.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.Views.DataManagement;

/// <summary>
/// 哨位编辑对话框
/// </summary>
public sealed partial class PositionEditDialog : ContentDialog
{
    /// <summary>
    /// 要编辑的哨位
    /// </summary>
    public PositionDto? Position { get; set; }

    /// <summary>
    /// 可选技能列表
    /// </summary>
    public IEnumerable<SkillDto>? AvailableSkills { get; set; }

    /// <summary>
    /// 编辑后的数据（用于返回）
    /// </summary>
    public UpdatePositionDto? EditedPosition { get; private set; }

    public PositionEditDialog()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// 对话框打开时的处理
    /// </summary>
    private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        // 设置无障碍属性
        NameTextBox.SetValue(AutomationProperties.NameProperty, "哨位名称，必填");
        NameTextBox.SetValue(AutomationProperties.HelpTextProperty, "请输入哨位名称，最多100个字符");

        LocationTextBox.SetValue(AutomationProperties.NameProperty, "地点，必填");
        LocationTextBox.SetValue(AutomationProperties.HelpTextProperty, "请输入哨位地点，最多200个字符");

        DescriptionTextBox.SetValue(AutomationProperties.NameProperty, "介绍");
        DescriptionTextBox.SetValue(AutomationProperties.HelpTextProperty, "请输入哨位介绍，可选，最多500个字符");

        SkillsListView.SetValue(AutomationProperties.NameProperty, "所需技能列表，可选，支持多选");
        SkillsListView.SetValue(AutomationProperties.HelpTextProperty, "使用空格键选择或取消选择技能，Tab键移动到下一个控件");

        // 加载可选技能
        if (AvailableSkills != null)
        {
            SkillsListView.ItemsSource = AvailableSkills;
        }

        // 预填充数据
        if (Position != null)
        {
            NameTextBox.Text = Position.Name;
            LocationTextBox.Text = Position.Location;
            DescriptionTextBox.Text = Position.Description ?? string.Empty;

            // 选中当前哨位的技能
            if (AvailableSkills != null && Position.RequiredSkillIds.Count > 0)
            {
                foreach (var skill in AvailableSkills)
                {
                    if (Position.RequiredSkillIds.Contains(skill.Id))
                    {
                        SkillsListView.SelectedItems.Add(skill);
                    }
                }
            }

            // 更新技能计数
            UpdateSkillCount();
        }

        // 自动聚焦到名称输入框
        NameTextBox.Focus(FocusState.Programmatic);
        
        // 初始验证状态
        UpdatePrimaryButtonState();
    }

    /// <summary>
    /// 技能选择变更时更新计数
    /// </summary>
    private void SkillsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSkillCount();
    }

    /// <summary>
    /// 更新技能选择计数显示
    /// </summary>
    private void UpdateSkillCount()
    {
        SkillCountText.Text = $"已选择: {SkillsListView.SelectedItems.Count}";
    }

    /// <summary>
    /// 输入验证（实时）
    /// </summary>
    private void ValidateInput(object sender, TextChangedEventArgs e)
    {
        // 清除之前的错误提示
        ValidationErrorBar.IsOpen = false;

        // 实时验证各个字段
        ValidateNameField();
        ValidateLocationField();
        
        // 更新保存按钮状态
        UpdatePrimaryButtonState();
    }

    /// <summary>
    /// 验证名称字段
    /// </summary>
    private bool ValidateNameField()
    {
        var name = NameTextBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowFieldError(NameErrorText, "哨位名称不能为空");
            return false;
        }
        
        if (name.Length > 100)
        {
            ShowFieldError(NameErrorText, "哨位名称长度不能超过100字符");
            return false;
        }
        
        HideFieldError(NameErrorText);
        return true;
    }

    /// <summary>
    /// 验证地点字段
    /// </summary>
    private bool ValidateLocationField()
    {
        var location = LocationTextBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(location))
        {
            ShowFieldError(LocationErrorText, "地点不能为空");
            return false;
        }
        
        if (location.Length > 200)
        {
            ShowFieldError(LocationErrorText, "地点长度不能超过200字符");
            return false;
        }
        
        HideFieldError(LocationErrorText);
        return true;
    }

    /// <summary>
    /// 显示字段错误
    /// </summary>
    private void ShowFieldError(TextBlock errorText, string message)
    {
        errorText.Text = message;
        errorText.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// 隐藏字段错误
    /// </summary>
    private void HideFieldError(TextBlock errorText)
    {
        errorText.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// 更新保存按钮状态
    /// </summary>
    private void UpdatePrimaryButtonState()
    {
        // 只有所有必填字段都有效时才启用保存按钮
        var isValid = ValidateNameField() && ValidateLocationField();
        IsPrimaryButtonEnabled = isValid;
    }

    /// <summary>
    /// 主按钮点击时的验证
    /// </summary>
    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();

        try
        {
            // 验证输入
            var name = NameTextBox.Text?.Trim();
            var location = LocationTextBox.Text?.Trim();
            var description = DescriptionTextBox.Text?.Trim();

            // 必填字段验证
            if (!ValidateNameField())
            {
                ShowValidationError("请修正哨位名称错误");
                args.Cancel = true;
                return;
            }

            if (!ValidateLocationField())
            {
                ShowValidationError("请修正地点错误");
                args.Cancel = true;
                return;
            }

            if (!string.IsNullOrWhiteSpace(description) && description.Length > 500)
            {
                ShowValidationError("介绍长度不能超过500字符");
                args.Cancel = true;
                return;
            }

            // 收集选中的技能ID
            var selectedSkillIds = new List<int>();
            foreach (var item in SkillsListView.SelectedItems)
            {
                if (item is SkillDto skill)
                {
                    selectedSkillIds.Add(skill.Id);
                }
            }

            // 创建编辑后的DTO
            EditedPosition = new UpdatePositionDto
            {
                Name = name,
                Location = location,
                Description = string.IsNullOrWhiteSpace(description) ? null : description,
                Requirements = Position?.Requirements, // 保持原有的要求说明
                RequiredSkillIds = selectedSkillIds,
                AvailablePersonnelIds = Position?.AvailablePersonnelIds ?? new List<int>() // 保持原有的人员列表
            };
        }
        catch (Exception ex)
        {
            ShowValidationError($"验证失败: {ex.Message}");
            args.Cancel = true;
        }
        finally
        {
            deferral.Complete();
        }
    }

    /// <summary>
    /// 显示验证错误
    /// </summary>
    private void ShowValidationError(string message)
    {
        ValidationErrorBar.Message = message;
        ValidationErrorBar.IsOpen = true;
    }
}
