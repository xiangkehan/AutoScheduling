using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoScheduling3.Models;
using AutoScheduling3.Models.Constraints;
using AutoScheduling3.Services;

namespace AutoScheduling3.Tests
{
    /// <summary>
    /// 集成测试：测试完整的排班流程 (MSTest)
    /// </summary>
    [TestClass]
    public class IntegrationTests
    {
        private string _testDbPath = null!;
        private SchedulingService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            // 为每个测试创建临时数据库
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_scheduling_{Guid.NewGuid()}.db");
            _service = new SchedulingService(_testDbPath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // 清理测试数据库
            if (File.Exists(_testDbPath))
            {
                try
                {
                    File.Delete(_testDbPath);
                }
                catch
                {
                    // 忽略删除失败
                }
            }
        }

        [TestMethod]
        public async Task FullSchedulingWorkflow_WithBasicData_Succeeds()
        {
            // Arrange - 初始化数据库
            await _service.InitializeAsync();

            // 创建技能
            var basicSkillId = await _service.AddSkillAsync(new Skill
            {
                Name = "基础技能",
                Description = "基础哨位技能"
            });

            // 创建哨位
            var position1Id = await _service.AddPositionAsync(new PositionLocation
            {
                Name = "1号哨位",
                Location = "东门",
                Description = "主入口",
                Requirements = "基础技能",
                RequiredSkillIds = new List<int> { basicSkillId }
            });

            var position2Id = await _service.AddPositionAsync(new PositionLocation
            {
                Name = "2号哨位",
                Location = "西门",
                Description = "侧门",
                Requirements = "基础技能",
                RequiredSkillIds = new List<int> { basicSkillId }
            });

            // 创建人员
            var personIds = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                var personId = await _service.AddPersonalAsync(new Personal
                {
                    Name = $"人员{i + 1}",
                    PositionId = 1,
                    SkillIds = new List<int> { basicSkillId },
                    IsAvailable = true,
                    IsRetired = false,
                    RecentShiftIntervalCount = i * 2,
                    RecentHolidayShiftIntervalCount = i * 3,
                    RecentPeriodShiftIntervals = new int[12]
                });
                personIds.Add(personId);
            }

            // 配置休息日
            await _service.AddHolidayConfigAsync(new HolidayConfig
            {
                ConfigName = "测试配置",
                EnableWeekendRule = true,
                WeekendDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
                IsActive = true
            });

            // Act - 执行排班
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 3); // 排3天

            var schedule = await _service.ExecuteSchedulingAsync(
                personalIds: personIds,
                positionIds: new List<int> { position1Id, position2Id },
                startDate: startDate,
                endDate: endDate,
                useActiveHolidayConfig: true
            );

            // Assert - 验证结果
            Assert.IsNotNull(schedule);
            Assert.IsTrue(schedule.Shifts.Count > 0, "应该生成班次");
            
            // 验证日期范围
            var minDate = schedule.Shifts.Min(s => s.StartTime.Date);
            var maxDate = schedule.Shifts.Max(s => s.EndTime.Date);
            Assert.IsTrue(minDate >= startDate, "班次开始日期应在范围内");
            Assert.IsTrue(maxDate <= endDate.AddDays(1), "班次结束日期应在范围内");

            // 验证人员ID和哨位ID
            var usedPersonIds = schedule.Shifts.Select(s => s.PersonalId).Distinct().ToList();
            var usedPositionIds = schedule.Shifts.Select(s => s.PositionId).Distinct().ToList();
            
            foreach (var pid in usedPersonIds)
            {
                Assert.IsTrue(personIds.Contains(pid), $"人员{pid}应在参与列表中");
            }

            foreach (var posId in usedPositionIds)
            {
                Assert.IsTrue(posId == position1Id || posId == position2Id, 
                    $"哨位{posId}应在参与列表中");
            }

