using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.Models;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using AutoScheduling3.DTOs.ImportExport;
using AutoScheduling3.Data;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AutoScheduling3.ViewModels.Settings;

/// <summary>
/// 设置页面的ViewModel
/// </summary>
public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IStoragePathService _storagePathService;
    private readonly IThemeService _themeService;
    private readonly IConfigurationService _configurationService;
    private readonly IDataImportExportService _importExportService;
    private readonly DialogService _dialogService;
    private readonly DatabaseService _databaseService;

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

    /// <summary>
    /// 是否正在执行导入导出操作
    /// </summary>
    [ObservableProperty]
    private bool isImportExportInProgress;

    public SettingsPageViewModel(
        IStoragePathService storagePathService,
        IThemeService themeService,
        IConfigurationService configurationService,
        IDataImportExportService importExportService,
        DatabaseService databaseService)
    {
        _storagePathService = storagePathService ?? throw new ArgumentNullException(nameof(storagePathService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _importExportService = importExportService ?? throw new ArgumentNullException(nameof(importExportService));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
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

    /// <summary>
    /// 导出数据命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteImportExport))]
    private async Task ExportDataAsync()
    {
        AutoScheduling3.Views.Settings.ImportExportProgressDialog progressDialog = null;
        
        try
        {
            IsImportExportInProgress = true;

            // 使用 FileManagementHelper 显示文件保存对话框
            var file = await FileManagementHelper.ShowSaveFileDialogAsync();
            if (file == null)
            {
                // 用户取消了操作
                return;
            }

            // 创建并显示进度对话框
            progressDialog = new AutoScheduling3.Views.Settings.ImportExportProgressDialog
            {
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };
            
            // 异步显示对话框（不等待）
            _ = progressDialog.ShowAsync();

            // 创建进度报告（在 UI 线程上更新）
            var progress = new Progress<ExportProgress>(p =>
            {
                // 确保在 UI 线程上更新进度
                App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    progressDialog?.UpdateExportProgress(p);
                });
            });

            // 在后台线程执行导出操作，避免阻塞 UI
            ExportResult result = null;
            await Task.Run(async () =>
            {
                result = await _importExportService.ExportDataAsync(file.Path, progress);
            });

            // 关闭进度对话框（在 UI 线程上）
            progressDialog?.Hide();

            if (result.Success)
            {
                // 显示成功对话框
                await ShowExportSuccessDialogAsync(result);
            }
            else
            {
                // 显示错误对话框
                await _dialogService.ShowErrorAsync($"导出失败：{result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"导出数据失败: {ex.Message}");
            progressDialog?.Hide();
            await _dialogService.ShowErrorAsync("导出数据时发生错误", ex);
        }
        finally
        {
            IsImportExportInProgress = false;
        }
    }

    /// <summary>
    /// 导入数据命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteImportExport))]
    private async Task ImportDataAsync()
    {
        AutoScheduling3.Views.Settings.ImportExportProgressDialog progressDialog = null;
        
        try
        {
            IsImportExportInProgress = true;

            // 使用 FileManagementHelper 显示文件打开对话框
            var file = await FileManagementHelper.ShowOpenFileDialogAsync();
            if (file == null)
            {
                // 用户取消了操作
                return;
            }

            // 验证文件
            var validation = await _importExportService.ValidateImportDataAsync(file.Path);
            if (!validation.IsValid)
            {
                // 显示验证错误
                var errorMessage = "文件验证失败：\n\n";
                foreach (var error in validation.Errors)
                {
                    errorMessage += $"• {error.Message}\n";
                }
                await _dialogService.ShowErrorAsync(errorMessage);
                return;
            }

            // 显示冲突解决策略选择对话框
            var strategy = await ShowConflictResolutionDialogAsync();
            if (!strategy.HasValue)
            {
                // 用户取消了操作
                return;
            }

            // 确认导入
            var confirmed = await _dialogService.ShowConfirmAsync(
                "确认导入",
                "导入操作将修改数据库中的数据。系统会在导入前自动创建备份。\n\n是否继续？",
                "导入",
                "取消"
            );

            if (!confirmed)
            {
                return;
            }

            // 创建并显示进度对话框
            progressDialog = new AutoScheduling3.Views.Settings.ImportExportProgressDialog
            {
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };
            
            // 异步显示对话框（不等待）
            _ = progressDialog.ShowAsync();

            // 创建导入选项
            var options = new ImportOptions
            {
                Strategy = strategy.Value,
                CreateBackupBeforeImport = true,
                ValidateReferences = true,
                ContinueOnError = false
            };

            // 创建进度报告（在 UI 线程上更新）
            var progress = new Progress<ImportProgress>(p =>
            {
                // 确保在 UI 线程上更新进度
                App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    progressDialog?.UpdateImportProgress(p);
                });
            });

            // 在后台线程执行导入操作，避免阻塞 UI
            ImportResult result = null;
            await Task.Run(async () =>
            {
                result = await _importExportService.ImportDataAsync(file.Path, options, progress);
            });

            // 关闭进度对话框（在 UI 线程上）
            progressDialog?.Hide();

            if (result.Success)
            {
                // 显示成功对话框
                await ShowImportSuccessDialogAsync(result);
            }
            else
            {
                // 显示错误对话框
                await _dialogService.ShowErrorAsync($"导入失败：{result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"导入数据失败: {ex.Message}");
            progressDialog?.Hide();
            await _dialogService.ShowErrorAsync("导入数据时发生错误", ex);
        }
        finally
        {
            IsImportExportInProgress = false;
        }
    }

    /// <summary>
    /// 判断是否可以执行导入导出操作
    /// </summary>
    private bool CanExecuteImportExport()
    {
        return !IsImportExportInProgress;
    }

    /// <summary>
    /// 显示导出成功对话框
    /// </summary>
    private async Task ShowExportSuccessDialogAsync(ExportResult result)
    {
        var message = $"数据导出成功！\n\n" +
                      $"文件路径：{result.FilePath}\n" +
                      $"文件大小：{FormatFileSize(result.FileSize)}\n" +
                      $"耗时：{result.Duration.TotalSeconds:F1} 秒\n\n" +
                      $"导出统计：\n" +
                      $"• 技能：{result.Statistics.SkillCount} 条\n" +
                      $"• 人员：{result.Statistics.PersonnelCount} 条\n" +
                      $"• 哨位：{result.Statistics.PositionCount} 条\n" +
                      $"• 模板：{result.Statistics.TemplateCount} 条\n" +
                      $"• 约束：{result.Statistics.ConstraintCount} 条";

        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            Title = "导出成功",
            Content = new Microsoft.UI.Xaml.Controls.StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = message,
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
                    }
                }
            },
            PrimaryButtonText = "打开文件位置",
            CloseButtonText = "关闭",
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Close,
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        var dialogResult = await dialog.ShowAsync();
        if (dialogResult == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
        {
            // 使用 FileManagementHelper 打开文件所在位置
            await FileManagementHelper.OpenFileLocationAsync(result.FilePath);
        }
    }

    /// <summary>
    /// 显示导入成功对话框
    /// </summary>
    private async Task ShowImportSuccessDialogAsync(ImportResult result)
    {
        var message = $"数据导入成功！\n\n" +
                      $"耗时：{result.Duration.TotalSeconds:F1} 秒\n\n" +
                      $"导入统计：\n" +
                      $"• 总记录数：{result.Statistics.TotalRecords} 条\n" +
                      $"• 成功导入：{result.Statistics.ImportedRecords} 条\n" +
                      $"• 跳过记录：{result.Statistics.SkippedRecords} 条\n" +
                      $"• 失败记录：{result.Statistics.FailedRecords} 条";

        if (result.Warnings.Count > 0)
        {
            message += "\n\n警告信息：\n";
            foreach (var warning in result.Warnings)
            {
                message += $"• {warning}\n";
            }
        }

        await _dialogService.ShowSuccessAsync(message);
    }

    /// <summary>
    /// 显示冲突解决策略选择对话框
    /// </summary>
    private async Task<ConflictResolutionStrategy?> ShowConflictResolutionDialogAsync()
    {
        var comboBox = new Microsoft.UI.Xaml.Controls.ComboBox
        {
            Width = 300,
            SelectedIndex = 1 // 默认选择 Skip
        };

        comboBox.Items.Add(new Microsoft.UI.Xaml.Controls.ComboBoxItem
        {
            Content = "覆盖现有数据 (Replace)",
            Tag = ConflictResolutionStrategy.Replace
        });
        comboBox.Items.Add(new Microsoft.UI.Xaml.Controls.ComboBoxItem
        {
            Content = "跳过冲突数据 (Skip) - 推荐",
            Tag = ConflictResolutionStrategy.Skip
        });
        comboBox.Items.Add(new Microsoft.UI.Xaml.Controls.ComboBoxItem
        {
            Content = "合并数据 (Merge)",
            Tag = ConflictResolutionStrategy.Merge
        });

        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            Title = "选择冲突解决策略",
            Content = new Microsoft.UI.Xaml.Controls.StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = "当导入的数据与现有数据冲突时，如何处理？",
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
                    },
                    comboBox,
                    new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = "• 覆盖：删除现有数据并导入新数据\n" +
                               "• 跳过：保留现有数据，跳过冲突的导入数据\n" +
                               "• 合并：保留现有数据，仅导入不冲突的新数据",
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Microsoft.UI.Colors.Gray),
                        FontSize = 12
                    }
                }
            },
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
        {
            var selectedItem = comboBox.SelectedItem as Microsoft.UI.Xaml.Controls.ComboBoxItem;
            return (ConflictResolutionStrategy?)selectedItem?.Tag;
        }

        return null;
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    private string FormatFileSize(long bytes)
    {
        return FileManagementHelper.FormatFileSize(bytes);
    }

    /// <summary>
    /// 清空所有数据库内容命令
    /// </summary>
    [RelayCommand]
    private async Task ClearAllDatabaseAsync()
    {
        try
        {
            // 第一次确认
            var confirmed1 = await _dialogService.ShowConfirmAsync(
                "清空所有数据",
                "此操作将删除所有人员、哨位、技能、排班记录等数据。\n\n系统会在清空前自动创建备份。\n\n是否继续？",
                "继续",
                "取消"
            );

            if (!confirmed1)
            {
                return;
            }

            // 第二次确认（更严格）
            var confirmed2 = await _dialogService.ShowConfirmAsync(
                "最后确认",
                "⚠️ 警告：此操作不可撤销！\n\n所有数据将被永久删除（可从备份恢复）。\n\n确定要清空所有数据吗？",
                "确定清空",
                "取消"
            );

            if (!confirmed2)
            {
                return;
            }

            IsImportExportInProgress = true;

            // 创建备份
            string backupPath;
            try
            {
                backupPath = await _databaseService.CreateBackupAsync();
                System.Diagnostics.Debug.WriteLine($"备份已创建：{backupPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建备份失败: {ex.Message}");
                await _dialogService.ShowErrorAsync("创建备份失败，操作已取消", ex);
                return;
            }

            // 清空数据
            int deletedCount = 0;
            try
            {
                deletedCount = await _databaseService.ClearAllDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清空数据失败: {ex.Message}");
                await _dialogService.ShowErrorAsync("清空数据失败", ex);
                return;
            }

            // 显示成功消息
            await _dialogService.ShowSuccessAsync(
                $"数据清空成功！\n\n共删除 {deletedCount} 条记录。\n\n备份文件已保存，如需恢复请联系管理员。"
            );

            // 刷新存储信息
            await RefreshStorageInfoAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"清空数据库失败: {ex.Message}");
            await _dialogService.ShowErrorAsync("清空数据库时发生错误", ex);
        }
        finally
        {
            IsImportExportInProgress = false;
        }
    }
}
