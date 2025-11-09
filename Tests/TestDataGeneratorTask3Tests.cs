using AutoScheduling3.TestData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoScheduling3.Tests;

/// <summary>
/// 测试任务3的实现：配置和约束数据生成
/// </summary>
[TestClass]
public class TestDataGeneratorTask3Tests
{
    [TestMethod]
    public void GenerateTestData_ShouldIncludeHolidayConfigs()
    {
        // Arrange
        var config = TestDataConfiguration.CreateDefault();
        var generator = new TestDataGenerator(config);

        // Act
        var testData = generator.GenerateTestData();

        // Assert
        Assert.IsNotNull(testData.HolidayConfigs);
        Assert.AreEqual(config.HolidayConfigCount, testData.HolidayConfigs.Count);
        Assert.IsTrue(testData.HolidayConfigs.Any(h => h.IsActive), "至少应有一个启用的节假日配置");
    }

    [TestMethod]
    public void GenerateTestData_ShouldIncludeTemplates()
    {
        // Arrange
        var config = TestDataConfiguration.CreateDefault();
        var generator = new TestDataGenerator(config);

        // Act
        var testData = generator.GenerateTestData();

        // Assert
        Assert.IsNotNull(testData.Templates);
        Assert.AreEqual(config.TemplateCount, testData.Templates.Count);
        Assert.IsTrue(testData.Templates.All(t => t.PersonnelIds.Count > 0), "所有模板应包含人员");
        Assert.IsTrue(testData.Templates.All(t => t.PositionIds.Count > 0), "所有模板应包含哨位");
    }

    [TestMethod]
    public void GenerateTestData_ShouldIncludeFixedAssignments()
    {
        // Arrange
        var config = TestDataConfiguration.CreateDefault();
        var generator = new TestDataGenerator(config);

        // Act
        var testData = generator.GenerateTestData();

        // Assert
        Assert.IsNotNull(testData.FixedAssignments);
        Assert.AreEqual(config.FixedAssignmentCount, testData.FixedAssignments.Count);
        Assert.IsTrue(testData.FixedAssignments.All(f => f.AllowedPositionIds.Count > 0), "所有定岗规则应包含允许的哨位");
        Assert.IsTrue(testData.FixedAssignments.All(f => f.AllowedTimeSlots.Count > 0), "所有定岗规则应包含允许的时段");
    }

    [TestMethod]
    public void GenerateTestData_ShouldIncludeManualAssignments()
    {
        // Arrange
        var config = TestDataConfiguration.CreateDefault();
        var generator = new TestDataGenerator(config);

        // Act
        var testData = generator.GenerateTestData();

        // Assert
        Assert.IsNotNull(testData.ManualAssignments);
        Assert.IsTrue(testData.ManualAssignments.Count > 0, "应生成手动指定记录");
        Assert.IsTrue(testData.ManualAssignments.All(m => m.TimeSlot >= 0 && m.TimeSlot <= 11), "所有时段索引应在0-11范围内");
    }

    [TestMethod]
    public void GenerateTestData_ShouldValidateReferences()
    {
        // Arrange
        var config = TestDataConfiguration.CreateDefault();
        var generator = new TestDataGenerator(config);

        // Act & Assert - 应该不抛出异常
        var testData = generator.GenerateTestData();

        // 验证模板引用
        var personnelIds = testData.Personnel.Select(p => p.Id).ToHashSet();
        var positionIds = testData.Positions.Select(p => p.Id).ToHashSet();
        var holidayConfigIds = testData.HolidayConfigs.Select(h => h.Id).ToHashSet();

        foreach (var template in testData.Templates)
        {
            Assert.IsTrue(template.PersonnelIds.All(id => personnelIds.Contains(id)), "模板引用的人员ID应存在");
            Assert.IsTrue(template.PositionIds.All(id => positionIds.Contains(id)), "模板引用的哨位ID应存在");
            if (template.HolidayConfigId.HasValue)
            {
                Assert.IsTrue(holidayConfigIds.Contains(template.HolidayConfigId.Value), "模板引用的节假日配置ID应存在");
            }
        }

        // 验证定岗规则引用
        foreach (var assignment in testData.FixedAssignments)
        {
            Assert.IsTrue(personnelIds.Contains(assignment.PersonnelId), "定岗规则引用的人员ID应存在");
            Assert.IsTrue(assignment.AllowedPositionIds.All(id => positionIds.Contains(id)), "定岗规则引用的哨位ID应存在");
        }

        // 验证手动指定引用
        foreach (var assignment in testData.ManualAssignments)
        {
            Assert.IsTrue(personnelIds.Contains(assignment.PersonnelId), "手动指定引用的人员ID应存在");
            Assert.IsTrue(positionIds.Contains(assignment.PositionId), "手动指定引用的哨位ID应存在");
        }
    }
}
