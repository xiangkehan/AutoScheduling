using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.ImportExport;

namespace AutoScheduling3.TestData.Validation;

/// <summary>
/// 测试数据验证器
/// 负责验证生成的测试数据的完整性和正确性
/// </summary>
public class TestDataValidator
{
    /// <summary>
    /// 验证生成的数据
    /// </summary>
    /// <param name="data">要验证的数据</param>
    /// <exception cref="InvalidOperationException">当数据验证失败时抛出</exception>
    public void Validate(ExportData data)
    {
        var errors = new List<string>();

        // 验证数据不为空
        if (data.Skills == null || data.Skills.Count == 0)
            errors.Add("技能数据为空");

        if (data.Personnel == null || data.Personnel.Count == 0)
            errors.Add("人员数据为空");

        if (data.Positions == null || data.Positions.Count == 0)
            errors.Add("哨位数据为空");

        if (data.HolidayConfigs == null || data.HolidayConfigs.Count == 0)
            errors.Add("节假日配置数据为空");

        if (data.Templates == null || data.Templates.Count == 0)
            errors.Add("排班模板数据为空");

        // 如果基础数据为空，无法继续验证
        if (errors.Count > 0)
        {
            throw new InvalidOperationException("数据验证失败:\n" + string.Join("\n", errors));
        }

        // 创建ID集合用于引用验证
        var skillIds = new HashSet<int>(data.Skills.Select(s => s.Id));
        var personnelIds = new HashSet<int>(data.Personnel.Select(p => p.Id));
        var positionIds = new HashSet<int>(data.Positions.Select(p => p.Id));
        var holidayConfigIds = new HashSet<int>(data.HolidayConfigs.Select(h => h.Id));

        // 验证技能数据
        ValidateSkills(data.Skills, errors);

        // 验证人员数据
        ValidatePersonnel(data.Personnel, skillIds, errors);

        // 验证哨位数据
        ValidatePositions(data.Positions, skillIds, personnelIds, errors);

        // 验证节假日配置
        ValidateHolidayConfigs(data.HolidayConfigs, errors);

        // 验证排班模板
        ValidateTemplates(data.Templates, personnelIds, positionIds, holidayConfigIds, errors);

        // 验证定岗规则
        if (data.FixedAssignments != null && data.FixedAssignments.Count > 0)
        {
            ValidateFixedAssignments(data.FixedAssignments, personnelIds, positionIds, errors);
        }

        // 验证手动指定
        if (data.ManualAssignments != null && data.ManualAssignments.Count > 0)
        {
            ValidateManualAssignments(data.ManualAssignments, personnelIds, positionIds, errors);
        }

        // 如果有错误，抛出异常
        if (errors.Count > 0)
        {
            var errorMessage = "数据验证失败，发现 " + errors.Count + " 个错误:\n" +
                             string.Join("\n", errors.Select((e, i) => $"  {i + 1}. {e}"));
            throw new InvalidOperationException(errorMessage);
        }
    }

    /// <summary>
    /// 验证技能数据
    /// </summary>
    private void ValidateSkills(List<SkillDto> skills, List<string> errors)
    {
        var nameSet = new HashSet<string>();

        foreach (var skill in skills)
        {
            // 验证ID
            if (skill.Id <= 0)
                errors.Add($"技能ID必须大于0，当前值: {skill.Id}");

            // 验证名称
            if (string.IsNullOrWhiteSpace(skill.Name))
                errors.Add($"技能 (ID: {skill.Id}) 的名称不能为空");
            else if (nameSet.Contains(skill.Name))
                errors.Add($"技能名称重复: {skill.Name}");
            else
                nameSet.Add(skill.Name);

            // 验证描述
            if (string.IsNullOrWhiteSpace(skill.Description))
                errors.Add($"技能 {skill.Name} (ID: {skill.Id}) 的描述不能为空");

            // 验证时间戳
            if (skill.CreatedAt > DateTime.UtcNow)
                errors.Add($"技能 {skill.Name} 的创建时间不能是未来时间");

            if (skill.UpdatedAt > DateTime.UtcNow)
                errors.Add($"技能 {skill.Name} 的更新时间不能是未来时间");

            if (skill.UpdatedAt < skill.CreatedAt)
                errors.Add($"技能 {skill.Name} 的更新时间不能早于创建时间");
        }
    }

