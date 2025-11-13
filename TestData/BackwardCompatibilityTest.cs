using AutoScheduling3.DTOs.ImportExport;
using System.Reflection;
using Windows.Storage;

namespace AutoScheduling3.TestData;

/// <summary>
/// 验证重构后的 TestDataGenerator 向后兼容性
/// </summary>
public static class BackwardCompatibilityTest
{
    public static void Run()
    {
        Console.WriteLine("=== 开始向后兼容性验证 ===\n");
        
        bool allTestsPassed = true;
        
        // 测试 1: 验证构造函数签名
        allTestsPassed &= VerifyConstructorSignatures();
        
        // 测试 2: 验证公共方法签名
        allTestsPassed &= VerifyPublicMethodSignatures();
        
        // 测试 3: 验证默认构造函数功能
        allTestsPassed &= VerifyDefaultConstructor();
        
        // 测试 4: 验证配置构造函数功能
        allTestsPassed &= VerifyConfigConstructor();
        
        // 测试 5: 验证 GenerateTestData 功能
        allTestsPassed &= VerifyGenerateTestData();
        
        // 测试 6: 验证 GenerateTestDataAsJson 功能
        allTestsPassed &= VerifyGenerateTestDataAsJson();
        
        // 测试 7: 验证数据完整性
        allTestsPassed &= VerifyDataIntegrity();
        
        // 测试 8: 验证数据格式一致性
        allTestsPassed &= VerifyDataFormat();
        
        Console.WriteLine("\n=== 向后兼容性验证完成 ===");
        Console.WriteLine($"总体结果: {(allTestsPassed ? "✓ 全部通过" : "✗ 存在失败")}");
    }
    
    private static bool VerifyConstructorSignatures()
    {
        Console.WriteLine("测试 1: 验证构造函数签名");
        Console.WriteLine(new string('-', 60));
        
        try
        {
            var type = typeof(TestDataGenerator);
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            
            // 应该有两个公共构造函数
            if (constructors.Length != 2)
            {
                Console.WriteLine($"✗ 失败: 期望 2 个构造函数，实际 {constructors.Length} 个");
                return false;
            }
            
            // 验证无参构造函数
            var defaultCtor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
            if (defaultCtor == null)
            {
                Console.WriteLine("✗ 失败: 缺少无参构造函数");
                return false;
            }
            Console.WriteLine("✓ 无参构造函数存在");
            
            // 验证带配置参数的构造函数
            var configCtor = constructors.FirstOrDefault(c => 
                c.GetParameters().Length == 1 && 
                c.GetParameters()[0].ParameterType == typeof(TestDataConfiguration));
            if (configCtor == null)
            {
                Console.WriteLine("✗ 失败: 缺少 TestDataConfiguration 参数的构造函数");
                return false;
            }
            Console.WriteLine("✓ TestDataConfiguration 构造函数存在");
            
            Console.WriteLine("✓ 测试通过\n");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 测试异常: {ex.Message}\n");
            return false;
        }
    }
    
