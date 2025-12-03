# 设计文档

## 概述

本设计文档描述了为贪心调度算法添加回溯机制的技术方案。回溯机制将使算法能够在遇到无解情况时撤销之前的决策并尝试其他分配方案，从而显著提高排班表的完整性。

### 设计目标

1. **提高完整性**: 减少未分配时段的数量，提高排班表的完整率
2. **保持约束**: 不放松任何硬约束或软约束的验证逻辑
3. **性能可控**: 通过配置参数控制回溯深度，避免性能显著下降
4. **无缝集成**: 与现有的MRV策略、约束验证器和评分系统无缝集成
5. **可观测性**: 提供详细的进度报告和统计信息

### 核心思想

当前的贪心算法使用MRV启发式策略选择候选人员最少的时段优先分配，但这种贪心选择可能导致后续时段无人可分配。回溯机制通过以下方式解决这个问题：

1. **记录决策历史**: 将每次分配决策及其上下文保存到分配栈中
2. **检测死胡同**: 当发现未分配且无候选人员的时段时，判定为死胡同
3. **智能回溯**: 撤销最近的分配决策，尝试该决策点的下一个候选人员
4. **递归探索**: 如果当前决策点的所有候选都失败，继续回溯到更早的决策点
5. **路径记忆**: 避免重复尝试已经失败的分配路径

## 架构

### 组件关系图

```
┌─────────────────────────────────────────────────────────────┐
│                    GreedyScheduler                          │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  ExecuteAsync()                                       │  │
│  │    ├─ PreprocessAsync()                              │  │
│  │    ├─ InitializeDailyScheduling()                    │  │
│  │    └─ PerformGreedyAssignmentsWithBacktracking() ◄───┼──┼─ 新增
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            │
                            ├─────────────────┐
                            │                 │
                            ▼                 ▼
              ┌──────────────────┐         ┌──────────────────┐
              │ BacktrackingEngine│         │  AssignmentStack │
              │                  │         │                  │
              │ - TryAssign()    │◄────────┤ - Push()         │
              │ - Backtrack()    │         │ - Pop()          │
              │ - DetectDeadEnd()│         │ - Peek()         │
              │ - SelectBacktrack│         │ - IsEmpty()      │
              │   Point()        │         │                  │
              └──────────────────┘         └──────────────────┘
                      │                            │
                      │                            │
                      ▼                            ▼
              ┌──────────────────┐         ┌──────────────────┐
              │ AssignmentRecord │         │  StateSnapshot   │
              │                  │         │                  │
              │ - PositionIdx    │         │ - TensorState    │
              │ - PeriodIdx      │         │ - MRVState       │
              │ - PersonIdx      │         │ - ContextState   │
              │ - Candidates[]   │         │                  │
              │ - StateSnapshot  │         │                  │
              └──────────────────┘         └──────────────────┘
```

### 数据流

1. **正常分配流程**:
   ```
   MRVStrategy.SelectNextSlot() 
     → BacktrackingEngine.TryAssign()
       → ConstraintValidator.Validate()
         → SoftConstraintCalculator.SelectBestPerson()
           → AssignmentStack.Push(record)
             → UpdateTensor & Context
   ```

2. **回溯流程**:
   ```
   DetectDeadEnd()
     → SelectBacktrackPoint()
       → AssignmentStack.Pop()
         → RestoreState(snapshot)
           → TryNextCandidate()
             → TryAssign() (递归)
   ```

## 组件和接口

### 1. BacktrackingEngine

回溯引擎是核心组件，负责管理回溯逻辑。

```csharp
public class BacktrackingEngine
{
    private readonly SchedulingContext _context;
    private readonly FeasibilityTensor _tensor;
    private readonly MRVStrategy _mrvStrategy;
    private readonly ConstraintValidator _constraintValidator;
    private readonly SoftConstraintCalculator _softConstraintCalculator;
    private readonly AssignmentStack _assignmentStack;
    private readonly BacktrackingConfig _config;
    
    // 回溯统计
    private int _backtrackCount;
    private int _maxBacktrackDepth;
    
    /// <summary>
    /// 尝试分配人员到指定位置，支持回溯
    /// </summary>
    public async Task<bool> TryAssignWithBacktracking(
        int positionIdx, 
        int periodIdx, 
        DateTime date,
        IProgress<SchedulingProgressReport>? progress,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// 检测是否遇到死胡同
    /// </summary>
    public bool DetectDeadEnd();
    
    /// <summary>
    /// 执行回溯操作
    /// </summary>
    public async Task<bool> Backtrack(
        DateTime date,
        IProgress<SchedulingProgressReport>? progress);
    
    /// <summary>
    /// 选择回溯点
    /// </summary>
    private AssignmentRecord SelectBacktrackPoint();
    
    /// <summary>
    /// 恢复状态快照
    /// </summary>
    private void RestoreState(StateSnapshot snapshot);
    
    /// <summary>
    /// 获取回溯统计信息
    /// </summary>
    public BacktrackingStatistics GetStatistics();
}
```

