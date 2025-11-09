# 数据导入导出错误处理和日志记录

本目录包含数据导入导出功能的增强错误处理和日志记录组件。

## 组件概述

### 1. ErrorMessageTranslator（错误消息转换器）

**文件**: `ErrorMessageTranslator.cs`

**功能**: 将技术性的异常消息转换为用户友好的错误消息。

**主要方法**:
- `TranslateException(Exception, string)`: 将异常转换为用户友好的消息
- `GetDetailedErrorInfo(Exception)`: 获取详细的技术错误信息（用于日志记录）

**支持的异常类型**:
- `FileNotFoundException`: 文件未找到
- `DirectoryNotFoundException`: 目录未找到
- `IOException`: 文件操作错误
- `UnauthorizedAccessException`: 权限不足
- `JsonException`: JSON 格式错误
- `SqliteException`: 数据库错误
- `OutOfMemoryException`: 内存不足
- 其他通用异常

**使用示例**:
```csharp
try
{
    // 执行操作
}
catch (Exception ex)
{
    // 获取用户友好的错误消息
    var userMessage = ErrorMessageTranslator.TranslateException(ex, "数据导出");
    
    // 获取详细的技术信息（用于日志）
    var technicalDetails = ErrorMessageTranslator.GetDetailedErrorInfo(ex);
    _logger.LogError(technicalDetails);
}
```

### 2. ErrorRecoverySuggester（错误恢复建议器）

**文件**: `ErrorRecoverySuggester.cs`

**功能**: 根据错误类型提供具体的恢复建议。

**主要方法**:
- `GetRecoverySuggestions(Exception, string)`: 获取恢复建议列表
- `FormatSuggestions(List<string>)`: 格式化建议为用户友好的字符串

**建议类型**:
- 文件访问问题的解决方案
- 权限问题的解决方案
- 数据格式问题的解决方案
- 数据库错误的解决方案
- 内存问题的解决方案

**使用示例**:
```csharp
try
{
    // 执行操作
}
catch (Exception ex)
{
    // 获取恢复建议
    var suggestions = ErrorRecoverySuggester.GetRecoverySuggestions(ex, "导入");
    
    // 格式化并显示给用户
    var formattedSuggestions = ErrorRecoverySuggester.FormatSuggestions(suggestions);
    Console.WriteLine(formattedSuggestions);
}
```

### 3. OperationAuditLogger（操作审计日志记录器）

**文件**: `OperationAuditLogger.cs`

**功能**: 记录所有导入导出操作的审计日志。

**主要方法**:
- `LogExportOperation(ExportResult, string?)`: 记录导出操作
- `LogImportOperation(string, ImportOptions, ImportResult, string?)`: 记录导入操作
- `LogValidationOperation(string, ValidationResult, string?)`: 记录验证操作
- `LogErrorOperation(string, Exception, string?, string?)`: 记录错误操作

**日志位置**:
- 默认路径: `%USERPROFILE%\Documents\AutoScheduling3\AuditLogs\`
- 文件名格式: `audit_yyyyMMdd.log`

**日志格式**:
```
[2024-01-15 10:30:45.123] [Export] [User: username] [Status: SUCCESS] [Details: {...}]
[2024-01-15 10:35:22.456] [Import] [User: username] [Status: FAILED] [Details: {...}] [Error: ...]
```

**记录的信息**:
- 时间戳（精确到毫秒）
- 操作类型（Export/Import/Validation）
- 用户名
- 操作状态（SUCCESS/FAILED）
- 详细信息（JSON 格式）
- 错误消息（如果失败）

**使用示例**:
```csharp
var auditLogger = new OperationAuditLogger();

// 记录导出操作
auditLogger.LogExportOperation(exportResult);

// 记录导入操作
auditLogger.LogImportOperation(filePath, options, importResult);

// 记录验证操作
auditLogger.LogValidationOperation(filePath, validationResult);

