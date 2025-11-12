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
    /// <param name="skills">已生成的技能列表</param>
    public List<PersonnelDto> Generate(List<SkillDto> skills)
    {
        if (skills == null || skills.Count == 0)
            throw new ArgumentException("技能列表不能为空", nameof(skills));

        var personnel = new List<PersonnelDto>();
        var availableNames = _sampleData.GetAllNames();
        var usedNames = new HashSet<string>();

        for (int i = 1; i <= _config.PersonnelCount; i++)
        {
            // 使用UniqueNameGenerator生成唯一名称
            string name = _nameGenerator.Generate(availableNames, usedNames, "人员", i);
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
}