    private static bool VerifyPublicMethodSignatures()
    {
        Console.WriteLine("测试 2: 验证公共方法签名");
        Console.WriteLine(new string('-', 60));
        
        try
        {
            var type = typeof(TestDataGenerator);
            
            // 验证 GenerateTestData 方法
            var generateMethod = type.GetMethod("GenerateTestData", 
                BindingFlags.Public | BindingFlags.Instance,
                null, Type.EmptyTypes, null);
            if (generateMethod == null || generateMethod.ReturnType != typeof(ExportData))
            {
                Console.WriteLine("✗ 失败: GenerateTestData() 方法签名不正确");
                return false;
            }
            Console.WriteLine("✓ GenerateTestData() 方法签名正确");
            
            // 验证 ExportToFileAsync 方法
            var exportFileMethod = type.GetMethod("ExportToFileAsync",
                BindingFlags.Public | BindingFlags.Instance,
                null, new[] { typeof(string) }, null);
            if (exportFileMethod == null || exportFileMethod.ReturnType != typeof(Task))
            {
                Console.WriteLine("✗ 失败: ExportToFileAsync(string) 方法签名不正确");
                return false;
            }
            Console.WriteLine("✓ ExportToFileAsync(string) 方法签名正确");
            
            // 验证 ExportToStorageFileAsync 方法
            var exportStorageMethod = type.GetMethod("ExportToStorageFileAsync",
                BindingFlags.Public | BindingFlags.Instance,
                null, new[] { typeof(StorageFile) }, null);
            if (exportStorageMethod == null || exportStorageMethod.ReturnType != typeof(Task))
            {
                Console.WriteLine("✗ 失败: ExportToStorageFileAsync(StorageFile) 方法签名不正确");
                return false;
            }
            Console.WriteLine("✓ ExportToStorageFileAsync(StorageFile) 方法签名正确");
            
            // 验证 GenerateTestDataAsJson 方法
            var generateJsonMethod = type.GetMethod("GenerateTestDataAsJson",
                BindingFlags.Public | BindingFlags.Instance,
                null, Type.EmptyTypes, null);
            if (generateJsonMethod == null || generateJsonMethod.ReturnType != typeof(string))
            {
                Console.WriteLine("✗ 失败: GenerateTestDataAsJson() 方法签名不正确");
                return false;
            }
            Console.WriteLine("✓ GenerateTestDataAsJson() 方法签名正确");
            
            Console.WriteLine("✓ 测试通过\n");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 测试异常: {ex.Message}\n");
            return false;
        }
    }
    
    private static bool VerifyDefaultConstructor()
    {
        Console.WriteLine("测试 3: 验证默认构造函数功能");
        Console.WriteLine(new string('-', 60));
        
        try
        {
            var generator = new TestDataGenerator();
            Console.WriteLine("✓ 默认构造函数可以正常创建实例");
            
            var data = generator.GenerateTestData();
            if (data == null)
            {
                Console.WriteLine("✗ 失败: GenerateTestData() 返回 null");
                return false;
            }
            Console.WriteLine("✓ 可以生成测试数据");
            
            Console.WriteLine("✓ 测试通过\n");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 测试失败: {ex.Message}\n");
            return false;
        }
    }
    
    private static bool VerifyConfigConstructor()
    {
        Console.WriteLine("测试 4: 验证配置构造函数功能");
        Console.WriteLine(new string('-', 60));
        
        try
        {
            var config = new TestDataConfiguration
            {
                SkillCount = 5,
                PersonnelCount = 10,
                PositionCount = 8,
                HolidayConfigCount = 3,
                TemplateCount = 3,
                FixedAssignmentCount = 5,
                ManualAssignmentCount = 8
            };
            
            var generator = new TestDataGenerator(config);
            Console.WriteLine("✓ 配置构造函数可以正常创建实例");
            
            var data = generator.GenerateTestData();
            if (data == null)
            {
                Console.WriteLine("✗ 失败: GenerateTestData() 返回 null");
                return false;
            }
            Console.WriteLine("✓ 可以生成测试数据");
            
            // 验证生成的数据数量符合配置
            if (data.Skills.Count != config.SkillCount)
            {
                Console.WriteLine($"✗ 失败: 技能数量不匹配 (期望: {config.SkillCount}, 实际: {data.Skills.Count})");
                return false;
            }
            Console.WriteLine($"✓ 技能数量正确: {data.Skills.Count}");
            
            if (data.Personnel.Count != config.PersonnelCount)
            {
                Console.WriteLine($"✗ 失败: 人员数量不匹配 (期望: {config.PersonnelCount}, 实际: {data.Personnel.Count})");
                return false;
            }
            Console.WriteLine($"✓ 人员数量正确: {data.Personnel.Count}");
            
            if (data.Positions.Count != config.PositionCount)
            {
                Console.WriteLine($"✗ 失败: 哨位数量不匹配 (期望: {config.PositionCount}, 实际: {data.Positions.Count})");
                return false;
            }
            Console.WriteLine($"✓ 哨位数量正确: {data.Positions.Count}");
            
            Console.WriteLine("✓ 测试通过\n");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 测试失败: {ex.Message}\n");
            return false;
        }
    }
    
