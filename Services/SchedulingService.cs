using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoScheduling3.Models;
using AutoScheduling3.Models.Constraints;
using AutoScheduling3.Data;
using AutoScheduling3.History;
using AutoScheduling3.SchedulingEngine;
using AutoScheduling3.SchedulingEngine.Core;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Services;

public class SchedulingService : ISchedulingService
{
 private readonly PersonalRepository _personalRepo;
 private readonly PositionLocationRepository _positionRepo;
 private readonly SkillRepository _skillRepo;
 private readonly ConstraintRepository _constraintRepo;
 private readonly HistoryManagement _historyMgmt;

 public SchedulingService(string dbPath)
 {
 if (string.IsNullOrWhiteSpace(dbPath)) throw new ArgumentNullException(nameof(dbPath));
 _personalRepo = new PersonalRepository(dbPath);
 _positionRepo = new PositionLocationRepository(dbPath);
 _skillRepo = new SkillRepository(dbPath);
 _constraintRepo = new ConstraintRepository(dbPath);
 _historyMgmt = new HistoryManagement(dbPath);
 }

 public async Task InitializeAsync()
 {
 await _personalRepo.InitAsync();
 await _positionRepo.InitAsync();
 await _skillRepo.InitAsync();
 await _constraintRepo.InitAsync();
 await _historyMgmt.InitAsync();
 }

 public async Task<ScheduleDto> ExecuteSchedulingAsync(SchedulingRequestDto request, CancellationToken cancellationToken = default)
 {
 if (request == null) throw new ArgumentNullException(nameof(request));
 ValidateRequest(request);
 cancellationToken.ThrowIfCancellationRequested();

 // 并行加载基础数据
 var personalsTask = _personalRepo.GetByIdsAsync(request.PersonnelIds);
 var positionsTask = _positionRepo.GetByIdsAsync(request.PositionIds);
 var skillsTask = _skillRepo.GetAllAsync();
 await Task.WhenAll(personalsTask, positionsTask, skillsTask);

 // 构建上下文
 var context = new SchedulingContext
 {
 Personals = personalsTask.Result,
 Positions = positionsTask.Result,
 Skills = skillsTask.Result,
 StartDate = request.StartDate.Date,
 EndDate = request.EndDate.Date
 };
 cancellationToken.ThrowIfCancellationRequested();

 //休息日配置
 if (request.UseActiveHolidayConfig)
 {
 context.HolidayConfig = await _constraintRepo.GetActiveHolidayConfigAsync();
 }
 else if (request.HolidayConfigId.HasValue)
 {
 var allConfigs = await _constraintRepo.GetAllHolidayConfigsAsync();
 context.HolidayConfig = allConfigs.FirstOrDefault(c => c.Id == request.HolidayConfigId.Value);
 }
 cancellationToken.ThrowIfCancellationRequested();

 // 定岗规则
 if (request.EnabledFixedRuleIds?.Count >0)
 {
 var allRules = await _constraintRepo.GetAllFixedPositionRulesAsync(enabledOnly: true);
 context.FixedPositionRules = allRules.Where(r => request.EnabledFixedRuleIds.Contains(r.Id)).ToList();
 }
 else
 {
 context.FixedPositionRules = await _constraintRepo.GetAllFixedPositionRulesAsync(enabledOnly: true);
 }
 cancellationToken.ThrowIfCancellationRequested();

 // 手动指定
 if (request.EnabledManualAssignmentIds?.Count >0)
 {
 var manualRange = await _constraintRepo.GetManualAssignmentsByDateRangeAsync(request.StartDate, request.EndDate, enabledOnly: true);
 context.ManualAssignments = manualRange.Where(m => request.EnabledManualAssignmentIds.Contains(m.Id)).ToList();
 }
 else
 {
 context.ManualAssignments = await _constraintRepo.GetManualAssignmentsByDateRangeAsync(request.StartDate, request.EndDate, enabledOnly: true);
 }
 cancellationToken.ThrowIfCancellationRequested();

 // 最近历史排班（用于间隔评分等）
 context.LastConfirmedSchedule = await _historyMgmt.GetLastConfirmedScheduleAsync();
 cancellationToken.ThrowIfCancellationRequested();

 // 执行算法
 var scheduler = new GreedyScheduler(context);
 var modelSchedule = await scheduler.ExecuteAsync(cancellationToken);
 modelSchedule.Title = request.Title;
 modelSchedule.StartDate = request.StartDate.Date;
 modelSchedule.EndDate = request.EndDate.Date;
 modelSchedule.CreatedAt = DateTime.UtcNow;
 modelSchedule.PersonalIds = request.PersonnelIds;
 modelSchedule.PositionIds = request.PositionIds;
 cancellationToken.ThrowIfCancellationRequested();

 // 保存至缓冲区（草稿）
 _ = await _historyMgmt.AddToBufferAsync(modelSchedule);

 // 映射 DTO（草稿未确认）
 return await MapToScheduleDtoAsync(modelSchedule, confirmedAt: null);
 }

