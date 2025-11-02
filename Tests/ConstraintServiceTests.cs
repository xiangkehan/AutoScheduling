using AutoScheduling3.Data;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.Mappers;
using AutoScheduling3.Models;
using AutoScheduling3.Models.Constraints;
using AutoScheduling3.Services;
using AutoScheduling3.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit; // 添加此 using 指令以解决 CS0246: 未能找到类型或命名空间名“FactAttribute” 和 “Fact”

namespace AutoScheduling3.Tests;

/// <summary>
/// 约束管理服务测试类
/// </summary>
public class ConstraintServiceTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly IConstraintRepository _constraintRepository;
    private readonly IPersonalRepository _personnelRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly ConstraintMapper _constraintMapper;
    private readonly IConstraintService _constraintService;

    public ConstraintServiceTests()
    {
        // 创建临时测试数据库
        _testDbPath = Path.GetTempFileName();
        
        // 初始化仓储
        _constraintRepository = new ConstraintRepository(_testDbPath);
        _personnelRepository = new PersonalRepository(_testDbPath);
        _positionRepository = new PositionLocationRepository(_testDbPath);
        
        // 初始化映射器
        _constraintMapper = new ConstraintMapper(_personnelRepository, _positionRepository);
        
        // 初始化服务
        _constraintService = new ConstraintService(
            _constraintRepository,
            _personnelRepository,
            _positionRepository,
            _constraintMapper);

        // 初始化数据库
        InitializeDatabaseAsync().Wait();
    }

    private async Task InitializeDatabaseAsync()
    {
        await _constraintRepository.InitAsync();
        await _personnelRepository.InitAsync();
        await _positionRepository.InitAsync();

        // 创建测试数据
        await CreateTestDataAsync();
    }

    private async Task CreateTestDataAsync()
    {
        // 创建测试人员
        var personnel1 = new Personal
        {
            Name = "张三",
            PositionId = "班长",
            IsAvailable = true,
            IsRetired = false,
            SkillIds = new List<int> { 1, 2 },
            RecentShiftIntervalCount = 5,
            RecentHolidayShiftIntervalCount = 10,
            RecentTimeSlotIntervals = new int[12] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }
        };

        var personnel2 = new Personal
        {
            Name = "李四",
            PositionId = "副班长",
            IsAvailable = true,
            IsRetired = false,
            SkillIds = new List<int> { 2, 3 },
            RecentShiftIntervalCount = 3,
            RecentHolidayShiftIntervalCount = 7,
            RecentTimeSlotIntervals = new int[12] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1 }
        };

        await _personnelRepository.CreateAsync(personnel1);
        await _personnelRepository.CreateAsync(personnel2);

        // 创建测试哨位
        var position1 = new PositionLocation
        {
            Name = "东门哨位",
            Location = "东门",
            Description = "负责东门安全",
            Requirements = "需要持证上岗",
            RequiredSkillIds = new List<int> { 1, 2 }
        };

        var position2 = new PositionLocation
        {
            Name = "西门哨位",
            Location = "西门",
            Description = "负责西门安全",
            Requirements = "需要夜视能力",
            RequiredSkillIds = new List<int> { 2, 3 }
        };

        await _positionRepository.CreateAsync(position1);
        await _positionRepository.CreateAsync(position2);
    }

    #region 定岗规则测试

    [Fact]
    public async Task CreateFixedPositionRule_ValidRule_ShouldSucceed()
    {
        // Arrange
        var rule = new FixedPositionRule
        {
            PersonalId = 1,
            AllowedPositionIds = new List<int> { 1, 2 },
            AllowedPeriods = new List<int> { 0, 1, 2 },
            IsEnabled = true,
            Description = "测试定岗规则"
        };

        // Act
        var id = await _constraintService.CreateFixedPositionRuleAsync(rule);

        // Assert
        Assert.True(id > 0);
        var createdRule = await _constraintRepository.GetAllFixedPositionRulesAsync();
        Assert.Single(createdRule);
        Assert.Equal(rule.PersonalId, createdRule[0].PersonalId);
    }

    [Fact]
    public async Task CreateFixedPositionRule_InvalidPersonnel_ShouldThrowException()
    {
        // Arrange
        var rule = new FixedPositionRule
        {
            PersonalId = 999, // 不存在的人员ID
            AllowedPositionIds = new List<int> { 1 },
            AllowedPeriods = new List<int> { 0 },
            IsEnabled = true,
            Description = "测试规则"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _constraintService.CreateFixedPositionRuleAsync(rule));
    }

    [Fact]
    public async Task GetFixedPositionRulesByPerson_ValidPersonnel_ShouldReturnRules()
    {
        // Arrange
        var rule = new FixedPositionRule
        {
            PersonalId = 1,
            AllowedPositionIds = new List<int> { 1 },
            AllowedPeriods = new List<int> { 0, 1 },
            IsEnabled = true,
            Description = "测试规则"
        };
        await _constraintService.CreateFixedPositionRuleAsync(rule);

        // Act
        var rules = await _constraintService.GetFixedPositionRulesByPersonAsync(1);

        // Assert
        Assert.Single(rules);
        Assert.Equal(1, rules[0].PersonalId);
    }

    #endregion

    #region 手动指定测试

    [Fact]
    public async Task CreateManualAssignment_ValidAssignment_ShouldSucceed()
    {
        // Arrange
        var assignment = new ManualAssignment
        {
            PositionId = 1,
            PeriodIndex = 0,
            PersonalId = 1,
            Date = DateTime.Today.AddDays(1),
            IsEnabled = true,
            Remarks = "测试手动指定"
        };

        // Act
        var id = await _constraintService.CreateManualAssignmentAsync(assignment);

        // Assert
        Assert.True(id > 0);
        var assignments = await _constraintRepository.GetManualAssignmentsByDateRangeAsync(
            DateTime.Today, DateTime.Today.AddDays(2));
        Assert.Single(assignments);
    }

    [Fact]
    public async Task CreateManualAssignment_PastDate_ShouldThrowException()
    {
        // Arrange
        var assignment = new ManualAssignment
        {
            PositionId = 1,
            PeriodIndex = 0,
            PersonalId = 1,
            Date = DateTime.Today.AddDays(-1), // 过去的日期
            IsEnabled = true,
            Remarks = "测试"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _constraintService.CreateManualAssignmentAsync(assignment));
    }

    [Fact]
    public async Task CreateManualAssignment_InvalidTimeSlot_ShouldThrowException()
    {
        // Arrange
        var assignment = new ManualAssignment
        {
            PositionId = 1,
            PeriodIndex = 15, // 无效的时段索引
            PersonalId = 1,
            Date = DateTime.Today.AddDays(1),
            IsEnabled = true,
            Remarks = "测试"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _constraintService.CreateManualAssignmentAsync(assignment));
    }

    #endregion

    #region 休息日配置测试

    [Fact]
    public async Task CreateHolidayConfig_ValidConfig_ShouldSucceed()
    {
        // Arrange
        var config = new HolidayConfig
        {
            ConfigName = "2024年休息日配置",
            EnableWeekendRule = true,
            WeekendDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
            LegalHolidays = new List<DateTime> { new DateTime(2024, 1, 1), new DateTime(2024, 10, 1) },
            CustomHolidays = new List<DateTime> { new DateTime(2024, 12, 25) },
            ExcludedDates = new List<DateTime>(),
            IsActive = true
        };

        // Act
        var id = await _constraintService.CreateHolidayConfigAsync(config);

        // Assert
        Assert.True(id > 0);
        var activeConfig = await _constraintService.GetActiveHolidayConfigAsync();
        Assert.NotNull(activeConfig);
        Assert.Equal(config.ConfigName, activeConfig.ConfigName);
    }

    [Fact]
    public async Task IsHoliday_Weekend_ShouldReturnTrue()
    {
        // Arrange
        var config = new HolidayConfig
        {
            ConfigName = "测试配置",
            EnableWeekendRule = true,
            WeekendDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
            IsActive = true
        };
        await _constraintService.CreateHolidayConfigAsync(config);

        // 找到下一个周六
        var nextSaturday = DateTime.Today;
        while (nextSaturday.DayOfWeek != DayOfWeek.Saturday)
        {
            nextSaturday = nextSaturday.AddDays(1);
        }

        // Act
        var isHoliday = await _constraintService.IsHolidayAsync(nextSaturday);

        // Assert
        Assert.True(isHoliday);
    }

    [Fact]
    public async Task IsHoliday_LegalHoliday_ShouldReturnTrue()
    {
        // Arrange
        var holidayDate = new DateTime(2024, 1, 1);
        var config = new HolidayConfig
        {
            ConfigName = "测试配置",
            EnableWeekendRule = false,
            LegalHolidays = new List<DateTime> { holidayDate },
            IsActive = true
        };
        await _constraintService.CreateHolidayConfigAsync(config);

        // Act
        var isHoliday = await _constraintService.IsHolidayAsync(holidayDate);

        // Assert
        Assert.True(isHoliday);
    }

    [Fact]
    public async Task IsHoliday_ExcludedDate_ShouldReturnFalse()
    {
        // Arrange
        var excludedDate = new DateTime(2024, 1, 1);
        var config = new HolidayConfig
        {
            ConfigName = "测试配置",
            EnableWeekendRule = true,
            WeekendDays = new List<DayOfWeek> { DayOfWeek.Monday }, // 假设1月1日是周一
            LegalHolidays = new List<DateTime> { excludedDate },
            ExcludedDates = new List<DateTime> { excludedDate }, // 排除日期优先级最高
            IsActive = true
        };
        await _constraintService.CreateHolidayConfigAsync(config);

        // Act
        var isHoliday = await _constraintService.IsHolidayAsync(excludedDate);

        // Assert
        Assert.False(isHoliday); // 排除日期优先级最高，应该返回false
    }

    #endregion

    #region DTO方法测试

    [Fact]
    public async Task CreateFixedAssignmentDto_ValidDto_ShouldSucceed()
    {
        // Arrange
        var dto = new CreateFixedAssignmentDto
        {
            PersonnelId = 1,
            AllowedPositionIds = new List<int> { 1, 2 },
            AllowedTimeSlots = new List<int> { 0, 1, 2 },
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(30),
            IsEnabled = true,
            RuleName = "测试定岗规则",
            Description = "通过DTO创建的测试规则"
        };

        // Act
        var id = await _constraintService.CreateFixedAssignmentAsync(dto);

        // Assert
        Assert.True(id > 0);
        var dtos = await _constraintService.GetAllFixedAssignmentDtosAsync();
        Assert.Single(dtos);
        Assert.Equal(dto.PersonnelId, dtos[0].PersonnelId);
    }

    [Fact]
    public async Task CreateManualAssignmentDto_ValidDto_ShouldSucceed()
    {
        // Arrange
        var dto = new CreateManualAssignmentDto
        {
            PositionId = 1,
            TimeSlot = 0,
            PersonnelId = 1,
            Date = DateTime.Today.AddDays(1),
            IsEnabled = true,
            Remarks = "通过DTO创建的测试手动指定"
        };

        // Act
        var id = await _constraintService.CreateManualAssignmentAsync(dto);

        // Assert
        Assert.True(id > 0);
        var dtos = await _constraintService.GetManualAssignmentDtosByDateRangeAsync(
            DateTime.Today, DateTime.Today.AddDays(2));
        Assert.Single(dtos);
        Assert.Equal(dto.PositionId, dtos[0].PositionId);
    }

    [Fact]
    public async Task CreateHolidayConfigDto_ValidDto_ShouldSucceed()
    {
        // Arrange
        var dto = new CreateHolidayConfigDto
        {
            ConfigName = "通过DTO创建的配置",
            EnableWeekendRule = true,
            WeekendDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
            LegalHolidays = new List<DateTime> { new DateTime(2024, 1, 1) },
            IsActive = true
        };

        // Act
        var id = await _constraintService.CreateHolidayConfigAsync(dto);

        // Assert
        Assert.True(id > 0);
        var activeDto = await _constraintService.GetActiveHolidayConfigDtoAsync();
        Assert.NotNull(activeDto);
        Assert.Equal(dto.ConfigName, activeDto.ConfigName);
    }

    #endregion

    #region 业务规则验证测试

    [Fact]
    public async Task ValidateFixedPositionRule_ValidRule_ShouldReturnTrue()
    {
        // Arrange
        var rule = new FixedPositionRule
        {
            PersonalId = 1,
            AllowedPositionIds = new List<int> { 1 },
            AllowedPeriods = new List<int> { 0, 1 },
            IsEnabled = true,
            Description = "有效规则"
        };

        // Act
        var isValid = await _constraintService.ValidateFixedPositionRuleAsync(rule);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateFixedPositionRule_InvalidPersonnel_ShouldReturnFalse()
    {
        // Arrange
        var rule = new FixedPositionRule
        {
            PersonalId = 999, // 不存在的人员
            AllowedPositionIds = new List<int> { 1 },
            AllowedPeriods = new List<int> { 0 },
            IsEnabled = true,
            Description = "无效规则"
        };

        // Act
        var isValid = await _constraintService.ValidateFixedPositionRuleAsync(rule);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateManualAssignment_ValidAssignment_ShouldReturnTrue()
    {
        // Arrange
        var assignment = new ManualAssignment
        {
            PositionId = 1,
            PeriodIndex = 0,
            PersonalId = 1,
            Date = DateTime.Today.AddDays(1),
            IsEnabled = true,
            Remarks = "有效指定"
        };

        // Act
        var isValid = await _constraintService.ValidateManualAssignmentAsync(assignment);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateManualAssignment_PastDate_ShouldReturnFalse()
    {
        // Arrange
        var assignment = new ManualAssignment
        {
            PositionId = 1,
            PeriodIndex = 0,
            PersonalId = 1,
            Date = DateTime.Today.AddDays(-1), // 过去的日期
            IsEnabled = true,
            Remarks = "无效指定"
        };

        // Act
        var isValid = await _constraintService.ValidateManualAssignmentAsync(assignment);

        // Assert
        Assert.False(isValid);
    }

    #endregion

    public void Dispose()
    {
        // 清理测试数据库文件
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }
}