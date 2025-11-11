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
        System.Diagnostics.Debug.WriteLine("=== 开始生成测试数据 ===");
        System.Diagnostics.Debug.WriteLine($"配置：技能={_config.SkillCount}, 人员={_config.PersonnelCount}, " +
            $"哨位={_config.PositionCount}, 节假日配置={_config.HolidayConfigCount}, " +
            $"模板={_config.TemplateCount}, 定岗规则={_config.FixedAssignmentCount}, " +
            $"手动指定={_config.ManualAssignmentCount}");
        
        // 按依赖顺序生成数据
        var skills = GenerateSkills();
        System.Diagnostics.Debug.WriteLine($"✓ 生成技能数据：{skills.Count} 条");
        
        var personnel = GeneratePersonnel(skills);
        System.Diagnostics.Debug.WriteLine($"✓ 生成人员数据：{personnel.Count} 条");
        
        var positions = GeneratePositions(skills, personnel);
        System.Diagnostics.Debug.WriteLine($"✓ 生成哨位数据：{positions.Count} 条");
        
        var holidayConfigs = GenerateHolidayConfigs();
        System.Diagnostics.Debug.WriteLine($"✓ 生成节假日配置：{holidayConfigs.Count} 条");
        
        var templates = GenerateTemplates(personnel, positions, holidayConfigs);
        System.Diagnostics.Debug.WriteLine($"✓ 生成排班模板：{templates.Count} 条");
        
        var fixedAssignments = GenerateFixedAssignments(personnel, positions);
        System.Diagnostics.Debug.WriteLine($"✓ 生成定岗规则：{fixedAssignments.Count} 条");
        
        var manualAssignments = GenerateManualAssignments(personnel, positions);
        System.Diagnostics.Debug.WriteLine($"✓ 生成手动指定：{manualAssignments.Count} 条");

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
        System.Diagnostics.Debug.WriteLine("开始验证生成的数据...");
        ValidateGeneratedData(exportData);
        System.Diagnostics.Debug.WriteLine("✓ 数据验证通过");
        
        System.Diagnostics.Debug.WriteLine("=== 测试数据生成完成 ===");

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
    /// 生成唯一名称
    /// </summary>
    /// <param name="availableNames">可用的预定义名称列表</param>
    /// <param name="usedNames">已使用的名称集合</param>
    /// <param name="fallbackPrefix">备用名称前缀</param>
    /// <param name="index">当前索引</param>
    /// <returns>唯一的名称</returns>
    private string GenerateUniqueName(
        List<string> availableNames,
        HashSet<string> usedNames,
        string fallbackPrefix,
        int index)
    {
        const int maxRetries = 1000;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            string name;

            // 优先使用预定义名称
            if (availableNames.Count > 0)
            {
                var randomIndex = _random.Next(availableNames.Count);
                name = availableNames[randomIndex];

                // 检查是否已被使用
                if (!usedNames.Contains(name))
                {
                    availableNames.RemoveAt(randomIndex);
                    return name;
                }

                // 如果已被使用，从列表中移除并继续尝试
                availableNames.RemoveAt(randomIndex);
            }
            else
            {
                // 预定义名称用完后，生成带编号的备用名称
                name = $"{fallbackPrefix}{index + retryCount}";

                // 检查是否已被使用
                if (!usedNames.Contains(name))
                {
                    return name;
                }
            }

            retryCount++;
        }

        // 达到最大重试次数，抛出详细的异常信息
        var errorDetails = new System.Text.StringBuilder();
        errorDetails.AppendLine($"无法生成唯一名称，已达到最大重试次数限制。");
        errorDetails.AppendLine($"详细信息：");
        errorDetails.AppendLine($"  - 尝试次数: {maxRetries}");
        errorDetails.AppendLine($"  - 备用名称前缀: {fallbackPrefix}");
        errorDetails.AppendLine($"  - 当前索引: {index}");
        errorDetails.AppendLine($"  - 已使用名称数量: {usedNames.Count}");
        errorDetails.AppendLine($"  - 剩余预定义名称数量: {availableNames.Count}");
        errorDetails.AppendLine($"  - 预定义名称已用完: {availableNames.Count == 0}");
        
        if (usedNames.Count > 0)
        {
            var sampleNames = usedNames.Take(5).ToList();
            errorDetails.AppendLine($"  - 已使用名称示例: {string.Join(", ", sampleNames)}");
            if (usedNames.Count > 5)
            {
                errorDetails.AppendLine($"    ... 还有 {usedNames.Count - 5} 个名称");
            }
        }
        
        errorDetails.AppendLine($"建议：检查是否请求的数据量过大，或者预定义名称列表是否足够。");
        
        throw new InvalidOperationException(errorDetails.ToString());
    }

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
            // 使用GenerateUniqueName辅助方法生成唯一名称
            string name = GenerateUniqueName(availableNames, usedNames, "技能", i);
            usedNames.Add(name);

            var createdAt = DateTime.UtcNow.AddDays(-_random.Next(365));
            var updatedAt = createdAt.AddDays(_random.Next(0, (int)(DateTime.UtcNow - createdAt).TotalDays + 1));
            
            skills.Add(new SkillDto
            {
                Id = i,
                Name = name,
                Description = $"{name}相关的专业技能",
                IsActive = _random.Next(100) < 90, // 90%激活
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
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
            // 使用GenerateUniqueName辅助方法生成唯一名称
            string name = GenerateUniqueName(availableNames, usedNames, "人员", i);
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
        var usedLocations = new HashSet<string>();

        for (int i = 1; i <= _config.PositionCount; i++)
        {
            // 使用GenerateUniqueName辅助方法生成唯一的哨位名称
            string name = GenerateUniqueName(availableNames, usedNames, "哨位", i);
            usedNames.Add(name);

            // 使用GenerateUniqueName辅助方法生成唯一的地点名称
            string location = GenerateUniqueName(availableLocations, usedLocations, "位置", i);
            usedLocations.Add(location);

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

            // 生成时间戳，确保UpdatedAt不早于CreatedAt
            var positionCreatedAt = DateTime.UtcNow.AddDays(-_random.Next(180));
            var positionUpdatedAt = positionCreatedAt.AddDays(_random.Next(0, (int)(DateTime.UtcNow - positionCreatedAt).TotalDays + 1));

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
                CreatedAt = positionCreatedAt,
                UpdatedAt = positionUpdatedAt
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
        var usedNames = new HashSet<string>();
        var baseDate = DateTime.UtcNow;

        // 配置1: 标准周末配置
        var config1Name = "标准周末配置";
        usedNames.Add(config1Name);
        configs.Add(new HolidayConfigDto
        {
            Id = 1,
            ConfigName = config1Name,
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
            UpdatedAt = baseDate.AddDays(-10) // -10 > -90，所以这个是正确的
        });

        // 配置2: 单休配置
        if (_config.HolidayConfigCount > 1)
        {
            var config2Name = "单休配置";
            usedNames.Add(config2Name);
            configs.Add(new HolidayConfigDto
            {
                Id = 2,
                ConfigName = config2Name,
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
            // 生成唯一的配置名称
            string configName;
            int nameIndex = 1;
            int retryCount = 0;
            const int maxRetries = 1000;
            
            do
            {
                configName = $"自定义配置{nameIndex}";
                nameIndex++;
                retryCount++;
                
                if (retryCount >= maxRetries)
                {
                    var errorDetails = new System.Text.StringBuilder();
                    errorDetails.AppendLine($"无法生成唯一的节假日配置名称，已达到最大重试次数限制。");
                    errorDetails.AppendLine($"详细信息：");
                    errorDetails.AppendLine($"  - 尝试次数: {maxRetries}");
                    errorDetails.AppendLine($"  - 当前配置索引: {i}");
                    errorDetails.AppendLine($"  - 请求的配置总数: {_config.HolidayConfigCount}");
                    errorDetails.AppendLine($"  - 已使用名称数量: {usedNames.Count}");
                    errorDetails.AppendLine($"  - 最后尝试的名称: {configName}");
                    errorDetails.AppendLine($"建议：检查配置名称生成逻辑或减少请求的配置数量。");
                    
                    throw new InvalidOperationException(errorDetails.ToString());
                }
            } while (usedNames.Contains(configName));
            
            usedNames.Add(configName);

            var holidayConfigCreatedAt = baseDate.AddDays(-_random.Next(30, 90));
            var holidayConfigUpdatedAt = holidayConfigCreatedAt.AddDays(_random.Next(0, (int)(baseDate - holidayConfigCreatedAt).TotalDays + 1));
            
            configs.Add(new HolidayConfigDto
            {
                Id = i,
                ConfigName = configName,
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
                CreatedAt = holidayConfigCreatedAt,
                UpdatedAt = holidayConfigUpdatedAt
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
        var usedNames = new HashSet<string>();

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

            // 生成唯一的模板名称
            string templateName;
            int nameIndex = i;
            int retryCount = 0;
            const int maxRetries = 1000;
            
            do
            {
                templateName = $"{_sampleData.GetTemplateTypeName(type)}排班模板{nameIndex}";
                nameIndex++;
                retryCount++;
                
                if (retryCount >= maxRetries)
                {
                    var errorDetails = new System.Text.StringBuilder();
                    errorDetails.AppendLine($"无法生成唯一的模板名称，已达到最大重试次数限制。");
                    errorDetails.AppendLine($"详细信息：");
                    errorDetails.AppendLine($"  - 尝试次数: {maxRetries}");
                    errorDetails.AppendLine($"  - 模板类型: {type}");
                    errorDetails.AppendLine($"  - 当前模板索引: {i}");
                    errorDetails.AppendLine($"  - 请求的模板总数: {_config.TemplateCount}");
                    errorDetails.AppendLine($"  - 已使用名称数量: {usedNames.Count}");
                    errorDetails.AppendLine($"  - 最后尝试的名称: {templateName}");
                    
                    if (usedNames.Count > 0)
                    {
                        var sampleNames = usedNames.Take(5).ToList();
                        errorDetails.AppendLine($"  - 已使用名称示例: {string.Join(", ", sampleNames)}");
                        if (usedNames.Count > 5)
                        {
                            errorDetails.AppendLine($"    ... 还有 {usedNames.Count - 5} 个名称");
                        }
                    }
                    
                    errorDetails.AppendLine($"建议：检查模板名称生成逻辑或减少请求的模板数量。");
                    
                    throw new InvalidOperationException(errorDetails.ToString());
                }
            } while (usedNames.Contains(templateName));
            
            usedNames.Add(templateName);

            // 生成时间戳，确保UpdatedAt不早于CreatedAt
            var templateCreatedAt = baseDate.AddDays(-_random.Next(120));
            var templateUpdatedAt = templateCreatedAt.AddDays(_random.Next(0, (int)(baseDate - templateCreatedAt).TotalDays + 1));

            templates.Add(new SchedulingTemplateDto
            {
                Id = i,
                Name = templateName,
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
                CreatedAt = templateCreatedAt,
                UpdatedAt = templateUpdatedAt,
                LastUsedAt = _random.Next(100) < 70
                    ? templateUpdatedAt.AddDays(_random.Next(0, (int)(baseDate - templateUpdatedAt).TotalDays + 1))
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

            // 生成时间戳，确保UpdatedAt不早于CreatedAt
            var fixedAssignmentCreatedAt = baseDate.AddDays(-_random.Next(60));
            var fixedAssignmentUpdatedAt = fixedAssignmentCreatedAt.AddDays(_random.Next(0, (int)(baseDate - fixedAssignmentCreatedAt).TotalDays + 1));

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
                CreatedAt = fixedAssignmentCreatedAt,
                UpdatedAt = fixedAssignmentUpdatedAt
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
        int skippedCount = 0;
        int assignmentId = 1;
        
        System.Diagnostics.Debug.WriteLine(
            $"开始生成手动指定数据：" +
            $"请求数量: {_config.ManualAssignmentCount}, " +
            $"可用哨位: {positions.Count}, " +
            $"可用人员: {personnel.Count}, " +
            $"日期范围: {baseDate.AddDays(-10):yyyy-MM-dd} 至 {baseDate.AddDays(30):yyyy-MM-dd}");

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
            bool foundUnique = !usedCombinations.Contains(key);

            if (!foundUnique)
            {
                // 第一阶段：尝试不同的时段（最多12次）
                int originalTimeSlot = timeSlot;
                for (int timeSlotRetry = 0; timeSlotRetry < 12; timeSlotRetry++)
                {
                    timeSlot = (originalTimeSlot + timeSlotRetry) % 12;
                    key = $"{position.Id}_{date:yyyyMMdd}_{timeSlot}";
                    if (!usedCombinations.Contains(key))
                    {
                        foundUnique = true;
                        break;
                    }
                }

                // 第二阶段：如果所有时段都被占用，尝试不同的日期（最多5次）
                if (!foundUnique)
                {
                    for (int dateRetry = 1; dateRetry <= 5; dateRetry++)
                    {
                        date = baseDate.AddDays(_random.Next(-10, 30));
                        timeSlot = _random.Next(0, 12);
                        key = $"{position.Id}_{date:yyyyMMdd}_{timeSlot}";
                        
                        if (!usedCombinations.Contains(key))
                        {
                            foundUnique = true;
                            break;
                        }

                        // 对于新日期，也尝试不同的时段
                        for (int timeSlotRetry = 0; timeSlotRetry < 12; timeSlotRetry++)
                        {
                            timeSlot = (timeSlot + timeSlotRetry) % 12;
                            key = $"{position.Id}_{date:yyyyMMdd}_{timeSlot}";
                            if (!usedCombinations.Contains(key))
                            {
                                foundUnique = true;
                                break;
                            }
                        }

                        if (foundUnique)
                            break;
                    }
                }

                // 如果达到重试上限后仍然失败，跳过该记录
                if (!foundUnique)
                {
                    skippedCount++;
                    System.Diagnostics.Debug.WriteLine(
                        $"跳过手动指定记录 #{i}：" +
                        $"无法为哨位 {position.Name} (ID: {position.Id}) " +
                        $"找到唯一的日期和时段组合。" +
                        $"已尝试多个时段和日期组合。");
                    continue;
                }
            }

            usedCombinations.Add(key);

            // 生成时间戳，确保UpdatedAt不早于CreatedAt
            var manualAssignmentCreatedAt = baseDate.AddDays(-_random.Next(30));
            var manualAssignmentUpdatedAt = manualAssignmentCreatedAt.AddDays(_random.Next(0, (int)(baseDate - manualAssignmentCreatedAt).TotalDays + 1));

            assignments.Add(new ManualAssignmentDto
            {
                Id = assignmentId++,
                PositionId = position.Id,
                PositionName = position.Name,
                TimeSlot = timeSlot,
                PersonnelId = person.Id,
                PersonnelName = person.Name,
                Date = date,
                IsEnabled = _random.Next(100) < 90, // 90%启用
                Remarks = $"手动指定{person.Name}在{_sampleData.GetTimeSlotName(timeSlot)}值班",
                CreatedAt = manualAssignmentCreatedAt,
                UpdatedAt = manualAssignmentUpdatedAt
            });
        }

        // 如果跳过的记录数量超过请求数量的10%，记录警告信息
        if (skippedCount > 0)
        {
            double skipPercentage = (double)skippedCount / _config.ManualAssignmentCount * 100;
            string logMessage = $"手动指定数据生成完成：" +
                $"请求数量: {_config.ManualAssignmentCount}, " +
                $"成功生成: {assignments.Count}, " +
                $"跳过记录: {skippedCount} ({skipPercentage:F1}%), " +
                $"可用哨位数: {positions.Count}, " +
                $"可用人员数: {personnel.Count}";
            
            System.Diagnostics.Debug.WriteLine(logMessage);
            
            if (skipPercentage > 10)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"警告：跳过的记录数量（{skippedCount}条，{skipPercentage:F1}%）超过了请求数量的10%。" +
                    $"这可能表明哨位数量不足或日期范围过小。" +
                    $"建议增加哨位数量或扩大日期范围以避免冲突。");
            }
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

    #endregion
}
