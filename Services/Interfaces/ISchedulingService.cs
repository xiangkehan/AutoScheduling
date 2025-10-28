using AutoScheduling3.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 排班服务接口
/// </summary>
public interface ISchedulingService
{
    /// <summary>
    /// 执行排班算法
    /// </summary>
    Task<ScheduleDto> ExecuteSchedulingAsync(SchedulingRequestDto request);

    /// <summary>
    /// 获取草稿列表
    /// </summary>
    Task<List<ScheduleSummaryDto>> GetDraftsAsync();

    /// <summary>
    /// 根据ID获取排班详情
    /// </summary>
    Task<ScheduleDto?> GetScheduleByIdAsync(int id);

    /// <summary>
    /// 确认排班
    /// </summary>
    Task ConfirmScheduleAsync(int id);

    /// <summary>
    /// 删除草稿
    /// </summary>
    Task DeleteDraftAsync(int id);

    /// <summary>
    /// 获取历史记录
    /// </summary>
    Task<List<ScheduleSummaryDto>> GetHistoryAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 导出排班表
    /// </summary>
    Task<byte[]> ExportScheduleAsync(int id, string format);
}