    private static bool VerifyGenerateTestData()
    {
        Console.WriteLine("测试 5: 验证 GenerateTestData 功能");
        Console.WriteLine(new string('-', 60));
        
        try
        {
            var generator = new TestDataGenerator();
            var data = generator.GenerateTestData();
            
            // 验证返回的数据不为空
            if (data == null)
            {
                Console.WriteLine("✗ 失败: 返回数据为 null");
                return false;
            }
            Console.WriteLine("✓ 返回数据不为 null");
            
            // 验证所有集合都已初始化
            if (data.Skills == null || data.Personnel == null || data.Positions == null ||
                data.HolidayConfigs == null || data.Templates == null ||
                data.FixedAssignments == null || data.ManualAssignments == null)
            {
                Console.WriteLine("✗ 失败: 某些数据集合为 null");
                return false;
            }
            Console.WriteLine("✓ 所有数据集合已初始化");
            
            // 验证元数据存在
            if (data.Metadata == null)
            {
                Console.WriteLine("✗ 失败: 元数据为 null");
                return false;
            }
            Console.WriteLine("✓ 元数据已创建");
            
            Console.WriteLine($"  - 技能: {data.Skills.Count} 条");
            Console.WriteLine($"  - 人员: {data.Personnel.Count} 条");
            Console.WriteLine($"  - 哨位: {data.Positions.Count} 条");
            Console.WriteLine($"  - 节假日配置: {data.HolidayConfigs.Count} 条");
            Console.WriteLine($"  - 排班模板: {data.Templates.Count} 条");
            Console.WriteLine($"  - 定岗规则: {data.FixedAssignments.Count} 条");
            Console.WriteLine($"  - 手动指定: {data.ManualAssignments.Count} 条");
            
            Console.WriteLine("✓ 测试通过\n");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 测试失败: {ex.Message}\n");
            return false;
        }
    }
    
