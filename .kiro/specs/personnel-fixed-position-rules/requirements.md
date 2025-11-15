# Requirements Document

## Introduction

本需求文档定义了在人员管理页面的人员详情区域中添加定岗规则管理功能。定岗规则（Fixed Position Rule）是自动排班系统中的核心约束机制，用于定义特定人员仅能在指定哨位或时段上哨的业务规则。该功能将允许用户在查看人员详情时，直接为该人员创建、查看、编辑和删除定岗规则，提升用户体验和操作效率。

## Glossary

- **System**: 指自动排班系统（AutoScheduling3）
- **PersonnelDetailPanel**: 人员详情面板，位于人员管理页面右侧，显示选中人员的详细信息
- **FixedPositionRule**: 定岗规则，定义人员仅能在特定哨位或时段上哨的约束
- **AllowedPositionIds**: 允许的哨位ID列表，空列表表示不限制哨位
- **AllowedPeriods**: 允许的时段序号列表（0-11），空列表表示不限制时段
- **ConstraintService**: 约束服务，负责定岗规则的业务逻辑处理
- **RuleList**: 规则列表，显示当前人员的所有定岗规则
- **RuleForm**: 规则表单，用于创建或编辑定岗规则

## Requirements

### Requirement 1

**User Story:** 作为排班管理员，我希望在人员详情面板中查看该人员的所有定岗规则，以便了解该人员的排班约束情况

#### Acceptance Criteria

1. WHEN 用户在人员管理页面选中一个人员，THE System SHALL 在人员详情面板中显示"定岗规则"区域
2. WHEN 人员详情面板加载完成，THE System SHALL 自动查询并显示该人员的所有定岗规则
3. THE System SHALL 在规则列表中显示每条规则的关键信息，包括允许的哨位名称、允许的时段、启用状态和规则描述
4. WHEN 该人员没有定岗规则，THE System SHALL 显示"暂无定岗规则"的空状态提示
5. THE System SHALL 在规则列表中以卡片形式展示每条规则，每个卡片包含规则的完整信息

### Requirement 2

**User Story:** 作为排班管理员，我希望为选中的人员创建新的定岗规则，以便限制该人员只能在特定哨位或时段上哨

#### Acceptance Criteria

1. THE System SHALL 在定岗规则区域提供"添加定岗规则"按钮
2. WHEN 用户点击"添加定岗规则"按钮，THE System SHALL 显示定岗规则创建表单
3. THE System SHALL 在创建表单中提供允许哨位的多选列表，列表包含系统中所有可用哨位
4. THE System SHALL 在创建表单中提供允许时段的多选列表，列表包含12个时段（0-11）
5. THE System SHALL 在创建表单中提供规则描述输入框和启用状态开关
6. WHEN 用户未选择任何哨位，THE System SHALL 将AllowedPositionIds设置为空列表，表示不限制哨位
7. WHEN 用户未选择任何时段，THE System SHALL 将AllowedPeriods设置为空列表，表示不限制时段
8. WHEN 用户提交创建表单，THE System SHALL 调用ConstraintService的CreateFixedPositionRuleAsync方法创建规则
9. WHEN 规则创建成功，THE System SHALL 刷新规则列表并关闭创建表单
10. IF 规则创建失败，THEN THE System SHALL 显示错误消息并保持表单打开状态

### Requirement 3

**User Story:** 作为排班管理员，我希望编辑现有的定岗规则，以便调整人员的排班约束

#### Acceptance Criteria

1. THE System SHALL 在每条规则卡片上提供"编辑"按钮
2. WHEN 用户点击规则的"编辑"按钮，THE System SHALL 显示规则编辑表单并预填充该规则的现有数据
3. THE System SHALL 在编辑表单中允许用户修改允许的哨位、允许的时段、规则描述和启用状态
4. WHEN 用户提交编辑表单，THE System SHALL 调用ConstraintService的UpdateFixedPositionRuleAsync方法更新规则
5. WHEN 规则更新成功，THE System SHALL 刷新规则列表并关闭编辑表单
6. THE System SHALL 提供"取消"按钮，允许用户放弃编辑并恢复原始数据
7. IF 规则更新失败，THEN THE System SHALL 显示错误消息并保持表单打开状态

### Requirement 4

**User Story:** 作为排班管理员，我希望删除不再需要的定岗规则，以便移除过时的排班约束

#### Acceptance Criteria

1. THE System SHALL 在每条规则卡片上提供"删除"按钮
2. WHEN 用户点击规则的"删除"按钮，THE System SHALL 显示确认对话框
3. THE System SHALL 在确认对话框中显示警告信息"确定要删除此定岗规则吗？此操作无法撤销。"
4. WHEN 用户确认删除，THE System SHALL 调用ConstraintService的DeleteFixedPositionRuleAsync方法删除规则
5. WHEN 规则删除成功，THE System SHALL 从规则列表中移除该规则
6. IF 规则删除失败，THEN THE System SHALL 显示错误消息

### Requirement 5

**User Story:** 作为排班管理员，我希望在创建或编辑定岗规则时看到清晰的时段标识，以便准确选择允许的时段

#### Acceptance Criteria

1. THE System SHALL 在时段选择列表中显示时段序号（0-11）和对应的时间范围
2. THE System SHALL 将时段0-11映射为具体时间，例如"0 (00:00-02:00)"、"1 (02:00-04:00)"等
3. THE System SHALL 允许用户通过复选框多选时段
4. THE System SHALL 在表单中显示已选择的时段数量

### Requirement 6

**User Story:** 作为排班管理员，我希望在创建或编辑定岗规则时看到清晰的哨位信息，以便准确选择允许的哨位

#### Acceptance Criteria

1. THE System SHALL 在哨位选择列表中显示哨位名称
2. THE System SHALL 允许用户通过复选框多选哨位
3. THE System SHALL 在表单中显示已选择的哨位数量
4. THE System SHALL 从PositionService获取所有可用哨位列表

### Requirement 7

**User Story:** 作为排班管理员，我希望定岗规则管理功能具有良好的响应式布局，以便在不同屏幕尺寸下都能正常使用

#### Acceptance Criteria

1. WHEN 屏幕宽度小于768像素，THE System SHALL 将定岗规则区域显示在人员详情下方
2. WHEN 屏幕宽度大于等于768像素，THE System SHALL 将定岗规则区域显示在人员详情面板内
3. THE System SHALL 确保规则列表在小屏幕上可垂直滚动
4. THE System SHALL 确保规则表单在小屏幕上保持可读性和可操作性
