using AutoScheduling3.DTOs;

namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 定岗规则数据生成器
/// </summary>
public class FixedAssignmentGenerator : IEntityGenerator<FixedAssignmentDto>
{
    private readonly TestDataConfiguration _config;
    private readonly Random _random;

    public FixedAssignmentGenerator(
        TestDataConfiguration config,
        Random random)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>
    /// 生成定岗规则数据
    /// 为人员生成定岗规则，指定允许的哨位和时段
    /// </summary>
    /// <param name="dependencies">生成数据所需的依赖项：[0]=人员列表(List&lt;PersonnelDto&gt;), [1]=哨位列表(List&lt;PositionDto&gt;)</param>
    /// <returns>生成的定岗规则列表</returns>
    /// <exception cref="ArgumentException">当依赖项不足或类型不正确时抛出</exception>
    public List<FixedAssignmentDto> Generate(params object[] dependencies)
    {
        if (dependencies == null || dependencies.Length < 2)
            throw new ArgumentException("需要提供人员列表和哨位列表作为依赖项", nameof(dependencies));

        var personnel = dependencies[0] as List<PersonnelDto>;
        var positions = dependencies[1] as List<PositionDto>;

        if (personnel == null || personnel.Count == 0)
            throw new ArgumentException("人员列表不能为空或类型不正确", nameof(dependencies));
        if (positions == null || positions.Count == 0)
            throw new ArgumentException("哨位列表不能为空或类型不正确", nameof(dependencies));

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
}
