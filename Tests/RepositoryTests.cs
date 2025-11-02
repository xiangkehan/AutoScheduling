using System;
using System.Threading.Tasks;
using AutoScheduling3.Data;
using AutoScheduling3.Models;

namespace AutoScheduling3.Tests
{
    /// <summary>
    /// Repository 层单元测试
    /// 需求: 1.2, 2.1, 2.2
    /// </summary>
    public class RepositoryTests
    {
        private const string TestDbPath = ":memory:";

        /// <summary>
        /// 测试人员仓储 CRUD 操作
        /// </summary>
        public async Task<bool> TestPersonalRepositoryCrudAsync()
        {
            try
            {
                var repo = new PersonalRepository(TestDbPath);
                await repo.InitAsync();

                // 测试创建
                var person = new Personal
                {
                    Name = "测试人员",
                    IsAvailable = true,
                    IsRetired = false,
                    SkillIds = new List<int> { 1, 2 }
                };

                var id = await repo.CreateAsync(person);
                if (id <= 0) return false;

                // 测试读取
                var retrieved = await repo.GetByIdAsync(id);
                if (retrieved == null || retrieved.Name != person.Name) return false;

                // 测试更新
                retrieved.Name = "更新后的人员"; // 更新人员名称
                await repo.UpdateAsync(retrieved);

                var updated = await repo.GetByIdAsync(id);
                if (updated == null || updated.Name != "更新后的人员") return false;

                // 测试删除
                await repo.DeleteAsync(id);
                var deleted = await repo.GetByIdAsync(id);
                if (deleted != null) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 测试哨位仓储 CRUD 操作
        /// </summary>
        public async Task<bool> TestPositionRepositoryCrudAsync()
        {
            try
            {
                var repo = new PositionLocationRepository(TestDbPath);
                await repo.InitAsync();

                // 测试创建
                var position = new PositionLocation
                {
                    Name = "测试哨位",
                    Location = "测试地点",
                    Description = "测试描述",
                    Requirements = "测试要求"
                };

                var id = await repo.CreateAsync(position);
                if (id <= 0) return false;

                // 测试读取
                var retrieved = await repo.GetByIdAsync(id);
                if (retrieved == null || retrieved.Name != position.Name) return false;

                // 测试更新
                retrieved.Location = "更新地点";
                await repo.UpdateAsync(retrieved);

                var updated = await repo.GetByIdAsync(id);
                if (updated == null || updated.Location != "更新地点") return false;

                // 测试删除
                await repo.DeleteAsync(id);
                var deleted = await repo.GetByIdAsync(id);
                if (deleted != null) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 测试技能仓储 CRUD 操作
        /// </summary>
        public async Task<bool> TestSkillRepositoryCrudAsync()
        {
            try
            {
                var repo = new SkillRepository(TestDbPath);
                await repo.InitAsync();

                // 测试创建
                var skill = new Skill
                {
                    Name = "测试技能",
                    Description = "测试描述",
                    IsActive = true
                };

                var id = await repo.CreateAsync(skill);
                if (id <= 0) return false;

                // 测试读取
                var retrieved = await repo.GetByIdAsync(id);
                if (retrieved == null || retrieved.Name != skill.Name) return false;

                // 测试更新
                retrieved.Description = "更新描述";
                await repo.UpdateAsync(retrieved);

                var updated = await repo.GetByIdAsync(id);
                if (updated == null || updated.Description != "更新描述") return false;

                // 测试删除
                await repo.DeleteAsync(id);
                var deleted = await repo.GetByIdAsync(id);
                if (deleted != null) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 测试数据验证
        /// </summary>
        public async Task<bool> TestDataValidationAsync()
        {
            try
            {
                var repo = new PersonalRepository(TestDbPath);
                await repo.InitAsync();

                // 测试空名称验证
                var person = new Personal
                {
                    Name = "", // 空名称应该被处理
                    SkillIds = new List<int> { 1 }
                };

                var id = await repo.CreateAsync(person);
                var retrieved = await repo.GetByIdAsync(id);
                
                // 验证数据完整性
                return retrieved != null && 
                       retrieved.RecentPeriodShiftIntervals != null && 
                       retrieved.RecentPeriodShiftIntervals.Length == 12;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public async Task<TestResults> RunAllTestsAsync()
        {
            var results = new TestResults();

            results.PersonalCrudTest = await TestPersonalRepositoryCrudAsync();
            results.PositionCrudTest = await TestPositionRepositoryCrudAsync();
            results.SkillCrudTest = await TestSkillRepositoryCrudAsync();
            results.DataValidationTest = await TestDataValidationAsync();

            results.AllPassed = results.PersonalCrudTest && 
                               results.PositionCrudTest && 
                               results.SkillCrudTest && 
                               results.DataValidationTest;

            return results;
        }
    }

    /// <summary>
    /// 测试结果
    /// </summary>
    public class TestResults
    {
        public bool PersonalCrudTest { get; set; }
        public bool PositionCrudTest { get; set; }
        public bool SkillCrudTest { get; set; }
        public bool DataValidationTest { get; set; }
        public bool AllPassed { get; set; }

        public override string ToString()
        {
            return $"Personal CRUD: {(PersonalCrudTest ? "PASS" : "FAIL")}, " +
                   $"Position CRUD: {(PositionCrudTest ? "PASS" : "FAIL")}, " +
                   $"Skill CRUD: {(SkillCrudTest ? "PASS" : "FAIL")}, " +
                   $"Data Validation: {(DataValidationTest ? "PASS" : "FAIL")}, " +
                   $"Overall: {(AllPassed ? "PASS" : "FAIL")}";
        }
    }
}