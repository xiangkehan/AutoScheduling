using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

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
        try
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
        catch (Exception ex)
        {
            // 如果对话框显示失败，回退到调试输出
            System.Diagnostics.Debug.WriteLine($"Dialog failed to show: {title} - {message}. Error: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示错误对话框（标题 + 文本消息）
    /// 新增重载：支持传入标题和详细消息（解决调用处传入两个字符串的情况）
    /// </summary>
    public async Task ShowErrorAsync(string title, string message)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };

            // 创建一个任务来在5秒后自动关闭对话框
            var autoCloseTask = Task.Run(async () =>
            {
                await Task.Delay(5000); // 等待5秒

                // 在UI线程上关闭对话框
                if (App.MainWindow?.DispatcherQueue != null)
                {
                    App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            dialog.Hide();
                        }
                        catch
                        {
                            // 对话框可能已经被用户关闭
                        }
                    });
                }
            });

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            // 如果对话框显示失败，回退到调试输出
            System.Diagnostics.Debug.WriteLine($"Error dialog failed to show: {title} - {message}. Dialog error: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示错误对话框（5秒后自动关闭）
    /// 保持原有签名：message + optional exception
    /// </summary>
    public async Task ShowErrorAsync(string message, Exception? exception = null)
    {
        try
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

            // 创建一个任务来在5秒后自动关闭对话框
            var autoCloseTask = Task.Run(async () =>
            {
                await Task.Delay(5000); // 等待5秒

                // 在UI线程上关闭对话框
                if (App.MainWindow?.DispatcherQueue != null)
                {
                    App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            dialog.Hide();
                        }
                        catch
                        {
                            // 对话框可能已经被用户关闭
                        }
                    });
                }
            });

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            // 如果对话框显示失败，回退到调试输出
            System.Diagnostics.Debug.WriteLine($"Error dialog failed to show: {message}. Dialog error: {ex.Message}");
        }
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
    /// 显示警告对话框（带自定义标题）
    /// </summary>
    public async Task ShowWarningAsync(string title, string message)
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

    /// <summary>
    /// 显示输入对话框
    /// </summary>
    public async Task<string> ShowInputDialogAsync(string title, string message, string defaultText = "")
    {
        var inputTextBox = new TextBox
        {
            AcceptsReturn = false,
            Height = 32,
            Text = defaultText,
            SelectionStart = defaultText.Length
        };
        var dialog = new ContentDialog
        {
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = message, Margin = new Microsoft.UI.Xaml.Thickness(0,0,0,12) },
                    inputTextBox
                }
            },
            Title = title,
            IsSecondaryButtonEnabled = true,
            PrimaryButtonText = "OK",
            SecondaryButtonText = "Cancel",
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            return inputTextBox.Text;
        else
            return null;
    }
}
