using AutoScheduling3.DTOs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.Views.Scheduling;

/// <summary>
/// 冲突修复对话框
/// </summary>
public sealed partial class ConflictResolutionDialog : ContentDialog
{
    private ConflictDto? _conflict;
    private List<ConflictResolutionOption> _resolutionOptions = new();

    /// <summary>
    /// 选定的修复方案
    /// </summary>
    public ConflictResolutionOption? SelectedResolution { get; private set; }

    public ConflictResolutionDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 初始化对话框
    /// </summary>
    /// <param name="conflict">冲突信息</param>
    /// <param name="resolutionOptions">修复方案列表</param>
    public void Initialize(ConflictDto conflict, List<ConflictResolutionOption> resolutionOptions)
    {
        _conflict = conflict ?? throw new ArgumentNullException(nameof(conflict));
        _resolutionOptions = resolutionOptions ?? throw new ArgumentNullException(nameof(resolutionOptions));

        LoadConflictDetails();
        LoadResolutionOptions();
    }

    /// <summary>
    /// 加载冲突详情
    /// </summary>
    private void LoadConflictDetails()
    {
        if (_conflict == null) return;

        // 设置冲突类型
        ConflictTypeText.Text = GetConflictTypeDisplayName(_conflict.SubType);

        // 设置人员信息
        if (!string.IsNullOrEmpty(_conflict.PersonnelName))
        {
            PersonnelLabel.Visibility = Visibility.Visible;
            PersonnelText.Visibility = Visibility.Visible;
            PersonnelText.Text = _conflict.PersonnelName;
        }
        else
        {
            PersonnelLabel.Visibility = Visibility.Collapsed;
            PersonnelText.Visibility = Visibility.Collapsed;
        }

        // 设置哨位信息
        if (!string.IsNullOrEmpty(_conflict.PositionName))
        {
            PositionLabel.Visibility = Visibility.Visible;
            PositionText.Visibility = Visibility.Visible;
            PositionText.Text = _conflict.PositionName;
        }
        else
        {
            PositionLabel.Visibility = Visibility.Collapsed;
            PositionText.Visibility = Visibility.Collapsed;
        }

        // 设置时间信息
        if (_conflict.StartTime.HasValue)
        {
            TimeLabel.Visibility = Visibility.Visible;
            TimeText.Visibility = Visibility.Visible;
            
            if (_conflict.EndTime.HasValue)
            {
                TimeText.Text = $"{_conflict.StartTime.Value:yyyy-MM-dd HH:mm} - {_conflict.EndTime.Value:HH:mm}";
            }
            else
            {
                TimeText.Text = _conflict.StartTime.Value.ToString("yyyy-MM-dd HH:mm");
            }
        }
        else
        {
            TimeLabel.Visibility = Visibility.Collapsed;
            TimeText.Visibility = Visibility.Collapsed;
        }

        // 设置问题描述
        ProblemText.Text = _conflict.DetailedMessage ?? _conflict.Message;
    }

    /// <summary>
    /// 加载修复方案
    /// </summary>
    private void LoadResolutionOptions()
    {
        if (_resolutionOptions == null || _resolutionOptions.Count == 0)
        {
            // 无可用方案
            ResolutionOptionsItemsControl.Visibility = Visibility.Collapsed;
            NoOptionsPanel.Visibility = Visibility.Visible;
            IsPrimaryButtonEnabled = false;
            return;
        }

        ResolutionOptionsItemsControl.ItemsSource = _resolutionOptions;
        NoOptionsPanel.Visibility = Visibility.Collapsed;
        IsPrimaryButtonEnabled = true;

        // 默认选中推荐方案
        var recommendedOption = _resolutionOptions.FirstOrDefault(o => o.IsRecommended);
        if (recommendedOption != null)
        {
            SelectedResolution = recommendedOption;
        }
    }

    /// <summary>
    /// 获取冲突类型显示名称
    /// </summary>
    private string GetConflictTypeDisplayName(ConflictSubType subType)
    {
        return subType switch
        {
            ConflictSubType.SkillMismatch => "技能不匹配",
            ConflictSubType.PersonnelUnavailable => "人员不可用",
            ConflictSubType.DuplicateAssignment => "重复分配",
            ConflictSubType.InsufficientRest => "休息时间不足",
            ConflictSubType.ExcessiveWorkload => "工作量过大",
            ConflictSubType.WorkloadImbalance => "工作量不均衡",
            ConflictSubType.ConsecutiveOvertime => "连续工作超时",
            ConflictSubType.UnassignedSlot => "未分配时段",
            ConflictSubType.SuboptimalAssignment => "次优分配",
            _ => "未知类型"
        };
    }

    /// <summary>
    /// 主按钮点击事件
    /// </summary>
    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 获取选中的方案
        var selectedRadioButton = FindSelectedRadioButton(ResolutionOptionsItemsControl);
        if (selectedRadioButton != null && selectedRadioButton.DataContext is ConflictResolutionOption option)
        {
            SelectedResolution = option;
        }
        else if (_resolutionOptions.Count > 0)
        {
            // 如果没有选中，使用推荐方案或第一个方案
            SelectedResolution = _resolutionOptions.FirstOrDefault(o => o.IsRecommended) 
                              ?? _resolutionOptions.First();
        }
    }

    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    private void OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        SelectedResolution = null;
    }

    /// <summary>
    /// 查找选中的 RadioButton
    /// </summary>
    private RadioButton? FindSelectedRadioButton(ItemsControl itemsControl)
    {
        for (int i = 0; i < itemsControl.Items.Count; i++)
        {
            var container = itemsControl.ContainerFromIndex(i);
            if (container is RadioButton radioButton && radioButton.IsChecked == true)
            {
                return radioButton;
            }
        }
        return null;
    }
}
