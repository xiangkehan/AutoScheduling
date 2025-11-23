# 设计文档

## 概述

本设计文档描述了排班冲突管理系统的完善方案。该系统将在现有冲突检测基础上，提供全面的冲突检测、详细信息展示、定位、修复建议和报告导出功能，帮助用户快速识别和解决排班问题。

## UI 布局设计（ASCII 图）

### 增强的冲突面板布局

```
┌─────────────────────────────────────────────────────────┐
│  冲突管理面板                                [关闭]     │
├─────────────────────────────────────────────────────────┤
│  ┌─ 冲突统计 ─────────────────────────────────────┐   │
│  │  ⚠ 硬约束冲突: 3                              │   │
│  │  ⚠ 软约束冲突: 5                              │   │
│  │  ℹ 未分配时段: 12                             │   │
│  │  ✓ 已忽略: 2                                  │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
│  ┌─ 筛选和排序 ───────────────────────────────────┐   │
│  │  类型: [全部▼]  严重程度: [全部▼]            │   │
│  │  排序: [按类型▼]  搜索: [人员/哨位...]       │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
│  ┌─ 冲突列表 ─────────────────────────────────────┐   │
│  │  ┌─ 硬约束冲突 ──────────────────────────┐    │   │
│  │  │  ⚠ 技能不匹配                         │    │   │
│  │  │  张三 → 哨位A (2024-11-23 08:00)      │    │   │
│  │  │  缺少必需技能: 高级哨位                │    │   │
│  │  │  [定位] [修复]                        │    │   │
│  │  │                                        │    │   │
│  │  │  ⚠ 人员不可用                         │    │   │
│  │  │  李四 → 哨位B (2024-11-23 10:00)      │    │   │
│  │  │  原因: 请假                           │    │   │
│  │  │  [定位] [修复]                        │    │   │
│  │  └────────────────────────────────────────┘    │   │
│  │                                                 │   │
│  │  ┌─ 软约束冲突 ──────────────────────────┐    │   │
│  │  │  ⚠ 休息时间不足                       │    │   │
│  │  │  王五 (2024-11-23)                    │    │   │
│  │  │  班次间隔: 6小时 (建议≥8小时)        │    │   │
│  │  │  [定位] [忽略] [修复]                 │    │   │
│  │  │                                        │    │   │
│  │  │  ⚠ 工作量不均衡                       │    │   │
│  │  │  赵六: 25班次 (平均18班次)            │    │   │
│  │  │  超出平均值: 38.9%                    │    │   │
│  │  │  [定位] [忽略] [修复]                 │    │   │
│  │  └────────────────────────────────────────┘    │   │
│  │                                                 │   │
│  │  ┌─ 未分配时段 ──────────────────────────┐    │   │
│  │  │  ℹ 哨位B - 2024-11-23 22:00-00:00     │    │   │
│  │  │  [定位] [手动分配]                    │    │   │
│  │  └────────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────────┘   │
│                                                          │
│  ┌─ 操作 ─────────────────────────────────────────┐   │
│  │  [全部忽略软约束] [导出冲突报告] [冲突趋势]  │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```


### 冲突修复对话框

```
┌─────────────────────────────────────────────────────────┐
│  冲突修复建议                                [关闭]     │
├─────────────────────────────────────────────────────────┤
│  ┌─ 冲突详情 ─────────────────────────────────────┐   │
│  │  类型: 技能不匹配                             │   │
│  │  人员: 张三                                   │   │
│  │  哨位: 哨位A                                  │   │
│  │  时间: 2024-11-23 08:00-10:00                 │   │
│  │  问题: 缺少必需技能"高级哨位"                 │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
│  ┌─ 修复方案 ─────────────────────────────────────┐   │
│  │  ○ 方案1: 替换为李四（推荐）                 │   │
│  │     • 技能完全匹配                            │   │
│  │     • 当前工作量: 18班次（适中）              │   │
│  │     • 无时间冲突                              │   │
│  │     影响: 无其他冲突                          │   │
│  │                                                │   │
│  │  ○ 方案2: 替换为王五                          │   │
│  │     • 技能完全匹配                            │   │
│  │     • 当前工作量: 16班次（较低）              │   │
│  │     ⚠ 休息时间可能不足                        │   │
│  │     影响: 可能产生1个软约束冲突               │   │
│  │                                                │   │
│  │  ○ 方案3: 取消此班次分配                      │   │
│  │     影响: 哨位A在此时段将无人值守             │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
│  [应用方案] [取消]                                      │
└─────────────────────────────────────────────────────────┘
```

### 冲突趋势视图

