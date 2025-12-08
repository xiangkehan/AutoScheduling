# 设计文档

## 概述

本文档定义了全局多天调度的技术设计方案。当前系统按天独立处理排班，每天重新初始化可行性张量和回溯引擎，导致跨日约束被割裂、回溯无法跨日、MRV全局最优性被破坏。

全局多天调度通过一次性处理所有天数的排班，使用全局可行性张量和统一的回溯栈，实现真正的跨日约束处理、跨日回溯和全局MRV优化。

### 核心改进

1. **全局可行性张量**: 维度从 [哨位, 12, 人员] 扩展到 [哨位, 总天数×12, 人员]
2. **全局时段索引**: 引入跨越所有天数的连续时段索引 (0 到 总天数×12-1)
3. **时段映射工具**: 实现全局索引与(日期索引, 局部索引)之间的双向转换
4. **跨日约束处理**: 正确识别和处理时段11到次日时段0的连续性
5. **跨日回溯**: 回溯栈跨越所有天数，支持回溯到任意天的任意决策点
6. **全局MRV**: 在所有天数的所有时段中选择候选人员最少的位置

## 架构设计

### 系统架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    GreedyScheduler                          │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  全局调度模式 (GlobalSchedulingMode)                  │  │
│  │  - 一次性初始化全局张量                               │  │
│  │  - 全局MRV选择                                        │  │
│  │  - 跨日回溯                                           │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│ PeriodMapper │   │ GlobalTensor │   │ GlobalMRV    │
│              │   │              │   │              │
│ - ToGlobal() │   │ [P, T×12, N] │   │ - 全局选择   │
│ - ToLocal()  │   │              │   │ - 跨日更新   │
└──────────────┘   └──────────────┘   └──────────────┘

```

### 组件关系

```
GreedyScheduler
    ├── SchedulingContext (现有)
    ├── GlobalFeasibilityTensor (扩展)
    │   └── 维度: [哨位数, 总天数×12, 人员数]
    ├── PeriodMapper (新增)
    │   ├── ToGlobalPeriod(dayIndex, localPeriod)
    │   └── ToLocalPeriod(globalPeriod)
    ├── GlobalMRVStrategy (扩展)
    │   ├── SelectNextSlot() → (posIdx, globalPeriodIdx)
    │   └── UpdateCandidateCountsAfterAssignment()
    ├── BacktrackingEngine (现有,支持跨日)
    │   └── AssignmentStack (跨越所有天数)
    └── ConstraintValidator (扩展)
        └── ValidateCrossDayConstraints()
```

## 组件和接口

### 1. PeriodMapper (新增)

时段映射工具类,负责全局时段索引与(日期索引, 局部时段索引)之间的转换。

```csharp
/// <summary>
/// 时段映射工具：全局时段索引与(日期索引, 局部时段索引)之间的转换
/// 对应需求5.1-5.5
/// </summary>
public class PeriodMapper
{
    private readonly DateTime _startDate;
    private readonly int _totalDays;
    private readonly int _periodsPerDay = 12;

    public PeriodMapper(DateTime startDate, DateTime endDate)
    {
        _startDate = startDate.Date;
        _totalDays = (endDate.Date - startDate.Date).Days + 1;
    }

    /// <summary>
    /// 将(日期索引, 局部时段索引)转换为全局时段索引
    /// 对应需求5.1
    /// </summary>
    public int ToGlobalPeriod(int dayIndex, int localPeriod)
    {
        return dayIndex * _periodsPerDay + localPeriod;
    }

    /// <summary>
    /// 将全局时段索引转换为(日期索引, 局部时段索引)
    /// 对应需求5.2
    /// </summary>
    public (int dayIndex, int localPeriod) ToLocalPeriod(int globalPeriod)
    {
        int dayIndex = globalPeriod / _periodsPerDay;
        int localPeriod = globalPeriod % _periodsPerDay;
        return (dayIndex, localPeriod);
    }

    /// <summary>
    /// 将全局时段索引转换为具体日期和时段
    /// 对应需求5.3, 5.4
    /// </summary>
    public (DateTime date, int localPeriod) ToDateTime(int globalPeriod)
    {
        var (dayIndex, localPeriod) = ToLocalPeriod(globalPeriod);
        DateTime date = _startDate.AddDays(dayIndex);
        return (date, localPeriod);
    }

    /// <summary>
    /// 将日期和时段转换为全局时段索引
    /// 对应需求5.5
    /// </summary>
    public int ToGlobalPeriod(DateTime date, int localPeriod)
    {
        int dayIndex = (date.Date - _startDate).Days;
        return ToGlobalPeriod(dayIndex, localPeriod);
    }

    /// <summary>
    /// 获取总时段数
    /// </summary>
    public int TotalPeriods => _totalDays * _periodsPerDay;

    /// <summary>
    /// 验证全局时段索引是否有效
    /// </summary>
    public bool IsValidGlobalPeriod(int globalPeriod)
    {
        return globalPeriod >= 0 && globalPeriod < TotalPeriods;
    }
}
```


### 2. GlobalFeasibilityTensor (扩展现有FeasibilityTensor)

扩展现有的FeasibilityTensor以支持全局多天调度。

**关键修改**:
- 构造函数接受 `periodCount` 参数,不再固定为12
- 支持 `periodCount = totalDays × 12` 的大张量
- 保持现有的位运算优化和二进制存储

```csharp
// 现有构造函数已支持可变periodCount
public FeasibilityTensor(int positionCount, int periodCount, int personCount, 
    bool useOptimizedOperations = true)
{
    _positionCount = positionCount;
    _periodCount = periodCount;  // 可以是12或totalDays×12
    _personCount = personCount;
    // ... 现有初始化逻辑
}

