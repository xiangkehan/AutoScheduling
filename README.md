# AutoScheduling3 自动排班系统

## 项目概述

AutoScheduling3 是一个基于约束满足和贪心算法的智能排班系统，专为哨位人员排班设计。系统采用 WPF 桌面应用架构，使用 SQLite 作为数据存储，实现了复杂的排班约束处理和优化算法。

## 核心功能

### 1. 数据管理
- **人员管理**：维护人员信息、技能、可用性状态
- **哨位管理**：管理哨位位置、要求、技能需求
- **技能管理**：定义和管理技能体系
- **约束配置**：定岗规则、手动指定、休息日配置

### 2. 排班算法
- **硬约束**：
  - 夜哨唯一：同一晚上只能上一个夜哨
  - 时段不连续：相邻时段不能连续上哨
  - 人员可用性：仅可用人员参与排班
  - 定岗限制：特定人员只能在指定哨位/时段
  - 技能匹配：人员必须具备哨位所需技能
  - 单人上哨：每个哨位每个时段仅一人
  - 一人一哨：同一人员同一时段仅一个哨位
  - 手动指定：用户预先指定的固定分配

- **软约束优化**：
  - 充分休息：优先分配休息时间长的人员
  - 时段平衡：均衡各时段的分配
  - 休息日平衡：公平分配休息日班次

### 3. 历史管理
- 排班历史记录
- 缓冲区机制（允许预览后确认）
- 历史数据延续（跨周期约束处理）

## 技术架构

```
AutoScheduling3/
├── Models/                          # 数据模型层
│   ├── Personal.cs                 # 人员模型
│   ├── PositionLocation.cs         # 哨位模型
│   ├── Schedule.cs                 # 排班表模型
│   ├── SingleShift.cs              # 单次班次模型
│   ├── Skill.cs                    # 技能模型
│   ├── PersonScoreState.cs         # 人员评分状态
│   └── Constraints/                # 约束配置模型
│       ├── HolidayConfig.cs        # 休息日配置
│       ├── FixedPositionRule.cs    # 定岗规则
│       └── ManualAssignment.cs     # 手动指定
│
├── Data/                           # 数据访问层
│   ├── PersonalRepository.cs       # 人员数据访问
│   ├── PositionLocationRepository.cs # 哨位数据访问
│   ├── SchedulingRepository.cs     # 排班数据访问
│   ├── SkillRepository.cs          # 技能数据访问
│   └── ConstraintRepository.cs     # 约束数据访问
│
├── History/                        # 历史管理层
│   └── HistoryManagement.cs        # 历史记录管理
│
├── SchedulingEngine/               # 算法引擎层
│   ├── Core/                       # 核心组件
│   │   ├── FeasibilityTensor.cs    # 可行性张量
│   │   ├── IHardConstraint.cs      # 硬约束接口
│   │   └── SchedulingContext.cs    # 调度上下文
│   └── GreedyScheduler.cs          # 贪心调度器
│
└── Services/                       # 业务服务层
    └── SchedulingService.cs        # 统一服务入口
```

## 快速开始

### 1. 初始化数据库

```csharp
using AutoScheduling3.Services;

// 创建服务实例
var service = new SchedulingService("scheduling.db");

// 初始化所有数据库表
await service.InitializeAsync();
```

### 2. 添加基础数据

```csharp
// 添加技能
var skill1 = await service.AddSkillAsync(new Skill 
{ 
    Name = "基础哨位", 
    Description = "基础哨位值班技能" 
});

// 添加哨位
var position1 = await service.AddPositionAsync(new PositionLocation
{
    Name = "1号哨位",
    Location = "东门",
    Description = "主入口哨位",
    Requirements = "需要基础哨位技能",
    RequiredSkillIds = new List<int> { skill1 }
});

// 添加人员
var person1 = await service.AddPersonalAsync(new Personal
{
    Name = "张三",
    PositionId = 1,
    SkillIds = new List<int> { skill1 },
    IsAvailable = true,
    IsRetired = false
});
```

### 3. 配置休息日

```csharp
var holidayConfig = new HolidayConfig
{
    ConfigName = "2024年休息日配置",
    EnableWeekendRule = true,
    WeekendDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
    LegalHolidays = new List<DateTime>
    {
        new DateTime(2024, 1, 1),  // 元旦
        new DateTime(2024, 10, 1)  // 国庆
    },
    IsActive = true
};

await service.AddHolidayConfigAsync(holidayConfig);
```

### 4. 执行排班