    private static bool VerifyGenerateTestDataAsJson()
    {
        Console.WriteLine("测试 6: 验证 GenerateTestDataAsJson 功能");
        Console.WriteLine(new string('-', 60));
        
        try
        {
            var generator = new TestDataGenerator();
            var json = generator.GenerateTestDataAsJson();
            
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("✗ 失败: 返回的 JSON 为空");
                return false;
            }
            Console.WriteLine($"✓ 生成的 JSON 长度: {json.Length} 字符");
            
            // 验证 JSON 格式
            if (!json.TrimStart().StartsWith("{"))
            {
                Console.WriteLine("✗ 失败: JSON 格式不正确");
                return false;
            }
            Console.WriteLine("✓ JSON 格式正确");
            
            // 验证包含必要的字段
            var requiredFields = new[] { "skills", "personnel", "positions", "holidayConfigs", 
                                        "templates", "fixedAssignments", "manualAssignments", "metadata" };
            foreach (var field in requiredFields)
            {
                if (!json.Contains($"\"{field}\""))
                {
                    Console.WriteLine($"✗ 失败: JSON 缺少字段 '{field}'");
                    return false;
                }
            }
            Console.WriteLine("✓ JSON 包含所有必要字段");
            
            Console.WriteLine("✓ 测试通过\n");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 测试失败: {ex.Message}\n");
            return false;
        }
    }
    
    private static bool VerifyDataIntegrity()
    {
        Console.WriteLine("测试 7: 验证数据完整性");
        Console.WriteLine(new string('-', 60));
        
        try
        {
            var generator = new TestDataGenerator();
            var data = generator.GenerateTestData();
            
            // 验证技能数据
            foreach (var skill in data.Skills)
            {
                if (skill.Id <= 0 || string.IsNullOrWhiteSpace(skill.Name))
                {
                    Console.WriteLine($"✗ 失败: 技能数据不完整 (ID: {skill.Id})");
                    return false;
                }
            }
            Console.WriteLine($"✓ 技能数据完整 ({data.Skills.Count} 条)");
            
            // 验证人员数据
            var skillIds = data.Skills.Select(s => s.Id).ToHashSet();
            foreach (var person in data.Personnel)
            {
                if (person.Id <= 0 || string.IsNullOrWhiteSpace(person.Name))
                {
                    Console.WriteLine($"✗ 失败: 人员数据不完整 (ID: {person.Id})");
                    return false;
                }
                
                // 验证技能引用
                foreach (var skillId in person.SkillIds)
                {
                    if (!skillIds.Contains(skillId))
                    {
                        Console.WriteLine($"✗ 失败: 人员 {person.Name} 引用了不存在的技能 ID {skillId}");
                        return false;
                    }
                }
            }
            Console.WriteLine($"✓ 人员数据完整且引用有效 ({data.Personnel.Count} 条)");
            
            // 验证哨位数据
            var personnelIds = data.Personnel.Select(p => p.Id).ToHashSet();
            foreach (var position in data.Positions)
            {
                if (position.Id <= 0 || string.IsNullOrWhiteSpace(position.Name))
                {
                    Console.WriteLine($"✗ 失败: 哨位数据不完整 (ID: {position.Id})");
                    return false;
                }
                
                // 验证技能引用
                foreach (var skillId in position.RequiredSkillIds)
                {
                    if (!skillIds.Contains(skillId))
                    {
                        Console.WriteLine($"✗ 失败: 哨位 {position.Name} 引用了不存在的技能 ID {skillId}");
                        return false;
                    }
                }
                
                // 验证人员引用
                foreach (var personId in position.AvailablePersonnelIds)
                {
                    if (!personnelIds.Contains(personId))
                    {
                        Console.WriteLine($"✗ 失败: 哨位 {position.Name} 引用了不存在的人员 ID {personId}");
                        return false;
                    }
                }
            }
            Console.WriteLine($"✓ 哨位数据完整且引用有效 ({data.Positions.Count} 条)");
            
            // 验证手动指定唯一性
            var positionIds = data.Positions.Select(p => p.Id).ToHashSet();
            var manualAssignmentKeys = new HashSet<string>();
            foreach (var assignment in data.ManualAssignments)
            {
                if (!positionIds.Contains(assignment.PositionId))
                {
                    Console.WriteLine($"✗ 失败: 手动指定引用了不存在的哨位 ID {assignment.PositionId}");
                    return false;
                }
                
                if (!personnelIds.Contains(assignment.PersonnelId))
                {
                    Console.WriteLine($"✗ 失败: 手动指定引用了不存在的人员 ID {assignment.PersonnelId}");
                    return false;
                }
                
                var key = $"{assignment.PositionId}_{assignment.Date:yyyyMMdd}_{assignment.TimeSlot}";
                if (!manualAssignmentKeys.Add(key))
                {
                    Console.WriteLine($"✗ 失败: 手动指定存在重复 ({key})");
                    return false;
                }
            }
            Console.WriteLine($"✓ 手动指定数据完整、引用有效且无重复 ({data.ManualAssignments.Count} 条)");
            
            Console.WriteLine("✓ 测试通过\n");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 测试失败: {ex.Message}\n");
            return false;
        }
    }
    
    private static bool VerifyDataFormat()
    {
        Console.WriteLine("测试 8: 验证数据格式一致性");
        Console.WriteLine(new string('-', 60));
        
        try
        {
            var generator = new TestDataGenerator();
            var data = generator.GenerateTestData();
            
            // 验证元数据格式
            if (data.Metadata == null)
            {
                Console.WriteLine("✗ 失败: 元数据为 null");
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(data.Metadata.ExportVersion))
            {
                Console.WriteLine("✗ 失败: 导出版本为空");
                return false;
            }
            Console.WriteLine($"✓ 导出版本: {data.Metadata.ExportVersion}");
            
            if (data.Metadata.ExportTime == default)
            {
                Console.WriteLine("✗ 失败: 导出时间无效");
                return false;
            }
            Console.WriteLine($"✓ 导出时间: {data.Metadata.ExportTime}");
            
            // 验证数据统计
            if (data.Metadata.SkillCount != data.Skills.Count)
            {
                Console.WriteLine($"✗ 失败: 元数据中的技能数量不匹配");
                return false;
            }
            
            if (data.Metadata.PersonnelCount != data.Personnel.Count)
            {
                Console.WriteLine($"✗ 失败: 元数据中的人员数量不匹配");
                return false;
            }
            
            if (data.Metadata.PositionCount != data.Positions.Count)
            {
                Console.WriteLine($"✗ 失败: 元数据中的哨位数量不匹配");
                return false;
            }
            Console.WriteLine("✓ 元数据统计信息正确");
            
            Console.WriteLine("✓ 测试通过\n");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 测试失败: {ex.Message}\n");
            return false;
        }
    }
}
