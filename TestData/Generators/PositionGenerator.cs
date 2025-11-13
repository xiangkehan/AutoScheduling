using AutoScheduling3.DTOs;
using AutoScheduling3.TestData.Helpers;

namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 哨位数据生成器
/// </summary>
public class PositionGenerator : IEntityGenerator<PositionDto>
{
    private readonly TestDataConfiguration _config;
    private readonly SampleDataProvider _sampleData;
    private readonly UniqueNameGenerator _nameGenerator;
    private readonly Random _random;

    public PositionGenerator(
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
    /// 生成哨位数据
    /// </summary>
    /// <param name="skills">已生成的技能列表</param>
    /// <param name="personnel">已生成的人员列表</param>
    /// <returns>生成的哨位列表</returns>
    /// <exception cref="ArgumentException">当技能或人员列表为空时抛出</exception>
    public List<PositionDto> Generate(List<SkillDto> skills, List<PersonnelDto> personnel)
    {
        if (skills == null || skills.Count == 0)
            throw new ArgumentException("技能列表不能为空", nameof(skills));
        if (personnel == null || personnel.Count == 0)
            throw new ArgumentException("人员列表不能为空", nameof(personnel));

        var positions = new List<PositionDto>();
        var availableNames = _sampleData.GetAllPositionNames();
        var availableLocations = _sampleData.GetAllLocations();
        var usedNames = new HashSet<string>();
        var usedLocations = new HashSet<string>();

        for (int i = 1; i <= _config.PositionCount; i++)
        {
            // 使用UniqueNameGenerator生成唯一的哨位名称
            string name = _nameGenerator.Generate(availableNames, usedNames, "哨位", i);
            usedNames.Add(name);

            // 使用UniqueNameGenerator生成唯一的地点名称
            string location = _nameGenerator.Generate(availableLocations, usedLocations, "位置", i);
            usedLocations.Add(location);

            // 随机分配1-2个所需技能（不超过可用技能总数）
            var requiredSkillCount = _random.Next(1, Math.Min(3, skills.Count + 1));
            var requiredSkills = skills
                .OrderBy(x => _random.Next())
                .Take(requiredSkillCount)
                .ToList();

            // 找出具备所需技能的人员（必须可用、未退役，且至少拥有一项所需技能）
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
}
