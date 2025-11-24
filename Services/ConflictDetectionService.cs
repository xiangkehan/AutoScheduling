using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;

namespace AutoScheduling3.Services;

/// <summary>
/// 冲突检测服务实现
/// </summary>
public class ConflictDetectionService : IConflictDetectionService
{
    private readonly IPersonnelService _personnelService;
    private readonly IPositionService _positionService;

    // 缓存人员和哨位信息，避免重复查询
    private Dictionary<int, PersonnelDto>? _personnelCache;
    private Dictionary<int, PositionDto>? _positionCache;

    public ConflictDetectionService(
        IPersonnelService personnelService,
        IPositionService positionService)
    {
        _personnelService = personnelService;
        _positionService = positionService;
    }

    /// <summary>
    /// 检测排班中的所有冲突
    /// </summary>
    public async Task<List<ConflictDto>> DetectConflictsAsync(ScheduleDto schedule)
    {
        // 初始化缓存
        await InitializeCacheAsync(schedule);

        var allConflicts = new List<ConflictDto>();

        // 并行执行各种冲突检测
        var detectionTasks = new[]
        {
            DetectSkillMismatchAsync(schedule),
            DetectPersonnelUnavailableAsync(schedule),
            DetectInsufficientRestAsync(schedule),
            DetectWorkloadImbalanceAsync(schedule),
            DetectConsecutiveOvertimeAsync(schedule),
            DetectUnassignedSlotsAsync(schedule),
            DetectDuplicateAssignmentsAsync(schedule)
        };

        var results = await Task.WhenAll(detectionTasks);

        // 合并所有冲突
        foreach (var conflicts in results)
        {
            allConflicts.AddRange(conflicts);
        }

        return allConflicts;
    }

    /// <summary>
    /// 检测特定班次的冲突
    /// </summary>
    public async Task<List<ConflictDto>> DetectShiftConflictsAsync(object shift, ScheduleDto schedule)
    {
        // 初始化缓存
        await InitializeCacheAsync(schedule);

        var conflicts = new List<ConflictDto>();

        if (shift is not ShiftDto shiftDto)
        {
            return conflicts;
        }

        // 检测该班次的技能匹配
        var skillConflicts = await DetectShiftSkillMismatchAsync(shiftDto);
        conflicts.AddRange(skillConflicts);

        // 检测该班次的人员可用性
        var availabilityConflicts = await DetectShiftPersonnelAvailabilityAsync(shiftDto);
        conflicts.AddRange(availabilityConflicts);

        return conflicts;
    }

    /// <summary>
    /// 获取冲突统计信息
    /// </summary>
    public ConflictStatistics GetConflictStatistics(List<ConflictDto> conflicts)
    {
        var statistics = new ConflictStatistics
        {
            HardConflictCount = conflicts.Count(c => c.Type == "hard" && !c.IsIgnored),
            SoftConflictCount = conflicts.Count(c => c.Type == "soft" && !c.IsIgnored),
            UnassignedSlotCount = conflicts.Count(c => c.Type == "unassigned" && !c.IsIgnored),
            IgnoredConflictCount = conflicts.Count(c => c.IsIgnored)
        };

        // 按类型分组统计
        var conflictsByType = conflicts
            .Where(c => !c.IsIgnored)
            .GroupBy(c => c.SubType)
            .ToDictionary(g => g.Key, g => g.Count());

        statistics.ConflictsByType = conflictsByType;

        return statistics;
    }

    /// <summary>
    /// 检测技能不匹配冲突
    /// </summary>
    public async Task<List<ConflictDto>> DetectSkillMismatchAsync(ScheduleDto schedule)
    {
        var conflicts = new List<ConflictDto>();

        foreach (var shift in schedule.Shifts)
        {
            var shiftConflicts = await DetectShiftSkillMismatchAsync(shift);
            conflicts.AddRange(shiftConflicts);
        }

        return conflicts;
    }

    /// <summary>
    /// 检测人员不可用冲突
    /// </summary>
    public async Task<List<ConflictDto>> DetectPersonnelUnavailableAsync(ScheduleDto schedule)
    {
        var conflicts = new List<ConflictDto>();

        foreach (var shift in schedule.Shifts)
        {
            var shiftConflicts = await DetectShiftPersonnelAvailabilityAsync(shift);
            conflicts.AddRange(shiftConflicts);
        }

        return conflicts;
    }

