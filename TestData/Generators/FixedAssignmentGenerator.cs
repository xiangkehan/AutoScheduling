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
    /// </summary>
    /// <param name="personnel">已生成的人员列表</param>
    /// <param name="positions">已生成的哨位列表</param>
    public List<FixedAssignmentDto> Generate(
        List<PersonnelDto> personnel,
        List<PositionDto> positions)
    {
        if (personnel == null || personnel.Count == 0)
            throw new ArgumentException("人员列表不能为空", nameof(personnel));
        if (positions == null || positions.Count == 0)
            throw new ArgumentException("哨位列表不能为空", nameof(positions));

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
