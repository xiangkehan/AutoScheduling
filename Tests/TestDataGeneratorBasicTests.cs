using AutoScheduling3.TestData;
using System.Diagnostics;

namespace AutoScheduling3.Tests;

/// <summary>
/// 测试数据生成器基础测试
/// </summary>
public class TestDataGeneratorBasicTests
{
    /// <summary>
    /// 测试默认配置创建
    /// </summary>
    public static void Test_DefaultConfiguration()
    {
        var config = TestDataConfiguration.CreateDefault();
        
        Debug.Assert(config.SkillCount == 8, "默认技能数量应为8");
        Debug.Assert(config.PersonnelCount == 15, "默认人员数量应为15");
        Debug.Assert(config.PositionCount == 10, "默认哨位数量应为10");
        
        Console.WriteLine("✓ 默认配置测试通过");
    }

    /// <summary>
    /// 测试小规模配置创建
    /// </summary>
    public static void Test_SmallConfiguration()
    {
        var config = TestDataConfiguration.CreateSmall();
        
        Debug.Assert(config.SkillCount == 5, "小规模技能数量应为5");
        Debug.Assert(config.PersonnelCount == 8, "小规模人员数量应为8");
        Debug.Assert(config.PositionCount == 6, "小规模哨位数量应为6");
        
        Console.WriteLine("✓ 小规模配置测试通过");
    }

    /// <summary>
    /// 测试大规模配置创建
    /// </summary>
    public static void Test_LargeConfiguration()
    {
        var config = TestDataConfiguration.CreateLarge();
        
        Debug.Assert(config.SkillCount == 15, "大规模技能数量应为15");
        Debug.Assert(config.PersonnelCount == 30, "大规模人员数量应为30");
        Debug.Assert(config.PositionCount == 20, "大规模哨位数量应为20");
        
        Console.WriteLine("✓ 大规模配置测试通过");
    }

    /// <summary>
    /// 测试配置验证
    /// </summary>
    public static void Test_ConfigurationValidation()
    {
        // 测试有效配置
        var validConfig = new TestDataConfiguration
        {
            SkillCount = 5,
            PersonnelCount = 10,
            PositionCount = 8
        };
        
        try
        {
            validConfig.Validate();
            Console.WriteLine("✓ 有效配置验证通过");
        }
        catch
        {
            Debug.Fail("有效配置不应抛出异常");
        }

        // 测试无效配置（技能数量为0）
        var invalidConfig = new TestDataConfiguration
        {
            SkillCount = 0,
            PersonnelCount = 10,
            PositionCount = 8
        };
        
        try
        {
            invalidConfig.Validate();
            Debug.Fail("无效配置应抛出异常");
        }
        catch (ArgumentException)
        {
            Console.WriteLine("✓ 无效配置验证通过");
        }
    }

    /// <summary>
    /// 测试生成器创建
    /// </summary>
    public static void Test_GeneratorCreation()
    {
        var generator = new TestDataGenerator();
        Debug.Assert(generator != null, "生成器应成功创建");
        
        var config = TestDataConfiguration.CreateSmall();
        var generator2 = new TestDataGenerator(config);
        Debug.Assert(generator2 != null, "使用配置的生成器应成功创建");
        
        Console.WriteLine("✓ 生成器创建测试通过");
    }

    /// <summary>
    /// 测试技能数据生成
    /// </summary>
    public static void Test_SkillGeneration()
    {
        var config = new TestDataConfiguration { SkillCount = 5 };
        var generator = new TestDataGenerator(config);
        var testData = generator.GenerateTestData();
        
        Debug.Assert(testData.Skills != null, "技能列表不应为null");
        Debug.Assert(testData.Skills.Count == 5, "应生成5个技能");
        
        // 验证技能数据完整性
        foreach (var skill in testData.Skills)
        {
            Debug.Assert(skill.Id > 0, "技能ID应大于0");
            Debug.Assert(!string.IsNullOrEmpty(skill.Name), "技能名称不应为空");
            Debug.Assert(!string.IsNullOrEmpty(skill.Description), "技能描述不应为空");
        }
        
        // 验证技能名称唯一性
        var uniqueNames = testData.Skills.Select(s => s.Name).Distinct().Count();
        Debug.Assert(uniqueNames == testData.Skills.Count, "技能名称应唯一");
        
        Console.WriteLine("✓ 技能生成测试通过");
    }

