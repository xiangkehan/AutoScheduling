# 错误处理和日志记录 - 快速参考

## 快速开始

### 1. 使用错误消息转换器

```csharp
using AutoScheduling3.Services.ImportExport;

try
{
    // 你的代码
}
catch (Exception ex)
{
    // 获取用户友好的错误消息
    var userMessage = ErrorMessageTranslator.TranslateException(ex, "操作名称");
    
    // 显示给用户
    MessageBox.Show(userMessage);
    
    // 记录详细的技术信息到日志
    var details = ErrorMessageTranslator.GetDetailedErrorInfo(ex);
    _logger.LogError(details);
}
```

### 2. 使用错误恢复建议器

```csharp
using AutoScheduling3.Services.ImportExport;

try
{
    // 你的代码
}
catch (Exception ex)
{
    // 获取恢复建议
    var suggestions = ErrorRecoverySuggester.GetRecoverySuggestions(ex, "操作名称");
    
    // 格式化建议
    var formatted = ErrorRecoverySuggester.FormatSuggestions(suggestions);
    
    // 显示给用户或记录到日志
    Console.WriteLine(formatted);
}
```

### 3. 使用操作审计日志

```csharp
using AutoScheduling3.Services.ImportExport;

// 创建审计日志记录器（通常在服务构造函数中）
var auditLogger = new OperationAuditLogger();

// 记录导出操作
auditLogger.LogExportOperation(exportResult);

// 记录导入操作
auditLogger.LogImportOperation(filePath, options, importResult);

// 记录验证操作
auditLogger.LogValidationOperation(filePath, validationResult);

// 记录错误
auditLogger.LogErrorOperation("操作类型", exception, "上下文信息");
```

## 完整示例

### 导出操作的完整错误处理

```csharp
public async Task<ExportResult> ExportDataAsync(string filePath)
{
    var result = new ExportResult();
    var auditLogger = new OperationAuditLogger();
    
    try
    {
        _logger.Log("开始导出数据");
        
        // 执行导出操作
        // ... 你的导出代码 ...
        
        result.Success = true;
        _logger.Log("导出成功");
        
        // 记录审计日志
        auditLogger.LogExportOperation(result);
    }
    catch (Exception ex)
    {
        // 记录详细的技术错误
        _logger.LogError($"导出失败: {ex.Message}");
        _logger.LogError(ErrorMessageTranslator.GetDetailedErrorInfo(ex));
        
        // 转换为用户友好的消息
        result.Success = false;
        result.ErrorMessage = ErrorMessageTranslator.TranslateException(ex, "数据导出");
        
        // 记录审计日志
        auditLogger.LogExportOperation(result);
        
        // 获取并记录恢复建议
        var suggestions = ErrorRecoverySuggester.GetRecoverySuggestions(ex, "导出");
        var formatted = ErrorRecoverySuggester.FormatSuggestions(suggestions);
        _logger.Log($"恢复建议: {formatted}");
    }
    
    return result;
}
```

### 导入操作的完整错误处理

```csharp
public async Task<ImportResult> ImportDataAsync(string filePath, ImportOptions options)
{
    var result = new ImportResult();
    var auditLogger = new OperationAuditLogger();
    
    try
    {
        _logger.Log($"开始导入数据: {filePath}");
        
        // 执行导入操作
        // ... 你的导入代码 ...
        
        result.Success = true;
        _logger.Log("导入成功");
        
        // 记录审计日志
        auditLogger.LogImportOperation(filePath, options, result);
    }
    catch (Exception ex)
    {
        // 记录详细的技术错误
        _logger.LogError($"导入失败: {ex.Message}");
        _logger.LogError(ErrorMessageTranslator.GetDetailedErrorInfo(ex));
        
        // 转换为用户友好的消息
        result.Success = false;
        result.ErrorMessage = ErrorMessageTranslator.TranslateException(ex, "数据导入");
        
        // 记录审计日志
        auditLogger.LogImportOperation(filePath, options, result);
        
        // 获取并记录恢复建议
        var suggestions = ErrorRecoverySuggester.GetRecoverySuggestions(ex, "导入");
        var formatted = ErrorRecoverySuggester.FormatSuggestions(suggestions);
        _logger.Log($"恢复建议: {formatted}");
    }
    
    return result;
}
```

## 常见场景

### 场景 1: 文件不存在

```csharp
try
{
    var data = await File.ReadAllTextAsync(filePath);
}
catch (FileNotFoundException ex)
{
    // 用户消息: "数据导入失败：找不到指定的文件。请确认文件路径是否正确。"
    var message = ErrorMessageTranslator.TranslateException(ex, "数据导入");
    
    // 恢复建议:
    // 1. 确认文件路径是否正确
    // 2. 检查文件是否已被移动或删除
    // 3. 尝试重新选择文件
    var suggestions = ErrorRecoverySuggester.GetRecoverySuggestions(ex, "导入");
}
```

### 场景 2: 权限不足

```csharp
try
{
    await File.WriteAllTextAsync(filePath, data);
}
catch (UnauthorizedAccessException ex)
{
    // 用户消息: "数据导出失败：没有访问文件的权限。请以管理员身份运行或检查文件权限。"
    var message = ErrorMessageTranslator.TranslateException(ex, "数据导出");
    
    // 恢复建议:
    // 1. 以管理员身份运行应用程序
    // 2. 检查文件和目录的权限设置
    // 3. 确认文件未被设置为只读
    // 4. 选择有写入权限的其他位置
    var suggestions = ErrorRecoverySuggester.GetRecoverySuggestions(ex, "导出");
}
```

