using AutoScheduling3.DTOs;
using AutoScheduling3.TestData.Helpers;

namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 技能数据生成器
/// </summary>
public class SkillGenerator : IEntityGenerator<SkillDto>
{
    private readonly TestDataConfiguration _config;
    private readonly SampleDataProvider _sampleData;
    private readonly UniqueNameGenerator _nameGenerator;
    private readonly Random _random;

    public SkillGenerator(
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
    /// 生成技能数据
    /// </summary>
    public List<SkillDto> Generate()
    {
        var skills = new List<SkillDto>();
        var availableNames = _sampleData.GetAllSkillNames();
        var usedNames = new HashSet<string>();

        for (int i = 1; i <= _config.SkillCount; i++)
        {
            // 使用UniqueNameGenerator生成唯一名称
            string name = _nameGenerator.Generate(availableNames, usedNames, "技能", i);
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
}