    /// <summary>
    /// 检测休息时间不足冲突
    /// </summary>
    public async Task<List<ConflictDto>> DetectInsufficientRestAsync(ScheduleDto schedule)
    {
        var conflicts = new List<ConflictDto>();
        var minRestHours = 8; // 最小休息时间（小时）

        // 按人员分组
        var shiftsByPersonnel = schedule.Shifts
            .GroupBy(s => s.PersonnelId)
            .ToList();

        foreach (var group in shiftsByPersonnel)
        {
            var shifts = group.OrderBy(s => s.StartTime).ToList();

            for (int i = 0; i < shifts.Count - 1; i++)
            {
                var currentShift = shifts[i];
                var nextShift = shifts[i + 1];

                var restHours = (nextShift.StartTime - currentShift.EndTime).TotalHours;

                if (restHours < minRestHours)
                {
                    conflicts.Add(new ConflictDto
                    {
                        Type = "soft",
                        SubType = ConflictSubType.InsufficientRest,
                        Message = $"{currentShift.PersonnelName} 的班次间隔仅 {restHours:F1} 小时（建议 ≥{minRestHours} 小时）",
                        DetailedMessage = $"人员 {currentShift.PersonnelName} 在 {currentShift.EndTime:yyyy-MM-dd HH:mm} 结束班次后，" +
                                        $"在 {nextShift.StartTime:yyyy-MM-dd HH:mm} 又开始新班次，休息时间仅 {restHours:F1} 小时，" +
                                        $"少于建议的 {minRestHours} 小时。",
                        PersonnelId = currentShift.PersonnelId,
                        PersonnelName = currentShift.PersonnelName,
                        StartTime = currentShift.StartTime,
                        EndTime = nextShift.EndTime,
                        RelatedShiftIds = new List<int> { currentShift.Id, nextShift.Id },
                        Severity = 3,
                        IsFixable = true
                    });
                }
            }
        }

        return await Task.FromResult(conflicts);
    }

    /// <summary>
    /// 检测工作量不均衡冲突
    /// </summary>
    public async Task<List<ConflictDto>> DetectWorkloadImbalanceAsync(ScheduleDto schedule)
    {
        var conflicts = new List<ConflictDto>();

        // 计算每个人员的工作量
        var workloadByPersonnel = schedule.Shifts
            .GroupBy(s => s.PersonnelId)
            .Select(g => new
            {
                PersonnelId = g.Key,
                PersonnelName = g.First().PersonnelName,
                ShiftCount = g.Count()
            })
            .ToList();

        if (workloadByPersonnel.Count == 0)
        {
            return conflicts;
        }

        // 计算平均工作量
        var averageWorkload = workloadByPersonnel.Average(w => w.ShiftCount);
        var threshold = 0.3; // 超出平均值30%视为不均衡

        foreach (var workload in workloadByPersonnel)
        {
            var deviation = (workload.ShiftCount - averageWorkload) / averageWorkload;

            if (deviation > threshold)
            {
                var personnelShifts = schedule.Shifts
                    .Where(s => s.PersonnelId == workload.PersonnelId)
                    .ToList();

                conflicts.Add(new ConflictDto
                {
                    Type = "soft",
                    SubType = ConflictSubType.WorkloadImbalance,
                    Message = $"{workload.PersonnelName}: {workload.ShiftCount} 班次（平均 {averageWorkload:F1} 班次）",
                    DetailedMessage = $"人员 {workload.PersonnelName} 的工作量为 {workload.ShiftCount} 班次，" +
                                    $"超出平均值 {averageWorkload:F1} 班次的 {deviation:P1}，存在工作量不均衡。",
                    PersonnelId = workload.PersonnelId,
                    PersonnelName = workload.PersonnelName,
                    RelatedShiftIds = personnelShifts.Select(s => s.Id).ToList(),
                    Severity = 2,
                    IsFixable = true
                });
            }
        }

        return await Task.FromResult(conflicts);
    }

    /// <summary>
    /// 检测连续工作超时冲突
    /// </summary>
    public async Task<List<ConflictDto>> DetectConsecutiveOvertimeAsync(ScheduleDto schedule)
    {
        var conflicts = new List<ConflictDto>();
        var maxConsecutiveHours = 12; // 最大连续工作时间（小时）

        // 按人员分组
        var shiftsByPersonnel = schedule.Shifts
            .GroupBy(s => s.PersonnelId)
            .ToList();

        foreach (var group in shiftsByPersonnel)
        {
            var shifts = group.OrderBy(s => s.StartTime).ToList();

            // 查找连续的班次
            var consecutiveShifts = new List<ShiftDto>();
            DateTime? lastEndTime = null;

            foreach (var shift in shifts)
            {
                if (lastEndTime == null || shift.StartTime == lastEndTime)
                {
                    // 连续班次
                    consecutiveShifts.Add(shift);
                    lastEndTime = shift.EndTime;
                }
                else
                {
                    // 检查之前的连续班次
                    CheckConsecutiveOvertime(consecutiveShifts, maxConsecutiveHours, conflicts);

                    // 开始新的连续序列
                    consecutiveShifts.Clear();
                    consecutiveShifts.Add(shift);
                    lastEndTime = shift.EndTime;
                }
            }

            // 检查最后一组连续班次
            CheckConsecutiveOvertime(consecutiveShifts, maxConsecutiveHours, conflicts);
        }

        return await Task.FromResult(conflicts);
    }

