# 设计文档

## 概述

本设计文档描述了如何修复 `DataImportExportService` 中 Replace 策略的架构缺陷。当前实现存在事务保护缺失、N+1 查询问题、主键不一致和性能问题。本设计通过引入事务管理、批量操作、高效的数据比较和并发保护来解决这些问题。

### 设计目标

1. **原子性**：所有导入操作在单个事务中执行，确保全有或全无
2. **性能**：使用批量查询和操作，支持 1000+ 记录的高效导入
3. **一致性**：使用 UPDATE 而不是 DELETE+INSERT 保持主键一致性
4. **可靠性**：提供详细的错误处理和回滚机制
5. **可观测性**：记录性能指标和操作审计日志

## 架构

### 当前架构问题

```
当前流程（有问题）:
┌─────────────────────────────────────────────────────────────┐
│  ImportDataAsync()                                           │
│  ├─ 读取文件                                                 │
│  ├─ 验证数据                                                 │
│  ├─ 创建备份                                                 │
│  └─ 按顺序导入各表                                           │
│     ├─ ImportSkillsAsync()                                   │
│     │  └─ foreach skill:                                     │
│     │     ├─ ExistsAsync(id)  ← N+1 查询问题                │
│     │     ├─ DeleteAsync(id)  ← 主键不一致风险              │
│     │     └─ CreateAsync()    ← 无事务保护                  │
│     ├─ ImportPersonnelAsync()                                │
│     ├─ ImportPositionsAsync()                                │
│     └─ ...                                                   │
└─────────────────────────────────────────────────────────────┘
```

### 改进后的架构

```
改进流程（修复后）:
┌─────────────────────────────────────────────────────────────┐
│  ImportDataAsync()                                           │
│  ├─ 获取导入锁 🔒                                           │
│  ├─ 读取文件                                                 │
│  ├─ 验证数据（事务前）                                       │
│  ├─ 创建备份                                                 │
│  └─ 开始事务 ⚡                                             │
│     ├─ ImportSkillsWithTransactionAsync()                    │
│     │  ├─ 批量查询现有 IDs（单次查询）                      │
│     │  ├─ 分类：新增 vs 更新                                │
│     │  ├─ 批量 INSERT（新记录）                             │
│     │  └─ 批量 UPDATE（现有记录，仅更新变化字段）           │
│     ├─ ImportPersonnelWithTransactionAsync()                 │
│     ├─ ImportPositionsWithTransactionAsync()                 │
│     └─ ...                                                   │
│     ├─ 提交事务 ✓                                           │
│     └─ 失败时回滚 ✗                                         │
│  └─ 释放导入锁 🔓                                           │
└─────────────────────────────────────────────────────────────┘
```


## 组件和接口

### 1. ImportLockManager

管理导入操作的并发锁，防止多个导入同时执行。

```csharp
public class ImportLockManager
{
    private static readonly SemaphoreSlim _importLock = new SemaphoreSlim(1, 1);
    private static readonly TimeSpan _lockTimeout = TimeSpan.FromMinutes(30);
    
    public async Task<bool> TryAcquireLockAsync(CancellationToken cancellationToken = default);
    public void ReleaseLock();
    public bool IsLocked { get; }
}
```

### 2. BatchExistenceChecker

批量检查记录是否存在，避免 N+1 查询问题。

```csharp
public class BatchExistenceChecker
{
    // 批量查询现有记录的 ID
    public async Task<HashSet<int>> GetExistingIdsAsync<T>(
        IEnumerable<int> idsToCheck, 
        SqliteConnection connection, 
        SqliteTransaction transaction,
        string tableName);
    
    // 分类记录为新增和更新
    public (List<T> toInsert, List<T> toUpdate) ClassifyRecords<T>(
        List<T> records, 
        HashSet<int> existingIds,
        Func<T, int> idSelector);
}
```

### 3. DataComparer

比较导入数据与现有数据，跳过不必要的更新。

```csharp
public class DataComparer
{
    // 比较技能数据
    public bool AreSkillsEqual(SkillDto imported, Skill existing);
    
    // 比较人员数据
    public bool ArePersonnelEqual(PersonnelDto imported, Personal existing);
    
    // 比较哨位数据
    public bool ArePositionsEqual(PositionDto imported, PositionLocation existing);
    
    // 通用比较方法
    public bool AreEqual<TDto, TModel>(TDto dto, TModel model, 
        Func<TDto, TModel, bool> comparer);
}
```

