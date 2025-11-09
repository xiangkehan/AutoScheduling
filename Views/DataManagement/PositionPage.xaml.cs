using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media.Animation;
using AutoScheduling3.ViewModels.DataManagement;
using AutoScheduling3.Helpers;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace AutoScheduling3.Views.DataManagement;

/// <summary>
/// 哨位管理页面
/// </summary>
public sealed partial class PositionPage : Page
{
    public PositionViewModel ViewModel { get; }

    public PositionPage()
    {
        this.InitializeComponent();
        ViewModel = ((App)Application.Current).ServiceProvider.GetRequiredService<PositionViewModel>();
        this.DataContext = ViewModel; // 设置 DataContext 以支持 {Binding}
        this.Loaded += PositionPage_Loaded;
        
        // 订阅集合变化事件以触发动画
        ViewModel.AvailablePersonnel.CollectionChanged += OnAvailablePersonnelCollectionChanged;
    }

    private async void PositionPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDataAsync();
        
        // 初始化响应式布局
        if (MainContentGrid.ActualWidth > 0)
        {
            ApplyResponsiveLayout(MainContentGrid.ActualWidth);
        }
    }

    private void PositionListView_Loaded(object sender, RoutedEventArgs e)
    {
        // 订阅SelectedItem变化事件以实现自动滚动
        ViewModel.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.SelectedItem) && ViewModel.SelectedItem != null)
            {
                // 延迟执行以确保UI已更新
                _ = this.DispatcherQueue.TryEnqueue(() =>
                {
                    PositionListView.ScrollIntoView(ViewModel.SelectedItem);
                });
            }
        };
    }

    /// <summary>
    /// 显示创建哨位对话框
    /// </summary>
    private async void CreatePosition_Click(object sender, RoutedEventArgs e)
    {
        await ShowCreatePositionDialogAsync();
    }

    /// <summary>
    /// 显示编辑哨位对话框
    /// </summary>
    private async void EditPosition_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedItem == null)
        {
            await ViewModel.DialogService.ShowErrorAsync("请先选择要编辑的哨位");
            return;
        }

        try
        {
            // 检查 XamlRoot
            if (this.XamlRoot == null)
            {
                System.Diagnostics.Debug.WriteLine("XamlRoot is null, cannot show dialog");
                await ViewModel.DialogService.ShowErrorAsync("无法显示对话框，请刷新页面后重试");
                return;
            }

            // 创建编辑对话框
            var dialog = new PositionEditDialog
            {
                XamlRoot = this.XamlRoot,
                Position = ViewModel.SelectedItem,
                AvailableSkills = ViewModel.AvailableSkills
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.EditedPosition != null)
            {
                // 调用ViewModel的更新方法
                await ViewModel.UpdatePositionAsync(dialog.EditedPosition);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to show edit dialog: {ex.Message}");
            await ViewModel.DialogService.ShowErrorAsync("显示编辑对话框时发生错误", ex);
        }
    }

    /// <summary>
    /// 显示创建哨位对话框
    /// </summary>
    private async System.Threading.Tasks.Task ShowCreatePositionDialogAsync()
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

            // 技能是可选的，即使没有技能数据也可以创建哨位

            // 创建对话框
            var dialog = new ContentDialog
            {
                Title = "创建新哨位",
                PrimaryButtonText = "创建",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            // 创建输入控件
            var nameTextBox = new TextBox
            {
                Header = "哨位名称*",
                PlaceholderText = "请输入哨位名称",
                MaxLength = 100,
                TabIndex = 0
            };
            nameTextBox.SetValue(AutomationProperties.NameProperty, "哨位名称，必填");
            nameTextBox.SetValue(AutomationProperties.HelpTextProperty, "请输入哨位名称，最多100个字符");

            var locationTextBox = new TextBox
            {
                Header = "地点*",
                PlaceholderText = "请输入地点",
                MaxLength = 200,
                TabIndex = 1
            };
            locationTextBox.SetValue(AutomationProperties.NameProperty, "地点，必填");
            locationTextBox.SetValue(AutomationProperties.HelpTextProperty, "请输入哨位地点，最多200个字符");

            var descriptionTextBox = new TextBox
            {
                Header = "介绍",
                PlaceholderText = "请输入介绍（可选）",
                MaxLength = 500,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 80,
                TabIndex = 2
            };
            descriptionTextBox.SetValue(AutomationProperties.NameProperty, "介绍");
            descriptionTextBox.SetValue(AutomationProperties.HelpTextProperty, "请输入哨位介绍，可选，最多500个字符");

            // 创建技能选择ListView
            var skillsListView = new ListView
            {
                ItemsSource = ViewModel.AvailableSkills,
                SelectionMode = ListViewSelectionMode.Multiple,
                MaxHeight = 150,
                TabIndex = 3
            };
            skillsListView.SetValue(AutomationProperties.NameProperty, "所需技能列表，可选，支持多选");
            skillsListView.SetValue(AutomationProperties.HelpTextProperty, "使用空格键选择或取消选择技能，Tab键移动到下一个控件。如不选择任何技能，则表示此哨位无技能要求");

            // 设置ListView的ItemTemplate
            var dataTemplate = new DataTemplate();
            // Note: In code-behind, we'll use DisplayMemberPath for simplicity
            skillsListView.DisplayMemberPath = "Name";

            // 创建技能选择标题和计数显示
            var skillsHeaderGrid = new Grid();
            skillsHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            skillsHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            skillsHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            skillsHeaderGrid.Margin = new Thickness(0, 0, 0, 4);

            var skillsHeader = new TextBlock
            {
                Text = "所需技能（可选）",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            Grid.SetColumn(skillsHeader, 0);

            var skillCountText = new TextBlock
            {
                Text = "已选择: 0",
                FontSize = 12,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"],
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(skillCountText, 2);

            skillsHeaderGrid.Children.Add(skillsHeader);
            skillsHeaderGrid.Children.Add(skillCountText);

            // 更新技能选择计数
            skillsListView.SelectionChanged += (s, args) =>
            {
                skillCountText.Text = $"已选择: {skillsListView.SelectedItems.Count}";
            };

            var skillsContainer = new StackPanel();
            skillsContainer.Children.Add(skillsHeaderGrid);
            skillsContainer.Children.Add(skillsListView);

            // 创建布局容器
            var scrollViewer = new ScrollViewer
            {
                MaxHeight = 500
            };

            var stackPanel = new StackPanel { Spacing = 12 };
            stackPanel.Children.Add(nameTextBox);
            stackPanel.Children.Add(locationTextBox);
            stackPanel.Children.Add(descriptionTextBox);
            stackPanel.Children.Add(skillsContainer);

            scrollViewer.Content = stackPanel;
            dialog.Content = scrollViewer;

            // 对话框打开后自动聚焦第一个输入框
            dialog.Opened += (s, args) =>
            {
                nameTextBox.Focus(FocusState.Programmatic);
            };

            // 验证逻辑
            dialog.PrimaryButtonClick += (s, args) =>
            {
                var name = nameTextBox.Text?.Trim();
                var location = locationTextBox.Text?.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    args.Cancel = true;
                    _ = ViewModel.DialogService.ShowErrorAsync("验证失败", new Exception("哨位名称不能为空"));
                }
                else if (name.Length > 100)
                {
                    args.Cancel = true;
                    _ = ViewModel.DialogService.ShowErrorAsync("验证失败", new Exception("哨位名称长度不能超过100字符"));
                }
                else if (string.IsNullOrWhiteSpace(location))
                {
                    args.Cancel = true;
                    _ = ViewModel.DialogService.ShowErrorAsync("验证失败", new Exception("地点不能为空"));
                }
                else if (location.Length > 200)
                {
                    args.Cancel = true;
                    _ = ViewModel.DialogService.ShowErrorAsync("验证失败", new Exception("地点长度不能超过200字符"));
                }
                // 移除技能必选验证 - 允许创建没有技能要求的哨位
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // 将对话框输入绑定到 ViewModel.NewPosition
                ViewModel.NewPosition.Name = nameTextBox.Text.Trim();
                ViewModel.NewPosition.Location = locationTextBox.Text.Trim();
                ViewModel.NewPosition.Description = descriptionTextBox.Text?.Trim();
                
                // 收集选中的技能ID
                ViewModel.NewPosition.RequiredSkillIds.Clear();
                foreach (var item in skillsListView.SelectedItems)
                {
                    if (item is DTOs.SkillDto skill)
                    {
                        ViewModel.NewPosition.RequiredSkillIds.Add(skill.Id);
                    }
                }

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

    private void MainContentGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveLayout(e.NewSize.Width);
    }

    private void ApplyResponsiveLayout(double availableWidth)
    {
        var breakpoint = ResponsiveHelper.GetCurrentBreakpoint(availableWidth);
        
        // 根据断点调整布局
        switch (breakpoint)
        {
            case ResponsiveHelper.Breakpoint.XSmall:
            case ResponsiveHelper.Breakpoint.Small:
                // 小屏幕：垂直布局
                ApplyVerticalLayout();
                break;
                
            case ResponsiveHelper.Breakpoint.Medium:
            case ResponsiveHelper.Breakpoint.Large:
            case ResponsiveHelper.Breakpoint.XLarge:
                // 大屏幕：水平布局
                ApplyHorizontalLayout();
                break;
        }

        // 应用响应式边距
        var margin = ResponsiveHelper.GetResponsiveMargin(breakpoint);
        MainContentGrid.Margin = new Thickness(margin.Left / 2, margin.Top / 2, margin.Right / 2, margin.Bottom / 2);
    }

    private void ApplyVerticalLayout()
    {
        // 清除列定义，使用行布局
        MainContentGrid.ColumnDefinitions.Clear();
        MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // 设置行定义
        MainContentGrid.RowDefinitions.Clear();
        MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) }); // 间距
        MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // 重新定位元素
        Grid.SetColumn(PositionListGrid, 0);
        Grid.SetRow(PositionListGrid, 0);
        
        Grid.SetColumn(DetailScrollViewer, 0);
        Grid.SetRow(DetailScrollViewer, 2);

        // 隐藏分隔线
        SeparatorBorder.Visibility = Visibility.Collapsed;
    }

    private void ApplyHorizontalLayout()
    {
        // 设置列定义
        MainContentGrid.ColumnDefinitions.Clear();
        MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) }); // 间距
        MainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.5, GridUnitType.Star), MinWidth = 350, MaxWidth = 600 });

        // 清除行定义
        MainContentGrid.RowDefinitions.Clear();
        MainContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // 重新定位元素
        Grid.SetColumn(PositionListGrid, 0);
        Grid.SetRow(PositionListGrid, 0);
        
        Grid.SetColumn(DetailScrollViewer, 2);
        Grid.SetRow(DetailScrollViewer, 0);

        Grid.SetColumn(SeparatorBorder, 1);
        Grid.SetRow(SeparatorBorder, 0);

        // 显示分隔线
        SeparatorBorder.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// 处理可用人员集合变化事件
    /// </summary>
    private void OnAvailablePersonnelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            // 为新添加的项触发淡入动画
            foreach (var item in e.NewItems)
            {
                TriggerFadeInAnimation(item);
            }
        }
    }

    /// <summary>
    /// 为新添加的列表项触发淡入动画
    /// </summary>
    private async void TriggerFadeInAnimation(object item)
    {
        // 等待UI更新
        await System.Threading.Tasks.Task.Delay(10);
        
        // 查找对应的ListViewItem容器
        var container = AvailablePersonnelListView.ContainerFromItem(item) as ListViewItem;
        if (container != null)
        {
            var storyboard = Resources["FadeInStoryboard"] as Storyboard;
            if (storyboard != null)
            {
                Storyboard.SetTarget(storyboard, container);
                storyboard.Begin();
            }
        }
    }
}
