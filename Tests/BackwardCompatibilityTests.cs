using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.Data;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.Mappers;
using AutoScheduling3.Models;
using AutoScheduling3.Models.Constraints;
using AutoScheduling3.SchedulingEngine.Core;
using AutoScheduling3.Services;
using AutoScheduling3.Services.Interfaces;
using Xunit;

namespace AutoScheduling3.Tests;

/// <summary>
/// 向后兼容性测试 - 验证可选技能要求功能不影响现有功能
/// 对应需求: 1.4
/// </summary>
public class BackwardCompatibilityTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly IPositionRepository _positionRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IPersonalRepository _personnelRepository;
    private readonly IPositionService _positionService;
    private readonly PositionMapper _positionMapper;

    public BackwardCompatibilityTests()
    {
        // 创建临时测试数据库
        _testDbPath = Path.GetTempFileName();
        
        // 初始化仓储
        _positionRepository = new PositionLocationRepository(_testDbPath);
        _skillRepository = new SkillRepository(_testDbPath);
        _personnelRepository = new PersonalRepository(_testDbPath);
        
        // 初始化映射器
        _positionMapper = new PositionMapper(_skillRepository, _personnelRepository);
        
        // 初始化服务
        _positionService = new PositionService(
            _positionRepository,
            _skillRepository,
            _personnelRepository,
            _positionMapper);

        // 初始化数据库
        InitializeDatabaseAsync().Wait();
    }

    private async Task InitializeDatabaseAsync()
    {
        await _positionRepository.InitAsync();
        await _skillRepository.InitAsync();
        await _personnelRepository.InitAsync();

        // 创建测试技能
        await CreateTestSkillsAsync();
        
        // 创建测试人员
        await CreateTestPersonnelAsync();
        
        // 创建有技能要求的测试哨位
        await CreateTestPositionsWithSkillsAsync();
    }

    private async Task CreateTestSkillsAsync()
    {
        var skills = new List<Skill>
        {
            new Skill { Name = "持证上岗", Description = "需要持有上岗证", IsActive = true },
            new Skill { Name = "夜视能力", Description = "具备夜间值守能力", IsActive = true },
            new Skill { Name = "急救技能", Description = "掌握基本急救知识", IsActive = true }
        };

        foreach (var skill in skills)
        {
            await _skillRepository.CreateAsync(skill);
        }
    }

    private async Task CreateTestPersonnelAsync()
    {
        var personnel = new List<Personal>
        {
            new Personal
            {
                Name = "张三",
                IsAvailable = true,
                IsRetired = false,
                SkillIds = new List<int> { 1, 2 }
            },
            new Personal
            {
                Name = "李四",
                IsAvailable = true,
                IsRetired = false,
                SkillIds = new List<int> { 2, 3 }
            },
            new Personal
            {
                Name = "王五",
                IsAvailable = true,
                IsRetired = false,
                SkillIds = new List<int> { 1, 2, 3 }
            }
        };

        foreach (var person in personnel)
        {
            await _personnelRepository.CreateAsync(person);
        }
    }

    private async Task CreateTestPositionsWithSkillsAsync()
    {
        var positions = new List<PositionLocation>
        {
            new PositionLocation
            {
                Name = "东门哨位",
                Location = "东门",
                Description = "负责东门安全",
                Requirements = "需要持证上岗",
                RequiredSkillIds = new List<int> { 1, 2 }
            },
            new PositionLocation
            {
                Name = "西门哨位",
                Location = "西门",
                Description = "负责西门安全",
                Requirements = "需要夜视能力",
                RequiredSkillIds = new List<int> { 2 }
            }
        };

        foreach (var position in positions)
        {
            await _positionRepository.CreateAsync(position);
        }
    }

    #region 子任务 6.1: 测试现有哨位数据

    /// <summary>
    /// 测试查询现有有技能要求的哨位
    /// 需求: 1.4
    /// </summary>
    [Fact]
    public async Task GetExistingPositionsWithSkills_ShouldReturnCorrectData()
    {
        // Act
        var positions = await _positionService.GetAllAsync();

        // Assert
        Assert.NotEmpty(positions);
        Assert.All(positions, p =>
        {
            Assert.NotNull(p.RequiredSkillIds);
            Assert.NotEmpty(p.RequiredSkillIds);
        });
    }

    /// <summary>
    /// 测试显示功能 - 验证有技能要求的哨位正确显示技能列表
    /// 需求: 1.4
    /// </summary>
    [Fact]
    public async Task GetPositionById_WithSkills_ShouldDisplaySkillNames()
    {
        // Arrange
        var allPositions = await _positionService.GetAllAsync();
        var positionWithSkills = allPositions.First();

        // Act
        var position = await _positionService.GetByIdAsync(positionWithSkills.Id);

        // Assert
        Assert.NotNull(position);
        Assert.NotEmpty(position.RequiredSkillIds);
        Assert.NotEmpty(position.RequiredSkillNames);
        Assert.Equal(position.RequiredSkillIds.Count, position.RequiredSkillNames.Count);
    }

    /// <summary>
    /// 测试编辑功能 - 验证可以正常更新有技能要求的哨位
    /// 需求: 1.4
    /// </summary>
    [Fact]
    public async Task UpdatePosition_WithSkills_ShouldSucceed()
    {
        // Arrange
        var allPositions = await _positionService.GetAllAsync();
        var position = allPositions.First();
        
        var updateDto = new UpdatePositionDto
        {
            Name = position.Name + " (已更新)",
            Location = position.Location,
            Description = "更新后的描述",
            Requirements = position.Requirements,
            RequiredSkillIds = position.RequiredSkillIds
        };

        // Act
        await _positionService.UpdateAsync(position.Id, updateDto);
        var updatedPosition = await _positionService.GetByIdAsync(position.Id);

        // Assert
        Assert.NotNull(updatedPosition);
        Assert.Contains("(已更新)", updatedPosition.Name);
        Assert.Equal("更新后的描述", updatedPosition.Description);
        Assert.Equal(position.RequiredSkillIds.Count, updatedPosition.RequiredSkillIds.Count);
    }

    /// <summary>
    /// 测试排班功能 - 验证技能匹配逻辑对有技能要求的哨位正常工作
    /// 需求: 1.4
    /// </summary>
    [Fact]
    public async Task SchedulingEngine_WithSkillRequirements_ShouldMatchCorrectly()
    {
        // Arrange
        var positions = await _positionRepository.GetAllAsync();
        var personnel = await _personnelRepository.GetAllAsync();
        
        var context = new SchedulingContext
        {
            Positions = positions,
            Personals = personnel,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            FixedPositionRules = new List<FixedPositionRule>(),
            ManualAssignments = new List<ManualAssignment>()
        };

        var validator = new ConstraintValidator(context);

        // Act & Assert - 测试东门哨位 (需要技能1和2)
        var eastGatePosition = positions.First(p => p.Name == "东门哨位");
        var eastGateIdx = positions.IndexOf(eastGatePosition);

        // 张三有技能1和2，应该匹配
        var zhangSan = personnel.First(p => p.Name == "张三");
        var zhangSanIdx = personnel.IndexOf(zhangSan);
        Assert.True(validator.ValidateSkillMatch(zhangSanIdx, eastGateIdx));

        // 李四只有技能2和3，不应该匹配（缺少技能1）
        var liSi = personnel.First(p => p.Name == "李四");
        var liSiIdx = personnel.IndexOf(liSi);
        Assert.False(validator.ValidateSkillMatch(liSiIdx, eastGateIdx));

        // 王五有技能1、2、3，应该匹配
        var wangWu = personnel.First(p => p.Name == "王五");
        var wangWuIdx = personnel.IndexOf(wangWu);
        Assert.True(validator.ValidateSkillMatch(wangWuIdx, eastGateIdx));
    }

    #endregion

    #region 子任务 6.2: 测试编辑功能

    /// <summary>
    /// 测试编辑现有哨位，移除所有技能要求
    /// 需求: 1.4
    /// </summary>
    [Fact]
    public async Task UpdatePosition_RemoveAllSkills_ShouldSucceed()
    {
        // Arrange
        var allPositions = await _positionService.GetAllAsync();
        var position = allPositions.First();
        Assert.NotEmpty(position.RequiredSkillIds); // 确认初始有技能要求

        var updateDto = new UpdatePositionDto
        {
            Name = position.Name,
            Location = position.Location,
            Description = position.Description,
            Requirements = position.Requirements,
            RequiredSkillIds = new List<int>() // 移除所有技能要求
        };

        // Act
        await _positionService.UpdateAsync(position.Id, updateDto);
        var updatedPosition = await _positionService.GetByIdAsync(position.Id);

        // Assert
        Assert.NotNull(updatedPosition);
        Assert.Empty(updatedPosition.RequiredSkillIds);
    }

    /// <summary>
    /// 测试显示为"无技能要求" - 验证UI层应该显示特殊文本
    /// 需求: 1.4, 1.3
    /// </summary>
    [Fact]
    public async Task GetPosition_WithNoSkills_ShouldHaveEmptySkillList()
    {
        // Arrange - 创建无技能要求的哨位
        var position = new PositionLocation
        {
            Name = "临时哨位",
            Location = "临时地点",
            Description = "测试用临时哨位",
            Requirements = "无特殊要求",
            RequiredSkillIds = new List<int>()
        };
        var id = await _positionRepository.CreateAsync(position);

        // Act
        var retrievedPosition = await _positionService.GetByIdAsync(id);

        // Assert
        Assert.NotNull(retrievedPosition);
        Assert.Empty(retrievedPosition.RequiredSkillIds);
        Assert.Empty(retrievedPosition.RequiredSkillNames);
        // UI层应该根据空列表显示"无技能要求"
    }

    /// <summary>
    /// 测试再次编辑，添加技能要求
    /// 需求: 1.4
    /// </summary>
    [Fact]
    public async Task UpdatePosition_AddSkillsBack_ShouldSucceed()
    {
        // Arrange - 先创建无技能要求的哨位
        var position = new PositionLocation
        {
            Name = "测试哨位",
            Location = "测试地点",
            Description = "测试描述",
            Requirements = "测试要求",
            RequiredSkillIds = new List<int>()
        };
        var id = await _positionRepository.CreateAsync(position);

        // 验证初始状态
        var initialPosition = await _positionService.GetByIdAsync(id);
        Assert.Empty(initialPosition.RequiredSkillIds);

        // Act - 添加技能要求
        var updateDto = new UpdatePositionDto
        {
            Name = initialPosition.Name,
            Location = initialPosition.Location,
            Description = initialPosition.Description,
            Requirements = initialPosition.Requirements,
            RequiredSkillIds = new List<int> { 1, 2 } // 添加技能要求
        };
        await _positionService.UpdateAsync(id, updateDto);

        // Assert
        var updatedPosition = await _positionService.GetByIdAsync(id);
        Assert.NotNull(updatedPosition);
        Assert.NotEmpty(updatedPosition.RequiredSkillIds);
        Assert.Equal(2, updatedPosition.RequiredSkillIds.Count);
        Assert.NotEmpty(updatedPosition.RequiredSkillNames);
    }

    /// <summary>
    /// 测试恢复正常显示 - 验证添加技能后正常显示技能列表
    /// 需求: 1.4
    /// </summary>
    [Fact]
    public async Task UpdatePosition_AfterAddingSkills_ShouldDisplaySkillNames()
    {
        // Arrange - 创建无技能要求的哨位
        var position = new PositionLocation
        {
            Name = "测试哨位2",
            Location = "测试地点2",
            RequiredSkillIds = new List<int>()
        };
        var id = await _positionRepository.CreateAsync(position);

        // Act - 添加技能
        var updateDto = new UpdatePositionDto
        {
            Name = position.Name,
            Location = position.Location,
            RequiredSkillIds = new List<int> { 1 }
        };
        await _positionService.UpdateAsync(id, updateDto);

        // Assert
        var updatedPosition = await _positionService.GetByIdAsync(id);
        Assert.NotNull(updatedPosition);
        Assert.Single(updatedPosition.RequiredSkillIds);
        Assert.Single(updatedPosition.RequiredSkillNames);
        Assert.Equal("持证上岗", updatedPosition.RequiredSkillNames[0]);
    }

    /// <summary>
    /// 综合测试：完整的编辑流程
    /// 需求: 1.4
    /// </summary>
    [Fact]
    public async Task CompleteEditFlow_RemoveAndAddSkills_ShouldWork()
    {
        // Arrange - 创建有技能要求的哨位
        var position = new PositionLocation
        {
            Name = "综合测试哨位",
            Location = "综合测试地点",
            RequiredSkillIds = new List<int> { 1, 2 }
        };
        var id = await _positionRepository.CreateAsync(position);

        // Step 1: 验证初始状态
        var initialPosition = await _positionService.GetByIdAsync(id);
        Assert.Equal(2, initialPosition.RequiredSkillIds.Count);

        // Step 2: 移除所有技能
        var removeSkillsDto = new UpdatePositionDto
        {
            Name = initialPosition.Name,
            Location = initialPosition.Location,
            RequiredSkillIds = new List<int>()
        };
        await _positionService.UpdateAsync(id, removeSkillsDto);
        var noSkillsPosition = await _positionService.GetByIdAsync(id);
        Assert.Empty(noSkillsPosition.RequiredSkillIds);

        // Step 3: 添加不同的技能
        var addSkillsDto = new UpdatePositionDto
        {
            Name = noSkillsPosition.Name,
            Location = noSkillsPosition.Location,
            RequiredSkillIds = new List<int> { 3 }
        };
        await _positionService.UpdateAsync(id, addSkillsDto);
        var finalPosition = await _positionService.GetByIdAsync(id);
        Assert.Single(finalPosition.RequiredSkillIds);
        Assert.Equal(3, finalPosition.RequiredSkillIds[0]);
    }

    #endregion

    #region 排班引擎兼容性测试

    /// <summary>
    /// 测试排班引擎对无技能要求哨位的处理
    /// 需求: 3.1, 3.2, 3.3, 3.4
    /// </summary>
    [Fact]
    public async Task SchedulingEngine_WithNoSkillRequirement_ShouldAllowAllPersonnel()
    {
        // Arrange - 创建无技能要求的哨位
        var noSkillPosition = new PositionLocation
        {
            Name = "无技能要求哨位",
            Location = "测试地点",
            RequiredSkillIds = new List<int>()
        };
        await _positionRepository.CreateAsync(noSkillPosition);

        var positions = await _positionRepository.GetAllAsync();
        var personnel = await _personnelRepository.GetAllAsync();
        
        var context = new SchedulingContext
        {
            Positions = positions,
            Personals = personnel,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            FixedPositionRules = new List<FixedPositionRule>(),
            ManualAssignments = new List<ManualAssignment>()
        };

        var validator = new ConstraintValidator(context);
        var noSkillPos = positions.First(p => p.Name == "无技能要求哨位");
        var noSkillPosIdx = positions.IndexOf(noSkillPos);

        // Act & Assert - 所有人员都应该能匹配无技能要求的哨位
        foreach (var person in personnel)
        {
            var personIdx = personnel.IndexOf(person);
            Assert.True(validator.ValidateSkillMatch(personIdx, noSkillPosIdx),
                $"人员 {person.Name} 应该能够分配到无技能要求的哨位");
        }
    }

    /// <summary>
    /// 测试混合场景：同时存在有技能要求和无技能要求的哨位
    /// 需求: 1.4, 3.1
    /// </summary>
    [Fact]
    public async Task SchedulingEngine_MixedPositions_ShouldWorkCorrectly()
    {
        // Arrange - 添加无技能要求的哨位
        var noSkillPosition = new PositionLocation
        {
            Name = "混合测试无技能哨位",
            Location = "测试地点",
            RequiredSkillIds = new List<int>()
        };
        await _positionRepository.CreateAsync(noSkillPosition);

        var positions = await _positionRepository.GetAllAsync();
        var personnel = await _personnelRepository.GetAllAsync();
        
        var context = new SchedulingContext
        {
            Positions = positions,
            Personals = personnel,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            FixedPositionRules = new List<FixedPositionRule>(),
            ManualAssignments = new List<ManualAssignment>()
        };

        var validator = new ConstraintValidator(context);

        // Act & Assert
        // 1. 有技能要求的哨位仍然正确匹配
        var eastGate = positions.First(p => p.Name == "东门哨位");
        var eastGateIdx = positions.IndexOf(eastGate);
        var zhangSan = personnel.First(p => p.Name == "张三");
        var zhangSanIdx = personnel.IndexOf(zhangSan);
        Assert.True(validator.ValidateSkillMatch(zhangSanIdx, eastGateIdx));

        // 2. 无技能要求的哨位允许所有人员
        var noSkillPos = positions.First(p => p.Name == "混合测试无技能哨位");
        var noSkillPosIdx = positions.IndexOf(noSkillPos);
        Assert.True(validator.ValidateSkillMatch(zhangSanIdx, noSkillPosIdx));
        
        var liSi = personnel.First(p => p.Name == "李四");
        var liSiIdx = personnel.IndexOf(liSi);
        Assert.True(validator.ValidateSkillMatch(liSiIdx, noSkillPosIdx));
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
