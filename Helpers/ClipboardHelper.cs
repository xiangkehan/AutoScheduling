using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 剪贴板辅助类 - 管理剪贴板操作
/// </summary>
public static class ClipboardHelper
{
    /// <summary>
    /// 复制文本到剪贴板
    /// </summary>
    /// <param name="text">要复制的文本</param>
    /// <returns>复制是否成功</returns>
    public static async Task<bool> CopyTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            System.Diagnostics.Debug.WriteLine("ClipboardHelper: 尝试复制空文本");
            return false;
        }

        try
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
            
            System.Diagnostics.Debug.WriteLine($"ClipboardHelper: 成功复制文本到剪贴板: {text}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ClipboardHelper: 复制到剪贴板失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 显示复制成功的通知
    /// </summary>
    public static void ShowCopySuccessNotification()
    {
        System.Diagnostics.Debug.WriteLine("ClipboardHelper: 已复制到剪贴板");
    }
}
