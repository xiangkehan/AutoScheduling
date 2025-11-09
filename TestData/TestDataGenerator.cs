using AutoScheduling3.DTOs;
using AutoScheduling3.DTOs.ImportExport;
using System.Text.Json;
using Windows.Storage;

namespace AutoScheduling3.TestData;

/// <summary>
/// 测试数据生成器主类
/// </summary>
public class TestDataGenerator
{
    private readonly TestDataConfiguration _config;
    private readonly SampleDataProvider _sampleData;
    private readonly Random _random;

    /// <summary>
    /// 使用默认配置创建测试数据生成器
    /// </summary>
    public TestDataGenerator() : this(TestDataConfiguration.CreateDefault())
    {
    }

    /// <summary>
    /// 使用指定配置创建测试数据生成器
    /// </summary>
    public TestDataGenerator(TestDataConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _config.Validate();

        _random = new Random(_config.RandomSeed);
        _sampleData = new SampleDataProvider(_random);
    }

    /// <summary>
    /// 生成完整的测试数据集
    /// </summary>
    public ExportData GenerateTestData()
    {
        // 按依赖顺序生成数据
        var skills = GenerateSkills();
        var personnel = GeneratePersonnel(skills);
        var positions = GeneratePositions(skills, personnel);
        var holidayConfigs = GenerateHolidayConfigs();
        var templates = GenerateTemplates(personnel, positions, holidayConfigs);
        var fixedAssignments = GenerateFixedAssignments(personnel, positions);
        var manualAssignments = GenerateManualAssignments(personnel, positions);

        // 创建导出数据对象
        var exportData = new ExportData
        {
            Skills = skills,
            Personnel = personnel,
            Positions = positions,
            HolidayConfigs = holidayConfigs,
            Templates = templates,
            FixedAssignments = fixedAssignments,
            ManualAssignments = manualAssignments
        };

        // 创建元数据
        exportData.Metadata = CreateMetadata(exportData);

        // 验证生成的数据
        ValidateGeneratedData(exportData);

        return exportData;
    }

