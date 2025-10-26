using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.Models;
using AutoScheduling3.Models.Constraints;
using AutoScheduling3.Data;
using AutoScheduling3.History;
using AutoScheduling3.SchedulingEngine;
using AutoScheduling3.SchedulingEngine.Core;

namespace AutoScheduling3.Services
{
    /// <summary>
    /// 排班服务：统一的排班服务入口，整合所有数据访问和算法调用
    /// </summary>
    public class SchedulingService
    {
        private readonly string _dbPath;
        private readonly PersonalRepository _personalRepo;
        private readonly PositionLocationRepository _positionRepo;
        private readonly SkillRepository _skillRepo;
        private readonly ConstraintRepository _constraintRepo;
        private readonly HistoryManagement _historyMgmt;

        public SchedulingService(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
            _personalRepo = new PersonalRepository(dbPath);
            _positionRepo = new PositionLocationRepository(dbPath);
            _skillRepo = new SkillRepository(dbPath);
            _constraintRepo = new ConstraintRepository(dbPath);
            _historyMgmt = new HistoryManagement(dbPath);
        }

        /// <summary>
        /// 初始化所有数据库表
        /// </summary>
        public async Task InitializeAsync()
        {
            await _personalRepo.InitAsync();
            await _positionRepo.InitAsync();
            await _skillRepo.InitAsync();
            await _constraintRepo.InitAsync();
            await _historyMgmt.InitAsync();
        }

        /// <summary>
        /// 执行排班
        /// </summary>
        /// <param name="personalIds">参与排班的人员ID列表</param>
        /// <param name="positionIds">参与排班的哨位ID列表</param>
        /// <param name="startDate">排班开始日期</param>
        /// <param name="endDate">排班结束日期</param>
        /// <param name="useActiveHolidayConfig">是否使用活动的休息日配置</param>
        /// <returns>生成的排班表</returns>
        public async Task<Schedule> ExecuteSchedulingAsync(
            List<int> personalIds,
            List<int> positionIds,
            DateTime startDate,
            DateTime endDate,
            bool useActiveHolidayConfig = true)
        {
            // 第1步：加载数据
            var context = new SchedulingContext
            {
                Personals = await _personalRepo.GetByIdsAsync(personalIds),
                Positions = await _positionRepo.GetByIdsAsync(positionIds),
                Skills = await _skillRepo.GetAllAsync(),
                StartDate = startDate,
                EndDate = endDate
            };

            // 第2步：加载配置
            if (useActiveHolidayConfig)
            {
                context.HolidayConfig = await _constraintRepo.GetActiveHolidayConfigAsync();
            }

            context.FixedPositionRules = await _constraintRepo.GetAllFixedPositionRulesAsync(enabledOnly: true);
            context.ManualAssignments = await _constraintRepo.GetManualAssignmentsByDateRangeAsync(
                startDate, endDate, enabledOnly: true);

            // 第3步：加载历史数据
            context.LastConfirmedSchedule = await _historyMgmt.GetLastConfirmedScheduleAsync();

            // 第4步：执行调度算法
            var scheduler = new GreedyScheduler(context);
            var schedule = await scheduler.ExecuteAsync();

            // 第5步：保存到缓冲区
            int bufferId = await _historyMgmt.AddToBufferAsync(schedule);

            // 返回生成的排班表
            return schedule;
        }

        /// <summary>
        /// 确认排班（将缓冲区的排班移至历史记录）
        /// </summary>
        public async Task ConfirmSchedulingAsync(int bufferId)
        {
            await _historyMgmt.ConfirmBufferScheduleAsync(bufferId);

            // TODO: 更新人员的历史统计数据
            // 这需要读取确认的排班表，计算每个人员的新间隔数，然后更新到Personal表
        }

        /// <summary>
        /// 获取所有缓冲区排班
        /// </summary>
        public async Task<List<(Schedule Schedule, DateTime CreateTime, int BufferId)>> GetBufferSchedulesAsync()
        {
            return await _historyMgmt.GetAllBufferSchedulesAsync();
        }

        /// <summary>
        /// 获取所有历史排班
        /// </summary>
        public async Task<List<(Schedule Schedule, DateTime ConfirmTime)>> GetHistorySchedulesAsync()
        {
            return await _historyMgmt.GetAllHistorySchedulesAsync();
        }

