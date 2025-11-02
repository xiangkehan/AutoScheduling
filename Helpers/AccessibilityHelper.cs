using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 无障碍辅助类 - 提供无障碍功能支持
/// </summary>
public static class AccessibilityHelper
{
    /// <summary>
    /// 设置元素的无障碍属性
    /// </summary>
    public static void SetAccessibilityProperties(FrameworkElement element, string name, string? helpText = null, AutomationControlType? controlType = null)
    {
        AutomationProperties.SetName(element, name);
        
        if (!string.IsNullOrEmpty(helpText))
        {
            AutomationProperties.SetHelpText(element, helpText);
        }

        if (controlType.HasValue)
        {
            AutomationProperties.SetAutomationControlType(element, controlType.Value);
        }
    }

    /// <summary>
    /// 设置标题级别
    /// </summary>
    public static void SetHeadingLevel(FrameworkElement element, AutomationHeadingLevel level)
    {
        AutomationProperties.SetHeadingLevel(element, level);
    }

    /// <summary>
    /// 设置实时区域
    /// </summary>
    public static void SetLiveRegion(FrameworkElement element, AutomationLiveSetting liveSetting)
    {
        AutomationProperties.SetLiveSetting(element, liveSetting);
    }

    /// <summary>
    /// 设置元素为地标
    /// </summary>
    public static void SetLandmark(FrameworkElement element, AutomationLandmarkType landmarkType)
    {
        AutomationProperties.SetLandmarkType(element, landmarkType);
    }

    /// <summary>
    /// 设置描述关系
    /// </summary>
    public static void SetDescribedBy(FrameworkElement element, FrameworkElement describingElement)
    {
        var describedByList = new List<FrameworkElement> { describingElement };
        AutomationProperties.SetDescribedBy(element, describedByList);
    }

    /// <summary>
    /// 设置标签关系
    /// </summary>
    public static void SetLabeledBy(FrameworkElement element, FrameworkElement labelElement)
    {
        var labeledByList = new List<FrameworkElement> { labelElement };
        AutomationProperties.SetLabeledBy(element, labeledByList);
    }

    /// <summary>
    /// 设置键盘导航
    /// </summary>
    public static void SetKeyboardNavigation(FrameworkElement element, bool isTabStop = true, int tabIndex = 0)
    {
        if (element is Control control)
        {
            control.IsTabStop = isTabStop;
            control.TabIndex = tabIndex;
        }
    }

    /// <summary>
    /// 设置焦点管理
    /// </summary>
    public static void SetFocusable(FrameworkElement element, bool isFocusable = true)
    {
        if (element is Control control)
        {
            control.IsTabStop = isFocusable;
        }
        else
        {
            element.IsTabStop = isFocusable;
        }
    }

    /// <summary>
    /// 为按钮设置无障碍属性
    /// </summary>
    public static void SetupButton(Button button, string name, string? description = null, string? keyboardShortcut = null)
    {
        SetAccessibilityProperties(button, name, description, AutomationControlType.Button);
        
        if (!string.IsNullOrEmpty(keyboardShortcut))
        {
            AutomationProperties.SetAcceleratorKey(button, keyboardShortcut);
        }
    }

    /// <summary>
    /// 为文本框设置无障碍属性
    /// </summary>
    public static void SetupTextBox(TextBox textBox, string name, string? placeholder = null, bool isRequired = false)
    {
        SetAccessibilityProperties(textBox, name, null, AutomationControlType.Edit);
        
        if (!string.IsNullOrEmpty(placeholder))
        {
            textBox.PlaceholderText = placeholder;
        }

        if (isRequired)
        {
            AutomationProperties.SetIsRequiredForForm(textBox, true);
        }
    }

    /// <summary>
    /// 为列表设置无障碍属性
    /// </summary>
    public static void SetupList(ListView listView, string name, string? description = null)
    {
        SetAccessibilityProperties(listView, name, description, AutomationControlType.List);
        AutomationProperties.SetItemType(listView, "列表项");
    }

    /// <summary>
    /// 为网格设置无障碍属性
    /// </summary>
    public static void SetupGrid(DataGrid dataGrid, string name, string? description = null)
    {
        SetAccessibilityProperties(dataGrid, name, description, AutomationControlType.DataGrid);
        AutomationProperties.SetItemType(dataGrid, "数据行");
    }

