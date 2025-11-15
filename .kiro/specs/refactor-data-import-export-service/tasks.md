# Implementation Plan

- [x] 1. 创建数据验证服务





  - 创建 `IDataValidationService` 接口，定义所有验证方法
  - 创建 `DataValidationService` 类实现接口
  - 从 `DataImportExportService` 迁移 `ValidateImportDataAsync` 方法
  - 从 `DataImportExportService` 迁移 `ValidateRequiredFields` 方法
  - 从 `DataImportExportService` 迁移 `ValidateDataConstraints` 方法
  - 从 `DataImportExportService` 迁移 `ValidateForeignKeyReferences` 方法
  - 确保所有验证逻辑正确迁移，包括错误处理和日志记录
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 2. 创建数据导出服务




  - 创建 `IDataExportService` 接口，定义所有导出方法
  - 创建 `DataExportService` 类实现接口
  - 从 `DataImportExportService` 迁移 `ExportSkillsAsync` 方法
  - 从 `DataImportExportService` 迁移 `ExportPersonnelAsync` 方法
  - 从 `DataImportExportService` 迁移 `ExportPositionsAsync` 方法
  - 从 `DataImportExportService` 迁移 `ExportTemplatesAsync` 方法
  - 从 `DataImportExportService` 迁移 `ExportFixedAssignmentsAsync` 方法
  - 从 `DataImportExportService` 迁移 `ExportManualAssignmentsAsync` 方法
  - 从 `DataImportExportService` 迁移 `ExportHolidayConfigsAsync` 方法
  - 从 `DataImportExportService` 迁移 `CalculateStatistics` 方法
  - 从 `DataImportExportService` 迁移 `GetDatabaseVersionAsync` 和 `GetApplicationVersion` 辅助方法
  - 确保导出服务使用映射服务进行DTO转换
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 3. 创建数据映射服务





  - 创建 `IDataMappingService` 接口，定义所有映射方法（双向）
  - 创建 `DataMappingService` 类实现接口
  - 从 `DataImportExportService` 迁移 `MapToSkill` 方法
  - 从 `DataImportExportService` 迁移 `MapToPersonnel` 方法
  - 从 `DataImportExportService` 迁移 `MapToPosition` 方法
  - 从 `DataImportExportService` 迁移 `MapToHolidayConfig` 方法
  - 从 `DataImportExportService` 迁移 `MapToTemplate` 方法
  - 从 `DataImportExportService` 迁移 `MapToFixedPositionRule` 方法
  - 从 `DataImportExportService` 迁移 `MapToManualAssignment` 方法
  - 实现反向映射方法（Model to DTO）用于导出服务
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [x] 4. 更新依赖注入配置




  - 在 `ServiceCollectionExtensions` 中注册 `IDataValidationService`
  - 在 `ServiceCollectionExtensions` 中注册 `IDataExportService`
  - 在 `ServiceCollectionExtensions` 中注册 `IDataMappingService`
  - 配置所有新服务为 Singleton 生命周期
  - 验证依赖注入容器能够正确解析所有服务
  - _Requirements: 8.1, 8.2, 8.3, 8.4_




- [x] 5. 重构 DataImportExportService 为协调器










  - 在 `DataImportExportService` 构造函数中注入新创建的三个服务
  - 重构 `ExportDataAsync` 方法，使用 `IDataExportService` 进行数据导出
  - 重构 `ValidateImportDataAsync` 方法，委托给 `IDataValidationService`
  - 保持 `ImportDataAsync` 方法的事务和锁管理逻辑
  - 更新导出流程中的映射调用，使用 `IDataMappingService`
  - 保持所有公共方法签名不变

  - 保持异常处理和日志记录行为不变
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 7.1, 7.2, 7.3, 7.4_

- [x] 6. 清理和优化代码






  - 从 `DataImportExportService` 中移除已迁移到新服务的私有方法
  - 移除不再使用的字段和依赖
  - 更新代码注释和文档字符串
  - 确保命名空间正确反映文件结构
  - 验证所有文件位于正确的目录中
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_

- [ ] 7. 验证向后兼容性和功能完整性
  - 验证 `IDataImportExportService` 接口未被修改
  - 验证所有公共方法签名保持不变
  - 测试导出功能，确保生成的JSON格式与之前一致
  - 测试导入功能，确保数据正确导入数据库
  - 测试验证功能，确保所有验证规则正常工作
  - 验证异常处理行为与重构前一致
  - 验证日志记录格式和内容与重构前一致
  - 验证审计日志功能正常工作
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ]* 8. 编写单元测试
  - 为 `DataValidationService` 编写单元测试
  - 为 `DataExportService` 编写单元测试
  - 为 `DataMappingService` 编写单元测试
  - 为重构后的 `DataImportExportService` 编写单元测试
  - 使用模拟对象隔离测试依赖
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [ ]* 9. 更新文档
  - 更新架构文档，说明新的服务结构
  - 更新开发者指南，说明如何使用新服务
  - 在 `ServiceCollectionExtensions` 中添加注释说明服务注册
  - 更新 README 或相关文档，说明重构的变更
  - _Requirements: 8.5_
