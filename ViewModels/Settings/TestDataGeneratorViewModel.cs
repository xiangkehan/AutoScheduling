using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoScheduling3.TestData;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;
using AutoScheduling3.Data.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls;

namespace AutoScheduling3.ViewModels.Settings;

/// <summary>
/// 测试数据生成器页面的ViewModel
/// </summary>
public partial class TestDataGeneratorViewModel : ObservableObject
{
    private readonly IDataImportExportService _importExportService;
    private readonly FileLocationManager _fileLocationManager;
    private readonly DialogService _dialogService;
    private readonly ILogger _logger;
    private StorageFile? _customOutputFile;

    #region 配置选项

    /// <summary>
    /// 数据规模选项
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> scaleOptions = new()
    {
        "小规模 (快速测试)",
        "中等规模 (默认)",
        "大规模 (压力测试)",
        "自定义"
    };

    /// <summary>
    /// 选中的数据规模
    /// </summary>
    [ObservableProperty]
    private string selectedScale = "中等规模 (默认)";

    /// <summary>
    /// 是否为自定义规模
    /// </summary>
    [ObservableProperty]
    private bool isCustomScale;

    #endregion

    #region 自定义配置

    /// <summary>
    /// 技能数量
    /// </summary>
    [ObservableProperty]
    private int skillCount = 8;

    /// <summary>
    /// 人员数量
    /// </summary>
    [ObservableProperty]
    private int personnelCount = 15;

    /// <summary>
    /// 哨位数量
    /// </summary>
    [ObservableProperty]
    private int positionCount = 10;

    /// <summary>
    /// 模板数量
    /// </summary>
    [ObservableProperty]
    private int templateCount = 3;

    /// <summary>
    /// 定岗规则数量
    /// </summary>
    [ObservableProperty]
    private int fixedAssignmentCount = 5;

    /// <summary>
    /// 手动指定数量
    /// </summary>
    [ObservableProperty]
    private int manualAssignmentCount = 8;

    /// <summary>
    /// 节假日配置数量
    /// </summary>
    [ObservableProperty]
    private int holidayConfigCount = 2;

    /// <summary>
    /// 随机种子
    /// </summary>
    [ObservableProperty]
    private int randomSeed = 42;

    #endregion

    #region 生成选项

    /// <summary>
    /// 自动打开文件位置
    /// </summary>
    [ObservableProperty]
    private bool autoOpenFileLocation = true;

    /// <summary>
    /// 生成后显示数据统计
    /// </summary>
    [ObservableProperty]
    private bool showStatisticsAfterGeneration = true;

    #endregion

    #region 输出文件

    /// <summary>
    /// 输出文件路径
    /// </summary>
    [ObservableProperty]
    private string outputFilePath = string.Empty;

    /// <summary>
    /// 默认输出目录
    /// </summary>
    public string DefaultOutputDirectory => "ApplicationData\\LocalFolder\\TestData";

    #endregion

    #region 最近文件列表

    /// <summary>
    /// 最近生成的文件
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<GeneratedFileInfo> recentFiles = new();

    #endregion

    #region 状态和进度

    /// <summary>
    /// 是否正在生成
    /// </summary>
    [ObservableProperty]
    private bool isGenerating;

    /// <summary>
    /// 状态消息
    /// </summary>
    [ObservableProperty]
    private string statusMessage = string.Empty;

    /// <summary>
    /// 进度百分比
    /// </summary>
    [ObservableProperty]
    private int progressPercentage;

    #endregion

