# 约束集成测试场景

本文档描述了如何验证回溯机制与约束验证系统的集成。

## 使用约束一致性验证

### 基本用法

```csharp
// 在排班完成后验证约束一致性
var backtrackingEngine = new BacktrackingEngine(
    context, tensor, mrvStrategy, constraintValidator, 
    softConstraintCalculator, config, logger);

// 执行排班...
await PerformScheduling();

// 验证约束一致性
var report = backtrackingEngine.VerifyConstraintConsistency(date);

if (!report.IsConsistent)
{
    Console.WriteLine(report.ToString());
    // 处理约束违反...
}
```

### 在GreedyScheduler中集成

```csharp
// 在PerformGreedyAssignmentsAsync方法的末尾添加验证
if (_config.Backtracking.EnableBacktracking && _config.VerifyConstraintConsistency)
{
    var consistencyReport = backtrackingEngine.VerifyConstraintConsistency(date);
    
    if (!consistencyReport.IsConsistent)
    {
        _logger.LogWarning($"约束一致性验证失败:\n{consistencyReport}");
        
        // 可选：将验证结果添加到进度报告
        if (progress != null)
        {
            var warningReport = new SchedulingProgressReport
            {
                CurrentStage = SchedulingStage.Backtracking,
                StageDescription = "约束一致性验证失败",
                Warnings = consistencyReport.Issues,
                HasErrors = true
            };
            progress.Report(warningReport);
        }
    }
    else
    {
        _logger.Log($"约束一致性验证通过: {consistencyReport.Summary}");
    }
}
```

## 测试场景

### 场景1: 基本约束验证

**目的**: 验证所有已分配的时段都满足硬约束

**步骤**:
1. 创建简单的调度上下文（3人员，2哨位，1天）
2. 执行贪心分配（不触发回溯）
3. 调用 `VerifyConstraintConsistency()`
4. 验证报告显示 `IsConsistent = true`

**预期结果**: 
- `TotalAssignmentsChecked > 0`
- `ConstraintViolationsFound = 0`
- `IsConsistent = true`

### 场景2: 回溯后约束验证

**目的**: 验证回溯后的分配仍然满足所有约束

**步骤**:
1. 创建会触发死胡同的调度上下文
2. 执行贪心分配，触发回溯
3. 回溯成功后，调用 `VerifyConstraintConsistency()`
4. 验证所有分配都满足约束

**预期结果**:
- 回溯次数 > 0
- `IsConsistent = true`
- 无约束违反

### 场景3: 夜哨唯一约束验证

**目的**: 验证夜哨唯一约束在回溯场景下的正确性

**步骤**:
1. 创建调度上下文，包含夜哨时段（11, 0, 1, 2）
2. 执行分配，确保有人员被分配到夜哨
3. 如果触发回溯，验证回溯后夜哨约束仍然有效
4. 调用 `VerifyConstraintConsistency()`
5. 检查报告中是否有夜哨唯一约束违反

**预期结果**:
- 每个人员最多只在一个夜哨时段
- `Issues` 中不包含 "夜哨唯一约束违反"

### 场景4: 时段不连续约束验证

**目的**: 验证时段不连续约束在回溯场景下的正确性

**步骤**:
1. 创建调度上下文
2. 执行分配，可能触发回溯
3. 调用 `VerifyConstraintConsistency()`
4. 检查是否有人员在连续时段被分配

**预期结果**:
- 没有人员在相邻时段（如5和6）被分配
- 没有跨日连续分配（如前一天时段11和当天时段0）
- `Issues` 中不包含 "时段不连续约束违反"

### 场景5: 人员时段唯一性验证

**目的**: 验证每个人员在每个时段只在一个哨位

**步骤**:
1. 创建调度上下文
2. 执行分配
3. 调用 `VerifyConstraintConsistency()`
4. 检查是否有人员在同一时段被分配到多个哨位

**预期结果**:
- 每个人员在每个时段最多只在一个哨位
- `Issues` 中不包含 "人员时段唯一性约束违反"

### 场景6: 状态恢复后约束验证

**目的**: 验证状态恢复后约束仍然有效

**步骤**:
1. 创建调度上下文
2. 执行分配，记录初始状态
3. 触发回溯，恢复状态
4. 在状态恢复后立即调用 `VerifyConstraintConsistency()`
5. 验证恢复后的状态满足所有约束

**预期结果**:
- 状态恢复后，所有约束仍然有效
- `IsConsistent = true`

## 手动测试步骤

### 步骤1: 准备测试环境

```csharp
// 创建测试上下文
var context = new SchedulingContext
{
    StartDate = DateTime.Today,
    EndDate = DateTime.Today,
    Personnel = CreateTestPersonnel(5),
    Positions = CreateTestPositions(3),
    Skills = CreateTestSkills(2)
};

// 初始化映射
context.InitializeMappings();
context.InitializePersonScoreStates();
context.InitializeAssignments();
```

### 步骤2: 配置回溯机制

