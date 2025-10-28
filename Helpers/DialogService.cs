using Microsoft.UI.Xaml.Controls;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 对话框服务 - 管理对话框显示
/// </summary>
public class DialogService
{
    /// <summary>
    /// 显示消息对话框
    /// </summary>
    public async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    public async Task ShowErrorAsync(string message, Exception? exception = null)
    {
        string content = message;
        if (exception != null)
        {
            content += $"\n\n详细信息:\n{exception.Message}";
        }

        var dialog = new ContentDialog
        {
            Title = "错误",
            Content = content,
            CloseButtonText = "确定",
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    public async Task<bool> ShowConfirmAsync(string title, string message, string primaryButtonText = "确定", string secondaryButtonText = "取消")
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// 显示警告对话框
    /// </summary>
    public async Task ShowWarningAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "警告",
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示成功消息
    /// </summary>
    public async Task ShowSuccessAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "成功",
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示加载对话框
    /// </summary>
    public ContentDialog ShowLoadingDialog(string message = "正在加载...")
    {
        var dialog = new ContentDialog
        {
            Title = "请稍候",
            Content = new StackPanel
            {
                Children =
                {
                    new ProgressRing { IsActive = true, Width = 50, Height = 50 },
                    new TextBlock { Text = message, Margin = new Microsoft.UI.Xaml.Thickness(0, 10, 0, 0) }
                }
            },
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        _ = dialog.ShowAsync();
        return dialog;
    }
}
