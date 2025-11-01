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
using System.Text;
using AutoScheduling3.Data.Interfaces;

namespace AutoScheduling3.Services;

public class SchedulingService : ISchedulingService
{
    private readonly IPersonalRepository _personalRepo;
    private readonly IPositionRepository _positionRepo;
    private readonly ISkillRepository _skillRepo;
    private readonly IConstraintRepository _constraintRepo;
    private readonly IHistoryManagement _historyMgmt;

    public SchedulingService(IPersonalRepository personalRepo, IPositionRepository positionRepo, ISkillRepository skillRepo, IConstraintRepository constraintRepo, IHistoryManagement historyMgmt)
    {
        _personalRepo = personalRepo;
        _positionRepo = positionRepo;
        _skillRepo = skillRepo;
        _constraintRepo = constraintRepo;
        _historyMgmt = historyMgmt;
    }

    public async Task InitializeAsync()
    {
        // 如果具体实现支持 InitAsync 可调用 (反射检测)
        var initMethods = new object[]{ _personalRepo, _positionRepo, _skillRepo, _constraintRepo, _historyMgmt };
        foreach (var svc in initMethods)
        {
            var mi = svc.GetType().GetMethod("InitAsync");
            if (mi != null)
            {
                var task = mi.Invoke(svc, null) as Task;
                if (task != null) await task;
            }
        }
    }

