using System;
using System.Threading.Tasks;
using AutoScheduling3.Services;
using AutoScheduling3.Data;
using AutoScheduling3.DTOs.Mappers;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Tests
{
    /// <summary>
    /// 技能服务单元测试
    /// 需求: 2.2
    /// </summary>
    public class SkillServiceTests
    {
        private const string TestDbPath = ":memory:";

        /// <summary>
        /// 测试技能服务 CRUD 操作
        /// </summary>
        public async Task<bool> TestSkillServiceCrudAsync()
        {
            try
            {
                // 初始化依赖
                var skillRepo = new SkillRepository(TestDbPath);
                var personnelRepo = new PersonalRepository(TestDbPath);
                var positionRepo = new PositionLocationRepository(TestDbPath);
                var mapper = new SkillMapper();

                await skillRepo.InitAsync();
                await personnelRepo.InitAsync();
                await positionRepo.InitAsync();

                var service = new SkillService(skillRepo, personnelRepo, positionRepo, mapper);

                // 测试创建技能
                var createDto = new CreateSkillDto
                {
                    Name = "测试技能",
                    Description = "这是一个测试技能"
                };

                var createdSkill = await service.CreateAsync(createDto);
                if (createdSkill == null || createdSkill.Id <= 0) return false;
                if (createdSkill.Name != createDto.Name) return false;

                // 测试获取技能
                var retrievedSkill = await service.GetByIdAsync(createdSkill.Id);
                if (retrievedSkill == null || retrievedSkill.Name != createDto.Name) return false;

                // 测试更新技能
                var updateDto = new UpdateSkillDto
                {
                    Name = "更新后的技能",
                    Description = "更新后的描述",
                    IsActive = true
                };

                await service.UpdateAsync(createdSkill.Id, updateDto);
                var updatedSkill = await service.GetByIdAsync(createdSkill.Id);
                if (updatedSkill == null || updatedSkill.Name != updateDto.Name) return false;

                // 测试获取所有技能
                var allSkills = await service.GetAllAsync();
                if (allSkills == null || allSkills.Count == 0) return false;

                // 测试删除技能（应该成功，因为没有被使用）
                await service.DeleteAsync(createdSkill.Id);
                var deletedSkill = await service.GetByIdAsync(createdSkill.Id);
                if (deletedSkill != null) return false;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试技能名称唯一性验证
        /// </summary>
        public async Task<bool> TestSkillNameUniquenessAsync()
        {
            try
            {
                // 初始化依赖
                var skillRepo = new SkillRepository(TestDbPath);
                var personnelRepo = new PersonalRepository(TestDbPath);
                var positionRepo = new PositionLocationRepository(TestDbPath);
                var mapper = new SkillMapper();

                await skillRepo.InitAsync();
                await personnelRepo.InitAsync();
                await positionRepo.InitAsync();

                var service = new SkillService(skillRepo, personnelRepo, positionRepo, mapper);

                // 创建第一个技能
                var createDto1 = new CreateSkillDto
                {
                    Name = "唯一技能",
                    Description = "第一个技能"
                };

                var skill1 = await service.CreateAsync(createDto1);

                // 尝试创建同名技能（应该失败）
                var createDto2 = new CreateSkillDto
                {
                    Name = "唯一技能", // 相同名称
                    Description = "第二个技能"
                };

                try
                {
                    await service.CreateAsync(createDto2);
                    return false; // 如果没有抛出异常，测试失败
                }
                catch (ArgumentException)
                {
                    // 预期的异常，测试通过
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试技能使用情况检查
        /// </summary>
        public async Task<bool> TestSkillUsageCheckAsync()
        {
            try
            {
                // 初始化依赖
                var skillRepo = new SkillRepository(TestDbPath);
                var personnelRepo = new PersonalRepository(TestDbPath);
                var positionRepo = new PositionLocationRepository(TestDbPath);
                var mapper = new SkillMapper();

                await skillRepo.InitAsync();
                await personnelRepo.InitAsync();
                await positionRepo.InitAsync();

                var service = new SkillService(skillRepo, personnelRepo, positionRepo, mapper);

                // 创建技能
                var createDto = new CreateSkillDto
                {
                    Name = "被使用的技能",
                    Description = "这个技能会被人员使用"
                };

                var skill = await service.CreateAsync(createDto);

                // 检查使用情况（应该没有被使用）
                var usageInfo = await service.CheckSkillUsageAsync(skill.Id);
                if (usageInfo.IsInUse) return false;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 运行所有技能服务测试
        /// </summary>
        public async Task<SkillServiceTestResults> RunAllTestsAsync()
        {
            var results = new SkillServiceTestResults();

            results.CrudTest = await TestSkillServiceCrudAsync();
            results.UniquenessTest = await TestSkillNameUniquenessAsync();
            results.UsageCheckTest = await TestSkillUsageCheckAsync();

            results.AllPassed = results.CrudTest && 
                               results.UniquenessTest && 
                               results.UsageCheckTest;

            return results;
        }
    }

    /// <summary>
    /// 技能服务测试结果
    /// </summary>
    public class SkillServiceTestResults
    {
        public bool CrudTest { get; set; }
        public bool UniquenessTest { get; set; }
        public bool UsageCheckTest { get; set; }
        public bool AllPassed { get; set; }

        public override string ToString()
        {
            return $"CRUD Test: {(CrudTest ? "PASS" : "FAIL")}, " +
                   $"Uniqueness Test: {(UniquenessTest ? "PASS" : "FAIL")}, " +
                   $"Usage Check Test: {(UsageCheckTest ? "PASS" : "FAIL")}, " +
                   $"Overall: {(AllPassed ? "PASS" : "FAIL")}";
        }
    }
}