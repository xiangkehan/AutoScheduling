using AutoScheduling3.TestData;
using System;
using Xunit;

namespace AutoScheduling3.Tests;

/// <summary>
/// 数据验证测试
/// </summary>
public class DataValidationTests
{
    [Fact]
    public void TestConfigurationValidation_ValidConfig_ShouldPass()
    {
        // Arrange
        var config = TestDataConfiguration.CreateDefault();

        // Act & Assert - 不应抛出异常
        config.Validate();
    }

    [Fact]
    public void TestConfigurationValidation_InvalidSkillCount_ShouldThrow()
    {
        // Arrange
        var config = new TestDataConfiguration
        {
            SkillCount = 0, // 无效值
            PersonnelCount = 10,
            PositionCount = 5
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("技能数量至少需要1个", exception.Message);
    }

    [Fact]
    public void TestConfigurationValidation_ExcessiveSkillCount_ShouldThrow()
    {
        // Arrange
        var config = new TestDataConfiguration
        {
            SkillCount = 100, // 超过上限
            PersonnelCount = 10,
            PositionCount = 5
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("技能数量不能超过50", exception.Message);
    }

    [Fact]
    public void TestConfigurationValidation_NegativeFixedAssignmentCount_ShouldThrow()
    {
        // Arrange
        var config = new TestDataConfiguration
        {
            SkillCount = 5,
            PersonnelCount = 10,
            PositionCount = 5,
            FixedAssignmentCount = -1 // 负数
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("定岗规则数量不能为负数", exception.Message);
    }

    [Fact]
    public void TestConfigurationValidation_InsufficientPersonnel_ShouldThrow()
    {
        // Arrange
        var config = new TestDataConfiguration
        {
            SkillCount = 5,
            PersonnelCount = 5, // 人员数量不足
            PositionCount = 5,
            TemplateCount = 3 // 需要至少 3*3=9 个人员
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("人员数量", exception.Message);
        Assert.Contains("排班模板数量", exception.Message);
    }

    [Fact]
    public void TestConfigurationValidateWithResult_ValidConfig_ShouldReturnValid()
    {
        // Arrange
        var config = TestDataConfiguration.CreateDefault();

        // Act
        var result = config.ValidateWithResult();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void TestConfigurationValidateWithResult_InvalidConfig_ShouldReturnErrors()
    {
        // Arrange
        var config = new TestDataConfiguration
        {
            SkillCount = 0,
            PersonnelCount = -1,
            PositionCount = 100
        };

        // Act
        var result = config.ValidateWithResult();

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("技能数量"));
        Assert.Contains(result.Errors, e => e.Contains("人员数量"));
        Assert.Contains(result.Errors, e => e.Contains("哨位数量"));
    }

    [Fact]
    public void TestConfigurationValidateWithResult_WithWarnings_ShouldReturnWarnings()
    {
        // Arrange
        var config = new TestDataConfiguration
        {
            SkillCount = 1, // 技能数量较少
            PersonnelCount = 10, // 人员数量较多
            PositionCount = 5,
            TemplateCount = 3
        };

        // Act
        var result = config.ValidateWithResult();

        // Assert
        Assert.True(result.IsValid); // 仍然有效
        Assert.True(result.HasWarnings); // 但有警告
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void TestGeneratedDataValidation_ValidData_ShouldPass()
    {
        // Arrange
        var config = TestDataConfiguration.CreateSmall();
        var generator = new TestDataGenerator(config);

        // Act & Assert - 不应抛出异常
        var data = generator.GenerateTestData();

        // 验证数据已生成
        Assert.NotNull(data);
        Assert.NotEmpty(data.Skills);
        Assert.NotEmpty(data.Personnel);
        Assert.NotEmpty(data.Positions);
    }

    [Fact]
    public void TestValidationResult_GetFormattedMessage_ShouldFormatCorrectly()
    {
        // Arrange
        var result = new ValidationResult
        {
            IsValid = false,
            Errors = new List<string> { "错误1", "错误2" },
            Warnings = new List<string> { "警告1" }
        };

        // Act
        var message = result.GetFormattedMessage();

        // Assert
        Assert.Contains("错误:", message);
        Assert.Contains("错误1", message);
        Assert.Contains("错误2", message);
        Assert.Contains("警告:", message);
        Assert.Contains("警告1", message);
    }
}