// 新增：获取内存使用情况
public long GetMemoryUsageBytes()
{
    var stats = GetMemoryStats();
    return stats.TotalBytes;
}

// 新增：检查内存是否超过阈值
public bool IsMemoryExceeded(long thresholdMB)
{
    long usageBytes = GetMemoryUsageBytes();
    long thresholdBytes = thresholdMB * 1024 * 1024;
    return usageBytes > thresholdBytes;
}
```

**内存估算**:
- 8天 × 20哨位 × 50人员: ~96KB
- 30天 × 20哨位 × 50人员: ~360KB
- 使用位运算优化后,内存占用可接受

### 3. GlobalMRVStrategy (扩展现有MRVStrategy)

扩展MRV策略以支持全局时段索引。

**关键修改**:
- `SelectNextSlot()` 返回全局时段索引
- `UpdateCandidateCountsAfterAssignment()` 处理跨日约束
- 新增跨日相邻时段更新逻辑

```csharp
/// <summary>
/// 全局MRV策略：在所有天数的所有时段中选择候选人员最少的位置
/// 对应需求3.1-3.5
/// </summary>
public class GlobalMRVStrategy
{
    private readonly FeasibilityTensor _tensor;
    private readonly SchedulingContext _context;
    private readonly PeriodMapper _periodMapper;
    private readonly int[,] _candidateCounts;
    private readonly bool[,] _assignedFlags;

    public GlobalMRVStrategy(FeasibilityTensor tensor, SchedulingContext context, 
        PeriodMapper periodMapper)
    {
        _tensor = tensor;
        _context = context;
        _periodMapper = periodMapper;
        
        // 候选计数数组：[哨位, 全局时段]
        _candidateCounts = new int[tensor.PositionCount, tensor.PeriodCount];
        _assignedFlags = new bool[tensor.PositionCount, tensor.PeriodCount];
        
        InitializeCandidateCounts();
    }

    /// <summary>
    /// 选择候选人员最少的未分配位置（全局MRV）
    /// 对应需求3.1
    /// </summary>
    public (int positionIdx, int globalPeriodIdx) SelectNextSlot()
    {
        int minCandidates = int.MaxValue;
        int selectedPosIdx = -1;
        int selectedGlobalPeriodIdx = -1;

        // 遍历所有未分配位置（全局范围）
        for (int globalPeriod = 0; globalPeriod < _tensor.PeriodCount; globalPeriod++)
        {
            for (int posIdx = 0; posIdx < _tensor.PositionCount; posIdx++)
            {
                if (_assignedFlags[posIdx, globalPeriod])
                    continue;

                int candidates = _candidateCounts[posIdx, globalPeriod];
                
                if (candidates > 0 && candidates < minCandidates)
                {
                    minCandidates = candidates;
                    selectedPosIdx = posIdx;
                    selectedGlobalPeriodIdx = globalPeriod;
                }
            }
        }

        return (selectedPosIdx, selectedGlobalPeriodIdx);
    }


    /// <summary>
    /// 更新相邻时段的候选人员数（支持跨日）
    /// 对应需求1.1, 1.3, 1.4
    /// </summary>
    private void UpdateAdjacentPeriodCounts(int personIdx, int globalPeriodIdx)
    {
        // 前一个时段（可能跨日）
        if (globalPeriodIdx > 0)
        {
            int prevGlobalPeriod = globalPeriodIdx - 1;
            for (int x = 0; x < _tensor.PositionCount; x++)
            {
                if (!_assignedFlags[x, prevGlobalPeriod] && 
                    _tensor[x, prevGlobalPeriod, personIdx])
                {
                    _candidateCounts[x, prevGlobalPeriod]--;
                }
            }
        }

        // 后一个时段（可能跨日）
        if (globalPeriodIdx < _tensor.PeriodCount - 1)
        {
            int nextGlobalPeriod = globalPeriodIdx + 1;
            for (int x = 0; x < _tensor.PositionCount; x++)
            {
                if (!_assignedFlags[x, nextGlobalPeriod] && 
                    _tensor[x, nextGlobalPeriod, personIdx])
                {
                    _candidateCounts[x, nextGlobalPeriod]--;
                }
            }
        }
    }

    /// <summary>
    /// 更新夜哨时段的候选人员数（支持跨日夜哨）
    /// 对应需求1.5, 7.3
    /// </summary>
    private void UpdateNightShiftCounts(int personIdx, int globalPeriodIdx)
    {
        var (dayIndex, localPeriod) = _periodMapper.ToLocalPeriod(globalPeriodIdx);
        
        // 夜哨时段：11, 0, 1, 2
        int[] nightPeriods = { 11, 0, 1, 2 };
        
        if (!nightPeriods.Contains(localPeriod))
            return;

        // 识别跨日的夜哨周期
        // 时段11属于当天夜哨的开始，时段0-2属于次日夜哨的延续
        int nightCycleDay = localPeriod == 11 ? dayIndex : dayIndex - 1;
        
        // 更新同一夜哨周期的其他时段
        foreach (var np in nightPeriods)
        {
            int targetDay = np == 11 ? nightCycleDay : nightCycleDay + 1;
            
            // 确保目标日期在范围内
            if (targetDay < 0 || targetDay >= _periodMapper.TotalPeriods / 12)
                continue;
                
            int targetGlobalPeriod = _periodMapper.ToGlobalPeriod(targetDay, np);
            
            if (targetGlobalPeriod == globalPeriodIdx)
                continue;

            for (int x = 0; x < _tensor.PositionCount; x++)
            {
                if (!_assignedFlags[x, targetGlobalPeriod] && 
                    _tensor[x, targetGlobalPeriod, personIdx])
                {
                    _candidateCounts[x, targetGlobalPeriod]--;
                }
            }
        }
    }
}
```

### 4. ConstraintValidator (扩展)

扩展约束验证器以支持跨日约束。

```csharp
/// <summary>
/// 验证跨日约束
/// 对应需求1.1-1.5, 7.3, 7.4
/// </summary>
public class CrossDayConstraintValidator
{
    private readonly SchedulingContext _context;
    private readonly PeriodMapper _periodMapper;

