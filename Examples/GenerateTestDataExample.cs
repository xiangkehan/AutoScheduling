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
    /// 示例2.1: 使用演练场景配置（高可用率、低退役率）
    /// </summary>
    public static async Task Example2_1_DrillScenario()
    {
        // 使用演练场景配置：95%可用率，5%退役率
        var config = TestDataConfiguration.CreateDrillScenario();
        var generator = new TestDataGenerator(config);

        var testData = generator.GenerateTestData();
        await generator.ExportToFileAsync("test-data-drill.json");

        Console.WriteLine("演练场景测试数据已生成");
        Console.WriteLine($"  - 人员可用率: {config.PersonnelAvailabilityRate:P0}");
        Console.WriteLine($"  - 人员退役率: {config.PersonnelRetirementRate:P0}");
        Console.WriteLine($"  - 每哨位最小人员: {config.MinPersonnelPerPosition}");
    }

    /// <summary>
    /// 示例2.2: 使用实战场景配置（较低可用率、较高退役率）
    /// </summary>
    public static async Task Example2_2_CombatScenario()
    {
        // 使用实战场景配置：75%可用率，15%退役率
        var config = TestDataConfiguration.CreateCombatScenario();
        var generator = new TestDataGenerator(config);

        var testData = generator.GenerateTestData();
        await generator.ExportToFileAsync("test-data-combat.json");

        Console.WriteLine("实战场景测试数据已生成");
        Console.WriteLine($"  - 人员可用率: {config.PersonnelAvailabilityRate:P0}");
        Console.WriteLine($"  - 人员退役率: {config.PersonnelRetirementRate:P0}");
        Console.WriteLine($"  - 每哨位最小人员: {config.MinPersonnelPerPosition}");
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
            MinPersonnelPerPosition = 4,           // 每个哨位至少4个可用人员
            PersonnelAvailabilityRate = 0.80,     // 80%的人员可用
            PersonnelRetirementRate = 0.12,       // 12%的人员已退役
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
    /// 示例4.1: 配置验证示例
    /// </summary>
    public static void Example4_1_ConfigurationValidation()
    {
        // 创建一个可能有问题的配置
        var config = new TestDataConfiguration
        {
            PersonnelCount = 10,
            PositionCount = 5,
            MinPersonnelPerPosition = 3,
            PersonnelAvailabilityRate = 0.70,
            PersonnelRetirementRate = 0.20
        };

        // 验证配置
        var (isValid, errors, warnings) = config.ValidateWithResult();

        Console.WriteLine("=== 配置验证结果 ===");
        Console.WriteLine($"配置有效: {isValid}");

        if (errors.Any())
        {
            Console.WriteLine("\n错误:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  ❌ {error}");
            }
        }

        if (warnings.Any())
        {
            Console.WriteLine("\n警告:");
            foreach (var warning in warnings)
            {
                Console.WriteLine($"  ⚠️  {warning}");
            }
        }

        if (isValid && !warnings.Any())
        {
            Console.WriteLine("\n✓ 配置完全正常，可以使用");
        }
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
    /// 示例5.1: 检查智能技能分配的效果
    /// </summary>
    public static void Example5_1_InspectSkillAssignment()
    {
        var config = TestDataConfiguration.CreateDefault();
        var generator = new TestDataGenerator(config);
        var testData = generator.GenerateTestData();

        Console.WriteLine("=== 智能技能分配效果检查 ===\n");

        // 统计人员技能数量分布
        var skillCountDistribution = testData.Personnel
            .GroupBy(p => p.SkillIds.Count)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        Console.WriteLine("人员技能数量分布:");
        foreach (var (skillCount, personCount) in skillCountDistribution)
        {
            var percentage = (double)personCount / testData.Personnel.Count * 100;
            Console.WriteLine($"  {skillCount}个技能: {personCount}人 ({percentage:F1}%)");
        }

        // 检查每个哨位的可用人员数量
        Console.WriteLine("\n哨位可用人员数量:");
        var minAvailable = testData.Positions.Min(p => p.AvailablePersonnelIds.Count);
        var maxAvailable = testData.Positions.Max(p => p.AvailablePersonnelIds.Count);
        var avgAvailable = testData.Positions.Average(p => p.AvailablePersonnelIds.Count);

        Console.WriteLine($"  最少: {minAvailable}人");
        Console.WriteLine($"  最多: {maxAvailable}人");
        Console.WriteLine($"  平均: {avgAvailable:F1}人");
        Console.WriteLine($"  配置要求: 至少{config.MinPersonnelPerPosition}人");

        // 检查是否所有哨位都满足最小人员要求
        var unsatisfiedPositions = testData.Positions
            .Where(p => p.AvailablePersonnelIds.Count < config.MinPersonnelPerPosition)
            .ToList();

        if (unsatisfiedPositions.Any())
        {
            Console.WriteLine($"\n⚠️  警告: {unsatisfiedPositions.Count}个哨位未满足最小人员要求");
        }
        else
        {
            Console.WriteLine($"\n✓ 所有哨位都满足最小人员要求");
        }

        // 显示多技能人员示例
        Console.WriteLine("\n多技能人员示例:");
        var multiSkilledPersonnel = testData.Personnel
            .Where(p => p.SkillIds.Count >= 2)
            .Take(3);

        foreach (var person in multiSkilledPersonnel)
        {
            Console.WriteLine($"  {person.Name}: {string.Join(", ", person.SkillNames)}");
            
            // 找出该人员可以满足的哨位
            var satisfiedPositions = testData.Positions
                .Where(pos => pos.RequiredSkillIds.All(skillId => person.SkillIds.Contains(skillId)))
                .Select(pos => pos.Name)
                .ToList();
            
            Console.WriteLine($"    可满足哨位: {string.Join(", ", satisfiedPositions)}");
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