    /// <summary>
    /// 构造函数
    /// </summary>
    public TestDataGeneratorViewModel(
        IDataImportExportService importExportService,
        ILogger logger)
    {
        _importExportService = importExportService ?? throw new ArgumentNullException(nameof(importExportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileLocationManager = new FileLocationManager(_logger);
        _dialogService = new DialogService();

        // 初始化
        _ = InitializeAsync();
    }

    /// <summary>
    /// 异步初始化ViewModel
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // 生成默认文件名
            OutputFilePath = _fileLocationManager.GenerateNewFileName();

            // 加载最近文件
            await LoadRecentFilesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"初始化TestDataGeneratorViewModel失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 当选中的规模改变时
    /// </summary>
    partial void OnSelectedScaleChanged(string value)
    {
        IsCustomScale = value == "自定义";

        // 应用预设配置
        var config = value switch
        {
            "小规模 (快速测试)" => TestDataConfiguration.CreateSmall(),
            "大规模 (压力测试)" => TestDataConfiguration.CreateLarge(),
            "自定义" => null, // 保持当前自定义值
            _ => TestDataConfiguration.CreateDefault()
        };

        if (config != null)
        {
            SkillCount = config.SkillCount;
            PersonnelCount = config.PersonnelCount;
            PositionCount = config.PositionCount;
            TemplateCount = config.TemplateCount;
            FixedAssignmentCount = config.FixedAssignmentCount;
            ManualAssignmentCount = config.ManualAssignmentCount;
            HolidayConfigCount = config.HolidayConfigCount;
            RandomSeed = config.RandomSeed;
        }
    }

    /// <summary>
    /// 加载最近文件列表
    /// </summary>
    private async Task LoadRecentFilesAsync()
    {
        try
        {
            var files = await _fileLocationManager.GetRecentTestDataFilesAsync();
            RecentFiles.Clear();
            foreach (var file in files)
            {
                // 设置命令
                file.ImportCommand = new RelayCommand<GeneratedFileInfo>(async (f) => await ImportFileAsync(f));
                file.OpenLocationCommand = new RelayCommand<GeneratedFileInfo>(async (f) => await OpenFileLocationAsync(f));
                RecentFiles.Add(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"加载最近文件列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建配置对象
    /// </summary>
    private TestDataConfiguration CreateConfiguration()
    {
        return new TestDataConfiguration
        {
            SkillCount = SkillCount,
            PersonnelCount = PersonnelCount,
            PositionCount = PositionCount,
            TemplateCount = TemplateCount,
            FixedAssignmentCount = FixedAssignmentCount,
            ManualAssignmentCount = ManualAssignmentCount,
            HolidayConfigCount = HolidayConfigCount,
            RandomSeed = RandomSeed
        };
    }

    #region 命令

    /// <summary>
    /// 生成测试数据命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        try
        {
            IsGenerating = true;
            StatusMessage = "正在生成测试数据...";
            ProgressPercentage = 0;

            // 创建配置
            var config = CreateConfiguration();

            // 验证配置
            try
            {
                config.Validate();
            }
            catch (ArgumentException ex)
            {
                await _dialogService.ShowErrorAsync($"配置验证失败：{ex.Message}");
                return;
            }

            // 创建生成器
            StatusMessage = "正在初始化生成器...";
            ProgressPercentage = 10;
            var generator = new TestDataGenerator(config);

            // 生成数据
            StatusMessage = "正在生成数据...";
            ProgressPercentage = 30;
            var testData = generator.GenerateTestData();

            // 确定保存位置
            StorageFile file;
            if (_customOutputFile != null)
            {
                file = _customOutputFile;
            }
            else
            {
                // 创建默认文件
                StatusMessage = "正在创建文件...";
                ProgressPercentage = 60;
                file = await _fileLocationManager.CreateNewTestDataFileAsync();
            }

            // 导出到文件
            StatusMessage = "正在保存文件...";
            ProgressPercentage = 70;
            await generator.ExportToStorageFileAsync(file);

            // 添加到最近文件
            await _fileLocationManager.AddToRecentFilesAsync(file);

            // 刷新最近文件列表
            await LoadRecentFilesAsync();

            StatusMessage = "生成完成！";
            ProgressPercentage = 100;

            // 显示成功消息
            if (ShowStatisticsAfterGeneration)
            {
                await ShowSuccessDialogAsync(file, testData);
            }
            else
            {
                await _dialogService.ShowSuccessAsync($"测试数据已生成！\n\n文件：{file.Name}");
            }

            // 可选：打开文件位置
            if (AutoOpenFileLocation)
            {
                try
                {
                    var folder = await file.GetParentAsync();
                    await Windows.System.Launcher.LaunchFolderAsync(folder);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"打开文件位置失败: {ex.Message}");
                }
            }

            // 重置自定义文件
            _customOutputFile = null;
            OutputFilePath = _fileLocationManager.GenerateNewFileName();
        }
        catch (Exception ex)
        {
            StatusMessage = $"生成失败: {ex.Message}";
            _logger.LogError($"生成测试数据失败: {ex.Message}");
            await _dialogService.ShowErrorAsync("生成测试数据时发生错误", ex);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    /// <summary>
    /// 判断是否可以生成
    /// </summary>
    private bool CanGenerate()
    {
        return !IsGenerating;
    }

    /// <summary>
    /// 浏览保存位置命令
    /// </summary>
    [RelayCommand]
    private async Task BrowseAsync()
    {
        try
        {
            var picker = new FileSavePicker();

            // 获取窗口句柄 (WinUI3需要)
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("JSON文件", new List<string> { ".json" });
            picker.SuggestedFileName = _fileLocationManager.GenerateNewFileName();

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                OutputFilePath = file.Name;
                _customOutputFile = file;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"浏览文件位置失败: {ex.Message}");
            await _dialogService.ShowErrorAsync("选择文件位置时发生错误", ex);
        }
    }

    /// <summary>
    /// 导入文件命令
    /// </summary>
    [RelayCommand]
    private async Task ImportFileAsync(GeneratedFileInfo fileInfo)
    {
        _logger.Log($"ImportFileAsync called with fileInfo: {fileInfo?.FileName ?? "null"}");
        _logger.Log($"StorageFile is null: {fileInfo?.StorageFile == null}");
        
        if (fileInfo == null)
        {
            await _dialogService.ShowWarningAsync("文件信息为空");
            return;
        }
        
        if (fileInfo.StorageFile == null)
        {
            _logger.LogError($"StorageFile is null for file: {fileInfo.FileName}");
            await _dialogService.ShowWarningAsync($"无法访问文件：{fileInfo.FileName}\n\n文件可能已被删除或移动。");
            // 刷新列表
            await LoadRecentFilesAsync();
            return;
        }

        try
        {
            // 选择导入策略
            var strategyDialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "选择导入策略",
                Content = new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"即将导入文件：{fileInfo.FileName}\n\n请选择遇到重复记录时的处理策略：",
                            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
                        },
                        new RadioButtons
                        {
                            Header = "冲突解决策略",
                            SelectedIndex = 0,
                            Items =
                            {
                                new RadioButton { Content = "覆盖 - 用新数据替换现有数据", Tag = "Replace" },
                                new RadioButton { Content = "跳过 - 保留现有数据，跳过重复项", Tag = "Skip" },
                                new RadioButton { Content = "合并 - 合并新旧数据", Tag = "Merge" }
                            }
                        }
                    }
                },
                PrimaryButtonText = "导入",
                CloseButtonText = "取消",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };

            var strategyResult = await strategyDialog.ShowAsync();
            if (strategyResult != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                return;
            }

            // 获取选择的策略
            var radioButtons = (strategyDialog.Content as StackPanel)?.Children
                .OfType<RadioButtons>()
                .FirstOrDefault();
            
            var selectedRadio = radioButtons?.Items
                .OfType<RadioButton>()
                .FirstOrDefault(r => r.IsChecked == true);
            
            var strategyTag = selectedRadio?.Tag?.ToString() ?? "Replace";
            var strategy = strategyTag switch
            {
                "Skip" => DTOs.ImportExport.ConflictResolutionStrategy.Skip,
                "Merge" => DTOs.ImportExport.ConflictResolutionStrategy.Merge,
                _ => DTOs.ImportExport.ConflictResolutionStrategy.Replace
            };

            _logger.Log($"Selected import strategy: {strategy}");

            // 使用DataImportExportService导入
            var options = new DTOs.ImportExport.ImportOptions
            {
                Strategy = strategy,
                CreateBackupBeforeImport = true,
                ValidateReferences = true,
                ContinueOnError = false
            };

            var result = await _importExportService.ImportDataAsync(
                fileInfo.FilePath,
                options,
                null);

            if (result.Success)
            {
                await _dialogService.ShowSuccessAsync(
                    $"导入成功！\n\n" +
                    $"总记录数：{result.Statistics.TotalRecords}\n" +
                    $"成功导入：{result.Statistics.ImportedRecords}\n" +
                    $"跳过记录：{result.Statistics.SkippedRecords}");
            }
            else
            {
                await _dialogService.ShowErrorAsync($"导入失败：{result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"导入文件失败: {ex.Message}");
            await _dialogService.ShowErrorAsync("导入文件时发生错误", ex);
        }
    }

    /// <summary>
    /// 打开文件位置命令
    /// </summary>
    [RelayCommand]
    private async Task OpenFileLocationAsync(GeneratedFileInfo fileInfo)
    {
        if (fileInfo?.StorageFile == null)
        {
            await _dialogService.ShowWarningAsync("文件信息无效");
            return;
        }

        try
        {
            var folder = await fileInfo.StorageFile.GetParentAsync();
            await Windows.System.Launcher.LaunchFolderAsync(folder);
        }
        catch (Exception ex)
        {
            _logger.LogError($"打开文件位置失败: {ex.Message}");
            await _dialogService.ShowErrorAsync("打开文件位置时发生错误", ex);
        }
    }

    /// <summary>
    /// 清理旧文件命令
    /// </summary>
    [RelayCommand]
    private async Task CleanOldFilesAsync()
    {
        try
        {
            var confirmed = await _dialogService.ShowConfirmAsync(
                "确认清理",
                "是否清理30天前的旧测试数据文件？",
                "清理",
                "取消"
            );

            if (!confirmed)
            {
                return;
            }

            await _fileLocationManager.CleanOldFilesAsync(30);
            await LoadRecentFilesAsync();

            await _dialogService.ShowSuccessAsync("旧文件已清理");
        }
        catch (Exception ex)
        {
            _logger.LogError($"清理旧文件失败: {ex.Message}");
            await _dialogService.ShowErrorAsync("清理旧文件时发生错误", ex);
        }
    }

    /// <summary>
    /// 刷新最近文件列表命令
    /// </summary>
    [RelayCommand]
    private async Task RefreshRecentFilesAsync()
    {
        await LoadRecentFilesAsync();
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 显示成功对话框
    /// </summary>
    private async Task ShowSuccessDialogAsync(StorageFile file, DTOs.ImportExport.ExportData testData)
    {
        var properties = await file.GetBasicPropertiesAsync();
        var fileSize = (long)properties.Size;

        var message = $"测试数据生成成功！\n\n" +
                      $"文件名：{file.Name}\n" +
                      $"文件大小：{FormatFileSize(fileSize)}\n" +
                      $"保存位置：{file.Path}\n\n" +
                      $"数据统计：\n" +
                      $"• 技能：{testData.Skills?.Count ?? 0} 条\n" +
                      $"• 人员：{testData.Personnel?.Count ?? 0} 条\n" +
                      $"• 哨位：{testData.Positions?.Count ?? 0} 条\n" +
                      $"• 模板：{testData.Templates?.Count ?? 0} 条\n" +
                      $"• 定岗规则：{testData.FixedAssignments?.Count ?? 0} 条\n" +
                      $"• 手动指定：{testData.ManualAssignments?.Count ?? 0} 条\n" +
                      $"• 节假日配置：{testData.HolidayConfigs?.Count ?? 0} 条";

        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            Title = "生成成功",
            Content = new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = message,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
            },
            PrimaryButtonText = "打开文件位置",
            CloseButtonText = "关闭",
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Close,
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
        {
            var folder = await file.GetParentAsync();
            await Windows.System.Launcher.LaunchFolderAsync(folder);
        }
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    #endregion
}
