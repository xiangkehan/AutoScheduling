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
    /// 生成人员数据
    /// </summary>
    /// <param name="dependencies">生成数据所需的依赖项：[0]=技能列表(List&lt;SkillDto&gt;)</param>
    /// <returns>生成的人员列表</returns>
    /// <exception cref="ArgumentException">当依赖项不足或类型不正确时抛出</exception>
    public List<PersonnelDto> Generate(params object[] dependencies)
    {
        if (dependencies == null || dependencies.Length < 1)
            throw new ArgumentException("需要提供技能列表作为依赖项", nameof(dependencies));

        var skills = dependencies[0] as List<SkillDto>;

        if (skills == null || skills.Count == 0)
            throw new ArgumentException("技能列表不能为空或类型不正确", nameof(dependencies));

        var personnel = new List<PersonnelDto>();
        var availableNames = _sampleData.GetAllNames();
        var usedNames = new HashSet<string>();

        for (int i = 1; i <= _config.PersonnelCount; i++)
        {
            // 使用UniqueNameGenerator生成唯一名称
            string name = _nameGenerator.Generate(availableNames, usedNames, "人员", i);
            usedNames.Add(name);

            // 随机分配1-3个技能（不超过可用技能总数）
            var skillCount = _random.Next(1, Math.Min(4, skills.Count + 1));
            // 使用随机排序后取前N个，确保技能分配的随机性
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
}