    /// <summary>
    /// 设置状态信息
    /// </summary>
    public static void AnnounceStatus(FrameworkElement element, string message)
    {
        AutomationProperties.SetName(element, message);
        SetLiveRegion(element, AutomationLiveSetting.Assertive);
    }

    /// <summary>
    /// 设置错误信息
    /// </summary>
    public static void AnnounceError(FrameworkElement element, string errorMessage)
    {
        AutomationProperties.SetName(element, $"错误: {errorMessage}");
        SetLiveRegion(element, AutomationLiveSetting.Assertive);
    }

    /// <summary>
    /// 设置成功信息
    /// </summary>
    public static void AnnounceSuccess(FrameworkElement element, string successMessage)
    {
        AutomationProperties.SetName(element, $"成功: {successMessage}");
        SetLiveRegion(element, AutomationLiveSetting.Polite);
    }

    /// <summary>
    /// 创建跳过链接
    /// </summary>
    public static Button CreateSkipLink(string text, FrameworkElement targetElement)
    {
        var skipButton = new Button
        {
            Content = text,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, -1000, 0, 0) // 屏幕外位置
        };

        skipButton.GotFocus += (s, e) =>
        {
            skipButton.Visibility = Visibility.Visible;
            skipButton.Margin = new Thickness(8);
        };

        skipButton.LostFocus += (s, e) =>
        {
            skipButton.Visibility = Visibility.Collapsed;
            skipButton.Margin = new Thickness(0, -1000, 0, 0);
        };

        skipButton.Click += (s, e) =>
        {
            targetElement.Focus(FocusState.Programmatic);
        };

        SetupButton(skipButton, text, "跳过导航到主要内容");
        
        return skipButton;
    }

    /// <summary>
    /// 设置表单验证
    /// </summary>
    public static void SetFormValidation(FrameworkElement element, bool hasError, string? errorMessage = null)
    {
        if (hasError && !string.IsNullOrEmpty(errorMessage))
        {
            AutomationProperties.SetHelpText(element, errorMessage);
            AutomationProperties.SetHasValidationError(element, true);
        }
        else
        {
            AutomationProperties.SetHelpText(element, string.Empty);
            AutomationProperties.SetHasValidationError(element, false);
        }
    }

    /// <summary>
    /// 设置进度指示器
    /// </summary>
    public static void SetupProgressIndicator(ProgressBar progressBar, string name, double? minimum = null, double? maximum = null)
    {
        SetAccessibilityProperties(progressBar, name, null, AutomationControlType.ProgressBar);
        
        if (minimum.HasValue)
        {
            AutomationProperties.SetRangeMinimum(progressBar, minimum.Value);
        }
        
        if (maximum.HasValue)
        {
            AutomationProperties.SetRangeMaximum(progressBar, maximum.Value);
        }
    }

    /// <summary>
    /// 设置切换开关
    /// </summary>
    public static void SetupToggleSwitch(ToggleSwitch toggleSwitch, string name, string? description = null)
    {
        SetAccessibilityProperties(toggleSwitch, name, description, AutomationControlType.Button);
        AutomationProperties.SetToggleState(toggleSwitch, toggleSwitch.IsOn ? ToggleState.On : ToggleState.Off);
        
        toggleSwitch.Toggled += (s, e) =>
        {
            AutomationProperties.SetToggleState(toggleSwitch, toggleSwitch.IsOn ? ToggleState.On : ToggleState.Off);
        };
    }

    /// <summary>
    /// 设置组合框
    /// </summary>
    public static void SetupComboBox(ComboBox comboBox, string name, string? description = null)
    {
        SetAccessibilityProperties(comboBox, name, description, AutomationControlType.ComboBox);
        AutomationProperties.SetIsExpandable(comboBox, true);
        
        comboBox.DropDownOpened += (s, e) =>
        {
            AutomationProperties.SetExpandCollapseState(comboBox, ExpandCollapseState.Expanded);
        };
        
        comboBox.DropDownClosed += (s, e) =>
        {
            AutomationProperties.SetExpandCollapseState(comboBox, ExpandCollapseState.Collapsed);
        };
    }
}