using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls; // Added for Page
using Microsoft.UI.Xaml.Automation;
using AutoScheduling3.ViewModels.DataManagement;
using System;

namespace AutoScheduling3.Views.DataManagement;

/// <summary>
/// 技能管理页面
/// </summary>
public sealed partial class SkillPage : Page
{
    public SkillViewModel ViewModel { get; }

    public SkillPage()
    {
        this.InitializeComponent();
        ViewModel = ((App)Application.Current).ServiceProvider.GetRequiredService<SkillViewModel>();
        this.Loaded += SkillPage_Loaded;
    }

    private async void SkillPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDataAsync();
    }

    private void SkillGridView_Loaded(object sender, RoutedEventArgs e)
    {
        // 订阅SelectedItem变化事件以实现自动滚动
        ViewModel.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.SelectedItem) && ViewModel.SelectedItem != null)
            {
                // 延迟执行以确保UI已更新
                _ = this.DispatcherQueue.TryEnqueue(() =>
                {
                    SkillGridView.ScrollIntoView(ViewModel.SelectedItem, ScrollIntoViewAlignment.Default);
                });
            }
        };
    }

    /// <summary>
    /// 显示创建技能对话框
    /// </summary>
    private async void CreateSkill_Click(object sender, RoutedEventArgs e)
    {
        await ShowCreateSkillDialogAsync();
    }

    /// <summary>
    /// 显示创建技能对话框
    /// </summary>
    private async System.Threading.Tasks.Task ShowCreateSkillDialogAsync()
    {
        try
        {
            // 检查 XamlRoot
            if (this.XamlRoot == null)
            {
                System.Diagnostics.Debug.WriteLine("XamlRoot is null, cannot show dialog");
                await ViewModel.DialogService.ShowErrorAsync("无法显示对话框，请刷新页面后重试");
                return;
            }

            // 创建对话框
            var dialog = new ContentDialog
            {
                Title = "创建新技能",
                PrimaryButtonText = "创建",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            // 创建输入控件
            var nameTextBox = new TextBox
            {
                Header = "技能名称*",
                PlaceholderText = "请输入技能名称",
                MaxLength = 50,
                TabIndex = 0
            };
            nameTextBox.SetValue(AutomationProperties.NameProperty, "技能名称，必填");
            nameTextBox.SetValue(AutomationProperties.HelpTextProperty, "请输入技能名称，最多50个字符");

            var descriptionTextBox = new TextBox
            {
                Header = "技能描述",
                PlaceholderText = "请输入技能描述（可选）",
                MaxLength = 200,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 80,
                TabIndex = 1
            };
            descriptionTextBox.SetValue(AutomationProperties.NameProperty, "技能描述");
            descriptionTextBox.SetValue(AutomationProperties.HelpTextProperty, "请输入技能描述，可选，最多200个字符");

            // 创建布局容器
            var stackPanel = new StackPanel { Spacing = 12 };
            stackPanel.Children.Add(nameTextBox);
            stackPanel.Children.Add(descriptionTextBox);

            dialog.Content = stackPanel;

            // 对话框打开后自动聚焦第一个输入框
            dialog.Opened += (s, args) =>
            {
                nameTextBox.Focus(FocusState.Programmatic);
            };

            // 验证逻辑
            dialog.PrimaryButtonClick += (s, args) =>
            {
                var name = nameTextBox.Text?.Trim();
                
                if (string.IsNullOrWhiteSpace(name))
                {
                    args.Cancel = true;
                    _ = ViewModel.DialogService.ShowErrorAsync("验证失败", new Exception("技能名称不能为空"));
                }
                else if (name.Length > 50)
                {
                    args.Cancel = true;
                    _ = ViewModel.DialogService.ShowErrorAsync("验证失败", new Exception("技能名称长度不能超过50字符"));
                }
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // 将对话框输入绑定到 ViewModel.NewSkill
                ViewModel.NewSkill.Name = nameTextBox.Text.Trim();
                ViewModel.NewSkill.Description = descriptionTextBox.Text?.Trim();

                // 执行创建命令
                await ViewModel.CreateCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to show dialog: {ex.Message}");
            await ViewModel.DialogService.ShowErrorAsync("显示对话框时发生错误", ex);
        }
    }
}