    /// <summary>
    /// 验证人员数据
    /// </summary>
    private void ValidatePersonnel(List<PersonnelDto> personnel, HashSet<int> skillIds, List<string> errors)
    {
        var nameSet = new HashSet<string>();

        foreach (var person in personnel)
        {
            // 验证ID
            if (person.Id <= 0)
                errors.Add($"人员ID必须大于0，当前值: {person.Id}");

            // 验证名称
            if (string.IsNullOrWhiteSpace(person.Name))
                errors.Add($"人员 (ID: {person.Id}) 的名称不能为空");
            else if (nameSet.Contains(person.Name))
                errors.Add($"人员名称重复: {person.Name}");
            else
                nameSet.Add(person.Name);

            // 验证技能引用
            if (person.SkillIds == null || person.SkillIds.Count == 0)
                errors.Add($"人员 {person.Name} 必须至少拥有一个技能");
            else
            {
                foreach (var skillId in person.SkillIds)
                {
                    if (!skillIds.Contains(skillId))
                        errors.Add($"人员 {person.Name} 引用了不存在的技能ID: {skillId}");
                }
            }

            // 验证技能名称列表与ID列表一致
            if (person.SkillNames == null || person.SkillNames.Count != person.SkillIds.Count)
                errors.Add($"人员 {person.Name} 的技能名称列表与技能ID列表数量不一致");

            // 验证班次间隔数组
            if (person.RecentPeriodShiftIntervals == null || person.RecentPeriodShiftIntervals.Length != 12)
                errors.Add($"人员 {person.Name} 的时段班次间隔数组必须包含12个元素");

            // 验证数值范围
            if (person.RecentShiftIntervalCount < 0)
                errors.Add($"人员 {person.Name} 的班次间隔计数不能为负数");

            if (person.RecentHolidayShiftIntervalCount < 0)
                errors.Add($"人员 {person.Name} 的节假日班次间隔计数不能为负数");
        }
    }

    /// <summary>
    /// 验证哨位数据
    /// </summary>
    private void ValidatePositions(List<PositionDto> positions, HashSet<int> skillIds, 
        HashSet<int> personnelIds, List<string> errors)
    {
        var nameSet = new HashSet<string>();

        foreach (var position in positions)
        {
            // 验证ID
            if (position.Id <= 0)
                errors.Add($"哨位ID必须大于0，当前值: {position.Id}");

            // 验证名称
            if (string.IsNullOrWhiteSpace(position.Name))
                errors.Add($"哨位 (ID: {position.Id}) 的名称不能为空");
            else if (nameSet.Contains(position.Name))
                errors.Add($"哨位名称重复: {position.Name}");
            else
                nameSet.Add(position.Name);

            // 验证地点
            if (string.IsNullOrWhiteSpace(position.Location))
                errors.Add($"哨位 {position.Name} 的地点不能为空");

            // 验证所需技能引用
            if (position.RequiredSkillIds == null || position.RequiredSkillIds.Count == 0)
                errors.Add($"哨位 {position.Name} 必须至少需要一个技能");
            else
            {
                foreach (var skillId in position.RequiredSkillIds)
                {
                    if (!skillIds.Contains(skillId))
                        errors.Add($"哨位 {position.Name} 引用了不存在的技能ID: {skillId}");
                }
            }

            // 验证技能名称列表与ID列表一致
            if (position.RequiredSkillNames == null || 
                position.RequiredSkillNames.Count != position.RequiredSkillIds.Count)
                errors.Add($"哨位 {position.Name} 的技能名称列表与技能ID列表数量不一致");

            // 验证可用人员引用
            if (position.AvailablePersonnelIds != null)
            {
                foreach (var personId in position.AvailablePersonnelIds)
                {
                    if (!personnelIds.Contains(personId))
                        errors.Add($"哨位 {position.Name} 引用了不存在的人员ID: {personId}");
                }

                // 验证人员名称列表与ID列表一致
                if (position.AvailablePersonnelNames == null || 
                    position.AvailablePersonnelNames.Count != position.AvailablePersonnelIds.Count)
                    errors.Add($"哨位 {position.Name} 的人员名称列表与人员ID列表数量不一致");
            }

            // 验证时间戳
            if (position.CreatedAt > DateTime.UtcNow)
                errors.Add($"哨位 {position.Name} 的创建时间不能是未来时间");

            if (position.UpdatedAt > DateTime.UtcNow)
                errors.Add($"哨位 {position.Name} 的更新时间不能是未来时间");
        }
    }