        /// <summary>
        /// 删除缓冲区排班
        /// </summary>
        public async Task DeleteBufferScheduleAsync(int bufferId)
        {
            await _historyMgmt.DeleteBufferScheduleAsync(bufferId);
        }

        /// <summary>
        /// 清空所有缓冲区
        /// </summary>
        public async Task ClearBufferAsync()
        {
            await _historyMgmt.ClearBufferAsync();
        }

        #region 数据管理方法

        // 人员管理
        public async Task<int> AddPersonalAsync(Personal personal) => await _personalRepo.AddAsync(personal);
        public async Task<Personal?> GetPersonalAsync(int id) => await _personalRepo.GetByIdAsync(id);
        public async Task<List<Personal>> GetAllPersonalsAsync() => await _personalRepo.GetAllAsync();
        public async Task UpdatePersonalAsync(Personal personal) => await _personalRepo.UpdateAsync(personal);
        public async Task DeletePersonalAsync(int id) => await _personalRepo.DeleteAsync(id);

        // 哨位管理
        public async Task<int> AddPositionAsync(PositionLocation position) => await _positionRepo.AddAsync(position);
        public async Task<PositionLocation?> GetPositionAsync(int id) => await _positionRepo.GetByIdAsync(id);
        public async Task<List<PositionLocation>> GetAllPositionsAsync() => await _positionRepo.GetAllAsync();
        public async Task UpdatePositionAsync(PositionLocation position) => await _positionRepo.UpdateAsync(position);
        public async Task DeletePositionAsync(int id) => await _positionRepo.DeleteAsync(id);

        // 技能管理
        public async Task<int> AddSkillAsync(Skill skill) => await _skillRepo.AddAsync(skill);
        public async Task<Skill?> GetSkillAsync(int id) => await _skillRepo.GetByIdAsync(id);
        public async Task<List<Skill>> GetAllSkillsAsync() => await _skillRepo.GetAllAsync();
        public async Task UpdateSkillAsync(Skill skill) => await _skillRepo.UpdateAsync(skill);
        public async Task DeleteSkillAsync(int id) => await _skillRepo.DeleteAsync(id);

        // 定岗规则管理
        public async Task<int> AddFixedPositionRuleAsync(FixedPositionRule rule) => await _constraintRepo.AddFixedPositionRuleAsync(rule);
        public async Task<List<FixedPositionRule>> GetAllFixedPositionRulesAsync() => await _constraintRepo.GetAllFixedPositionRulesAsync(enabledOnly: false);
        public async Task UpdateFixedPositionRuleAsync(FixedPositionRule rule) => await _constraintRepo.UpdateFixedPositionRuleAsync(rule);
        public async Task DeleteFixedPositionRuleAsync(int id) => await _constraintRepo.DeleteFixedPositionRuleAsync(id);

        // 手动指定管理
        public async Task<int> AddManualAssignmentAsync(ManualAssignment assignment) => await _constraintRepo.AddManualAssignmentAsync(assignment);
        public async Task<List<ManualAssignment>> GetManualAssignmentsByDateRangeAsync(DateTime start, DateTime end) => 
            await _constraintRepo.GetManualAssignmentsByDateRangeAsync(start, end, enabledOnly: false);
        public async Task UpdateManualAssignmentAsync(ManualAssignment assignment) => await _constraintRepo.UpdateManualAssignmentAsync(assignment);
        public async Task DeleteManualAssignmentAsync(int id) => await _constraintRepo.DeleteManualAssignmentAsync(id);

        // 休息日配置管理
        public async Task<int> AddHolidayConfigAsync(HolidayConfig config) => await _constraintRepo.AddHolidayConfigAsync(config);
        public async Task<HolidayConfig?> GetActiveHolidayConfigAsync() => await _constraintRepo.GetActiveHolidayConfigAsync();
        public async Task<List<HolidayConfig>> GetAllHolidayConfigsAsync() => await _constraintRepo.GetAllHolidayConfigsAsync();
        public async Task UpdateHolidayConfigAsync(HolidayConfig config) => await _constraintRepo.UpdateHolidayConfigAsync(config);
        public async Task DeleteHolidayConfigAsync(int id) => await _constraintRepo.DeleteHolidayConfigAsync(id);

        #endregion
    }
}
