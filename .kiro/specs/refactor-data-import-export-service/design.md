# Design Document

## Overview

本设计文档描述了如何将庞大的 `DataImportExportService` 类（约3000行代码）重构为多个职责明确、易于维护的服务类。重构遵循单一职责原则（SRP）和依赖倒置原则（DIP），通过创建专门的服务来处理验证、导出、映射等不同职责，同时保持向后兼容性。

## Architecture

### 当前架构分析

`DataImportExportService` 当前承担以下职责：

1. **导入协调** - 协调整个导入流程（约500行）
2. **导出协调** - 协调整个导出流程（约300行）
3. **数据验证** - 验证导入数据的完整性和正确性（约600行）
4. **数据导出** - 从仓储读取并转换数据为DTO（约400行）
5. **数据映射** - DTO与模型之间的转换（约200行）
6. **辅助方法** - 统计计算、版本获取等（约100行）
7. **错误处理** - 异常处理和日志记录（分散在各处）

### 目标架构

```
DataImportExportService (Orchestrator)
├── IDataValidationService (验证服务)
├── IDataExportService (导出服务)
├── IDataMappingService (映射服务)
├── Existing: OperationAuditLogger
├── Existing: DatabaseBackupManager
└── Existing: Various Repositories
```

### 架构原则

1. **单一职责原则** - 每个服务只负责一个明确的职责
2. **依赖倒置原则** - 依赖接口而非具体实现
3. **开闭原则** - 对扩展开放，对修改关闭
4. **向后兼容** - 保持现有公共API不变


## Components and Interfaces

### 1. IDataValidationService

负责所有数据验证逻辑。

```csharp
public interface IDataValidationService
{
    /// <summary>
    /// 验证导入数据的完整性和正确性
    /// </summary>
    Task<ValidationResult> ValidateImportDataAsync(string filePath);
    
    /// <summary>
    /// 验证必需字段存在性和数据类型
    /// </summary>
    void ValidateRequiredFields(ExportData exportData, ValidationResult result);
    
    /// <summary>
    /// 验证数据约束（字符串长度、数值范围、唯一性）
    /// </summary>
    void ValidateDataConstraints(ExportData exportData, ValidationResult result);
    
    /// <summary>
    /// 验证外键引用完整性
    /// </summary>
    Task ValidateForeignKeyReferences(ExportData exportData, ValidationResult result);
}
```

**职责：**
- 文件存在性和格式验证
- 必需字段验证
- 主键重复检查
- 数据约束验证（长度、范围等）
- 重复名称检测（警告）
- 外键引用完整性验证

**依赖：**
- ILogger（日志记录）
- OperationAuditLogger（审计日志）

### 2. IDataExportService

负责所有数据导出逻辑。

```csharp
public interface IDataExportService
{
    /// <summary>
    /// 导出技能数据
    /// </summary>
    Task<List<SkillDto>> ExportSkillsAsync();
    
    /// <summary>
    /// 导出人员数据
    /// </summary>
    Task<List<PersonnelDto>> ExportPersonnelAsync();
    
    /// <summary>
    /// 导出哨位数据
    /// </summary>
    Task<List<PositionDto>> ExportPositionsAsync();
    
    /// <summary>
    /// 导出排班模板数据
    /// </summary>
    Task<List<SchedulingTemplateDto>> ExportTemplatesAsync();
    
    /// <summary>
    /// 导出固定分配约束数据
    /// </summary>
    Task<List<FixedAssignmentDto>> ExportFixedAssignmentsAsync();
    
    /// <summary>
    /// 导出手动分配约束数据
    /// </summary>
    Task<List<ManualAssignmentDto>> ExportManualAssignmentsAsync();
    
    /// <summary>
    /// 导出节假日配置数据
    /// </summary>
    Task<List<HolidayConfigDto>> ExportHolidayConfigsAsync();
    
    /// <summary>
    /// 计算数据统计信息
    /// </summary>
    DataStatistics CalculateStatistics(ExportData exportData);
}
```

**职责：**
- 从各个仓储读取数据
- 将模型转换为DTO（使用映射服务）
- 计算导出统计信息
- 处理导出过程中的错误

**依赖：**
- IPersonalRepository
- IPositionRepository
- ISkillRepository
- ITemplateRepository
- IConstraintRepository
- IDataMappingService（映射服务）
- ILogger（日志记录）


### 3. IDataMappingService

负责DTO与模型之间的双向转换。