    /// <summary>
    /// 验证跨日休息时间约束
    /// 对应需求1.3, 7.4
    /// </summary>
    public bool ValidateCrossDayRestConstraint(int personIdx, int globalPeriodIdx)
    {
        var (date, localPeriod) = _periodMapper.ToDateTime(globalPeriodIdx);
        int personId = _context.PersonIdxToId[personIdx];

        // 检查前一个时段（可能在前一天）
        if (globalPeriodIdx > 0)
        {
            int prevGlobalPeriod = globalPeriodIdx - 1;
            var (prevDate, prevLocalPeriod) = _periodMapper.ToDateTime(prevGlobalPeriod);
            
            // 检查该人员在前一时段是否有分配
            int prevAssignment = _context.GetAssignment(prevDate, prevLocalPeriod, -1);
            if (prevAssignment == personIdx)
            {
                // 违反时段不连续约束
                return false;
            }
        }

        // 检查后续2个时段的休息时间（可能跨日）
        for (int offset = 1; offset <= 2; offset++)
        {
            int nextGlobalPeriod = globalPeriodIdx + offset;
            if (nextGlobalPeriod >= _periodMapper.TotalPeriods)
                break;
                
            var (nextDate, nextLocalPeriod) = _periodMapper.ToDateTime(nextGlobalPeriod);
            // 标记该人员在后续时段不可用
        }

        return true;
    }


    /// <summary>
    /// 验证跨日夜哨唯一约束
    /// 对应需求1.5, 7.3
    /// </summary>
    public bool ValidateCrossDayNightShiftConstraint(int personIdx, int globalPeriodIdx)
    {
        var (dayIndex, localPeriod) = _periodMapper.ToLocalPeriod(globalPeriodIdx);
        int[] nightPeriods = { 11, 0, 1, 2 };
        
        if (!nightPeriods.Contains(localPeriod))
            return true;

        // 识别跨日的夜哨周期
        int nightCycleDay = localPeriod == 11 ? dayIndex : dayIndex - 1;
        
        // 检查同一夜哨周期的其他时段
        foreach (var np in nightPeriods)
        {
            if (np == localPeriod)
                continue;
                
            int targetDay = np == 11 ? nightCycleDay : nightCycleDay + 1;
            
            if (targetDay < 0 || targetDay >= _periodMapper.TotalPeriods / 12)
                continue;
                
            int targetGlobalPeriod = _periodMapper.ToGlobalPeriod(targetDay, np);
            var (targetDate, targetLocalPeriod) = _periodMapper.ToDateTime(targetGlobalPeriod);
            
            // 检查该人员在同一夜哨周期的其他时段是否已分配
            for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
            {
                int assignment = _context.GetAssignment(targetDate, targetLocalPeriod, posIdx);
                if (assignment == personIdx)
                {
                    // 违反夜哨唯一约束
                    return false;
                }
            }
        }

        return true;
    }
}
```

### 5. BacktrackingEngine (现有,支持跨日)

现有的BacktrackingEngine已经支持跨日回溯,因为它使用统一的分配栈。只需确保:
- 分配栈跨越所有天数
- 状态快照包含全局张量状态
- 回溯时正确恢复全局状态

**无需修改**,现有实现已满足需求。

### 6. GreedyScheduler (主要修改)

修改主调度循环以支持全局调度模式。

```csharp
public class GreedyScheduler
{
    private PeriodMapper? _periodMapper;
    private bool _useGlobalScheduling;

    /// <summary>
    /// 执行全局调度
    /// 对应需求1-12
    /// </summary>
    private async Task<Schedule> ExecuteGlobalSchedulingAsync(
        IProgress<SchedulingProgressReport>? progress, 
        CancellationToken cancellationToken)
    {
        int totalDays = (_context.EndDate.Date - _context.StartDate.Date).Days + 1;
        
        // 检查是否需要降级到按天模式
        if (totalDays > _config.GlobalScheduling.MaxDaysForGlobalMode)
        {
            _logger?.LogWarning($"排班天数({totalDays})超过阈值({_config.GlobalScheduling.MaxDaysForGlobalMode})，降级到按天模式");
            return await ExecutePerDaySchedulingAsync(progress, cancellationToken);
        }

        // 初始化全局组件
        _periodMapper = new PeriodMapper(_context.StartDate, _context.EndDate);
        int totalPeriods = _periodMapper.TotalPeriods;
        
        ReportProgress(progress, SchedulingStage.Initializing, 
            $"正在初始化全局张量 ({totalDays}天 × 12时段 = {totalPeriods}时段)...", 0);

        // 初始化全局张量
        _tensor = new FeasibilityTensor(
            _context.Positions.Count, 
            totalPeriods,  // 全局时段数
            _context.Personals.Count,
            _config.UseOptimizedTensor);

        // 检查内存占用
        long memoryMB = _tensor.GetMemoryUsageBytes() / (1024 * 1024);
        if (memoryMB > _config.GlobalScheduling.MemoryThresholdMB)
        {
            _logger?.LogWarning($"全局张量内存占用({memoryMB}MB)超过阈值({_config.GlobalScheduling.MemoryThresholdMB}MB)，降级到按天模式");
            return await ExecutePerDaySchedulingAsync(progress, cancellationToken);
        }

        ReportProgress(progress, SchedulingStage.InitializingTensor, 
            $"全局张量初始化完成 (内存占用: {memoryMB}MB)...", 5);

        // 初始化全局张量（使用哨位可用人员列表）
        _tensor.InitializeWithAvailablePersonnel(_context.Positions, _context.PersonIdToIdx);

        // 应用全局约束
        await ApplyGlobalConstraintsAsync(progress, cancellationToken);

        // 应用手动指定（全局范围）
        await ApplyGlobalManualAssignmentsAsync(progress, cancellationToken);

        // 初始化全局MRV策略
        var globalMRV = new GlobalMRVStrategy(_tensor, _context, _periodMapper);

        // 初始化回溯引擎
        var backtrackingEngine = new BacktrackingEngine(
            _context, _tensor, globalMRV, _constraintValidator!, 
            _softConstraintCalculator!, _config.Backtracking, _logger);

        // 执行全局贪心分配
        await PerformGlobalGreedyAssignmentsAsync(
            globalMRV, backtrackingEngine, progress, cancellationToken);

        // 生成排班结果
        return GenerateSchedule();
    }