    /// <summary>
    /// 验证节假日配置
    /// </summary>
    private void ValidateHolidayConfigs(List<HolidayConfigDto> configs, List<string> errors)
    {
        var nameSet = new HashSet<string>();
        var activeCount = 0;

        foreach (var config in configs)
        {
            // 验证ID
            if (config.Id <= 0)
                errors.Add($"节假日配置ID必须大于0，当前值: {config.Id}");

            // 验证名称
            if (string.IsNullOrWhiteSpace(config.ConfigName))
                errors.Add($"节假日配置 (ID: {config.Id}) 的名称不能为空");
            else if (nameSet.Contains(config.ConfigName))
                errors.Add($"节假日配置名称重复: {config.ConfigName}");
            else
                nameSet.Add(config.ConfigName);

            // 验证周末规则
            if (config.EnableWeekendRule && (config.WeekendDays == null || config.WeekendDays.Count == 0))
                errors.Add($"节假日配置 {config.ConfigName} 启用了周末规则但未指定周末日期");

            // 统计激活的配置
            if (config.IsActive)
                activeCount++;

            // 验证时间戳
            if (config.CreatedAt > DateTime.UtcNow)
                errors.Add($"节假日配置 {config.ConfigName} 的创建时间不能是未来时间");

            if (config.UpdatedAt > DateTime.UtcNow)
                errors.Add($"节假日配置 {config.ConfigName} 的更新时间不能是未来时间");
        }

        // 验证至少有一个激活的配置
        if (activeCount == 0)
            errors.Add("至少需要一个激活的节假日配置");

        // 验证不能有多个激活的配置
        if (activeCount > 1)
            errors.Add($"只能有一个激活的节假日配置，当前有 {activeCount} 个");
    }

    /// <summary>
    /// 验证排班模板
    /// </summary>
    private void ValidateTemplates(List<SchedulingTemplateDto> templates, HashSet<int> personnelIds,
        HashSet<int> positionIds, HashSet<int> holidayConfigIds, List<string> errors)
    {
        var nameSet = new HashSet<string>();
        var defaultCount = 0;

        foreach (var template in templates)
        {
            // 验证ID
            if (template.Id <= 0)
                errors.Add($"排班模板ID必须大于0，当前值: {template.Id}");

            // 验证名称
            if (string.IsNullOrWhiteSpace(template.Name))
                errors.Add($"排班模板 (ID: {template.Id}) 的名称不能为空");
            else if (nameSet.Contains(template.Name))
                errors.Add($"排班模板名称重复: {template.Name}");
            else
                nameSet.Add(template.Name);

            // 验证模板类型
            var validTypes = new[] { "regular", "holiday", "special" };
            if (!validTypes.Contains(template.TemplateType))
                errors.Add($"排班模板 {template.Name} 的类型无效: {template.TemplateType}");

            // 验证人员引用
            if (template.PersonnelIds == null || template.PersonnelIds.Count == 0)
                errors.Add($"排班模板 {template.Name} 必须至少包含一个人员");
            else
            {
                foreach (var personId in template.PersonnelIds)
                {
                    if (!personnelIds.Contains(personId))
                        errors.Add($"排班模板 {template.Name} 引用了不存在的人员ID: {personId}");
                }
            }

            // 验证哨位引用
            if (template.PositionIds == null || template.PositionIds.Count == 0)
                errors.Add($"排班模板 {template.Name} 必须至少包含一个哨位");
            else
            {
                foreach (var positionId in template.PositionIds)
                {
                    if (!positionIds.Contains(positionId))
                        errors.Add($"排班模板 {template.Name} 引用了不存在的哨位ID: {positionId}");
                }
            }

            // 验证节假日配置引用
            if (template.HolidayConfigId.HasValue && !holidayConfigIds.Contains(template.HolidayConfigId.Value))
            {
                errors.Add($"排班模板 {template.Name} 引用了不存在的节假日配置ID: {template.HolidayConfigId}");
            }

            // 验证排班天数
            if (template.DurationDays <= 0)
                errors.Add($"排班模板 {template.Name} 的排班天数必须大于0");

            // 验证使用次数
            if (template.UsageCount < 0)
                errors.Add($"排班模板 {template.Name} 的使用次数不能为负数");

            // 统计默认模板
            if (template.IsDefault)
                defaultCount++;

            // 验证时间戳
            if (template.CreatedAt > DateTime.UtcNow)
                errors.Add($"排班模板 {template.Name} 的创建时间不能是未来时间");

            if (template.UpdatedAt > DateTime.UtcNow)
                errors.Add($"排班模板 {template.Name} 的更新时间不能是未来时间");

            if (template.LastUsedAt.HasValue && template.LastUsedAt.Value > DateTime.UtcNow)
                errors.Add($"排班模板 {template.Name} 的最后使用时间不能是未来时间");
        }

        // 验证至少有一个默认模板
        if (defaultCount == 0)
            errors.Add("至少需要一个默认排班模板");

        // 验证不能有多个默认模板
        if (defaultCount > 1)
            errors.Add($"只能有一个默认排班模板，当前有 {defaultCount} 个");
    }

