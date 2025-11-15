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
using AutoScheduling3.SchedulingEngine.Core;

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

    /// <summary>
    /// 初始化排班服务 - 对应需求3.1, 4.1
    /// </summary>
    public async Task InitializeAsync()
    {
        // 初始化历史管理系统 - 对应需求4.1, 4.2
        await _historyMgmt.InitAsync();
        
        // 如果具体实现支持 InitAsync 可调用 (反射检测)
        var initMethods = new object[]{ _personalRepo, _positionRepo, _skillRepo, _constraintRepo };
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
        System.Diagnostics.Debug.WriteLine("=== 开始执行排班 ===");
        
        if (request == null) 
        {
            System.Diagnostics.Debug.WriteLine("错误: request 为 null");
            throw new ArgumentNullException(nameof(request));
        }
        
        System.Diagnostics.Debug.WriteLine($"排班标题: {request.Title}");
        System.Diagnostics.Debug.WriteLine($"日期范围: {request.StartDate:yyyy-MM-dd} 到 {request.EndDate:yyyy-MM-dd}");
        System.Diagnostics.Debug.WriteLine($"人员数量: {request.PersonnelIds?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"哨位数量: {request.PositionIds?.Count ?? 0}");
        
        Task<List<Personal>> personalsTask;
        Task<List<PositionLocation>> positionsTask;
        Task<List<Skill>> skillsTask;

        try
        {
            System.Diagnostics.Debug.WriteLine("步骤1: 基本参数验证");
            ValidateRequest(request);
            
            System.Diagnostics.Debug.WriteLine("步骤2: 业务规则验证");
            await ValidateSchedulingRequestBusinessLogicAsync(request);
            
            cancellationToken.ThrowIfCancellationRequested();

            System.Diagnostics.Debug.WriteLine("步骤3: 加载基础数据");
            // 并行加载基础数据
            personalsTask = (_personalRepo as PersonalRepository)?.GetByIdsAsync(request.PersonnelIds) ?? _personalRepo.GetPersonnelByIdsAsync(request.PersonnelIds);
            positionsTask = (_positionRepo as PositionLocationRepository)?.GetByIdsAsync(request.PositionIds) ?? _positionRepo.GetPositionsByIdsAsync(request.PositionIds);
            skillsTask = _skillRepo.GetAllAsync();
            await Task.WhenAll(personalsTask, positionsTask, skillsTask);
            
            System.Diagnostics.Debug.WriteLine($"加载完成 - 人员: {personalsTask.Result.Count}, 哨位: {positionsTask.Result.Count}, 技能: {skillsTask.Result.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"排班前期验证失败: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"堆栈: {ex.StackTrace}");
            throw;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine("步骤4: 构建排班上下文");
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

            System.Diagnostics.Debug.WriteLine("步骤5: 加载休息日配置");
            //休息日配置
            if (request.UseActiveHolidayConfig)
            {
                context.HolidayConfig = await _constraintRepo.GetActiveHolidayConfigAsync();
                System.Diagnostics.Debug.WriteLine($"使用活动休息日配置: {context.HolidayConfig?.Id ?? 0}");
            }
            else if (request.HolidayConfigId.HasValue)
            {
                var allConfigs = await _constraintRepo.GetAllHolidayConfigsAsync();
                context.HolidayConfig = allConfigs.FirstOrDefault(c => c.Id == request.HolidayConfigId.Value);
                System.Diagnostics.Debug.WriteLine($"使用指定休息日配置: {request.HolidayConfigId}");
            }
            cancellationToken.ThrowIfCancellationRequested();

            System.Diagnostics.Debug.WriteLine("步骤6: 加载定岗规则");
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
            System.Diagnostics.Debug.WriteLine($"定岗规则数量: {context.FixedPositionRules?.Count ?? 0}");
            cancellationToken.ThrowIfCancellationRequested();

            System.Diagnostics.Debug.WriteLine("步骤7: 加载手动指定");
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
            System.Diagnostics.Debug.WriteLine($"手动指定数量: {context.ManualAssignments?.Count ?? 0}");
            cancellationToken.ThrowIfCancellationRequested();

            System.Diagnostics.Debug.WriteLine("步骤8: 加载历史排班");
            // 最近历史排班（用于间隔评分等）
            context.LastConfirmedSchedule = await _historyMgmt.GetLastConfirmedScheduleAsync();
            System.Diagnostics.Debug.WriteLine($"最近确认排班: {context.LastConfirmedSchedule?.Id ?? 0}");
            cancellationToken.ThrowIfCancellationRequested();

            System.Diagnostics.Debug.WriteLine("步骤9: 执行排班算法");
            // 执行算法
            var scheduler = new GreedyScheduler(context);
            var modelSchedule = await scheduler.ExecuteAsync(cancellationToken);
            
            System.Diagnostics.Debug.WriteLine($"算法执行完成，生成 {modelSchedule.Results?.Count ?? 0} 个班次");
            
            modelSchedule.Header = request.Title;
            modelSchedule.StartDate = request.StartDate.Date;
            modelSchedule.EndDate = request.EndDate.Date;
            modelSchedule.CreatedAt = DateTime.UtcNow;
            modelSchedule.PersonnelIds = request.PersonnelIds;
            modelSchedule.PositionIds = request.PositionIds;
            cancellationToken.ThrowIfCancellationRequested();

            System.Diagnostics.Debug.WriteLine("步骤10: 保存到缓冲区");
            // 保存至缓冲区（草稿）
            _ = await _historyMgmt.AddToBufferAsync(modelSchedule);

            System.Diagnostics.Debug.WriteLine("步骤11: 映射DTO");
            // 映射 DTO（草稿未确认）
            var result = await MapToScheduleDtoAsync(modelSchedule, confirmedAt: null);
            
            System.Diagnostics.Debug.WriteLine("=== 排班执行成功 ===");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"排班执行失败: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"内部异常: {ex.InnerException.Message}");
            }
            throw;
        }
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
            ShiftCount = b.Schedule.Results.Count,
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
            ShiftCount = h.Schedule.Results.Count,
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

    /// <summary>
    /// 创建手动指定
    /// </summary>
    public async Task<ManualAssignmentDto> CreateManualAssignmentAsync(CreateManualAssignmentDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // 验证人员和哨位是否存在
        var personnel = await _personalRepo.GetByIdAsync(dto.PersonnelId);
        if (personnel == null)
            throw new ArgumentException($"人员ID {dto.PersonnelId} 不存在", nameof(dto.PersonnelId));

        var position = await _positionRepo.GetByIdAsync(dto.PositionId);
        if (position == null)
            throw new ArgumentException($"哨位ID {dto.PositionId} 不存在", nameof(dto.PositionId));

        // 创建ManualAssignment模型
        var assignment = new ManualAssignment
        {
            PositionId = dto.PositionId,
            PeriodIndex = dto.TimeSlot,
            PersonalId = dto.PersonnelId,
            Date = dto.Date.Date,
            IsEnabled = dto.IsEnabled,
            Remarks = dto.Remarks ?? string.Empty
        };

        // 保存到数据库
        var id = await _constraintRepo.AddManualAssignmentAsync(assignment);

        // 返回DTO
        return new ManualAssignmentDto
        {
            Id = id,
            PositionId = assignment.PositionId,
            PositionName = position.Name,
            TimeSlot = assignment.PeriodIndex,
            PersonnelId = assignment.PersonalId,
            PersonnelName = personnel.Name,
            Date = assignment.Date,
            IsEnabled = assignment.IsEnabled,
            Remarks = assignment.Remarks,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

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
            Shifts = schedule.Results.Select(s => new ShiftDto
            {
                Id = s.Id,
                ScheduleId = s.ScheduleId,
                PositionId = s.PositionId,
                PositionName = positionNames.TryGetValue(s.PositionId, out var pname) ? pname : string.Empty,
                PersonnelId = s.PersonnelId,
                PersonnelName = personnelNames.TryGetValue(s.PersonnelId, out var pername) ? pername : string.Empty,
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
        var assignedTriples = schedule.Results
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

        // 验证哨位可用人员数据完整性 - 根据需求3.3
        await ValidatePositionAvailablePersonnelDataIntegrityAsync(positionList, personnelList);

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
    /// 验证技能兼容性 - 使用新数据模型
    /// </summary>
    private async Task ValidateSkillCompatibilityAsync(List<Personal> personnel, List<PositionLocation> positions)
    {
        var incompatiblePositions = new List<string>();

        foreach (var position in positions)
        {
            if (position.RequiredSkillIds?.Any() != true)
                continue; // 无技能要求的哨位跳过

            // 使用新数据模型：从哨位的可用人员列表中检查技能匹配
            if (position.AvailablePersonnelIds?.Any() != true)
            {
                incompatiblePositions.Add(position.Name);
                continue;
            }

            // 获取哨位可用人员中能胜任的人员
            var availablePersonnel = personnel.Where(p => 
                position.AvailablePersonnelIds.Contains(p.Id)).ToList();

            var compatiblePersonnel = availablePersonnel.Where(p => 
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
    /// 验证哨位可用人员数据完整性 - 根据需求3.3
    /// </summary>
    private async Task ValidatePositionAvailablePersonnelDataIntegrityAsync(List<PositionLocation> positions, List<Personal> personnel)
    {
        var personnelIds = personnel.Select(p => p.Id).ToHashSet();
        var dataIntegrityIssues = new List<string>();

        foreach (var position in positions)
        {
            if (position.AvailablePersonnelIds?.Any() != true)
            {
                dataIntegrityIssues.Add($"哨位 '{position.Name}' 没有配置可用人员");
                continue;
            }

            // 检查可用人员ID是否都存在
            var invalidPersonnelIds = position.AvailablePersonnelIds
                .Where(id => !personnelIds.Contains(id))
                .ToList();

            if (invalidPersonnelIds.Any())
            {
                dataIntegrityIssues.Add($"哨位 '{position.Name}' 包含无效的人员ID: {string.Join(", ", invalidPersonnelIds)}");
            }

            // 检查可用人员中是否有人能胜任该哨位
            var availablePersonnel = personnel.Where(p => 
                position.AvailablePersonnelIds.Contains(p.Id) && 
                p.IsAvailable && 
                !p.IsRetired).ToList();

            if (!availablePersonnel.Any())
            {
                dataIntegrityIssues.Add($"哨位 '{position.Name}' 没有可用的人员");
                continue;
            }

            // 检查技能匹配
            if (position.RequiredSkillIds?.Any() == true)
            {
                var skilledPersonnel = availablePersonnel.Where(p => 
                    p.SkillIds?.Any() == true && 
                    position.RequiredSkillIds.All(requiredSkill => p.SkillIds.Contains(requiredSkill))
                ).ToList();

                if (!skilledPersonnel.Any())
                {
                    dataIntegrityIssues.Add($"哨位 '{position.Name}' 的可用人员中没有人具备所需技能");
                }
            }
        }

        if (dataIntegrityIssues.Any())
        {
            throw new ArgumentException($"哨位可用人员数据完整性验证失败:\n{string.Join("\n", dataIntegrityIssues)}");
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
        var assignedSlots = schedule.Results
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
        var personnelShiftCounts = schedule.Results
            .GroupBy(s => s.PersonnelId)
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

    #region 排班引擎集成 - 对应需求3.1, 3.2

    /// <summary>
    /// 验证排班引擎的可用性和配置
    /// </summary>
    private async Task ValidateSchedulingEngineAsync()
    {
        // 验证排班引擎依赖的组件是否正常
        try
        {
            // 测试约束验证器
            var testContext = new SchedulingContext
            {
                Personals = new List<Personal>(),
                Positions = new List<PositionLocation>(),
                Skills = new List<Skill>(),
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            };
            
            var constraintValidator = new ConstraintValidator(testContext);
            var softConstraintCalculator = new SoftConstraintCalculator(testContext);
            
            // 验证组件初始化成功
            if (constraintValidator == null || softConstraintCalculator == null)
            {
                throw new InvalidOperationException("排班引擎组件初始化失败");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"排班引擎验证失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取排班引擎状态信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetSchedulingEngineStatusAsync()
    {
        var status = new Dictionary<string, object>();
        
        try
        {
            await ValidateSchedulingEngineAsync();
            status["EngineStatus"] = "Available";
            status["LastCheck"] = DateTime.UtcNow;
            
            // 获取历史管理状态
            var lastConfirmed = await _historyMgmt.GetLastConfirmedScheduleAsync();
            status["LastConfirmedSchedule"] = lastConfirmed?.Id ?? 0;
            
            // 获取缓冲区状态
            var buffers = await _historyMgmt.GetAllBufferSchedulesAsync();
            status["BufferCount"] = buffers.Count;
            
            // 获取约束配置状态
            var holidayConfig = await _constraintRepo.GetActiveHolidayConfigAsync();
            status["ActiveHolidayConfig"] = holidayConfig?.Id ?? 0;
            
            var fixedRules = await _constraintRepo.GetAllFixedPositionRulesAsync(enabledOnly: true);
            status["ActiveFixedRules"] = fixedRules.Count;
            
        }
        catch (Exception ex)
        {
            status["EngineStatus"] = "Error";
            status["ErrorMessage"] = ex.Message;
            status["LastCheck"] = DateTime.UtcNow;
        }
        
        return status;
    }

    #endregion

    #region 历史管理集成 - 对应需求4.1, 4.2, 4.3, 4.4

    /// <summary>
    /// 获取排班统计信息 - 对应需求4.1, 4.2
    /// </summary>
    public async Task<ScheduleStatisticsDto> GetScheduleStatisticsAsync()
    {
        var histories = await _historyMgmt.GetAllHistorySchedulesAsync();
        var buffers = await _historyMgmt.GetAllBufferSchedulesAsync();
        
        var statistics = new ScheduleStatisticsDto
        {
            TotalSchedules = histories.Count + buffers.Count,
            ConfirmedSchedules = histories.Count,
            DraftSchedules = buffers.Count,
        };
        
        // 计算人员班次统计
        var personnelShiftCounts = new Dictionary<int, int>();
        var timeSlotDistribution = new Dictionary<int, double>();
        
        foreach (var (schedule, _) in histories)
        {
            foreach (var shift in schedule.Results)
            {
                // 人员班次计数
                if (!personnelShiftCounts.ContainsKey(shift.PersonnelId))
                    personnelShiftCounts[shift.PersonnelId] = 0;
                personnelShiftCounts[shift.PersonnelId]++;
                
                // 时段分布统计
                var periodIndex = CalcPeriodIndex(shift.StartTime);
                if (!timeSlotDistribution.ContainsKey(periodIndex))
                    timeSlotDistribution[periodIndex] = 0;
                timeSlotDistribution[periodIndex]++;
            }
        }
        
        // 计算时段分布百分比
        var totalShifts = timeSlotDistribution.Values.Sum();
        if (totalShifts > 0)
        {
            var normalizedDistribution = new Dictionary<int, double>();
            foreach (var kvp in timeSlotDistribution)
            {
                normalizedDistribution[kvp.Key] = kvp.Value / totalShifts * 100;
            }
            statistics.TimeSlotDistribution = normalizedDistribution;
        }
        
        statistics.PersonnelShiftCounts = personnelShiftCounts;
        
        return statistics;
    }

    /// <summary>
    /// 批量确认多个草稿排班 - 对应需求4.3, 4.4
    /// </summary>
    public async Task ConfirmMultipleSchedulesAsync(List<int> scheduleIds)
    {
        if (scheduleIds == null || !scheduleIds.Any())
            throw new ArgumentException("排班ID列表不能为空", nameof(scheduleIds));
        
        var buffers = await _historyMgmt.GetAllBufferSchedulesAsync();
        var validBuffers = buffers.Where(b => scheduleIds.Contains(b.Schedule.Id)).ToList();
        
        if (validBuffers.Count != scheduleIds.Count)
        {
            var foundIds = validBuffers.Select(b => b.Schedule.Id).ToList();
            var missingIds = scheduleIds.Except(foundIds).ToList();
            throw new InvalidOperationException($"以下排班ID在草稿中未找到: {string.Join(", ", missingIds)}");
        }
        
        // 按创建时间顺序确认
        var sortedBuffers = validBuffers.OrderBy(b => b.CreateTime).ToList();
        
        foreach (var buffer in sortedBuffers)
        {
            await ValidateScheduleForConfirmationAsync(buffer.Schedule);
            await _historyMgmt.ConfirmBufferScheduleAsync(buffer.BufferId);
        }
    }

    /// <summary>
    /// 清理过期的草稿排班 - 对应需求4.2
    /// </summary>
    public async Task CleanupExpiredDraftsAsync(int daysToKeep = 7)
    {
        var buffers = await _historyMgmt.GetAllBufferSchedulesAsync();
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        
        var expiredBuffers = buffers.Where(b => b.CreateTime < cutoffDate).ToList();
        
        foreach (var buffer in expiredBuffers)
        {
            await _historyMgmt.DeleteBufferScheduleAsync(buffer.BufferId);
        }
        
        if (expiredBuffers.Any())
        {
            System.Diagnostics.Debug.WriteLine($"已清理 {expiredBuffers.Count} 个过期草稿排班");
        }
    }

    #endregion
}
