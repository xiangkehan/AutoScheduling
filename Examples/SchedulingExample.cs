using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.Models;
using AutoScheduling3.Models.Constraints;
using AutoScheduling3.Services;

namespace AutoScheduling3.Examples
{
    /// <summary>
    /// 排班系统使用示例
    /// </summary>
    public class SchedulingExample
    {
        private readonly SchedulingService _service;

        public SchedulingExample()
        {
            // 使用本地数据库文件
            _service = new SchedulingService("scheduling_example.db");
        }

        /// <summary>
        /// 运行完整示例
        /// </summary>
        public async Task RunExampleAsync()
        {
            Console.WriteLine("=== 自动排班系统示例 ===\n");

            // 第1步：初始化数据库
            await InitializeDatabaseAsync();

            // 第2步：设置基础数据
            await SetupBasicDataAsync();

            // 第3步：配置约束
            await SetupConstraintsAsync();

            // 第4步：执行排班
            await ExecuteSchedulingAsync();

            // 第5步：查看结果
            await ViewResultsAsync();

            Console.WriteLine("\n=== 示例完成 ===");
        }

        /// <summary>
        /// 第1步：初始化数据库
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            Console.WriteLine("第1步：初始化数据库...");
            await _service.InitializeAsync();
            Console.WriteLine("✓ 数据库初始化完成\n");
        }

        /// <summary>
        /// 第2步：设置基础数据
        /// </summary>
        private async Task SetupBasicDataAsync()
        {
            Console.WriteLine("第2步：设置基础数据...");

            // 创建技能
            var basicSkillId = await _service.AddSkillAsync(new Skill
            {
                Name = "基础哨位技能",
                Description = "所有哨位的基本要求"
            });

            var advancedSkillId = await _service.AddSkillAsync(new Skill
            {
                Name = "高级哨位技能",
                Description = "重点哨位的额外要求"
            });

            Console.WriteLine($"✓ 创建技能：基础({basicSkillId}), 高级({advancedSkillId})");

            // 创建哨位
            var position1Id = await _service.AddPositionAsync(new PositionLocation
            {
                Name = "1号哨位",
                Location = "东门",
                Description = "主入口监控",
                Requirements = "需要基础技能",
                RequiredSkillIds = new List<int> { basicSkillId }
            });

            var position2Id = await _service.AddPositionAsync(new PositionLocation
            {
                Name = "2号哨位",
                Location = "西门",
                Description = "侧门监控",
                Requirements = "需要基础技能",
                RequiredSkillIds = new List<int> { basicSkillId }
            });

            var position3Id = await _service.AddPositionAsync(new PositionLocation
            {
                Name = "3号哨位",
                Location = "监控室",
                Description = "重点监控",
                Requirements = "需要高级技能",
                RequiredSkillIds = new List<int> { basicSkillId, advancedSkillId }
            });

            Console.WriteLine($"✓ 创建哨位：1号({position1Id}), 2号({position2Id}), 3号({position3Id})");

            // 创建人员
            var personNames = new[] { "张三", "李四", "王五", "赵六", "钱七", "孙八", "周九", "吴十" };
            for (int i = 0; i < personNames.Length; i++)
            {
                var skillIds = i < 2 
                    ? new List<int> { basicSkillId, advancedSkillId }  // 前2人有高级技能
                    : new List<int> { basicSkillId };                   // 其他人只有基础技能

                var personId = await _service.AddPersonalAsync(new Personal
                {
                    Name = personNames[i],
                    PositionId = 1,
                    SkillIds = skillIds,
                    IsAvailable = true,
                    IsRetired = false,
                    RecentShiftIntervalCount = i * 2,  // 模拟不同的休息时间
                    RecentHolidayShiftIntervalCount = i * 3,
                    RecentPeriodShiftIntervals = new int[12]
                });
            }

            Console.WriteLine($"✓ 创建人员：共{personNames.Length}人\n");
        }