    /// <summary>
    /// 验证定岗规则
    /// </summary>
    private void ValidateFixedAssignments(List<FixedAssignmentDto> assignments, 
        HashSet<int> personnelIds, HashSet<int> positionIds, List<string> errors)
    {
        foreach (var assignment in assignments)
        {
            // 验证ID
            if (assignment.Id <= 0)
                errors.Add($"定岗规则ID必须大于0，当前值: {assignment.Id}");

            // 验证人员引用
            if (!personnelIds.Contains(assignment.PersonnelId))
                errors.Add($"定岗规则 {assignment.RuleName} 引用了不存在的人员ID: {assignment.PersonnelId}");

            // 验证哨位引用
            if (assignment.AllowedPositionIds == null || assignment.AllowedPositionIds.Count == 0)
                errors.Add($"定岗规则 {assignment.RuleName} 必须至少包含一个允许的哨位");
            else
            {
                foreach (var positionId in assignment.AllowedPositionIds)
                {
                    if (!positionIds.Contains(positionId))
                        errors.Add($"定岗规则 {assignment.RuleName} 引用了不存在的哨位ID: {positionId}");
                }
            }

            // 验证哨位名称列表与ID列表一致
            if (assignment.AllowedPositionNames == null || 
                assignment.AllowedPositionNames.Count != assignment.AllowedPositionIds.Count)
                errors.Add($"定岗规则 {assignment.RuleName} 的哨位名称列表与哨位ID列表数量不一致");

            // 验证时段
            if (assignment.AllowedTimeSlots == null || assignment.AllowedTimeSlots.Count == 0)
                errors.Add($"定岗规则 {assignment.RuleName} 必须至少包含一个允许的时段");
            else
            {
                foreach (var timeSlot in assignment.AllowedTimeSlots)
                {
                    if (timeSlot < 0 || timeSlot > 11)
                        errors.Add($"定岗规则 {assignment.RuleName} 包含无效的时段索引: {timeSlot}");
                }
            }

            // 验证日期范围
            if (assignment.EndDate < assignment.StartDate)
                errors.Add($"定岗规则 {assignment.RuleName} 的结束日期早于开始日期");

            // 验证规则名称
            if (string.IsNullOrWhiteSpace(assignment.RuleName))
                errors.Add($"定岗规则 (ID: {assignment.Id}) 的名称不能为空");

            // 验证时间戳
            if (assignment.CreatedAt > DateTime.UtcNow)
                errors.Add($"定岗规则 {assignment.RuleName} 的创建时间不能是未来时间");

            if (assignment.UpdatedAt > DateTime.UtcNow)
                errors.Add($"定岗规则 {assignment.RuleName} 的更新时间不能是未来时间");
        }
    }

    /// <summary>
    /// 验证手动指定
    /// </summary>
    private void ValidateManualAssignments(List<ManualAssignmentDto> assignments,
        HashSet<int> personnelIds, HashSet<int> positionIds, List<string> errors)
    {
        var combinationSet = new HashSet<string>();

        foreach (var assignment in assignments)
        {
            // 验证ID
            if (assignment.Id <= 0)
                errors.Add($"手动指定ID必须大于0，当前值: {assignment.Id}");

            // 验证人员引用
            if (!personnelIds.Contains(assignment.PersonnelId))
                errors.Add($"手动指定 (ID: {assignment.Id}) 引用了不存在的人员ID: {assignment.PersonnelId}");

            // 验证哨位引用
            if (!positionIds.Contains(assignment.PositionId))
                errors.Add($"手动指定 (ID: {assignment.Id}) 引用了不存在的哨位ID: {assignment.PositionId}");

            // 验证时段范围
            if (assignment.TimeSlot < 0 || assignment.TimeSlot > 11)
                errors.Add($"手动指定 (ID: {assignment.Id}) 包含无效的时段索引: {assignment.TimeSlot}");

            // 验证日期
            if (assignment.Date == default)
                errors.Add($"手动指定 (ID: {assignment.Id}) 的日期无效");

            // 验证唯一性（同一哨位、日期、时段不能重复）
            var key = $"{assignment.PositionId}_{assignment.Date:yyyyMMdd}_{assignment.TimeSlot}";
            if (combinationSet.Contains(key))
                errors.Add($"手动指定存在重复：哨位ID {assignment.PositionId}，日期 {assignment.Date:yyyy-MM-dd}，时段 {assignment.TimeSlot}");
            else
                combinationSet.Add(key);

            // 验证时间戳
            if (assignment.CreatedAt > DateTime.UtcNow)
                errors.Add($"手动指定 (ID: {assignment.Id}) 的创建时间不能是未来时间");

            if (assignment.UpdatedAt > DateTime.UtcNow)
                errors.Add($"手动指定 (ID: {assignment.Id}) 的更新时间不能是未来时间");
        }
    }
}