```csharp
public interface IDataMappingService
{
    // DTO to Model
    Models.Skill MapToSkill(SkillDto dto);
    Models.Personal MapToPersonnel(PersonnelDto dto);
    Models.PositionLocation MapToPosition(PositionDto dto);
    Models.Constraints.HolidayConfig MapToHolidayConfig(HolidayConfigDto dto);
    Models.SchedulingTemplate MapToTemplate(SchedulingTemplateDto dto);
    Models.Constraints.FixedPositionRule MapToFixedPositionRule(FixedAssignmentDto dto);
    Models.Constraints.ManualAssignment MapToManualAssignment(ManualAssignmentDto dto);
    
    // Model to DTO
    SkillDto MapToSkillDto(Models.Skill model);
    PersonnelDto MapToPersonnelDto(Models.Personal model);
    PositionDto MapToPositionDto(Models.PositionLocation model);
    HolidayConfigDto MapToHolidayConfigDto(Models.Constraints.HolidayConfig model);
    SchedulingTemplateDto MapToTemplateDto(Models.SchedulingTemplate model);
    FixedAssignmentDto MapToFixedAssignmentDto(Models.Constraints.FixedPositionRule model);
    ManualAssignmentDto MapToManualAssignmentDto(Models.Constraints.ManualAssignment model);
}
```

**职责：**
- DTO到模型的转换
- 模型到DTO的转换
- 处理复杂字段的映射（数组、日期等）
- 提供统一的映射逻辑

**依赖：**
- 无（纯映射逻辑）

**实现考虑：**
- 可以考虑使用 AutoMapper 来简化映射代码
- 如果不使用 AutoMapper，保持当前的手动映射方式

### 4. DataImportExportService (Refactored as Orchestrator)

重构后的主服务，作为协调器。

```csharp
public class DataImportExportService : IDataImportExportService
{
    private readonly IDataValidationService _validationService;
    private readonly IDataExportService _exportService;
    private readonly IDataMappingService _mappingService;
    private readonly DatabaseBackupManager _backupManager;
    private readonly ILogger _logger;
    private readonly OperationAuditLogger _auditLogger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // 保持现有的导入相关依赖
    private readonly IPersonalRepository _personnelRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IConstraintRepository _constraintRepository;
    
    public async Task<ExportResult> ExportDataAsync(string filePath, IProgress<ExportProgress>? progress = null)
    {
        // 协调导出流程
        // 1. 验证文件路径
        // 2. 调用 _exportService 导出各表数据
        // 3. 序列化为JSON
        // 4. 写入文件
        // 5. 返回结果
    }
    
    public async Task<ImportResult> ImportDataAsync(string filePath, ImportOptions options, IProgress<ImportProgress>? progress = null)
    {
        // 协调导入流程
        // 1. 获取导入锁
        // 2. 调用 _validationService 验证数据
        // 3. 创建备份
        // 4. 开始事务
        // 5. 调用各个 Importer 导入数据
        // 6. 提交事务
        // 7. 验证完整性
        // 8. 返回结果
    }
    
    public async Task<ValidationResult> ValidateImportDataAsync(string filePath)
    {
        // 委托给验证服务
        return await _validationService.ValidateImportDataAsync(filePath);
    }
}
```

**职责：**
- 协调导入导出流程
- 管理事务和锁
- 处理进度报告
- 错误处理和日志记录
- 审计日志记录

**依赖：**
- IDataValidationService
- IDataExportService
- IDataMappingService
- 所有仓储接口（用于导入）
- DatabaseBackupManager
- ILogger
- OperationAuditLogger


## Data Models

### ExportData

保持现有的 `ExportData` 结构不变，包含：
- Metadata（元数据）
- Skills（技能列表）
- Personnel（人员列表）
- Positions（哨位列表）
- Templates（模板列表）
- FixedAssignments（固定分配列表）
- ManualAssignments（手动分配列表）
- HolidayConfigs（节假日配置列表）

### ValidationResult

保持现有的 `ValidationResult` 结构不变，包含：
- IsValid（是否有效）
- Errors（错误列表）
- Warnings（警告列表）
- Metadata（元数据）

### ImportResult / ExportResult

保持现有的结果结构不变。

## Error Handling

### 错误处理策略

1. **验证服务** - 捕获验证异常，转换为用户友好的错误消息
2. **导出服务** - 捕获导出异常，记录详细日志，抛出给协调器
3. **映射服务** - 不捕获异常，让调用者处理
4. **协调器** - 统一处理所有异常，记录审计日志，返回结果

### 日志记录

- 每个服务使用注入的 `ILogger` 记录操作日志
- 协调器使用 `OperationAuditLogger` 记录审计日志
- 保持现有的日志格式和详细程度

## Testing Strategy

### 单元测试

1. **DataValidationService**
   - 测试各种验证场景（必需字段、约束、外键等）
   - 使用模拟数据，不依赖数据库

2. **DataExportService**
   - 使用模拟仓储测试导出逻辑
   - 验证DTO转换的正确性

3. **DataMappingService**
   - 测试所有映射方法
   - 验证双向映射的一致性

4. **DataImportExportService (Orchestrator)**
   - 使用模拟服务测试协调逻辑
   - 验证错误处理和事务管理

### 集成测试

- 测试完整的导入导出流程
- 使用测试数据库验证数据完整性
- 验证向后兼容性

## File Organization

### 目录结构

