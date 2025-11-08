using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using AutoScheduling3.ViewModels.Settings;
using System;
using System.Linq;

namespace AutoScheduling3.Views.Settings;

/// <summary>
/// 设置页面 - 管理应用程序个性化设置
/// </summary>
public sealed partial class SettingsPage : Page
{
    private IThemeService? _themeService;
    private IConfigurationService? _configurationService;
    private bool _isInitializing = true;

    /// <summary>
    /// 获取ViewModel实例
    /// </summary>
    public SettingsPageViewModel ViewModel { get; }

    public SettingsPage()
    {
        // 从服务定位器获取ViewModel
        ViewModel = ServiceLocator.GetService<SettingsPageViewModel>() 
                    ?? throw new InvalidOperationException("SettingsPageViewModel not registered");
        
        InitializeComponent();
        Loaded += SettingsPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        // 获取服务实例
        _themeService = ServiceLocator.GetService<IThemeService>();
        _configurationService = ServiceLocator.GetService<IConfigurationService>();
        
        InitializeSettings();
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        // 应用页面进入动画
        this.RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform();
        var animation = AnimationHelper.CreateSlideInFromLeftAnimation(this);
        animation.Begin();
    }

    /// <summary>
    /// 初始化设置界面
    /// </summary>
    private void InitializeSettings()
    {
        if (_themeService == null || _configurationService == null) return;

        _isInitializing = true;

        try
        {
            // 设置主题选择
            var currentTheme = _themeService.CurrentTheme;
            var themeItem = ThemeComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag?.ToString() == currentTheme.ToString());
            if (themeItem != null)
            {
                ThemeComboBox.SelectedItem = themeItem;
            }

            // 设置动画选项
            AnimationToggle.IsOn = _themeService.IsAnimationEnabled;
            AnimationSpeedSlider.Value = _themeService.AnimationSpeedMultiplier;
            UpdateSpeedValueText();

            // 设置高对比度
            HighContrastToggle.IsOn = _themeService.IsHighContrastEnabled;

            // 设置字体大小
            var fontSize = _configurationService.GetValue("FontSize", 14);
            var fontSizeItem = FontSizeComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag?.ToString() == fontSize.ToString());
            if (fontSizeItem != null)
            {
                FontSizeComboBox.SelectedItem = fontSizeItem;
            }

            // 设置无障碍选项
            KeyboardHintsToggle.IsOn = _configurationService.GetValue("ShowKeyboardHints", false);
            ScreenReaderToggle.IsOn = _configurationService.GetValue("ScreenReaderOptimized", false);
            ReduceEffectsToggle.IsOn = _configurationService.GetValue("ReduceVisualEffects", false);