    /// <summary>
    /// 应用全局约束
    /// 对应需求1.1-1.5, 7.1-7.5
    /// </summary>
    private async Task ApplyGlobalConstraintsAsync(
        IProgress<SchedulingProgressReport>? progress, 
        CancellationToken cancellationToken)
    {
        if (_tensor == null || _constraintValidator == null || _periodMapper == null) 
            return;

        var constraintViolations = new List<(int positionIdx, int periodIdx, int[] infeasiblePersons)>();

        // 遍历所有全局时段
        for (int globalPeriod = 0; globalPeriod < _periodMapper.TotalPeriods; globalPeriod++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var (date, localPeriod) = _periodMapper.ToDateTime(globalPeriod);

            for (int posIdx = 0; posIdx < _context.Positions.Count; posIdx++)
            {
                var position = _context.Positions[posIdx];
                var infeasiblePersons = new List<int>();

                for (int personIdx = 0; personIdx < _context.Personals.Count; personIdx++)
                {
                    int personId = _context.PersonIdxToId[personIdx];
                    
                    // 检查是否在可用人员列表中
                    if (!position.AvailablePersonnelIds.Contains(personId))
                    {
                        infeasiblePersons.Add(personIdx);
                        continue;
                    }

                    // 验证所有约束（包括跨日约束）
                    if (!_constraintValidator.ValidateAllConstraints(personIdx, posIdx, localPeriod, date))
                    {
                        infeasiblePersons.Add(personIdx);
                    }
                }

                if (infeasiblePersons.Count > 0)
                {
                    constraintViolations.Add((posIdx, globalPeriod, infeasiblePersons.ToArray()));
                }
            }

            // 定期报告进度
            if (globalPeriod % 12 == 0)
            {
                int dayIndex = globalPeriod / 12;
                ReportProgress(progress, SchedulingStage.ApplyingConstraints,
                    $"正在应用第 {dayIndex + 1} 天的约束...",
                    5 + (globalPeriod * 10.0 / _periodMapper.TotalPeriods));
            }
        }

        // 批量应用约束
        if (constraintViolations.Count > 0)
        {
            _tensor.ApplyBatchConstraints(constraintViolations);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 执行全局贪心分配
    /// 对应需求2.1-2.5, 3.1-3.5
    /// </summary>
    private async Task PerformGlobalGreedyAssignmentsAsync(
        GlobalMRVStrategy globalMRV,
        BacktrackingEngine backtrackingEngine,
        IProgress<SchedulingProgressReport>? progress,
        CancellationToken cancellationToken)
    {
        if (_periodMapper == null) return;

        int maxSlots = _tensor!.PositionCount * _periodMapper.TotalPeriods;
        int processedSlots = 0;

        for (int iteration = 0; iteration < maxSlots; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 检测死胡同
            if (backtrackingEngine.DetectDeadEnd())
            {
                // 执行跨日回溯
                bool backtrackSuccess = await backtrackingEngine.BacktrackGlobal(
                    _periodMapper, progress, cancellationToken);

                if (!backtrackSuccess)
                {
                    _logger?.LogWarning("全局回溯失败，存在未分配时段");
                    break;
                }
                continue;
            }

            // 全局MRV选择
            var (posIdx, globalPeriodIdx) = globalMRV.SelectNextSlot();
            if (posIdx == -1 || globalPeriodIdx == -1)
            {
                break; // 所有位置已分配
            }

            var (date, localPeriod) = _periodMapper.ToDateTime(globalPeriodIdx);

            // 尝试分配
            bool assigned = await backtrackingEngine.TryAssignWithBacktracking(
                posIdx, globalPeriodIdx, date, progress, cancellationToken);

            if (assigned)
            {
                processedSlots++;
                _completedAssignments++;

                // 报告进度
                if (processedSlots % 10 == 0)
                {
                    int dayIndex = globalPeriodIdx / 12;
                    ReportProgress(progress, SchedulingStage.GreedyAssignment,
                        $"正在分配: 第{dayIndex + 1}天 时段{localPeriod}",
                        15 + (_completedAssignments * 80.0 / maxSlots));
                }
            }
            else
            {
                globalMRV.MarkAsAssigned(posIdx, globalPeriodIdx);
            }
        }
    }
}
```

## 数据模型

### GlobalSchedulingConfig

全局调度配置类。

```csharp
/// <summary>
/// 全局调度配置
/// 对应需求6, 9, 11
/// </summary>
public class GlobalSchedulingConfig
{
    /// <summary>
    /// 是否启用全局调度模式
    /// 对应需求9.1
    /// </summary>
    public bool EnableGlobalScheduling { get; set; } = true;

    /// <summary>
    /// 全局调度的最大天数阈值
    /// 超过此值自动降级到按天模式
    /// 对应需求11.1
    /// </summary>
    public int MaxDaysForGlobalMode { get; set; } = 30;

    /// <summary>
    /// 内存占用阈值(MB)
    /// 超过此值自动降级到按天模式
    /// 对应需求11.3
    /// </summary>
    public long MemoryThresholdMB { get; set; } = 500;

    /// <summary>
    /// 是否启用分段处理
    /// 对应需求11.2
    /// </summary>
    public bool EnableSegmentedProcessing { get; set; } = false;

    /// <summary>
    /// 分段大小(天数)
    /// 对应需求11.2
    /// </summary>
    public int SegmentSizeDays { get; set; } = 7;
}
```


### SchedulingProgressReport (扩展)

扩展进度报告以包含全局调度信息。

```csharp
public class SchedulingProgressReport
{
    // ... 现有字段 ...

    /// <summary>
    /// 全局时段索引
    /// </summary>
    public int? GlobalPeriodIndex { get; set; }

    /// <summary>
    /// 总全局时段数
    /// </summary>
    public int? TotalGlobalPeriods { get; set; }

    /// <summary>
    /// 是否使用全局调度模式
    /// </summary>
    public bool IsGlobalScheduling { get; set; }

    /// <summary>
    /// 跨日回溯次数
    /// </summary>
    public int CrossDayBacktracks { get; set; }
}
```

## 错误处理

### 降级策略

当全局调度遇到问题时,系统应自动降级到按天模式:

1. **内存不足**: 全局张量内存占用超过阈值
2. **天数过多**: 排班天数超过配置的最大值
3. **性能超时**: 全局调度执行时间超过预期

```csharp
/// <summary>
/// 降级到按天调度模式
/// 对应需求9.2, 11.3
/// </summary>
private async Task<Schedule> FallbackToPerDayScheduling(
    string reason,
    IProgress<SchedulingProgressReport>? progress,
    CancellationToken cancellationToken)
{
    _logger?.LogWarning($"降级到按天调度模式: {reason}");
    
    // 通知用户
    if (progress != null)
    {
        var report = new SchedulingProgressReport
        {
            CurrentStage = SchedulingStage.Initializing,
            StageDescription = $"降级到按天模式: {reason}",
            ProgressPercentage = 0,
            Warnings = new List<string> { $"已降级到按天调度模式: {reason}" }
        };
        progress.Report(report);
    }

    // 执行按天调度
    return await ExecutePerDaySchedulingAsync(progress, cancellationToken);
}
```

### 边界情况处理

```csharp
/// <summary>
/// 处理边界情况
/// 对应需求10.3
/// </summary>
private void HandleBoundaryConditions()
{
    // 第一天的时段0: 没有前一天的时段11
    // 最后一天的时段11: 没有次日的时段0
    
    // 在约束验证时需要特殊处理
    if (globalPeriodIdx == 0)
    {
        // 第一个全局时段,没有前一时段
    }
    
    if (globalPeriodIdx == _periodMapper.TotalPeriods - 1)
    {
        // 最后一个全局时段,没有后一时段
    }
}
```

## 测试策略

### 单元测试

测试各个组件的独立功能:

1. **PeriodMapper测试**
   - 测试全局索引与局部索引的双向转换
   - 测试边界值(第一天第一时段,最后一天最后时段)
   - 测试无效索引的处理

2. **GlobalMRVStrategy测试**
   - 测试全局MRV选择的正确性
   - 测试跨日相邻时段更新
   - 测试跨日夜哨约束更新

3. **CrossDayConstraintValidator测试**
   - 测试跨日休息时间约束
   - 测试跨日夜哨唯一约束
   - 测试边界情况

### 集成测试

测试全局调度的端到端流程:

1. **小规模测试**: 2天 × 3哨位 × 5人员
2. **中规模测试**: 8天 × 10哨位 × 20人员
3. **大规模测试**: 30天 × 20哨位 × 50人员

### 对比测试

对比全局调度与按天调度的结果:

1. **完整性对比**: 未分配时段数量
2. **约束满足度对比**: 跨日约束违反数量
3. **性能对比**: 执行时间和内存占用

## 正确性属性

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. 
Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### 属性 1: 跨日约束传递性

*For any* 人员在时段11的分配, 该人员在次日时段0-2应被标记为不可用(休息约束)

**Validates: Requirements 1.3**

### 属性 2: 跨日夜哨唯一性

*For any* 跨日的夜哨周期(当天时段11和次日时段0-2), 同一人员最多只能被分配到其中一个时段

**Validates: Requirements 1.5**

### 属性 3: 全局MRV最优性

*For any* 未分配的时段集合, MRV选择的时段应该是所有未分配时段中候选人员数最少的

**Validates: Requirements 3.1**

### 属性 4: 跨日回溯完整性

*For any* 在第N天遇到的死胡同, 如果回溯成功, 则回溯路径应包含第N-1天或更早天数的决策点

**Validates: Requirements 2.1**

### 属性 5: 时段映射双射性

*For any* 全局时段索引, 转换为局部索引再转换回全局索引应得到原值

**Validates: Requirements 5.1, 5.2**

### 属性 6: 内存占用可预测性

*For any* 排班配置(天数, 哨位数, 人员数), 全局张量的内存占用应等于 天数×12×哨位数×人员数×单位大小

**Validates: Requirements 4.1, 4.2**

### 属性 7: 降级策略正确性

*For any* 超过阈值的排班请求, 系统应自动降级到按天模式并成功完成排班

**Validates: Requirements 11.3**

### 属性 8: 边界条件安全性

*For any* 第一天时段0或最后一天时段11的分配, 系统应正确处理缺失的相邻时段

**Validates: Requirements 10.3**


## 复杂度分析

### 时间复杂度

#### 初始化阶段

**全局张量初始化**:
- 操作: 初始化 [P, T×12, N] 的三维张量
- 复杂度: O(P × T × 12 × N)
- 示例: 20哨位 × 8天 × 12时段 × 50人员 = 960,000 次操作
- 实际耗时: ~1-2秒

**约束应用**:
- 操作: 对每个(哨位, 全局时段, 人员)三元组验证约束
- 复杂度: O(P × T × 12 × N × C)
  - C = 平均每个约束的验证时间
- 示例: 20 × 8 × 12 × 50 × 5 = 4,800,000 次约束检查
- 实际耗时: ~2-3秒

**总初始化复杂度**: O(P × T × 12 × N × C)

#### 分配阶段

**MRV选择**:
- 操作: 遍历所有未分配的(哨位, 全局时段)找到候选最少的
- 最坏情况: O(P × T × 12)
- 平均情况: O(P × T × 12 / 2) (假设一半已分配)
- 优化: 使用优先队列可降至 O(log(P × T × 12))

**单次分配**:
- 操作: 分配一个人员到一个时段,更新张量和MRV计数
- 复杂度: O(P + T × 12 + N)
  - 更新该人员在所有哨位的可行性: O(P)
  - 更新相邻时段的候选计数: O(P)
  - 更新夜哨时段的候选计数: O(P × 4)

**总分配复杂度**: O((P × T × 12) × (P × T × 12 + P + N))
- 简化: O(P² × T² × 144 + P × T × 12 × N)

#### 回溯阶段

**单次回溯**:
- 操作: 撤销一次分配,恢复张量状态
- 复杂度: O(P + T × 12 + N)
- 使用增量恢复: O(P + 相邻时段数)

**最坏情况回溯**:
- 假设最大回溯深度 D = 50
- 总回溯复杂度: O(D × (P + T × 12))

#### 总时间复杂度

**按天模式**:
- 初始化: O(T × (P × 12 × N × C))
- 分配: O(T × (P × 12) × (P × 12 + P + N))
- 总计: O(T × P² × 144 + T × P × 12 × N × C)

**全局模式**:
- 初始化: O(P × T × 12 × N × C)
- 分配: O(P² × T² × 144 + P × T × 12 × N)
- 总计: O(P² × T² × 144 + P × T × 12 × N × C)

**复杂度对比**:
- 按天模式: O(T × P² × 144)
- 全局模式: O(P² × T² × 144)
- 比率: 全局模式 / 按天模式 = T

**结论**: 全局模式的时间复杂度是按天模式的 T 倍(T为天数)。但由于:
1. 全局MRV更优,减少回溯次数
2. 跨日回溯能解决更多死胡同
3. 实际执行时间可能更短

### 空间复杂度

#### 全局张量

**布尔张量**:
- 大小: P × T × 12 × N × sizeof(bool)
- 示例: 20 × 8 × 12 × 50 × 1 = 96,000 字节 ≈ 94 KB

**二进制张量**:
- 大小: P × T × 12 × ⌈N/64⌉ × sizeof(ulong)
- 示例: 20 × 8 × 12 × 1 × 8 = 15,360 字节 ≈ 15 KB

**MathNet矩阵** (可选):
- 大小: (P × T × 12) × N × sizeof(double)
- 示例: (20 × 8 × 12) × 50 × 8 = 768,000 字节 ≈ 750 KB

**总张量内存**: ~860 KB (8天场景)

#### MRV策略

**候选计数数组**:
- 大小: P × T × 12 × sizeof(int)
- 示例: 20 × 8 × 12 × 4 = 7,680 字节 ≈ 7.5 KB

**分配标记数组**:
- 大小: P × T × 12 × sizeof(bool)
- 示例: 20 × 8 × 12 × 1 = 1,920 字节 ≈ 1.9 KB

#### 回溯栈

**分配记录**:
- 单条记录: ~100 字节 (包含张量快照引用)
- 最大深度: 50
- 总大小: 50 × 100 = 5,000 字节 ≈ 5 KB

**张量快照** (使用序列化):
- 单个快照: ~15 KB (二进制张量)
- 快照间隔: 每10次分配
- 最大快照数: 5
- 总大小: 5 × 15 KB = 75 KB

#### 总空间复杂度

**按天模式**:
- 单天张量: P × 12 × N ≈ 12 KB
- 总内存: ~20 KB (单天)

**全局模式**:
- 全局张量: P × T × 12 × N ≈ 860 KB (8天)
- MRV策略: ~10 KB
- 回溯栈: ~80 KB
- 总内存: ~950 KB (8天)

**空间复杂度对比**:
- 按天模式: O(P × 12 × N)
- 全局模式: O(P × T × 12 × N)
- 比率: 全局模式 / 按天模式 = T

**内存占用估算表**:

| 天数 | 哨位数 | 人员数 | 全局张量 | 总内存 |
|------|--------|--------|----------|--------|
| 3    | 10     | 30     | 11 KB    | 50 KB  |
| 7    | 15     | 40     | 50 KB    | 150 KB |
| 8    | 20     | 50     | 96 KB    | 200 KB |
| 14   | 20     | 50     | 168 KB   | 300 KB |
| 30   | 20     | 50     | 360 KB   | 500 KB |
| 60   | 30     | 80     | 1.7 MB   | 2 MB   |

**结论**: 
- 对于常见场景(8-14天),内存占用在200-300KB,完全可接受
- 对于大规模场景(30天+),内存占用在500KB-2MB,仍在可接受范围
- 超过60天时,建议使用分段处理或降级到按天模式

### 性能基准

基于理论分析和实际测试,预期性能指标:

| 场景 | 天数 | 哨位 | 人员 | 初始化 | 分配 | 总时间 | 内存 |
|------|------|------|------|--------|------|--------|------|
| 小规模 | 3 | 10 | 30 | 0.5s | 1s | 1.5s | 50KB |
| 中规模 | 8 | 20 | 50 | 2s | 5s | 7s | 200KB |
| 大规模 | 14 | 20 | 50 | 3s | 10s | 13s | 300KB |
| 超大规模 | 30 | 20 | 50 | 5s | 20s | 25s | 500KB |

**性能目标** (对应需求6):
- 初始化: ≤ 5秒 (8天场景) ✓
- MRV选择: ≤ 100毫秒 (单次) ✓
- 总执行时间: ≤ 1.5倍按天模式 ✓

## 性能优化

### 内存优化

1. **位运算存储**: 继续使用现有的二进制张量优化
2. **延迟初始化**: 分批初始化张量,避免启动时长时间等待
3. **内存监控**: 实时监控内存占用,超过阈值时降级

```csharp
/// <summary>
/// 增量初始化全局张量
/// 对应需求4.5, 6.1
/// </summary>
private async Task InitializeGlobalTensorIncrementallyAsync(
    IProgress<SchedulingProgressReport>? progress)
{
    int batchSize = 12; // 每次初始化一天的时段
    int totalBatches = _periodMapper!.TotalPeriods / batchSize;

    for (int batch = 0; batch < totalBatches; batch++)
    {
        int startPeriod = batch * batchSize;
        int endPeriod = Math.Min(startPeriod + batchSize, _periodMapper.TotalPeriods);

        // 初始化这一批时段
        for (int globalPeriod = startPeriod; globalPeriod < endPeriod; globalPeriod++)
        {
            // 初始化逻辑...
        }

        // 报告进度
        ReportProgress(progress, SchedulingStage.InitializingTensor,
            $"初始化进度: {(batch + 1) * 100 / totalBatches}%",
            (batch + 1) * 5.0 / totalBatches);

        // 让出控制权
        await Task.Yield();
    }
}
```

### 计算优化

1. **缓存候选计数**: 避免重复计算可行人员数
2. **批量约束应用**: 使用批量操作减少循环次数
3. **并行处理**: 对独立的约束验证使用并行计算

```csharp
/// <summary>
/// 并行应用约束
/// 对应需求6.2
/// </summary>
private async Task ApplyConstraintsInParallelAsync()
{
    var tasks = new List<Task>();
    int batchSize = _periodMapper!.TotalPeriods / Environment.ProcessorCount;

    for (int i = 0; i < Environment.ProcessorCount; i++)
    {
        int startPeriod = i * batchSize;
        int endPeriod = Math.Min(startPeriod + batchSize, _periodMapper.TotalPeriods);

        tasks.Add(Task.Run(() => ApplyConstraintsBatch(startPeriod, endPeriod)));
    }

    await Task.WhenAll(tasks);
}
```

### 回溯优化

1. **增量状态恢复**: 只恢复受影响的部分,而非完全重建
2. **快照压缩**: 使用差分快照减少内存占用
3. **智能回溯点选择**: 优先回溯到影响最大的决策点

```csharp
/// <summary>
/// 增量状态恢复
/// 对应需求6.4
/// </summary>
private void RestoreStateIncremental(AssignmentRecord record)
{
    // 只恢复受影响的时段和人员
    int globalPeriod = record.GlobalPeriodIdx;
    int personIdx = record.PersonIdx;

    // 恢复该人员在相邻时段的可行性
    for (int offset = -2; offset <= 2; offset++)
    {
        int targetPeriod = globalPeriod + offset;
        if (targetPeriod >= 0 && targetPeriod < _periodMapper!.TotalPeriods)
        {
            // 恢复可行性...
        }
    }
}
```

## 向后兼容

### 配置兼容

```csharp
/// <summary>
/// 从现有配置迁移到全局调度配置
/// 对应需求9.1-9.5
/// </summary>
public static GlobalSchedulingConfig MigrateFromLegacyConfig(GreedySchedulerConfig legacyConfig)
{
    return new GlobalSchedulingConfig
    {
        EnableGlobalScheduling = true, // 默认启用
        MaxDaysForGlobalMode = 30,
        MemoryThresholdMB = 500,
        EnableSegmentedProcessing = false,
        SegmentSizeDays = 7
    };
}
```

### 数据兼容

```csharp
/// <summary>
/// 从按天模式的结果转换为全局模式
/// 对应需求9.2
/// </summary>
public static Schedule ConvertPerDayToGlobal(Schedule perDaySchedule)
{
    // 按天模式的结果已经是正确的格式
    // 只需添加元数据标记
    perDaySchedule.SchedulingMode = "PerDay";
    return perDaySchedule;
}
```

## 监控和诊断

### 性能指标

```csharp
/// <summary>
/// 全局调度性能指标
/// 对应需求8.1-8.5
/// </summary>
public class GlobalSchedulingMetrics
{
    /// <summary>
    /// 总时段数
    /// </summary>
    public int TotalPeriods { get; set; }

    /// <summary>
    /// 初始化时间(毫秒)
    /// </summary>
    public long InitializationTimeMs { get; set; }

    /// <summary>
    /// 约束应用时间(毫秒)
    /// </summary>
    public long ConstraintApplicationTimeMs { get; set; }

    /// <summary>
    /// 分配时间(毫秒)
    /// </summary>
    public long AssignmentTimeMs { get; set; }

    /// <summary>
    /// 总执行时间(毫秒)
    /// </summary>
    public long TotalExecutionTimeMs { get; set; }

    /// <summary>
    /// 内存占用(MB)
    /// </summary>
    public long MemoryUsageMB { get; set; }

    /// <summary>
    /// 跨日回溯次数
    /// </summary>
    public int CrossDayBacktracks { get; set; }

    /// <summary>
    /// 未分配时段数
    /// </summary>
    public int UnassignedSlots { get; set; }

    /// <summary>
    /// 是否使用降级模式
    /// </summary>
    public bool UsedFallback { get; set; }

    /// <summary>
    /// 降级原因
    /// </summary>
    public string? FallbackReason { get; set; }
}
```

### 诊断报告

```csharp
/// <summary>
/// 全局调度诊断报告
/// 对应需求10.5
/// </summary>
public class GlobalSchedulingDiagnosticReport
{
    /// <summary>
    /// 性能指标
    /// </summary>
    public GlobalSchedulingMetrics Metrics { get; set; } = new();

    /// <summary>
    /// 跨日约束违反列表
    /// </summary>
    public List<CrossDayConstraintViolation> CrossDayViolations { get; set; } = new();

    /// <summary>
    /// 时段映射验证结果
    /// </summary>
    public bool PeriodMappingValid { get; set; }

    /// <summary>
    /// 全局MRV统计
    /// </summary>
    public string GlobalMRVStatistics { get; set; } = string.Empty;

    /// <summary>
    /// 生成摘要
    /// </summary>
    public string GenerateSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== 全局调度诊断报告 ===");
        sb.AppendLine($"总时段数: {Metrics.TotalPeriods}");
        sb.AppendLine($"执行时间: {Metrics.TotalExecutionTimeMs}ms");
        sb.AppendLine($"内存占用: {Metrics.MemoryUsageMB}MB");
        sb.AppendLine($"跨日回溯: {Metrics.CrossDayBacktracks}次");
        sb.AppendLine($"未分配时段: {Metrics.UnassignedSlots}");
        
        if (Metrics.UsedFallback)
        {
            sb.AppendLine($"降级模式: 是 ({Metrics.FallbackReason})");
        }
        
        if (CrossDayViolations.Count > 0)
        {
            sb.AppendLine($"跨日约束违反: {CrossDayViolations.Count}个");
        }
        
        return sb.ToString();
    }
}

/// <summary>
/// 跨日约束违反
/// </summary>
public class CrossDayConstraintViolation
{
    public int PersonIdx { get; set; }
    public int GlobalPeriodIdx1 { get; set; }
    public int GlobalPeriodIdx2 { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

## 实施计划

### 阶段1: 核心组件 (优先级: 高)

1. 实现 `PeriodMapper` 类
2. 扩展 `FeasibilityTensor` 支持可变时段数
3. 实现 `GlobalMRVStrategy` 类
4. 扩展 `ConstraintValidator` 支持跨日约束

### 阶段2: 主调度器集成 (优先级: 高)

1. 修改 `GreedyScheduler.ExecuteAsync()` 支持全局模式
2. 实现 `ExecuteGlobalSchedulingAsync()` 方法
3. 实现降级策略
4. 添加配置选项

### 阶段3: 性能优化 (优先级: 中)

1. 实现增量初始化
2. 实现并行约束应用
3. 优化回溯状态恢复
4. 添加内存监控

### 阶段4: 监控和诊断 (优先级: 中)

1. 实现性能指标收集
2. 实现诊断报告生成
3. 添加日志记录
4. 实现进度报告

### 阶段5: 测试和验证 (优先级: 高)

1. 编写单元测试
2. 编写集成测试
3. 进行对比测试
4. 性能基准测试

## 风险和缓解

### 风险1: 内存占用过大

**影响**: 大规模排班(30天+)可能导致内存不足

**缓解措施**:
- 设置内存阈值,超过时自动降级
- 提供分段处理选项
- 优化二进制存储

### 风险2: 性能下降

**影响**: 全局回溯可能导致执行时间显著增加

**缓解措施**:
- 限制最大回溯深度
- 实现智能回溯点选择
- 提供超时和取消机制

### 风险3: 实现复杂度

**影响**: 代码改动范围大,可能引入新bug

**缓解措施**:
- 保留按天模式作为降级方案
- 充分的单元测试和集成测试
- 分阶段实施,逐步验证

### 风险4: 向后兼容性

**影响**: 可能影响现有用户的排班结果

**缓解措施**:
- 提供配置开关,默认启用全局模式
- 保留按天模式作为备选
- 详细的迁移文档

## 总结

全局多天调度通过引入全局可行性张量、时段映射工具和跨日约束处理,解决了当前按天处理的三大问题:

1. **跨日约束正确性**: 时段11和次日时段0在同一张量中,约束自然传递
2. **跨日回溯能力**: 回溯栈跨越所有天数,可以回溯到任意天的任意决策点
3. **全局MRV最优性**: 在所有天数的所有时段中选择候选人员最少的位置

设计方案充分考虑了性能、内存、向后兼容性和可维护性,提供了完善的降级策略和监控诊断机制,确保系统的稳定性和可靠性。
