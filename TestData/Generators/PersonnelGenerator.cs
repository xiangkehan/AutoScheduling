using AutoScheduling3.DTOs;
using AutoScheduling3.TestData.Helpers;

namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 人员数据生成器
/// </summary>
public class PersonnelGenerator : IEntityGenerator<PersonnelDto>
{
    private readonly TestDataConfiguration _config;
    private readonly SampleDataProvider _sampleData;
    private readonly UniqueNameGenerator _nameGenerator;
    private readonly Random _random;

    public PersonnelGenerator(
        TestDataConfiguration config,
        SampleDataProvider sampleData,
        UniqueNameGenerator nameGenerator,
        Random random)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _sampleData = sampleData ?? throw new ArgumentNullException(nameof(sampleData));
        _nameGenerator = nameGenerator ?? throw new ArgumentNullException(nameof(nameGenerator));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>
    /// 生成人员数据（不分配技能）
    /// </summary>
    /// <param name="dependencies">无依赖项</param>
    /// <returns>生成的人员列表（技能为空）</returns>
    public List<PersonnelDto> Generate(params object[] dependencies)
    {
        var personnel = new List<PersonnelDto>();
        var availableNames = _sampleData.GetAllNames();
        var usedNames = new HashSet<string>();

        for (int i = 1; i <= _config.PersonnelCount; i++)
        {
            // 使用UniqueNameGenerator生成唯一名称
            string name = _nameGenerator.Generate(availableNames, usedNames, "人员", i);
            usedNames.Add(name);

            personnel.Add(new PersonnelDto
            {
                Id = i,
                Name = name,
                SkillIds = new List<int>(),  // 空列表，由 SkillAssigner 填充
                SkillNames = new List<string>(),  // 空列表，由 SkillAssigner 填充
                IsAvailable = _random.NextDouble() < _config.PersonnelAvailabilityRate,  // 使用配置的可用率
                IsRetired = _random.NextDouble() < _config.PersonnelRetirementRate,      // 使用配置的退役率
                RecentShiftIntervalCount = _random.Next(0, 20),
                RecentHolidayShiftIntervalCount = _random.Next(0, 10),
                RecentPeriodShiftIntervals = Enumerable.Range(0, 12)
                    .Select(_ => _random.Next(0, 15))
                    .ToArray()
            });
        }

        return personnel;
    }
}
