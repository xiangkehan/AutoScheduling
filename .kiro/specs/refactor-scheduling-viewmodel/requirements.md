# 需求文档

## 简介

SchedulingViewModel 是排班向导的核心视图模型,当前文件包含 3012 行代码,远超过项目规范的 1000 行限制。该文件负责管理排班创建的完整流程,包括基本信息配置、人员选择、哨位选择、约束配置、手动指定管理、草稿保存等多个功能模块。需要将其拆分为多个职责单一的文件,提高代码的可维护性和可测试性。

## 术语表

- **SchedulingViewModel**: 排班向导的主视图模型类
- **Partial Class**: C# 的部分类特性,允许将一个类的定义分散到多个文件中
- **MVVM**: Model-View-ViewModel 架构模式
- **ObservableProperty**: CommunityToolkit.Mvvm 提供的属性特性,用于自动实现属性变更通知
- **RelayCommand**: CommunityToolkit.Mvvm 提供的命令实现
- **手动指定**: 用户手动指定某个人员在特定日期和时段值守特定哨位的约束
- **草稿**: 保存排班创建过程中的临时状态,允许用户稍后继续编辑
- **模板**: 预定义的排班配置,包含人员、哨位和约束设置

## 需求

### 需求 1: 文件大小合规

**用户故事:** 作为开发人员,我希望所有代码文件都符合项目规范(不超过 1000 行),以便更容易理解和维护代码。

#### 验收标准

1. WHEN 拆分完成后,THE System SHALL 确保每个文件的行数不超过 1000 行
2. WHEN 拆分完成后,THE System SHALL 确保主文件 SchedulingViewModel.cs 的行数在 300-600 行之间
3. WHEN 拆分完成后,THE System SHALL 确保所有部分类文件的行数在 200-800 行之间
4. WHEN 拆分完成后,THE System SHALL 保持原有功能完全不变

### 需求 2: 职责清晰分离

**用户故事:** 作为开发人员,我希望每个文件都有明确的职责,以便快速定位和修改特定功能的代码。

#### 验收标准

1. WHEN 查看文件结构时,THE System SHALL 按功能模块将代码分离到不同的部分类文件中
2. WHEN 查看主文件时,THE System SHALL 仅包含构造函数、核心属性和依赖注入
3. WHEN 查看命令文件时,THE System SHALL 仅包含命令定义和命令执行方法
4. WHEN 查看验证文件时,THE System SHALL 仅包含表单验证和步骤验证逻辑
5. WHEN 查看状态管理文件时,THE System SHALL 仅包含状态重置、草稿保存和恢复逻辑

### 需求 3: 手动指定管理独立

**用户故事:** 作为开发人员,我希望手动指定相关的代码集中在一个文件中,以便更容易维护这个复杂的功能模块。

#### 验收标准

1. WHEN 查看手动指定文件时,THE System SHALL 包含所有手动指定的 CRUD 操作方法
2. WHEN 查看手动指定文件时,THE System SHALL 包含手动指定的表单管理逻辑
3. WHEN 查看手动指定文件时,THE System SHALL 包含手动指定的验证逻辑
4. WHEN 修改手动指定功能时,THE System SHALL 只需要修改一个文件

### 需求 4: 哨位人员管理独立

**用户故事:** 作为开发人员,我希望哨位人员管理相关的代码集中在一个文件中,以便更容易维护这个功能模块。

#### 验收标准

1. WHEN 查看哨位人员管理文件时,THE System SHALL 包含为哨位添加人员的所有方法
2. WHEN 查看哨位人员管理文件时,THE System SHALL 包含临时移除人员的方法
3. WHEN 查看哨位人员管理文件时,THE System SHALL 包含撤销和保存更改的方法
4. WHEN 查看哨位人员管理文件时,THE System SHALL 包含手动添加参与人员的方法

### 需求 5: 模板和约束管理独立

**用户故事:** 作为开发人员,我希望模板加载和约束配置相关的代码集中在一个文件中,以便更容易维护这些功能。

#### 验收标准

1. WHEN 查看模板约束文件时,THE System SHALL 包含加载模板的方法
2. WHEN 查看模板约束文件时,THE System SHALL 包含保存模板的方法
3. WHEN 查看模板约束文件时,THE System SHALL 包含加载约束数据的方法
4. WHEN 查看模板约束文件时,THE System SHALL 包含应用模板约束的方法

### 需求 6: 向导流程管理独立

**用户故事:** 作为开发人员,我希望向导的步骤导航和流程控制逻辑集中在一个文件中,以便更容易理解和维护向导流程。

#### 验收标准

1. WHEN 查看向导流程文件时,THE System SHALL 包含步骤导航方法(NextStep, PreviousStep)
2. WHEN 查看向导流程文件时,THE System SHALL 包含步骤验证方法
3. WHEN 查看向导流程文件时,THE System SHALL 包含执行排班的方法
4. WHEN 查看向导流程文件时,THE System SHALL 包含构建排班请求的方法
5. WHEN 查看向导流程文件时,THE System SHALL 包含构建摘要的方法

### 需求 7: 保持现有功能

**用户故事:** 作为用户,我希望重构后的系统功能与重构前完全一致,不会出现任何功能缺失或行为变化。

#### 验收标准

1. WHEN 重构完成后,THE System SHALL 保持所有公共 API 不变
2. WHEN 重构完成后,THE System SHALL 保持所有命令的行为不变
3. WHEN 重构完成后,THE System SHALL 保持所有属性的行为不变
4. WHEN 重构完成后,THE System SHALL 保持所有事件处理逻辑不变
5. WHEN 编译项目时,THE System SHALL 成功编译且无警告

### 需求 8: 命名规范一致

**用户故事:** 作为开发人员,我希望所有拆分后的文件都遵循统一的命名规范,以便快速识别文件的用途。

#### 验收标准

1. WHEN 查看文件名时,THE System SHALL 使用格式 "SchedulingViewModel.{功能模块}.cs"
2. WHEN 查看文件名时,THE System SHALL 使用英文描述功能模块
3. WHEN 查看文件名时,THE System SHALL 确保文件名清晰表达文件内容
4. WHEN 查看文件结构时,THE System SHALL 将所有部分类文件放在同一目录下