    /// <summary>
    /// 导出测试数据到JSON文件（传统方式）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public async Task ExportToFileAsync(string filePath)
    {
        var exportData = GenerateTestData();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        var json = JsonSerializer.Serialize(exportData, options);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// 导出测试数据到StorageFile（WinUI3方式）
    /// </summary>
    /// <param name="file">StorageFile对象</param>
    public async Task ExportToStorageFileAsync(StorageFile file)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));

        var exportData = GenerateTestData();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        var json = JsonSerializer.Serialize(exportData, options);
        await FileIO.WriteTextAsync(file, json);
    }

    /// <summary>
    /// 生成测试数据并返回JSON字符串
    /// </summary>
    /// <returns>JSON格式的测试数据</returns>
    public string GenerateTestDataAsJson()
    {
        var exportData = GenerateTestData();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        return JsonSerializer.Serialize(exportData, options);
    }

    #region 私有生成方法

    /// <summary>
    /// 生成技能数据
    /// </summary>
    private List<SkillDto> GenerateSkills()
    {
        var skills = new List<SkillDto>();
        var availableNames = _sampleData.GetAllSkillNames();
        var usedNames = new HashSet<string>();

        for (int i = 1; i <= _config.SkillCount; i++)
        {
            string name;
            do
            {
                if (availableNames.Count == 0)
                {
                    // 如果用完了预定义的名称，生成新的
                    name = $"技能{i}";
                }
                else
                {
                    var index = _random.Next(availableNames.Count);
                    name = availableNames[index];
                    availableNames.RemoveAt(index);
                }
            } while (usedNames.Contains(name));

            usedNames.Add(name);

            skills.Add(new SkillDto
            {
                Id = i,
                Name = name,
                Description = $"{name}相关的专业技能",
                IsActive = _random.Next(100) < 90, // 90%激活
                CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(365)),
                UpdatedAt = DateTime.UtcNow.AddDays(-_random.Next(30))
            });
        }

        return skills;
    }

    /// <summary>
    /// 生成人员数据
    /// </summary>
    private List<PersonnelDto> GeneratePersonnel(List<SkillDto> skills)
    {
        var personnel = new List<PersonnelDto>();
        var availableNames = _sampleData.GetAllNames();
        var usedNames = new HashSet<string>();

        for (int i = 1; i <= _config.PersonnelCount; i++)
        {
            string name;
            do
            {
                if (availableNames.Count == 0)
                {
                    // 如果用完了预定义的名称，生成新的
                    name = $"人员{i}";
                }
                else
                {
                    var index = _random.Next(availableNames.Count);
                    name = availableNames[index];
                    availableNames.RemoveAt(index);
                }
            } while (usedNames.Contains(name));

            usedNames.Add(name);

            // 随机分配1-3个技能
            var skillCount = _random.Next(1, Math.Min(4, skills.Count + 1));
            var assignedSkills = skills
                .OrderBy(x => _random.Next())
                .Take(skillCount)
                .ToList();

            personnel.Add(new PersonnelDto
            {
                Id = i,
                Name = name,
                SkillIds = assignedSkills.Select(s => s.Id).ToList(),
                SkillNames = assignedSkills.Select(s => s.Name).ToList(),
                IsAvailable = _random.Next(100) < 85, // 85%可用
                IsRetired = _random.Next(100) < 10,   // 10%退役
                RecentShiftIntervalCount = _random.Next(0, 20),
                RecentHolidayShiftIntervalCount = _random.Next(0, 10),
                RecentPeriodShiftIntervals = Enumerable.Range(0, 12)
                    .Select(_ => _random.Next(0, 15))
                    .ToArray()
            });
        }

        return personnel;
    }

    /// <summary>
    /// 生成哨位数据
    /// </summary>
    private List<PositionDto> GeneratePositions(List<SkillDto> skills, List<PersonnelDto> personnel)
    {
        var positions = new List<PositionDto>();
        var availableNames = _sampleData.GetAllPositionNames();
        var availableLocations = _sampleData.GetAllLocations();
        var usedNames = new HashSet<string>();

        for (int i = 1; i <= _config.PositionCount; i++)
        {
            string name;
            do
            {
                if (availableNames.Count == 0)
                {
                    name = $"哨位{i}";
                }
                else
                {
                    var index = _random.Next(availableNames.Count);
                    name = availableNames[index];
                    availableNames.RemoveAt(index);
                }
            } while (usedNames.Contains(name));

            usedNames.Add(name);

            // 选择地点
            string location;
            if (availableLocations.Count > 0)
            {
                var locIndex = _random.Next(availableLocations.Count);
                location = availableLocations[locIndex];
                availableLocations.RemoveAt(locIndex);
            }
            else
            {
                location = $"位置{i}";
            }

            // 随机分配1-2个所需技能
            var requiredSkillCount = _random.Next(1, Math.Min(3, skills.Count + 1));
            var requiredSkills = skills
                .OrderBy(x => _random.Next())
                .Take(requiredSkillCount)
                .ToList();

            // 找出具备所需技能的人员
            var availablePersonnel = personnel
                .Where(p => p.IsAvailable && !p.IsRetired)
                .Where(p => requiredSkills.Any(rs => p.SkillIds.Contains(rs.Id)))
                .ToList();

            var skillNames = string.Join("、", requiredSkills.Select(s => s.Name));

            positions.Add(new PositionDto
            {
                Id = i,
                Name = name,
                Location = location,
                Description = _sampleData.GetRandomDescription(name),
                Requirements = _sampleData.GetRandomRequirement(skillNames),
                RequiredSkillIds = requiredSkills.Select(s => s.Id).ToList(),
                RequiredSkillNames = requiredSkills.Select(s => s.Name).ToList(),
                AvailablePersonnelIds = availablePersonnel.Select(p => p.Id).ToList(),
                AvailablePersonnelNames = availablePersonnel.Select(p => p.Name).ToList(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(180)),
                UpdatedAt = DateTime.UtcNow.AddDays(-_random.Next(30))
            });
        }

        return positions;
    }

    /// <summary>
    /// 生成节假日配置数据
    /// </summary>
    private List<HolidayConfigDto> GenerateHolidayConfigs()
    {
        var configs = new List<HolidayConfigDto>();
        var baseDate = DateTime.UtcNow;

        // 配置1: 标准周末配置
        configs.Add(new HolidayConfigDto
        {
            Id = 1,
            ConfigName = "标准周末配置",
            EnableWeekendRule = true,
            WeekendDays = new List<DayOfWeek>
            {
                DayOfWeek.Saturday,
                DayOfWeek.Sunday
            },
            LegalHolidays = new List<DateTime>
            {
                new DateTime(baseDate.Year, 1, 1),   // 元旦
                new DateTime(baseDate.Year, 5, 1),   // 劳动节
                new DateTime(baseDate.Year, 10, 1),  // 国庆节
            },
            CustomHolidays = new List<DateTime>(),
            ExcludedDates = new List<DateTime>(),
            IsActive = true,
            CreatedAt = baseDate.AddDays(-90),
            UpdatedAt = baseDate.AddDays(-10)
        });

        // 配置2: 单休配置
        if (_config.HolidayConfigCount > 1)
        {
            configs.Add(new HolidayConfigDto
            {
                Id = 2,
                ConfigName = "单休配置",
                EnableWeekendRule = true,
                WeekendDays = new List<DayOfWeek> { DayOfWeek.Sunday },
                LegalHolidays = new List<DateTime>
                {
                    new DateTime(baseDate.Year, 1, 1),
                    new DateTime(baseDate.Year, 5, 1),
                },
                CustomHolidays = new List<DateTime>
                {
                    baseDate.AddDays(15),
                    baseDate.AddDays(30),
                },
                ExcludedDates = new List<DateTime>(),
                IsActive = false,
                CreatedAt = baseDate.AddDays(-60),
                UpdatedAt = baseDate.AddDays(-5)
            });
        }

        // 配置3+: 额外的自定义配置
        for (int i = 3; i <= _config.HolidayConfigCount; i++)
        {
            configs.Add(new HolidayConfigDto
            {
                Id = i,
                ConfigName = $"自定义配置{i - 2}",
                EnableWeekendRule = _random.Next(100) < 80, // 80%启用周末规则
                WeekendDays = _random.Next(100) < 50
                    ? new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday }
                    : new List<DayOfWeek> { DayOfWeek.Sunday },
                LegalHolidays = new List<DateTime>
                {
                    new DateTime(baseDate.Year, 1, 1),
                },
                CustomHolidays = Enumerable.Range(1, _random.Next(2, 5))
                    .Select(_ => baseDate.AddDays(_random.Next(1, 90)))
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList(),
                ExcludedDates = new List<DateTime>(),
                IsActive = false,
                CreatedAt = baseDate.AddDays(-_random.Next(30, 90)),
                UpdatedAt = baseDate.AddDays(-_random.Next(1, 15))
            });
        }

        return configs;
    }

    /// <summary>
    /// 生成排班模板数据
    /// </summary>
    private List<SchedulingTemplateDto> GenerateTemplates(
        List<PersonnelDto> personnel,
        List<PositionDto> positions,
        List<HolidayConfigDto> holidayConfigs)
    {
        var templates = new List<SchedulingTemplateDto>();
        var templateTypes = new[] { "regular", "holiday", "special" };
        var baseDate = DateTime.UtcNow;

        for (int i = 1; i <= _config.TemplateCount; i++)
        {
            var type = templateTypes[Math.Min(i - 1, templateTypes.Length - 1)];

            // 选择至少5个可用人员（如果人员总数少于5，则选择所有可用人员）
            var availablePersonnel = personnel
                .Where(p => p.IsAvailable && !p.IsRetired)
                .ToList();

            var personnelToSelect = Math.Max(5, availablePersonnel.Count / 2);
            personnelToSelect = Math.Min(personnelToSelect, availablePersonnel.Count);

            var selectedPersonnel = availablePersonnel
                .OrderBy(x => _random.Next())
                .Take(personnelToSelect)
                .ToList();

            // 选择至少3个哨位（如果哨位总数少于3，则选择所有哨位）
            var positionsToSelect = Math.Max(3, positions.Count / 2);
            positionsToSelect = Math.Min(positionsToSelect, positions.Count);

            var selectedPositions = positions
                .OrderBy(x => _random.Next())
                .Take(positionsToSelect)
                .ToList();

            templates.Add(new SchedulingTemplateDto
            {
                Id = i,
                Name = $"{_sampleData.GetTemplateTypeName(type)}排班模板{i}",
                Description = $"用于{_sampleData.GetTemplateTypeName(type)}的排班模板",
                TemplateType = type,
                IsDefault = i == 1,
                PersonnelIds = selectedPersonnel.Select(p => p.Id).ToList(),
                PositionIds = selectedPositions.Select(p => p.Id).ToList(),
                HolidayConfigId = holidayConfigs.FirstOrDefault()?.Id,
                UseActiveHolidayConfig = true,
                EnabledFixedRuleIds = new List<int>(),
                EnabledManualAssignmentIds = new List<int>(),
                DurationDays = type == "regular" ? 7 : (type == "holiday" ? 3 : 1),
                StrategyConfig = "{}",
                UsageCount = _random.Next(0, 50),
                IsActive = true,
                CreatedAt = baseDate.AddDays(-_random.Next(120)),
                UpdatedAt = baseDate.AddDays(-_random.Next(20)),
                LastUsedAt = _random.Next(100) < 70
                    ? baseDate.AddDays(-_random.Next(10))
                    : null
            });
        }

        return templates;
    }

    /// <summary>
    /// 生成定岗规则数据
    /// </summary>
    private List<FixedAssignmentDto> GenerateFixedAssignments(
        List<PersonnelDto> personnel,
        List<PositionDto> positions)
    {
        var assignments = new List<FixedAssignmentDto>();
        var baseDate = DateTime.UtcNow;

        for (int i = 1; i <= _config.FixedAssignmentCount; i++)
        {
            var person = personnel[_random.Next(personnel.Count)];

            // 选择1-3个允许的哨位
            var allowedPositionCount = _random.Next(1, Math.Min(4, positions.Count + 1));
            var allowedPositions = positions
                .OrderBy(x => _random.Next())
                .Take(allowedPositionCount)
                .ToList();

            // 选择2-6个允许的时段
            var timeSlotCount = _random.Next(2, 7);
            var allowedTimeSlots = Enumerable.Range(0, 12)
                .OrderBy(x => _random.Next())
                .Take(timeSlotCount)
                .OrderBy(x => x)
                .ToList();

            var startDate = baseDate.AddDays(_random.Next(-30, 30));
            var endDate = startDate.AddDays(_random.Next(30, 180));

            assignments.Add(new FixedAssignmentDto
            {
                Id = i,
                PersonnelId = person.Id,
                PersonnelName = person.Name,
                AllowedPositionIds = allowedPositions.Select(p => p.Id).ToList(),
                AllowedPositionNames = allowedPositions.Select(p => p.Name).ToList(),
                AllowedTimeSlots = allowedTimeSlots,
                StartDate = startDate,
                EndDate = endDate,
                IsEnabled = _random.Next(100) < 80, // 80%启用
                RuleName = $"{person.Name}的定岗规则",
                Description = $"限制{person.Name}只能在指定哨位和时段工作",
                CreatedAt = baseDate.AddDays(-_random.Next(60)),
                UpdatedAt = baseDate.AddDays(-_random.Next(10))
            });
        }

        return assignments;
    }

    /// <summary>
    /// 生成手动指定数据
    /// </summary>
    private List<ManualAssignmentDto> GenerateManualAssignments(
        List<PersonnelDto> personnel,
        List<PositionDto> positions)
    {
        var assignments = new List<ManualAssignmentDto>();
        var baseDate = DateTime.UtcNow;
        var usedCombinations = new HashSet<string>();

        for (int i = 1; i <= _config.ManualAssignmentCount; i++)
        {
            var position = positions[_random.Next(positions.Count)];

            // 优先选择该哨位的可用人员，如果没有则随机选择
            var person = position.AvailablePersonnelIds.Count > 0
                ? personnel.FirstOrDefault(p => position.AvailablePersonnelIds.Contains(p.Id))
                  ?? personnel[_random.Next(personnel.Count)]
                : personnel[_random.Next(personnel.Count)];

            var date = baseDate.AddDays(_random.Next(-10, 30));
            var timeSlot = _random.Next(0, 12);

            // 确保不重复（同一哨位、日期、时段）
            var key = $"{position.Id}_{date:yyyyMMdd}_{timeSlot}";
            if (usedCombinations.Contains(key))
            {
                // 尝试不同的时段
                for (int retry = 0; retry < 12; retry++)
                {
                    timeSlot = (timeSlot + 1) % 12;
                    key = $"{position.Id}_{date:yyyyMMdd}_{timeSlot}";
                    if (!usedCombinations.Contains(key))
                        break;
                }

                // 如果还是重复，跳过这次生成
                if (usedCombinations.Contains(key))
                    continue;
            }

            usedCombinations.Add(key);

            assignments.Add(new ManualAssignmentDto
            {
                Id = i,
                PositionId = position.Id,
                PositionName = position.Name,
                TimeSlot = timeSlot,
                PersonnelId = person.Id,
                PersonnelName = person.Name,
                Date = date,
                IsEnabled = _random.Next(100) < 90, // 90%启用
                Remarks = $"手动指定{person.Name}在{_sampleData.GetTimeSlotName(timeSlot)}值班",
                CreatedAt = baseDate.AddDays(-_random.Next(30)),
                UpdatedAt = baseDate.AddDays(-_random.Next(5))
            });
        }

        return assignments;
    }

    #endregion

    #region 元数据和验证

    /// <summary>
    /// 创建导出元数据
    /// </summary>
    private ExportMetadata CreateMetadata(ExportData data)
    {
        return new ExportMetadata
        {
            ExportVersion = "1.0",
            ExportedAt = DateTime.UtcNow,
            DatabaseVersion = 1,
            ApplicationVersion = "1.0.0.0",
            Statistics = new DataStatistics
            {
                SkillCount = data.Skills?.Count ?? 0,
                PersonnelCount = data.Personnel?.Count ?? 0,
                PositionCount = data.Positions?.Count ?? 0,
                TemplateCount = data.Templates?.Count ?? 0,
                ConstraintCount = (data.FixedAssignments?.Count ?? 0) +
                                (data.ManualAssignments?.Count ?? 0) +
                                (data.HolidayConfigs?.Count ?? 0)
            }
        };
    }

    /// <summary>
    /// 验证生成的数据
    /// </summary>
    private void ValidateGeneratedData(ExportData data)
    {
        // 验证引用完整性
        var skillIds = new HashSet<int>(data.Skills.Select(s => s.Id));
        var personnelIds = new HashSet<int>(data.Personnel.Select(p => p.Id));
        var positionIds = new HashSet<int>(data.Positions.Select(p => p.Id));
        var holidayConfigIds = new HashSet<int>(data.HolidayConfigs.Select(h => h.Id));

        // 验证人员的技能引用
        foreach (var person in data.Personnel)
        {
            foreach (var skillId in person.SkillIds)
            {
                if (!skillIds.Contains(skillId))
                    throw new InvalidOperationException(
                        $"人员 {person.Name} 引用了不存在的技能ID: {skillId}");
            }
        }

        // 验证哨位的技能引用
        foreach (var position in data.Positions)
        {
            foreach (var skillId in position.RequiredSkillIds)
            {
                if (!skillIds.Contains(skillId))
                    throw new InvalidOperationException(
                        $"哨位 {position.Name} 引用了不存在的技能ID: {skillId}");
            }

            // 验证哨位的人员引用
            foreach (var personId in position.AvailablePersonnelIds)
            {
                if (!personnelIds.Contains(personId))
                    throw new InvalidOperationException(
                        $"哨位 {position.Name} 引用了不存在的人员ID: {personId}");
            }
        }

        // 验证排班模板的引用
        foreach (var template in data.Templates)
        {
            // 验证人员引用
            foreach (var personId in template.PersonnelIds)
            {
                if (!personnelIds.Contains(personId))
                    throw new InvalidOperationException(
                        $"排班模板 {template.Name} 引用了不存在的人员ID: {personId}");
            }

            // 验证哨位引用
            foreach (var positionId in template.PositionIds)
            {
                if (!positionIds.Contains(positionId))
                    throw new InvalidOperationException(
                        $"排班模板 {template.Name} 引用了不存在的哨位ID: {positionId}");
            }

            // 验证节假日配置引用
            if (template.HolidayConfigId.HasValue && !holidayConfigIds.Contains(template.HolidayConfigId.Value))
            {
                throw new InvalidOperationException(
                    $"排班模板 {template.Name} 引用了不存在的节假日配置ID: {template.HolidayConfigId}");
            }
        }

        // 验证定岗规则的引用
        foreach (var assignment in data.FixedAssignments)
        {
            // 验证人员引用
            if (!personnelIds.Contains(assignment.PersonnelId))
                throw new InvalidOperationException(
                    $"定岗规则 {assignment.RuleName} 引用了不存在的人员ID: {assignment.PersonnelId}");

            // 验证哨位引用
            foreach (var positionId in assignment.AllowedPositionIds)
            {
                if (!positionIds.Contains(positionId))
                    throw new InvalidOperationException(
                        $"定岗规则 {assignment.RuleName} 引用了不存在的哨位ID: {positionId}");
            }

            // 验证时段范围
            foreach (var timeSlot in assignment.AllowedTimeSlots)
            {
                if (timeSlot < 0 || timeSlot > 11)
                    throw new InvalidOperationException(
                        $"定岗规则 {assignment.RuleName} 包含无效的时段索引: {timeSlot}");
            }

            // 验证日期范围
            if (assignment.EndDate < assignment.StartDate)
                throw new InvalidOperationException(
                    $"定岗规则 {assignment.RuleName} 的结束日期早于开始日期");
        }

        // 验证手动指定的引用
        foreach (var assignment in data.ManualAssignments)
        {
            // 验证人员引用
            if (!personnelIds.Contains(assignment.PersonnelId))
                throw new InvalidOperationException(
                    $"手动指定 (ID: {assignment.Id}) 引用了不存在的人员ID: {assignment.PersonnelId}");

            // 验证哨位引用
            if (!positionIds.Contains(assignment.PositionId))
                throw new InvalidOperationException(
                    $"手动指定 (ID: {assignment.Id}) 引用了不存在的哨位ID: {assignment.PositionId}");

            // 验证时段范围
            if (assignment.TimeSlot < 0 || assignment.TimeSlot > 11)
                throw new InvalidOperationException(
                    $"手动指定 (ID: {assignment.Id}) 包含无效的时段索引: {assignment.TimeSlot}");
        }
    }

    #endregion
}