### 2. AssignmentStack

分配栈用于记录分配历史，支持回溯操作。

```csharp
public class AssignmentStack
{
    private readonly Stack<AssignmentRecord> _stack;
    private readonly int _maxDepth;
    
    /// <summary>
    /// 压入分配记录
    /// </summary>
    public void Push(AssignmentRecord record);
    
    /// <summary>
    /// 弹出最近的分配记录
    /// </summary>
    public AssignmentRecord? Pop();
    
    /// <summary>
    /// 查看栈顶记录但不弹出
    /// </summary>
    public AssignmentRecord? Peek();
    
    /// <summary>
    /// 检查栈是否为空
    /// </summary>
    public bool IsEmpty();
    
    /// <summary>
    /// 获取当前栈深度
    /// </summary>
    public int Depth { get; }
    
    /// <summary>
    /// 清空栈
    /// </summary>
    public void Clear();
}
```

### 3. AssignmentRecord

分配记录包含单次分配的完整信息。

```csharp
public class AssignmentRecord
{
    /// <summary>
    /// 哨位索引
    /// </summary>
    public int PositionIdx { get; set; }
    
    /// <summary>
    /// 时段索引
    /// </summary>
    public int PeriodIdx { get; set; }
    
    /// <summary>
    /// 分配的人员索引
    /// </summary>
    public int PersonIdx { get; set; }
    
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// 候选人员列表（按评分排序）
    /// </summary>
    public List<int> Candidates { get; set; }
    
    /// <summary>
    /// 当前尝试的候选人员索引
    /// </summary>
    public int CurrentCandidateIndex { get; set; }
    
    /// <summary>
    /// 状态快照
    /// </summary>
    public StateSnapshot Snapshot { get; set; }
    
    /// <summary>
    /// 是否还有未尝试的候选人员
    /// </summary>
    public bool HasMoreCandidates => CurrentCandidateIndex < Candidates.Count - 1;
    
    /// <summary>
    /// 获取下一个候选人员
    /// </summary>
    public int? GetNextCandidate();
}
```

### 4. StateSnapshot

状态快照用于保存分配前的系统状态。

```csharp
public class StateSnapshot
{
    /// <summary>
    /// 可行性张量的状态（使用写时复制）
    /// </summary>
    public byte[] TensorState { get; set; }
    
    /// <summary>
    /// MRV策略的候选计数
    /// </summary>
    public int[,] CandidateCounts { get; set; }
    
    /// <summary>
    /// MRV策略的分配标记
    /// </summary>
    public bool[,] AssignedFlags { get; set; }
    
    /// <summary>
    /// 调度上下文的分配记录
    /// </summary>
    public Dictionary<(int period, int position), int> Assignments { get; set; }
    
    /// <summary>
    /// 创建快照的时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
}
```

### 5. BacktrackingConfig

回溯配置类，扩展现有的GreedySchedulerConfig。

```csharp
public class BacktrackingConfig
{
    /// <summary>
    /// 是否启用回溯机制
    /// </summary>
    public bool EnableBacktracking { get; set; } = true;
    
    /// <summary>
    /// 最大回溯深度
    /// </summary>
    public int MaxBacktrackDepth { get; set; } = 50;
    
    /// <summary>
    /// 每个决策点保留的候选人员数量
    /// </summary>
    public int MaxCandidatesPerDecision { get; set; } = 5;
    
    /// <summary>
    /// 是否启用智能回溯点选择
    /// </summary>
    public bool EnableSmartBacktrackSelection { get; set; } = true;
    
    /// <summary>
    /// 是否启用路径记忆（避免重复尝试）
    /// </summary>
    public bool EnablePathMemory { get; set; } = true;
    
    /// <summary>
    /// 状态快照保存间隔（每N次分配保存一次完整快照）
    /// </summary>
    public int SnapshotInterval { get; set; } = 10;
    
    /// <summary>
    /// 内存使用阈值（MB），超过后降低回溯深度
    /// </summary>
    public long MemoryThresholdMB { get; set; } = 500;
    
    /// <summary>
    /// 是否记录回溯日志
    /// </summary>
    public bool LogBacktracking { get; set; } = true;
}
```