### 4. BatchImporter

执行批量插入和更新操作。

```csharp
public class BatchImporter
{
    private const int DefaultBatchSize = 100;
    
    // 批量插入记录
    public async Task<int> BatchInsertAsync<T>(
        List<T> records,
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        Func<T, Dictionary<string, object>> fieldMapper,
        int batchSize = DefaultBatchSize);
    
    // 批量更新记录
    public async Task<int> BatchUpdateAsync<T>(
        List<T> records,
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        Func<T, Dictionary<string, object>> fieldMapper,
        Func<T, int> idSelector,
        int batchSize = DefaultBatchSize);
}
```

### 5. PerformanceMonitor

监控导入操作的性能指标。

```csharp
public class PerformanceMonitor
{
    public Stopwatch TotalTimer { get; }
    public Dictionary<string, TimeSpan> OperationTimings { get; }
    
    public void StartOperation(string operationName);
    public void EndOperation(string operationName);
    public PerformanceReport GenerateReport(int totalRecords);
}

public class PerformanceReport
{
    public TimeSpan TotalDuration { get; set; }
    public Dictionary<string, TimeSpan> OperationBreakdown { get; set; }
    public double RecordsPerSecond { get; set; }
    public string Summary { get; set; }
}
```


## 数据模型

### ImportContext

导入操作的上下文对象，在整个导入过程中传递。

```csharp
public class ImportContext
{
    public SqliteConnection Connection { get; set; }
    public SqliteTransaction Transaction { get; set; }
    public ImportOptions Options { get; set; }
    public PerformanceMonitor PerformanceMonitor { get; set; }
    public ImportStatistics Statistics { get; set; }
    public List<string> Warnings { get; set; }
    public CancellationToken CancellationToken { get; set; }
}
```

### ImportStatistics（扩展）

扩展现有的 ImportStatistics 以包含更多详细信息。

```csharp
public class ImportStatistics
{
    // 现有字段
    public int TotalRecords { get; set; }
    public int ImportedRecords { get; set; }
    public int SkippedRecords { get; set; }
    public int FailedRecords { get; set; }
    public Dictionary<string, int> RecordsByTable { get; set; }
    
    // 新增字段
    public int InsertedRecords { get; set; }
    public int UpdatedRecords { get; set; }
    public int UnchangedRecords { get; set; }
    public Dictionary<string, TableImportStats> DetailsByTable { get; set; }
}

public class TableImportStats
{
    public int Total { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public int Skipped { get; set; }
    public TimeSpan Duration { get; set; }
}
```

## 核心算法

### 批量存在性检查算法

```csharp
// 伪代码
async Task<HashSet<int>> GetExistingIdsAsync(List<int> idsToCheck, connection, transaction, tableName)
{
    if (idsToCheck.Count == 0)
        return new HashSet<int>();
    
    // 构建 IN 查询
    var placeholders = string.Join(",", idsToCheck.Select((_, i) => $"@id{i}"));
    var query = $"SELECT Id FROM {tableName} WHERE Id IN ({placeholders})";
    
    using var command = new SqliteCommand(query, connection, transaction);
    for (int i = 0; i < idsToCheck.Count; i++)
    {
        command.Parameters.AddWithValue($"@id{i}", idsToCheck[i]);
    }
    
    var existingIds = new HashSet<int>();
    using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        existingIds.Add(reader.GetInt32(0));
    }
    
    return existingIds;
}
```

### 批量插入算法

```csharp
// 伪代码
async Task<int> BatchInsertAsync(List<T> records, connection, transaction, tableName, fieldMapper, batchSize)
{
    int totalInserted = 0;
    
    for (int i = 0; i < records.Count; i += batchSize)
    {
        var batch = records.Skip(i).Take(batchSize).ToList();
        
        // 构建批量 INSERT 语句
        var fields = fieldMapper(batch[0]).Keys;
        var fieldList = string.Join(", ", fields);
        
        var valuesClauses = new List<string>();
        var command = new SqliteCommand { Connection = connection, Transaction = transaction };
        
        for (int j = 0; j < batch.Count; j++)
        {
            var record = batch[j];
            var fieldValues = fieldMapper(record);
            
            var placeholders = new List<string>();
            foreach (var kvp in fieldValues)
            {
                var paramName = $"@{kvp.Key}_{i}_{j}";
                placeholders.Add(paramName);
                command.Parameters.AddWithValue(paramName, kvp.Value ?? DBNull.Value);
            }
            
            valuesClauses.Add($"({string.Join(", ", placeholders)})");
        }
        
        command.CommandText = $"INSERT INTO {tableName} ({fieldList}) VALUES {string.Join(", ", valuesClauses)}";
        
        totalInserted += await command.ExecuteNonQueryAsync();
    }
    
    return totalInserted;
}
```

