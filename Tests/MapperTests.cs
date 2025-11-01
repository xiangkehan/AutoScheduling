using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.Mappers;
using AutoScheduling3.Models;
using AutoScheduling3.Data.Interfaces;

namespace AutoScheduling3.Tests
{
    /// <summary>
    /// 映射器单元测试
    /// 需求: 2.1, 2.2, 3.1
    /// </summary>
    public class MapperTests
    {
        /// <summary>
        /// 测试技能映射器（不依赖外部服务）
        /// </summary>
        public bool TestSkillMapper()
        {
            try
            {
                var mapper = new SkillMapper();

                // 测试 Model 转 DTO
                var skill = new Skill
                {
                    Id = 1,
                    Name = "测试技能",
                    Description = "测试描述",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var dto = mapper.ToDto(skill);
                if (dto.Id != skill.Id || dto.Name != skill.Name || dto.Description != skill.Description)
                    return false;

                // 测试 DTO 转 Model
                var modelFromDto = mapper.ToModel(dto);
                if (modelFromDto.Id != dto.Id || modelFromDto.Name != dto.Name || modelFromDto.Description != dto.Description)
                    return false;

                // 测试 CreateSkillDto 转 Model
                var createDto = new CreateSkillDto
                {
                    Name = "新技能",
                    Description = "新描述"
                };

                var newModel = mapper.ToModel(createDto);
                if (newModel.Name != createDto.Name || newModel.Description != createDto.Description || !newModel.IsActive)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 测试人员映射器基本功能（不依赖异步方法）
        /// </summary>
        public bool TestPersonnelMapperBasic()
        {
            try
            {
                // 使用 null 仓储进行基本测试（仅测试同步方法）
                var mapper = new PersonnelMapper(null, null);

                // 测试 Model 转 DTO（同步版本）
                var personnel = new Personal
                {
                    Id = 1,
                    Name = "测试人员",
                    PositionId = 1,
                    SkillIds = new List<int> { 1, 2 },
                    IsAvailable = true,
                    IsRetired = false,
                    RecentShiftIntervalCount = 5,
                    RecentHolidayShiftIntervalCount = 10,
                    RecentPeriodShiftIntervals = new int[12] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }
                };

                var dto = mapper.ToDto(personnel);
                if (dto.Id != personnel.Id || dto.Name != personnel.Name || dto.PositionId != personnel.PositionId)
                    return false;

                if (dto.SkillIds.Count != personnel.SkillIds.Count || dto.RecentPeriodShiftIntervals.Length != 12)
                    return false;

                // 测试 DTO 转 Model
                var modelFromDto = mapper.ToModel(dto);
                if (modelFromDto.Id != dto.Id || modelFromDto.Name != dto.Name || modelFromDto.PositionId != dto.PositionId)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 测试哨位映射器基本功能（不依赖异步方法）
        /// </summary>
        public bool TestPositionMapperBasic()
        {
            try
            {
                // 使用 null 仓储进行基本测试（仅测试同步方法）
                var mapper = new PositionMapper(null);

                // 测试 Model 转 DTO（同步版本）
                var position = new PositionLocation
                {
                    Id = 1,
                    Name = "测试哨位",
                    Location = "测试地点",
                    Description = "测试描述",
                    Requirements = "测试要求",
                    RequiredSkillIds = new List<int> { 1, 2, 3 }
                };

                var dto = mapper.ToDto(position);
                if (dto.Id != position.Id || dto.Name != position.Name || dto.Location != position.Location)
                    return false;

                if (dto.RequiredSkillIds.Count != position.RequiredSkillIds.Count)
                    return false;

                // 测试 DTO 转 Model
                var modelFromDto = mapper.ToModel(dto);
                if (modelFromDto.Id != dto.Id || modelFromDto.Name != dto.Name || modelFromDto.Location != dto.Location)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 测试排班映射器基本功能（不依赖异步方法）
        /// </summary>
        public bool TestScheduleMapperBasic()
        {
            try
            {
                // 使用 null 仓储进行基本测试（仅测试同步方法）
                var mapper = new ScheduleMapper(null, null);

                // 测试 Model 转 DTO（同步版本）
                var schedule = new Schedule
                {
                    Id = 1,
                    Header = "测试排班表",
                    PersonnelIds = new List<int> { 1, 2, 3 },
                    PositionIds = new List<int> { 1, 2 },
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    Results = new List<SingleShift>()
                };

                var dto = mapper.ToDto(schedule);
                if (dto.Id != schedule.Id || dto.Title != schedule.Header)
                    return false;

                if (dto.PersonnelIds.Count != schedule.PersonnelIds.Count || dto.PositionIds.Count != schedule.PositionIds.Count)
                    return false;

                // 测试 DTO 转 Model
                var modelFromDto = mapper.ToModel(dto);
                if (modelFromDto.Id != dto.Id || modelFromDto.Header != dto.Title)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 运行所有映射器测试
        /// </summary>
        public MapperTestResults RunAllTests()
        {
            var results = new MapperTestResults();

            results.SkillMapperTest = TestSkillMapper();
            results.PersonnelMapperTest = TestPersonnelMapperBasic();
            results.PositionMapperTest = TestPositionMapperBasic();
            results.ScheduleMapperTest = TestScheduleMapperBasic();

            results.AllPassed = results.SkillMapperTest && 
                               results.PersonnelMapperTest && 
                               results.PositionMapperTest && 
                               results.ScheduleMapperTest;

            return results;
        }
    }

    /// <summary>
    /// 映射器测试结果
    /// </summary>
    public class MapperTestResults
    {
        public bool SkillMapperTest { get; set; }
        public bool PersonnelMapperTest { get; set; }
        public bool PositionMapperTest { get; set; }
        public bool ScheduleMapperTest { get; set; }
        public bool AllPassed { get; set; }

        public override string ToString()
        {
            return $"Skill Mapper: {(SkillMapperTest ? "PASS" : "FAIL")}, " +
                   $"Personnel Mapper: {(PersonnelMapperTest ? "PASS" : "FAIL")}, " +
                   $"Position Mapper: {(PositionMapperTest ? "PASS" : "FAIL")}, " +
                   $"Schedule Mapper: {(ScheduleMapperTest ? "PASS" : "FAIL")}, " +
                   $"Overall: {(AllPassed ? "PASS" : "FAIL")}";
        }
    }
}