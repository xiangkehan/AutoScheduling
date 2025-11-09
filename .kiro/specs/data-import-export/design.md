# 设计文档

## 概述

本设计文档描述了排班系统的数据导入导出功能的技术实现方案。该功能允许用户将所有核心数据（人员、哨位、技能、排班模板、约束规则等）导出为 JSON 格式文件，并支持从备份文件导入数据，提供多种冲突解决策略。

### 设计目标

1. **完整性**：导出所有核心业务数据，确保可以完整恢复系统状态
2. **可靠性**：提供数据验证和错误处理机制，确保数据完整性
3. **灵活性**：支持多种冲突解决策略，适应不同使用场景
4. **用户友好**：提供清晰的进度反馈和错误提示
5. **可扩展性**：设计易于扩展，支持未来新增数据表

## 架构

### 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Layer                              │
│  ┌──────────────────┐         ┌──────────────────┐         │
│  │  Settings Page   │         │  Dialog Service  │         │
│  │  - Export Button │         │  - File Picker   │         │
│  │  - Import Button │         │  - Progress      │         │
│  └──────────────────┘         └──────────────────┘         │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                      Service Layer                           │
│  ┌──────────────────────────────────────────────────────┐  │
│  │           DataImportExportService                     │  │
│  │  - ExportDataAsync()                                  │  │
│  │  - ImportDataAsync()                                  │  │
│  │  - ValidateImportData()                               │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    Repository Layer                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │  Personnel   │  │  Position    │  │  Skill       │     │
│  │  Repository  │  │  Repository  │  │  Repository  │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │  Template    │  │  Constraint  │  │  Holiday     │     │
│  │  Repository  │  │  Repository  │  │  Repository  │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                      Data Layer                              │
│                    SQLite Database                           │
└─────────────────────────────────────────────────────────────┘
```

### 数据流

#### 导出流程

```
User Action → Settings Page → DataImportExportService
    ↓
Read from Repositories (Personnel, Position, Skill, etc.)
    ↓
Serialize to JSON
    ↓
Save to File (with timestamp)
    ↓
Show Success Message
```

#### 导入流程

```
User Action → Settings Page → File Picker
    ↓
DataImportExportService.ImportDataAsync()
    ↓
Read & Parse JSON File
    ↓
Validate Data Structure & Integrity
    ↓
User Selects Conflict Resolution Strategy
    ↓
Create Database Backup (using DatabaseBackupManager)
    ↓
Begin Transaction
    ↓
Import Data (按依赖顺序: Skills → Personnel/Positions → Templates → Constraints)
    ↓
Commit Transaction / Rollback on Error
    ↓
Show Import Summary
```

## 组件和接口

### 1. DataImportExportService

核心服务类，负责协调导入导出操作。

```csharp
public class DataImportExportService : IDataImportExportService
{
    // 依赖注入
    private readonly IPersonalRepository _personnelRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISchedulingTemplateRepository _templateRepository;
    private readonly IConstraintRepository _constraintRepository;
    private readonly DatabaseBackupManager _backupManager;
    private readonly ILogger _logger;

    // 导出方法
    Task<ExportResult> ExportDataAsync(string filePath, IProgress<ExportProgress> progress = null);
    
    // 导入方法
    Task<ImportResult> ImportDataAsync(string filePath, ImportOptions options, IProgress<ImportProgress> progress = null);
    
    // 验证方法
    Task<ValidationResult> ValidateImportDataAsync(string filePath);
}
```

### 2. 数据传输对象 (DTOs)

#### ExportData

导出数据的根对象，包含所有核心数据和元数据。

```csharp
public class ExportData
{
    public ExportMetadata Metadata { get; set; }
    public List<SkillDto> Skills { get; set; }
    public List<PersonnelDto> Personnel { get; set; }
    public List<PositionDto> Positions { get; set; }
    public List<SchedulingTemplateDto> Templates { get; set; }
    public List<FixedAssignmentDto> FixedAssignments { get; set; }
    public List<ManualAssignmentDto> ManualAssignments { get; set; }
    public List<HolidayConfigDto> HolidayConfigs { get; set; }
}
```

#### ExportMetadata

导出文件的元数据信息。

```csharp
public class ExportMetadata
{
    public string ExportVersion { get; set; } = "1.0";
    public DateTime ExportedAt { get; set; }
    public int DatabaseVersion { get; set; }
    public string ApplicationVersion { get; set; }
    public DataStatistics Statistics { get; set; }
}