## 数据模型

### 回溯统计信息

```csharp
public class BacktrackingStatistics
{
    /// <summary>
    /// 总回溯次数
    /// </summary>
    public int TotalBacktracks { get; set; }
    
    /// <summary>
    /// 最大回溯深度
    /// </summary>
    public int MaxDepthReached { get; set; }
    
    /// <summary>
    /// 成功回溯次数
    /// </summary>
    public int SuccessfulBacktracks { get; set; }
    
    /// <summary>
    /// 失败回溯次数
    /// </summary>
    public int FailedBacktracks { get; set; }
    
    /// <summary>
    /// 平均回溯深度
    /// </summary>
    public double AverageBacktrackDepth { get; set; }
    
    /// <summary>
    /// 回溯耗时（毫秒）
    /// </summary>
    public long BacktrackingTimeMs { get; set; }
    
    /// <summary>
    /// 避免的重复路径数量
    /// </summary>
    public int AvoidedDuplicatePaths { get; set; }
}
```

### 扩展的进度报告

```csharp
// 扩展现有的 SchedulingStage 枚举
public enum SchedulingStage
{
    // ... 现有阶段 ...
    Backtracking,           // 新增：回溯中
    BacktrackingComplete,   // 新增：回溯完成
}

// 扩展现有的 SchedulingProgressReport
public class SchedulingProgressReport
{
    // ... 现有字段 ...
    
    /// <summary>
    /// 回溯统计信息
    /// </summary>
    public BacktrackingStatistics? BacktrackingStats { get; set; }
    
    /// <summary>
    /// 当前回溯深度
    /// </summary>
    public int CurrentBacktrackDepth { get; set; }
}
```


## 正确性属性

*属性是一个特征或行为，应该在系统的所有有效执行中保持为真——本质上是关于系统应该做什么的正式陈述。属性作为人类可读规范和机器可验证的正确性保证之间的桥梁。*

### 属性反思

在编写正确性属性之前，我们需要识别并消除冗余：

**潜在冗余分析**:
1. 属性1（回溯触发）和属性2（回溯行为）可以合并，因为触发回溯必然伴随回溯行为
2. 属性4（状态恢复）和属性5（往返一致性）有重叠，往返一致性已经包含了状态恢复的验证
3. 属性11（约束验证一致性）、属性12（评分一致性）和属性13（张量操作一致性）可以合并为一个综合的"系统一致性"属性
4. 属性15（完成排班满足硬约束）是所有排班算法的基本要求，不是回溯特有的，但仍需保留以确保回溯不破坏约束

**消除冗余后的属性集**:
- 保留属性1-3（回溯基本行为）
- 合并属性4-5为单一往返属性
- 保留属性6-10（配置和进度报告）
- 合并属性11-13为系统一致性属性
- 保留属性14-17（智能回溯和验证）

### 核心属性

**属性 1: 死胡同触发回溯**
*对于任何*调度场景，当检测到存在未分配且无候选人员的时段时，如果启用了回溯且未超过最大深度，系统应该启动回溯机制并尝试撤销最近的分配决策
**验证需求: 1.1, 1.2**

**属性 2: 回溯深度限制**
*对于任何*调度场景，当回溯深度达到配置的最大回溯深度时，系统应该停止回溯并返回部分结果，不应继续尝试更深的回溯
**验证需求: 1.3**

**属性 3: 成功回溯的完整性**
*对于任何*通过回溯成功完成的排班，返回的排班方案应该是完整的（所有时段都已分配），并且回溯次数应该被正确记录在统计信息中
**验证需求: 1.4**

**属性 4: 分配栈操作正确性**
*对于任何*分配操作，执行分配后分配栈的大小应该增加1，并且栈顶记录应该包含哨位索引、时段索引、人员索引、可行性张量快照和候选人员列表
**验证需求: 2.1, 2.2**

**属性 5: 回溯状态往返一致性**
*对于任何*分配操作，如果执行分配然后立即回溯，系统的可行性张量、MRV策略状态和调度上下文应该恢复到分配前的状态
**验证需求: 2.3, 2.4, 8.2**

