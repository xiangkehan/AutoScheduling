# Requirements Document

## Introduction

本需求文档定义了在排班配置向导的约束配置步骤中添加手动指定（ManualAssignment）创建和管理功能。手动指定允许用户在生成排班表时预先指定某个人员在特定日期、时段和哨位上哨。该功能支持两种使用场景：临时手动指定（仅在当前排班中生效）和模板手动指定（保存到模板中，在使用该模板时生效）。同时，在最后的确认步骤中需要显示所有启用的手动指定的详细信息。

## Glossary

- **System**: 指自动排班系统（AutoScheduling3）
- **CreateSchedulingPage**: 创建排班页面，包含5步配置向导
- **ManualAssignment**: 手动指定，定义特定人员在特定日期、时段和哨位上哨的强制约束
- **ConstraintConfigurationStep**: 约束配置步骤，即向导的第4步
- **SummaryStep**: 确认步骤，即向导的第5步
- **SchedulingTemplate**: 排班模板，保存排班配置参数供后续复用
- **SchedulingService**: 排班服务，负责手动指定的业务逻辑处理
- **ManualAssignmentDto**: 手动指定数据传输对象
- **CreateManualAssignmentDto**: 创建手动指定的数据传输对象

## Requirements

### Requirement 1

**User Story:** 作为排班管理员，我希望在约束配置步骤中创建新的手动指定，以便在生成排班表时强制指定某些人员在特定时间和哨位上哨

#### Acceptance Criteria

1. THE System SHALL 在约束配置步骤的手动指定区域提供"添加手动指定"按钮
2. WHEN 用户点击"添加手动指定"按钮，THE System SHALL 显示手动指定创建表单
3. THE System SHALL 在创建表单中提供日期选择器，限制日期范围在排班开始日期和结束日期之间
4. THE System SHALL 在创建表单中提供人员下拉列表，列表包含步骤2中选择的所有人员
5. THE System SHALL 在创建表单中提供哨位下拉列表，列表包含步骤3中选择的所有哨位
6. THE System SHALL 在创建表单中提供时段下拉列表，列表包含12个时段（0-11）及其对应的时间范围
7. THE System SHALL 在创建表单中提供描述输入框和启用状态开关
8. WHEN 用户提交创建表单，THE System SHALL 验证所有必填字段已填写
9. WHEN 表单验证通过，THE System SHALL 将新的手动指定添加到临时列表中
10. WHEN 手动指定创建成功，THE System SHALL 刷新手动指定列表并关闭创建表单

### Requirement 2

**User Story:** 作为排班管理员，我希望编辑临时创建的手动指定，以便在提交排班前修正错误

#### Acceptance Criteria

1. THE System SHALL 在每条临时创建的手动指定卡片上提供"编辑"按钮
2. WHEN 用户点击手动指定的"编辑"按钮，THE System SHALL 显示编辑表单并预填充该手动指定的现有数据
3. THE System SHALL 在编辑表单中允许用户修改日期、人员、哨位、时段、描述和启用状态
4. WHEN 用户提交编辑表单，THE System SHALL 验证所有必填字段已填写
5. WHEN 表单验证通过，THE System SHALL 更新临时列表中的手动指定
6. THE System SHALL 提供"取消"按钮，允许用户放弃编辑并恢复原始数据

### Requirement 3

**User Story:** 作为排班管理员，我希望删除临时创建的手动指定，以便移除不需要的指定

#### Acceptance Criteria

1. THE System SHALL 在每条临时创建的手动指定卡片上提供"删除"按钮
2. WHEN 用户点击手动指定的"删除"按钮，THE System SHALL 显示确认对话框
3. THE System SHALL 在确认对话框中显示警告信息"确定要删除此手动指定吗？"
4. WHEN 用户确认删除，THE System SHALL 从临时列表中移除该手动指定
5. THE System SHALL 刷新手动指定列表

### Requirement 4

**User Story:** 作为排班管理员，我希望区分临时手动指定和已保存的手动指定，以便了解哪些指定会被持久化

#### Acceptance Criteria

1. THE System SHALL 在手动指定列表中使用不同的视觉样式区分临时手动指定和已保存的手动指定
2. THE System SHALL 在临时手动指定卡片上显示"临时"标签
3. THE System SHALL 仅在临时手动指定卡片上显示编辑和删除按钮
4. THE System SHALL 在已保存的手动指定卡片上仅显示启用/禁用开关

