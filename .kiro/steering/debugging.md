# 调试规范

## 调试策略

当用户要求解决问题时，根据问题复杂度采取不同策略：

### 简单问题
- 直接分析代码逻辑
- 快速定位并修复
- 无需添加额外调试信息

### 复杂问题
采用"添加调试 → 确认问题 → 清理调试"的三步流程：

#### 1. 添加调试信息
在关键位置添加临时调试代码，帮助定位问题：

```csharp
// 临时调试：记录关键变量状态
_logger?.LogDebug($"[DEBUG][{nameof(ClassName)}] 变量名: {value}, 状态: {state}");

// 临时调试：追踪执行流程
_logger?.LogDebug($"[DEBUG][{nameof(ClassName)}] 进入方法: {methodName}, 参数: {param}");

// 临时调试：捕获异常详情
catch (Exception ex)
{
    _logger?.LogError($"[DEBUG][{nameof(ClassName)}] 异常详情: {ex.Message}, 堆栈: {ex.StackTrace}");
    throw;
}
```

**调试信息特征**：
- 使用 `[DEBUG][类名]` 前缀标记，通过 `nameof()` 获取类名
- 记录关键变量、执行路径、异常详情
- 使用现有的 `ILogger` 接口
- 添加注释说明 `// 临时调试：...`

#### 2. 用户确认
- 要求用户运行程序并提供调试输出
- 根据输出分析问题根源
- 确定修复方案

#### 3. 清理调试信息
问题解决后，**必须移除**所有临时调试代码：
- 删除带 `[DEBUG]` 前缀的日志
- 删除 `// 临时调试：` 注释的代码块
- 保持代码整洁，仅保留必要的错误处理日志

## 调试工具

### 日志记录
- 使用项目现有的 `ILogger` 接口（`Data/Logging/ILogger.cs`）
- 调试级别：`LogDebug()` 用于临时调试，`LogError()` 用于错误记录
- 生产代码保留 `LogError()` 和 `LogWarning()`，移除 `LogDebug()`

### 诊断报告
- 对于算法类问题，使用现有的诊断 DTO（如 `BacktrackingDiagnosticReport`）
- 提供结构化的诊断信息，便于分析

## 注意事项

1. **临时性原则**：调试代码是临时的，问题解决后必须清理
2. **标记清晰**：使用统一的标记（`[DEBUG]` 和注释）便于识别和清理
3. **不影响性能**：调试代码不应显著影响程序性能
4. **保护隐私**：不记录敏感信息（密码、个人数据等）
5. **完整清理**：确认问题解决后，彻底移除所有调试代码

## 示例流程

**问题**：排班算法在某些情况下失败

**步骤 1 - 添加调试**：
```csharp
// 临时调试：记录回溯状态
_logger?.LogDebug($"[DEBUG][{nameof(BacktrackingEngine)}] 回溯深度: {depth}, 剩余选择: {remainingChoices}");
```

**步骤 2 - 用户确认**：
用户运行程序，提供输出：
```
[DEBUG][BacktrackingEngine] 回溯深度: 5, 剩余选择: 0
```

**步骤 3 - 清理调试**：
```csharp
// 移除临时调试代码
// _logger?.LogDebug($"[DEBUG][{nameof(BacktrackingEngine)}] 回溯深度: {depth}, 剩余选择: {remainingChoices}");
```

最终提交时完全删除注释行。
