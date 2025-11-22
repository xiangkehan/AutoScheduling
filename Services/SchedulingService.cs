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
        // 调用新的重载方法，不传递进度报告
        var result = await ExecuteSchedulingAsync(request, progress: null, cancellationToken);
        
        // 如果失败，抛出异常以保持向后兼容
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "排班执行失败");
        }
        
        return result.Schedule!;
    }

    public async Task<SchedulingResult> ExecuteSchedulingAsync(SchedulingRequestDto request, IProgress<SchedulingProgressReport>? progress = null, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var executionTimer = System.Diagnostics.Stopwatch.StartNew();
        Schedule? partialSchedule = null;
        int partialCompletedAssignments = 0;
        
        System.Diagnostics.Debug.WriteLine("=== 开始执行排班 ===");
        
        if (request == null) 
        {
            System.Diagnostics.Debug.WriteLine("错误: request 为 null");
            return new SchedulingResult
            {
                IsSuccess = false,
                ErrorMessage = "排班请求不能为空",
                TotalDuration = DateTime.UtcNow - startTime
            };
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
            // 报告初始化阶段 - 对应需求1.1, 1.2, 1.3
            ReportProgress(progress, SchedulingStage.Initializing, "正在初始化排班环境...", 0, executionTimer.Elapsed);
            
            System.Diagnostics.Debug.WriteLine("步骤1: 基本参数验证");
            ValidateRequest(request);
            
            System.Diagnostics.Debug.WriteLine("步骤2: 业务规则验证");
            await ValidateSchedulingRequestBusinessLogicAsync(request);
            
            cancellationToken.ThrowIfCancellationRequested();

            // 报告加载数据阶段 - 对应需求1.2, 2.1
            ReportProgress(progress, SchedulingStage.LoadingData, "正在加载排班数据...", 5, executionTimer.Elapsed);
            
            System.Diagnostics.Debug.WriteLine("步骤3: 加载基础数据");
            // 并行加载基础数据
            personalsTask = (_personalRepo as PersonalRepository)?.GetByIdsAsync(request.PersonnelIds) ?? _personalRepo.GetPersonnelByIdsAsync(request.PersonnelIds);
            positionsTask = (_positionRepo as PositionLocationRepository)?.GetByIdsAsync(request.PositionIds) ?? _positionRepo.GetPositionsByIdsAsync(request.PositionIds);
            skillsTask = _skillRepo.GetAllAsync();
            await Task.WhenAll(personalsTask, positionsTask, skillsTask);
            
            System.Diagnostics.Debug.WriteLine($"加载完成 - 人员: {personalsTask.Result.Count}, 哨位: {positionsTask.Result.Count}, 技能: {skillsTask.Result.Count}");
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("排班已取消");
            return new SchedulingResult
            {
                IsSuccess = false,
                ErrorMessage = "排班已取消",
                TotalDuration = DateTime.UtcNow - startTime
            };
        }
        catch (ArgumentException ex)
        {
            System.Diagnostics.Debug.WriteLine($"参数验证失败: {ex.Message}");
            return new SchedulingResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                TotalDuration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"排班前期验证失败: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"堆栈: {ex.StackTrace}");
            return new SchedulingResult
            {
                IsSuccess = false,
                ErrorMessage = $"排班初始化失败: {ex.Message}",
                TotalDuration = DateTime.UtcNow - startTime
            };
        }

        try
        {
            // 报告构建上下文阶段 - 对应需求1.2, 1.3
            ReportProgress(progress, SchedulingStage.BuildingContext, "正在构建排班上下文...", 10, executionTimer.Elapsed);
            
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
            
            // 取消检查点 - 对应需求7.1, 7.2, 7.3
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
            
            // 取消检查点 - 对应需求7.1, 7.2, 7.3
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
            
            // 取消检查点 - 对应需求7.1, 7.2, 7.3
            cancellationToken.ThrowIfCancellationRequested();

            System.Diagnostics.Debug.WriteLine("步骤8: 加载历史排班");
            // 最近历史排班（用于间隔评分等）
            context.LastConfirmedSchedule = await _historyMgmt.GetLastConfirmedScheduleAsync();
            System.Diagnostics.Debug.WriteLine($"最近确认排班: {context.LastConfirmedSchedule?.Id ?? 0}");
            
            // 取消检查点 - 对应需求7.1, 7.2, 7.3
            cancellationToken.ThrowIfCancellationRequested();

            // 报告即将开始算法执行 - 对应需求1.2, 1.4
            ReportProgress(progress, SchedulingStage.InitializingTensor, "正在准备执行排班算法...", 15, executionTimer.Elapsed);
            
            // 取消检查点 - 对应需求7.1, 7.2, 7.3
            cancellationToken.ThrowIfCancellationRequested();
            
            System.Diagnostics.Debug.WriteLine("步骤9: 执行排班算法");
            // 执行算法，传递进度报告和取消令牌 - 对应需求1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 7.1, 7.2, 7.3
            var scheduler = new GreedyScheduler(context);
            var modelSchedule = await scheduler.ExecuteAsync(progress, cancellationToken);
            
            // 保存部分进度信息，用于错误处理 - 对应需求4.1, 4.2, 4.3, 4.4, 4.5
            partialSchedule = modelSchedule;
            partialCompletedAssignments = modelSchedule.Results?.Count ?? 0;
            
            System.Diagnostics.Debug.WriteLine($"算法执行完成，生成 {partialCompletedAssignments} 个班次");
            
            // 取消检查点 - 对应需求7.1, 7.2, 7.3
            cancellationToken.ThrowIfCancellationRequested();
            
            modelSchedule.Header = request.Title;
            modelSchedule.StartDate = request.StartDate.Date;
            modelSchedule.EndDate = request.EndDate.Date;
            modelSchedule.CreatedAt = DateTime.UtcNow;
            modelSchedule.PersonnelIds = request.PersonnelIds;
            modelSchedule.PositionIds = request.PositionIds;
            
            // 取消检查点 - 对应需求7.1, 7.2, 7.3
            cancellationToken.ThrowIfCancellationRequested();

            // 报告完成阶段 - 对应需求1.2, 1.5
            ReportProgress(progress, SchedulingStage.Finalizing, "正在保存排班结果...", 95, executionTimer.Elapsed);
            
            System.Diagnostics.Debug.WriteLine("步骤10: 保存到缓冲区");
            // 保存至缓冲区（草稿）
            _ = await _historyMgmt.AddToBufferAsync(modelSchedule);

            System.Diagnostics.Debug.WriteLine("步骤11: 映射DTO");
            // 映射 DTO（草稿未确认）
            var scheduleDto = await MapToScheduleDtoAsync(modelSchedule, confirmedAt: null);
            
            // 报告完成 - 对应需求1.2, 1.5
            ReportProgress(progress, SchedulingStage.Completed, "排班完成", 100, executionTimer.Elapsed);
            
            System.Diagnostics.Debug.WriteLine("=== 排班执行成功 ===");
            
            executionTimer.Stop();
            
            // 构建完整的排班结果，包含统计信息 - 对应需求3.1, 3.2, 3.3, 3.4, 3.5
            return await BuildSchedulingResult(modelSchedule, scheduleDto, DateTime.UtcNow - startTime);
        }
        catch (OperationCanceledException)
        {
            // 捕获取消异常，返回取消状态 - 对应需求4.5, 7.1, 7.2, 7.3, 7.4, 7.5
            System.Diagnostics.Debug.WriteLine("排班已取消");
            executionTimer.Stop();
            
            // 报告取消状态
            ReportProgress(progress, SchedulingStage.Failed, "排班已取消", 0, executionTimer.Elapsed);
            
            // 构建失败结果，包含部分完成的分配数和冲突详情 - 对应需求4.5
            return await BuildFailureResult(
                "排班已取消",
                partialSchedule,
                partialCompletedAssignments,
                DateTime.UtcNow - startTime,
                request);
        }
        catch (InvalidOperationException ex)
        {
            // 捕获业务逻辑错误，返回业务逻辑错误的 SchedulingResult - 对应需求4.2, 4.3
            System.Diagnostics.Debug.WriteLine($"业务逻辑错误: {ex.Message}");
            executionTimer.Stop();
            
            // 报告失败状态
            ReportProgress(progress, SchedulingStage.Failed, $"业务逻辑错误: {ex.Message}", 0, executionTimer.Elapsed);
            
            // 构建失败结果，包含部分完成的分配数和冲突详情 - 对应需求4.2, 4.3
            return await BuildFailureResult(
                $"业务逻辑错误: {ex.Message}",
                partialSchedule,
                partialCompletedAssignments,
                DateTime.UtcNow - startTime,
                request);
        }
        catch (Exception ex)
        {
            // 捕获通用异常，返回系统错误的 SchedulingResult - 对应需求4.4, 4.5
            System.Diagnostics.Debug.WriteLine($"排班执行失败: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"内部异常: {ex.InnerException.Message}");
            }
            executionTimer.Stop();
            
            // 报告失败状态
            ReportProgress(progress, SchedulingStage.Failed, $"系统错误: {ex.Message}", 0, executionTimer.Elapsed);
            
            // 构建失败结果，包含部分完成的分配数和冲突详情 - 对应需求4.4, 4.5
            return await BuildFailureResult(
                $"系统错误: {ex.Message}",
                partialSchedule,
                partialCompletedAssignments,
                DateTime.UtcNow - startTime,
                request);
        }
    }

    /// <summary>
    /// 报告进度 - 对应需求1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3
    /// </summary>
    private void ReportProgress(
        IProgress<SchedulingProgressReport>? progress,
        SchedulingStage stage,
        string description,
        double progressPercentage,
        TimeSpan elapsedTime,
        string? currentPositionName = null,
        int? currentPeriodIndex = null,
        DateTime? currentDate = null)
    {
        if (progress == null) return;

        var report = new SchedulingProgressReport
        {
            ProgressPercentage = Math.Min(100, Math.Max(0, progressPercentage)),
            CurrentStage = stage,
            StageDescription = description,
            CompletedAssignments = 0, // 将由GreedyScheduler更新
            TotalSlotsToAssign = 0, // 将由GreedyScheduler更新
            RemainingSlots = 0,
            CurrentPositionName = currentPositionName,
            CurrentPeriodIndex = currentPeriodIndex ?? -1,
            CurrentDate = currentDate ?? DateTime.Today,
            ElapsedTime = elapsedTime,
            Warnings = new List<string>(),
            HasErrors = false,
            ErrorMessage = null
        };

        progress.Report(report);
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

    #region 排班结果构建 - 对应需求3.1, 3.2, 3.3, 3.4, 3.5

    /// <summary>
    /// 构建排班结果，包含完整的统计信息 - 对应需求3.1, 3.2, 3.3, 3.4, 3.5
    /// </summary>
    /// <param name="schedule">排班模型</param>
    /// <param name="scheduleDto">排班DTO</param>
    /// <param name="totalDuration">总执行时长</param>
    /// <returns>包含统计信息的排班结果</returns>
    private async Task<SchedulingResult> BuildSchedulingResult(Schedule schedule, ScheduleDto scheduleDto, TimeSpan totalDuration)
    {
        // 计算总分配数 - 对应需求3.1, 3.2
        var totalAssignments = schedule.Results.Count;

        // 计算人员工作量 - 对应需求3.3
        var personnelWorkloads = await CalculatePersonnelWorkloads(schedule);

        // 计算哨位覆盖率 - 对应需求3.3
        var positionCoverages = await CalculatePositionCoverages(schedule);

        // 计算软约束评分 - 对应需求3.3
        var softScores = await CalculateSoftConstraintScores(schedule);

        // 构建统计信息对象
        var statistics = new SchedulingStatistics
        {
            TotalAssignments = totalAssignments,
            PersonnelWorkloads = personnelWorkloads,
            PositionCoverages = positionCoverages,
            SoftScores = softScores
        };

        // 识别并收集冲突信息 - 对应需求3.4, 3.5
        var conflicts = scheduleDto.Conflicts.Select(c => new ConflictInfo
        {
            ConflictType = c.Type,
            Description = c.Message,
            PositionId = c.PositionId,
            PositionName = null, // 将在UI层填充
            PeriodIndex = c.PeriodIndex,
            Date = c.StartTime?.Date
        }).ToList();

        // 返回完整的排班结果
        return new SchedulingResult
        {
            IsSuccess = true,
            Schedule = scheduleDto,
            Statistics = statistics,
            Conflicts = conflicts,
            TotalDuration = totalDuration
        };
    }

    /// <summary>
    /// 计算人员工作量统计 - 对应需求3.3
    /// </summary>
    /// <param name="schedule">排班模型</param>
    /// <returns>人员工作量字典（键为人员ID）</returns>
    private async Task<Dictionary<int, PersonnelWorkload>> CalculatePersonnelWorkloads(Schedule schedule)
    {
        var workloads = new Dictionary<int, PersonnelWorkload>();

        // 获取人员名称映射
        var personnel = await _personalRepo.GetPersonnelByIdsAsync(schedule.PersonnelIds);
        var personnelNames = personnel.ToDictionary(p => p.Id, p => p.Name);

        // 按人员分组统计班次
        var shiftsByPersonnel = schedule.Results.GroupBy(s => s.PersonnelId);

        foreach (var group in shiftsByPersonnel)
        {
            var personnelId = group.Key;
            var shifts = group.ToList();

            // 统计总班次数
            var totalShifts = shifts.Count;

            // 统计日哨数和夜哨数
            // 日哨：06:00-18:00 (时段索引 3-8)
            // 夜哨：18:00-06:00 (时段索引 9-11, 0-2)
            var dayShifts = shifts.Count(s => 
            {
                var periodIndex = CalcPeriodIndex(s.StartTime);
                return periodIndex >= 3 && periodIndex <= 8;
            });

            var nightShifts = shifts.Count(s => 
            {
                var periodIndex = CalcPeriodIndex(s.StartTime);
                return periodIndex >= 9 || periodIndex <= 2;
            });

            // 获取人员姓名
            var personnelName = personnelNames.TryGetValue(personnelId, out var name) 
                ? name 
                : $"未知人员 (ID: {personnelId})";

            workloads[personnelId] = new PersonnelWorkload
            {
                PersonnelId = personnelId,
                PersonnelName = personnelName,
                TotalShifts = totalShifts,
                DayShifts = dayShifts,
                NightShifts = nightShifts
            };
        }

        // 为没有分配班次的人员添加零记录
        foreach (var personnelId in schedule.PersonnelIds)
        {
            if (!workloads.ContainsKey(personnelId))
            {
                var personnelName = personnelNames.TryGetValue(personnelId, out var name) 
                    ? name 
                    : $"未知人员 (ID: {personnelId})";

                workloads[personnelId] = new PersonnelWorkload
                {
                    PersonnelId = personnelId,
                    PersonnelName = personnelName,
                    TotalShifts = 0,
                    DayShifts = 0,
                    NightShifts = 0
                };
            }
        }

        return workloads;
    }

    /// <summary>
    /// 计算哨位覆盖率统计 - 对应需求3.3
    /// </summary>
    /// <param name="schedule">排班模型</param>
    /// <returns>哨位覆盖率字典（键为哨位ID）</returns>
    private async Task<Dictionary<int, PositionCoverage>> CalculatePositionCoverages(Schedule schedule)
    {
        var coverages = new Dictionary<int, PositionCoverage>();

        // 获取哨位名称映射
        var positions = await _positionRepo.GetPositionsByIdsAsync(schedule.PositionIds);
        var positionNames = positions.ToDictionary(p => p.Id, p => p.Name);

        // 计算总时段数：天数 × 12个时段/天
        var totalDays = (schedule.EndDate.Date - schedule.StartDate.Date).Days + 1;
        var totalSlotsPerPosition = totalDays * 12;

        // 按哨位分组统计已分配时段数
        var shiftsByPosition = schedule.Results.GroupBy(s => s.PositionId);

        foreach (var positionId in schedule.PositionIds)
        {
            var positionName = positionNames.TryGetValue(positionId, out var name) 
                ? name 
                : $"未知哨位 (ID: {positionId})";

            // 获取该哨位的已分配时段数
            var assignedSlots = shiftsByPosition
                .FirstOrDefault(g => g.Key == positionId)
                ?.Count() ?? 0;

            // 计算覆盖率
            var coverageRate = totalSlotsPerPosition > 0 
                ? (double)assignedSlots / totalSlotsPerPosition 
                : 0.0;

            coverages[positionId] = new PositionCoverage
            {
                PositionId = positionId,
                PositionName = positionName,
                AssignedSlots = assignedSlots,
                TotalSlots = totalSlotsPerPosition,
                CoverageRate = Math.Round(coverageRate, 4) // 保留4位小数
            };
        }

        return coverages;
    }

    /// <summary>
    /// 计算软约束评分 - 对应需求3.3
    /// </summary>
    /// <param name="schedule">排班模型</param>
    /// <returns>软约束评分</returns>
    private async Task<SoftConstraintScores> CalculateSoftConstraintScores(Schedule schedule)
    {
        // 构建排班上下文用于评分计算
        var context = new SchedulingContext
        {
            Personals = await _personalRepo.GetPersonnelByIdsAsync(schedule.PersonnelIds),
            Positions = await _positionRepo.GetPositionsByIdsAsync(schedule.PositionIds),
            Skills = await _skillRepo.GetAllAsync(),
            StartDate = schedule.StartDate.Date,
            EndDate = schedule.EndDate.Date,
            HolidayConfig = await _constraintRepo.GetActiveHolidayConfigAsync(),
            LastConfirmedSchedule = await _historyMgmt.GetLastConfirmedScheduleAsync()
        };

        // 创建软约束计算器
        var softConstraintCalculator = new SchedulingEngine.Core.SoftConstraintCalculator(context);

        // 计算各项软约束评分
        double restScore = 0.0;
        double timeSlotBalanceScore = 0.0;
        double holidayBalanceScore = 0.0;

        try
        {
            // 创建人员ID到索引的映射
            var personIdToIndex = new Dictionary<int, int>();
            for (int i = 0; i < context.Personals.Count; i++)
            {
                personIdToIndex[context.Personals[i].Id] = i;
            }

            // 遍历所有班次，累计评分
            foreach (var shift in schedule.Results)
            {
                // 检查人员ID是否在映射中
                if (!personIdToIndex.TryGetValue(shift.PersonnelId, out var personIdx))
                {
                    System.Diagnostics.Debug.WriteLine($"警告: 人员ID {shift.PersonnelId} 不在上下文中，跳过评分计算");
                    continue;
                }

                var date = shift.StartTime.Date;
                var periodIndex = CalcPeriodIndex(shift.StartTime);

                // 计算休息时间评分
                restScore += softConstraintCalculator.CalculateRestScore(personIdx, date);

                // 计算时段平衡评分
                timeSlotBalanceScore += softConstraintCalculator.CalculateTimeSlotBalanceScore(personIdx, periodIndex);

                // 计算节假日平衡评分
                holidayBalanceScore += softConstraintCalculator.CalculateHolidayBalanceScore(personIdx, date);
            }

            // 归一化评分（可选，根据实际需求调整）
            var totalShifts = schedule.Results.Count;
            if (totalShifts > 0)
            {
                restScore /= totalShifts;
                timeSlotBalanceScore /= totalShifts;
                holidayBalanceScore /= totalShifts;
            }
        }
        catch (Exception ex)
        {
            // 如果评分计算失败，记录日志但不影响整体流程
            System.Diagnostics.Debug.WriteLine($"软约束评分计算失败: {ex.Message}");
        }

        // 计算总分
        var totalScore = restScore + timeSlotBalanceScore + holidayBalanceScore;

        return new SoftConstraintScores
        {
            TotalScore = Math.Round(totalScore, 2),
            RestScore = Math.Round(restScore, 2),
            TimeSlotBalanceScore = Math.Round(timeSlotBalanceScore, 2),
            HolidayBalanceScore = Math.Round(holidayBalanceScore, 2)
        };
    }

    /// <summary>
    /// 构建失败结果，包含错误消息、部分完成的分配数、冲突详情 - 对应需求4.1, 4.2, 4.3, 4.4, 4.5
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="partialSchedule">部分完成的排班（如果有）</param>
    /// <param name="partialCompletedAssignments">部分完成的分配数</param>
    /// <param name="totalDuration">总执行时长</param>
    /// <param name="request">原始排班请求</param>
    /// <returns>包含失败信息的排班结果</returns>
    private async Task<SchedulingResult> BuildFailureResult(
        string errorMessage,
        Schedule? partialSchedule,
        int partialCompletedAssignments,
        TimeSpan totalDuration,
        SchedulingRequestDto request)
    {
        var conflicts = new List<ConflictInfo>();
        SchedulingStatistics? statistics = null;

        try
        {
            // 如果有部分完成的排班，尝试构建统计信息和冲突详情
            if (partialSchedule != null && partialSchedule.Results?.Any() == true)
            {
                // 设置排班的基本信息
                partialSchedule.Header = request.Title;
                partialSchedule.StartDate = request.StartDate.Date;
                partialSchedule.EndDate = request.EndDate.Date;
                partialSchedule.PersonnelIds = request.PersonnelIds;
                partialSchedule.PositionIds = request.PositionIds;

                // 计算部分统计信息
                var personnelWorkloads = await CalculatePersonnelWorkloads(partialSchedule);
                var positionCoverages = await CalculatePositionCoverages(partialSchedule);
                var softScores = await CalculateSoftConstraintScores(partialSchedule);

                statistics = new SchedulingStatistics
                {
                    TotalAssignments = partialCompletedAssignments,
                    PersonnelWorkloads = personnelWorkloads,
                    PositionCoverages = positionCoverages,
                    SoftScores = softScores
                };

                // 生成冲突信息
                var scheduleDto = await MapToScheduleDtoAsync(partialSchedule, confirmedAt: null);
                conflicts = scheduleDto.Conflicts.Select(c => new ConflictInfo
                {
                    ConflictType = c.Type,
                    Description = c.Message,
                    PositionId = c.PositionId,
                    PositionName = null,
                    PeriodIndex = c.PeriodIndex,
                    Date = c.StartTime?.Date
                }).ToList();

                // 添加失败原因作为冲突信息
                conflicts.Insert(0, new ConflictInfo
                {
                    ConflictType = "error",
                    Description = errorMessage,
                    PositionId = null,
                    PositionName = null,
                    PeriodIndex = null,
                    Date = null
                });
            }
            else
            {
                // 没有部分结果，只添加错误信息
                conflicts.Add(new ConflictInfo
                {
                    ConflictType = "error",
                    Description = errorMessage,
                    PositionId = null,
                    PositionName = null,
                    PeriodIndex = null,
                    Date = null
                });
            }
        }
        catch (Exception ex)
        {
            // 如果构建失败信息时出错，记录日志但不影响返回
            System.Diagnostics.Debug.WriteLine($"构建失败结果时出错: {ex.Message}");
            
            // 确保至少有基本的错误信息
            if (!conflicts.Any())
            {
                conflicts.Add(new ConflictInfo
                {
                    ConflictType = "error",
                    Description = errorMessage,
                    PositionId = null,
                    PositionName = null,
                    PeriodIndex = null,
                    Date = null
                });
            }
        }

        return new SchedulingResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Schedule = null,
            Statistics = statistics,
            Conflicts = conflicts,
            TotalDuration = totalDuration
        };
    }

    #endregion

    #region 排班表格数据构建 - 对应需求5.1, 5.2, 5.3, 5.4, 5.5

    /// <summary>
    /// 构建排班表格数据 - 对应需求5.1, 5.2, 5.3, 5.4, 5.5
    /// </summary>
    /// <param name="scheduleDto">排班DTO</param>
    /// <returns>排班表格数据</returns>
    public async Task<ScheduleGridData> BuildScheduleGridData(ScheduleDto scheduleDto)
    {
        if (scheduleDto == null)
            throw new ArgumentNullException(nameof(scheduleDto));

        var gridData = new ScheduleGridData
        {
            StartDate = scheduleDto.StartDate.Date,
            EndDate = scheduleDto.EndDate.Date,
            PositionIds = scheduleDto.PositionIds.ToList(),
            TotalPeriods = 12 // 每天12个时段
        };

        // 计算总天数
        gridData.TotalDays = (scheduleDto.EndDate.Date - scheduleDto.StartDate.Date).Days + 1;

        // 构建列数据：遍历 PositionIds，创建 ScheduleGridColumn 列表 - 对应需求5.1, 5.2
        var positions = await _positionRepo.GetPositionsByIdsAsync(scheduleDto.PositionIds);
        var positionNames = positions.ToDictionary(p => p.Id, p => p.Name);

        for (int colIndex = 0; colIndex < scheduleDto.PositionIds.Count; colIndex++)
        {
            var positionId = scheduleDto.PositionIds[colIndex];
            var positionName = positionNames.TryGetValue(positionId, out var name) 
                ? name 
                : $"未知哨位 (ID: {positionId})";

            gridData.Columns.Add(new ScheduleGridColumn
            {
                PositionId = positionId,
                PositionName = positionName,
                ColumnIndex = colIndex
            });
        }

        // 构建行数据：遍历日期范围和时段，创建 ScheduleGridRow 列表 - 对应需求5.1, 5.2
        int rowIndex = 0;
        for (var date = scheduleDto.StartDate.Date; date <= scheduleDto.EndDate.Date; date = date.AddDays(1))
        {
            for (int periodIndex = 0; periodIndex < 12; periodIndex++)
            {
                var startHour = periodIndex * 2;
                var endHour = (periodIndex + 1) * 2;
                var timeRange = $"{startHour:D2}:00-{endHour:D2}:00";
                var displayText = $"{date:yyyy-MM-dd} {timeRange}";

                gridData.Rows.Add(new ScheduleGridRow
                {
                    Date = date,
                    PeriodIndex = periodIndex,
                    TimeRange = timeRange,
                    DisplayText = displayText,
                    RowIndex = rowIndex
                });

                rowIndex++;
            }
        }

        // 构建单元格数据：遍历 Shifts，创建 ScheduleGridCell 字典，键为 "row_col" - 对应需求5.1, 5.2
        var shiftsByKey = new Dictionary<string, ShiftDto>();
        foreach (var shift in scheduleDto.Shifts)
        {
            var shiftDate = shift.StartTime.Date;
            var shiftPeriodIndex = shift.PeriodIndex;
            var shiftPositionId = shift.PositionId;

            // 查找对应的行索引和列索引
            var row = gridData.Rows.FirstOrDefault(r => 
                r.Date == shiftDate && r.PeriodIndex == shiftPeriodIndex);
            var col = gridData.Columns.FirstOrDefault(c => 
                c.PositionId == shiftPositionId);

            if (row != null && col != null)
            {
                var key = $"{row.RowIndex}_{col.ColumnIndex}";
                
                // 如果同一个单元格有多个班次（不应该发生，但做防御性处理）
                if (!shiftsByKey.ContainsKey(key))
                {
                    shiftsByKey[key] = shift;
                }
            }
        }

        // 标记手动指定的单元格（通过查询 ManualAssignments） - 对应需求5.3, 5.4
        var manualAssignments = await _constraintRepo.GetManualAssignmentsByDateRangeAsync(
            scheduleDto.StartDate, 
            scheduleDto.EndDate, 
            enabledOnly: true);

        var manualAssignmentKeys = new HashSet<string>();
        foreach (var manual in manualAssignments)
        {
            var row = gridData.Rows.FirstOrDefault(r => 
                r.Date == manual.Date.Date && r.PeriodIndex == manual.PeriodIndex);
            var col = gridData.Columns.FirstOrDefault(c => 
                c.PositionId == manual.PositionId);

            if (row != null && col != null)
            {
                var key = $"{row.RowIndex}_{col.ColumnIndex}";
                manualAssignmentKeys.Add(key);
            }
        }

        // 标记有冲突的单元格（通过 Conflicts 列表） - 对应需求5.4, 5.5
        var conflictKeys = new Dictionary<string, string>();
        foreach (var conflict in scheduleDto.Conflicts)
        {
            if (conflict.StartTime.HasValue && conflict.PositionId.HasValue && conflict.PeriodIndex.HasValue)
            {
                var conflictDate = conflict.StartTime.Value.Date;
                var conflictPeriodIndex = conflict.PeriodIndex.Value;
                var conflictPositionId = conflict.PositionId.Value;

                var row = gridData.Rows.FirstOrDefault(r => 
                    r.Date == conflictDate && r.PeriodIndex == conflictPeriodIndex);
                var col = gridData.Columns.FirstOrDefault(c => 
                    c.PositionId == conflictPositionId);

                if (row != null && col != null)
                {
                    var key = $"{row.RowIndex}_{col.ColumnIndex}";
                    conflictKeys[key] = conflict.Message;
                }
            }
        }

        // 创建所有单元格
        foreach (var row in gridData.Rows)
        {
            foreach (var col in gridData.Columns)
            {
                var key = $"{row.RowIndex}_{col.ColumnIndex}";
                
                var cell = new ScheduleGridCell
                {
                    RowIndex = row.RowIndex,
                    ColumnIndex = col.ColumnIndex,
                    IsAssigned = shiftsByKey.ContainsKey(key),
                    IsManualAssignment = manualAssignmentKeys.Contains(key),
                    HasConflict = conflictKeys.ContainsKey(key),
                    ConflictMessage = conflictKeys.TryGetValue(key, out var conflictMsg) ? conflictMsg : null
                };

                // 如果已分配，填充人员信息
                if (cell.IsAssigned && shiftsByKey.TryGetValue(key, out var shift))
                {
                    cell.PersonnelId = shift.PersonnelId;
                    cell.PersonnelName = shift.PersonnelName;
                }

                gridData.Cells[key] = cell;
            }
        }

        return gridData;
    }

    #endregion
}
