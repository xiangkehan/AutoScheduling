using AutoScheduling3.TestData;

namespace AutoScheduling3.TestData;

/// <summary>
/// 验证手动指定生成功能
/// </summary>
public static class VerifyManualAssignments
{
    public static void Run()
    {
        Console.WriteLine("=== 验证手动指定生成功能 ===\n");
        
        var config = new TestDataConfiguration
        {
            SkillCount = 5,
            PersonnelCount = 10,
            PositionCount = 8,
            ManualAssignmentCount = 8
        };
        
        var generator = new TestDataGenerator(config);
        var testData = generator.GenerateTestData();
        
        Console.WriteLine($"生成的手动指定数量: {testData.ManualAssignments.Count}");
        Console.WriteLine("\n手动指定详情:");
        Console.WriteLine(new string('-', 80));
        
        foreach (var assignment in testData.ManualAssignments)
        {
            Console.WriteLine($"ID: {assignment.Id}");
            Console.WriteLine($"  哨位: {assignment.PositionName} (ID: {assignment.PositionId})");
            Console.WriteLine($"  人员: {assignment.PersonnelName} (ID: {assignment.PersonnelId})");
            Console.WriteLine($"  日期: {assignment.Date:yyyy-MM-dd}");
            Console.WriteLine($"  时段: {assignment.TimeSlot} ({GetTimeSlotDisplay(assignment.TimeSlot)})");
            Console.WriteLine($"  启用: {(assignment.IsEnabled ? "是" : "否")}");
            Console.WriteLine($"  备注: {assignment.Remarks}");
            Console.WriteLine(new string('-', 80));
        }
        
        // 验证没有重复
        var combinations = testData.ManualAssignments
            .Select(a => $"{a.PositionId}_{a.Date:yyyyMMdd}_{a.TimeSlot}")
            .ToList();
        var uniqueCount = combinations.Distinct().Count();
        
        Console.WriteLine($"\n总记录数: {combinations.Count}");
        Console.WriteLine($"唯一组合数: {uniqueCount}");
        Console.WriteLine($"重复检查: {(uniqueCount == combinations.Count ? "✓ 通过" : "✗ 失败")}");
        
        // 验证引用完整性
        bool allReferencesValid = true;
        foreach (var assignment in testData.ManualAssignments)
        {
            if (!testData.Positions.Any(p => p.Id == assignment.PositionId))
            {
                Console.WriteLine($"✗ 错误: 哨位ID {assignment.PositionId} 不存在");
                allReferencesValid = false;
            }
            if (!testData.Personnel.Any(p => p.Id == assignment.PersonnelId))
            {
                Console.WriteLine($"✗ 错误: 人员ID {assignment.PersonnelId} 不存在");
                allReferencesValid = false;
            }
            if (assignment.TimeSlot < 0 || assignment.TimeSlot > 11)
            {
                Console.WriteLine($"✗ 错误: 时段索引 {assignment.TimeSlot} 超出范围");
                allReferencesValid = false;
            }
        }
        
        Console.WriteLine($"引用完整性: {(allReferencesValid ? "✓ 通过" : "✗ 失败")}");
        
        Console.WriteLine("\n=== 验证完成 ===");
    }
    
    private static string GetTimeSlotDisplay(int slot)
    {
        var hours = slot * 2;
        return $"{hours:D2}:00-{(hours + 2):D2}:00";
    }
}