        /// <summary>
        /// 第3步：配置约束
        /// </summary>
        private async Task SetupConstraintsAsync()
        {
            Console.WriteLine("第3步：配置约束...");

            // 配置休息日
            var holidayConfigId = await _service.AddHolidayConfigAsync(new HolidayConfig
            {
                ConfigName = "2024年1月休息日配置",
                EnableWeekendRule = true,
                WeekendDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
                LegalHolidays = new List<DateTime>
                {
                    new DateTime(2024, 1, 1)  // 元旦
                },
                CustomHolidays = new List<DateTime>(),
                ExcludedDates = new List<DateTime>(),
                IsActive = true
            });

            Console.WriteLine($"✓ 配置休息日规则 (ID: {holidayConfigId})");

            // 配置定岗规则（示例：张三只能在夜哨时段）
            var ruleId = await _service.AddFixedPositionRuleAsync(new FixedPositionRule
            {
                PersonalId = 1,
                AllowedPositionIds = new List<int>(), // 空表示不限制哨位
                AllowedPeriods = new List<int> { 11, 0, 1, 2 }, // 只能夜哨
                IsEnabled = true,
                Description = "张三专职夜哨"
            });

            Console.WriteLine($"✓ 配置定岗规则 (ID: {ruleId})");

            // 配置手动指定（示例：指定李四在1月1日早班）
            var assignmentId = await _service.AddManualAssignmentAsync(new ManualAssignment
            {
                PositionId = 1,
                PeriodIndex = 3,  // 早晨时段
                PersonalId = 2,
                Date = new DateTime(2024, 1, 1),
                IsEnabled = true,
                Remarks = "元旦值班安排"
            });

            Console.WriteLine($"✓ 配置手动指定 (ID: {assignmentId})\n");
        }

        /// <summary>
        /// 第4步：执行排班
        /// </summary>
        private async Task ExecuteSchedulingAsync()
        {
            Console.WriteLine("第4步：执行排班算法...");

            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 7); // 排一周

            try
            {
                var schedule = await _service.ExecuteSchedulingAsync(
                    personalIds: new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 },
                    positionIds: new List<int> { 1, 2, 3 },
                    startDate: startDate,
                    endDate: endDate,
                    useActiveHolidayConfig: true
                );

                Console.WriteLine($"✓ 排班完成：{schedule.Title}");
                Console.WriteLine($"  生成班次数：{schedule.Shifts.Count}");
                Console.WriteLine($"  参与人员：{schedule.PersonalIds.Count}人");
                Console.WriteLine($"  涉及哨位：{schedule.PositionIds.Count}个\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 排班失败：{ex.Message}\n");
            }
        }

        /// <summary>
        /// 第5步：查看结果
        /// </summary>
        private async Task ViewResultsAsync()
        {
            Console.WriteLine("第5步：查看排班结果...\n");

            // 查看缓冲区排班
            var bufferSchedules = await _service.GetBufferSchedulesAsync();
            Console.WriteLine($"缓冲区排班数量：{bufferSchedules.Count}");

            foreach (var (bufferSchedule, createTime, bufferId) in bufferSchedules)
            {
                Console.WriteLine($"\n--- 缓冲区排班 {bufferId} ---");
                Console.WriteLine($"标题：{bufferSchedule.Title}");
                Console.WriteLine($"创建时间：{createTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"班次总数：{bufferSchedule.Shifts.Count}");

                // 显示前20个班次
                Console.WriteLine("\n前20个班次示例：");
                foreach (var shift in bufferSchedule.Shifts.Take(20))
                {
                    Console.WriteLine($"  {shift.StartTime:yyyy-MM-dd HH:mm} - {shift.EndTime:HH:mm} | 哨位{shift.PositionId} | 人员{shift.PersonalId}");
                }

                // 确认该排班
                Console.Write($"\n是否确认此排班？(y/n): ");
                // 在实际应用中，这里应该等待用户输入
                // 为了示例，我们自动确认
                bool confirm = true;

                if (confirm)
                {
                    await _service.ConfirmSchedulingAsync(bufferId);
                    Console.WriteLine("✓ 排班已确认并移入历史记录");
                }
            }

            // 查看历史排班
            var historySchedules = await _service.GetHistorySchedulesAsync();
            Console.WriteLine($"\n历史排班数量：{historySchedules.Count}");

            foreach (var (historySchedule, confirmTime) in historySchedules.Take(3))
            {
                Console.WriteLine($"\n--- 历史排班 ---");
                Console.WriteLine($"标题：{historySchedule.Title}");
                Console.WriteLine($"确认时间：{confirmTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"班次总数：{historySchedule.Shifts.Count}");
            }
        }

        /// <summary>
        /// 运行示例的静态入口
        /// </summary>
        public static async Task Main(string[] args)
        {
            var example = new SchedulingExample();
            await example.RunExampleAsync();

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
