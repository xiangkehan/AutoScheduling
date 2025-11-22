# 需求文档

## 简介

本功能旨在重构排班创建流程的步骤顺序，将步骤2改为选择哨位，步骤3改为选择参与人员。实现自动从已选哨位的可用人员集合中提取参与人员，并提供基于哨位的人员管理界面。用户可以查看每个哨位的可用人员，进行临时性修改（仅在本次排班中生效），也可以选择将修改永久保存到数据库。

## 术语表

- **System**: 排班创建向导系统
- **User**: 使用排班系统的用户
- **Position**: 哨位/岗位，需要人员值守的位置
- **Personnel**: 人员，可以被分配到哨位的工作人员
- **Available Personnel**: 可用人员，具备哨位所需技能且可以被分配的人员
- **Temporary Change**: 临时性更改，仅在当前排班创建流程中生效的修改
- **Permanent Change**: 永久性更改，保存到数据库并影响后续所有排班的修改
- **Step 2**: 排班创建向导的第2步，用于选择参与哨位
- **Step 3**: 排班创建向导的第3步，用于选择参与人员

## 需求

### 需求 1: 自动提取参与人员

**用户故事:** 作为排班管理员，我希望在选择哨位后系统自动提取所有可用人员，这样我就不需要手动逐个选择人员。

#### 验收标准

1. WHEN User 完成步骤2（选择哨位）并进入步骤3时，THE System SHALL 自动从所有已选哨位的AvailablePersonnelIds集合中提取唯一的人员列表
2. WHEN User 在步骤2中添加新哨位时，THE System SHALL 自动将该哨位的可用人员添加到参与人员列表中
3. WHEN User 在步骤2中移除哨位时，THE System SHALL 自动从参与人员列表中移除仅属于该哨位的人员
4. THE System SHALL 确保参与人员列表中不包含重复的人员ID
5. THE System SHALL 允许用户从AvailablePersonnels中手动添加不在任何已选哨位AvailablePersonnelIds列表中的人员
6. THE System SHALL 在步骤3的界面上明确标识哪些人员是自动提取的，哪些是手动添加的

### 需求 2: 基于哨位的人员展示

**用户故事:** 作为排班管理员，我希望能够查看每个哨位都有哪些可用人员，这样我就能更好地了解人员分布情况。

#### 验收标准

1. THE System SHALL 在步骤3中展示所有已选哨位的列表
2. THE System SHALL 为每个哨位展示其AvailablePersonnelIds对应的人员列表
3. THE System SHALL 显示每个哨位的AvailablePersonnelCount属性值
4. THE System SHALL 使用可展开的UI控件（如Expander）显示每个哨位的人员列表
5. THE System SHALL 使用不同的视觉样式标识被多个哨位共享的人员

### 需求 3: 为指定哨位临时调整人员

**用户故事:** 作为排班管理员，我希望能够临时调整某个哨位的可用人员，这样我就可以在不影响数据库的情况下为该哨位进行本次排班的特殊安排。

#### 验收标准

1. THE System SHALL 允许用户在步骤3中为每个哨位从AvailablePersonnels临时添加人员到其AvailablePersonnelIds列表
2. THE System SHALL 允许用户在步骤3中为每个哨位临时移除人员从其AvailablePersonnelIds列表
3. THE System SHALL 使用视觉标识（如➕图标）标记临时添加到哨位的人员
4. THE System SHALL 使用视觉标识（如➖图标）标记临时从哨位移除的人员
5. WHEN User 完成排班创建流程时，THE System SHALL 丢弃所有临时性更改而不更新数据库
6. WHEN User 取消排班创建流程时，THE System SHALL 丢弃所有临时性更改而不更新数据库
7. THE System SHALL 在步骤5的SummarySections中展示所有临时性更改的摘要

### 需求 3.1: 手动添加参与人员（不属于任何哨位）

**用户故事:** 作为排班管理员，我希望能够添加不属于任何哨位的人员参与排班，这样我就可以灵活调配特殊角色的人员。

#### 验收标准