public class DataStatistics
{
    public int SkillCount { get; set; }
    public int PersonnelCount { get; set; }
    public int PositionCount { get; set; }
    public int TemplateCount { get; set; }
    public int ConstraintCount { get; set; }
}
```

#### ImportOptions

导入操作的配置选项。

```csharp
public class ImportOptions
{
    public ConflictResolutionStrategy Strategy { get; set; } = ConflictResolutionStrategy.Skip;
    public bool CreateBackupBeforeImport { get; set; } = true;
    public bool ValidateReferences { get; set; } = true;
    public bool ContinueOnError { get; set; } = false;
}

public enum ConflictResolutionStrategy
{
    Replace,  // 覆盖现有数据
    Skip,     // 跳过冲突数据
    Merge     // 合并数据（保留现有，添加新数据）
}
```

#### ExportResult / ImportResult

操作结果对象。

```csharp
public class ExportResult
{
    public bool Success { get; set; }
    public string FilePath { get; set; }
    public long FileSize { get; set; }
    public DataStatistics Statistics { get; set; }
    public TimeSpan Duration { get; set; }
    public string ErrorMessage { get; set; }
}

public class ImportResult
{
    public bool Success { get; set; }
    public ImportStatistics Statistics { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Warnings { get; set; }
    public string ErrorMessage { get; set; }
}

public class ImportStatistics
{
    public int TotalRecords { get; set; }
    public int ImportedRecords { get; set; }
    public int SkippedRecords { get; set; }
    public int FailedRecords { get; set; }
    public Dictionary<string, int> RecordsByTable { get; set; }
}
```

#### Progress 对象

用于报告进度的对象。

```csharp
public class ExportProgress
{
    public string CurrentTable { get; set; }
    public int ProcessedRecords { get; set; }
    public int TotalRecords { get; set; }
    public double PercentComplete { get; set; }
}

public class ImportProgress
{
    public string CurrentTable { get; set; }
    public string CurrentOperation { get; set; } // "Validating", "Importing", "Verifying"
    public int ProcessedRecords { get; set; }
    public int TotalRecords { get; set; }
    public double PercentComplete { get; set; }
}
```

### 3. ValidationResult

数据验证结果对象。

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; }
    public List<ValidationWarning> Warnings { get; set; }
    public ExportMetadata Metadata { get; set; }
}

public class ValidationError
{
    public string Table { get; set; }
    public int? RecordId { get; set; }
    public string Field { get; set; }
    public string Message { get; set; }
    public ValidationErrorType Type { get; set; }
}

public enum ValidationErrorType
{
    MissingField,
    InvalidDataType,
    InvalidValue,
    BrokenReference,
    DuplicateKey,
    ConstraintViolation
}

public class ValidationWarning
{
    public string Table { get; set; }
    public string Message { get; set; }
}
```

## 数据模型

### JSON 文件格式

导出的 JSON 文件结构如下：

```json
{
  "metadata": {
    "exportVersion": "1.0",
    "exportedAt": "2024-01-15T10:30:00Z",
    "databaseVersion": 1,
    "applicationVersion": "1.0.0",
    "statistics": {
      "skillCount": 10,
      "personnelCount": 50,
      "positionCount": 20,
      "templateCount": 5,
      "constraintCount": 15
    }
  },
  "skills": [
    {
      "id": 1,
      "name": "技能名称",
      "description": "技能描述",
      "isActive": true,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    }
  ],
  "personnel": [
    {
      "id": 1,
      "name": "人员姓名",
      "skillIds": [1, 2, 3],
      "isAvailable": true,
      "isRetired": false,
      "recentShiftIntervalCount": 0,
      "recentHolidayShiftIntervalCount": 0,
      "recentPeriodShiftIntervals": [0,0,0,0,0,0,0,0,0,0,0,0]
    }
  ],
  "positions": [
    {
      "id": 1,
      "name": "哨位名称",
      "location": "地点",
      "description": "描述",
      "requirements": "要求",
      "requiredSkillIds": [1, 2],
      "availablePersonnelIds": [1, 2, 3]
    }
  ],
  "templates": [...],
  "fixedAssignments": [...],
  "manualAssignments": [...],
  "holidayConfigs": [...]
}
```

### 数据依赖关系

导入时必须按照以下顺序处理数据，以确保外键引用的完整性：

1. **Skills** (无依赖)
2. **Personnel** (依赖 Skills)
3. **Positions** (依赖 Skills 和 Personnel)
4. **HolidayConfigs** (无依赖)
5. **Templates** (依赖 Personnel, Positions, HolidayConfigs)
6. **FixedAssignments** (依赖 Personnel, Positions)
7. **ManualAssignments** (依赖 Personnel, Positions)

## 错误处理

### 导出错误处理

1. **数据库访问错误**
   - 捕获 SqliteException
   - 记录详细错误日志
   - 向用户显示友好错误消息

2. **文件系统错误**
   - 捕获 IOException, UnauthorizedAccessException
   - 检查磁盘空间
   - 检查文件权限
   - 提供备选保存位置

3. **序列化错误**
   - 捕获 JsonException
   - 记录导致错误的数据
   - 提供部分导出选项

### 导入错误处理

1. **文件格式错误**
   - 验证 JSON 格式
   - 检查必需字段
   - 显示详细验证错误报告

2. **数据验证错误**
   - 验证数据类型
   - 验证数据约束
   - 验证外键引用
   - 生成验证报告

3. **导入失败回滚**
   - 使用数据库事务
   - 失败时自动回滚
   - 从自动备份恢复（如果启用）

4. **部分导入失败**
   - 记录失败的记录
   - 继续导入其他记录（如果 ContinueOnError = true）
   - 生成详细的导入报告

## 测试策略

### 单元测试

1. **DataImportExportService 测试**
   - 测试导出功能（正常流程）
   - 测试导入功能（各种冲突策略）
   - 测试数据验证逻辑
   - 测试错误处理

2. **数据验证测试**
   - 测试必需字段验证
   - 测试数据类型验证
   - 测试外键引用验证
   - 测试约束条件验证

3. **冲突解决测试**
   - 测试 Replace 策略
   - 测试 Skip 策略
   - 测试 Merge 策略

### 集成测试

1. **完整导出导入流程测试**
   - 导出完整数据库
   - 清空数据库
   - 导入备份数据
   - 验证数据完整性

2. **大数据量测试**
   - 测试导出 1000+ 记录
   - 测试导入 1000+ 记录
   - 验证性能和内存使用

3. **错误恢复测试**
   - 模拟导入失败
   - 验证事务回滚
   - 验证备份恢复

### UI 测试

1. **用户交互测试**
   - 测试导出按钮功能
   - 测试导入按钮功能
   - 测试文件选择对话框
   - 测试进度显示

2. **错误提示测试**
   - 测试各种错误场景的提示
   - 验证错误消息的清晰度

## 实现细节

### 导出实现

```csharp
public async Task<ExportResult> ExportDataAsync(string filePath, IProgress<ExportProgress> progress = null)
{
    var startTime = DateTime.UtcNow;
    var result = new ExportResult();
    
    try
    {
        // 1. 收集所有数据
        var exportData = new ExportData
        {
            Metadata = new ExportMetadata
            {
                ExportedAt = DateTime.UtcNow,
                DatabaseVersion = await GetDatabaseVersionAsync(),
                ApplicationVersion = GetApplicationVersion()
            }
        };
        
        // 2. 按表导出数据（带进度报告）
        progress?.Report(new ExportProgress { CurrentTable = "Skills", PercentComplete = 0 });
        exportData.Skills = await ExportSkillsAsync();
        
        progress?.Report(new ExportProgress { CurrentTable = "Personnel", PercentComplete = 20 });
        exportData.Personnel = await ExportPersonnelAsync();
        
        progress?.Report(new ExportProgress { CurrentTable = "Positions", PercentComplete = 40 });
        exportData.Positions = await ExportPositionsAsync();
        
        progress?.Report(new ExportProgress { CurrentTable = "Templates", PercentComplete = 60 });
        exportData.Templates = await ExportTemplatesAsync();
        
        progress?.Report(new ExportProgress { CurrentTable = "Constraints", PercentComplete = 80 });
        exportData.FixedAssignments = await ExportFixedAssignmentsAsync();
        exportData.ManualAssignments = await ExportManualAssignmentsAsync();
        exportData.HolidayConfigs = await ExportHolidayConfigsAsync();
        
        // 3. 更新统计信息
        exportData.Metadata.Statistics = CalculateStatistics(exportData);
        
        // 4. 序列化为 JSON
        progress?.Report(new ExportProgress { CurrentTable = "Serializing", PercentComplete = 90 });
        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        // 5. 写入文件
        await File.WriteAllTextAsync(filePath, json);
        
        // 6. 返回结果
        progress?.Report(new ExportProgress { CurrentTable = "Complete", PercentComplete = 100 });
        result.Success = true;
        result.FilePath = filePath;
        result.FileSize = new FileInfo(filePath).Length;
        result.Statistics = exportData.Metadata.Statistics;
        result.Duration = DateTime.UtcNow - startTime;
    }
    catch (Exception ex)
    {
        _logger.LogError($"Export failed: {ex.Message}");
        result.Success = false;
        result.ErrorMessage = ex.Message;
    }
    
    return result;
}
```

### 导入实现

```csharp
public async Task<ImportResult> ImportDataAsync(string filePath, ImportOptions options, IProgress<ImportProgress> progress = null)
{
    var startTime = DateTime.UtcNow;
    var result = new ImportResult { Statistics = new ImportStatistics() };
    
    try
    {
        // 1. 读取并解析 JSON 文件
        progress?.Report(new ImportProgress { CurrentOperation = "Reading file", PercentComplete = 0 });
        var json = await File.ReadAllTextAsync(filePath);
        var exportData = JsonSerializer.Deserialize<ExportData>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        // 2. 验证数据
        progress?.Report(new ImportProgress { CurrentOperation = "Validating", PercentComplete = 10 });
        var validation = await ValidateImportDataAsync(exportData);
        if (!validation.IsValid)
        {
            result.Success = false;
            result.ErrorMessage = "Data validation failed";
            result.Warnings = validation.Errors.Select(e => e.Message).ToList();
            return result;
        }
        
        // 3. 创建备份
        if (options.CreateBackupBeforeImport)
        {
            progress?.Report(new ImportProgress { CurrentOperation = "Creating backup", PercentComplete = 20 });
            await _backupManager.CreateBackupAsync();
        }
        
        // 4. 开始事务导入
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        
        try
        {
            // 按依赖顺序导入
            progress?.Report(new ImportProgress { CurrentTable = "Skills", PercentComplete = 30 });
            await ImportSkillsAsync(exportData.Skills, options, connection, transaction);
            
            progress?.Report(new ImportProgress { CurrentTable = "Personnel", PercentComplete = 45 });
            await ImportPersonnelAsync(exportData.Personnel, options, connection, transaction);
            
            progress?.Report(new ImportProgress { CurrentTable = "Positions", PercentComplete = 60 });
            await ImportPositionsAsync(exportData.Positions, options, connection, transaction);
            
            progress?.Report(new ImportProgress { CurrentTable = "Templates", PercentComplete = 75 });
            await ImportTemplatesAsync(exportData.Templates, options, connection, transaction);
            
            progress?.Report(new ImportProgress { CurrentTable = "Constraints", PercentComplete = 90 });
            await ImportConstraintsAsync(exportData, options, connection, transaction);
            
            // 提交事务
            await transaction.CommitAsync();
            
            progress?.Report(new ImportProgress { CurrentOperation = "Complete", PercentComplete = 100 });
            result.Success = true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError($"Import failed: {ex.Message}");
        result.Success = false;
        result.ErrorMessage = ex.Message;
    }
    
    result.Duration = DateTime.UtcNow - startTime;
    return result;
}
```

### 冲突解决实现

```csharp
private async Task ImportSkillsAsync(List<SkillDto> skills, ImportOptions options, SqliteConnection connection, SqliteTransaction transaction)
{
    foreach (var skill in skills)
    {
        var exists = await _skillRepository.ExistsAsync(skill.Id);
        
        switch (options.Strategy)
        {
            case ConflictResolutionStrategy.Replace:
                if (exists)
                {
                    await _skillRepository.UpdateAsync(MapToModel(skill));
                }
                else
                {
                    await _skillRepository.CreateAsync(MapToModel(skill));
                }
                break;
                
            case ConflictResolutionStrategy.Skip:
                if (!exists)
                {
                    await _skillRepository.CreateAsync(MapToModel(skill));
                }
                break;
                
            case ConflictResolutionStrategy.Merge:
                if (!exists)
                {
                    await _skillRepository.CreateAsync(MapToModel(skill));
                }
                // 如果存在，保留现有数据
                break;
        }
    }
}
```

## 性能考虑

1. **批量操作**：使用批量插入减少数据库往返次数
2. **流式处理**：对于大文件，考虑使用流式 JSON 解析
3. **异步操作**：所有 I/O 操作使用异步方法
4. **进度报告**：定期报告进度，避免 UI 冻结
5. **内存管理**：及时释放大对象，避免内存溢出

## 安全考虑

1. **文件路径验证**：验证用户选择的文件路径，防止路径遍历攻击
2. **数据验证**：严格验证导入数据，防止注入攻击
3. **备份保护**：导入前自动创建备份，确保数据安全
4. **事务保护**：使用数据库事务，确保原子性
5. **权限检查**：检查文件读写权限，提供清晰的错误提示
