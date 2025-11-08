# Requirements Document

## Introduction

本功能旨在将 AutoScheduling3 应用程序的所有数据存储从使用 `Environment.SpecialFolder.LocalApplicationData` 迁移到使用 WinUI3 标准的 `ApplicationData.Current.LocalFolder`。这符合 WinUI3 的最佳实践，并确保应用程序在 Windows 应用商店环境中正确运行。

## Glossary

- **ApplicationData**: WinUI3 提供的应用程序数据管理 API，用于访问应用程序的本地、漫游和临时数据存储位置
- **LocalFolder**: ApplicationData.Current.LocalFolder，应用程序的本地数据存储文件夹
- **DatabaseService**: 管理 SQLite 数据库初始化、版本管理和迁移的服务
- **DatabaseConfiguration**: 提供数据库路径和连接设置的配置类
- **StoragePathService**: 管理应用程序存储文件信息的服务
- **Legacy Path**: 使用 Environment.SpecialFolder.LocalApplicationData 的旧路径
- **New Path**: 使用 ApplicationData.Current.LocalFolder 的新路径

## Requirements

### Requirement 1: 更新数据库路径配置

**User Story:** 作为开发者，我希望数据库配置使用 WinUI3 标准的 ApplicationData API，以便应用程序符合平台最佳实践

#### Acceptance Criteria

1. WHEN THE DatabaseConfiguration 类获取默认数据库路径时，THE DatabaseConfiguration SHALL 使用 ApplicationData.Current.LocalFolder 而不是 Environment.SpecialFolder.LocalApplicationData
2. WHEN THE DatabaseConfiguration 类创建数据库目录时，THE DatabaseConfiguration SHALL 在 ApplicationData.Current.LocalFolder 下创建应用程序子文件夹
3. WHEN THE DatabaseConfiguration 类获取备份路径时，THE DatabaseConfiguration SHALL 在 ApplicationData.Current.LocalFolder 下的 backups 子文件夹中创建备份文件
4. THE DatabaseConfiguration SHALL 保持向后兼容性，能够检测并迁移旧路径的数据库文件

### Requirement 2: 更新存储路径服务

**User Story:** 作为开发者，我希望存储路径服务使用 WinUI3 标准的 ApplicationData API，以便统一管理所有应用程序数据文件

#### Acceptance Criteria

1. WHEN THE StoragePathService 获取配置文件路径时，THE StoragePathService SHALL 使用 ApplicationData.Current.LocalFolder
2. WHEN THE StoragePathService 获取数据库文件路径时，THE StoragePathService SHALL 使用 DatabaseConfiguration 提供的新路径
3. THE StoragePathService SHALL 正确报告所有存储文件的位置和状态

### Requirement 3: 实现数据迁移逻辑

**User Story:** 作为用户，我希望应用程序能够自动迁移现有数据，以便升级后不会丢失任何数据

#### Acceptance Criteria

1. WHEN THE 应用程序首次启动使用新路径时，THE DatabaseService SHALL 检测旧路径是否存在数据库文件
2. IF 旧路径存在数据库文件且新路径不存在，THEN THE DatabaseService SHALL 自动将数据库文件复制到新路径
3. WHEN THE 数据迁移完成后，THE DatabaseService SHALL 在旧路径创建一个迁移标记文件，记录迁移时间和新路径
4. IF 数据迁移失败，THEN THE DatabaseService SHALL 记录错误日志并回退到旧路径
5. THE DatabaseService SHALL 在迁移前创建数据库备份

### Requirement 4: 更新备份管理器

**User Story:** 作为用户，我希望数据库备份存储在标准的应用程序数据位置，以便系统能够正确管理备份文件

#### Acceptance Criteria

1. WHEN THE DatabaseBackupManager 创建备份时，THE DatabaseBackupManager SHALL 将备份文件存储在 ApplicationData.Current.LocalFolder 的 backups 子文件夹中
2. THE DatabaseBackupManager SHALL 能够列出和管理新路径下的所有备份文件
3. THE DatabaseBackupManager SHALL 在迁移时保留旧路径的备份文件引用

### Requirement 5: 确保路径访问权限

**User Story:** 作为开发者，我希望应用程序能够正确处理路径访问权限，以便在受限环境中正常运行

#### Acceptance Criteria

1. WHEN THE 应用程序访问 ApplicationData.Current.LocalFolder 时，THE 应用程序 SHALL 正确处理 UnauthorizedAccessException
2. WHEN THE 应用程序创建子文件夹时，THE 应用程序 SHALL 验证文件夹创建成功
3. IF 路径访问失败，THEN THE 应用程序 SHALL 提供清晰的错误消息给用户

### Requirement 6: 更新测试和诊断

**User Story:** 作为开发者，我希望测试和诊断功能能够正确反映新的存储路径，以便验证迁移的正确性

#### Acceptance Criteria

1. THE DatabaseService 诊断功能 SHALL 报告当前使用的存储路径
2. THE DatabaseService 诊断功能 SHALL 检测是否存在旧路径的数据文件
3. THE StoragePathService SHALL 在存储文件信息中显示正确的文件路径
4. THE 应用程序 SHALL 提供一个方法来验证 ApplicationData.Current.LocalFolder 的可访问性

### Requirement 7: 文档和日志更新

**User Story:** 作为开发者和用户，我希望有清晰的文档和日志记录路径变更，以便理解和排查问题

#### Acceptance Criteria

1. THE DatabaseService SHALL 在初始化时记录使用的数据库路径
2. WHEN THE 数据迁移发生时，THE DatabaseService SHALL 记录详细的迁移日志
3. THE 应用程序 SHALL 在启动时验证并记录 ApplicationData.Current.LocalFolder 的路径
4. THE 代码注释 SHALL 说明为什么使用 ApplicationData.Current.LocalFolder
