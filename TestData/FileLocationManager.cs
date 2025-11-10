using AutoScheduling3.Data.Logging;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace AutoScheduling3.TestData;

/// <summary>
/// 文件位置管理器，负责管理测试数据文件的存储位置和历史记录（符合WinUI3规范）
/// </summary>
public class FileLocationManager
{
    private const string TestDataFolderName = "TestData";
    private const string RecentFilesKey = "TestDataGenerator_RecentFiles";
    private const int MaxRecentFiles = 20;

    private readonly ILogger _logger;
    private StorageFolder? _testDataFolder;

    /// <summary>
    /// 创建文件位置管理器实例
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public FileLocationManager(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取或创建默认测试数据文件夹 (WinUI3方式)
    /// </summary>
    /// <returns>测试数据文件夹</returns>
    public async Task<StorageFolder> GetTestDataFolderAsync()
    {
        if (_testDataFolder == null)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            _testDataFolder = await localFolder.CreateFolderAsync(
                TestDataFolderName,
                CreationCollisionOption.OpenIfExists);

            _logger.Log($"Test data folder: {_testDataFolder.Path}");
        }
        return _testDataFolder;
    }

    /// <summary>
    /// 生成新的文件名
    /// </summary>
    /// <returns>带时间戳的文件名</returns>
    public string GenerateNewFileName()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        return $"test-data-{timestamp}.json";
    }

    /// <summary>
    /// 创建新的测试数据文件
    /// </summary>
    /// <returns>新创建的StorageFile</returns>
    public async Task<StorageFile> CreateNewTestDataFileAsync()
    {
        var folder = await GetTestDataFolderAsync();
        var fileName = GenerateNewFileName();
        var file = await folder.CreateFileAsync(
            fileName,
            CreationCollisionOption.GenerateUniqueName);

        _logger.Log($"Created test data file: {file.Path}");
        return file;
    }

    /// <summary>
    /// 添加到最近文件列表 (使用FutureAccessList)
    /// </summary>
    /// <param name="file">要添加的文件</param>
    public async Task AddToRecentFilesAsync(StorageFile file)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));

        try
        {
            // 添加到FutureAccessList以保持访问权限
            var token = StorageApplicationPermissions.FutureAccessList.Add(
                file,
                file.Name);

            // 保存到LocalSettings
            var recentFiles = await GetRecentFileTokensAsync();

            var fileInfo = new RecentFileInfo
            {
                Token = token,
                FileName = file.Name,
                FilePath = file.Path,
                GeneratedAt = DateTime.Now
            };

            // 移除重复项
            recentFiles.RemoveAll(f => f.FilePath == file.Path);

            // 添加到列表开头
            recentFiles.Insert(0, fileInfo);

            // 限制列表大小
            if (recentFiles.Count > MaxRecentFiles)
            {
                // 移除超出的项并清理FutureAccessList
                for (int i = MaxRecentFiles; i < recentFiles.Count; i++)
                {
                    try
                    {
                        StorageApplicationPermissions.FutureAccessList.Remove(
                            recentFiles[i].Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to remove token from FutureAccessList: {ex.Message}");
                    }
                }
                recentFiles = recentFiles.Take(MaxRecentFiles).ToList();
            }

            // 保存到LocalSettings
            await SaveRecentFileTokensAsync(recentFiles);

            _logger.Log($"Added to recent files: {file.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to add to recent files: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 获取最近的文件列表
    /// </summary>
    /// <returns>最近生成的文件信息列表</returns>
    public async Task<List<GeneratedFileInfo>> GetRecentTestDataFilesAsync()
    {
        var result = new List<GeneratedFileInfo>();
        var recentFiles = await GetRecentFileTokensAsync();

        foreach (var fileInfo in recentFiles)
        {
            try
            {
                // 尝试从FutureAccessList获取文件
                var file = await StorageApplicationPermissions.FutureAccessList
                    .GetFileAsync(fileInfo.Token);

                var properties = await file.GetBasicPropertiesAsync();

                result.Add(new GeneratedFileInfo
                {
                    FileName = file.Name,
                    FilePath = file.Path,
                    GeneratedAt = fileInfo.GeneratedAt,
                    FileSize = (long)properties.Size,
                    StorageFile = file
                });
            }
            catch (Exception ex)
            {
                // 文件可能已被删除，从列表中移除
                _logger.Log($"File not accessible: {fileInfo.FileName}, removing from list");
                try
                {
                    StorageApplicationPermissions.FutureAccessList.Remove(fileInfo.Token);
                }
                catch
                {
                    // 忽略移除失败
                }
            }
        }

        // 更新列表（移除无效项）
        if (result.Count < recentFiles.Count)
        {
            var validTokens = result.Select(r =>
                recentFiles.First(f => f.FilePath == r.FilePath)).ToList();
            await SaveRecentFileTokensAsync(validTokens);
        }

        return result;
    }

    /// <summary>
    /// 从LocalSettings读取最近文件令牌
    /// </summary>
    private async Task<List<RecentFileInfo>> GetRecentFileTokensAsync()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.TryGetValue(RecentFilesKey, out var value))
            {
                var json = value as string;
                if (!string.IsNullOrEmpty(json))
                {
                    return JsonSerializer.Deserialize<List<RecentFileInfo>>(json)
                        ?? new List<RecentFileInfo>();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to load recent files: {ex.Message}");
        }

        return new List<RecentFileInfo>();
    }

    /// <summary>
    /// 保存最近文件令牌到LocalSettings
    /// </summary>
    private async Task SaveRecentFileTokensAsync(List<RecentFileInfo> recentFiles)
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            var json = JsonSerializer.Serialize(recentFiles);
            settings.Values[RecentFilesKey] = json;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to save recent files: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 清理旧文件
    /// </summary>
    /// <param name="daysToKeep">保留的天数，默认30天</param>
    /// <returns>删除的文件数量</returns>
    public async Task<int> CleanOldFilesAsync(int daysToKeep = 30)
    {
        if (daysToKeep < 0)
            throw new ArgumentException("保留天数不能为负数", nameof(daysToKeep));

        try
        {
            var folder = await GetTestDataFolderAsync();
            var files = await folder.GetFilesAsync();
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

            int deletedCount = 0;
            foreach (var file in files)
            {
                try
                {
                    var properties = await file.GetBasicPropertiesAsync();
                    if (properties.DateModified.DateTime < cutoffDate)
                    {
                        await file.DeleteAsync();
                        deletedCount++;
                        _logger.Log($"Deleted old file: {file.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to delete file {file.Name}: {ex.Message}");
                }
            }

            _logger.Log($"Cleaned {deletedCount} old test data files (older than {daysToKeep} days)");
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to clean old files: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// 最近文件信息（用于LocalSettings存储）
/// </summary>
public class RecentFileInfo
{
    public string Token { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// 生成的文件信息（用于UI显示）
/// </summary>
public class GeneratedFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public long FileSize { get; set; }
    public StorageFile? StorageFile { get; set; }

    /// <summary>
    /// 格式化的文件大小
    /// </summary>
    public string FormattedSize => FormatFileSize(FileSize);

    /// <summary>
    /// 格式化的日期
    /// </summary>
    public string FormattedDate => GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// 导入命令（由ViewModel提供）
    /// </summary>
    public System.Windows.Input.ICommand? ImportCommand { get; set; }

    /// <summary>
    /// 打开位置命令（由ViewModel提供）
    /// </summary>
    public System.Windows.Input.ICommand? OpenLocationCommand { get; set; }

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
}