    public async Task<ScheduleDto> ExecuteSchedulingAsync(SchedulingRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        ValidateRequest(request);
        
        // 业务规则验证
        await ValidateSchedulingRequestBusinessLogicAsync(request);
        
        cancellationToken.ThrowIfCancellationRequested();

        // 并行加载基础数据
        var personalsTask = (_personalRepo as PersonalRepository)?.GetByIdsAsync(request.PersonnelIds) ?? _personalRepo.GetPersonnelByIdsAsync(request.PersonnelIds);
        var positionsTask = (_positionRepo as PositionLocationRepository)?.GetByIdsAsync(request.PositionIds) ?? _positionRepo.GetPositionsByIdsAsync(request.PositionIds);
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
        if (request.EnabledFixedRuleIds?.Count > 0)
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
        if (request.EnabledManualAssignmentIds?.Count > 0)
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
        modelSchedule.Header = request.Title;
        modelSchedule.StartDate = request.StartDate.Date;
        modelSchedule.EndDate = request.EndDate.Date;
        modelSchedule.CreatedAt = DateTime.UtcNow;
        modelSchedule.PersonnelIds = request.PersonnelIds;
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
            Title = b.Schedule.Header,
            StartDate = b.Schedule.StartDate,
            EndDate = b.Schedule.EndDate,
            PersonnelCount = b.Schedule.PersonnelIds.Count,
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
        
        // 业务规则验证：确认前的最终检查
        await ValidateScheduleForConfirmationAsync(buffer.Schedule);
        
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
            Title = h.Schedule.Header,
            StartDate = h.Schedule.StartDate,
            EndDate = h.Schedule.EndDate,
            PersonnelCount = h.Schedule.PersonnelIds.Count,
            PositionCount = h.Schedule.PositionIds.Count,
            ShiftCount = h.Schedule.Shifts.Count,
            CreatedAt = h.Schedule.CreatedAt == default ? h.ConfirmTime : h.Schedule.CreatedAt,
            ConfirmedAt = h.ConfirmTime
        }).ToList();
    }

    public async Task<byte[]> ExportScheduleAsync(int id, string format)
    {
        var scheduleDto = await GetScheduleByIdAsync(id);
        if (scheduleDto == null)
        {
            throw new ArgumentException("Schedule not found", nameof(id));
        }

        if (format.Equals("excel", StringComparison.OrdinalIgnoreCase) || format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            return await GenerateCsvAsync(scheduleDto);
        }

        throw new NotImplementedException($"Export format '{format}' is not supported.");
    }

    private async Task<byte[]> GenerateCsvAsync(ScheduleDto schedule)
    {
        var sb = new StringBuilder();

        // Header Row: Dates
        sb.Append("Position/Date");
        for (var date = schedule.StartDate.Date; date <= schedule.EndDate.Date; date = date.AddDays(1))
        {
            sb.Append($",{date:yyyy-MM-dd}");
        }
        sb.AppendLine();

        // Data Rows: One row per position
        var shiftsByPositionAndDate = schedule.Shifts
            .ToLookup(s => s.PositionId);

        var allPositions = await _positionRepo.GetPositionsByIdsAsync(schedule.PositionIds);
        var positionDict = allPositions.ToDictionary(p => p.Id);

        foreach (var positionId in schedule.PositionIds)
        {
            if (!positionDict.TryGetValue(positionId, out var position)) continue;

            sb.Append($"\"{position.Name}\"");

            for (var date = schedule.StartDate.Date; date <= schedule.EndDate.Date; date = date.AddDays(1))
            {
                var shiftsForDay = shiftsByPositionAndDate[positionId]
                    .Where(s => s.StartTime.Date == date)
                    .OrderBy(s => s.StartTime)
                    .Select(s => $"{s.PersonnelName}({s.StartTime:HH:mm}-{s.EndTime:HH:mm})")
                    .ToList();

                var cellValue = string.Join(" | ", shiftsForDay);
                sb.Append($",\"{cellValue}\"");
            }
            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // === 新增接口实现：供前端向导加载约束数据 ===
    public async Task<List<HolidayConfig>> GetHolidayConfigsAsync() => await _constraintRepo.GetAllHolidayConfigsAsync();
    public async Task<List<FixedPositionRule>> GetFixedPositionRulesAsync(bool enabledOnly = true) => await _constraintRepo.GetAllFixedPositionRulesAsync(enabledOnly);
    public async Task<List<ManualAssignment>> GetManualAssignmentsAsync(DateTime startDate, DateTime endDate, bool enabledOnly = true) => await _constraintRepo.GetManualAssignmentsByDateRangeAsync(startDate, endDate, enabledOnly);

    private static void ValidateRequest(SchedulingRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) throw new ArgumentException("排班标题不能为空", nameof(request));
        if (request.StartDate.Date > request.EndDate.Date) throw new ArgumentException("开始日期不能晚于结束日期", nameof(request));
        if (request.PersonnelIds == null || request.PersonnelIds.Count == 0) throw new ArgumentException("至少选择一名人员", nameof(request));
        if (request.PositionIds == null || request.PositionIds.Count == 0) throw new ArgumentException("至少选择一个哨位", nameof(request));
        var spanDays = (request.EndDate.Date - request.StartDate.Date).Days + 1;
        if (spanDays > 365) throw new ArgumentException("排班周期不能超过365天", nameof(request.EndDate));
    }

    private async Task<ScheduleDto> MapToScheduleDtoAsync(Schedule schedule, DateTime? confirmedAt, DateTime? createdAtOverride = null)
    {
        // 加载名称映射
        var positions = await _positionRepo.GetPositionsByIdsAsync(schedule.PositionIds);
        var personals = await _personalRepo.GetPersonnelByIdsAsync(schedule.PersonnelIds);
        var positionNames = positions.ToDictionary(p => p.Id, p => p.Name);
        var personnelNames = personals.ToDictionary(p => p.Id, p => p.Name);

        var dto = new ScheduleDto
        {
            Id = schedule.Id,
            Title = schedule.Header,
            PersonnelIds = schedule.PersonnelIds.ToList(),
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
        int totalDays = (schedule.EndDate.Date - schedule.StartDate.Date).Days + 1;
        var assignedTriples = schedule.Shifts
        .GroupBy(s => (Date: s.StartTime.Date, Period: CalcPeriodIndex(s.StartTime), Pos: s.PositionId))
        .Select(g => g.Key)
        .ToHashSet();
        for (int d = 0; d < totalDays; d++)
        {
            var date = schedule.StartDate.Date.AddDays(d);
            for (int period = 0; period < 12; period++)
            {
                foreach (var posId in schedule.PositionIds)
                {
                    if (!assignedTriples.Contains((date, period, posId)))
                    {
                        var slotStart = date.AddHours(period * 2);
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

    private static int CalcPeriodIndex(DateTime startTime) => startTime.Hour / 2;

    #region 业务规则验证

    /// <summary>
    /// 验证排班请求的业务规则
    /// </summary>
    private async Task ValidateSchedulingRequestBusinessLogicAsync(SchedulingRequestDto request)
    {
        // 验证人员是否存在且可用
        var personnelList = await _personalRepo.GetPersonnelByIdsAsync(request.PersonnelIds);
        var existingPersonnelIds = personnelList.Select(p => p.Id).ToHashSet();
        var missingPersonnelIds = request.PersonnelIds.Except(existingPersonnelIds).ToList();
        
        if (missingPersonnelIds.Any())
            throw new ArgumentException($"以下人员ID不存在: {string.Join(", ", missingPersonnelIds)}");

        // 检查人员可用性
        var unavailablePersonnel = personnelList.Where(p => !p.IsAvailable || p.IsRetired).ToList();
        if (unavailablePersonnel.Any())
        {
            var names = string.Join(", ", unavailablePersonnel.Select(p => p.Name));
            throw new ArgumentException($"以下人员当前不可用: {names}");
        }

        // 验证哨位是否存在
        var positionList = await _positionRepo.GetPositionsByIdsAsync(request.PositionIds);
        var existingPositionIds = positionList.Select(p => p.Id).ToHashSet();
        var missingPositionIds = request.PositionIds.Except(existingPositionIds).ToList();
        
        if (missingPositionIds.Any())
            throw new ArgumentException($"以下哨位ID不存在: {string.Join(", ", missingPositionIds)}");

        // 验证技能匹配：至少要有一些人员能胜任一些哨位
        await ValidateSkillCompatibilityAsync(personnelList, positionList);

        // 验证日期范围合理性
        var daySpan = (request.EndDate.Date - request.StartDate.Date).Days + 1;
        if (daySpan < 1)
            throw new ArgumentException("排班周期至少为1天");

        // 验证人员数量与哨位数量的合理性
        ValidatePersonnelPositionRatio(personnelList.Count, positionList.Count, daySpan);
    }

    /// <summary>
    /// 验证技能兼容性
    /// </summary>
    private async Task ValidateSkillCompatibilityAsync(List<Personal> personnel, List<PositionLocation> positions)
    {
        var incompatiblePositions = new List<string>();

        foreach (var position in positions)
        {
            if (position.RequiredSkillIds?.Any() != true)
                continue; // 无技能要求的哨位跳过

            // 检查是否有人员能胜任此哨位
            var compatiblePersonnel = personnel.Where(p => 
                p.SkillIds?.Any() == true && 
                position.RequiredSkillIds.All(requiredSkill => p.SkillIds.Contains(requiredSkill))
            ).ToList();

            if (!compatiblePersonnel.Any())
            {
                incompatiblePositions.Add(position.Name);
            }
        }

        if (incompatiblePositions.Any())
        {
            throw new ArgumentException($"以下哨位没有合适的人员能够胜任: {string.Join(", ", incompatiblePositions)}");
        }
    }

    /// <summary>
    /// 验证人员与哨位数量比例的合理性
    /// </summary>
    private void ValidatePersonnelPositionRatio(int personnelCount, int positionCount, int daySpan)
    {
        // 计算总需求班次数（哨位数 × 天数 × 12个时段）
        var totalShiftsNeeded = positionCount * daySpan * 12;
        
        // 计算理论最大可提供班次数（假设每人每天最多8个时段，即16小时）
        var maxShiftsAvailable = personnelCount * daySpan * 8;
        
        if (totalShiftsNeeded > maxShiftsAvailable)
        {
            throw new ArgumentException($"人员数量不足：需要 {totalShiftsNeeded} 个班次，但最多只能提供 {maxShiftsAvailable} 个班次");
        }

        // 警告：如果人员过少可能导致排班困难
        var utilizationRate = (double)totalShiftsNeeded / maxShiftsAvailable;
        if (utilizationRate > 0.8) // 超过80%利用率
        {
            // 这里可以记录警告日志，但不抛出异常
            System.Diagnostics.Debug.WriteLine($"警告：人员利用率过高 ({utilizationRate:P1})，可能导致排班困难");
        }
    }

    /// <summary>
    /// 验证排班结果是否可以确认
    /// </summary>
    private async Task ValidateScheduleForConfirmationAsync(Schedule schedule)
    {
        if (schedule == null)
            throw new ArgumentNullException(nameof(schedule));

        // 验证排班是否有未分配的关键时段
        var criticalUnassignedSlots = await GetCriticalUnassignedSlotsAsync(schedule);
        if (criticalUnassignedSlots.Any())
        {
            var slotDescriptions = criticalUnassignedSlots.Take(5).Select(slot => 
                $"{slot.Date:MM-dd} {slot.Period * 2:00}:00-{(slot.Period + 1) * 2:00}:00");
            var message = $"存在未分配的关键时段: {string.Join(", ", slotDescriptions)}";
            if (criticalUnassignedSlots.Count > 5)
                message += $" 等共{criticalUnassignedSlots.Count}个时段";
            
            throw new InvalidOperationException(message);
        }

        // 验证是否有人员过度工作
        var overworkedPersonnel = await GetOverworkedPersonnelAsync(schedule);
        if (overworkedPersonnel.Any())
        {
            var descriptions = overworkedPersonnel.Take(3).Select(p => 
                $"{p.Name}({p.ShiftCount}班次)");
            var message = $"以下人员工作量过重: {string.Join(", ", descriptions)}";
            
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// 获取关键未分配时段
    /// </summary>
    private async Task<List<(DateTime Date, int Period, int PositionId)>> GetCriticalUnassignedSlotsAsync(Schedule schedule)
    {
        var unassignedSlots = new List<(DateTime Date, int Period, int PositionId)>();
        var assignedSlots = schedule.Shifts
            .Select(s => (Date: s.StartTime.Date, Period: CalcPeriodIndex(s.StartTime), PositionId: s.PositionId))
            .ToHashSet();

        var totalDays = (schedule.EndDate.Date - schedule.StartDate.Date).Days + 1;
        
        for (int d = 0; d < totalDays; d++)
        {
            var date = schedule.StartDate.Date.AddDays(d);
            
            // 检查是否为休息日
            var holidayConfig = await _constraintRepo.GetActiveHolidayConfigAsync();
            var isHoliday = holidayConfig?.IsHoliday(date) ?? false;
            
            for (int period = 0; period < 12; period++)
            {
                // 夜间时段（22:00-06:00）在休息日可以不分配
                var isNightShift = period >= 11 || period <= 2;
                if (isHoliday && isNightShift)
                    continue;

                foreach (var positionId in schedule.PositionIds)
                {
                    if (!assignedSlots.Contains((date, period, positionId)))
                    {
                        unassignedSlots.Add((date, period, positionId));
                    }
                }
            }
        }

        return unassignedSlots;
    }

    /// <summary>
    /// 获取过度工作的人员
    /// </summary>
    private async Task<List<(string Name, int ShiftCount)>> GetOverworkedPersonnelAsync(Schedule schedule)
    {
        var personnelShiftCounts = schedule.Shifts
            .GroupBy(s => s.PersonalId)
            .ToDictionary(g => g.Key, g => g.Count());

        var personnel = await _personalRepo.GetPersonnelByIdsAsync(schedule.PersonnelIds);
        var personnelNames = personnel.ToDictionary(p => p.Id, p => p.Name);

        var daySpan = (schedule.EndDate.Date - schedule.StartDate.Date).Days + 1;
        var maxReasonableShifts = daySpan * 4; // 每天最多4个班次（8小时）

        var overworkedPersonnel = new List<(string Name, int ShiftCount)>();
        
        foreach (var kvp in personnelShiftCounts)
        {
            if (kvp.Value > maxReasonableShifts)
            {
                var name = personnelNames.TryGetValue(kvp.Key, out var pName) ? pName : $"ID:{kvp.Key}";
                overworkedPersonnel.Add((name, kvp.Value));
            }
        }

        return overworkedPersonnel;
    }

    #endregion
}
