using AutoScheduling3.Data.Logging;
using AutoScheduling3.TestData;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace AutoScheduling3.Examples;

/// <summary>
/// 测试数据生成器使用示例
/// </summary>
public class GenerateTestDataExample
{
    /// <summary>
    /// 示例1: 使用默认配置生成测试数据
    /// </summary>
    public static async Task Example1_DefaultConfiguration()
    {
        // 创建生成器（使用默认配置）
        var generator = new TestDataGenerator();

        // 生成测试数据
        var testData = generator.GenerateTestData();

        // 导出到文件
        var fileName = $"test-data-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        await generator.ExportToFileAsync(fileName);

        Console.WriteLine($"测试数据已生成并导出到: {fileName}");
        Console.WriteLine($"  - 技能: {testData.Skills.Count}");
        Console.WriteLine($"  - 人员: {testData.Personnel.Count}");
        Console.WriteLine($"  - 哨位: {testData.Positions.Count}");
    }

    /// <summary>
    /// 示例2: 使用小规模配置生成测试数据
    /// </summary>
    public static async Task Example2_SmallConfiguration()
    {
        // 使用小规模预设配置
        var config = TestDataConfiguration.CreateSmall();
        var generator = new TestDataGenerator(config);

        // 生成并导出
        await generator.ExportToFileAsync("test-data-small.json");

        Console.WriteLine("小规模测试数据已生成");
    }

    /// <summary>
    /// 示例3: 使用大规模配置生成测试数据
    /// </summary>
    public static async Task Example3_LargeConfiguration()
    {
        // 使用大规模预设配置
        var config = TestDataConfiguration.CreateLarge();
        var generator = new TestDataGenerator(config);

        // 生成并导出
        await generator.ExportToFileAsync("test-data-large.json");

        Console.WriteLine("大规模测试数据已生成");
    }

    /// <summary>
    /// 示例4: 使用自定义配置生成测试数据
    /// </summary>
    public static async Task Example4_CustomConfiguration()
    {
        // 创建自定义配置
        var config = new TestDataConfiguration
        {
            SkillCount = 10,
            PersonnelCount = 20,
            PositionCount = 15,
            TemplateCount = 4,
            FixedAssignmentCount = 8,
            ManualAssignmentCount = 12,
            HolidayConfigCount = 2,
            RandomSeed = 123 // 使用不同的随机种子
        };

        var generator = new TestDataGenerator(config);
        var testData = generator.GenerateTestData();

        await generator.ExportToFileAsync("test-data-custom.json");

        Console.WriteLine("自定义配置测试数据已生成");
        Console.WriteLine($"元数据: {testData.Metadata.ExportVersion}");
        Console.WriteLine($"导出时间: {testData.Metadata.ExportedAt}");
    }

    /// <summary>
    /// 示例5: 生成数据并检查内容
    /// </summary>
    public static void Example5_InspectGeneratedData()
    {
        var generator = new TestDataGenerator();
        var testData = generator.GenerateTestData();

        // 检查技能
        Console.WriteLine("\n=== 技能列表 ===");
        foreach (var skill in testData.Skills.Take(3))
        {
            Console.WriteLine($"ID: {skill.Id}, 名称: {skill.Name}, 描述: {skill.Description}");
        }

        // 检查人员
        Console.WriteLine("\n=== 人员列表 ===");
        foreach (var person in testData.Personnel.Take(3))
        {
            Console.WriteLine($"ID: {person.Id}, 姓名: {person.Name}");
            Console.WriteLine($"  技能: {string.Join(", ", person.SkillNames)}");
            Console.WriteLine($"  可用: {person.IsAvailable}, 退役: {person.IsRetired}");
        }

        // 检查哨位
        Console.WriteLine("\n=== 哨位列表 ===");
        foreach (var position in testData.Positions.Take(3))
        {
            Console.WriteLine($"ID: {position.Id}, 名称: {position.Name}, 地点: {position.Location}");
            Console.WriteLine($"  所需技能: {string.Join(", ", position.RequiredSkillNames)}");
            Console.WriteLine($"  可用人员数: {position.AvailablePersonnelIds.Count}");
        }
    }

    /// <summary>
    /// 示例6: 使用StorageFile导出（WinUI3方式）
    /// </summary>
    public static async Task Example6_ExportToStorageFile()
    {
        // 创建生成器
        var generator = new TestDataGenerator();

        // 使用ApplicationData.LocalFolder创建文件
        var localFolder = ApplicationData.Current.LocalFolder;
        var testDataFolder = await localFolder.CreateFolderAsync("TestData", CreationCollisionOption.OpenIfExists);
        
        var fileName = $"test-data-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        var file = await testDataFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

        // 导出到StorageFile
        await generator.ExportToStorageFileAsync(file);

        Console.WriteLine($"测试数据已导出到: {file.Path}");
    }

    /// <summary>
    /// 示例7: 生成JSON字符串（不保存到文件）
    /// </summary>
    public static void Example7_GenerateJsonString()
    {
        var generator = new TestDataGenerator(TestDataConfiguration.CreateSmall());
        
        // 生成JSON字符串
        var json = generator.GenerateTestDataAsJson();
        
        Console.WriteLine($"生成的JSON长度: {json.Length} 字符");
        Console.WriteLine($"前100个字符: {json.Substring(0, Math.Min(100, json.Length))}...");
    }