**属性 6: 无解判定正确性**
*对于任何*调度场景，当分配栈为空且仍存在未分配时段时，系统应该判定为无解并返回部分结果
**验证需求: 2.5**

**属性 7: 配置参数有效性**
*对于任何*有效的配置参数（最大回溯深度、启用开关、候选人员数量），系统应该接受并正确应用这些配置；对于无效的配置参数，系统应该使用默认值并记录警告
**验证需求: 3.1, 3.2, 3.3, 3.5**

**属性 8: 禁用回溯的行为一致性**
*对于任何*调度场景，当回溯机制被禁用时，系统的行为应该与原始贪心算法完全一致
**验证需求: 3.4**

**属性 9: 回溯进度报告完整性**
*对于任何*发生回溯的调度过程，进度报告应该包含回溯启动通知、回溯次数、回溯深度，以及在排班完成时包含完整的回溯统计信息
**验证需求: 4.1, 4.2, 4.5**

**属性 10: 回溯失败报告完整性**
*对于任何*回溯失败的场景，系统应该报告失败原因和未分配时段的详细信息（包括哨位名称、时段索引、候选人员数量）
**验证需求: 4.3**

**属性 11: 系统组件一致性**
*对于任何*回溯和重新分配操作，系统应该使用相同的约束验证器、软约束计算器和张量操作方法，确保验证逻辑和评分逻辑的一致性
**验证需求: 1.5, 7.1, 7.2, 7.3, 7.4**

**属性 12: 智能回溯点选择**
*对于任何*需要回溯的场景，如果启用了智能回溯，系统应该分析导致死胡同的关键决策点，并优先回溯到对当前未分配时段影响最大的决策点
**验证需求: 5.1, 5.2**

**属性 13: 候选人员尝试顺序**
*对于任何*决策点，系统应该按照软约束评分从高到低的顺序尝试候选人员，当所有候选人员都失败时，应该继续回溯到更早的决策点
**验证需求: 5.3, 5.4**

**属性 14: 路径记忆避免重复**
*对于任何*启用了路径记忆的调度过程，系统不应该重复尝试已经失败的分配路径
**验证需求: 5.5**

**属性 15: 回溯后硬约束保持**
*对于任何*完成的排班（无论是否使用回溯），所有分配都应该满足硬约束（技能匹配、可用性、单人上哨、一人一哨、夜哨唯一、时段不连续）
**验证需求: 8.1**

**属性 16: 回溯诊断信息完整性**
*对于任何*报告无解的场景，系统应该提供详细的诊断信息，包括未分配时段列表、每个时段的候选人员数量、回溯历史和失败原因
**验证需求: 8.3**

**属性 17: 回溯日志记录**
*对于任何*启用了回溯日志的调度过程，系统应该记录每次回溯操作的详细信息，包括回溯时间、回溯深度、回溯原因和尝试的候选人员
**验证需求: 8.4**

**属性 18: 内存管理自适应**
*对于任何*调度过程，当内存使用超过配置的阈值时，系统应该自动降低回溯深度或禁用回溯，以避免内存溢出
**验证需求: 6.5**

**属性 19: 快照保存频率控制**
*对于任何*回溯深度较大的场景，系统应该按照配置的快照间隔保存状态快照，而不是每次分配都保存完整快照
**验证需求: 6.3**

## 错误处理

### 错误场景

1. **回溯深度超限**
   - 检测: 当回溯深度达到MaxBacktrackDepth时
   - 处理: 停止回溯，返回部分结果，记录警告
   - 用户反馈: 通过进度报告通知用户回溯深度已达上限

2. **内存不足**
   - 检测: 监控内存使用，当超过MemoryThresholdMB时
   - 处理: 自动降低回溯深度或禁用回溯
   - 用户反馈: 记录警告日志，通知用户内存限制

3. **无解场景**
   - 检测: 分配栈为空且仍有未分配时段
   - 处理: 返回部分结果，生成详细诊断报告
   - 用户反馈: 提供未分配时段列表和建议

4. **配置参数无效**
   - 检测: 在初始化时验证配置参数
   - 处理: 使用默认值替换无效参数
   - 用户反馈: 记录警告日志

5. **状态恢复失败**
   - 检测: 在恢复状态快照时验证数据完整性
   - 处理: 如果快照损坏，回退到更早的快照
   - 用户反馈: 记录错误日志，可能需要重新开始

### 异常类型

