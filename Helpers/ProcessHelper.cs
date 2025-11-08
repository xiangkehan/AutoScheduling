using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AutoScheduling3.Helpers;

/// <summary>
/// 进程辅助类 - 管理文件系统操作
/// </summary>
public static class ProcessHelper
{
    /// <summary>
    /// 在文件资源管理器中打开目录并选中指定文件
    /// </summary>
    /// <param name="filePath">文件的完整路径</param>
    /// <returns>操作是否成功</returns>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    /// <exception cref="UnauthorizedAccessException">当权限不足时抛出</exception>
    public static async Task<bool> OpenDirectoryAndSelectFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            System.Diagnostics.Debug.WriteLine("ProcessHelper: 文件路径为空");
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        }

        try
        {
            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"ProcessHelper: 文件不存在: {filePath}");
                
                // 如果文件不存在，尝试打开父目录
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    return await OpenDirectoryAsync(directory);
                }
                
                throw new FileNotFoundException("文件不存在，且父目录也不存在", filePath);
            }

            // 使用 explorer.exe 的 /select 参数打开并选中文件
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
            System.Diagnostics.Debug.WriteLine($"ProcessHelper: 成功打开目录并选中文件: {filePath}");
            
            return await Task.FromResult(true);
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessHelper: 权限不足: {ex.Message}");
            throw new UnauthorizedAccessException("权限不足，无法访问该文件或目录", ex);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (DirectoryNotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessHelper: 目录不存在: {ex.Message}");
            throw new DirectoryNotFoundException("目录不存在", ex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessHelper: 打开目录并选中文件失败: {ex.Message}");
            throw new InvalidOperationException("无法打开文件资源管理器", ex);
        }
    }

    /// <summary>
    /// 在文件资源管理器中打开目录
    /// </summary>
    /// <param name="directoryPath">目录的完整路径</param>
    /// <returns>操作是否成功</returns>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
    /// <exception cref="UnauthorizedAccessException">当权限不足时抛出</exception>
    public static async Task<bool> OpenDirectoryAsync(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            System.Diagnostics.Debug.WriteLine("ProcessHelper: 目录路径为空");
            throw new ArgumentException("目录路径不能为空", nameof(directoryPath));
        }

        try
        {
            // 检查目录是否存在
            if (!Directory.Exists(directoryPath))
            {
                System.Diagnostics.Debug.WriteLine($"ProcessHelper: 目录不存在: {directoryPath}");
                throw new DirectoryNotFoundException($"目录不存在: {directoryPath}");
            }

            // 使用 explorer.exe 打开目录
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{directoryPath}\"",
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
            System.Diagnostics.Debug.WriteLine($"ProcessHelper: 成功打开目录: {directoryPath}");
            
            return await Task.FromResult(true);
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessHelper: 权限不足: {ex.Message}");
            throw new UnauthorizedAccessException("权限不足，无法访问该目录", ex);
        }
        catch (DirectoryNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessHelper: 打开目录失败: {ex.Message}");
            throw new InvalidOperationException("无法打开文件资源管理器", ex);
        }
    }
}
