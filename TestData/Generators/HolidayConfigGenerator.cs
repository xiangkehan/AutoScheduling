using AutoScheduling3.DTOs;

namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 节假日配置数据生成器
/// </summary>
public class HolidayConfigGenerator : IEntityGenerator<HolidayConfigDto>
{
    private readonly TestDataConfiguration _config;
    private readonly Random _random;

    public HolidayConfigGenerator(
        TestDataConfiguration config,
        Random random)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>
    /// 生成节假日配置数据
    /// 生成标准周末配置、单休配置和自定义配置，确保只有一个配置处于激活状态
    /// </summary>
    /// <param name="dependencies">生成数据所需的依赖项（此生成器不需要依赖项）</param>
    /// <returns>生成的节假日配置列表</returns>
    public List<HolidayConfigDto> Generate(params object[] dependencies)
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
            UpdatedAt = baseDate.AddDays(-10)
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
}