```csharp
// 执行排班
var schedule = await service.ExecuteSchedulingAsync(
    personalIds: new List<int> { 1, 2, 3, 4, 5 },  // 参与人员
    positionIds: new List<int> { 1, 2, 3 },        // 参与哨位
    startDate: new DateTime(2024, 1, 1),
    endDate: new DateTime(2024, 1, 7),
    useActiveHolidayConfig: true
);

Console.WriteLine($"生成排班表：{schedule.Title}");
Console.WriteLine($"班次总数：{schedule.Shifts.Count}");

// 查看缓冲区中的排班
var bufferSchedules = await service.GetBufferSchedulesAsync();
foreach (var (bufferSchedule, createTime, bufferId) in bufferSchedules)
{
    Console.WriteLine($"缓冲区排班 {bufferId}: {bufferSchedule.Title} (创建于 {createTime})");
    
    // 确认该排班
    await service.ConfirmSchedulingAsync(bufferId);
}
```

### 5. 查看排班结果

```csharp
// 查看已确认的历史排班
var historySchedules = await service.GetHistorySchedulesAsync();
foreach (var (historySchedule, confirmTime) in historySchedules)
{
    Console.WriteLine($"\n排班表：{historySchedule.Title}");
    Console.WriteLine($"确认时间：{confirmTime}");
    
    foreach (var shift in historySchedule.Shifts.Take(10)) // 显示前10个班次
    {
        Console.WriteLine($"  哨位{shift.PositionId} - 人员{shift.PersonalId}: {shift.StartTime:yyyy-MM-dd HH:mm} - {shift.EndTime:HH:mm}");
    }
}
```

## 时段定义

系统将一天分为12个时段，每个时段2小时：

| 时段 | 时间范围 | 说明 |
|-----|---------|------|
| 0 | 00:00-02:00 | 凌晨 (夜哨) |
| 1 | 02:00-04:00 | 深夜 (夜哨) |
| 2 | 04:00-06:00 | 黎明 (夜哨) |
| 3 | 06:00-08:00 | 早晨 |
| 4 | 08:00-10:00 | 上午1 |
| 5 | 10:00-12:00 | 上午2 |
| 6 | 12:00-14:00 | 中午 |
| 7 | 14:00-16:00 | 下午1 |
| 8 | 16:00-18:00 | 下午2 |
| 9 | 18:00-20:00 | 傍晚 |
| 10 | 20:00-22:00 | 晚间 |
| 11 | 22:00-24:00 | 夜间 (夜哨) |

**夜哨定义**：时段 11、0、1、2（22:00-06:00）

## 高级功能

### 定岗规则

限制特定人员只能在指定哨位或时段上岗：

```csharp
var rule = new FixedPositionRule
{
    PersonalId = 1,
    AllowedPositionIds = new List<int> { 1, 2 },  // 只能在1号和2号哨位
    AllowedPeriods = new List<int> { 0, 1, 2, 11 }, // 只能在夜哨时段
    IsEnabled = true,
    Description = "张三仅夜哨"
};

await service.AddFixedPositionRuleAsync(rule);
```

### 手动指定

预先指定某些特定的分配：

```csharp
var assignment = new ManualAssignment
{
    PositionId = 1,
    PeriodIndex = 11,  // 夜间时段
    PersonalId = 1,
    Date = new DateTime(2024, 1, 1),
    IsEnabled = true,
    Remarks = "特殊安排"
};

await service.AddManualAssignmentAsync(assignment);
```

## 算法原理

### 核心思想
系统采用基于**可行性张量**的贪心算法：

1. **可行性张量**：三维布尔数组 `[哨位][时段][人员]`，表示每个分配方案的可行性
2. **约束处理**：通过位运算快速应用硬约束，更新张量
3. **贪心选择**：在可行人员中，选择软约束评分最高的人员
4. **动态更新**：每次分配后，立即更新约束张量

### 软约束评分
```
总分 = 充分休息得分 + 时段平衡得分 + (休息日平衡得分)
     = RecentShiftInterval + PeriodIntervals[当前时段] + (RecentHolidayInterval)
```

数值越大，优先级越高。

## 未来扩展

设计预留了扩展接口，可以在未来实现：

1. **遗传算法优化**：在贪心初始解基础上，使用遗传算法进一步优化
2. **多目标优化**：支持软约束权重配置
3. **回溯机制**：在无解时自动放宽约束重试
4. **更复杂的约束**：如连续工作天数限制、最小/最大班次数等

## 技术栈

- **.NET / C#**：核心开发语言
- **WPF**：桌面应用界面框架
- **SQLite**：轻量级数据库
- **Microsoft.Data.Sqlite**：数据库访问库
- **System.Text.Json**：JSON序列化

## 许可证

本项目仅供学习和内部使用。

## 贡献

欢迎提出问题和建议。

---

**开发时间**：2024年
**版本**：v3.0
