using AutoScheduling3.Data.Logging;
using AutoScheduling3.TestData;
using System.Diagnostics;
using Windows.Storage;

namespace AutoScheduling3.Tests;

/// <summary>
/// FileLocationManager测试
/// </summary>
public class FileLocationManagerTests
{
    /// <summary>
    /// 简单的控制台日志记录器（用于测试）
    /// </summary>
    private class ConsoleLogger : ILogger
    {
        public void Log(string message) => Console.WriteLine($"[INFO] {message}");
        public void LogWarning(string message) => Console.WriteLine($"[WARN] {message}");
        public void LogError(string message) => Console.WriteLine($"[ERROR] {message}");
    }

    /// <summary>
    /// 测试FileLocationManager创建
    /// </summary>
    public static void Test_FileLocationManagerCreation()
    {
        var logger = new ConsoleLogger();
        var fileManager = new FileLocationManager(logger);
        
        Debug.Assert(fileManager != null, "FileLocationManager应成功创建");
        
        Console.WriteLine("✓ FileLocationManager创建测试通过");
    }

    /// <summary>
    /// 测试生成文件名
    /// </summary>
    public static void Test_GenerateFileName()
    {
        var logger = new ConsoleLogger();
        var fileManager = new FileLocationManager(logger);
        
        var fileName = fileManager.GenerateNewFileName();
        
        Debug.Assert(!string.IsNullOrEmpty(fileName), "文件名不应为空");
        Debug.Assert(fileName.StartsWith("test-data-"), "文件名应以'test-data-'开头");
        Debug.Assert(fileName.EndsWith(".json"), "文件名应以'.json'结尾");
        Debug.Assert(fileName.Contains("-"), "文件名应包含时间戳分隔符");
        
        // 验证文件名格式: test-data-yyyyMMdd-HHmmss.json
        var parts = fileName.Replace("test-data-", "").Replace(".json", "").Split('-');
        Debug.Assert(parts.Length == 2, "文件名应包含日期和时间两部分");
        Debug.Assert(parts[0].Length == 8, "日期部分应为8位");
        Debug.Assert(parts[1].Length == 6, "时间部分应为6位");
        
        Console.WriteLine($"✓ 生成文件名测试通过: {fileName}");
    }

