using AutoScheduling3.DTOs;
using AutoScheduling3.TestData.Helpers;

namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 技能分配器，根据哨位需求为人员分配技能
/// </summary>
public class SkillAssigner
{
    private readonly TestDataConfiguration _config;
    private readonly Random _random;
    private readonly SkillCooccurrenceAnalyzer _cooccurrenceAnalyzer;

    /// <summary>
    /// 初始化技能分配器
    /// </summary>
    /// <param name="config">测试数据配置</param>
    /// <param name="random">随机数生成器</param>
    public SkillAssigner(TestDataConfiguration config, Random random)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _cooccurrenceAnalyzer = new SkillCooccurrenceAnalyzer();
    }

    /// <summary>
    /// 根据哨位需求为人员分配技能
    /// </summary>
    /// <param name="personnel">人员列表（无技能）</param>
    /// <param name="positions">哨位列表（含技能需求）</param>
    /// <param name="skills">技能列表</param>
    /// <returns>更新后的人员列表（含技能）</returns>
    public List<PersonnelDto> AssignSkills(
        List<PersonnelDto> personnel,
        List<PositionDto> positions,
        List<SkillDto> skills)
    {
        if (personnel == null || personnel.Count == 0)
            throw new ArgumentException("人员列表不能为空", nameof(personnel));

        if (positions == null || positions.Count == 0)
            throw new ArgumentException("哨位列表不能为空", nameof(positions));

        if (skills == null || skills.Count == 0)
            throw new ArgumentException("技能列表不能为空", nameof(skills));

        Console.WriteLine($"[SkillAssigner] 开始技能分配：{personnel.Count}个人员，{positions.Count}个哨位，{skills.Count}个技能");

        // 为每个人员初始化技能集合（使用HashSet便于查找和去重）
        var personnelSkillSets = new Dictionary<int, HashSet<int>>();
        foreach (var person in personnel)
        {
            personnelSkillSets[person.Id] = new HashSet<int>();
        }

        // 阶段1：为每个哨位分配基本人员
        AssignBasicPersonnelToPositions(personnel, positions, personnelSkillSets);

        // 阶段2：创建多技能人员（30-40%）
        CreateMultiSkilledPersonnel(personnel, positions, skills, personnelSkillSets);

        // 补充技能：确保每个人员至少有1个技能
        EnsureMinimumSkills(personnel, skills, personnelSkillSets);

        // 转换为DTO格式
        ConvertSkillSetsToDto(personnel, skills, personnelSkillSets);

        // 输出统计信息
        PrintStatistics(personnel, positions, personnelSkillSets);

        return personnel;
    }

    /// <summary>
    /// 阶段1：为每个哨位分配基本人员
    /// </summary>
    private void AssignBasicPersonnelToPositions(
        List<PersonnelDto> personnel,
        List<PositionDto> positions,
        Dictionary<int, HashSet<int>> personnelSkillSets)
    {
        Console.WriteLine($"[SkillAssigner] 阶段1：为每个哨位分配基本人员（每个哨位至少{_config.MinPersonnelPerPosition}人）");

        // 按需求排序哨位：优先满足需求人员较少的哨位
        var sortedPositions = positions
            .OrderBy(p => CountAvailablePersonnel(personnel, p, personnelSkillSets))
            .ToList();

        foreach (var position in sortedPositions)
        {
            // 筛选可用且未退役的人员
            var availablePersonnel = personnel
                .Where(p => p.IsAvailable && !p.IsRetired)
                .ToList();

            // 按技能数量排序（优先选择技能少的人员，避免过度分配）
            var sortedPersonnel = availablePersonnel
                .OrderBy(p => personnelSkillSets[p.Id].Count)
                .ThenBy(_ => _random.Next()) // 相同技能数量时随机排序
                .ToList();

            int assignedCount = 0;

            foreach (var person in sortedPersonnel)
            {
                if (assignedCount >= _config.MinPersonnelPerPosition)
                    break;

                var personSkills = personnelSkillSets[person.Id];

                // 检查是否已经符合该哨位的技能要求
                if (position.RequiredSkillIds.All(skillId => personSkills.Contains(skillId)))
                {
                    assignedCount++;
                    continue;
                }

                // 为该人员添加哨位所需的所有技能
                foreach (var skillId in position.RequiredSkillIds)
                {
                    personSkills.Add(skillId);
                }

                assignedCount++;
            }

            Console.WriteLine($"  哨位 [{position.Name}] 需要技能 [{string.Join(", ", position.RequiredSkillNames)}]，已分配 {assignedCount} 人");
        }
    }

    /// <summary>
    /// 阶段2：创建多技能人员
    /// </summary>
    private void CreateMultiSkilledPersonnel(
        List<PersonnelDto> personnel,
        List<PositionDto> positions,
        List<SkillDto> skills,
        Dictionary<int, HashSet<int>> personnelSkillSets)
    {
        Console.WriteLine($"[SkillAssigner] 阶段2：创建多技能人员（目标比例：{_config.MultiSkilledPersonnelRate:P0}）");

        // 构建技能共现矩阵
        _cooccurrenceAnalyzer.BuildCooccurrenceMatrix(positions);

        // 筛选可用且未退役、技能数量少于3的人员
        var availablePersonnel = personnel
            .Where(p => p.IsAvailable && !p.IsRetired && personnelSkillSets[p.Id].Count < 3)
            .ToList();

        if (availablePersonnel.Count == 0)
        {
            Console.WriteLine("  没有可用人员可以添加额外技能");
            return;
        }

        // 使用配置中的多技能人员比例
        int targetCount = (int)(availablePersonnel.Count * _config.MultiSkilledPersonnelRate);

        Console.WriteLine($"  目标：为 {targetCount}/{availablePersonnel.Count} 个人员添加额外技能");

        // 随机打乱人员列表
        var shuffledPersonnel = availablePersonnel.OrderBy(_ => _random.Next()).ToList();

        int actualCount = 0;
        foreach (var person in shuffledPersonnel.Take(targetCount))
        {
            var personSkills = personnelSkillSets[person.Id];

            // 找出与该人员现有技能高频共现的技能
            var cooccurringSkills = _cooccurrenceAnalyzer.GetCooccurringSkills(
                personSkills,
                skills,
                topN: 3);

            // 过滤掉已有的技能
            var candidateSkills = cooccurringSkills
                .Where(s => !personSkills.Contains(s.Id))
                .ToList();

            // 如果没有高频共现的技能，随机选择一个
            if (candidateSkills.Count == 0)
            {
                candidateSkills = skills
                    .Where(s => !personSkills.Contains(s.Id))
                    .ToList();
            }

            // 添加1个技能（确保不超过3个）
            if (candidateSkills.Count > 0 && personSkills.Count < 3)
            {
                var newSkill = candidateSkills[_random.Next(candidateSkills.Count)];
                personSkills.Add(newSkill.Id);
                actualCount++;
            }
        }

        Console.WriteLine($"  实际为 {actualCount} 个人员添加了额外技能");
    }

    /// <summary>
    /// 补充技能：确保每个人员至少有1个技能
    /// </summary>
    private void EnsureMinimumSkills(
        List<PersonnelDto> personnel,
        List<SkillDto> skills,
        Dictionary<int, HashSet<int>> personnelSkillSets)
    {
        Console.WriteLine("[SkillAssigner] 补充技能：确保每个人员至少有1个技能");

        int supplementedCount = 0;

        foreach (var person in personnel)
        {
            var personSkills = personnelSkillSets[person.Id];

            if (personSkills.Count == 0)
            {
                // 随机分配1-2个技能
                int count = _random.Next(1, 3);
                var randomSkills = skills
                    .OrderBy(_ => _random.Next())
                    .Take(count)
                    .ToList();

                foreach (var skill in randomSkills)
                {
                    personSkills.Add(skill.Id);
                }

                supplementedCount++;
            }
            else if (personSkills.Count == 1)
            {
                // 50%概率再分配1个技能
                if (_random.NextDouble() < 0.5)
                {
                    var availableSkills = skills
                        .Where(s => !personSkills.Contains(s.Id))
                        .ToList();

                    if (availableSkills.Count > 0)
                    {
                        var randomSkill = availableSkills[_random.Next(availableSkills.Count)];
                        personSkills.Add(randomSkill.Id);
                    }
                }
            }
        }

        Console.WriteLine($"  为 {supplementedCount} 个无技能人员补充了技能");
    }

    /// <summary>
    /// 转换技能集合为DTO格式
    /// </summary>
    private void ConvertSkillSetsToDto(
        List<PersonnelDto> personnel,
        List<SkillDto> skills,
        Dictionary<int, HashSet<int>> personnelSkillSets)
    {
        var skillDict = skills.ToDictionary(s => s.Id, s => s);

        foreach (var person in personnel)
        {
            var personSkills = personnelSkillSets[person.Id];

            person.SkillIds = personSkills.OrderBy(id => id).ToList();
            person.SkillNames = personSkills
                .OrderBy(id => id)
                .Select(id => skillDict.TryGetValue(id, out var skill) ? skill.Name : $"未知技能{id}")
                .ToList();
        }
    }

    /// <summary>
    /// 输出统计信息
    /// </summary>
    private void PrintStatistics(
        List<PersonnelDto> personnel,
        List<PositionDto> positions,
        Dictionary<int, HashSet<int>> personnelSkillSets)
    {
        Console.WriteLine("\n[SkillAssigner] 技能分配统计：");

        // 人员技能分布
        var skillDistribution = personnelSkillSets.Values
            .GroupBy(skills => skills.Count)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        Console.WriteLine("  人员技能分布：");
        foreach (var kvp in skillDistribution)
        {
            double percentage = (double)kvp.Value / personnel.Count * 100;
            Console.WriteLine($"    {kvp.Key}个技能：{kvp.Value}人 ({percentage:F1}%)");
        }

        // 多技能人员比例
        int multiSkilledCount = personnelSkillSets.Values.Count(skills => skills.Count >= 2);
        double multiSkilledRate = (double)multiSkilledCount / personnel.Count * 100;
        Console.WriteLine($"  多技能人员（≥2个技能）：{multiSkilledCount}人 ({multiSkilledRate:F1}%)");

        // 每个哨位的可用人员数量
        Console.WriteLine("\n  各哨位可用人员数量：");
        foreach (var position in positions)
        {
            int availableCount = CountAvailablePersonnel(personnel, position, personnelSkillSets);
            string status = availableCount >= _config.MinPersonnelPerPosition ? "✓" : "✗";
            Console.WriteLine($"    {status} 哨位 [{position.Name}]：{availableCount}人（需求：{_config.MinPersonnelPerPosition}人）");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 计算哨位的可用人员数量
    /// </summary>
    private int CountAvailablePersonnel(
        List<PersonnelDto> personnel,
        PositionDto position,
        Dictionary<int, HashSet<int>> personnelSkillSets)
    {
        return personnel
            .Where(p => p.IsAvailable && !p.IsRetired)
            .Count(p => position.RequiredSkillIds.All(skillId => personnelSkillSets[p.Id].Contains(skillId)));
    }
}
