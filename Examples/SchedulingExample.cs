using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.Models;
using AutoScheduling3.Models.Constraints;
using AutoScheduling3.Services;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Data;
using AutoScheduling3.Data.Interfaces;
using AutoScheduling3.History;

namespace AutoScheduling3.Examples
{
    /// <summary>
    /// 排班系统使用示例（已简化以匹配新版接口）
    /// </summary>
    public class SchedulingExample
    {
        private readonly ISchedulingService _service;

        public SchedulingExample()
        {
            string db = "scheduling_example.db"; // 手动装配依赖用于示例
            var personalRepo = new PersonalRepository(db);
            var positionRepo = new PositionLocationRepository(db);
            var skillRepo = new SkillRepository(db);
            var constraintRepo = new ConstraintRepository(db);
            var historyMgmt = new HistoryManagement(db);
            _service = new SchedulingService(personalRepo, positionRepo, skillRepo, constraintRepo, historyMgmt);
            ((SchedulingService)_service).InitializeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 运行完整示例
        /// </summary>
        public async Task RunExampleAsync()
        {
            Console.WriteLine("=== 自动排班系统示例(简化版) ===\n");

            // 第4步：执行排班
            await ExecuteSchedulingAsync();

            // 第5步：查看结果
            await ViewResultsAsync();

            Console.WriteLine("\n=== 示例完成 ===");
        }

        /// <summary>
        /// 第4步：执行排班
        /// </summary>
        private async Task ExecuteSchedulingAsync()
        {
            Console.WriteLine("执行排班...");
            var request = new SchedulingRequestDto
            {
                Title = "示例排班表",
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 1, 3),
                PersonnelIds = new List<int> { 1, 2, 3 }, // 假设数据库已存在这些人员
                PositionIds = new List<int> { 1, 2 },      // 假设数据库已存在这些哨位
                UseActiveHolidayConfig = true
            };
            try
            {
                var result = await _service.ExecuteSchedulingAsync(request, null, CancellationToken.None);
                if (result.IsSuccess && result.Schedule != null)
                {
                    Console.WriteLine($"✓ 排班完成：{result.Schedule.Title} 班次数={result.Schedule.Shifts.Count}");
                }
                else
                {
                    Console.WriteLine($"✗ 排班失败：{result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 排班失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 第5步：查看结果
        /// </summary>
        private async Task ViewResultsAsync()
        {
            Console.WriteLine("\n草稿列表:");
            var drafts = await _service.GetDraftsAsync();
            foreach (var d in drafts)
            {
                Console.WriteLine($"Draft {d.Id} {d.Title} {d.StartDate:yyyy-MM-dd}~{d.EndDate:yyyy-MM-dd} Shifts={d.ShiftCount}");
            }
            if (drafts.Any())
            {
                var first = drafts.First();
                Console.WriteLine("确认第一个草稿...");
                await _service.ConfirmScheduleAsync(first.Id);
            }
            Console.WriteLine("\n历史列表:");
            var histories = await _service.GetHistoryAsync();
            foreach (var h in histories)
            {
                Console.WriteLine($"History {h.Id} {h.Title} Confirmed={h.ConfirmedAt:yyyy-MM-dd HH:mm:ss}");
            }
        }
    }
}
