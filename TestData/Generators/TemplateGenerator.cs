using AutoScheduling3.DTOs;

namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 排班模板数据生成器
/// </summary>
public class TemplateGenerator : IEntityGenerator<SchedulingTemplateDto>
{
    private readonly TestDataConfiguration _config;
    private readonly SampleDataProvider _sampleData;
    private readonly Random _random;

    public TemplateGenerator(
        TestDataConfiguration config,
        SampleDataProvider sampleData,
        Random random)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _sampleData = sampleData ?? throw new ArgumentNullException(nameof(sampleData));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>
    /// 生成排班模板数据
    /// 生成不同类型的排班模板（regular、holiday、special），为每个模板选择合适数量的人员和哨位
    /// </summary>
    /// <param name="dependencies">生成数据所需的依赖项：[0]=人员列表(List&lt;PersonnelDto&gt;), [1]=哨位列表(List&lt;PositionDto&gt;), [2]=节假日配置列表(List&lt;HolidayConfigDto&gt;)</param>
    /// <returns>生成的排班模板列表</returns>
    /// <exception cref="ArgumentException">当依赖项不足或类型不正确时抛出</exception>
    public List<SchedulingTemplateDto> Generate(params object[] dependencies)
    {
        if (dependencies == null || dependencies.Length < 3)
            throw new ArgumentException("需要提供人员列表、哨位列表和节假日配置列表作为依赖项", nameof(dependencies));

        var personnel = dependencies[0] as List<PersonnelDto>;
        var positions = dependencies[1] as List<PositionDto>;
        var holidayConfigs = dependencies[2] as List<HolidayConfigDto>;

        if (personnel == null || personnel.Count == 0)
            throw new ArgumentException("人员列表不能为空或类型不正确", nameof(dependencies));
        if (positions == null || positions.Count == 0)
            throw new ArgumentException("哨位列表不能为空或类型不正确", nameof(dependencies));
        if (holidayConfigs == null || holidayConfigs.Count == 0)
            throw new ArgumentException("节假日配置列表不能为空或类型不正确", nameof(dependencies));

        var templates = new List<SchedulingTemplateDto>();
        var templateTypes = new[] { "regular", "holiday", "special" };
        var baseDate = DateTime.UtcNow;
        var usedNames = new HashSet<string>();

        for (int i = 1; i <= _config.TemplateCount; i++)
        {
            var type = templateTypes[Math.Min(i - 1, templateTypes.Length - 1)];

            // 选择可用人员（必须可用且未退役）
            var availablePersonnel = personnel
                .Where(p => p.IsAvailable && !p.IsRetired)
                .ToList();

            // 选择至少5个人员或可用人员的一半（取较大值），但不超过可用人员总数
            var personnelToSelect = Math.Max(5, availablePersonnel.Count / 2);
            personnelToSelect = Math.Min(personnelToSelect, availablePersonnel.Count);

            var selectedPersonnel = availablePersonnel
                .OrderBy(x => _random.Next())
                .Take(personnelToSelect)
                .ToList();

            // 选择至少3个哨位或哨位总数的一半（取较大值），但不超过哨位总数
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
}