 public async Task<List<ScheduleSummaryDto>> GetDraftsAsync()
 {
 var buffers = await _historyMgmt.GetAllBufferSchedulesAsync();
 return buffers.Select(b => new ScheduleSummaryDto
 {
 Id = b.Schedule.Id,
 Title = b.Schedule.Title,
 StartDate = b.Schedule.StartDate,
 EndDate = b.Schedule.EndDate,
 PersonnelCount = b.Schedule.PersonalIds.Count,
 PositionCount = b.Schedule.PositionIds.Count,
 ShiftCount = b.Schedule.Shifts.Count,
 CreatedAt = b.CreateTime,
 ConfirmedAt = null
 }).ToList();
 }

 public async Task<ScheduleDto?> GetScheduleByIdAsync(int id)
 {
 var buffers = await _historyMgmt.GetAllBufferSchedulesAsync();
 var buffer = buffers.FirstOrDefault(b => b.Schedule.Id == id);
 if (buffer.Schedule != null)
 {
 return await MapToScheduleDtoAsync(buffer.Schedule, confirmedAt: null, createdAtOverride: buffer.CreateTime);
 }

 var histories = await _historyMgmt.GetAllHistorySchedulesAsync();
 var history = histories.FirstOrDefault(h => h.Schedule.Id == id);
 if (history.Schedule != null)
 {
 return await MapToScheduleDtoAsync(history.Schedule, confirmedAt: history.ConfirmTime, createdAtOverride: history.ConfirmTime);
 }
 return null;
 }

 public async Task ConfirmScheduleAsync(int id)
 {
 var buffers = await _historyMgmt.GetAllBufferSchedulesAsync();
 var buffer = buffers.FirstOrDefault(b => b.Schedule.Id == id);
 if (buffer.Schedule == null)
 {
 throw new InvalidOperationException("未找到待确认的草稿排班");
 }
 await _historyMgmt.ConfirmBufferScheduleAsync(buffer.BufferId);
 }

 public async Task DeleteDraftAsync(int id)
 {
 var buffers = await _historyMgmt.GetAllBufferSchedulesAsync();
 var buffer = buffers.FirstOrDefault(b => b.Schedule.Id == id);
 if (buffer.Schedule == null) return;
 await _historyMgmt.DeleteBufferScheduleAsync(buffer.BufferId);
 }

 public async Task<List<ScheduleSummaryDto>> GetHistoryAsync(DateTime? startDate = null, DateTime? endDate = null)
 {
 var histories = await _historyMgmt.GetAllHistorySchedulesAsync();
 if (startDate.HasValue)
 histories = histories.Where(h => h.ConfirmTime.Date >= startDate.Value.Date).ToList();
 if (endDate.HasValue)
 histories = histories.Where(h => h.ConfirmTime.Date <= endDate.Value.Date).ToList();
 return histories.Select(h => new ScheduleSummaryDto
 {
 Id = h.Schedule.Id,
 Title = h.Schedule.Title,
 StartDate = h.Schedule.StartDate,
 EndDate = h.Schedule.EndDate,
 PersonnelCount = h.Schedule.PersonalIds.Count,
 PositionCount = h.Schedule.PositionIds.Count,
 ShiftCount = h.Schedule.Shifts.Count,
 CreatedAt = h.Schedule.CreatedAt == default ? h.ConfirmTime : h.Schedule.CreatedAt,
 ConfirmedAt = h.ConfirmTime
 }).ToList();
 }

 public Task<byte[]> ExportScheduleAsync(int id, string format)
 {
 // 占位：后续实现 Excel / PDF 导出
 throw new NotImplementedException("Export not implemented yet");
 }

