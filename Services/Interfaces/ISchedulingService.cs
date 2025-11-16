using AutoScheduling3.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.Models.Constraints; // 新增约束模型引用

namespace AutoScheduling3.Services.Interfaces;

/// <summary>
/// 排班服务接口
/// </summary>
public interface ISchedulingService
{
 /// <summary>
 /// 执行排班算法
 /// </summary>
 Task<ScheduleDto> ExecuteSchedulingAsync(SchedulingRequestDto request, CancellationToken cancellationToken = default);

 /// <summary>
 /// 执行排班算法（支持进度报告和取消）
 /// </summary>
 /// <param name="request">排班请求</param>
 /// <param name="progress">进度报告回调</param>
 /// <param name="cancellationToken">取消令牌</param>
 /// <returns>排班结果</returns>
 Task<SchedulingResult> ExecuteSchedulingAsync(SchedulingRequestDto request, IProgress<SchedulingProgressReport>? progress = null, CancellationToken cancellationToken = default);

 /// <summary>
 /// 获取草稿列表
 /// </summary>
 Task<List<ScheduleSummaryDto>> GetDraftsAsync();

 /// <summary>
 /// 根据ID获取排班详情（草稿或历史）
 /// </summary>
 Task<ScheduleDto?> GetScheduleByIdAsync(int id);

 /// <summary>
 /// 确认排班（草稿转历史）
 /// </summary>
 Task ConfirmScheduleAsync(int id);

 /// <summary>
 /// 删除草稿
 /// </summary>
 Task DeleteDraftAsync(int id);

 /// <summary>
 /// 获取历史记录（可按日期范围过滤）
 /// </summary>
 Task<List<ScheduleSummaryDto>> GetHistoryAsync(DateTime? startDate = null, DateTime? endDate = null);

 /// <summary>
 /// 导出排班表
 /// </summary>
 Task<byte[]> ExportScheduleAsync(int id, string format);

 // === 新增：前端向导需要的约束配置加载方法 ===

 /// <summary>
 /// 获取全部休息日配置列表
 /// </summary>
 Task<List<HolidayConfig>> GetHolidayConfigsAsync();

 /// <summary>
 /// 获取全部定岗规则（可选是否仅启用）
 /// </summary>
 Task<List<FixedPositionRule>> GetFixedPositionRulesAsync(bool enabledOnly = true);

 /// <summary>
 /// 获取指定日期范围内的手动指定
 /// </summary>
 Task<List<ManualAssignment>> GetManualAssignmentsAsync(DateTime startDate, DateTime endDate, bool enabledOnly = true);

 // === 新增：排班引擎集成方法 ===

 /// <summary>
 /// 获取排班引擎状态信息
 /// </summary>
 Task<Dictionary<string, object>> GetSchedulingEngineStatusAsync();

 /// <summary>
 /// 获取排班统计信息
 /// </summary>
 Task<ScheduleStatisticsDto> GetScheduleStatisticsAsync();

 /// <summary>
 /// 批量确认多个草稿排班
 /// </summary>
 Task ConfirmMultipleSchedulesAsync(List<int> scheduleIds);

 /// <summary>
 /// 清理过期的草稿排班
 /// </summary>
 Task CleanupExpiredDraftsAsync(int daysToKeep = 7);

 /// <summary>
 /// 创建手动指定
 /// </summary>
 Task<ManualAssignmentDto> CreateManualAssignmentAsync(CreateManualAssignmentDto dto);

 /// <summary>
 /// 构建排班表格数据
 /// </summary>
 /// <param name="scheduleDto">排班DTO</param>
 /// <returns>排班表格数据</returns>
 Task<ScheduleGridData> BuildScheduleGridData(ScheduleDto scheduleDto);
}
