namespace AutoScheduling3.TestData;

/// <summary>
/// 示例数据提供者，提供中文示例数据
/// </summary>
public class SampleDataProvider
{
    private readonly Random _random;

    // 中文姓名库
    private static readonly List<string> ChineseNames = new()
    {
        "张伟", "李娜", "王芳", "刘洋", "陈静",
        "杨军", "赵敏", "黄强", "周涛", "吴磊",
        "徐丽", "孙鹏", "马超", "朱婷", "胡斌",
        "郭亮", "林峰", "何敏", "高洋", "罗娟",
        "梁超", "宋杰", "唐丽", "韩冰", "冯强",
        "于洋", "董敏", "袁涛", "蒋磊", "潘婷"
    };

    // 技能名称库
    private static readonly List<string> SkillNames = new()
    {
        "安全检查", "设备维护", "应急处理", "监控操作",
        "巡逻执勤", "消防管理", "通讯联络", "文档记录",
        "车辆管理", "访客接待", "系统操作", "数据分析",
        "应急响应", "设施巡检", "安全培训"
    };

    // 哨位名称库
    private static readonly List<string> PositionNames = new()
    {
        "主门岗", "东门岗", "西门岗", "北门岗",
        "监控室", "巡逻岗", "停车场岗", "仓库岗",
        "办公楼岗", "生产区岗", "南门岗", "中央岗",
        "后勤岗", "安保岗", "值班室"
    };

    // 地点库
    private static readonly List<string> Locations = new()
    {
        "主入口", "东侧入口", "西侧入口", "北侧入口",
        "中央监控室", "园区巡逻路线", "地下停车场",
        "仓库区域", "办公楼大厅", "生产车间入口",
        "南侧入口", "中央广场", "后勤区域",
        "安保中心", "值班中心"
    };

    // 描述模板库
    private static readonly List<string> DescriptionTemplates = new()
    {
        "负责{0}的日常管理和监督工作",
        "执行{0}相关的安全检查任务",
        "维护{0}的正常运行秩序",
        "处理{0}的突发事件和应急情况",
        "监控{0}的安全状况",
        "协调{0}的各项工作安排",
        "确保{0}的安全和稳定",
        "管理{0}的人员进出"
    };

    // 要求说明模板库
    private static readonly List<string> RequirementTemplates = new()
    {
        "需要具备{0}技能，能够独立完成相关工作",
        "要求掌握{0}技能，有相关工作经验优先",
        "必须具有{0}技能，能够应对各种突发情况",
        "需要熟练掌握{0}技能，责任心强",
        "要求具备{0}技能，能够严格执行规章制度"
    };

    public SampleDataProvider(Random random)
    {
        _random = random;
    }

    /// <summary>
    /// 获取随机中文姓名
    /// </summary>
    public string GetRandomName()
    {
        return ChineseNames[_random.Next(ChineseNames.Count)];
    }

    /// <summary>
    /// 获取随机技能名称
    /// </summary>
    public string GetRandomSkillName()
    {
        return SkillNames[_random.Next(SkillNames.Count)];
    }

    /// <summary>
    /// 获取随机哨位名称
    /// </summary>
    public string GetRandomPositionName()
    {
        return PositionNames[_random.Next(PositionNames.Count)];
    }

    /// <summary>
    /// 获取随机地点
    /// </summary>
    public string GetRandomLocation()
    {
        return Locations[_random.Next(Locations.Count)];
    }

    /// <summary>
    /// 获取随机描述
    /// </summary>
    public string GetRandomDescription(string subject)
    {
        var template = DescriptionTemplates[_random.Next(DescriptionTemplates.Count)];
        return string.Format(template, subject);
    }

    /// <summary>
    /// 获取随机要求说明
    /// </summary>
    public string GetRandomRequirement(string skills)
    {
        var template = RequirementTemplates[_random.Next(RequirementTemplates.Count)];
        return string.Format(template, skills);
    }

    /// <summary>
    /// 获取时段名称
    /// </summary>
    public string GetTimeSlotName(int slot)
    {
        if (slot < 0 || slot > 11)
            throw new ArgumentOutOfRangeException(nameof(slot), "时段索引必须在0-11之间");

        var hours = slot * 2;
        return $"{hours:D2}:00-{(hours + 2):D2}:00";
    }

    /// <summary>
    /// 获取模板类型名称
    /// </summary>
    public string GetTemplateTypeName(string type)
    {
        return type switch
        {
            "regular" => "常规",
            "holiday" => "节假日",
            "special" => "特殊",
            _ => "未知"
        };
    }

    /// <summary>
    /// 获取所有可用的姓名（用于避免重复）
    /// </summary>
    public List<string> GetAllNames()
    {
        return new List<string>(ChineseNames);
    }

    /// <summary>
    /// 获取所有可用的技能名称（用于避免重复）
    /// </summary>
    public List<string> GetAllSkillNames()
    {
        return new List<string>(SkillNames);
    }

    /// <summary>
    /// 获取所有可用的哨位名称（用于避免重复）
    /// </summary>
    public List<string> GetAllPositionNames()
    {
        return new List<string>(PositionNames);
    }

    /// <summary>
    /// 获取所有可用的地点（用于避免重复）
    /// </summary>
    public List<string> GetAllLocations()
    {
        return new List<string>(Locations);
    }
}