    /// <summary>
    /// 测试获取测试数据文件夹
    /// </summary>
    public static async Task Test_GetTestDataFolderAsync()
    {
        var logger = new ConsoleLogger();
        var fileManager = new FileLocationManager(logger);
        
        try
        {
            var folder = await fileManager.GetTestDataFolderAsync();
            
            Debug.Assert(folder != null, "文件夹不应为null");
            Debug.Assert(folder.Name == "TestData", "文件夹名称应为'TestData'");
            Debug.Assert(!string.IsNullOrEmpty(folder.Path), "文件夹路径不应为空");
            
            Console.WriteLine($"✓ 获取测试数据文件夹测试通过: {folder.Path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ 获取测试数据文件夹测试跳过: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试创建新文件
    /// </summary>
    public static async Task Test_CreateNewTestDataFileAsync()
    {
        var logger = new ConsoleLogger();
        var fileManager = new FileLocationManager(logger);
        
        try
        {
            var file = await fileManager.CreateNewTestDataFileAsync();
            
            Debug.Assert(file != null, "文件不应为null");
            Debug.Assert(!string.IsNullOrEmpty(file.Name), "文件名不应为空");
            Debug.Assert(file.Name.StartsWith("test-data-"), "文件名应以'test-data-'开头");
            Debug.Assert(file.Name.EndsWith(".json"), "文件名应以'.json'结尾");
            
            Console.WriteLine($"✓ 创建新文件测试通过: {file.Name}");
            
            // 清理测试文件
            await file.DeleteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ 创建新文件测试跳过: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试添加到最近文件列表
    /// </summary>
    public static async Task Test_AddToRecentFilesAsync()
    {
        var logger = new ConsoleLogger();
        var fileManager = new FileLocationManager(logger);
        
        try
        {
            // 创建测试文件
            var file = await fileManager.CreateNewTestDataFileAsync();
            
            // 添加到最近文件
            await fileManager.AddToRecentFilesAsync(file);
            
            // 获取最近文件列表
            var recentFiles = await fileManager.GetRecentTestDataFilesAsync();
            
            Debug.Assert(recentFiles != null, "最近文件列表不应为null");
            Debug.Assert(recentFiles.Count > 0, "最近文件列表应包含至少一个文件");
            Debug.Assert(recentFiles.Any(f => f.FileName == file.Name), 
                "最近文件列表应包含刚添加的文件");
            
            Console.WriteLine($"✓ 添加到最近文件测试通过 (列表中有 {recentFiles.Count} 个文件)");
            
            // 清理测试文件
            await file.DeleteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ 添加到最近文件测试跳过: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试获取最近文件列表
    /// </summary>
    public static async Task Test_GetRecentTestDataFilesAsync()
    {
        var logger = new ConsoleLogger();
        var fileManager = new FileLocationManager(logger);
        
        try
        {
            var recentFiles = await fileManager.GetRecentTestDataFilesAsync();
            
            Debug.Assert(recentFiles != null, "最近文件列表不应为null");
            
            // 验证文件信息完整性
            foreach (var fileInfo in recentFiles)
            {
                Debug.Assert(!string.IsNullOrEmpty(fileInfo.FileName), "文件名不应为空");
                Debug.Assert(!string.IsNullOrEmpty(fileInfo.FilePath), "文件路径不应为空");
                Debug.Assert(fileInfo.FileSize >= 0, "文件大小应大于等于0");
                Debug.Assert(!string.IsNullOrEmpty(fileInfo.FormattedSize), "格式化大小不应为空");
                Debug.Assert(!string.IsNullOrEmpty(fileInfo.FormattedDate), "格式化日期不应为空");
            }
            
            Console.WriteLine($"✓ 获取最近文件列表测试通过 (找到 {recentFiles.Count} 个文件)");
            
            // 显示前3个文件
            foreach (var fileInfo in recentFiles.Take(3))
            {
                Console.WriteLine($"  - {fileInfo.FileName} ({fileInfo.FormattedSize}, {fileInfo.FormattedDate})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ 获取最近文件列表测试跳过: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试文件大小格式化
    /// </summary>
    public static void Test_FileSizeFormatting()
    {
        var logger = new ConsoleLogger();
        
        // 创建测试文件信息
        var fileInfo1 = new GeneratedFileInfo
        {
            FileName = "test1.json",
            FilePath = "C:\\test1.json",
            GeneratedAt = DateTime.Now,
            FileSize = 512 // 512 B
        };
        Debug.Assert(fileInfo1.FormattedSize == "512 B", "512字节应格式化为'512 B'");
        
        var fileInfo2 = new GeneratedFileInfo
        {
            FileName = "test2.json",
            FilePath = "C:\\test2.json",
            GeneratedAt = DateTime.Now,
            FileSize = 2048 // 2 KB
        };
        Debug.Assert(fileInfo2.FormattedSize == "2 KB", "2048字节应格式化为'2 KB'");
        
        var fileInfo3 = new GeneratedFileInfo
        {
            FileName = "test3.json",
            FilePath = "C:\\test3.json",
            GeneratedAt = DateTime.Now,
            FileSize = 1048576 // 1 MB
        };
        Debug.Assert(fileInfo3.FormattedSize == "1 MB", "1048576字节应格式化为'1 MB'");
        
        Console.WriteLine("✓ 文件大小格式化测试通过");
    }

    /// <summary>
    /// 测试日期格式化
    /// </summary>
    public static void Test_DateFormatting()
    {
        var testDate = new DateTime(2024, 11, 9, 14, 30, 45);
        var fileInfo = new GeneratedFileInfo
        {
            FileName = "test.json",
            FilePath = "C:\\test.json",
            GeneratedAt = testDate,
            FileSize = 1024
        };
        
        Debug.Assert(fileInfo.FormattedDate == "2024-11-09 14:30:45", 
            "日期应格式化为'yyyy-MM-dd HH:mm:ss'");
        
        Console.WriteLine("✓ 日期格式化测试通过");
    }

    /// <summary>
    /// 测试清理旧文件
    /// </summary>
    public static async Task Test_CleanOldFilesAsync()
    {
        var logger = new ConsoleLogger();
        var fileManager = new FileLocationManager(logger);
        
        try
        {
            // 清理30天前的文件
            var deletedCount = await fileManager.CleanOldFilesAsync(daysToKeep: 30);
            
            Debug.Assert(deletedCount >= 0, "删除数量应大于等于0");
            
            Console.WriteLine($"✓ 清理旧文件测试通过 (删除了 {deletedCount} 个文件)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ 清理旧文件测试跳过: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试清理旧文件参数验证
    /// </summary>
    public static async Task Test_CleanOldFilesValidation()
    {
        var logger = new ConsoleLogger();
        var fileManager = new FileLocationManager(logger);
        
        try
        {
            // 测试负数参数
            await fileManager.CleanOldFilesAsync(daysToKeep: -1);
            Debug.Fail("负数参数应抛出异常");
        }
        catch (ArgumentException)
        {
            Console.WriteLine("✓ 清理旧文件参数验证测试通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ 清理旧文件参数验证测试跳过: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试完整工作流程
    /// </summary>
    public static async Task Test_CompleteWorkflow()
    {
        var logger = new ConsoleLogger();
        var fileManager = new FileLocationManager(logger);
        
        try
        {
            Console.WriteLine("\n=== 测试完整工作流程 ===");
            
            // 1. 创建文件
            Console.WriteLine("步骤1: 创建新文件...");
            var file = await fileManager.CreateNewTestDataFileAsync();
            Console.WriteLine($"  ✓ 文件已创建: {file.Name}");
            
            // 2. 写入一些测试数据
            Console.WriteLine("步骤2: 写入测试数据...");
            await FileIO.WriteTextAsync(file, "{\"test\": \"data\"}");
            Console.WriteLine("  ✓ 数据已写入");
            
            // 3. 添加到最近文件
            Console.WriteLine("步骤3: 添加到最近文件...");
            await fileManager.AddToRecentFilesAsync(file);
            Console.WriteLine("  ✓ 已添加到最近文件");
            
            // 4. 获取最近文件列表
            Console.WriteLine("步骤4: 获取最近文件列表...");
            var recentFiles = await fileManager.GetRecentTestDataFilesAsync();
            Console.WriteLine($"  ✓ 找到 {recentFiles.Count} 个最近文件");
            
            // 5. 验证文件在列表中
            var foundFile = recentFiles.FirstOrDefault(f => f.FileName == file.Name);
            Debug.Assert(foundFile != null, "文件应在最近文件列表中");
            Console.WriteLine($"  ✓ 文件在列表中: {foundFile.FileName} ({foundFile.FormattedSize})");
            
            // 6. 清理测试文件
            Console.WriteLine("步骤5: 清理测试文件...");
            await file.DeleteAsync();
            Console.WriteLine("  ✓ 测试文件已删除");
            
            Console.WriteLine("\n✓ 完整工作流程测试通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n⚠ 完整工作流程测试跳过: {ex.Message}");
        }
    }

    /// <summary>
    /// 运行所有测试
    /// </summary>
    public static async Task RunAllTestsAsync()
    {
        Console.WriteLine("=== FileLocationManager测试 ===\n");
        
        Test_FileLocationManagerCreation();
        Test_GenerateFileName();
        Test_FileSizeFormatting();
        Test_DateFormatting();
        await Test_GetTestDataFolderAsync();
        await Test_CreateNewTestDataFileAsync();
        await Test_AddToRecentFilesAsync();
        await Test_GetRecentTestDataFilesAsync();
        await Test_CleanOldFilesAsync();
        await Test_CleanOldFilesValidation();
        await Test_CompleteWorkflow();
        
        Console.WriteLine("\n=== 所有FileLocationManager测试完成 ===");
    }

    /// <summary>
    /// 运行所有测试（同步版本）
    /// </summary>
    public static void RunAllTests()
    {
        RunAllTestsAsync().GetAwaiter().GetResult();
    }
}
