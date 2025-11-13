# Requirements Document

## Introduction

本文档定义了重构 `DataImportExportService` 类的需求。当前该类承担了过多职责（约3000行代码），违反了单一职责原则（SRP），导致代码难以维护、测试和扩展。本次重构将该服务拆分为多个职责明确的服务类，提高代码的可维护性和可测试性。

## Glossary

- **DataImportExportService**: 当前的数据导入导出服务类，负责所有导入导出相关功能
- **SRP (Single Responsibility Principle)**: 单一职责原则，每个类应该只有一个引起变化的原因
- **Orchestrator**: 协调器，负责协调多个服务的执行流程
- **Validator**: 验证器，负责数据验证逻辑
- **Exporter**: 导出器，负责数据导出逻辑
- **Mapper**: 映射器，负责DTO与模型之间的转换
- **Repository**: 仓储，负责数据访问

## Requirements

### Requirement 1: 识别当前服务的职责

**User Story:** 作为开发人员，我希望清楚地识别 `DataImportExportService` 当前承担的所有职责，以便合理地进行拆分。

#### Acceptance Criteria

1. THE System SHALL 识别并列出 `DataImportExportService` 中的所有主要职责类别
2. THE System SHALL 分析每个职责的代码行数和复杂度
3. THE System SHALL 识别职责之间的依赖关系
4. THE System SHALL 记录当前的依赖注入结构

### Requirement 2: 设计服务拆分架构

**User Story:** 作为架构师，我希望设计一个清晰的服务拆分架构，使每个服务类都遵循单一职责原则。

#### Acceptance Criteria

1. THE System SHALL 设计一个主协调器服务（Orchestrator）来协调导入导出流程
2. THE System SHALL 设计独立的验证服务（Validator）来处理所有数据验证逻辑
3. THE System SHALL 设计独立的导出服务（Exporter）来处理数据导出逻辑
4. THE System SHALL 设计独立的映射服务（Mapper）来处理DTO与模型的转换
5. THE System SHALL 确保新架构保持向后兼容性

### Requirement 3: 创建数据验证服务

**User Story:** 作为开发人员，我希望将所有数据验证逻辑提取到独立的服务中，以便更容易地测试和维护验证规则。

#### Acceptance Criteria

1. THE System SHALL 创建 `DataValidationService` 类来处理所有验证逻辑
2. THE System SHALL 将 `ValidateImportDataAsync` 方法迁移到新服务
3. THE System SHALL 将 `ValidateRequiredFields` 方法迁移到新服务
4. THE System SHALL 将 `ValidateDataConstraints` 方法迁移到新服务
5. THE System SHALL 将 `ValidateForeignKeyReferences` 方法迁移到新服务

### Requirement 4: 创建数据导出服务

**User Story:** 作为开发人员，我希望将所有数据导出逻辑提取到独立的服务中，以便更容易地扩展导出功能。

#### Acceptance Criteria

1. THE System SHALL 创建 `DataExportService` 类来处理所有导出逻辑
2. THE System SHALL 将所有 `Export*Async` 方法迁移到新服务
3. THE System SHALL 将导出相关的辅助方法迁移到新服务
4. THE System SHALL 保持导出进度报告功能
5. THE System SHALL 保持导出统计信息生成功能

### Requirement 5: 创建数据映射服务

**User Story:** 作为开发人员，我希望将所有DTO与模型的映射逻辑提取到独立的服务中，以便统一管理映射规则。

#### Acceptance Criteria

1. THE System SHALL 创建 `DataMappingService` 类来处理所有映射逻辑
2. THE System SHALL 将所有 `MapTo*` 方法迁移到新服务
3. THE System SHALL 提供双向映射功能（DTO到模型，模型到DTO）
4. THE System SHALL 确保映射逻辑的可测试性
5. THE System SHALL 考虑使用 AutoMapper 或类似工具来简化映射代码

### Requirement 6: 重构主服务为协调器

**User Story:** 作为开发人员，我希望将 `DataImportExportService` 重构为一个轻量级的协调器，只负责协调其他服务的执行。

#### Acceptance Criteria

1. THE System SHALL 保留 `DataImportExportService` 作为主入口点
2. THE System SHALL 将 `DataImportExportService` 重构为协调器模式
3. THE System SHALL 在协调器中注入所有新创建的服务
4. THE System SHALL 保持原有的公共API接口不变
5. THE System SHALL 将具体实现委托给专门的服务

### Requirement 7: 保持向后兼容性

**User Story:** 作为系统维护者，我希望重构后的代码保持向后兼容，不影响现有的调用代码。

#### Acceptance Criteria

1. THE System SHALL 保持 `IDataImportExportService` 接口不变
2. THE System SHALL 保持所有公共方法签名不变
3. THE System SHALL 保持相同的异常处理行为
4. THE System SHALL 保持相同的日志记录行为
5. THE System SHALL 确保所有现有功能正常工作

### Requirement 8: 更新依赖注入配置

**User Story:** 作为开发人员，我希望更新依赖注入配置，以便正确注册所有新创建的服务。

#### Acceptance Criteria

1. THE System SHALL 在 `ServiceCollectionExtensions` 中注册所有新服务
2. THE System SHALL 配置正确的服务生命周期（Singleton/Scoped/Transient）
3. THE System SHALL 确保服务依赖关系正确配置
4. THE System SHALL 验证依赖注入容器能够正确解析所有服务
5. THE System SHALL 更新相关文档说明新的服务结构

### Requirement 9: 改进代码可测试性

**User Story:** 作为测试工程师，我希望重构后的代码更容易进行单元测试。

#### Acceptance Criteria

1. THE System SHALL 确保每个新服务都有明确的接口定义
2. THE System SHALL 确保服务之间的依赖通过接口注入
3. THE System SHALL 减少每个服务的依赖数量
4. THE System SHALL 使每个服务的方法更加独立和可测试
5. THE System SHALL 提供测试示例或测试指南

### Requirement 10: 定义合理的文件组织结构

**User Story:** 作为开发人员，我希望重构后的代码有清晰的文件组织结构，便于查找和维护。

#### Acceptance Criteria

1. THE System SHALL 在 `Services/ImportExport` 目录下组织所有导入导出相关服务
2. THE System SHALL 为验证、导出、映射等不同职责创建独立的文件
3. THE System SHALL 保持现有的 Importers、Comparison、Monitoring 等子目录结构
4. THE System SHALL 确保文件命名清晰反映其职责
5. THE System SHALL 更新命名空间以反映新的文件结构
