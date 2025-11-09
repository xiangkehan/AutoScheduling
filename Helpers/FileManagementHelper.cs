using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 文件管理辅助类 - 提供文件导入导出相关的文件管理功能
/// </summary>
public static class FileManagementHelper
{
    /// <summary>
    /// 获取默认导出路径（用户文档文件夹）
    /// </summary>
    /// <returns>用户文档文件夹的完整路径</returns>
    public static string GetDefaultExportPath()
    {
        try
        {
            // 获取用户文档文件夹路径
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            
            if (string.IsNullOrEmpty(documentsPath))
            {
                // 如果无法获取文档文件夹，使用桌面作为备选
                documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            
            if (string.IsNullOrEmpty(documentsPath))
            {
                // 如果仍然无法获取，使用用户配置文件目录
                documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            
            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 默认导出路径: {documentsPath}");
            return documentsPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 获取默认导出路径失败: {ex.Message}");
            // 返回当前目录作为最后的备选
            return Directory.GetCurrentDirectory();
        }
    }

    /// <summary>
    /// 生成带时间戳的导出文件名
    /// </summary>
    /// <param name="prefix">文件名前缀（默认为 "AutoScheduling_Export"）</param>
    /// <param name="extension">文件扩展名（默认为 ".json"）</param>
    /// <returns>生成的文件名（不包含路径）</returns>
    public static string GenerateExportFileName(string prefix = "AutoScheduling_Export", string extension = ".json")
    {
        try
        {
            // 确保扩展名以点开头
            if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("."))
            {
                extension = "." + extension;
            }
            
            // 生成时间戳格式：yyyyMMdd_HHmmss
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            // 组合文件名
            var fileName = $"{prefix}_{timestamp}{extension}";
            
            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 生成文件名: {fileName}");
            return fileName;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 生成文件名失败: {ex.Message}");
            // 返回一个简单的默认文件名
            return $"{prefix}_backup{extension}";
        }
    }

    /// <summary>
    /// 生成完整的导出文件路径（包含默认路径和时间戳文件名）
    /// </summary>
    /// <param name="prefix">文件名前缀（默认为 "AutoScheduling_Export"）</param>
    /// <param name="extension">文件扩展名（默认为 ".json"）</param>
    /// <returns>完整的文件路径</returns>
    public static string GenerateExportFilePath(string prefix = "AutoScheduling_Export", string extension = ".json")
    {
        try
        {
            var defaultPath = GetDefaultExportPath();
            var fileName = GenerateExportFileName(prefix, extension);
            var fullPath = Path.Combine(defaultPath, fileName);
            
            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 生成完整文件路径: {fullPath}");
            return fullPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 生成完整文件路径失败: {ex.Message}");
            throw new InvalidOperationException("无法生成导出文件路径", ex);
        }
    }

    /// <summary>
    /// 显示文件保存对话框（用于导出）
    /// </summary>
    /// <param name="suggestedFileName">建议的文件名（可选）</param>
    /// <param name="fileTypeDescription">文件类型描述（默认为 "JSON 文件"）</param>
    /// <param name="fileExtension">文件扩展名（默认为 ".json"）</param>
    /// <returns>��户选择的文件，如果取消则返回 null</returns>
    public static async Task<StorageFile?> ShowSaveFileDialogAsync(
        string? suggestedFileName = null,
        string fileTypeDescription = "JSON 文件",
        string fileExtension = ".json")
    {
        try
        {
            var savePicker = new FileSavePicker();
            
            // 获取窗口句柄并初始化
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(savePicker, hwnd);

            // 设置起始位置为文档库
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            
            // 确保扩展名以点开头
            if (!string.IsNullOrEmpty(fileExtension) && !fileExtension.StartsWith("."))
            {
                fileExtension = "." + fileExtension;
            }
            
            // 添加文件类型
            savePicker.FileTypeChoices.Add(fileTypeDescription, new[] { fileExtension });
            
            // 设置建议的文件名
            if (string.IsNullOrEmpty(suggestedFileName))
            {
                suggestedFileName = GenerateExportFileName("AutoScheduling_Export", fileExtension);
            }
            savePicker.SuggestedFileName = suggestedFileName;

            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 显示保存文件对话框，建议文件名: {suggestedFileName}");
            
            // 显示对话框并返回结果
            var file = await savePicker.PickSaveFileAsync();
            
            if (file != null)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 用户选择了文件: {file.Path}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("FileManagementHelper: 用户取消了保存操作");
            }
            
            return file;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 显示保存文件对话框失败: {ex.Message}");
            throw new InvalidOperationException("无法显示文件保存对话框", ex);
        }
    }

