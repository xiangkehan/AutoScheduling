using AutoScheduling3.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Data.Interfaces
{
    /// <summary>
    /// 排班仓储接口：管理排班历史和缓冲区
    /// 需求: 1.2, 2.1, 2.2, 4.1, 4.2, 4.3, 4.4
    /// </summary>
    public interface ISchedulingRepository
    {
        /// <summary>
        /// 获取所有排班表
        /// </summary>
        Task<List<Schedule>> GetAllAsync();

        /// <summary>
        /// 根据ID获取排班表
        /// </summary>
        Task<Schedule?> GetByIdAsync(int id);

        /// <summary>
        /// 创建排班表
        /// </summary>
        Task<int> CreateAsync(Schedule schedule);

        /// <summary>
        /// 更新排班表
        /// </summary>
        Task UpdateAsync(Schedule schedule);

        /// <summary>
        /// 删除排班表
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// 检查排班表是否存在
        /// </summary>
        Task<bool> ExistsAsync(int id);

        /// <summary>
        /// 获取历史缓冲区中的排班表（未确认）
        /// </summary>
        Task<List<Schedule>> GetBufferSchedulesAsync();

        /// <summary>
        /// 获取确定历史区中的排班表（已确认）
        /// </summary>
        Task<List<Schedule>> GetConfirmedSchedulesAsync();

        /// <summary>
        /// 获取历史缓冲区中的单次排班
        /// </summary>
        Task<List<SingleShift>> GetBufferShiftsAsync();

        /// <summary>
        /// 获取确定历史区中的单次排班
        /// </summary>
        Task<List<SingleShift>> GetConfirmedShiftsAsync();

        /// <summary>
        /// 将缓冲区数据转移到确定历史区
        /// </summary>
        Task MoveBufferToHistoryAsync();

        /// <summary>
        /// 清空历史缓冲区
        /// </summary>
        Task ClearBufferAsync();

        /// <summary>
        /// 确认排班实施
        /// </summary>
        Task ConfirmScheduleAsync(int scheduleId);

        /// <summary>
        /// 获取指定日期范围内的排班记录
        /// </summary>
        Task<List<SingleShift>> GetShiftsByDateRangeAsync(DateTime startDate, DateTime endDate, bool confirmedOnly = true);

        /// <summary>
        /// 获取指定人员的最近排班记录
        /// </summary>
        Task<List<SingleShift>> GetRecentShiftsByPersonnelAsync(int personnelId, int days = 30);

        /// <summary>
        /// 保存排班结果到缓冲区
        /// </summary>
        Task SaveToBufferAsync(Schedule schedule);
    }
}