### 批量更新算法

```csharp
// 伪代码
async Task<int> BatchUpdateAsync(List<T> records, connection, transaction, tableName, fieldMapper, idSelector, batchSize)
{
    int totalUpdated = 0;
    
    for (int i = 0; i < records.Count; i += batchSize)
    {
        var batch = records.Skip(i).Take(batchSize).ToList();
        
        foreach (var record in batch)
        {
            var fieldValues = fieldMapper(record);
            var id = idSelector(record);
            
            var setClauses = new List<string>();
            var command = new SqliteCommand { Connection = connection, Transaction = transaction };
            
            foreach (var kvp in fieldValues)
            {
                if (kvp.Key != "Id") // 不更新主键
                {
                    setClauses.Add($"{kvp.Key} = @{kvp.Key}");
                    command.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                }
            }
            
            command.Parameters.AddWithValue("@Id", id);
            command.CommandText = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE Id = @Id";
            
            totalUpdated += await command.ExecuteNonQueryAsync();
        }
    }
    
    return totalUpdated;
}
```


## 错误处理

### 事务回滚策略

1. **验证失败**：在事务开始前失败，无需回滚
2. **导入过程失败**：回滚整个事务，数据库恢复到导入前状态
3. **部分批次失败**：根据 `ContinueOnError` 选项决定是否继续
4. **回滚失败**：记录严重错误，建议从备份恢复

### 错误分类和处理

