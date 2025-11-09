using AutoScheduling3.TestData;
using System.Text.Json;
using Windows.Storage;

namespace AutoScheduling3.TestData;

/// <summary>
/// 简单的测试脚本，用于验证导出功能
/// </summary>
public class TestExportFunctionality
{
    public static async Task RunTestsAsync()
    {
        Console.WriteLine("=== 测试数据导出功能 ===\n");

        // 测试1: 生成JSON字符串
        Console.WriteLine("测试1: 生成JSON字符串...");
        var generator = new TestDataGenerator(TestDataConfiguration.CreateSmall());
        var json = generator.GenerateTestDataAsJson();
        Console.WriteLine($"✓ 成功生成JSON字符串 (长度: {json.Length} 字符)");
        
        // 验证JSON格式
        try
        {
            var testParse = JsonSerializer.Deserialize<object>(json);
            Console.WriteLine("✓ JSON格式有效");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ JSON格式无效: {ex.Message}");
            return;
        }

        // 测试2: 导出到临时文件
        Console.WriteLine("\n测试2: 导出到临时文件...");
        var tempFile = Path.Combine(Path.GetTempPath(), $"test-export-{DateTime.Now:yyyyMMddHHmmss}.json");
        try
        {
            await generator.ExportToFileAsync(tempFile);
            if (File.Exists(tempFile))
            {
                var fileInfo = new FileInfo(tempFile);
                Console.WriteLine($"✓ 成功导出到文件: {tempFile}");
                Console.WriteLine($"  文件大小: {fileInfo.Length} 字节");
                
                // 验证文件内容
                var fileContent = await File.ReadAllTextAsync(tempFile);
                if (fileContent.Contains("\"skills\"") && fileContent.Contains("\"personnel\""))
                {
                    Console.WriteLine("✓ 文件内容验证通过");
                }
                
                // 清理
                File.Delete(tempFile);
                Console.WriteLine("✓ 临时文件已清理");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 文件导出失败: {ex.Message}");
        }

        // 测试3: 导出到StorageFile
        Console.WriteLine("\n测试3: 导出到StorageFile...");
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var testFolder = await localFolder.CreateFolderAsync("TestExports", 
                CreationCollisionOption.OpenIfExists);
            
            var fileName = $"test-export-{DateTime.Now:yyyyMMddHHmmss}.json";
            var file = await testFolder.CreateFileAsync(fileName, 
                CreationCollisionOption.ReplaceExisting);
            
            await generator.ExportToStorageFileAsync(file);
            
            var properties = await file.GetBasicPropertiesAsync();
            Console.WriteLine($"✓ 成功导出到StorageFile: {file.Path}");
            Console.WriteLine($"  文件大小: {properties.Size} 字节");
            
            // 验证文件内容
            var storageContent = await FileIO.ReadTextAsync(file);
            if (storageContent.Contains("\"skills\"") && storageContent.Contains("\"personnel\""))
            {
                Console.WriteLine("✓ StorageFile内容验证通过");
            }
            
            // 清理
            await file.DeleteAsync();
            Console.WriteLine("✓ 测试文件已清理");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ StorageFile导出失败: {ex.Message}");
        }

        // 测试4: 验证JSON序列化选项
        Console.WriteLine("\n测试4: 验证JSON序列化选项...");
        var testJson = generator.GenerateTestDataAsJson();
        
        // 检查camelCase命名
        if (testJson.Contains("\"skills\"") && !testJson.Contains("\"Skills\""))
        {
            Console.WriteLine("✓ 使用camelCase命名策略");
        }
        
        // 检查格式化输出
        if (testJson.Contains("\n") && testJson.Contains("  "))
        {
            Console.WriteLine("✓ 使用格式化输出（缩进）");
        }
        
        // 检查必需字段
        if (testJson.Contains("\"metadata\"") && 
            testJson.Contains("\"exportVersion\"") &&
            testJson.Contains("\"statistics\""))
        {
            Console.WriteLine("✓ 包含所有必需字段");
        }

        Console.WriteLine("\n=== 所有导出功能测试完成 ===");
    }
}