    /// <summary>
    /// 示例8: 使用FileSavePicker让用户选择保存位置（WinUI3方式）
    /// </summary>
    /// <param name="windowHandle">窗口句柄（WinUI3需要）</param>
    public static async Task Example8_ExportWithFilePicker(IntPtr windowHandle)
    {
        var generator = new TestDataGenerator();

        // 创建FileSavePicker
        var picker = new FileSavePicker();
        
        // 初始化窗口句柄（WinUI3需要）
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
        
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("JSON文件", new List<string> { ".json" });
        picker.SuggestedFileName = $"test-data-{DateTime.Now:yyyyMMdd-HHmmss}.json";

        // 让用户选择保存位置
        var file = await picker.PickSaveFileAsync();
        
        if (file != null)
        {
            // 导出到用户选择的文件
            await generator.ExportToStorageFileAsync(file);
            Console.WriteLine($"测试数据已保存到: {file.Path}");
        }
        else
        {
            Console.WriteLine("用户取消了保存操作");
        }
    }

    /// <summary>
    /// 示例9: 使用FileLocationManager管理测试数据文件
    /// </summary>
    public static async Task Example9_UseFileLocationManager(ILogger logger)
    {
        // 创建FileLocationManager
        var fileManager = new FileLocationManager(logger);

        // 创建生成器
        var generator = new TestDataGenerator();

        // 创建新的测试数据文件
        var file = await fileManager.CreateNewTestDataFileAsync();
        Console.WriteLine($"创建文件: {file.Name}");

        // 导出测试数据
        await generator.ExportToStorageFileAsync(file);
        Console.WriteLine("测试数据已导出");

        // 添加到最近文件列表
        await fileManager.AddToRecentFilesAsync(file);
        Console.WriteLine("已添加到最近文件列表");

        // 获取最近文件列表
        var recentFiles = await fileManager.GetRecentTestDataFilesAsync();
        Console.WriteLine($"\n最近生成的文件 ({recentFiles.Count}):");
        foreach (var fileInfo in recentFiles.Take(5))
        {
            Console.WriteLine($"  - {fileInfo.FileName}");
            Console.WriteLine($"    大小: {fileInfo.FormattedSize}");
            Console.WriteLine($"    时间: {fileInfo.FormattedDate}");
        }
    }

    /// <summary>
    /// 示例10: 清理旧的测试数据文件
    /// </summary>
    public static async Task Example10_CleanOldFiles(ILogger logger)
    {
        var fileManager = new FileLocationManager(logger);

        // 清理30天前的文件
        var deletedCount = await fileManager.CleanOldFilesAsync(daysToKeep: 30);
        Console.WriteLine($"已清理 {deletedCount} 个旧文件");

        // 清理7天前的文件（更激进的清理）
        deletedCount = await fileManager.CleanOldFilesAsync(daysToKeep: 7);
        Console.WriteLine($"已清理 {deletedCount} 个旧文件（7天以上）");
    }

    /// <summary>
    /// 示例11: 完整的工作流程 - 生成、保存、管理
    /// </summary>
    public static async Task Example11_CompleteWorkflow(ILogger logger)
    {
        Console.WriteLine("=== 完整的测试数据生成工作流程 ===\n");

        // 1. 创建管理器和生成器
        var fileManager = new FileLocationManager(logger);
        var generator = new TestDataGenerator(TestDataConfiguration.CreateDefault());

        // 2. 生成测试数据
        Console.WriteLine("步骤1: 生成测试数据...");
        var testData = generator.GenerateTestData();
        Console.WriteLine($"  ✓ 生成完成: {testData.Skills.Count} 技能, {testData.Personnel.Count} 人员, {testData.Positions.Count} 哨位");

        // 3. 创建文件并导出
        Console.WriteLine("\n步骤2: 创建文件并导出...");
        var file = await fileManager.CreateNewTestDataFileAsync();
        await generator.ExportToStorageFileAsync(file);
        Console.WriteLine($"  ✓ 已导出到: {file.Name}");

        // 4. 添加到最近文件
        Console.WriteLine("\n步骤3: 添加到最近文件列表...");
        await fileManager.AddToRecentFilesAsync(file);
        Console.WriteLine("  ✓ 已添加到最近文件");

        // 5. 显示最近文件
        Console.WriteLine("\n步骤4: 查看最近文件...");
        var recentFiles = await fileManager.GetRecentTestDataFilesAsync();
        Console.WriteLine($"  ✓ 找到 {recentFiles.Count} 个最近文件");
        foreach (var fileInfo in recentFiles.Take(3))
        {
            Console.WriteLine($"    - {fileInfo.FileName} ({fileInfo.FormattedSize})");
        }

        // 6. 清理旧文件（可选）
        Console.WriteLine("\n步骤5: 清理旧文件...");
        var deletedCount = await fileManager.CleanOldFilesAsync(daysToKeep: 30);
        Console.WriteLine($"  ✓ 清理了 {deletedCount} 个旧文件");

        Console.WriteLine("\n=== 工作流程完成 ===");
    }
}