```csharp
public enum ImportErrorSeverity
{
    Warning,    // 可继续，记录警告
    Error,      // 当前记录失败，可能继续
    Critical    // 必须停止，回滚事务
}

public class ImportError
{
    public ImportErrorSeverity Severity { get; set; }
    public string Table { get; set; }
    public int? RecordId { get; set; }
    public string Message { get; set; }
    public Exception Exception { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### 错误恢复流程

```
导入失败处理流程:
┌─────────────────────────────────────────────────────────────┐
│  1. 捕获异常                                                 │
│  2. 记录详细错误信息（表、记录ID、错误消息）                 │
│  3. 回滚事务                                                 │
│  4. 释放导入锁                                               │
│  5. 记录审计日志                                             │
│  6. 生成用户友好的错误消息                                   │
│  7. 提供恢复建议                                             │
│     - 检查数据格式                                           │
│     - 验证外键引用                                           │
│     - 从备份恢复（如果需要）                                 │
└─────────────────────────────────────────────────────────────┘
```

## 测试策略

### 单元测试

1. **ImportLockManager 测试**
   - 测试锁获取和释放
   - 测试并发锁冲突
   - 测试锁超时

2. **BatchExistenceChecker 测试**
   - 测试批量 ID 查询
   - 测试记录分类（新增 vs 更新）
   - 测试空列表处理

3. **DataComparer 测试**
   - 测试各实体类型的比较
   - 测试相等和不相等情况
   - 测试 null 值处理

4. **BatchImporter 测试**
   - 测试批量插入
   - 测试批量更新
   - 测试批次大小处理

### 集成测试

1. **事务完整性测试**
   - 测试成功提交
   - 测试失败回滚
   - 测试部分失败处理

2. **性能测试**
   - 测试 100 条记录导入
   - 测试 1000 条记录导入
   - 测试 10000 条记录导入
   - 验证性能指标

3. **并发测试**
   - 测试同时发起多个导入
   - 验证锁机制正常工作
   - 测试锁超时场景

4. **数据一致性测试**
   - 导出数据
   - 使用 Replace 策略导入
   - 验证主键保持不变
   - 验证数据完全一致


## 实现细节

### 改进后的 ImportDataAsync 方法

```csharp
public async Task<ImportResult> ImportDataAsync(
    string filePath, 
    ImportOptions options, 
    IProgress<ImportProgress>? progress = null)
{
    var startTime = DateTime.UtcNow;
    var result = new ImportResult 
    { 
        Statistics = new ImportStatistics 
        { 
            RecordsByTable = new Dictionary<string, int>(),
            DetailsByTable = new Dictionary<string, TableImportStats>()
        },
        Warnings = new List<string>()
    };
    
    ImportLockManager? lockManager = null;
    
    try
    {
        // 1. 获取导入锁
        lockManager = new ImportLockManager();
        if (!await lockManager.TryAcquireLockAsync())
        {
            throw new InvalidOperationException("另一个导入操作正在进行中，请稍后再试");
        }
        
        // 2. 读取并解析文件
        progress?.Report(new ImportProgress { CurrentOperation = "Reading file", PercentComplete = 0 });
        var json = await File.ReadAllTextAsync(filePath);
        var exportData = JsonSerializer.Deserialize<ExportData>(json, _jsonOptions);
        
        // 3. 验证数据（事务前）
        progress?.Report(new ImportProgress { CurrentOperation = "Validating", PercentComplete = 10 });
        var validation = await ValidateImportDataAsync(filePath);
        if (!validation.IsValid)
        {
            result.Success = false;
            result.ErrorMessage = "数据验证失败";
            result.Warnings = validation.Errors.Select(e => e.Message).ToList();
            return result;
        }
        
        // 4. 创建备份
        if (options.CreateBackupBeforeImport)
        {
            progress?.Report(new ImportProgress { CurrentOperation = "Creating backup", PercentComplete = 20 });
            await _backupManager.CreateBackupAsync();
        }
        
        // 5. 创建导入上下文
        var performanceMonitor = new PerformanceMonitor();
        performanceMonitor.StartOperation("Total");
        
        // 6. 开始事务导入
        var connectionString = GetConnectionString();
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        
        using var transaction = connection.BeginTransaction();
        
        var context = new ImportContext
        {
            Connection = connection,
            Transaction = transaction,
            Options = options,
            PerformanceMonitor = performanceMonitor,
            Statistics = result.Statistics,
            Warnings = result.Warnings
        };
        
        try
        {
            // 按依赖顺序导入各表
            await ImportSkillsWithTransactionAsync(exportData.Skills, context, progress);
            await ImportPersonnelWithTransactionAsync(exportData.Personnel, context, progress);
            await ImportPositionsWithTransactionAsync(exportData.Positions, context, progress);
            await ImportHolidayConfigsWithTransactionAsync(exportData.HolidayConfigs, context, progress);
            await ImportTemplatesWithTransactionAsync(exportData.Templates, context, progress);
            await ImportFixedAssignmentsWithTransactionAsync(exportData.FixedAssignments, context, progress);
            await ImportManualAssignmentsWithTransactionAsync(exportData.ManualAssignments, context, progress);
            
            // 提交事务
            await transaction.CommitAsync();
            
            performanceMonitor.EndOperation("Total");
            
            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;
            
            // 生成性能报告
            var perfReport = performanceMonitor.GenerateReport(result.Statistics.TotalRecords);
            _logger.Log($"Import performance: {perfReport.Summary}");
        }
        catch (Exception ex)
        {
            // 回滚事务
            await transaction.RollbackAsync();
            throw;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError($"Import failed: {ex.Message}");
        result.Success = false;
        result.ErrorMessage = ErrorMessageTranslator.TranslateException(ex, "数据导入");
        result.Duration = DateTime.UtcNow - startTime;
    }
    finally
    {
        // 释放导入锁
        lockManager?.ReleaseLock();
    }
    
    return result;
}
```

### 改进后的 ImportSkillsWithTransactionAsync 方法

```csharp
private async Task ImportSkillsWithTransactionAsync(
    List<SkillDto> skills, 
    ImportContext context, 
    IProgress<ImportProgress>? progress)
{
    if (skills == null || skills.Count == 0)
        return;
    
    context.PerformanceMonitor.StartOperation("Skills");
    
    var tableName = "Skills";
    var tableStats = new TableImportStats { Total = skills.Count };
    
    try
    {
        // 1. 批量查询现有 IDs
        var idsToCheck = skills.Select(s => s.Id).ToList();
        var existenceChecker = new BatchExistenceChecker();
        var existingIds = await existenceChecker.GetExistingIdsAsync(
            idsToCheck, 
            context.Connection, 
            context.Transaction, 
            tableName);
        
        // 2. 分类记录
        var (toInsert, toUpdate) = existenceChecker.ClassifyRecords(
            skills, 
            existingIds, 
            s => s.Id);
        
        // 3. 根据策略处理
        switch (context.Options.Strategy)
        {
            case ConflictResolutionStrategy.Replace:
                // 批量插入新记录
                if (toInsert.Count > 0)
                {
                    var batchImporter = new BatchImporter();
                    var inserted = await batchImporter.BatchInsertAsync(
                        toInsert,
                        context.Connection,
                        context.Transaction,
                        tableName,
                        MapSkillToFields);
                    tableStats.Inserted = inserted;
                }
                
                // 批量更新现有记录（仅更新变化的）
                if (toUpdate.Count > 0)
                {
                    var dataComparer = new DataComparer();
                    var recordsToUpdate = new List<SkillDto>();
                    
                    // 获取现有数据进行比较
                    foreach (var skill in toUpdate)
                    {
                        var existing = await _skillRepository.GetByIdAsync(skill.Id);
                        if (existing != null && !dataComparer.AreSkillsEqual(skill, existing))
                        {
                            recordsToUpdate.Add(skill);
                        }
                        else
                        {
                            tableStats.Unchanged++;
                        }
                    }
                    
                    if (recordsToUpdate.Count > 0)
                    {
                        var batchImporter = new BatchImporter();
                        var updated = await batchImporter.BatchUpdateAsync(
                            recordsToUpdate,
                            context.Connection,
                            context.Transaction,
                            tableName,
                            MapSkillToFields,
                            s => s.Id);
                        tableStats.Updated = updated;
                    }
                }
                break;
                
            case ConflictResolutionStrategy.Skip:
                // 仅插入新记录
                if (toInsert.Count > 0)
                {
                    var batchImporter = new BatchImporter();
                    var inserted = await batchImporter.BatchInsertAsync(
                        toInsert,
                        context.Connection,
                        context.Transaction,
                        tableName,
                        MapSkillToFields);
                    tableStats.Inserted = inserted;
                }
                tableStats.Skipped = toUpdate.Count;
                break;
                
            case ConflictResolutionStrategy.Merge:
                // 与 Replace 相同，但保留现有数据的其他字段
                // 当前实现中 Merge 和 Replace 行为相同
                goto case ConflictResolutionStrategy.Replace;
        }
        
        context.Statistics.DetailsByTable[tableName] = tableStats;
        context.Statistics.RecordsByTable[tableName] = tableStats.Inserted + tableStats.Updated;
        
        progress?.Report(new ImportProgress 
        { 
            CurrentTable = tableName,
            ProcessedRecords = skills.Count,
            TotalRecords = skills.Count,
            PercentComplete = 30
        });
    }
    finally
    {
        context.PerformanceMonitor.EndOperation("Skills");
        tableStats.Duration = context.PerformanceMonitor.OperationTimings["Skills"];
    }
}

// 字段映射方法
private Dictionary<string, object> MapSkillToFields(SkillDto skill)
{
    return new Dictionary<string, object>
    {
        ["Id"] = skill.Id,
        ["Name"] = skill.Name,
        ["Description"] = skill.Description ?? string.Empty,
        ["IsActive"] = skill.IsActive ? 1 : 0,
        ["CreatedAt"] = skill.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        ["UpdatedAt"] = skill.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
    };
}
```

## 性能优化

### 批量操作优化

1. **批次大小**：默认 100 条记录/批次，可配置
2. **参数化查询**：使用参数化查询防止 SQL 注入
3. **索引利用**：确保主键索引用于 WHERE 子句
4. **连接复用**：在整个事务中复用同一连接

### 内存优化

1. **流式处理**：对于超大文件，考虑流式 JSON 解析
2. **及时释放**：处理完批次后及时释放内存
3. **避免重复加载**：缓存存在性检查结果

### 查询优化

1. **IN 查询**：使用 IN 子句批量查询 IDs
2. **索引扫描**：确保查询使用索引而不是全表扫描
3. **最小化往返**：减少数据库往返次数

## 安全考虑

1. **SQL 注入防护**：所有查询使用参数化
2. **事务隔离**：使用适当的事务隔离级别
3. **锁超时**：防止死锁，设置合理的超时时间
4. **备份验证**：导入前验证备份创建成功
5. **审计日志**：记录所有导入操作的详细信息

## 向后兼容性

1. **接口保持不变**：`ImportDataAsync` 方法签名不变
2. **选项扩展**：`ImportOptions` 添加新字段，保持向后兼容
3. **渐进式迁移**：可以逐步迁移各表的导入逻辑
4. **回退机制**：保留旧实现作为备用方案
