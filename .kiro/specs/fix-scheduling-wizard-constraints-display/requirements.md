# Requirements Document

## Introduction

本规范旨在解决排班向导（CreateSchedulingPage）中第4步约束配置页面不显示内容的问题。用户在使用排班向导时，进入第4步"配置约束"页面后，约束数据（休息日配置、定岗规则、手动指定）无法正常显示，导致用户无法配置排班约束。

## Glossary

- **SchedulingWizard**: 排班向导，指 CreateSchedulingPage 页面，用于引导用户完成排班参数配置的分步界面
- **ConstraintData**: 约束数据，包括休息日配置（HolidayConfig）、定岗规则（FixedPositionRule）和手动指定（ManualAssignment）
- **SchedulingViewModel**: 排班视图模型，负责管理排班向导的状态和数据
- **ConstraintRepository**: 约束数据仓库，负责从数据库加载约束数据
- **LoadConstraintsCommand**: 加载约束命令，用于从数据库加载约束数据到视图模型

## Requirements

### Requirement 1

**User Story:** 作为排班管理员，我希望在排班向导的第4步能够看到所有可用的约束配置选项，以便我可以选择合适的约束来生成排班表

#### Acceptance Criteria

1. WHEN 用户导航到排班向导的第4步，THE SchedulingWizard SHALL 自动触发约束数据加载
2. WHILE 约束数据正在加载，THE SchedulingWizard SHALL 显示加载指示器
3. WHEN 约束数据加载完成，THE SchedulingWizard SHALL 显示所有可用的休息日配置列表
4. WHEN 约束数据加载完成，THE SchedulingWizard SHALL 显示所有启用的定岗规则列表
5. WHEN 约束数据加载完成，THE SchedulingWizard SHALL 显示指定日期范围内的手动指定列表

### Requirement 2

**User Story:** 作为排班管理员，我希望约束数据加载失败时能够看到明确的错误提示，以便我可以了解问题并采取相应措施

#### Acceptance Criteria

1. IF 约束数据加载失败，THEN THE SchedulingWizard SHALL 显示错误对话框
2. WHEN 显示错误对话框，THE SchedulingWizard SHALL 包含具体的错误信息
3. WHEN 约束数据加载失败，THE SchedulingWizard SHALL 允许用户重试加载或返回上一步

### Requirement 3

**User Story:** 作为排班管理员，我希望在修改日期范围后约束数据能够自动更新，以便我可以看到与当前日期范围相关的约束

#### Acceptance Criteria

1. WHEN 用户在第1步修改开始日期或结束日期，THE SchedulingWizard SHALL 在进入第4步时重新加载约束数据
2. WHEN 约束数据重新加载，THE SchedulingWizard SHALL 仅显示与当前日期范围相关的手动指定
3. WHEN 约束数据重新加载，THE SchedulingWizard SHALL 保持用户之前选择的约束启用状态

### Requirement 4

**User Story:** 作为排班管理员，我希望从模板加载排班时约束数据能够正确应用，以便我可以快速使用预设的约束配置

#### Acceptance Criteria

1. WHEN 用户从模板加载排班，THE SchedulingWizard SHALL 加载模板中保存的约束配置
2. WHEN 约束数据加载完成，THE SchedulingWizard SHALL 根据模板设置自动启用相应的约束
3. WHEN 用户进入第4步，THE SchedulingWizard SHALL 显示已启用的约束项
4. WHEN 模板中的约束在数据库中不存在，THE SchedulingWizard SHALL 显示警告信息

### Requirement 5

**User Story:** 作为开发人员，我希望约束数据加载过程有详细的日志记录，以便我可以诊断和调试数据加载问题

#### Acceptance Criteria

1. WHEN 约束数据加载开始，THE SchedulingViewModel SHALL 记录调试日志
2. WHEN 约束数据加载完成，THE SchedulingViewModel SHALL 记录加载的数据数量
3. IF 约束数据加载失败，THEN THE SchedulingViewModel SHALL 记录详细的错误信息和堆栈跟踪
4. WHEN 约束数据从数据库返回为空，THE SchedulingViewModel SHALL 记录警告日志