            // 启用动画速度滑块
            AnimationSpeedSlider.IsEnabled = AnimationToggle.IsOn;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    /// <summary>
    /// 主题选择变更
    /// </summary>
    private async void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || _themeService == null) return;

        if (ThemeComboBox.SelectedItem is ComboBoxItem item && 
            Enum.TryParse<ElementTheme>(item.Tag?.ToString(), out var theme))
        {
            await _themeService.SetThemeAsync(theme);
            
            // 播放切换动画
            AnimationHelper.AnimateButtonPress(ThemeComboBox);
        }
    }

    /// <summary>
    /// 动画开关切换
    /// </summary>
    private async void AnimationToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || _themeService == null) return;

        await _themeService.SetAnimationEnabledAsync(AnimationToggle.IsOn);
        AnimationSpeedSlider.IsEnabled = AnimationToggle.IsOn;
    }

    /// <summary>
    /// 动画速度调整
    /// </summary>
    private async void AnimationSpeedSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing || _themeService == null) return;

        await _themeService.SetAnimationSpeedMultiplierAsync(AnimationSpeedSlider.Value);
        UpdateSpeedValueText();
    }

    /// <summary>
    /// 更新速度显示文本
    /// </summary>
    private void UpdateSpeedValueText()
    {
        SpeedValueText.Text = $"{AnimationSpeedSlider.Value:F1}x";
    }

    /// <summary>
    /// 高对比度切换
    /// </summary>
    private async void HighContrastToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || _themeService == null) return;

        await _themeService.SetHighContrastEnabledAsync(HighContrastToggle.IsOn);
        
        // 应用高对比度主题
        if (HighContrastToggle.IsOn)
        {
            if (this.XamlRoot?.Content is FrameworkElement root)
            {
                root.RequestedTheme = ElementTheme.Default;
                // 这里可以添加更多高对比度样式的应用逻辑
            }
        }
    }

    /// <summary>
    /// 字体大小选择变更
    /// </summary>
    private async void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || _configurationService == null) return;

        if (FontSizeComboBox.SelectedItem is ComboBoxItem item &&
            int.TryParse(item.Tag?.ToString(), out var fontSize))
        {
            await _configurationService.SetValueAsync("FontSize", fontSize);
            
            // 这里可以添加动态更新字体大小的逻辑
            ApplyFontSize(fontSize);
        }
    }

    /// <summary>
    /// 应用字体大小
    /// </summary>
    private void ApplyFontSize(int fontSize)
    {
        // 更新应用程序资源中的字体大小
        if (Application.Current.Resources.ContainsKey("DefaultFontSize"))
        {
            Application.Current.Resources["DefaultFontSize"] = fontSize;
        }
        else
        {
            Application.Current.Resources.Add("DefaultFontSize", fontSize);
        }
    }

    /// <summary>
    /// 键盘提示切换
    /// </summary>
    private async void KeyboardHintsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || _configurationService == null) return;

        await _configurationService.SetValueAsync("ShowKeyboardHints", KeyboardHintsToggle.IsOn);
    }

    /// <summary>
    /// 屏幕阅读器优化切换
    /// </summary>
    private async void ScreenReaderToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || _configurationService == null) return;

        await _configurationService.SetValueAsync("ScreenReaderOptimized", ScreenReaderToggle.IsOn);
    }

    /// <summary>
    /// 减少视觉效果切换
    /// </summary>
    private async void ReduceEffectsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || _configurationService == null) return;

        await _configurationService.SetValueAsync("ReduceVisualEffects", ReduceEffectsToggle.IsOn);
    }

    /// <summary>
    /// 重置所有设置
    /// </summary>
    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        // 显示确认对话框
        var dialog = new ContentDialog
        {
            Title = "重置设置",
            Content = "确定要重置所有设置到默认值吗？此操作无法撤销。",
            PrimaryButtonText = "重置",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ResetAllSettings();
        }
    }

    /// <summary>
    /// 重置所有设置到默认值
    /// </summary>
    private async System.Threading.Tasks.Task ResetAllSettings()
    {
        if (_themeService == null || _configurationService == null) return;

        try
        {
            // 重置主题设置
            await _themeService.SetThemeAsync(ElementTheme.Default);
            await _themeService.SetAnimationEnabledAsync(true);
            await _themeService.SetAnimationSpeedMultiplierAsync(1.0);
            await _themeService.SetHighContrastEnabledAsync(false);

            // 重置其他设置
            await _configurationService.SetValueAsync("FontSize", 14);
            await _configurationService.SetValueAsync("ShowKeyboardHints", false);
            await _configurationService.SetValueAsync("ScreenReaderOptimized", false);
            await _configurationService.SetValueAsync("ReduceVisualEffects", false);

            // 重新初始化界面
            InitializeSettings();

            // 显示成功消息
            var successDialog = new ContentDialog
            {
                Title = "重置完成",
                Content = "所有设置已重置到默认值。",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await successDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            // 显示错误消息
            var errorDialog = new ContentDialog
            {
                Title = "重置失败",
                Content = $"重置设置时发生错误：{ex.Message}",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }
}