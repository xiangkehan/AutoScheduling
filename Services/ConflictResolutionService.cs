using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;

namespace AutoScheduling3.Services;

/// <summary>
/// 冲突修复服务实现
/// </summary>
public class ConflictResolutionService : IConflictResolutionService
{
    private readonly IPersonnelService _personnelService;
    private readonly IPositionService _positionService;
    private readonly IConflictDetectionService _conflictDetectionService;

    public ConflictResolutionService(
        IPersonnelService personnelService,
        IPositionService positionService,
        IConflictDetectionService conflictDetectionService)
    {
        _personnelService = personnelService;
        _positionService = positionService;
        _conflictDetectionService = conflictDetectionService;
    }

    /// <summary>
    /// 生成冲突修复方案
    /// </summary>
    public async Task<List<ConflictResolutionOption>> GenerateResolutionOptionsAsync(
        ConflictDto conflict,
        ScheduleDto schedule)
    {
        return conflict.SubType switch
        {
            ConflictSubType.SkillMismatch => await GenerateSkillMismatchResolutionsAsync(conflict, schedule),
            ConflictSubType.WorkloadImbalance => await GenerateWorkloadImbalanceResolutionsAsync(conflict, schedule),
            ConflictSubType.UnassignedSlot => await GenerateUnassignedSlotResolutionsAsync(conflict, schedule),
            ConflictSubType.InsufficientRest => await GenerateInsufficientRestResolutionsAsync(conflict, schedule),
            _ => new List<ConflictResolutionOption>()
        };
    }

    /// <summary>
    /// 应用修复方案
    /// </summary>
    public async Task<ScheduleDto> ApplyResolutionAsync(
        ConflictResolutionOption option,
        ScheduleDto schedule)
    {
        // 验证方案有效性
        var (isValid, reason) = await ValidateResolutionAsync(option, schedule);
        if (!isValid)
        {
            throw new InvalidOperationException($"修复方案无效: {reason}");
        }

        // 根据方案类型执行相应操作
        return option.Type switch
        {
            ResolutionType.ReplacePersonnel => await ApplyReplacePersonnelAsync(option, schedule),
            ResolutionType.RemoveAssignment => await ApplyRemoveAssignmentAsync(option, schedule),
            ResolutionType.AdjustTime => await ApplyAdjustTimeAsync(option, schedule),
            ResolutionType.ReassignShifts => await ApplyReassignShiftsAsync(option, schedule),
            _ => throw new NotSupportedException($"不支持的修复方案类型: {option.Type}")
        };
    }

    /// <summary>
    /// 验证修复方案的有效性
    /// </summary>
    public async Task<(bool IsValid, string? Reason)> ValidateResolutionAsync(
        ConflictResolutionOption option,
        ScheduleDto schedule)
    {
        // 基本验证
        if (option == null)
        {
            return (false, "修复方案不能为空");
        }

        if (option.ResolutionData == null && option.Type != ResolutionType.RemoveAssignment)
        {
            return (false, "修复方案数据不能为空");
        }

        // 根据方案类型进行特定验证
        return option.Type switch
        {
            ResolutionType.ReplacePersonnel => await ValidateReplacePersonnelAsync(option, schedule),
            ResolutionType.RemoveAssignment => (true, null),
            ResolutionType.AdjustTime => await ValidateAdjustTimeAsync(option, schedule),
            ResolutionType.ReassignShifts => await ValidateReassignShiftsAsync(option, schedule),
            _ => (false, $"不支持的修复方案类型: {option.Type}")
        };
    }