```csharp
/// <summary>
/// 回溯深度超限异常
/// </summary>
public class BacktrackDepthExceededException : Exception
{
    public int MaxDepth { get; }
    public int CurrentDepth { get; }
    
    public BacktrackDepthExceededException(int maxDepth, int currentDepth)
        : base($"回溯深度 {currentDepth} 超过最大限制 {maxDepth}")
    {
        MaxDepth = maxDepth;
        CurrentDepth = currentDepth;
    }
}

/// <summary>
/// 状态恢复失败异常
/// </summary>
public class StateRestorationException : Exception
{
    public StateSnapshot CorruptedSnapshot { get; }
    
    public StateRestorationException(StateSnapshot snapshot, string message)
        : base($"状态恢复失败: {message}")
    {
        CorruptedSnapshot = snapshot;
    }
}
```

## 测试策略

### 单元测试

单元测试用于验证各个组件的基本功能：

1. **AssignmentStack测试**
   - 测试Push/Pop操作的正确性
   - 测试栈深度限制
   - 测试空栈处理

2. **AssignmentRecord测试**
   - 测试候选人员管理
   - 测试GetNextCandidate逻辑
   - 测试HasMoreCandidates判断

3. **StateSnapshot测试**
   - 测试快照创建
   - 测试快照恢复
   - 测试写时复制机制

4. **BacktrackingEngine测试**
   - 测试死胡同检测
   - 测试回溯点选择
   - 测试状态恢复

### 属性测试

属性测试用于验证回溯机制在各种输入下的正确性。使用C#的FsCheck库进行属性测试。

