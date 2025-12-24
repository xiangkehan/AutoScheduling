# 回溯引擎异常类

本目录包含回溯引擎使用的自定义异常类。

## 异常类型

### BacktrackDepthExceededException
**用途**: 当回溯深度超过配置的最大值时抛出

**属性**:
- `MaxDepth`: 配置的最大回溯深度
- `CurrentDepth`: 当前回溯深度

**使用场景**:
- 回溯深度达到 `BacktrackingConfig.MaxBacktrackDepth` 时
- 防止无限回溯导致的性能问题

**对应需求**: 1.3, 2.4

### StateRestorationException
**用途**: 当状态快照恢复失败时抛出

**属性**:
- `CorruptedSnapshot`: 损坏的状态快照对象（可选）

**使用场景**:
- 状态快照数据损坏或不完整
- 恢复过程中发生意外错误
- 快照版本不兼容

**对应需求**: 2.4

## 使用示例

```csharp
// 检查回溯深度
if (currentDepth >= maxDepth)
{
    throw new BacktrackDepthExceededException(maxDepth, currentDepth);
}

// 恢复状态时的错误处理
try
{
    RestoreState(snapshot);
}
catch (Exception ex)
{
    throw new StateRestorationException(snapshot, "状态恢复失败", ex);
}
```

## 错误处理策略

1. **BacktrackDepthExceededException**: 
   - 捕获后应停止回溯
   - 返回部分结果
   - 生成诊断报告

2. **StateRestorationException**:
   - 捕获后尝试使用更早的快照
   - 如果无法恢复，重新开始排班
   - 记录详细错误日志

## 相关文件

- `SchedulingEngine/Core/BacktrackingEngine.cs`: 使用这些异常的主要组件
- `DTOs/BacktrackingDiagnosticReport.cs`: 错误诊断报告
- `DTOs/BacktrackingStatistics.cs`: 回溯统计信息
