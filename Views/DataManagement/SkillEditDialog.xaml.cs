using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using AutoScheduling3.DTOs;
using System;

namespace AutoScheduling3.Views.DataManagement;

/// <summary>
/// 技能编辑对话框
/// </summary>
public sealed partial class SkillEditDialog : ContentDialog
{
    /// <summary>
    /// 要编辑的技能
    /// </summary>
    public SkillDto? Skill { get; set; }

    /// <summary>
    /// 编辑后的数据（用于返回）
    /// </summary>
    public UpdateSkillDto? EditedSkill { get; private set; }

    public SkillEditDialog()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// 对话框打开时的处理
    /// </summary>
    private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        // 设置无障碍属性
        NameTextBox.SetValue(AutomationProperties.NameProperty, "技能名称，必填");
        NameTextBox.SetValue(AutomationProperties.HelpTextProperty, "请输入技能名称，最多50个字符");

        DescriptionTextBox.SetValue(AutomationProperties.NameProperty, "技能描述");
        DescriptionTextBox.SetValue(AutomationProperties.HelpTextProperty, "请输入技能描述，可选，最多200个字符");

        // 预填充数据
        if (Skill != null)
        {
            NameTextBox.Text = Skill.Name;
            DescriptionTextBox.Text = Skill.Description ?? string.Empty;
        }

        // 自动聚焦到名称输入框
        NameTextBox.Focus(FocusState.Programmatic);
        
        // 初始验证状态
        UpdatePrimaryButtonState();
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
        ValidateDescriptionField();
        
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
            ShowFieldError(NameErrorText, "技能名称不能为空");
            return false;
        }
        
        if (name.Length > 50)
        {
            ShowFieldError(NameErrorText, "技能名称长度不能超过50字符");
            return false;
        }
        
        HideFieldError(NameErrorText);
        return true;
    }

    /// <summary>
    /// 验证描述字段
    /// </summary>
    private bool ValidateDescriptionField()
    {
        var description = DescriptionTextBox.Text?.Trim();
        
        if (!string.IsNullOrWhiteSpace(description) && description.Length > 200)
        {
            ShowFieldError(DescriptionErrorText, "技能描述长度不能超过200字符");
            return false;
        }
        
        HideFieldError(DescriptionErrorText);
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
        var isValid = ValidateNameField() && ValidateDescriptionField();
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
            var description = DescriptionTextBox.Text?.Trim();

            // 必填字段验证
            if (!ValidateNameField())
            {
                ShowValidationError("请修正技能名称错误");
                args.Cancel = true;
                return;
            }

            if (!ValidateDescriptionField())
            {
                ShowValidationError("请修正技能描述错误");
                args.Cancel = true;
                return;
            }

            // 创建编辑后的DTO
            EditedSkill = new UpdateSkillDto
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(description) ? null : description,
                IsActive = Skill?.IsActive ?? true // 保持原有的激活状态
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