**测试框架**: FsCheck (F#的QuickCheck移植版)
**测试配置**: 每个属性测试至少运行100次迭代

#### 测试生成器

```csharp
/// <summary>
/// 生成随机的调度上下文
/// </summary>
public class SchedulingContextGenerator
{
    public static Arbitrary<SchedulingContext> Generate()
    {
        return Arb.From(
            from personnelCount in Gen.Choose(5, 20)
            from positionCount in Gen.Choose(3, 10)
            from skillCount in Gen.Choose(2, 5)
            from startDate in GenDate()
            from endDate in GenDate().Where(d => d >= startDate)
            select CreateContext(personnelCount, positionCount, skillCount, startDate, endDate)
        );
    }
}

/// <summary>
/// 生成会导致死胡同的场景
/// </summary>
public class DeadEndScenarioGenerator
{
    public static Arbitrary<SchedulingContext> Generate()
    {
        // 生成人员数量少于哨位数量的场景
        // 或者生成技能不匹配的场景
        // 确保会出现无法分配的情况
    }
}
```

#### 属性测试用例

**测试 1: 死胡同触发回溯**
```csharp
[Property]
public Property DeadEndTriggersBacktracking()
{
    return Prop.ForAll(
        DeadEndScenarioGenerator.Generate(),
        context =>
        {
            var config = new BacktrackingConfig { EnableBacktracking = true };
            var scheduler = new GreedyScheduler(context, config);
            var engine = new BacktrackingEngine(context, config);
            
            // 运行到死胡同
            var deadEndDetected = engine.DetectDeadEnd();
            var backtrackTriggered = engine.GetStatistics().TotalBacktracks > 0;
            
            return deadEndDetected.Implies(backtrackTriggered);
        }
    ).Label("Feature: greedy-scheduler-backtracking, Property 1: 死胡同触发回溯");
}
```

**测试 2: 回溯深度限制**
```csharp
[Property]
public Property BacktrackDepthLimit()
{
    return Prop.ForAll(
        SchedulingContextGenerator.Generate(),
        Gen.Choose(1, 20), // maxDepth
        (context, maxDepth) =>
        {
            var config = new BacktrackingConfig 
            { 
                EnableBacktracking = true,
                MaxBacktrackDepth = maxDepth 
            };
            var scheduler = new GreedyScheduler(context, config);
            
            var result = scheduler.ExecuteAsync().Result;
            var stats = scheduler.GetBacktrackingStatistics();
            
            return stats.MaxDepthReached <= maxDepth;
        }
    ).Label("Feature: greedy-scheduler-backtracking, Property 2: 回溯深度限制");
}
```

**测试 3: 成功回溯的完整性**
```csharp
[Property]
public Property SuccessfulBacktrackingCompleteness()
{
    return Prop.ForAll(
        SchedulingContextGenerator.Generate(),
        context =>
        {
            var config = new BacktrackingConfig { EnableBacktracking = true };
            var scheduler = new GreedyScheduler(context, config);
            
            var result = scheduler.ExecuteAsync().Result;
            var stats = scheduler.GetBacktrackingStatistics();
            
            if (stats.SuccessfulBacktracks > 0)
            {
                // 如果回溯成功，排班应该是完整的
                var totalSlots = context.Positions.Count * 12 * 
                    ((context.EndDate - context.StartDate).Days + 1);
                return result.Results.Count == totalSlots;
            }
            return true;
        }
    ).Label("Feature: greedy-scheduler-backtracking, Property 3: 成功回溯的完整性");
}
```

**测试 4: 分配栈操作正确性**
```csharp
[Property]
public Property AssignmentStackCorrectness()
{
    return Prop.ForAll(
        Gen.Choose(1, 10), // positionIdx
        Gen.Choose(0, 11), // periodIdx
        Gen.Choose(1, 20), // personIdx
        (posIdx, periodIdx, personIdx) =>
        {
            var stack = new AssignmentStack(maxDepth: 100);
            var initialDepth = stack.Depth;
            
            var record = new AssignmentRecord
            {
                PositionIdx = posIdx,
                PeriodIdx = periodIdx,
                PersonIdx = personIdx,
                Candidates = new List<int> { personIdx },
                Snapshot = new StateSnapshot()
            };
            
            stack.Push(record);
            
            return stack.Depth == initialDepth + 1 &&
                   stack.Peek()?.PositionIdx == posIdx &&
                   stack.Peek()?.PeriodIdx == periodIdx &&
                   stack.Peek()?.PersonIdx == personIdx;
        }
    ).Label("Feature: greedy-scheduler-backtracking, Property 4: 分配栈操作正确性");
}
```

**测试 5: 回溯状态往返一致性**
```csharp
[Property]
public Property BacktrackStateRoundTrip()
{
    return Prop.ForAll(
        SchedulingContextGenerator.Generate(),
        context =>
        {
            var config = new BacktrackingConfig { EnableBacktracking = true };
            var engine = new BacktrackingEngine(context, config);
            
            // 保存初始状态
            var initialState = CaptureState(context);
            
            // 执行分配
            var assigned = engine.TryAssignWithBacktracking(0, 0, DateTime.Now, null, CancellationToken.None).Result;
            
            if (assigned)
            {
                // 执行回溯
                engine.Backtrack(DateTime.Now, null).Wait();
                
                // 验证状态恢复
                var restoredState = CaptureState(context);
                return StatesEqual(initialState, restoredState);
            }
            return true;
        }
    ).Label("Feature: greedy-scheduler-backtracking, Property 5: 回溯状态往返一致性");
}
```

**测试 15: 回溯后硬约束保持**
```csharp
[Property]
public Property HardConstraintsAfterBacktracking()
{
    return Prop.ForAll(
        SchedulingContextGenerator.Generate(),
        context =>
        {
            var config = new BacktrackingConfig { EnableBacktracking = true };
            var scheduler = new GreedyScheduler(context, config);
            
            var result = scheduler.ExecuteAsync().Result;
            
            // 验证所有分配都满足硬约束
            foreach (var shift in result.Results)
            {
                var validator = new ConstraintValidator(context);
                var personIdx = context.PersonIdToIdx[shift.PersonnelId];
                var posIdx = context.PositionIdToIdx[shift.PositionId];
                var periodIdx = shift.TimeSlotIndex;
                
                if (!validator.ValidateAllConstraints(personIdx, posIdx, periodIdx, shift.StartTime))
                {
                    return false;
                }
            }
            return true;
        }
    ).Label("Feature: greedy-scheduler-backtracking, Property 15: 回溯后硬约束保持");
}
```

### 集成测试

集成测试验证回溯机制与整个调度系统的集成：

1. **完整排班流程测试**
   - 测试从初始化到完成的完整流程
   - 验证回溯与MRV策略的协作
   - 验证回溯与约束验证的协作

2. **进度报告测试**
   - 验证回溯事件的进度报告
   - 验证统计信息的准确性

3. **性能测试**
   - 测试回溯对性能的影响
   - 验证内存使用是否在可接受范围内

### 测试数据

使用TestDataGenerator生成各种测试场景：

1. **简单场景**: 人员充足，约束宽松，不需要回溯
2. **紧张场景**: 人员紧张，约束严格，需要少量回溯
3. **困难场景**: 人员不足，约束复杂，需要深度回溯
4. **无解场景**: 约束冲突，无法完全分配