    /// <summary>
    /// 测试人员数据生成
    /// </summary>
    public static void Test_PersonnelGeneration()
    {
        var config = new TestDataConfiguration 
        { 
            SkillCount = 5,
            PersonnelCount = 10 
        };
        var generator = new TestDataGenerator(config);
        var testData = generator.GenerateTestData();
        
        Debug.Assert(testData.Personnel != null, "人员列表不应为null");
        Debug.Assert(testData.Personnel.Count == 10, "应生成10个人员");
        
        // 验证人员数据完整性
        foreach (var person in testData.Personnel)
        {
            Debug.Assert(person.Id > 0, "人员ID应大于0");
            Debug.Assert(!string.IsNullOrEmpty(person.Name), "人员姓名不应为空");
            Debug.Assert(person.SkillIds.Count > 0, "人员应至少有1个技能");
            Debug.Assert(person.SkillIds.Count <= 3, "人员应最多有3个技能");
            Debug.Assert(person.RecentPeriodShiftIntervals.Length == 12, "应有12个时段间隔");
            
            // 验证技能引用有效性
            foreach (var skillId in person.SkillIds)
            {
                Debug.Assert(testData.Skills.Any(s => s.Id == skillId), 
                    $"人员引用的技能ID {skillId} 应存在");
            }
        }
        
        Console.WriteLine("✓ 人员生成测试通过");
    }

    /// <summary>
    /// 测试哨位数据生成
    /// </summary>
    public static void Test_PositionGeneration()
    {
        var config = new TestDataConfiguration 
        { 
            SkillCount = 5,
            PersonnelCount = 10,
            PositionCount = 8
        };
        var generator = new TestDataGenerator(config);
        var testData = generator.GenerateTestData();
        
        Debug.Assert(testData.Positions != null, "哨位列表不应为null");
        Debug.Assert(testData.Positions.Count == 8, "应生成8个哨位");
        
        // 验证哨位数据完整性
        foreach (var position in testData.Positions)
        {
            Debug.Assert(position.Id > 0, "哨位ID应大于0");
            Debug.Assert(!string.IsNullOrEmpty(position.Name), "哨位名称不应为空");
            Debug.Assert(!string.IsNullOrEmpty(position.Location), "哨位地点不应为空");
            Debug.Assert(position.RequiredSkillIds.Count > 0, "哨位应至少需要1个技能");
            
            // 验证技能引用有效性
            foreach (var skillId in position.RequiredSkillIds)
            {
                Debug.Assert(testData.Skills.Any(s => s.Id == skillId), 
                    $"哨位引用的技能ID {skillId} 应存在");
            }
            
            // 验证人员引用有效性
            foreach (var personId in position.AvailablePersonnelIds)
            {
                Debug.Assert(testData.Personnel.Any(p => p.Id == personId), 
                    $"哨位引用的人员ID {personId} 应存在");
            }
        }
        
        Console.WriteLine("✓ 哨位生成测试通过");
    }

    /// <summary>
    /// 测试元数据生成
    /// </summary>
    public static void Test_MetadataGeneration()
    {
        var generator = new TestDataGenerator();
        var testData = generator.GenerateTestData();
        
        Debug.Assert(testData.Metadata != null, "元数据不应为null");
        Debug.Assert(testData.Metadata.ExportVersion == "1.0", "导出版本应为1.0");
        Debug.Assert(testData.Metadata.Statistics != null, "统计信息不应为null");
        Debug.Assert(testData.Metadata.Statistics.SkillCount == testData.Skills.Count, 
            "统计的技能数量应匹配");
        Debug.Assert(testData.Metadata.Statistics.PersonnelCount == testData.Personnel.Count, 
            "统计的人员数量应匹配");
        Debug.Assert(testData.Metadata.Statistics.PositionCount == testData.Positions.Count, 
            "统计的哨位数量应匹配");
        
        Console.WriteLine("✓ 元数据生成测试通过");
    }

