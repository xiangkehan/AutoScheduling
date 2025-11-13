using AutoScheduling3.DTOs;

namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 手动指定数据生成器
/// </summary>
public class ManualAssignmentGenerator : IEntityGenerator<ManualAssignmentDto>
{
    private readonly TestDataConfiguration _config;
    private readonly SampleDataProvider _sampleData;
    private readonly Random _random;

    public ManualAssignmentGenerator(
        TestDataConfiguration config,
        SampleDataProvider sampleData,
        Random random)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _sampleData = sampleData ?? throw new ArgumentNullException(nameof(sampleData));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>
    /// 生成手动指定数据
    /// 生成手动指定数据，确保唯一性（同一哨位、日期、时段不重复）
    /// 使用重试逻辑尝试不同时段和日期以避免冲突
    /// </summary>
    /// <param name="personnel">已生成的人员列表</param>
    /// <param name="positions">已生成的哨位列表</param>
    /// <returns>生成的手动指定列表</returns>
    /// <exception cref="ArgumentException">当任何参数列表为空时抛出</exception>
    public List<ManualAssignmentDto> Generate(
        List<PersonnelDto> personnel,
        List<PositionDto> positions)
    {
        if (personnel == null || personnel.Count == 0)
            throw new ArgumentException("人员列表不能为空", nameof(personnel));
        if (positions == null || positions.Count == 0)
            throw new ArgumentException("哨位列表不能为空", nameof(positions));

        var assignments = new List<ManualAssignmentDto>();
        var baseDate = DateTime.UtcNow;
        var usedCombinations = new HashSet<string>();
        int skippedCount = 0;
        int assignmentId = 1;
        
        System.Diagnostics.Debug.WriteLine(
            $"开始生成手动指定数据：" +
            $"请求数量: {_config.ManualAssignmentCount}, " +
            $"可用哨位: {positions.Count}, " +
            $"可用人员: {personnel.Count}, " +
            $"日期范围: {baseDate.AddDays(-10):yyyy-MM-dd} 至 {baseDate.AddDays(30):yyyy-MM-dd}");

        for (int i = 1; i <= _config.ManualAssignmentCount; i++)
        {
            var position = positions[_random.Next(positions.Count)];

            // 优先选择该哨位的可用人员，如果没有则随机选择
            var person = position.AvailablePersonnelIds.Count > 0
                ? personnel.FirstOrDefault(p => position.AvailablePersonnelIds.Contains(p.Id))
                  ?? personnel[_random.Next(personnel.Count)]
                : personnel[_random.Next(personnel.Count)];

            var date = baseDate.AddDays(_random.Next(-10, 30));
            var timeSlot = _random.Next(0, 12);

            // 确保不重复（同一哨位、日期、时段的组合必须唯一）
            var key = $"{position.Id}_{date:yyyyMMdd}_{timeSlot}";
            bool foundUnique = !usedCombinations.Contains(key);

            if (!foundUnique)
            {
                // 第一阶段：尝试不同的时段（最多12次，覆盖所有可能的时段）
                int originalTimeSlot = timeSlot;
                for (int timeSlotRetry = 0; timeSlotRetry < 12; timeSlotRetry++)
                {
                    timeSlot = (originalTimeSlot + timeSlotRetry) % 12;
                    key = $"{position.Id}_{date:yyyyMMdd}_{timeSlot}";
                    if (!usedCombinations.Contains(key))
                    {
                        foundUnique = true;
                        break;
                    }
                }

                // 第二阶段：如果当前日期的所有时段都被占用，尝试不同的日期（最多5次）
                if (!foundUnique)
                {
                    for (int dateRetry = 1; dateRetry <= 5; dateRetry++)
                    {
                        date = baseDate.AddDays(_random.Next(-10, 30));
                        timeSlot = _random.Next(0, 12);
                        key = $"{position.Id}_{date:yyyyMMdd}_{timeSlot}";
                        
                        if (!usedCombinations.Contains(key))
                        {
                            foundUnique = true;
                            break;
                        }

                        // 对于新日期，也尝试所有可能的时段
                        for (int timeSlotRetry = 0; timeSlotRetry < 12; timeSlotRetry++)
                        {
                            timeSlot = (timeSlot + timeSlotRetry) % 12;
                            key = $"{position.Id}_{date:yyyyMMdd}_{timeSlot}";
                            if (!usedCombinations.Contains(key))
                            {
                                foundUnique = true;
                                break;
                            }
                        }

                        if (foundUnique)
                            break;
                    }
                }

                // 如果达到重试上限后仍然失败，跳过该记录并记录日志
                if (!foundUnique)
                {
                    skippedCount++;
                    System.Diagnostics.Debug.WriteLine(
                        $"跳过手动指定记录 #{i}：" +
                        $"无法为哨位 {position.Name} (ID: {position.Id}) " +
                        $"找到唯一的日期和时段组合。" +
                        $"已尝试多个时段和日期组合。");
                    continue;
                }
            }

            usedCombinations.Add(key);

            // 生成时间戳，确保UpdatedAt不早于CreatedAt
            var manualAssignmentCreatedAt = baseDate.AddDays(-_random.Next(30));
            var manualAssignmentUpdatedAt = manualAssignmentCreatedAt.AddDays(_random.Next(0, (int)(baseDate - manualAssignmentCreatedAt).TotalDays + 1));

            assignments.Add(new ManualAssignmentDto
            {
                Id = assignmentId++,
                PositionId = position.Id,
                PositionName = position.Name,
                TimeSlot = timeSlot,
                PersonnelId = person.Id,
                PersonnelName = person.Name,
                Date = date,
                IsEnabled = _random.Next(100) < 90, // 90%启用
                Remarks = $"手动指定{person.Name}在{_sampleData.GetTimeSlotName(timeSlot)}值班",
                CreatedAt = manualAssignmentCreatedAt,
                UpdatedAt = manualAssignmentUpdatedAt
            });
        }

        // 如果跳过的记录数量超过请求数量的10%，记录警告信息
        if (skippedCount > 0)
        {
            double skipPercentage = (double)skippedCount / _config.ManualAssignmentCount * 100;
            string logMessage = $"手动指定数据生成完成：" +
                $"请求数量: {_config.ManualAssignmentCount}, " +
                $"成功生成: {assignments.Count}, " +
                $"跳过记录: {skippedCount} ({skipPercentage:F1}%), " +
                $"可用哨位数: {positions.Count}, " +
                $"可用人员数: {personnel.Count}";
            
            System.Diagnostics.Debug.WriteLine(logMessage);
            
            if (skipPercentage > 10)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"警告：跳过的记录数量（{skippedCount}条，{skipPercentage:F1}%）超过了请求数量的10%。" +
                    $"这可能表明哨位数量不足或日期范围过小。" +
                    $"建议增加哨位数量或扩大日期范围以避免冲突。");
            }
        }

        return assignments;
    }
}
