using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace AutoScheduling3.Services
{
    public class DialogService
    {
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

        public async Task ShowWarningAsync(string message)
        {
            await ShowMessageAsync("警告", message);
        }

        public async Task ShowErrorAsync(string message)
        {
            await ShowMessageAsync("错误", message);
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }
    }
}