    /// <summary>
    /// 显示文件打开对话框（用于导入）
    /// </summary>
    /// <param name="fileTypeDescription">文件类型描述（默认为 "JSON 文件"）</param>
    /// <param name="fileExtension">文件扩展名（默认为 ".json"）</param>
    /// <returns>用户选择的文件，如果取消则返回 null</returns>
    public static async Task<StorageFile?> ShowOpenFileDialogAsync(
        string fileTypeDescription = "JSON 文件",
        string fileExtension = ".json")
    {
        try
        {
            var openPicker = new FileOpenPicker();
            
            // 获取窗口句柄并初始化
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(openPicker, hwnd);

            // 设置视图模式和起始位置
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            
            // 确保扩展名以点开头
            if (!string.IsNullOrEmpty(fileExtension) && !fileExtension.StartsWith("."))
            {
                fileExtension = "." + fileExtension;
            }
            
            // 添加文件类型过滤器
            openPicker.FileTypeFilter.Add(fileExtension);

            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 显示打开文件对话框，文件类型: {fileExtension}");
            
            // 显示对话框并返回结果
            var file = await openPicker.PickSingleFileAsync();
            
            if (file != null)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 用户选择了文件: {file.Path}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("FileManagementHelper: 用户取消了打开操作");
            }
            
            return file;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 显示打开文件对话框失败: {ex.Message}");
            throw new InvalidOperationException("无法显示文件打开对话框", ex);
        }
    }

    /// <summary>
    /// 打开文件所在文件夹并选中该文件
    /// </summary>
    /// <param name="filePath">文件的完整路径</param>
    /// <returns>操作是否成功</returns>
    public static async Task<bool> OpenFileLocationAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                System.Diagnostics.Debug.WriteLine("FileManagementHelper: 文件路径为空");
                throw new ArgumentException("文件路径不能为空", nameof(filePath));
            }

            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 打开文件位置: {filePath}");
            
            // 使用 ProcessHelper 打开文件位置
            return await ProcessHelper.OpenDirectoryAndSelectFileAsync(filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 打开文件位置失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 验证文件路径是否有效
    /// </summary>
    /// <param name="filePath">要验证的文件路径</param>
    /// <returns>如果路径有效则返回 true，否则返回 false</returns>
    public static bool IsValidFilePath(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            // 检查路径是否包含无效字符
            var invalidChars = Path.GetInvalidPathChars();
            if (filePath.IndexOfAny(invalidChars) >= 0)
            {
                return false;
            }

            // 尝试获取完整路径（这会验证路径格式）
            var fullPath = Path.GetFullPath(filePath);
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 确保目录存在，如果不存在则创建
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <returns>操作是否成功</returns>
    public static bool EnsureDirectoryExists(string directoryPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return false;
            }

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 创建目录: {directoryPath}");
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 确保目录存在失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 格式化文件大小为人类可读的格式
    /// </summary>
    /// <param name="bytes">文件大小（字节）</param>
    /// <returns>格式化后的文件大小字符串</returns>
    public static string FormatFileSize(long bytes)
    {
        try
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FileManagementHelper: 格式化文件大小失败: {ex.Message}");
            return $"{bytes} B";
        }
    }
}
