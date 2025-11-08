# Requirements Document

## Introduction

本功能旨在将 AutoScheduling3 应用程序的所有数据存储从使用 `Environment.SpecialFolder.LocalApplicationData` 迁移到使用 WinUI3 标准的 `ApplicationData.Current.LocalFolder`。这符合 WinUI3 的最佳实践，并确保应用程序在 Windows 应用商店环境中正确运行。

## Glossary

- **ApplicationData**: WinUI3 提供的应用程序数据管理 API，用于访问应用程序的本地、漫游和临时数据存储位置
- **LocalFolder**: ApplicationData.Current.LocalFolder，应用程序的本地数据存储文件夹
- **DatabaseService**: 管理 SQLite 数据库初始化、版本管理和迁移的服务
- **DatabaseConfiguration**: 提供数据库路径和连接设置的配置类
- **StoragePathService**: 管理应用程序存储文件信息的服务


## Requirements

### Requirement 1: 更新数据库路径配置

**User Story:** 作为开发者，我希望数据库配置使用 WinUI3 标准的 ApplicationData API，以便应用程序符合平台最佳实践

#### Acceptance Criteria

1. WHEN THE DatabaseConfiguration 类获取默认数据库路径时，THE DatabaseConfiguration SHALL 使用 ApplicationData.Current.LocalFolder 而不是 Environment.SpecialFolder.LocalApplicationData
2. WHEN THE DatabaseConfiguration 类创建数据库目录时，THE DatabaseConfiguration SHALL 在 ApplicationData.Current.LocalFolder 下直接存储数据库文件
3. WHEN THE DatabaseConfiguration 类获取备份路径时，THE DatabaseConfiguration SHALL 在 ApplicationData.Current.LocalFolder 下的 backups 子文件夹中创建备份文件

### Requirement 2: 更新配置服务

**User Story:** 作为开发者，我希望配置服务使用 WinUI3 标准的 ApplicationData API，以便配置文件存储在正确的位置

#### Acceptance Criteria

1. WHEN THE ConfigurationService 初始化时，THE ConfigurationService SHALL 使用 ApplicationData.Current.LocalFolder 作为配置文件的存储路径
2. THE ConfigurationService SHALL 在 ApplicationData.Current.LocalFolder 下的 Settings 子文件夹中存储 config.json 文件
3. THE ConfigurationService SHALL 在初始化时创建 Settings 文件夹（如果不存在）
4. THE ConfigurationService SHALL 正确处理文件夹创建和文件访问权限

### Requirement 3: 更新存储路径服务

**User Story:** 作为开发者，我希望存储路径服务使用 WinUI3 标准的 ApplicationData API，以便统一管理所有应用程序数据文件

#### Acceptance Criteria

1. WHEN THE StoragePathService 获取配置文件路径时，THE StoragePathService SHALL 使用 ApplicationData.Current.LocalFolder
2. WHEN THE StoragePathService 获取数据库文件路径时，THE StoragePathService SHALL 使用 DatabaseConfiguration 提供的新路径
3. THE StoragePathService SHALL 正确报告所有存储文件的位置和状态

### Requirement 4: 更新备份管理器

**User Story:** 作为用户，我希望数据库备份存储在标准的应用程序数据位置，以便系统能够正确管理备份文件

#### Acceptance Criteria

1. WHEN THE DatabaseBackupManager 创建备份时，THE DatabaseBackupManager SHALL 将备份文件存储在 ApplicationData.Current.LocalFolder 的 backups 子文件夹中
2. THE DatabaseBackupManager SHALL 能够列出和管理新路径下的所有备份文件

### Requirement 5: 确保路径访问权限

**User Story:** 作为开发者，我希望应用程序能够正确处理路径访问权限，以便在受限环境中正常运行

#### Acceptance Criteria

1. WHEN THE 应用程序访问 ApplicationData.Current.LocalFolder 时，THE 应用程序 SHALL 正确处理 UnauthorizedAccessException
2. WHEN THE 应用程序创建子文件夹时，THE 应用程序 SHALL 验证文件夹创建成功
3. IF 路径访问失败，THEN THE 应用程序 SHALL 提供清晰的错误消息给用户

### Requirement 6: 更新测试和诊断

**User Story:** 作为开发者，我希望测试和诊断功能能够正确反映新的存储路径，以便验证路径变更的正确性

#### Acceptance Criteria

1. THE DatabaseService 诊断功能 SHALL 报告当前使用的存储路径
2. THE StoragePathService SHALL 在存储文件信息中显示正确的文件路径
3. THE 应用程序 SHALL 提供一个方法来验证 ApplicationData.Current.LocalFolder 的可访问性

### Requirement 7: 更新设置界面显示存储位置

**User Story:** 作为用户，我希望在设置界面中看到当前的数据存储位置，以便了解我的数据存储在哪里

#### Acceptance Criteria

1. THE 设置界面 SHALL 显示当前使用的 ApplicationData.Current.LocalFolder 路径
2. THE 设置界面 SHALL 显示数据库文件的完整路径
3. THE 设置界面 SHALL 显示备份文件夹的路径
4. THE 设置界面 SHALL 提供一个按钮来打开数据存储文件夹

### Requirement 8: 文档和日志更新

**User Story:** 作为开发者和用户，我希望有清晰的文档和日志记录路径变更，以便理解和排查问题

#### Acceptance Criteria

1. THE DatabaseService SHALL 在初始化时记录使用的数据库路径
2. THE 应用程序 SHALL 在启动时验证并记录 ApplicationData.Current.LocalFolder 的路径
3. THE 代码注释 SHALL 说明为什么使用 ApplicationData.Current.LocalFolder