```csharp
var config = new GreedySchedulerConfig
{
    Backtracking = new BacktrackingConfig
    {
        EnableBacktracking = true,
        MaxBacktrackDepth = 20,
        MaxCandidatesPerDecision = 3,
        LogBacktracking = true
    }
};
```

### 步骤3: 执行排班

```csharp
var scheduler = new GreedyScheduler(context, config);
var schedule = await scheduler.ExecuteAsync();
```

### 步骤4: 验证约束一致性

```csharp
// 注意：需要访问BacktrackingEngine实例
// 可以在GreedyScheduler中添加公共方法来暴露验证功能
var report = scheduler.VerifyConstraintConsistency(DateTime.Today);

Console.WriteLine(report.ToString());

if (!report.IsConsistent)
{
    // 处理约束违反
    foreach (var issue in report.Issues)
    {
        Console.WriteLine($"问题: {issue}");
    }
}
```

## 自动化测试建议

### 单元测试

```csharp
[Fact]
public void VerifyConstraintConsistency_WithValidAssignments_ReturnsConsistent()
{
    // Arrange
    var context = CreateTestContext();
    var engine = CreateBacktrackingEngine(context);
    
    // 手动创建有效的分配
    context.RecordAssignment(DateTime.Today, 0, 0, 0);
    context.RecordAssignment(DateTime.Today, 1, 1, 1);
    
    // Act
    var report = engine.VerifyConstraintConsistency(DateTime.Today);
    
    // Assert
    Assert.True(report.IsConsistent);
    Assert.Equal(2, report.TotalAssignmentsChecked);
    Assert.Equal(0, report.ConstraintViolationsFound);
}

[Fact]
public void VerifyConstraintConsistency_WithNightShiftViolation_ReturnsInconsistent()
{
    // Arrange
    var context = CreateTestContext();
    var engine = CreateBacktrackingEngine(context);
    
    // 手动创建违反夜哨唯一约束的分配
    context.RecordAssignment(DateTime.Today, 11, 0, 0); // 夜哨时段11
    context.RecordAssignment(DateTime.Today, 0, 1, 0);  // 夜哨时段0，同一人员
    
    // Act
    var report = engine.VerifyConstraintConsistency(DateTime.Today);
    
    // Assert
    Assert.False(report.IsConsistent);
    Assert.Contains("夜哨唯一约束违反", report.Issues[0]);
}
```

### 集成测试

```csharp
[Fact]
public async Task GreedyScheduler_WithBacktracking_MaintainsConstraintConsistency()
{
    // Arrange
    var context = CreateComplexTestContext(); // 会触发回溯的场景
    var config = new GreedySchedulerConfig
    {
        Backtracking = new BacktrackingConfig
        {
            EnableBacktracking = true,
            MaxBacktrackDepth = 50
        }
    };
    var scheduler = new GreedyScheduler(context, config);
    
    // Act
    var schedule = await scheduler.ExecuteAsync();
    
    // 获取回溯引擎并验证约束一致性
    var report = scheduler.GetBacktrackingEngine()
        .VerifyConstraintConsistency(DateTime.Today);
    
    // Assert
    Assert.True(report.IsConsistent, 
        $"约束一致性验证失败: {report.Summary}");
    Assert.Empty(report.Issues);
}
```

## 性能考虑

约束一致性验证会遍历所有分配并重新验证约束，可能影响性能。建议：

1. **开发环境**: 启用完整验证
2. **生产环境**: 仅在检测到问题时启用
3. **测试环境**: 始终启用以捕获回归问题

可以通过配置控制：

```csharp
public class BacktrackingConfig
{
    /// <summary>
    /// 是否启用约束一致性验证（仅用于调试）
    /// </summary>
    public bool EnableConstraintConsistencyVerification { get; set; } = false;
}
```

## 故障排查

### 问题1: 验证报告显示约束违反

**可能原因**:
- 状态恢复不完整
- 约束应用逻辑有bug
- 回溯逻辑有bug

**排查步骤**:
1. 检查报告中的具体违反项
2. 查看日志中的回溯历史
3. 使用调试器跟踪状态恢复过程
4. 验证约束验证器的逻辑

### 问题2: 验证性能问题

**可能原因**:
- 分配数量过多
- 约束验证逻辑复杂

**优化措施**:
1. 仅在必要时启用验证
2. 使用采样验证（仅验证部分分配）
3. 缓存约束验证结果

### 问题3: 跨日约束验证失败

**可能原因**:
- 跨日状态未正确处理
- 日期边界处理有bug

**排查步骤**:
1. 检查时段11和次日时段0的分配
2. 验证跨日约束的应用逻辑
3. 检查日期计算是否正确

## 总结

约束一致性验证是确保回溯机制正确性的重要工具。通过系统地验证所有约束，可以：

1. ✅ 确保回溯使用相同的约束验证器
2. ✅ 验证约束验证逻辑未被修改
3. ✅ 检测状态恢复的问题
4. ✅ 提供详细的诊断信息
5. ✅ 支持自动化测试

建议在开发和测试阶段启用完整的约束一致性验证，以确保回溯机制的正确性和可靠性。