```
┌─────────────────────────────────────────────────────────┐
│  冲突趋势分析                                [关闭]     │
├─────────────────────────────────────────────────────────┤
│  ┌─ 时间范围 ─────────────────────────────────────┐   │
│  │  [2024-11-01] 至 [2024-11-30]  [应用]        │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
│  ┌─ 冲突数量趋势 ─────────────────────────────────┐   │
│  │  冲突数                                        │   │
│  │  15 │                                          │   │
│  │  12 │     ●                                    │   │
│  │   9 │   ●   ●                                  │   │
│  │   6 │ ●       ●   ●                            │   │
│  │   3 │           ●   ●   ●                      │   │
│  │   0 └─────────────────────────────────────     │   │
│  │     11/1  11/8  11/15 11/22 11/29            │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
│  ┌─ 冲突类型分布 ─────────────────────────────────┐   │
│  │  技能不匹配:     ████████░░ 35%               │   │
│  │  休息时间不足:   ██████░░░░ 25%               │   │
│  │  工作量不均:     ████░░░░░░ 20%               │   │
│  │  未分配时段:     ██░░░░░░░░ 10%               │   │
│  │  其他:           ██░░░░░░░░ 10%               │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
│  ┌─ 解决率统计 ───────────────────────────────────┐   │
│  │  总冲突数: 45                                  │   │
│  │  已解决: 32 (71.1%)                            │   │
│  │  已忽略: 8 (17.8%)                             │   │
│  │  待处理: 5 (11.1%)                             │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
│  [导出报告]                                             │
└─────────────────────────────────────────────────────────┘
```


## 架构设计

### 整体架构

系统采用 MVVM 架构模式，在现有 ScheduleResultViewModel 基础上扩展冲突管理功能：

```
┌─────────────────────────────────────────────────────────┐
│                    视图层 (View)                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │  ScheduleResultPage.xaml                         │  │
│  │  - 冲突面板 (SplitView)                          │  │
│  │  - 冲突列表 (ListView)                           │  │
│  │  - 冲突统计卡片                                  │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  ConflictResolutionDialog.xaml (新增)            │  │
│  │  - 修复方案选择                                  │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  ConflictTrendDialog.xaml (新增)                 │  │
│  │  - 趋势图表                                      │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                 视图模型层 (ViewModel)                  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  ScheduleResultViewModel (扩展)                  │  │
│  │  - 冲突数据管理                                  │  │
│  │  - 冲突筛选和排序                                │  │
│  │  - 冲突定位命令                                  │  │
│  │  - 冲突修复命令                                  │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  ScheduleResultViewModel.Conflicts.cs (新增)     │  │
│  │  - 冲突相关逻辑的 partial class                  │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                   服务层 (Service)                      │
│  ┌──────────────────────────────────────────────────┐  │
│  │  IConflictDetectionService (新增接口)            │  │
│  │  - DetectConflictsAsync()                        │  │
│  │  - GetConflictDetailsAsync()                     │  │
│  │  - GetResolutionSuggestionsAsync()               │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  ConflictDetectionService (新增实现)             │  │
│  │  - 多种冲突检测器                                │  │
│  │  - 冲突分类和优先级                              │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  IConflictResolutionService (新增接口)           │  │
│  │  - GenerateResolutionOptionsAsync()              │  │
│  │  - ApplyResolutionAsync()                        │  │
│  │  - ValidateResolutionAsync()                     │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  ConflictResolutionService (新增实现)            │  │
│  │  - 修复方案生成                                  │  │
│  │  - 影响评估                                      │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  IConflictReportService (新增接口)               │  │
│  │  - ExportConflictReportAsync()                   │  │
│  │  - GenerateTrendDataAsync()                      │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                  数据访问层 (Repository)                │
│  ┌──────────────────────────────────────────────────┐  │
│  │  现有 Repository (复用)                          │  │
│  │  - ScheduleRepository                            │  │
│  │  - PersonnelRepository                           │  │
│  │  - PositionRepository                            │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### 数据流

```
1. 加载排班结果
   ScheduleResultPage → ScheduleResultViewModel → SchedulingService
   
2. 自动检测冲突
   ScheduleResultViewModel → ConflictDetectionService → 返回冲突列表
   
3. 用户点击"定位"
   View → ViewModel.LocateConflictCommand → 高亮表格单元格
   
4. 用户点击"修复"
   View → ViewModel.FixConflictCommand → ConflictResolutionService
   → 生成修复方案 → 显示对话框
   
5. 应用修复方案
   Dialog → ViewModel.ApplyResolutionCommand → SchedulingService
   → 更新排班 → 重新检测冲突
   
6. 导出冲突报告
   View → ViewModel.ExportConflictReportCommand → ConflictReportService
   → 生成报告文件
```


## 组件和接口

### 1. 增强的冲突数据模型

#### ConflictDto (扩展现有)

```csharp
/// <summary>
/// 冲突/约束提示 DTO（扩展）
/// </summary>
public class ConflictDto
{
    /// <summary>
    /// 冲突ID（用于跟踪和忽略状态）
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 冲突类型：hard / soft / info / unassigned
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 具体冲突子类型
    /// </summary>
    public ConflictSubType SubType { get; set; }

    /// <summary>
    /// 冲突描述
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 详细描述（用于对话框显示）
    /// </summary>
    public string? DetailedMessage { get; set; }

    /// <summary>
    /// 相关哨位ID
    /// </summary>
    public int? PositionId { get; set; }

    /// <summary>
    /// 相关哨位名称
    /// </summary>
    public string? PositionName { get; set; }

    /// <summary>
    /// 相关人员ID
    /// </summary>
    public int? PersonnelId { get; set; }