1. THE System SHALL 允许用户在步骤3中从AvailablePersonnels添加不在任何已选哨位AvailablePersonnelIds列表中的人员
2. THE System SHALL 在手动添加人员对话框中只显示不在任何已选哨位可用人员列表中的人员
3. THE System SHALL 将手动添加的人员存储在ManuallyAddedPersonnelIds列表中
4. THE System SHALL 在"手动添加的人员"区域单独显示这些人员
5. THE System SHALL 使用视觉标识明确区分手动添加的人员和从哨位提取的人员
6. THE System SHALL 在排班算法中允许手动添加的人员被分配到任何哨位

### 需求 4: 永久性人员调整

**用户故事:** 作为排班管理员，我希望能够将临时性的人员调整保存为永久性的，这样后续的排班就不需要重复进行相同的调整。

#### 验收标准

1. THE System SHALL 为每个临时性更改提供"保存为永久"的按钮
2. WHEN User 点击"保存为永久"按钮时，THE System SHALL 调用IPositionService.UpdateAsync方法更新对应哨位的AvailablePersonnelIds
3. THE System SHALL 在保存前使用ContentDialog显示确认对话框，列出将要进行的永久性更改
4. WHEN 保存成功时，THE System SHALL 使用DialogService.ShowSuccessAsync显示成功提示
5. IF 保存失败，THEN THE System SHALL 使用DialogService.ShowErrorAsync显示错误信息并保留临时性更改状态

### 需求 5: 步骤顺序调整

**用户故事:** 作为排班管理员，我希望先选择哨位再选择人员，这样系统就能自动为我提取可用人员。

#### 验收标准

1. THE System SHALL 将步骤2和步骤3的内容互换（步骤2改为选择哨位，步骤3改为选择人员）
2. THE System SHALL 在步骤2中提供哨位选择界面（原步骤3的界面）
3. THE System SHALL 在步骤3中提供基于哨位的人员管理界面（重构后的新界面）
4. THE System SHALL 允许用户在步骤3中返回步骤2修改哨位选择
5. WHEN User 在步骤2和步骤3之间切换时，THE System SHALL 保持已做的选择和更改

### 需求 6: 向后兼容性

**用户故事:** 作为系统维护者，我希望重构后的功能不影响其他模块的正常运行，这样就能确保系统的稳定性。

#### 验收标准

1. THE System SHALL 保持PersonnelDto和PositionDto的数据结构不变
2. THE System SHALL 保持IPersonnelService和IPositionService的接口不变
3. THE System SHALL 确保LoadTemplateCommand功能正常工作
4. THE System SHALL 确保ISchedulingDraftService的保存和恢复功能正常工作
5. THE System SHALL 确保ManualAssignmentManager功能正常工作
6. THE System SHALL 确保LoadConstraintsCommand功能正常工作
7. THE System SHALL 确保切换页面后保存进度功能正常工作

### 需求 7: 性能要求

**用户故事:** 作为排班管理员，我希望人员提取和界面更新能够快速响应，这样我就能高效地完成排班配置。

#### 验收标准

1. WHEN SelectedPositions.Count小于等于50时，THE System SHALL 在500毫秒内完成人员提取
2. WHEN SelectedPositions.Count大于50时，THE System SHALL 在2秒内完成人员提取
3. THE System SHALL 在用户进行临时性更改时提供即时的UI反馈（不超过100毫秒）
4. THE System SHALL 使用ItemsRepeater或ListView的虚拟化功能渲染超过100人的列表
5. THE System SHALL 在CurrentStep属性变化时避免重复调用LoadDataCommand

### 需求 8: 用户体验

**用户故事:** 作为排班管理员，我希望界面清晰易用，这样我就能快速理解和操作。

#### 验收标准

1. THE System SHALL 使用WinUI 3的卡片样式（CardBackgroundFillColorDefaultBrush）区分哨位和人员
2. THE System SHALL 使用SymbolIcon和颜色标识临时性更改
3. THE System SHALL 在用户点击"保存为永久"按钮前使用ContentDialog提供确认提示
4. THE System SHALL 为每个临时性更改提供撤销按钮
5. THE System SHALL 使用InfoBar控件提供操作说明或提示信息