    /// <summary>
    /// 检测未分配时段
    /// </summary>
    public async Task<List<ConflictDto>> DetectUnassignedSlotsAsync(ScheduleDto schedule)
    {
        var conflicts = new List<ConflictDto>();

        // 获取所有需要覆盖的时段
        var startDate = schedule.StartDate.Date;
        var endDate = schedule.EndDate.Date;
        var positions = schedule.PositionIds;

        // 创建所有应该被分配的时段集合
        var requiredSlots = new HashSet<string>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            foreach (var positionId in positions)
            {
                for (int period = 0; period < 12; period++)
                {
                    requiredSlots.Add($"{positionId}_{date:yyyy-MM-dd}_{period}");
                }
            }
        }

        // 移除已分配的时段
        foreach (var shift in schedule.Shifts)
        {
            var slotKey = $"{shift.PositionId}_{shift.StartTime.Date:yyyy-MM-dd}_{shift.PeriodIndex}";
            requiredSlots.Remove(slotKey);
        }

        // 为每个未分配的时段创建冲突
        foreach (var slotKey in requiredSlots)
        {
            var parts = slotKey.Split('_');
            var positionId = int.Parse(parts[0]);
            var date = DateTime.Parse(parts[1]);
            var periodIndex = int.Parse(parts[2]);

            var position = _positionCache?.GetValueOrDefault(positionId);
            var startTime = date.AddHours(periodIndex * 2);

            conflicts.Add(new ConflictDto
            {
                Type = "unassigned",
                SubType = ConflictSubType.UnassignedSlot,
                Message = $"{position?.Name ?? $"哨位{positionId}"} - {startTime:yyyy-MM-dd HH:mm}-{startTime.AddHours(2):HH:mm}",
                DetailedMessage = $"哨位 {position?.Name ?? $"哨位{positionId}"} 在 {startTime:yyyy-MM-dd HH:mm} 至 {startTime.AddHours(2):HH:mm} 时段未分配人员。",
                PositionId = positionId,
                PositionName = position?.Name,
                StartTime = startTime,
                EndTime = startTime.AddHours(2),
                PeriodIndex = periodIndex,
                Severity = 4,
                IsFixable = true
            });
        }