 // === 新增接口实现：供前端向导加载约束数据 ===
 public async Task<List<HolidayConfig>> GetHolidayConfigsAsync() => await _constraintRepo.GetAllHolidayConfigsAsync();
 public async Task<List<FixedPositionRule>> GetFixedPositionRulesAsync(bool enabledOnly = true) => await _constraintRepo.GetAllFixedPositionRulesAsync(enabledOnly);
 public async Task<List<ManualAssignment>> GetManualAssignmentsAsync(DateTime startDate, DateTime endDate, bool enabledOnly = true) => await _constraintRepo.GetManualAssignmentsByDateRangeAsync(startDate, endDate, enabledOnly);

 private static void ValidateRequest(SchedulingRequestDto request)
 {
 if (string.IsNullOrWhiteSpace(request.Title)) throw new ArgumentException("排班标题不能为空", nameof(request));
 if (request.StartDate.Date > request.EndDate.Date) throw new ArgumentException("开始日期不能晚于结束日期", nameof(request));
 if (request.PersonnelIds == null || request.PersonnelIds.Count ==0) throw new ArgumentException("至少选择一名人员", nameof(request));
 if (request.PositionIds == null || request.PositionIds.Count ==0) throw new ArgumentException("至少选择一个哨位", nameof(request));
 var spanDays = (request.EndDate.Date - request.StartDate.Date).Days +1;
 if (spanDays >365) throw new ArgumentException("排班周期不能超过365天", nameof(request.EndDate));
 }

 private async Task<ScheduleDto> MapToScheduleDtoAsync(Schedule schedule, DateTime? confirmedAt, DateTime? createdAtOverride = null)
 {
 // 加载名称映射
 var positions = await _positionRepo.GetByIdsAsync(schedule.PositionIds);
 var personals = await _personalRepo.GetByIdsAsync(schedule.PersonalIds);
 var positionNames = positions.ToDictionary(p => p.Id, p => p.Name);
 var personnelNames = personals.ToDictionary(p => p.Id, p => p.Name);

 var dto = new ScheduleDto
 {
 Id = schedule.Id,
 Title = schedule.Title,
 PersonnelIds = schedule.PersonalIds.ToList(),
 PositionIds = schedule.PositionIds.ToList(),
 Shifts = schedule.Shifts.Select(s => new ShiftDto
 {
 Id = s.Id,
 ScheduleId = s.ScheduleId,
 PositionId = s.PositionId,
 PositionName = positionNames.TryGetValue(s.PositionId, out var pname) ? pname : string.Empty,
 PersonnelId = s.PersonalId,
 PersonnelName = personnelNames.TryGetValue(s.PersonalId, out var pername) ? pername : string.Empty,
 StartTime = s.StartTime,
 EndTime = s.EndTime,
 PeriodIndex = CalcPeriodIndex(s.StartTime)
 }).ToList(),
 CreatedAt = createdAtOverride ?? schedule.CreatedAt,
 ConfirmedAt = confirmedAt,
 StartDate = schedule.StartDate,
 EndDate = schedule.EndDate,
 Conflicts = GenerateBasicConflicts(schedule)
 };
 return dto;
 }

 private List<ConflictDto> GenerateBasicConflicts(Schedule schedule)
 {
 var conflicts = new List<ConflictDto>();
 // 未分配冲突：扫描全部日期、时段、哨位组合
 int totalDays = (schedule.EndDate.Date - schedule.StartDate.Date).Days +1;
 var assignedTriples = schedule.Shifts
 .GroupBy(s => (Date: s.StartTime.Date, Period: CalcPeriodIndex(s.StartTime), Pos: s.PositionId))
 .Select(g => g.Key)
 .ToHashSet();
 for (int d =0; d < totalDays; d++)
 {
 var date = schedule.StartDate.Date.AddDays(d);
 for (int period =0; period <12; period++)
 {
 foreach (var posId in schedule.PositionIds)
 {
 if (!assignedTriples.Contains((date, period, posId)))
 {
 var slotStart = date.AddHours(period *2);
 conflicts.Add(new ConflictDto
 {
 Type = "unassigned",
 Message = "该时段未分配人员",
 PositionId = posId,
 StartTime = slotStart,
 EndTime = slotStart.AddHours(2),
 PeriodIndex = period
 });
 }
 }
 }
 }
 return conflicts;
 }

 private static int CalcPeriodIndex(DateTime startTime) => startTime.Hour /2;
}