### 场景 3: JSON 格式错误

```csharp
try
{
    var data = JsonSerializer.Deserialize<ExportData>(json);
}
catch (JsonException ex)
{
    // 用户消息: "数据导入失败：文件格式不正确。请确认这是一个有效的导出文件。"
    var message = ErrorMessageTranslator.TranslateException(ex, "数据导入");
    
    // 恢复建议:
    // 1. 确认选择的是有效的导出文件
    // 2. 检查文件是否完整（未被截断或损坏）
    // 3. 尝试重新导出数据
    // 4. 使用文本编辑器检查 JSON 格式是否正确
    var suggestions = ErrorRecoverySuggester.GetRecoverySuggestions(ex, "导入");
}
```

### 场景 4: 数据库错误

```csharp
try
{
    await _repository.CreateAsync(entity);
}
catch (SqliteException ex)
{
    // 根据错误代码提供不同的消息
    // 例如: "数据导入失败：数据违反了唯一性约束。可能存在重复的记录。"
    var message = ErrorMessageTranslator.TranslateException(ex, "数据导入");
    
    // 根据错误代码提供不同的建议
    var suggestions = ErrorRecoverySuggester.GetRecoverySuggestions(ex, "导入");
}
```

## 日志级别使用指南

### INFO 级别 (_logger.Log)
用于记录正常的操作流程：
```csharp
_logger.Log("开始导出数据");
_logger.Log($"导出了 {count} 条记录");
_logger.Log("导出完成");
```

### WARNING 级别 (_logger.LogWarning)
用于记录警告信息（非致命错误）：
```csharp
_logger.LogWarning("数据完整性验证警告");
_logger.LogWarning($"跳过了 {count} 条无效记录");
```

### ERROR 级别 (_logger.LogError)
用于记录错误信息：
```csharp
_logger.LogError($"操作失败: {ex.Message}");
_logger.LogError(ErrorMessageTranslator.GetDetailedErrorInfo(ex));
```

## 审计日志查看

### 日志位置
```
Windows: C:\Users\{用户名}\Documents\AutoScheduling3\AuditLogs\
文件名: audit_20240115.log
```

### 日志格式
```
[时间戳] [操作类型] [用户] [状态] [详细信息] [错误消息]
```

### 示例日志条目
```
[2024-01-15 10:30:45.123] [Export] [User: john] [Status: SUCCESS] [Details: {"FilePath":"C:\\export.json","FileSize":12345}]
[2024-01-15 10:35:22.456] [Import] [User: john] [Status: FAILED] [Details: {"FilePath":"C:\\import.json"}] [Error: 文件格式不正确]
```

## 最佳实践

### ✅ 推荐做法

1. **始终转换错误消息**
```csharp
// 好的做法
result.ErrorMessage = ErrorMessageTranslator.TranslateException(ex, "操作名称");

// 不好的做法
result.ErrorMessage = ex.Message; // 技术性太强
```

2. **记录详细的技术信息**
```csharp
// 好的做法
_logger.LogError(ErrorMessageTranslator.GetDetailedErrorInfo(ex));

// 不好的做法
_logger.LogError(ex.Message); // 信息不够详细
```

3. **提供恢复建议**
```csharp
// 好的做法
var suggestions = ErrorRecoverySuggester.GetRecoverySuggestions(ex, "操作");
_logger.Log(ErrorRecoverySuggester.FormatSuggestions(suggestions));

// 不好的做法
// 不提供任何建议
```

4. **记录审计日志**
```csharp
// 好的做法
auditLogger.LogExportOperation(result); // 成功和失败都记录

// 不好的做法
if (result.Success) {
    auditLogger.LogExportOperation(result); // 只记录成功
}
```

### ❌ 避免的做法

1. **不要吞没异常**
```csharp
// 错误
try {
    // 操作
} catch { }

// 正确
try {
    // 操作
} catch (Exception ex) {
    _logger.LogError(ErrorMessageTranslator.GetDetailedErrorInfo(ex));
    throw; // 或者适当处理
}
```

2. **不要在日志中记录敏感信息**
```csharp
// 错误
_logger.Log($"用户密码: {password}");

// 正确
_logger.Log("用户认证成功");
```

3. **不要使用通用的错误消息**
```csharp
// 错误
result.ErrorMessage = "操作失败";

// 正确
result.ErrorMessage = ErrorMessageTranslator.TranslateException(ex, "数据导出");
```

## 故障排除

### 问题: 审计日志文件未创建
**检查**:
1. 目录权限
2. 磁盘空间
3. 查看调试输出窗口

### 问题: 错误消息显示为英文
**原因**: 异常类型未在 `ErrorMessageTranslator` 中处理
**解决**: 添加新的异常类型支持

### 问题: 恢复建议不适用
**原因**: 建议过于通用
**解决**: 在 `ErrorRecoverySuggester` 中添加更具体的处理

## 扩展示例

### 添加新的异常类型

1. 在 `ErrorMessageTranslator.cs` 中:
```csharp
return exception switch
{
    // ... 现有的 cases ...
    YourNewException => "用户友好的错误消息",
    _ => $"{context}失败：{exception.Message}"
};
```

2. 在 `ErrorRecoverySuggester.cs` 中:
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

## 相关文档

- [完整文档](README.md) - 详细的组件说明
- [实现总结](IMPLEMENTATION_SUMMARY.md) - 实现细节和测试建议