    /// <summary>
    /// 相关人员姓名
    /// </summary>
    public string? PersonnelName { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 时段索引
    /// </summary>
    public int? PeriodIndex { get; set; }

    /// <summary>
    /// 相关班次ID列表（可能涉及多个班次）
    /// </summary>
    public List<int> RelatedShiftIds { get; set; } = new();

    /// <summary>
    /// 严重程度（1-5，5最严重）
    /// </summary>
    public int Severity { get; set; } = 3;

    /// <summary>
    /// 是否已忽略
    /// </summary>
    public bool IsIgnored { get; set; }

    /// <summary>
    /// 是否可修复
    /// </summary>
    public bool IsFixable { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
```

#### ConflictSubType 枚举

```csharp
/// <summary>
/// 冲突子类型
/// </summary>
public enum ConflictSubType
{
    // 硬约束冲突
    SkillMismatch,              // 技能不匹配
    PersonnelUnavailable,       // 人员不可用
    DuplicateAssignment,        // 重复分配
    
    // 软约束冲突
    InsufficientRest,           // 休息时间不足
    ExcessiveWorkload,          // 工作量过大
    WorkloadImbalance,          // 工作量不均衡
    ConsecutiveOvertime,        // 连续工作超时
    
    // 信息提示
    UnassignedSlot,             // 未分配时段
    SuboptimalAssignment,       // 次优分配
    
    // 其他
    Unknown                     // 未知类型
}
```


#### ConflictStatistics 数据模型

```csharp
/// <summary>
/// 冲突统计信息
/// </summary>
public class ConflictStatistics
{
    /// <summary>
    /// 硬约束冲突数量
    /// </summary>
    public int HardConflictCount { get; set; }

    /// <summary>
    /// 软约束冲突数量
    /// </summary>
    public int SoftConflictCount { get; set; }

    /// <summary>
    /// 未分配时段数量
    /// </summary>
    public int UnassignedSlotCount { get; set; }

    /// <summary>
    /// 已忽略冲突数量
    /// </summary>
    public int IgnoredConflictCount { get; set; }

    /// <summary>
    /// 总冲突数量
    /// </summary>
    public int TotalConflictCount => HardConflictCount + SoftConflictCount + UnassignedSlotCount;

    /// <summary>
    /// 按类型分组的冲突数量
    /// </summary>
    public Dictionary<ConflictSubType, int> ConflictsByType { get; set; } = new();
}
```

#### ConflictResolutionOption 数据模型

```csharp
/// <summary>
/// 冲突修复方案
/// </summary>
public class ConflictResolutionOption
{
    /// <summary>
    /// 方案ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 方案标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 方案描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 方案类型
    /// </summary>
    public ResolutionType Type { get; set; }

    /// <summary>
    /// 是否推荐
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// 优点列表
    /// </summary>
    public List<string> Pros { get; set; } = new();

    /// <summary>
    /// 缺点/警告列表
    /// </summary>
    public List<string> Cons { get; set; } = new();

    /// <summary>
    /// 影响描述
    /// </summary>
    public string Impact { get; set; } = string.Empty;

    /// <summary>
    /// 预期产生的新冲突数量
    /// </summary>
    public int ExpectedNewConflicts { get; set; }

    /// <summary>
    /// 方案数据（用于应用修复）
    /// </summary>
    public object? ResolutionData { get; set; }
}
```

#### ResolutionType 枚举

```csharp
/// <summary>
/// 修复方案类型
/// </summary>
public enum ResolutionType
{
    ReplacePersonnel,       // 替换人员
    RemoveAssignment,       // 取消分配
    AdjustTime,             // 调整时间
    ReassignShifts,         // 重新分配班次
    IgnoreConflict,         // 忽略冲突
    ManualFix               // 手动修复
}
```


### 2. 服务接口

#### IConflictDetectionService

```csharp
/// <summary>
/// 冲突检测服务接口
/// </summary>
public interface IConflictDetectionService
{
    /// <summary>
    /// 检测排班中的所有冲突
    /// </summary>
    /// <param name="schedule">排班数据</param>
    /// <returns>冲突列表</returns>
    Task<List<ConflictDto>> DetectConflictsAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测特定班次的冲突
    /// </summary>
    /// <param name="shift">班次数据</param>
    /// <param name="schedule">完整排班数据</param>
    /// <returns>冲突列表</returns>
    Task<List<ConflictDto>> DetectShiftConflictsAsync(ShiftDto shift, ScheduleDto schedule);

    /// <summary>
    /// 获取冲突统计信息
    /// </summary>
    /// <param name="conflicts">冲突列表</param>
    /// <returns>统计信息</returns>
    ConflictStatistics GetConflictStatistics(List<ConflictDto> conflicts);

    /// <summary>
    /// 检测技能不匹配冲突
    /// </summary>
    Task<List<ConflictDto>> DetectSkillMismatchAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测人员不可用冲突
    /// </summary>
    Task<List<ConflictDto>> DetectPersonnelUnavailableAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测休息时间不足冲突
    /// </summary>
    Task<List<ConflictDto>> DetectInsufficientRestAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测工作量不均衡冲突
    /// </summary>
    Task<List<ConflictDto>> DetectWorkloadImbalanceAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测连续工作超时冲突
    /// </summary>
    Task<List<ConflictDto>> DetectConsecutiveOvertimeAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测未分配时段
    /// </summary>
    Task<List<ConflictDto>> DetectUnassignedSlotsAsync(ScheduleDto schedule);

    /// <summary>
    /// 检测重复分配冲突
    /// </summary>
    Task<List<ConflictDto>> DetectDuplicateAssignmentsAsync(ScheduleDto schedule);
}
```

#### IConflictResolutionService

```csharp
/// <summary>
/// 冲突修复服务接口
/// </summary>
public interface IConflictResolutionService
{
    /// <summary>
    /// 生成冲突修复方案
    /// </summary>
    /// <param name="conflict">冲突信息</param>
    /// <param name="schedule">排班数据</param>
    /// <returns>修复方案列表</returns>
    Task<List<ConflictResolutionOption>> GenerateResolutionOptionsAsync(
        ConflictDto conflict, 
        ScheduleDto schedule);

    /// <summary>
    /// 应用修复方案
    /// </summary>
    /// <param name="option">选定的修复方案</param>
    /// <param name="schedule">排班数据</param>
    /// <returns>更新后的排班数据</returns>
    Task<ScheduleDto> ApplyResolutionAsync(
        ConflictResolutionOption option, 
        ScheduleDto schedule);

    /// <summary>
    /// 验证修复方案的有效性
    /// </summary>
    /// <param name="option">修复方案</param>
    /// <param name="schedule">排班数据</param>
    /// <returns>是否有效及原因</returns>
    Task<(bool IsValid, string? Reason)> ValidateResolutionAsync(
        ConflictResolutionOption option, 
        ScheduleDto schedule);

    /// <summary>
    /// 评估修复方案的影响
    /// </summary>
    /// <param name="option">修复方案</param>
    /// <param name="schedule">排班数据</param>
    /// <returns>影响评估结果</returns>
    Task<ResolutionImpact> EvaluateImpactAsync(
        ConflictResolutionOption option, 
        ScheduleDto schedule);
}
```


#### IConflictReportService

```csharp
/// <summary>
/// 冲突报告服务接口
/// </summary>
public interface IConflictReportService
{
    /// <summary>
    /// 导出冲突报告
    /// </summary>
    /// <param name="conflicts">冲突列表</param>
    /// <param name="schedule">排班数据</param>
    /// <param name="format">导出格式（excel/pdf）</param>
    /// <returns>报告文件字节数组</returns>
    Task<byte[]> ExportConflictReportAsync(
        List<ConflictDto> conflicts, 
        ScheduleDto schedule, 
        string format);

    /// <summary>
    /// 生成冲突趋势数据
    /// </summary>
    /// <param name="scheduleId">排班ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>趋势数据</returns>
    Task<ConflictTrendData> GenerateTrendDataAsync(
        int scheduleId, 
        DateTime startDate, 
        DateTime endDate);
}
```

#### ResolutionImpact 数据模型

```csharp
/// <summary>
/// 修复方案影响评估
/// </summary>
public class ResolutionImpact
{
    /// <summary>
    /// 将解决的冲突数量
    /// </summary>
    public int ResolvedConflicts { get; set; }

    /// <summary>
    /// 可能产生的新冲突数量
    /// </summary>
    public int NewConflicts { get; set; }

    /// <summary>
    /// 影响的人员ID列表
    /// </summary>
    public List<int> AffectedPersonnelIds { get; set; } = new();

    /// <summary>
    /// 影响的哨位ID列表
    /// </summary>
    public List<int> AffectedPositionIds { get; set; } = new();

    /// <summary>
    /// 影响描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
```

#### ConflictTrendData 数据模型

```csharp
/// <summary>
/// 冲突趋势数据
/// </summary>
public class ConflictTrendData
{
    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 按日期统计的冲突数量
    /// </summary>
    public Dictionary<DateTime, int> ConflictsByDate { get; set; } = new();

    /// <summary>
    /// 按类型统计的冲突数量
    /// </summary>
    public Dictionary<ConflictSubType, int> ConflictsByType { get; set; } = new();

    /// <summary>
    /// 总冲突数
    /// </summary>
    public int TotalConflicts { get; set; }

    /// <summary>
    /// 已解决冲突数
    /// </summary>
    public int ResolvedConflicts { get; set; }

    /// <summary>
    /// 已忽略冲突数
    /// </summary>
    public int IgnoredConflicts { get; set; }

    /// <summary>
    /// 待处理冲突数
    /// </summary>
    public int PendingConflicts { get; set; }

    /// <summary>
    /// 解决率
    /// </summary>
    public double ResolutionRate => TotalConflicts > 0 
        ? (double)ResolvedConflicts / TotalConflicts 
        : 0;
}
```


### 3. ViewModel 扩展

#### ScheduleResultViewModel.Conflicts.cs (Partial Class)

```csharp
/// <summary>
/// ScheduleResultViewModel 的冲突管理部分
/// </summary>
public partial class ScheduleResultViewModel
{
    #region 冲突相关属性

    private ObservableCollection<ConflictDto> _allConflicts = new();
    public ObservableCollection<ConflictDto> AllConflicts
    {
        get => _allConflicts;
        set => SetProperty(ref _allConflicts, value);
    }

    private ObservableCollection<ConflictDto> _filteredConflicts = new();
    public ObservableCollection<ConflictDto> FilteredConflicts
    {
        get => _filteredConflicts;
        set => SetProperty(ref _filteredConflicts, value);
    }

    private ConflictStatistics? _conflictStatistics;
    public ConflictStatistics? ConflictStatistics
    {
        get => _conflictStatistics;
        set => SetProperty(ref _conflictStatistics, value);
    }

    private ConflictDto? _selectedConflict;
    public ConflictDto? SelectedConflict
    {
        get => _selectedConflict;
        set => SetProperty(ref _selectedConflict, value);
    }

    private string _conflictTypeFilter = "All";
    public string ConflictTypeFilter
    {
        get => _conflictTypeFilter;
        set
        {
            if (SetProperty(ref _conflictTypeFilter, value))
            {
                ApplyConflictFilters();
            }
        }
    }

    private string _conflictSeverityFilter = "All";
    public string ConflictSeverityFilter
    {
        get => _conflictSeverityFilter;
        set
        {
            if (SetProperty(ref _conflictSeverityFilter, value))
            {
                ApplyConflictFilters();
            }
        }
    }

    private string _conflictSortBy = "Type";
    public string ConflictSortBy
    {
        get => _conflictSortBy;
        set
        {
            if (SetProperty(ref _conflictSortBy, value))
            {
                ApplyConflictFilters();
            }
        }
    }

    private string _conflictSearchText = string.Empty;
    public string ConflictSearchText
    {
        get => _conflictSearchText;
        set
        {
            if (SetProperty(ref _conflictSearchText, value))
            {
                ApplyConflictFilters();
            }
        }
    }

    private HashSet<string> _highlightedCellKeys = new();
    public HashSet<string> HighlightedCellKeys
    {
        get => _highlightedCellKeys;
        set => SetProperty(ref _highlightedCellKeys, value);
    }

    #endregion

    #region 冲突相关命令

    public IAsyncRelayCommand RefreshConflictsCommand { get; }
    public IAsyncRelayCommand<ConflictDto> LocateConflictCommand { get; }
    public IAsyncRelayCommand<ConflictDto> IgnoreConflictCommand { get; }
    public IAsyncRelayCommand<ConflictDto> UnignoreConflictCommand { get; }
    public IAsyncRelayCommand<ConflictDto> FixConflictCommand { get; }
    public IAsyncRelayCommand IgnoreAllSoftConflictsCommand { get; }
    public IAsyncRelayCommand ExportConflictReportCommand { get; }
    public IAsyncRelayCommand ShowConflictTrendCommand { get; }
    public IRelayCommand ClearHighlightsCommand { get; }

    #endregion

    #region 冲突检测

    /// <summary>
    /// 检测所有冲突
    /// </summary>
    private async Task DetectConflictsAsync()
    {
        if (Schedule == null) return;

        try
        {
            var conflicts = await _conflictDetectionService.DetectConflictsAsync(Schedule);
            AllConflicts = new ObservableCollection<ConflictDto>(conflicts);
            ConflictStatistics = _conflictDetectionService.GetConflictStatistics(conflicts);
            ApplyConflictFilters();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("冲突检测失败", ex);
        }
    }

    /// <summary>
    /// 刷新冲突列表
    /// </summary>
    private async Task RefreshConflictsAsync()
    {
        await DetectConflictsAsync();
    }

    #endregion

    #region 冲突筛选和排序

    /// <summary>
    /// 应用冲突筛选
    /// </summary>
    private void ApplyConflictFilters()
    {
        var filtered = AllConflicts.AsEnumerable();

        // 按类型筛选
        if (ConflictTypeFilter != "All")
        {
            filtered = filtered.Where(c => c.Type == ConflictTypeFilter);
        }

        // 按严重程度筛选
        if (ConflictSeverityFilter != "All")
        {
            var severity = int.Parse(ConflictSeverityFilter);
            filtered = filtered.Where(c => c.Severity >= severity);
        }

        // 按搜索文本筛选
        if (!string.IsNullOrWhiteSpace(ConflictSearchText))
        {
            filtered = filtered.Where(c =>
                (c.PersonnelName?.Contains(ConflictSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.PositionName?.Contains(ConflictSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                c.Message.Contains(ConflictSearchText, StringComparison.OrdinalIgnoreCase));
        }

        // 排序
        filtered = ConflictSortBy switch
        {
            "Type" => filtered.OrderBy(c => c.Type).ThenBy(c => c.SubType),
            "Date" => filtered.OrderBy(c => c.StartTime),
            "Severity" => filtered.OrderByDescending(c => c.Severity),
            _ => filtered
        };

        FilteredConflicts = new ObservableCollection<ConflictDto>(filtered);
    }

    #endregion
}
```


## 错误处理

### 错误场景

1. **冲突检测失败**
   - 原因：数据不完整、服务异常
   - 处理：显示错误提示，允许重试

2. **修复方案生成失败**
   - 原因：无可用人员、约束冲突
   - 处理：显示警告，提供手动修复选项

3. **修复方案应用失败**
   - 原因：数据验证失败、并发冲突
   - 处理：回滚更改，显示错误信息

4. **报告导出失败**
   - 原因：文件权限、磁盘空间不足
   - 处理：显示错误提示，建议更换路径

### 错误处理策略

```csharp
try
{
    // 执行冲突检测
    var conflicts = await _conflictDetectionService.DetectConflictsAsync(schedule);
}
catch (InvalidOperationException ex)
{
    // 业务逻辑错误
    await _dialogService.ShowWarningAsync("操作无效", ex.Message);
}
catch (DataException ex)
{
    // 数据错误
    await _dialogService.ShowErrorAsync("数据错误", ex);
}
catch (Exception ex)
{
    // 未知错误
    await _dialogService.ShowErrorAsync("系统错误", ex);
    // 记录日志
    _logger.LogError(ex, "冲突检测失败");
}
```

## 测试策略

### 单元测试

测试范围：
- 冲突检测服务的各个检测方法
- 冲突修复方案生成逻辑
- 冲突筛选和排序逻辑
- 数据模型验证

测试框架：xUnit + Moq

示例测试：
```csharp
[Fact]
public async Task DetectSkillMismatch_ShouldReturnConflict_WhenPersonnelLacksRequiredSkill()
{
    // Arrange
    var schedule = CreateTestSchedule();
    var service = new ConflictDetectionService(_personnelService, _positionService);

    // Act
    var conflicts = await service.DetectSkillMismatchAsync(schedule);

    // Assert
    Assert.NotEmpty(conflicts);
    Assert.Contains(conflicts, c => c.SubType == ConflictSubType.SkillMismatch);
}
```

### 集成测试

测试范围：
- 完整的冲突检测流程
- 修复方案应用和验证
- 报告导出功能

### UI 测试

测试范围：
- 冲突面板显示和交互
- 冲突定位功能
- 修复对话框操作


## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: 冲突检测完整性
*For any* 排班数据，如果存在技能不匹配、人员不可用、休息时间不足、工作量不均衡、连续工作超时、未分配时段或重复分配的情况，冲突检测服务应该识别出相应类型的冲突
**Validates: Requirements 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8**

### Property 2: 冲突分类正确性
*For any* 检测到的冲突列表，所有冲突都应该被正确分类为硬约束冲突（hard）、软约束冲突（soft）、信息提示（info）或未分配时段（unassigned）
**Validates: Requirements 1.9**

### Property 3: 冲突信息完整性
*For any* 检测到的冲突，冲突对象应该包含类型、描述、相关人员ID和姓名（如适用）、相关哨位ID和姓名（如适用）、日期和时段信息（如适用）
**Validates: Requirements 2.2, 2.3, 2.4, 2.5, 2.6**

### Property 4: 冲突统计准确性
*For any* 冲突列表，统计信息中的硬约束冲突数、软约束冲突数、未分配时段数应该与实际冲突列表中对应类型的冲突数量一致
**Validates: Requirements 2.1**

### Property 5: 冲突分组一致性
*For any* 冲突列表，按类型分组后，每个分组中的所有冲突都应该具有相同的冲突类型
**Validates: Requirements 2.8**

### Property 6: 冲突定位准确性
*For any* 选定的冲突，定位操作应该高亮显示该冲突相关的所有班次对应的单元格
**Validates: Requirements 3.1, 3.2, 3.3**

### Property 7: 高亮状态唯一性
*For any* 时刻，只有当前选定冲突相关的单元格应该被高亮显示，之前的高亮应该被清除
**Validates: Requirements 3.6**

### Property 8: 修复方案有效性
*For any* 冲突和生成的修复方案，应用该方案后应该解决原冲突，且不应该产生相同类型的新冲突
**Validates: Requirements 4.8, 4.9**

### Property 9: 修复方案影响评估准确性
*For any* 修复方案，影响评估中预期产生的新冲突数量应该与实际应用方案后检测到的新冲突数量一致
**Validates: Requirements 4.7**

### Property 10: 忽略状态持久性
*For any* 被标记为已忽略的冲突，在保存排班结果后重新加载，该冲突的忽略状态应该保持不变
**Validates: Requirements 5.7**

### Property 11: 硬约束不可忽略
*For any* 硬约束冲突，系统不应该允许执行忽略操作
**Validates: Requirements 5.6**

### Property 12: 冲突报告完整性
*For any* 导出的冲突报告，应该包含冲突统计摘要、所有冲突的详细信息、排班基本信息，且冲突应该按类型分组
**Validates: Requirements 6.2, 6.3, 6.4, 6.5**

### Property 13: 实时更新响应性
*For any* 排班修改操作，系统应该自动重新执行冲突检测，且冲突列表应该反映最新的冲突状态
**Validates: Requirements 7.1, 7.2, 7.3, 7.4**

### Property 14: 冲突筛选正确性
*For any* 筛选条件（类型、严重程度、搜索文本），筛选后的冲突列表中的所有冲突都应该满足筛选条件
**Validates: Requirements 8.2, 8.4, 8.7**

### Property 15: 冲突排序一致性
*For any* 排序方式（按类型、按日期、按严重程度），排序后的冲突列表应该按照指定的排序规则有序排列
**Validates: Requirements 8.6**

### Property 16: 视觉提示准确性
*For any* 表格单元格，如果对应的班次有硬约束冲突应显示红色图标，有软约束冲突应显示黄色图标，未分配应显示灰色图标
**Validates: Requirements 9.1, 9.2, 9.3**

### Property 17: 趋势统计准确性
*For any* 时间范围，冲突趋势数据中的总冲突数应该等于已解决数、已忽略数和待处理数之和
**Validates: Requirements 10.4**


## 实现细节

### 1. 冲突检测实现

#### 技能不匹配检测

```csharp
public async Task<List<ConflictDto>> DetectSkillMismatchAsync(ScheduleDto schedule)
{
    var conflicts = new List<ConflictDto>();
    
    foreach (var shift in schedule.Shifts)
    {
        // 获取哨位要求的技能
        var position = await _positionService.GetByIdAsync(shift.PositionId);
        var requiredSkills = position.RequiredSkills;
        
        // 获取人员拥有的技能
        var personnel = await _personnelService.GetByIdAsync(shift.PersonnelId);
        var personnelSkills = personnel.Skills;
        
        // 检查是否缺少必需技能
        var missingSkills = requiredSkills.Except(personnelSkills).ToList();
        
        if (missingSkills.Any())
        {
            conflicts.Add(new ConflictDto
            {
                Type = "hard",
                SubType = ConflictSubType.SkillMismatch,
                Message = $"{personnel.Name} 缺少必需技能: {string.Join(", ", missingSkills)}",
                PersonnelId = shift.PersonnelId,
                PersonnelName = shift.PersonnelName,
                PositionId = shift.PositionId,
                PositionName = shift.PositionName,
                StartTime = shift.StartTime,
                PeriodIndex = shift.PeriodIndex,
                RelatedShiftIds = new List<int> { shift.Id },
                Severity = 5
            });
        }
    }
    
    return conflicts;
}
```

#### 休息时间不足检测

```csharp
public async Task<List<ConflictDto>> DetectInsufficientRestAsync(ScheduleDto schedule)
{
    var conflicts = new List<ConflictDto>();
    var minRestHours = 8; // 最小休息时间（小时）
    
    // 按人员分组
    var shiftsByPersonnel = schedule.Shifts
        .GroupBy(s => s.PersonnelId)
        .ToList();
    
    foreach (var group in shiftsByPersonnel)
    {
        var shifts = group.OrderBy(s => s.StartTime).ToList();
        
        for (int i = 0; i < shifts.Count - 1; i++)
        {
            var currentShift = shifts[i];
            var nextShift = shifts[i + 1];
            
            var restHours = (nextShift.StartTime - currentShift.EndTime).TotalHours;
            
            if (restHours < minRestHours)
            {
                conflicts.Add(new ConflictDto
                {
                    Type = "soft",
                    SubType = ConflictSubType.InsufficientRest,
                    Message = $"{currentShift.PersonnelName} 的班次间隔仅 {restHours:F1} 小时（建议 ≥{minRestHours} 小时）",
                    PersonnelId = currentShift.PersonnelId,
                    PersonnelName = currentShift.PersonnelName,
                    StartTime = currentShift.StartTime,
                    RelatedShiftIds = new List<int> { currentShift.Id, nextShift.Id },
                    Severity = 3
                });
            }
        }
    }
    
    return conflicts;
}
```

### 2. 修复方案生成实现

#### 技能不匹配修复

```csharp
public async Task<List<ConflictResolutionOption>> GenerateSkillMismatchResolutionsAsync(
    ConflictDto conflict, 
    ScheduleDto schedule)
{
    var options = new List<ConflictResolutionOption>();
    
    // 获取该时段的班次
    var shift = schedule.Shifts.First(s => s.Id == conflict.RelatedShiftIds[0]);
    
    // 查找具有匹配技能的可用人员
    var position = await _positionService.GetByIdAsync(shift.PositionId);
    var allPersonnel = await _personnelService.GetAllAsync();
    
    var suitablePersonnel = allPersonnel
        .Where(p => p.Id != shift.PersonnelId) // 排除当前人员
        .Where(p => position.RequiredSkills.All(skill => p.Skills.Contains(skill))) // 技能匹配
        .Where(p => IsPersonnelAvailable(p, shift, schedule)) // 时间可用
        .OrderBy(p => GetPersonnelWorkload(p.Id, schedule)) // 按工作量排序
        .ToList();
    
    // 为每个合适的人员生成替换方案
    foreach (var personnel in suitablePersonnel.Take(3)) // 最多3个方案
    {
        var workload = GetPersonnelWorkload(personnel.Id, schedule);
        var impact = await EvaluateReplacementImpact(shift, personnel.Id, schedule);
        
        options.Add(new ConflictResolutionOption
        {
            Title = $"替换为 {personnel.Name}",
            Description = $"将此班次分配给 {personnel.Name}",
            Type = ResolutionType.ReplacePersonnel,
            IsRecommended = options.Count == 0, // 第一个方案为推荐
            Pros = new List<string>
            {
                "技能完全匹配",
                $"当前工作量: {workload} 班次",
                impact.NewConflicts == 0 ? "无时间冲突" : null
            }.Where(s => s != null).ToList(),
            Cons = impact.NewConflicts > 0 
                ? new List<string> { $"可能产生 {impact.NewConflicts} 个新冲突" }
                : new List<string>(),
            Impact = impact.Description,
            ExpectedNewConflicts = impact.NewConflicts,
            ResolutionData = new { NewPersonnelId = personnel.Id }
        });
    }
    
    // 添加取消分配方案
    options.Add(new ConflictResolutionOption
    {
        Title = "取消此班次分配",
        Description = "移除此班次的人员分配",
        Type = ResolutionType.RemoveAssignment,
        IsRecommended = false,
        Pros = new List<string> { "立即解决冲突" },
        Cons = new List<string> { "哨位在此时段将无人值守" },
        Impact = "哨位覆盖率降低",
        ExpectedNewConflicts = 1, // 会产生未分配时段
        ResolutionData = null
    });
    
    return options;
}
```

### 3. 冲突定位实现

```csharp
private async Task LocateConflictAsync(ConflictDto conflict)
{
    try
    {
        // 清除之前的高亮
        HighlightedCellKeys.Clear();
        
        // 根据冲突相关的班次ID找到对应的单元格
        foreach (var shiftId in conflict.RelatedShiftIds)
        {
            var shift = Schedule.Shifts.FirstOrDefault(s => s.Id == shiftId);
            if (shift == null) continue;
            
            // 计算单元格键
            var row = GridData.Rows.FirstOrDefault(r => 
                r.Date.Date == shift.StartTime.Date && 
                r.PeriodIndex == shift.PeriodIndex);
            var col = GridData.Columns.FirstOrDefault(c => 
                c.PositionId == shift.PositionId);
            
            if (row != null && col != null)
            {
                var cellKey = $"{row.RowIndex}_{col.ColumnIndex}";
                HighlightedCellKeys.Add(cellKey);
            }
        }
        
        // 触发UI更新
        OnPropertyChanged(nameof(HighlightedCellKeys));
        
        // 滚动到第一个高亮单元格（需要在View中实现）
        // 通过事件或消息通知View滚动
    }
    catch (Exception ex)
    {
        await _dialogService.ShowErrorAsync("定位冲突失败", ex);
    }
}
```

### 4. 文件结构设计

新增文件：
```
Services/
  ├── ConflictDetectionService.cs          (~400行)
  ├── ConflictResolutionService.cs         (~350行)
  ├── ConflictReportService.cs             (~250行)
  └── Interfaces/
      ├── IConflictDetectionService.cs     (~80行)
      ├── IConflictResolutionService.cs    (~60行)
      └── IConflictReportService.cs        (~40行)

ViewModels/Scheduling/
  └── ScheduleResultViewModel.Conflicts.cs (~500行, partial class)

DTOs/
  ├── ConflictDto.cs (扩展现有)            (~150行)
  ├── ConflictStatistics.cs                (~50行)
  ├── ConflictResolutionOption.cs          (~80行)
  ├── ResolutionImpact.cs                  (~40行)
  ├── ConflictTrendData.cs                 (~60行)
  └── Enums/
      ├── ConflictSubType.cs               (~30行)
      └── ResolutionType.cs                (~20行)

Views/Scheduling/
  ├── ConflictResolutionDialog.xaml        (~200行)
  ├── ConflictResolutionDialog.xaml.cs     (~100行)
  ├── ConflictTrendDialog.xaml             (~250行)
  └── ConflictTrendDialog.xaml.cs          (~80行)
```

修改文件：
```
ViewModels/Scheduling/
  └── ScheduleResultViewModel.cs           (添加依赖注入和命令初始化, ~50行修改)

Views/Scheduling/
  └── ScheduleResultPage.xaml              (增强冲突面板UI, ~100行修改)

Extensions/
  └── ServiceCollectionExtensions.cs       (注册新服务, ~10行添加)
```

总计：
- 新增文件：~2,500行
- 修改文件：~160行
- 总代码量：~2,660行

### 5. 依赖注入配置

```csharp
// Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddConflictManagementServices(
    this IServiceCollection services)
{
    services.AddScoped<IConflictDetectionService, ConflictDetectionService>();
    services.AddScoped<IConflictResolutionService, ConflictResolutionService>();
    services.AddScoped<IConflictReportService, ConflictReportService>();
    
    return services;
}
```

## 性能考虑

1. **冲突检测优化**
   - 使用并行处理检测不同类型的冲突
   - 缓存人员和哨位信息，避免重复查询
   - 对大数据集使用分批处理

2. **UI 响应性**
   - 冲突检测使用后台任务，不阻塞UI
   - 冲突列表使用虚拟化，支持大量冲突
   - 高亮更新使用防抖，避免频繁刷新

3. **内存管理**
   - 及时清理不再使用的冲突数据
   - 使用弱引用缓存大对象
   - 限制修复方案数量，避免内存溢出

## 安全考虑

1. **数据验证**
   - 验证冲突数据的完整性和有效性
   - 验证修复方案的合法性
   - 防止SQL注入和XSS攻击

2. **权限控制**
   - 检查用户是否有权限修改排班
   - 记录所有修复操作的审计日志
   - 限制敏感操作的访问

3. **并发控制**
   - 使用乐观锁防止并发修改冲突
   - 检测数据版本，避免覆盖他人更改
   - 提供冲突解决机制