    /// <summary>
    /// 评估修复方案的影响
    /// </summary>
    public async Task<ResolutionImpact> EvaluateImpactAsync(
        ConflictResolutionOption option,
        ScheduleDto schedule)
    {
        // 创建临时排班副本
        var tempSchedule = CloneSchedule(schedule);

        try
        {
            // 应用修复方案到临时排班
            tempSchedule = await ApplyResolutionAsync(option, tempSchedule);

            // 检测新的冲突
            var newConflicts = await _conflictDetectionService.DetectConflictsAsync(tempSchedule);
            var originalConflicts = await _conflictDetectionService.DetectConflictsAsync(schedule);

            // 计算影响
            var resolvedCount = originalConflicts.Count - newConflicts.Count;
            var newConflictCount = newConflicts.Count(c =>
                !originalConflicts.Any(oc => oc.Id == c.Id));

            // 收集受影响的人员和哨位
            var affectedPersonnelIds = new HashSet<int>();
            var affectedPositionIds = new HashSet<int>();

            foreach (var shift in tempSchedule.Shifts)
            {
                var originalShift = schedule.Shifts.FirstOrDefault(s => s.Id == shift.Id);
                if (originalShift == null || originalShift.PersonnelId != shift.PersonnelId)
                {
                    affectedPersonnelIds.Add(shift.PersonnelId);
                    affectedPositionIds.Add(shift.PositionId);
                }
            }

            return new ResolutionImpact
            {
                ResolvedConflicts = Math.Max(0, resolvedCount),
                NewConflicts = newConflictCount,
                AffectedPersonnelIds = affectedPersonnelIds.ToList(),
                AffectedPositionIds = affectedPositionIds.ToList(),
                Description = GenerateImpactDescription(resolvedCount, newConflictCount, affectedPersonnelIds.Count)
            };
        }
        catch (Exception ex)
        {
            return new ResolutionImpact
            {
                ResolvedConflicts = 0,
                NewConflicts = 0,
                Description = $"无法评估影响: {ex.Message}"
            };
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 克隆排班数据
    /// </summary>
    private ScheduleDto CloneSchedule(ScheduleDto schedule)
    {
        return new ScheduleDto
        {
            Id = schedule.Id,
            Title = schedule.Title,
            StartDate = schedule.StartDate,
            EndDate = schedule.EndDate,
            PositionIds = new List<int>(schedule.PositionIds),
            Shifts = schedule.Shifts.Select(s => new ShiftDto
            {
                Id = s.Id,
                PersonnelId = s.PersonnelId,
                PersonnelName = s.PersonnelName,
                PositionId = s.PositionId,
                PositionName = s.PositionName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                PeriodIndex = s.PeriodIndex
            }).ToList()
        };
    }

    /// <summary>
    /// 生成影响描述
    /// </summary>
    private string GenerateImpactDescription(int resolvedCount, int newConflictCount, int affectedPersonnelCount)
    {
        var parts = new List<string>();

        if (resolvedCount > 0)
        {
            parts.Add($"将解决 {resolvedCount} 个冲突");
        }

        if (newConflictCount > 0)
        {
            parts.Add($"可能产生 {newConflictCount} 个新冲突");
        }

        if (affectedPersonnelCount > 0)
        {
            parts.Add($"影响 {affectedPersonnelCount} 名人员");
        }

        return parts.Count > 0 ? string.Join("，", parts) : "无明显影响";
    }

    /// <summary>
    /// 验证替换人员方案
    /// </summary>
    private async Task<(bool IsValid, string? Reason)> ValidateReplacePersonnelAsync(
        ConflictResolutionOption option,
        ScheduleDto schedule)
    {
        // 这里需要根据 ResolutionData 的实际结构进行验证
        // 暂时返回有效
        return await Task.FromResult<(bool, string?)>((true, null));
    }

    /// <summary>
    /// 验证调整时间方案
    /// </summary>
    private async Task<(bool IsValid, string? Reason)> ValidateAdjustTimeAsync(
        ConflictResolutionOption option,
        ScheduleDto schedule)
    {
        return await Task.FromResult<(bool, string?)>((true, null));
    }

    /// <summary>
    /// 验证重新分配方案
    /// </summary>
    private async Task<(bool IsValid, string? Reason)> ValidateReassignShiftsAsync(
        ConflictResolutionOption option,
        ScheduleDto schedule)
    {
        return await Task.FromResult<(bool, string?)>((true, null));
    }

    /// <summary>
    /// 应用替换人员方案
    /// </summary>
    private async Task<ScheduleDto> ApplyReplacePersonnelAsync(
        ConflictResolutionOption option,
        ScheduleDto schedule)
    {
        if (option.ResolutionData == null)
        {
            throw new InvalidOperationException("修复方案数据不能为空");
        }

        // 解析方案数据
        dynamic data = option.ResolutionData;

        // 处理未分配时段的情况（需要创建新班次）
        if (data.GetType().GetProperty("ShiftId") == null)
        {
            // 创建新班次
            var newShift = new ShiftDto
            {
                Id = schedule.Shifts.Any() ? schedule.Shifts.Max(s => s.Id) + 1 : 1,
                ScheduleId = schedule.Id,
                PositionId = (int)data.PositionId,
                PositionName = (string)data.PositionName,
                PersonnelId = (int)data.PersonnelId,
                PersonnelName = (string)data.PersonnelName,
                StartTime = (DateTime)data.StartTime,
                EndTime = data.EndTime != null ? (DateTime)data.EndTime : ((DateTime)data.StartTime).AddHours(2),
                PeriodIndex = (int)data.PeriodIndex,
                IsManualAssignment = false
            };

            schedule.Shifts.Add(newShift);
        }
        else
        {
            // 替换现有班次的人员
            int shiftId = (int)data.ShiftId;
            var shift = schedule.Shifts.FirstOrDefault(s => s.Id == shiftId);

            if (shift == null)
            {
                throw new InvalidOperationException($"找不到ID为 {shiftId} 的班次");
            }

            shift.PersonnelId = (int)data.NewPersonnelId;
            shift.PersonnelName = (string)data.NewPersonnelName;
        }

        return await Task.FromResult(schedule);
    }

    /// <summary>
    /// 应用取消分配方案
    /// </summary>
    private async Task<ScheduleDto> ApplyRemoveAssignmentAsync(
        ConflictResolutionOption option,
        ScheduleDto schedule)
    {
        if (option.ResolutionData == null)
        {
            throw new InvalidOperationException("修复方案数据不能为空");
        }

        // 解析方案数据
        dynamic data = option.ResolutionData;
        int shiftId = (int)data.ShiftId;

        // 移除班次
        var shift = schedule.Shifts.FirstOrDefault(s => s.Id == shiftId);
        if (shift != null)
        {
            schedule.Shifts.Remove(shift);
        }

        return await Task.FromResult(schedule);
    }

    /// <summary>
    /// 应用调整时间方案
    /// </summary>
    private async Task<ScheduleDto> ApplyAdjustTimeAsync(
        ConflictResolutionOption option,
        ScheduleDto schedule)
    {
        if (option.ResolutionData == null)
        {
            throw new InvalidOperationException("修复方案数据不能为空");
        }

        // 解析方案数据
        dynamic data = option.ResolutionData;
        int shiftId = (int)data.ShiftId;
        DateTime newStartTime = (DateTime)data.NewStartTime;
        DateTime newEndTime = (DateTime)data.NewEndTime;
        int newPeriodIndex = (int)data.NewPeriodIndex;

        // 调整班次时间
        var shift = schedule.Shifts.FirstOrDefault(s => s.Id == shiftId);
        if (shift == null)
        {
            throw new InvalidOperationException($"找不到ID为 {shiftId} 的班次");
        }

        shift.StartTime = newStartTime;
        shift.EndTime = newEndTime;
        shift.PeriodIndex = newPeriodIndex;

        return await Task.FromResult(schedule);
    }

    /// <summary>
    /// 应用重新分配方案
    /// </summary>
    private async Task<ScheduleDto> ApplyReassignShiftsAsync(
        ConflictResolutionOption option,
        ScheduleDto schedule)
    {
        if (option.ResolutionData == null)
        {
            throw new InvalidOperationException("修复方案数据不能为空");
        }

        // 解析方案数据
        dynamic data = option.ResolutionData;
        List<int> shiftIds = ((IEnumerable<dynamic>)data.ShiftIds).Cast<int>().ToList();
        int newPersonnelId = (int)data.NewPersonnelId;
        string newPersonnelName = (string)data.NewPersonnelName;

        // 重新分配班次
        foreach (var shiftId in shiftIds)
        {
            var shift = schedule.Shifts.FirstOrDefault(s => s.Id == shiftId);
            if (shift != null)
            {
                shift.PersonnelId = newPersonnelId;
                shift.PersonnelName = newPersonnelName;
            }
        }

        return await Task.FromResult(schedule);
    }

    /// <summary>
    /// 生成技能不匹配修复方案
    /// </summary>
    private async Task<List<ConflictResolutionOption>> GenerateSkillMismatchResolutionsAsync(
        ConflictDto conflict,
        ScheduleDto schedule)
    {
        var options = new List<ConflictResolutionOption>();

        // 获取该时段的班次
        if (conflict.RelatedShiftIds.Count == 0)
        {
            return options;
        }

        var shift = schedule.Shifts.FirstOrDefault(s => s.Id == conflict.RelatedShiftIds[0]);
        if (shift == null)
        {
            return options;
        }

        // 获取哨位信息
        var position = await _positionService.GetByIdAsync(shift.PositionId);
        if (position == null)
        {
            return options;
        }

        // 获取所有人员
        var allPersonnel = await _personnelService.GetAllAsync();

        // 查找具有匹配技能的可用人员
        var suitablePersonnel = allPersonnel
            .Where(p => p.Id != shift.PersonnelId) // 排除当前人员
            .Where(p => p.IsAvailable && !p.IsRetired) // 可用且未退役
            .Where(p => position.RequiredSkillIds.All(skillId => p.SkillIds.Contains(skillId))) // 技能匹配
            .Where(p => IsPersonnelAvailableForShift(p.Id, shift, schedule)) // 时间可用
            .OrderBy(p => GetPersonnelWorkload(p.Id, schedule)) // 按工作量排序
            .ToList();

        // 为每个合适的人员生成替换方案
        foreach (var personnel in suitablePersonnel.Take(3)) // 最多3个方案
        {
            var workload = GetPersonnelWorkload(personnel.Id, schedule);
            var impact = await EvaluateReplacementImpactAsync(shift, personnel.Id, schedule);

            var pros = new List<string> { "技能完全匹配", $"当前工作量: {workload} 班次" };
            if (impact.NewConflicts == 0)
            {
                pros.Add("无时间冲突");
            }

            var cons = new List<string>();
            if (impact.NewConflicts > 0)
            {
                cons.Add($"可能产生 {impact.NewConflicts} 个新冲突");
            }

            options.Add(new ConflictResolutionOption
            {
                Title = $"替换为 {personnel.Name}",
                Description = $"将此班次分配给 {personnel.Name}",
                Type = ResolutionType.ReplacePersonnel,
                IsRecommended = options.Count == 0, // 第一个方案为推荐
                Pros = pros,
                Cons = cons,
                Impact = impact.Description,
                ExpectedNewConflicts = impact.NewConflicts,
                ResolutionData = new { ShiftId = shift.Id, NewPersonnelId = personnel.Id, NewPersonnelName = personnel.Name }
            });
        }

        // 添加取消分配方案
        options.Add(new ConflictResolutionOption
        {
            Title = "取消此班次分配",
            Description = "移除此班次的人员分配",
            Type = ResolutionType.RemoveAssignment,
            IsRecommended = false,
            Pros = new List<string> { "立即解决冲突" },
            Cons = new List<string> { "哨位在此时段将无人值守" },
            Impact = "哨位覆盖率降低",
            ExpectedNewConflicts = 1, // 会产生未分配时段
            ResolutionData = new { ShiftId = shift.Id }
        });

        return options;
    }

    /// <summary>
    /// 检查人员在指定班次时间是否可用
    /// </summary>
    private bool IsPersonnelAvailableForShift(int personnelId, ShiftDto shift, ScheduleDto schedule)
    {
        // 检查该人员在同一时段是否已有其他班次
        return !schedule.Shifts.Any(s =>
            s.PersonnelId == personnelId &&
            s.StartTime == shift.StartTime &&
            s.PeriodIndex == shift.PeriodIndex &&
            s.Id != shift.Id);
    }

    /// <summary>
    /// 获取人员的工作量（班次数量）
    /// </summary>
    private int GetPersonnelWorkload(int personnelId, ScheduleDto schedule)
    {
        return schedule.Shifts.Count(s => s.PersonnelId == personnelId);
    }

    /// <summary>
    /// 评估替换人员的影响
    /// </summary>
    private async Task<ResolutionImpact> EvaluateReplacementImpactAsync(ShiftDto shift, int newPersonnelId, ScheduleDto schedule)
    {
        // 创建临时排班副本
        var tempSchedule = CloneSchedule(schedule);

        // 找到对应的班次并替换人员
        var tempShift = tempSchedule.Shifts.FirstOrDefault(s => s.Id == shift.Id);
        if (tempShift != null)
        {
            var personnel = await _personnelService.GetByIdAsync(newPersonnelId);
            tempShift.PersonnelId = newPersonnelId;
            tempShift.PersonnelName = personnel?.Name ?? $"人员{newPersonnelId}";
        }

        // 检测新的冲突
        var newConflicts = await _conflictDetectionService.DetectConflictsAsync(tempSchedule);
        var originalConflicts = await _conflictDetectionService.DetectConflictsAsync(schedule);

        // 计算新产生的冲突数量
        var newConflictCount = newConflicts.Count(c =>
            !originalConflicts.Any(oc => oc.Id == c.Id));

        return new ResolutionImpact
        {
            ResolvedConflicts = 1,
            NewConflicts = newConflictCount,
            AffectedPersonnelIds = new List<int> { newPersonnelId, shift.PersonnelId },
            AffectedPositionIds = new List<int> { shift.PositionId },
            Description = newConflictCount == 0 ? "无其他冲突" : $"可能产生 {newConflictCount} 个新冲突"
        };
    }

    /// <summary>
    /// 生成工作量不均衡修复方案
    /// </summary>
    private async Task<List<ConflictResolutionOption>> GenerateWorkloadImbalanceResolutionsAsync(
        ConflictDto conflict,
        ScheduleDto schedule)
    {
        var options = new List<ConflictResolutionOption>();

        if (conflict.PersonnelId == null)
        {
            return options;
        }

        // 获取工作量过高的人员的班次
        var overloadedPersonnelId = conflict.PersonnelId.Value;
        var overloadedShifts = schedule.Shifts
            .Where(s => s.PersonnelId == overloadedPersonnelId)
            .OrderBy(s => s.StartTime)
            .ToList();

        // 计算平均工作量
        var workloadByPersonnel = schedule.Shifts
            .GroupBy(s => s.PersonnelId)
            .Select(g => new { PersonnelId = g.Key, ShiftCount = g.Count() })
            .ToList();

        var averageWorkload = workloadByPersonnel.Average(w => w.ShiftCount);

        // 获取所有人员
        var allPersonnel = await _personnelService.GetAllAsync();

        // 查找工作量较低的人员
        var underloadedPersonnel = workloadByPersonnel
            .Where(w => w.PersonnelId != overloadedPersonnelId)
            .Where(w => w.ShiftCount < averageWorkload)
            .OrderBy(w => w.ShiftCount)
            .Take(5)
            .ToList();

        // 为每个工作量较低的人员生成重新分配方案
        foreach (var underloaded in underloadedPersonnel)
        {
            var personnel = allPersonnel.FirstOrDefault(p => p.Id == underloaded.PersonnelId);
            if (personnel == null || !personnel.IsAvailable || personnel.IsRetired)
            {
                continue;
            }

            // 找到可以转移的班次（技能匹配且无时间冲突）
            var transferableShifts = new List<ShiftDto>();
            foreach (var shift in overloadedShifts)
            {
                var position = await _positionService.GetByIdAsync(shift.PositionId);
                if (position == null)
                {
                    continue;
                }

                // 检查技能匹配
                var hasRequiredSkills = position.RequiredSkillIds.All(skillId => personnel.SkillIds.Contains(skillId));
                if (!hasRequiredSkills)
                {
                    continue;
                }

                // 检查时间冲突
                var hasTimeConflict = schedule.Shifts.Any(s =>
                    s.PersonnelId == personnel.Id &&
                    s.StartTime == shift.StartTime &&
                    s.PeriodIndex == shift.PeriodIndex);

                if (!hasTimeConflict)
                {
                    transferableShifts.Add(shift);
                }
            }

            if (transferableShifts.Count > 0)
            {
                // 建议转移1-2个班次
                var shiftsToTransfer = transferableShifts.Take(2).ToList();
                var shiftDescriptions = string.Join("、", shiftsToTransfer.Select(s =>
                    $"{s.PositionName} ({s.StartTime:MM-dd HH:mm})"));

                options.Add(new ConflictResolutionOption
                {
                    Title = $"转移班次给 {personnel.Name}",
                    Description = $"将以下班次转移给 {personnel.Name}：{shiftDescriptions}",
                    Type = ResolutionType.ReassignShifts,
                    IsRecommended = options.Count == 0,
                    Pros = new List<string>
                    {
                        $"{personnel.Name} 当前工作量: {underloaded.ShiftCount} 班次（低于平均值）",
                        "技能匹配",
                        "无时间冲突"
                    },
                    Cons = new List<string>(),
                    Impact = $"平衡工作量分配，减少 {conflict.PersonnelName} 的负担",
                    ExpectedNewConflicts = 0,
                    ResolutionData = new
                    {
                        ShiftIds = shiftsToTransfer.Select(s => s.Id).ToList(),
                        NewPersonnelId = personnel.Id,
                        NewPersonnelName = personnel.Name
                    }
                });
            }
        }

        // 如果没有找到合适的方案，添加手动调整建议
        if (options.Count == 0)
        {
            options.Add(new ConflictResolutionOption
            {
                Title = "手动调整工作量",
                Description = "建议手动重新分配部分班次以平衡工作量",
                Type = ResolutionType.ManualFix,
                IsRecommended = false,
                Pros = new List<string> { "灵活调整" },
                Cons = new List<string> { "需要手动操作" },
                Impact = "需要人工介入",
                ExpectedNewConflicts = 0,
                ResolutionData = null
            });
        }

        return options;
    }

    /// <summary>
    /// 生成未分配时段修复方案
    /// </summary>
    private async Task<List<ConflictResolutionOption>> GenerateUnassignedSlotResolutionsAsync(
        ConflictDto conflict,
        ScheduleDto schedule)
    {
        var options = new List<ConflictResolutionOption>();

        if (conflict.PositionId == null || conflict.StartTime == null || conflict.PeriodIndex == null)
        {
            return options;
        }

        // 获取哨位信息
        var position = await _positionService.GetByIdAsync(conflict.PositionId.Value);
        if (position == null)
        {
            return options;
        }

        // 获取所有人员
        var allPersonnel = await _personnelService.GetAllAsync();

        // 查找可用且技能匹配的人员
        var suitablePersonnel = allPersonnel
            .Where(p => p.IsAvailable && !p.IsRetired) // 可用且未退役
            .Where(p => position.RequiredSkillIds.All(skillId => p.SkillIds.Contains(skillId))) // 技能匹配
            .Where(p => !schedule.Shifts.Any(s => // 无时间冲突
                s.PersonnelId == p.Id &&
                s.StartTime == conflict.StartTime &&
                s.PeriodIndex == conflict.PeriodIndex))
            .ToList();

        // 按工作量和休息时间排序
        var personnelWithScores = new List<(PersonnelDto Personnel, int Workload, double RestScore)>();

        foreach (var personnel in suitablePersonnel)
        {
            var workload = GetPersonnelWorkload(personnel.Id, schedule);

            // 计算休息时间评分（检查前后班次的间隔）
            var restScore = CalculateRestScore(personnel.Id, conflict.StartTime.Value, schedule);

            personnelWithScores.Add((personnel, workload, restScore));
        }

        // 排序：优先选择工作量低、休息时间充足的人员
        var sortedPersonnel = personnelWithScores
            .OrderBy(p => p.Workload)
            .ThenByDescending(p => p.RestScore)
            .Take(5)
            .ToList();

        // 为每个合适的人员生成分配方案
        foreach (var (personnel, workload, restScore) in sortedPersonnel)
        {
            var pros = new List<string>
            {
                "技能完全匹配",
                $"当前工作量: {workload} 班次"
            };

            if (restScore >= 8)
            {
                pros.Add("休息时间充足");
            }

            var cons = new List<string>();
            if (restScore < 8)
            {
                cons.Add($"休息时间可能不足（约 {restScore:F1} 小时）");
            }

            options.Add(new ConflictResolutionOption
            {
                Title = $"分配给 {personnel.Name}",
                Description = $"将此时段分配给 {personnel.Name}",
                Type = ResolutionType.ReplacePersonnel,
                IsRecommended = options.Count == 0,
                Pros = pros,
                Cons = cons,
                Impact = cons.Count > 0 ? "可能产生休息时间不足的软约束冲突" : "无明显影响",
                ExpectedNewConflicts = cons.Count,
                ResolutionData = new
                {
                    PositionId = conflict.PositionId.Value,
                    PositionName = conflict.PositionName,
                    PersonnelId = personnel.Id,
                    PersonnelName = personnel.Name,
                    StartTime = conflict.StartTime.Value,
                    EndTime = conflict.EndTime,
                    PeriodIndex = conflict.PeriodIndex.Value
                }
            });
        }

        // 如果没有找到合适的人员
        if (options.Count == 0)
        {
            options.Add(new ConflictResolutionOption
            {
                Title = "暂时保持未分配",
                Description = "当前没有合适的人员可以分配到此时段",
                Type = ResolutionType.ManualFix,
                IsRecommended = false,
                Pros = new List<string>(),
                Cons = new List<string> { "哨位在此时段无人值守" },
                Impact = "需要调整其他约束或增加可用人员",
                ExpectedNewConflicts = 0,
                ResolutionData = null
            });
        }

        return options;
    }

    /// <summary>
    /// 计算休息时间评分
    /// </summary>
    private double CalculateRestScore(int personnelId, DateTime targetTime, ScheduleDto schedule)
    {
        var personnelShifts = schedule.Shifts
            .Where(s => s.PersonnelId == personnelId)
            .OrderBy(s => s.StartTime)
            .ToList();

        if (personnelShifts.Count == 0)
        {
            return 24; // 无班次，休息时间充足
        }

        // 查找目标时间前后的班次
        var beforeShift = personnelShifts.LastOrDefault(s => s.EndTime <= targetTime);
        var afterShift = personnelShifts.FirstOrDefault(s => s.StartTime >= targetTime.AddHours(2));

        double minRestHours = 24;

        if (beforeShift != null)
        {
            var restBefore = (targetTime - beforeShift.EndTime).TotalHours;
            minRestHours = Math.Min(minRestHours, restBefore);
        }

        if (afterShift != null)
        {
            var restAfter = (afterShift.StartTime - targetTime.AddHours(2)).TotalHours;
            minRestHours = Math.Min(minRestHours, restAfter);
        }

        return minRestHours;
    }

    /// <summary>
    /// 生成休息时间不足修复方案
    /// </summary>
    private async Task<List<ConflictResolutionOption>> GenerateInsufficientRestResolutionsAsync(
        ConflictDto conflict,
        ScheduleDto schedule)
    {
        var options = new List<ConflictResolutionOption>();

        if (conflict.PersonnelId == null || conflict.RelatedShiftIds.Count < 2)
        {
            return options;
        }

        // 获取相关的两个班次
        var shift1 = schedule.Shifts.FirstOrDefault(s => s.Id == conflict.RelatedShiftIds[0]);
        var shift2 = schedule.Shifts.FirstOrDefault(s => s.Id == conflict.RelatedShiftIds[1]);

        if (shift1 == null || shift2 == null)
        {
            return options;
        }

        // 确保 shift1 是较早的班次
        if (shift1.StartTime > shift2.StartTime)
        {
            (shift1, shift2) = (shift2, shift1);
        }

        // 获取所有人员
        var allPersonnel = await _personnelService.GetAllAsync();

        // 方案1: 替换第二个班次的人员
        var replacementOptions = await GenerateReplacementOptionsForShift(shift2, schedule, allPersonnel);
        foreach (var option in replacementOptions.Take(2))
        {
            options.Add(option);
        }

        // 方案2: 替换第一个班次的人员
        var replacementOptions2 = await GenerateReplacementOptionsForShift(shift1, schedule, allPersonnel);
        foreach (var option in replacementOptions2.Take(1))
        {
            options.Add(option);
        }

        // 方案3: 取消其中一个班次
        options.Add(new ConflictResolutionOption
        {
            Title = $"取消 {shift2.PositionName} 的班次",
            Description = $"取消 {shift2.StartTime:yyyy-MM-dd HH:mm} 的班次分配",
            Type = ResolutionType.RemoveAssignment,
            IsRecommended = false,
            Pros = new List<string> { "立即解决休息时间冲突" },
            Cons = new List<string> { "哨位在此时段将无人值守" },
            Impact = "哨位覆盖率降低",
            ExpectedNewConflicts = 1,
            ResolutionData = new { ShiftId = shift2.Id }
        });

        return options;
    }

    /// <summary>
    /// 为指定班次生成替换人员方案
    /// </summary>
    private async Task<List<ConflictResolutionOption>> GenerateReplacementOptionsForShift(
        ShiftDto shift,
        ScheduleDto schedule,
        List<PersonnelDto> allPersonnel)
    {
        var options = new List<ConflictResolutionOption>();

        // 获取哨位信息
        var position = await _positionService.GetByIdAsync(shift.PositionId);
        if (position == null)
        {
            return options;
        }

        // 查找合适的替换人员
        var suitablePersonnel = allPersonnel
            .Where(p => p.Id != shift.PersonnelId)
            .Where(p => p.IsAvailable && !p.IsRetired)
            .Where(p => position.RequiredSkillIds.All(skillId => p.SkillIds.Contains(skillId)))
            .Where(p => IsPersonnelAvailableForShift(p.Id, shift, schedule))
            .OrderBy(p => GetPersonnelWorkload(p.Id, schedule))
            .Take(2)
            .ToList();

        foreach (var personnel in suitablePersonnel)
        {
            var workload = GetPersonnelWorkload(personnel.Id, schedule);
            var restScore = CalculateRestScore(personnel.Id, shift.StartTime, schedule);

            var pros = new List<string>
            {
                "技能匹配",
                $"当前工作量: {workload} 班次"
            };

            if (restScore >= 8)
            {
                pros.Add("休息时间充足");
            }

            options.Add(new ConflictResolutionOption
            {
                Title = $"替换为 {personnel.Name}",
                Description = $"将 {shift.PositionName} ({shift.StartTime:yyyy-MM-dd HH:mm}) 的班次改为 {personnel.Name}",
                Type = ResolutionType.ReplacePersonnel,
                IsRecommended = options.Count == 0,
                Pros = pros,
                Cons = restScore < 8 ? new List<string> { "可能产生新的休息时间冲突" } : new List<string>(),
                Impact = restScore < 8 ? "可能产生新的软约束冲突" : "解决休息时间冲突",
                ExpectedNewConflicts = restScore < 8 ? 1 : 0,
                ResolutionData = new
                {
                    ShiftId = shift.Id,
                    NewPersonnelId = personnel.Id,
                    NewPersonnelName = personnel.Name
                }
            });
        }

        return options;
    }

    #endregion
}