            Console.WriteLine($"✓ 排班成功：共生成 {schedule.Shifts.Count} 个班次");
            Console.WriteLine($"  参与人员数：{usedPersonIds.Count}/{personIds.Count}");
            Console.WriteLine($"  涉及哨位数：{usedPositionIds.Count}/2");
        }

        [TestMethod]
        public async Task SchedulingWithFixedPositionRule_RespectsConstraints()
        {
            // Arrange
            await _service.InitializeAsync();

            var skillId = await _service.AddSkillAsync(new Skill { Name = "技能1" });
            var pos1Id = await _service.AddPositionAsync(new PositionLocation
            {
                Name = "哨位1",
                RequiredSkillIds = new List<int> { skillId }
            });
            var pos2Id = await _service.AddPositionAsync(new PositionLocation
            {
                Name = "哨位2",
                RequiredSkillIds = new List<int> { skillId }
            });

            var person1Id = await _service.AddPersonalAsync(new Personal
            {
                Name = "专职夜哨人员",
                SkillIds = new List<int> { skillId },
                IsAvailable = true
            });

            var person2Id = await _service.AddPersonalAsync(new Personal
            {
                Name = "普通人员",
                SkillIds = new List<int> { skillId },
                IsAvailable = true
            });

            // 配置定岗规则：person1 只能夜哨
            await _service.AddFixedPositionRuleAsync(new FixedPositionRule
            {
                PersonalId = person1Id,
                AllowedPeriods = new List<int> { 11, 0, 1, 2 }, // 夜哨时段
                IsEnabled = true
            });

            await _service.AddHolidayConfigAsync(new HolidayConfig
            {
                ConfigName = "测试",
                EnableWeekendRule = false,
                IsActive = true
            });

            // Act
            var schedule = await _service.ExecuteSchedulingAsync(
                personalIds: new List<int> { person1Id, person2Id },
                positionIds: new List<int> { pos1Id, pos2Id },
                startDate: new DateTime(2024, 1, 1),
                endDate: new DateTime(2024, 1, 1),
                useActiveHolidayConfig: true
            );

            // Assert
            var person1Shifts = schedule.Shifts.Where(s => s.PersonalId == person1Id).ToList();
            if (person1Shifts.Count > 0)
            {
                foreach (var shift in person1Shifts)
                {
                    int hour = shift.StartTime.Hour;
                    int periodIdx = hour / 2;
                    
                    // 验证 person1 只在夜哨时段
                    Assert.IsTrue(periodIdx == 11 || periodIdx == 0 || periodIdx == 1 || periodIdx == 2,
                        $"专职夜哨人员应只在夜哨时段，实际时段：{periodIdx}");
                }
            }

            Console.WriteLine($"✓ 定岗规则测试通过：person1 共 {person1Shifts.Count} 个班次，均为夜哨");
        }

        [TestMethod]
        public async Task BufferAndConfirmWorkflow_WorksCorrectly()
        {
            // Arrange
            await _service.InitializeAsync();
            
            var skillId = await _service.AddSkillAsync(new Skill { Name = "技能" });
            var posId = await _service.AddPositionAsync(new PositionLocation
            {
                Name = "哨位",
                RequiredSkillIds = new List<int> { skillId }
            });
            var personId = await _service.AddPersonalAsync(new Personal
            {
                Name = "人员",
                SkillIds = new List<int> { skillId },
                IsAvailable = true
            });

            await _service.AddHolidayConfigAsync(new HolidayConfig
            {
                ConfigName = "测试",
                EnableWeekendRule = false,
                IsActive = true
            });

            // Act - 执行排班（自动进入缓冲区）
            var schedule = await _service.ExecuteSchedulingAsync(
                personalIds: new List<int> { personId },
                positionIds: new List<int> { posId },
                startDate: new DateTime(2024, 1, 1),
                endDate: new DateTime(2024, 1, 1),
                useActiveHolidayConfig: true
            );

            // 查询缓冲区
            var bufferSchedules = await _service.GetBufferSchedulesAsync();
            Assert.AreEqual(1, bufferSchedules.Count, "应该有1个缓冲区排班");

            var (bufferSchedule, createTime, bufferId) = bufferSchedules[0];
            Assert.IsNotNull(bufferSchedule);

            // 确认排班
            await _service.ConfirmSchedulingAsync(bufferId);

            // 验证已移至历史
            var historySchedules = await _service.GetHistorySchedulesAsync();
            Assert.AreEqual(1, historySchedules.Count, "应该有1个历史排班");

            var bufferSchedulesAfterConfirm = await _service.GetBufferSchedulesAsync();
            Assert.AreEqual(0, bufferSchedulesAfterConfirm.Count, "确认后缓冲区应为空");

            Console.WriteLine("✓ 缓冲区和确认流程测试通过");
        }
    }
}
