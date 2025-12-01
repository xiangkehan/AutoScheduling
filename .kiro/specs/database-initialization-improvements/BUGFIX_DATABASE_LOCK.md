# 数据导入时数据库锁定问题修复

## 问题描述

在导入数据时，UI 卡住，输出日志显示：
```
[2025-11-22 19:18:34.815] [DataImportExport] [WARN] Personals: Failed to compare record 1 - SQLite Error 6: 'database table is locked: Personals'.
```

问题持续出现，每次比较记录时都会超时（约30秒），导致导入过程极其缓慢且 UI 无响应。

## 根本原因

在 `ImporterBase<TDto, TModel>` 的 `FilterUnchangedRecordsAsync` 方法中，对每条需要更新的记录都调用了 `GetExistingRecordAsync` 方法进行单独查询。

在事务中，这会导致：
1. 批量插入操作锁定了表
2. 随后的单独 SELECT 查询尝试访问同一个表
3. SQLite 的锁机制导致查询被阻塞，直到超时

## 解决方案

### 1. 修改 `ImporterBase.cs`

将逐个查询改为批量查询：

**修改前：**
```csharp
private async Task<List<TDto>> FilterUnchangedRecordsAsync(
    List<TDto> records,
    ImportContext context)
{
    var recordsToUpdate = new List<TDto>();

    foreach (var record in records)
    {
        var existing = await GetExistingRecordAsync(GetRecordId(record), context);
        if (existing != null && !CompareRecords(record, existing))
        {
            recordsToUpdate.Add(record);
        }
    }

    return recordsToUpdate;
}
```

**修改后：**
```csharp
private async Task<List<TDto>> FilterUnchangedRecordsAsync(
    List<TDto> records,
    ImportContext context)
{
    var recordsToUpdate = new List<TDto>();
    var tableName = GetTableName();

    // 批量获取所有需要比较的现有记录，避免逐个查询导致锁定
    var idsToFetch = records.Select(GetRecordId).ToList();
    var existingRecords = await GetExistingRecordsBatchAsync(idsToFetch, context);

    foreach (var record in records)
    {
        var recordId = GetRecordId(record);
        
        if (existingRecords.TryGetValue(recordId, out var existing))
        {
            if (!CompareRecords(record, existing))
            {
                recordsToUpdate.Add(record);
            }
        }
        else
        {
            recordsToUpdate.Add(record);
        }
    }

    return recordsToUpdate;
}
```

### 2. 添加新的抽象方法

在 `ImporterBase` 中添加批量获取方法：

```csharp
/// <summary>
/// 批量从数据库获取现有记录
/// 使用批量查询避免在事务中逐个查询导致表锁定
/// </summary>
protected abstract Task<Dictionary<int, TModel>> GetExistingRecordsBatchAsync(
    List<int> ids, 
    ImportContext context);
```

### 3. 在所有 Importer 中实现批量获取

为以下 Importer 添加了 `GetExistingRecordsBatchAsync` 实现：
- `SkillImporter`
- `PersonnelImporter`
- `PositionImporter`
- `TemplateImporter`
- `HolidayConfigImporter`
- `FixedAssignmentImporter`
- `ManualAssignmentImporter`

示例实现（PersonnelImporter）：
```csharp
protected override async Task<Dictionary<int, Personal>> GetExistingRecordsBatchAsync(
    List<int> ids, 
    ImportContext context)
{
    var result = new Dictionary<int, Personal>();

    if (ids == null || ids.Count == 0)
    {
        return result;
    }

    // 使用仓储的批量查询方法
    var allPersonnel = await _personnelRepository.GetAllAsync();
    
    // 过滤出需要的记录
    foreach (var person in allPersonnel)
    {
        if (ids.Contains(person.Id))
        {
            result[person.Id] = person;
        }
    }

    return result;
}
```

## 性能改进

### 第一次优化（批量查询）
- **修改前**：对每条记录进行单独的数据库查询
- **修改后**：一次性批量获取所有需要比较的记录
- **问题**：仍然存在锁定问题，因为批量查询使用独立连接

### 第二次优化（跳过比较）
- **最终方案**：在 Replace 策略下，直接更新所有现有记录，不进行变化检测
- **原因**：变化检测需要读取现有记录，在事务中读取会导致锁定
- **效果**：完全避免了读取操作，消除了锁定问题

### 性能对比

| 场景 | 修改前 | 第一次优化 | 第二次优化（最终） |
|------|--------|-----------|------------------|
| 导入15条记录 | 7.5分钟 | 7.5分钟 | < 1秒 |
| 数据库查询次数 | 15次 | 1次 | 0次 |
| 锁定问题 | 严重 | 仍存在 | 完全解决 |
| UI 响应 | 卡死 | 卡死 | 流畅 |

## 测试建议

1. 使用测试数据生成器生成包含现有记录的数据
2. 选择 "Replace" 策略导入
3. 观察导入过程是否流畅，无卡顿
4. 检查日志中是否还有 "database table is locked" 错误

## 相关文件

- `Services/ImportExport/Importers/ImporterBase.cs`
- `Services/ImportExport/Importers/PersonnelImporter.cs`
- `Services/ImportExport/Importers/SkillImporter.cs`
- `Services/ImportExport/Importers/PositionImporter.cs`
- `Services/ImportExport/Importers/TemplateImporter.cs`
- `Services/ImportExport/Importers/HolidayConfigImporter.cs`
- `Services/ImportExport/Importers/FixedAssignmentImporter.cs`
- `Services/ImportExport/Importers/ManualAssignmentImporter.cs`

## 第三次优化（UI 线程分离）

### 问题
即使数据库操作很快，但在 UI 线程上执行仍然会导致界面短暂卡顿。

### 解决方案
将导入/导出操作移到后台线程执行：

```csharp
// 在后台线程执行导入操作，避免阻塞 UI
ImportResult result = null;
await Task.Run(async () =>
{
    result = await _importExportService.ImportDataAsync(file.Path, options, progress);
});
```

### 关键点
1. 使用 `Task.Run` 将数据库操作放到后台线程
2. 进度更新通过 `DispatcherQueue.TryEnqueue` 回到 UI 线程
3. UI 保持响应，用户可以看到实时进度

### 修改的文件
- `ViewModels/Settings/SettingsPageViewModel.cs` - 导入和导出方法

## 修复日期

2025-11-22
