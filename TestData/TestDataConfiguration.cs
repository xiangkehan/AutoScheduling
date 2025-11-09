namespace AutoScheduling3.TestData;

/// <summary>
/// 测试数据生成器配置类
/// </summary>
public class TestDataConfiguration
{
    /// <summary>
    /// 技能数量
    /// </summary>
    public int SkillCount { get; set; } = 8;

    /// <summary>
    /// 人员数量
    /// </summary>
    public int PersonnelCount { get; set; } = 15;

    /// <summary>
    /// 哨位数量
    /// </summary>
    public int PositionCount { get; set; } = 10;

    /// <summary>
    /// 排班模板数量
    /// </summary>
    public int TemplateCount { get; set; } = 3;

    /// <summary>
    /// 定岗规则数量
    /// </summary>
    public int FixedAssignmentCount { get; set; } = 5;

    /// <summary>
    /// 手动指定数量
    /// </summary>
    public int ManualAssignmentCount { get; set; } = 8;

    /// <summary>
    /// 节假日配置数量
    /// </summary>
    public int HolidayConfigCount { get; set; } = 2;

    /// <summary>
    /// 随机种子（用于可重现的随机数生成）
    /// </summary>
    public int RandomSeed { get; set; } = 42;

    /// <summary>
    /// 创建默认配置（中等规模）
    /// </summary>
    public static TestDataConfiguration CreateDefault()
    {
        return new TestDataConfiguration
        {
            SkillCount = 8,
            PersonnelCount = 15,
            PositionCount = 10,
            TemplateCount = 3,
            FixedAssignmentCount = 5,
            ManualAssignmentCount = 8,
            HolidayConfigCount = 2,
            RandomSeed = 42
        };
    }

    /// <summary>
    /// 创建小规模配置（快速测试）
    /// </summary>
    public static TestDataConfiguration CreateSmall()
    {
        return new TestDataConfiguration
        {
            SkillCount = 5,
            PersonnelCount = 8,
            PositionCount = 6,
            TemplateCount = 2,
            FixedAssignmentCount = 3,
            ManualAssignmentCount = 5,
            HolidayConfigCount = 1,
            RandomSeed = 42
        };
    }

    /// <summary>
    /// 创建大规模配置（压力测试）
    /// </summary>
    public static TestDataConfiguration CreateLarge()
    {
        return new TestDataConfiguration
        {
            SkillCount = 15,
            PersonnelCount = 30,
            PositionCount = 20,
            TemplateCount = 5,
            FixedAssignmentCount = 10,
            ManualAssignmentCount = 15,
            HolidayConfigCount = 3,
            RandomSeed = 42
        };
    }

    /// <summary>
    /// 验证配置的合理性
    /// </summary>
    public void Validate()
    {
        if (SkillCount < 1)
            throw new ArgumentException("至少需要生成1个技能", nameof(SkillCount));

        if (PersonnelCount < 1)
            throw new ArgumentException("至少需要生成1个人员", nameof(PersonnelCount));

        if (PositionCount < 1)
            throw new ArgumentException("至少需要生成1个哨位", nameof(PositionCount));

        if (TemplateCount < 1)
            throw new ArgumentException("至少需要生成1个排班模板", nameof(TemplateCount));

        if (FixedAssignmentCount < 0)
            throw new ArgumentException("定岗规则数量不能为负数", nameof(FixedAssignmentCount));

        if (ManualAssignmentCount < 0)
            throw new ArgumentException("手动指定数量不能为负数", nameof(ManualAssignmentCount));

        if (HolidayConfigCount < 1)
            throw new ArgumentException("至少需要生成1个节假日配置", nameof(HolidayConfigCount));

        // 验证数量上限
        if (SkillCount > 50)
            throw new ArgumentException("技能数量不能超过50", nameof(SkillCount));

        if (PersonnelCount > 100)
            throw new ArgumentException("人员数量不能超过100", nameof(PersonnelCount));

        if (PositionCount > 50)
            throw new ArgumentException("哨位数量不能超过50", nameof(PositionCount));

        if (TemplateCount > 20)
            throw new ArgumentException("排班模板数量不能超过20", nameof(TemplateCount));

        if (FixedAssignmentCount > 50)
            throw new ArgumentException("定岗规则数量不能超过50", nameof(FixedAssignmentCount));

        if (ManualAssignmentCount > 100)
            throw new ArgumentException("手动指定数量不能超过100", nameof(ManualAssignmentCount));

        if (HolidayConfigCount > 10)
            throw new ArgumentException("节假日配置数量不能超过10", nameof(HolidayConfigCount));
    }
}