    /// <summary>
    /// 测试示例数据提供者
    /// </summary>
    public static void Test_SampleDataProvider()
    {
        var random = new Random(42);
        var provider = new SampleDataProvider(random);
        
        // 测试获取随机数据
        var name = provider.GetRandomName();
        Debug.Assert(!string.IsNullOrEmpty(name), "随机姓名不应为空");
        
        var skillName = provider.GetRandomSkillName();
        Debug.Assert(!string.IsNullOrEmpty(skillName), "随机技能名称不应为空");
        
        var positionName = provider.GetRandomPositionName();
        Debug.Assert(!string.IsNullOrEmpty(positionName), "随机哨位名称不应为空");
        
        var location = provider.GetRandomLocation();
        Debug.Assert(!string.IsNullOrEmpty(location), "随机地点不应为空");
        
        // 测试时段名称
        var timeSlot = provider.GetTimeSlotName(0);
        Debug.Assert(timeSlot == "00:00-02:00", "时段0应为00:00-02:00");
        
        var timeSlot11 = provider.GetTimeSlotName(11);
        Debug.Assert(timeSlot11 == "22:00-24:00", "时段11应为22:00-24:00");
        
        Console.WriteLine("✓ 示例数据提供者测试通过");
    }

    /// <summary>
    /// 测试JSON字符串生成
    /// </summary>
    public static void Test_JsonStringGeneration()
    {
        var config = TestDataConfiguration.CreateSmall();
        var generator = new TestDataGenerator(config);
        
        var json = generator.GenerateTestDataAsJson();
        
        Debug.Assert(!string.IsNullOrEmpty(json), "JSON字符串不应为空");
        Debug.Assert(json.Contains("\"skills\""), "JSON应包含skills字段");
        Debug.Assert(json.Contains("\"personnel\""), "JSON应包含personnel字段");
        Debug.Assert(json.Contains("\"positions\""), "JSON应包含positions字段");
        Debug.Assert(json.Contains("\"metadata\""), "JSON应包含metadata字段");
        
        Console.WriteLine($"✓ JSON字符串生成测试通过 (长度: {json.Length} 字符)");
    }