        return await Task.FromResult(conflicts);
    }

    /// <summary>
    /// 检测重复分配冲突
    /// </summary>
    public async Task<List<ConflictDto>> DetectDuplicateAssignmentsAsync(ScheduleDto schedule)
    {
        var conflicts = new List<ConflictDto>();

        // 按人员和时间分组，查找重复分配
        var duplicates = schedule.Shifts
            .GroupBy(s => new { s.PersonnelId, s.StartTime, s.PeriodIndex })
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in duplicates)
        {
            var shifts = group.ToList();
            var firstShift = shifts.First();

            var positionNames = string.Join("、", shifts.Select(s => s.PositionName));

            conflicts.Add(new ConflictDto
            {
                Type = "hard",
                SubType = ConflictSubType.DuplicateAssignment,
                Message = $"{firstShift.PersonnelName} 在 {firstShift.StartTime:yyyy-MM-dd HH:mm} 被重复分配到多个哨位",
                DetailedMessage = $"人员 {firstShift.PersonnelName} 在 {firstShift.StartTime:yyyy-MM-dd HH:mm} 至 {firstShift.EndTime:HH:mm} " +
                                $"被同时分配到以下哨位：{positionNames}。",
                PersonnelId = firstShift.PersonnelId,
                PersonnelName = firstShift.PersonnelName,
                StartTime = firstShift.StartTime,
                EndTime = firstShift.EndTime,
                PeriodIndex = firstShift.PeriodIndex,
                RelatedShiftIds = shifts.Select(s => s.Id).ToList(),
                Severity = 5,
                IsFixable = true
            });
        }

        return await Task.FromResult(conflicts);
    }

    #region 私有辅助方法

    /// <summary>
    /// 初始化人员和哨位缓存
    /// </summary>
    private async Task InitializeCacheAsync(ScheduleDto schedule)
    {
        if (_personnelCache == null)
        {
            var allPersonnel = await _personnelService.GetAllAsync();
            _personnelCache = allPersonnel.ToDictionary(p => p.Id);
        }

        if (_positionCache == null)
        {
            var allPositions = await _positionService.GetAllAsync();
            _positionCache = allPositions.ToDictionary(p => p.Id);
        }
    }

    /// <summary>
    /// 检测单个班次的技能匹配
    /// </summary>
    private async Task<List<ConflictDto>> DetectShiftSkillMismatchAsync(ShiftDto shift)
    {
        var conflicts = new List<ConflictDto>();

        // 验证技能匹配
        var isSkillMatch = await _positionService.ValidatePersonnelSkillsAsync(shift.PersonnelId, shift.PositionId);

        if (!isSkillMatch)
        {
            var personnel = _personnelCache?.GetValueOrDefault(shift.PersonnelId);
            var position = _positionCache?.GetValueOrDefault(shift.PositionId);

            conflicts.Add(new ConflictDto
            {
                Type = "hard",
                SubType = ConflictSubType.SkillMismatch,
                Message = $"{shift.PersonnelName} → {shift.PositionName} ({shift.StartTime:yyyy-MM-dd HH:mm})",
                DetailedMessage = $"人员 {shift.PersonnelName} 缺少哨位 {shift.PositionName} 所需的技能。",
                PersonnelId = shift.PersonnelId,
                PersonnelName = shift.PersonnelName,
                PositionId = shift.PositionId,
                PositionName = shift.PositionName,
                StartTime = shift.StartTime,
                EndTime = shift.EndTime,
                PeriodIndex = shift.PeriodIndex,
                RelatedShiftIds = new List<int> { shift.Id },
                Severity = 5,
                IsFixable = true
            });
        }

        return conflicts;
    }

    /// <summary>
    /// 检测单个班次的人员可用性
    /// </summary>
    private async Task<List<ConflictDto>> DetectShiftPersonnelAvailabilityAsync(ShiftDto shift)
    {
        var conflicts = new List<ConflictDto>();

        var personnel = _personnelCache?.GetValueOrDefault(shift.PersonnelId);

        // 检查人员是否存在且可用
        if (personnel == null || !personnel.IsAvailable)
        {
            conflicts.Add(new ConflictDto
            {
                Type = "hard",
                SubType = ConflictSubType.PersonnelUnavailable,
                Message = $"{shift.PersonnelName} → {shift.PositionName} ({shift.StartTime:yyyy-MM-dd HH:mm})",
                DetailedMessage = $"人员 {shift.PersonnelName} 在此时段不可用。",
                PersonnelId = shift.PersonnelId,
                PersonnelName = shift.PersonnelName,
                PositionId = shift.PositionId,
                PositionName = shift.PositionName,
                StartTime = shift.StartTime,
                EndTime = shift.EndTime,
                PeriodIndex = shift.PeriodIndex,
                RelatedShiftIds = new List<int> { shift.Id },
                Severity = 5,
                IsFixable = true
            });
        }

        return await Task.FromResult(conflicts);
    }

    /// <summary>
    /// 检查连续工作超时
    /// </summary>
    private void CheckConsecutiveOvertime(List<ShiftDto> consecutiveShifts, int maxConsecutiveHours, List<ConflictDto> conflicts)
    {
        if (consecutiveShifts.Count == 0) return;

        var totalHours = (consecutiveShifts.Last().EndTime - consecutiveShifts.First().StartTime).TotalHours;

        if (totalHours > maxConsecutiveHours)
        {
            var firstShift = consecutiveShifts.First();

            conflicts.Add(new ConflictDto
            {
                Type = "soft",
                SubType = ConflictSubType.ConsecutiveOvertime,
                Message = $"{firstShift.PersonnelName} 连续工作 {totalHours:F1} 小时（建议 ≤{maxConsecutiveHours} 小时）",
                DetailedMessage = $"人员 {firstShift.PersonnelName} 从 {consecutiveShifts.First().StartTime:yyyy-MM-dd HH:mm} " +
                                $"到 {consecutiveShifts.Last().EndTime:yyyy-MM-dd HH:mm} 连续工作 {totalHours:F1} 小时，" +
                                $"超过建议的 {maxConsecutiveHours} 小时。",
                PersonnelId = firstShift.PersonnelId,
                PersonnelName = firstShift.PersonnelName,
                StartTime = consecutiveShifts.First().StartTime,
                EndTime = consecutiveShifts.Last().EndTime,
                RelatedShiftIds = consecutiveShifts.Select(s => s.Id).ToList(),
                Severity = 3,
                IsFixable = true
            });
        }
    }

    #endregion
}