// 记录错误
auditLogger.LogErrorOperation("Import", exception, "Additional context");
```

## 集成到 DataImportExportService

`DataImportExportService` 已经集成了所有这些组件：

### 导出操作
1. 执行导出
2. 如果成功：记录审计日志
3. 如果失败：
   - 记录详细的技术错误信息
   - 转换为用户友好的错误消息
   - 记录审计日志
   - 生成并记录恢复建议

### 导入操作
1. 执行导入
2. 如果成功：记录审计日志
3. 如果失败：
   - 记录详细的技术错误信息
   - 转换为用户友好的错误消息
   - 记录审计日志
   - 生成并记录恢复建议

### 验证操作
1. 执行验证
2. 记录审计日志
3. 如果失败：
   - 记录详细的技术错误信息
   - 转换为用户友好的错误消息
   - 生成并记录恢复建议

## 错误处理流程

```
异常发生
    ↓
记录详细技术信息（ErrorMessageTranslator.GetDetailedErrorInfo）
    ↓
转换为用户友好消息（ErrorMessageTranslator.TranslateException）
    ↓
记录审计日志（OperationAuditLogger）
    ↓
生成恢复建议（ErrorRecoverySuggester.GetRecoverySuggestions）
    ↓
记录恢复建议到日志
    ↓
返回用户友好的错误消息给 UI
```

## 日志级别

系统使用三个日志级别：

1. **INFO** (`_logger.Log`): 正常操作信息
   - 操作开始/完成
   - 数据统计
   - 进度信息

2. **WARNING** (`_logger.LogWarning`): 警告信息
   - 非致命错误
   - 数据完整性警告
   - 性能警告

3. **ERROR** (`_logger.LogError`): 错误信息
   - 操作失败
   - 异常详情
   - 技术错误信息

## 最佳实践

### 1. 错误消息
- 对用户显示友好的错误消息
- 在日志中记录详细的技术信息
- 始终提供恢复建议

### 2. 审计日志
- 记录所有重要操作
- 包含足够的上下文信息
- 定期清理旧日志文件

### 3. 异常处理
- 捕获具体的异常类型
- 提供有意义的错误消息
- 不要吞没异常
- 在适当的级别处理异常

### 4. 日志记录
- 使用适当的日志级别
- 包含相关的上下文信息
- 避免记录敏感信息
- 保持日志消息简洁明了

## 扩展指南

### 添加新的异常类型支持

在 `ErrorMessageTranslator.cs` 中添加新的 case：

```csharp
return exception switch
{
    // ... 现有的 cases ...
    YourNewException => "用户友好的错误消息",
    _ => $"{context}失败：{exception.Message}"
};
```

### 添加新的恢复建议

在 `ErrorRecoverySuggester.cs` 中添加新的 case：

```csharp
return exception switch
{
    // ... 现有的 cases ...
    YourNewException => new List<string>
    {
        "建议 1",
        "建议 2",
        "建议 3"
    },
    _ => new List<string> { "默认建议" }
};
```

### 自定义审计日志位置

```csharp
var customAuditLogger = new OperationAuditLogger("C:\\CustomPath\\AuditLogs");
```

## 故障排除

### 问题：审计日志未创建
**解决方案**:
1. 检查目录权限
2. 确认磁盘空间充足
3. 查看调试输出窗口的错误消息

### 问题：错误消息不够友好
**解决方案**:
1. 检查 `ErrorMessageTranslator` 是否支持该异常类型
2. 添加新的异常类型支持
3. 更新错误消息模板

### 问题：恢复建议不适用
**解决方案**:
1. 检查 `ErrorRecoverySuggester` 中的建议
2. 根据实际情况更新建议
3. 添加更具体的异常类型处理

## 性能考虑

- 审计日志使用文件锁确保线程安全
- 详细错误信息仅在发生错误时生成
- 日志文件按天分割，避免单个文件过大
- 建议定期清理旧的审计日志文件

## 安全考虑

- 不在审计日志中记录敏感信息（如密码、个人数据）
- 审计日志文件应有适当的访问权限
- 定期审查审计日志以检测异常活动
- 考虑加密存储审计日志文件
