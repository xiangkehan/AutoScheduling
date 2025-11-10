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
    /// <exception cref="ArgumentException">当配置参数不合理时抛出</exception>
    public void Validate()
    {
        var errors = new List<string>();

        // 验证最小值
        if (SkillCount < 1)
            errors.Add("技能数量至少需要1个，当前值: " + SkillCount);

        if (PersonnelCount < 1)
            errors.Add("人员数量至少需要1个，当前值: " + PersonnelCount);

        if (PositionCount < 1)
            errors.Add("哨位数量至少需要1个，当前值: " + PositionCount);

        if (TemplateCount < 1)
            errors.Add("排班模板数量至少需要1个，当前值: " + TemplateCount);

        if (FixedAssignmentCount < 0)
            errors.Add("定岗规则数量不能为负数，当前值: " + FixedAssignmentCount);

        if (ManualAssignmentCount < 0)
            errors.Add("手动指定数量不能为负数，当前值: " + ManualAssignmentCount);

        if (HolidayConfigCount < 1)
            errors.Add("节假日配置数量至少需要1个，当前值: " + HolidayConfigCount);

        // 验证最大值
        if (SkillCount > 50)
            errors.Add("技能数量不能超过50，当前值: " + SkillCount);

        if (PersonnelCount > 100)
            errors.Add("人员数量不能超过100，当前值: " + PersonnelCount);

        if (PositionCount > 50)
            errors.Add("哨位数量不能超过50，当前值: " + PositionCount);

        if (TemplateCount > 20)
            errors.Add("排班模板数量不能超过20，当前值: " + TemplateCount);

        if (FixedAssignmentCount > 50)
            errors.Add("定岗规则数量不能超过50，当前值: " + FixedAssignmentCount);

        if (ManualAssignmentCount > 100)
            errors.Add("手动指定数量不能超过100，当前值: " + ManualAssignmentCount);

        if (HolidayConfigCount > 10)
            errors.Add("节假日配置数量不能超过10，当前值: " + HolidayConfigCount);

        // 验证逻辑关系
        if (PersonnelCount < TemplateCount * 3)
        {
            errors.Add($"人员数量({PersonnelCount})应至少为排班模板数量({TemplateCount})的3倍，以确保每个模板有足够的人员");
        }

        if (PositionCount < TemplateCount * 2)
        {
            errors.Add($"哨位数量({PositionCount})应至少为排班模板数量({TemplateCount})的2倍，以确保每个模板有足够的哨位");
        }

        if (SkillCount < 2 && PersonnelCount > 5)
        {
            errors.Add($"当人员数量({PersonnelCount})较多时，建议至少生成2个技能，当前技能数量: {SkillCount}");
        }

        if (FixedAssignmentCount > PersonnelCount)
        {
            errors.Add($"定岗规则数量({FixedAssignmentCount})不应超过人员数量({PersonnelCount})");
        }

        if (ManualAssignmentCount > PositionCount * 12)
        {
            errors.Add($"手动指定数量({ManualAssignmentCount})不应超过哨位数量({PositionCount})乘以时段数(12)");
        }

        // 如果有错误，抛出异常
        if (errors.Count > 0)
        {
            var errorMessage = "配置验证失败:\n" + string.Join("\n", errors.Select((e, i) => $"  {i + 1}. {e}"));
            throw new ArgumentException(errorMessage);
        }
    }

    /// <summary>
    /// 验证配置并返回验证结果
    /// </summary>
    /// <returns>验证结果，包含是否有效和错误消息列表</returns>
    public ValidationResult ValidateWithResult()
    {
        var errors = new List<string>();

        // 验证最小值
        if (SkillCount < 1)
            errors.Add("技能数量至少需要1个，当前值: " + SkillCount);

        if (PersonnelCount < 1)
            errors.Add("人员数量至少需要1个，当前值: " + PersonnelCount);

        if (PositionCount < 1)
            errors.Add("哨位数量至少需要1个，当前值: " + PositionCount);

        if (TemplateCount < 1)
            errors.Add("排班模板数量至少需要1个，当前值: " + TemplateCount);

        if (FixedAssignmentCount < 0)
            errors.Add("定岗规则数量不能为负数，当前值: " + FixedAssignmentCount);

        if (ManualAssignmentCount < 0)
            errors.Add("手动指定数量不能为负数，当前值: " + ManualAssignmentCount);

        if (HolidayConfigCount < 1)
            errors.Add("节假日配置数量至少需要1个，当前值: " + HolidayConfigCount);

        // 验证最大值
        if (SkillCount > 50)
            errors.Add("技能数量不能超过50，当前值: " + SkillCount);

        if (PersonnelCount > 100)
            errors.Add("人员数量不能超过100，当前值: " + PersonnelCount);

        if (PositionCount > 50)
            errors.Add("哨位数量不能超过50，当前值: " + PositionCount);

        if (TemplateCount > 20)
            errors.Add("排班模板数量不能超过20，当前值: " + TemplateCount);

        if (FixedAssignmentCount > 50)
            errors.Add("定岗规则数量不能超过50，当前值: " + FixedAssignmentCount);

        if (ManualAssignmentCount > 100)
            errors.Add("手动指定数量不能超过100，当前值: " + ManualAssignmentCount);

        if (HolidayConfigCount > 10)
            errors.Add("节假日配置数量不能超过10，当前值: " + HolidayConfigCount);

        // 验证逻辑关系（警告级别）
        var warnings = new List<string>();

        if (PersonnelCount < TemplateCount * 3)
        {
            warnings.Add($"人员数量({PersonnelCount})应至少为排班模板数量({TemplateCount})的3倍，以确保每个模板有足够的人员");
        }

        if (PositionCount < TemplateCount * 2)
        {
            warnings.Add($"哨位数量({PositionCount})应至少为排班模板数量({TemplateCount})的2倍，以确保每个模板有足够的哨位");
        }

        if (SkillCount < 2 && PersonnelCount > 5)
        {
            warnings.Add($"当人员数量({PersonnelCount})较多时，建议至少生成2个技能，当前技能数量: {SkillCount}");
        }

        if (FixedAssignmentCount > PersonnelCount)
        {
            warnings.Add($"定岗规则数量({FixedAssignmentCount})不应超过人员数量({PersonnelCount})");
        }

        if (ManualAssignmentCount > PositionCount * 12)
        {
            warnings.Add($"手动指定数量({ManualAssignmentCount})不应超过哨位数量({PositionCount})乘以时段数(12)");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }
}

/// <summary>
/// 配置验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 配置是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误消息列表
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告消息列表
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 是否有警告
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>
    /// 获取所有消息的格式化字符串
    /// </summary>
    public string GetFormattedMessage()
    {
        var messages = new List<string>();

        if (Errors.Count > 0)
        {
            messages.Add("错误:");
            messages.AddRange(Errors.Select((e, i) => $"  {i + 1}. {e}"));
        }

        if (Warnings.Count > 0)
        {
            if (messages.Count > 0)
                messages.Add("");
            messages.Add("警告:");
            messages.AddRange(Warnings.Select((w, i) => $"  {i + 1}. {w}"));
        }

        return string.Join("\n", messages);
    }
}