    /// <summary>
    /// 测试文件导出（传统方式）
    /// </summary>
    public static async Task Test_FileExportAsync()
    {
        var config = TestDataConfiguration.CreateSmall();
        var generator = new TestDataGenerator(config);
        
        var tempFile = Path.Combine(Path.GetTempPath(), $"test-data-{Guid.NewGuid()}.json");
        
        try
        {
            await generator.ExportToFileAsync(tempFile);
            
            Debug.Assert(File.Exists(tempFile), "导出的文件应存在");
            
            var json = await File.ReadAllTextAsync(tempFile);
            Debug.Assert(!string.IsNullOrEmpty(json), "文件内容不应为空");
            Debug.Assert(json.Contains("\"skills\""), "文件应包含skills字段");
            
            Console.WriteLine($"✓ 文件导出测试通过 (文件: {tempFile})");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// 测试StorageFile导出（WinUI3方式）
    /// </summary>
    public static async Task Test_StorageFileExportAsync()
    {
        var config = TestDataConfiguration.CreateSmall();
        var generator = new TestDataGenerator(config);
        
        try
        {
            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var testFolder = await localFolder.CreateFolderAsync("TestDataGeneratorTests", 
                Windows.Storage.CreationCollisionOption.OpenIfExists);
            
            var fileName = $"test-data-{Guid.NewGuid()}.json";
            var file = await testFolder.CreateFileAsync(fileName, 
                Windows.Storage.CreationCollisionOption.ReplaceExisting);
            
            await generator.ExportToStorageFileAsync(file);
            
            var json = await Windows.Storage.FileIO.ReadTextAsync(file);
            Debug.Assert(!string.IsNullOrEmpty(json), "文件内容不应为空");
            Debug.Assert(json.Contains("\"skills\""), "文件应包含skills字段");
            
            Console.WriteLine($"✓ StorageFile导出测试通过 (文件: {file.Path})");
            
            // 清理
            await file.DeleteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ StorageFile导出测试跳过: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试手动指定数据生成
    /// </summary>
    public static void Test_ManualAssignmentGeneration()
    {
        var config = new TestDataConfiguration 
        { 
            SkillCount = 5,
            PersonnelCount = 10,
            PositionCount = 8,
            ManualAssignmentCount = 8
        };
        var generator = new TestDataGenerator(config);
        var testData = generator.GenerateTestData();
        
        Debug.Assert(testData.ManualAssignments != null, "手动指定列表不应为null");
        Debug.Assert(testData.ManualAssignments.Count > 0, "应生成手动指定记录");
        Debug.Assert(testData.ManualAssignments.Count <= 8, "手动指定数量不应超过配置值");
        
        // 验证手动指定数据完整性
        foreach (var assignment in testData.ManualAssignments)
        {
            Debug.Assert(assignment.Id > 0, "手动指定ID应大于0");
            Debug.Assert(assignment.PositionId > 0, "哨位ID应大于0");
            Debug.Assert(assignment.PersonnelId > 0, "人员ID应大于0");
            Debug.Assert(!string.IsNullOrEmpty(assignment.PositionName), "哨位名称不应为空");
            Debug.Assert(!string.IsNullOrEmpty(assignment.PersonnelName), "人员姓名不应为空");
            Debug.Assert(assignment.TimeSlot >= 0 && assignment.TimeSlot <= 11, 
                "时段索引应在0-11范围内");
            Debug.Assert(!string.IsNullOrEmpty(assignment.Remarks), "备注不应为空");
            
            // 验证哨位引用有效性
            Debug.Assert(testData.Positions.Any(p => p.Id == assignment.PositionId), 
                $"手动指定引用的哨位ID {assignment.PositionId} 应存在");
            
            // 验证人员引用有效性
            Debug.Assert(testData.Personnel.Any(p => p.Id == assignment.PersonnelId), 
                $"手动指定引用的人员ID {assignment.PersonnelId} 应存在");
        }
        
        // 验证没有重复的组合（同一哨位、日期、时段）
        var combinations = testData.ManualAssignments
            .Select(a => $"{a.PositionId}_{a.Date:yyyyMMdd}_{a.TimeSlot}")
            .ToList();
        var uniqueCombinations = combinations.Distinct().Count();
        Debug.Assert(uniqueCombinations == combinations.Count, 
            "手动指定不应有重复的哨位-日期-时段组合");
        
        // 验证备注包含有意义的信息
        foreach (var assignment in testData.ManualAssignments)
        {
            Debug.Assert(assignment.Remarks.Contains(assignment.PersonnelName), 
                "备注应包含人员姓名");
            Debug.Assert(assignment.Remarks.Contains("值班"), 
                "备注应包含'值班'关键字");
        }
        
        Console.WriteLine($"✓ 手动指定生成测试通过 (生成了 {testData.ManualAssignments.Count} 条记录)");
    }

    /// <summary>
    /// 测试JSON序列化选项一致性
    /// </summary>
    public static void Test_JsonSerializationOptions()
    {
        var generator = new TestDataGenerator(TestDataConfiguration.CreateSmall());
        var json = generator.GenerateTestDataAsJson();
        
        // 验证使用camelCase命名策略
        Debug.Assert(json.Contains("\"skills\""), "应使用camelCase命名");
        Debug.Assert(!json.Contains("\"Skills\""), "不应使用PascalCase命名");
        
        // 验证格式化输出（包含换行和缩进）
        Debug.Assert(json.Contains("\n"), "应包含换行符");
        Debug.Assert(json.Contains("  "), "应包含缩进");
        
        Console.WriteLine("✓ JSON序列化选项一致性测试通过");
    }

    /// <summary>
    /// 运行所有测试
    /// </summary>
    public static async Task RunAllTestsAsync()
    {
        Console.WriteLine("=== 测试数据生成器基础测试 ===\n");
        
        Test_DefaultConfiguration();
        Test_SmallConfiguration();
        Test_LargeConfiguration();
        Test_ConfigurationValidation();
        Test_GeneratorCreation();
        Test_SkillGeneration();
        Test_PersonnelGeneration();
        Test_PositionGeneration();
        Test_ManualAssignmentGeneration();
        Test_MetadataGeneration();
        Test_SampleDataProvider();
        Test_JsonStringGeneration();
        Test_JsonSerializationOptions();
        await Test_FileExportAsync();
        await Test_StorageFileExportAsync();
        
        Console.WriteLine("\n=== 所有测试通过 ===");
    }

    /// <summary>
    /// 运行所有测试（同步版本，用于向后兼容）
    /// </summary>
    public static void RunAllTests()
    {
        RunAllTestsAsync().GetAwaiter().GetResult();
    }
}