```
Services/
├── DataImportExportService.cs (重构后的协调器)
├── ImportExport/
│   ├── DataValidationService.cs (新增)
│   ├── DataExportService.cs (新增)
│   ├── DataMappingService.cs (新增)
│   ├── OperationAuditLogger.cs (现有)
│   ├── Importers/ (现有目录)
│   │   ├── ImporterBase.cs
│   │   ├── SkillImporter.cs
│   │   ├── PersonnelImporter.cs
│   │   ├── PositionImporter.cs
│   │   ├── TemplateImporter.cs
│   │   ├── HolidayConfigImporter.cs
│   │   ├── FixedAssignmentImporter.cs
│   │   └── ManualAssignmentImporter.cs
│   ├── Comparison/ (现有目录)
│   │   ├── DataComparerBase.cs
│   │   ├── CoreEntityComparer.cs
│   │   └── ConstraintComparer.cs
│   ├── Monitoring/ (现有目录)
│   │   ├── PerformanceMonitor.cs
│   │   └── PerformanceReport.cs
│   ├── Batch/ (现有目录)
│   │   ├── BatchImporter.cs
│   │   └── BatchExistenceChecker.cs
│   ├── Locking/ (现有目录)
│   │   └── ImportLockManager.cs
│   ├── Mappers/ (现有目录)
│   │   └── FieldMapper.cs
│   └── Models/ (现有目录)
│       ├── ImportContext.cs
│       ├── ImportErrorContext.cs
│       ├── TableImportStats.cs
│       └── ...
```

### 命名空间

```csharp
namespace AutoScheduling3.Services;
// DataImportExportService

namespace AutoScheduling3.Services.ImportExport;
// DataValidationService
// DataExportService
// DataMappingService
// OperationAuditLogger
```


## Dependency Injection Configuration

### ServiceCollectionExtensions 更新

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 现有服务注册...
        
        // 新增：导入导出相关服务
        services.AddSingleton<IDataValidationService, DataValidationService>();
        services.AddSingleton<IDataExportService, DataExportService>();
        services.AddSingleton<IDataMappingService, DataMappingService>();
        
        // 现有：主服务
        services.AddSingleton<IDataImportExportService, DataImportExportService>();
        
        return services;
    }
}
```

### 服务生命周期选择

- **Singleton** - 所有新服务都使用 Singleton，因为它们是无状态的
- **DataImportExportService** - 保持 Singleton
- **Repositories** - 保持现有配置

## Migration Strategy

### 阶段1：创建新服务（不破坏现有代码）

1. 创建 `IDataValidationService` 接口和 `DataValidationService` 实现
2. 创建 `IDataExportService` 接口和 `DataExportService` 实现
3. 创建 `IDataMappingService` 接口和 `DataMappingService` 实现
4. 从 `DataImportExportService` 复制相关代码到新服务
5. 在 DI 容器中注册新服务

### 阶段2：重构协调器（逐步替换）

1. 在 `DataImportExportService` 中注入新服务
2. 逐个方法替换为调用新服务
3. 保留旧方法标记为 `[Obsolete]`
4. 验证功能正常

### 阶段3：清理（移除冗余代码）

1. 移除 `DataImportExportService` 中的旧实现
2. 移除 `[Obsolete]` 标记
3. 更新文档
4. 运行完整测试套件

## Design Decisions and Rationales

### 为什么不使用 AutoMapper？

**决策：** 暂时保持手动映射，但在 `IDataMappingService` 接口中预留使用 AutoMapper 的可能性。

**理由：**
- 当前映射逻辑简单直接
- 避免引入新的依赖
- 如果未来映射变得复杂，可以轻松切换到 AutoMapper

### 为什么保留导入逻辑在协调器中？

**决策：** 导入逻辑暂时保留在 `DataImportExportService` 中，不创建单独的 `IDataImportService`。

**理由：**
- 导入逻辑已经通过 Importer 类进行了拆分
- 导入流程涉及复杂的事务管理和锁机制
- 避免过度拆分导致协调逻辑过于复杂

### 为什么使用 Singleton 生命周期？

**决策：** 所有新服务使用 Singleton 生命周期。

**理由：**
- 服务是无状态的
- 减少对象创建开销
- 与现有服务保持一致

### 为什么保持向后兼容？

**决策：** 保持 `IDataImportExportService` 接口和公共方法签名不变。

**理由：**
- 避免影响现有调用代码
- 降低重构风险
- 允许渐进式迁移

## Performance Considerations

虽然不进行性能优化，但需要确保重构不会导致性能退化：

1. **避免重复对象创建** - 使用 Singleton 服务
2. **保持批量操作** - 不改变现有的批量导入逻辑
3. **最小化方法调用开销** - 服务方法调用开销可忽略不计
4. **保持事务策略** - 不改变现有的事务管理方式

## Backward Compatibility

### 接口兼容性

- `IDataImportExportService` 接口保持不变
- 所有公共方法签名保持不变
- 返回类型保持不变

### 行为兼容性

- 异常处理行为保持不变
- 日志记录格式保持不变
- 进度报告机制保持不变
- 审计日志格式保持不变

### 数据兼容性

- 导入导出的JSON格式保持不变
- 数据库操作保持不变
- 验证规则保持不变
