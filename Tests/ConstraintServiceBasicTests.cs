using AutoScheduling3.Models.Constraints;
using AutoScheduling3.Services;
using AutoScheduling3.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AutoScheduling3.Tests;

/// <summary>
/// 约束管理服务基础功能测试
/// </summary>
public class ConstraintServiceBasicTests
{
    [Fact]
    public void ConstraintService_Interface_ShouldBeImplemented()
    {
        // Arrange & Act
        var serviceType = typeof(ConstraintService);
        var interfaceType = typeof(IConstraintService);

        // Assert
        Assert.True(interfaceType.IsAssignableFrom(serviceType));
    }

    [Fact]
    public void FixedPositionRule_Properties_ShouldBeCorrect()
    {
        // Arrange & Act
        var rule = new FixedPositionRule
        {
            Id = 1,
            PersonalId = 100,
            AllowedPositionIds = new List<int> { 1, 2, 3 },
            AllowedPeriods = new List<int> { 0, 1, 2, 3 },
            IsEnabled = true,
            Description = "测试定岗规则"
        };

        // Assert
        Assert.Equal(1, rule.Id);
        Assert.Equal(100, rule.PersonalId);
        Assert.Equal(3, rule.AllowedPositionIds.Count);
        Assert.Equal(4, rule.AllowedPeriods.Count);
        Assert.True(rule.IsEnabled);
        Assert.Equal("测试定岗规则", rule.Description);
    }

    [Fact]
    public void ManualAssignment_Properties_ShouldBeCorrect()
    {
        // Arrange & Act
        var assignment = new ManualAssignment
        {
            Id = 1,
            PositionId = 10,
            PeriodIndex = 5,
            PersonalId = 100,
            Date = new DateTime(2024, 1, 1),
            IsEnabled = true,
            Remarks = "测试手动指定"
        };

        // Assert
        Assert.Equal(1, assignment.Id);
        Assert.Equal(10, assignment.PositionId);
        Assert.Equal(5, assignment.PeriodIndex);
        Assert.Equal(100, assignment.PersonalId);
        Assert.Equal(new DateTime(2024, 1, 1), assignment.Date);
        Assert.True(assignment.IsEnabled);
        Assert.Equal("测试手动指定", assignment.Remarks);
    }

    [Fact]
    public void HolidayConfig_IsHoliday_ShouldWorkCorrectly()
    {
        // Arrange
        var config = new HolidayConfig
        {
            Id = 1,
            ConfigName = "测试配置",
            EnableWeekendRule = true,
            WeekendDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
            LegalHolidays = new List<DateTime> { new DateTime(2024, 1, 1) },
            CustomHolidays = new List<DateTime> { new DateTime(2024, 12, 25) },
            ExcludedDates = new List<DateTime> { new DateTime(2024, 1, 2) },
            IsActive = true
        };

        // Act & Assert
        // 测试法定节假日
        Assert.True(config.IsHoliday(new DateTime(2024, 1, 1)));
        
        // 测试自定义休息日
        Assert.True(config.IsHoliday(new DateTime(2024, 12, 25)));
        
        // 测试排除日期（优先级最高）
        Assert.False(config.IsHoliday(new DateTime(2024, 1, 2)));
        
        // 测试周末（假设某个日期是周六）
        var saturday = new DateTime(2024, 1, 6); // 2024年1月6日是周六
        Assert.True(config.IsHoliday(saturday));
        
        // 测试普通工作日
        var workday = new DateTime(2024, 1, 3); // 2024年1月3日是周三
        Assert.False(config.IsHoliday(workday));
    }

    [Fact]
    public void HolidayConfig_DisabledWeekendRule_ShouldNotConsiderWeekends()
    {
        // Arrange
        var config = new HolidayConfig
        {
            ConfigName = "测试配置",
            EnableWeekendRule = false, // 禁用周末规则
            WeekendDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
            IsActive = true
        };

        // Act & Assert
        var saturday = new DateTime(2024, 1, 6); // 2024年1月6日是周六
        Assert.False(config.IsHoliday(saturday)); // 应该返回false，因为禁用了周末规则
    }

    [Fact]
    public void FixedPositionRule_ToString_ShouldReturnCorrectFormat()
    {
        // Arrange
        var rule = new FixedPositionRule
        {
            Id = 123,
            PersonalId = 456,
            IsEnabled = true
        };

        // Act
        var result = rule.ToString();

        // Assert
        Assert.Contains("123", result);
        Assert.Contains("456", result);
        Assert.Contains("True", result);
    }

    [Fact]
    public void ManualAssignment_ToString_ShouldReturnCorrectFormat()
    {
        // Arrange
        var assignment = new ManualAssignment
        {
            Id = 1,
            PositionId = 10,
            PeriodIndex = 5,
            PersonalId = 100,
            Date = new DateTime(2024, 1, 1)
        };

        // Act
        var result = assignment.ToString();

        // Assert
        Assert.Contains("1", result);
        Assert.Contains("10", result);
        Assert.Contains("5", result);
        Assert.Contains("100", result);
        Assert.Contains("2024-01-01", result);
    }

    [Fact]
    public void HolidayConfig_ToString_ShouldReturnCorrectFormat()
    {
        // Arrange
        var config = new HolidayConfig
        {
            Id = 1,
            ConfigName = "测试配置",
            IsActive = true
        };

        // Act
        var result = config.ToString();

        // Assert
        Assert.Contains("1", result);
        Assert.Contains("测试配置", result);
        Assert.Contains("True", result);
    }
}