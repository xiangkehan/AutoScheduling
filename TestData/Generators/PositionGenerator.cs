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
    /// 生成哨位数据（不计算可用人员）
    /// </summary>
    /// <param name="dependencies">生成数据所需的依赖项：[0]=技能列表(List&lt;SkillDto&gt;)</param>
    /// <returns>生成的哨位列表（可用人员为空）</returns>
    /// <exception cref="ArgumentException">当依赖项不足或类型不正确时抛出</exception>
    public List<PositionDto> Generate(params object[] dependencies)
    {
        if (dependencies == null || dependencies.Length < 1)
            throw new ArgumentException("需要提供技能列表作为依赖项", nameof(dependencies));

        var skills = dependencies[0] as List<SkillDto>;

        if (skills == null || skills.Count == 0)
            throw new ArgumentException("技能列表不能为空或类型不正确", nameof(dependencies));

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
                AvailablePersonnelIds = new List<int>(),  // 初始化为空列表
                AvailablePersonnelNames = new List<string>(),  // 初始化为空列表
                IsActive = true,
                CreatedAt = positionCreatedAt,
                UpdatedAt = positionUpdatedAt
            });
        }

        return positions;
    }
}
