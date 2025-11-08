using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.Models;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace AutoScheduling3.ViewModels.Settings;

/// <summary>
/// 设置页面的ViewModel
/// </summary>
public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IStoragePathService _storagePathService;
    private readonly IThemeService _themeService;
    private readonly IConfigurationService _configurationService;
    private readonly DialogService _dialogService;

    /// <summary>
    /// 存储文件信息集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<StorageFileInfo> storageFiles = new();

    /// <summary>
    /// 是否正在加载存储信息
    /// </summary>
    [ObservableProperty]
    private bool isLoadingStorageInfo;

    public SettingsPageViewModel(
        IStoragePathService storagePathService,
        IThemeService themeService,
        IConfigurationService configurationService)
    {
        _storagePathService = storagePathService ?? throw new ArgumentNullException(nameof(storagePathService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _dialogService = new DialogService();

        // 初始化时加载存储文件信息
        _ = InitializeAsync();
    }

    /// <summary>
    /// 异步初始化ViewModel
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await LoadStorageFilesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"初始化ViewModel失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 复制文件路径到剪贴板
    /// </summary>
    /// <param name="filePath">要复制的文件路径</param>
    [RelayCommand]
    private async Task CopyPathAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            await _dialogService.ShowWarningAsync("文件路径为空，无法复制");
            return;
        }

        try
        {
            var success = await ClipboardHelper.CopyTextAsync(filePath);
            if (success)
            {
                ClipboardHelper.ShowCopySuccessNotification();
                await _dialogService.ShowSuccessAsync("已复制到剪贴板");
            }
            else
            {
                await _dialogService.ShowErrorAsync("复制失败，请重试");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"复制路径失败: {ex.Message}");
            await _dialogService.ShowErrorAsync("复制路径时发生错误", ex);
        }
    }

    /// <summary>
    /// 打开文件所在目录
    /// </summary>
    /// <param name="filePath">文件路径</param>
    [RelayCommand]
    private async Task OpenDirectoryAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            await _dialogService.ShowWarningAsync("文件路径为空，无法打开目录");
            return;
        }

        try
        {
            await ProcessHelper.OpenDirectoryAndSelectFileAsync(filePath);
        }
        catch (FileNotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"文件不存在: {ex.Message}");
            await _dialogService.ShowErrorAsync("文件不存在，无法打开目录");
        }
        catch (DirectoryNotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"目录不存在: {ex.Message}");
            await _dialogService.ShowErrorAsync("目录不存在");
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"权限不足: {ex.Message}");
            await _dialogService.ShowErrorAsync("权限不足，无法访问该文件或目录");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"打开目录失败: {ex.Message}");
            await _dialogService.ShowErrorAsync("打开目录时发生错误", ex);
        }
    }

    /// <summary>
    /// 刷新存储信息
    /// </summary>
    [RelayCommand]
    private async Task RefreshStorageInfoAsync()
    {
        await LoadStorageFilesAsync();
    }

    /// <summary>
    /// 加载存储文件信息
    /// </summary>
    private async Task LoadStorageFilesAsync()
    {
        IsLoadingStorageInfo = true;

        try
        {
            var files = await _storagePathService.GetStorageFilesAsync();
            
            StorageFiles.Clear();
            foreach (var file in files)
            {
                StorageFiles.Add(file);
            }

            // 如果没有加载到任何文件，显示警告
            if (StorageFiles.Count == 0)
            {
                await _dialogService.ShowWarningAsync("未找到任何存储文件信息");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"权限不足: {ex.Message}");
            await _dialogService.ShowErrorAsync("权限不足，无法访问存储文件信息");
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"IO错误: {ex.Message}");
            await _dialogService.ShowErrorAsync("读取存储文件信息时发生IO错误", ex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载存储文件信息失败: {ex.Message}");
            await _dialogService.ShowErrorAsync("加载存储文件信息时发生错误", ex);
        }
        finally
        {
            IsLoadingStorageInfo = false;
        }
    }
}