### Requirement 5

**User Story:** 作为排班管理员，我希望在保存为模板时将临时手动指定一起保存，以便在使用该模板时自动应用这些指定

#### Acceptance Criteria

1. WHEN 用户在确认步骤点击"保存为模板"按钮，THE System SHALL 将所有临时手动指定包含在模板数据中
2. THE System SHALL 调用SchedulingService的CreateManualAssignmentAsync方法保存每个临时手动指定
3. WHEN 模板保存成功，THE System SHALL 将临时手动指定转换为已保存的手动指定
4. THE System SHALL 更新手动指定列表的视觉状态，移除"临时"标签
5. IF 保存手动指定失败，THEN THE System SHALL 显示错误消息并回滚模板保存操作

### Requirement 6

**User Story:** 作为排班管理员，我希望在不保存模板的情况下执行排班时，临时手动指定仅在本次排班中生效

#### Acceptance Criteria

1. WHEN 用户在确认步骤点击"开始排班"按钮且未保存模板，THE System SHALL 将临时手动指定包含在排班请求中
2. THE System SHALL 在SchedulingRequestDto中包含临时手动指定的数据
3. THE System SHALL 确保临时手动指定在排班执行后不被持久化到数据库
4. WHEN 排班执行完成，THE System SHALL 清空临时手动指定列表

### Requirement 7

**User Story:** 作为排班管理员，我希望在确认步骤中查看所有启用的手动指定的详细信息，以便在执行排班前进行最后确认

#### Acceptance Criteria

1. THE System SHALL 在确认步骤的摘要信息中添加"手动指定"部分
2. WHEN 存在启用的手动指定，THE System SHALL 显示每条手动指定的详细信息
3. THE System SHALL 为每条手动指定显示以下信息：日期、人员姓名、哨位名称、时段（包含时间范围）、描述
4. THE System SHALL 按日期和时段排序显示手动指定列表
5. WHEN 没有启用的手动指定，THE System SHALL 显示"无启用的手动指定"

### Requirement 8

**User Story:** 作为排班管理员，我希望从模板加载排班配置时，自动加载该模板关联的手动指定

#### Acceptance Criteria

1. WHEN 用户从模板加载排班配置，THE System SHALL 调用SchedulingService获取该模板关联的手动指定
2. THE System SHALL 将模板关联的手动指定显示在手动指定列表中
3. THE System SHALL 将模板关联的手动指定标记为已保存状态
4. THE System SHALL 允许用户在模板手动指定的基础上添加新的临时手动指定

### Requirement 9

**User Story:** 作为排班管理员，我希望手动指定表单提供清晰的验证反馈，以便快速修正输入错误

#### Acceptance Criteria

1. WHEN 用户未选择日期，THE System SHALL 在日期字段下方显示错误消息"请选择日期"
2. WHEN 用户未选择人员，THE System SHALL 在人员字段下方显示错误消息"请选择人员"
3. WHEN 用户未选择哨位，THE System SHALL 在哨位字段下方显示错误消息"请选择哨位"
4. WHEN 用户未选择时段，THE System SHALL 在时段字段下方显示错误消息"请选择时段"
5. WHEN 选择的日期超出排班日期范围，THE System SHALL 显示错误消息"日期必须在排班开始日期和结束日期之间"
6. THE System SHALL 在所有验证错误修正后才允许提交表单

### Requirement 10

**User Story:** 作为排班管理员，我希望手动指定列表能够清晰地显示每条指定的关键信息，以便快速浏览和管理

#### Acceptance Criteria

1. THE System SHALL 在手动指定卡片中显示日期（格式：yyyy-MM-dd）
2. THE System SHALL 在手动指定卡片中显示人员姓名
3. THE System SHALL 在手动指定卡片中显示哨位名称
4. THE System SHALL 在手动指定卡片中显示时段序号和时间范围（例如："时段 3 (06:00-08:00)"）
5. THE System SHALL 在手动指定卡片中显示描述（如果有）
6. THE System SHALL 在手动指定卡片中显示启用状态开关
7. THE System SHALL 使用卡片布局展示每条手动指定，确保信息层次清晰
