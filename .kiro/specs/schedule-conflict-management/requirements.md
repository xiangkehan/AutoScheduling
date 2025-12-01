# 需求文档

## 简介

本功能旨在完善排班结果页面的冲突检测和展示系统，为用户提供全面的冲突管理能力。系统将自动检测多种类型的排班冲突，提供详细的冲突信息，并支持冲突定位、修复和报告导出功能，帮助用户快速识别和解决排班问题。

## 术语表

- **System**：AutoScheduling3 智能排班系统
- **User**：使用排班系统的操作人员
- **Conflict**：冲突，指违反硬约束或软约束的排班分配
- **Hard Constraint Conflict**：硬约束冲突，必须解决的冲突（如技能不匹配、人员不可用）
- **Soft Constraint Conflict**：软约束冲突，建议解决的冲突（如工作量不均、休息时间不足）
- **Conflict Panel**：冲突面板，显示所有检测到的冲突的UI组件
- **Conflict Detection**：冲突检测，自动扫描排班结果并识别冲突的过程
- **Conflict Locating**：冲突定位，在排班表格中高亮显示冲突相关的单元格
- **Conflict Resolution**：冲突修复，提供解决冲突的建议或自动修复方案
- **Unassigned Slot**：未分配时段，指某个哨位在某个时段没有分配人员
- **Personnel**：人员，可以被分配到哨位的工作人员
- **Position**：哨位，需要人员值守的岗位
- **Time Slot**：时段，一个2小时的时间段（0-11）
- **Shift**：班次，一个具体的人员-哨位-时段分配

## 需求

### 需求 1

**用户故事：** 作为排班管理员，我希望系统能够自动检测多种类型的排班冲突，以便全面了解排班方案的问题

#### 验收标准

1. WHEN THE System 加载排班结果, THE System SHALL 自动执行冲突检测
2. WHEN THE System 执行冲突检测, THE System SHALL 检测技能不匹配冲突
3. WHEN THE System 执行冲突检测, THE System SHALL 检测人员不可用冲突
4. WHEN THE System 执行冲突检测, THE System SHALL 检测连续工作超时冲突
5. WHEN THE System 执行冲突检测, THE System SHALL 检测休息时间不足冲突
6. WHEN THE System 执行冲突检测, THE System SHALL 检测工作量不均衡冲突
7. WHEN THE System 执行冲突检测, THE System SHALL 检测未分配时段
8. WHEN THE System 执行冲突检测, THE System SHALL 检测重复分配冲突
9. WHEN THE System 完成冲突检测, THE System SHALL 将冲突按类型和严重程度分类

### 需求 2

**用户故事：** 作为排班管理员，我希望能够查看详细的冲突信息，以便准确理解每个冲突的具体情况

#### 验收标准

1. WHEN THE User 打开冲突面板, THE System SHALL 显示冲突统计信息，包含硬约束冲突数、软约束冲突数、未分配时段数
2. WHEN THE User 查看冲突列表, THE System SHALL 为每个冲突显示冲突类型
3. WHEN THE User 查看冲突列表, THE System SHALL 为每个冲突显示冲突描述
4. WHEN THE User 查看冲突列表, THE System SHALL 为每个冲突显示相关人员姓名和ID
5. WHEN THE User 查看冲突列表, THE System SHALL 为每个冲突显示相关哨位名称和ID
6. WHEN THE User 查看冲突列表, THE System SHALL 为每个冲突显示相关日期和时段
7. WHERE THE 冲突涉及多个班次, THE System SHALL 显示所有相关班次的信息
8. WHEN THE User 查看冲突列表, THE System SHALL 按冲突类型分组显示

### 需求 3

**用户故事：** 作为排班管理员，我希望能够快速定位冲突在排班表中的位置，以便直观地查看冲突上下文

#### 验收标准

1. WHEN THE User 点击冲突项的"定位"按钮, THE System SHALL 在排班表格中高亮显示相关单元格
2. WHERE THE 冲突涉及单个班次, THE System SHALL 高亮显示该班次对应的单元格
3. WHERE THE 冲突涉及多个班次, THE System SHALL 高亮显示所有相关单元格
4. WHEN THE System 高亮单元格, THE System SHALL 自动滚动表格使高亮单元格可见
5. WHEN THE System 高亮单元格, THE System SHALL 使用明显的视觉效果（如边框颜色、背景色）
6. WHEN THE User 点击另一个冲突项, THE System SHALL 清除之前的高亮并高亮新的单元格
7. WHEN THE User 关闭冲突面板, THE System SHALL 清除所有冲突高亮

### 需求 4

**用户故事：** 作为排班管理员，我希望系统能够提供冲突修复建议，以便快速解决排班问题

#### 验收标准

1. WHERE THE 冲突是技能不匹配, THE System SHALL 提供"推荐替换人员"修复建议
2. WHERE THE 冲突是工作量不均衡, THE System SHALL 提供"重新分配班次"修复建议
3. WHERE THE 冲突是未分配时段, THE System SHALL 提供"推荐可用人员"修复建议
4. WHERE THE 冲突是休息时间不足, THE System SHALL 提供"调整班次时间"修复建议
5. WHEN THE User 点击"修复"按钮, THE System SHALL 显示修复建议对话框
6. WHERE THE 修复建议对话框显示, THE System SHALL 列出所有可行的修复方案
7. WHERE THE 修复建议对话框显示, THE System SHALL 为每个方案显示预期效果和影响
8. WHEN THE User 选择修复方案并确认, THE System SHALL 应用修复方案并更新排班结果
9. WHEN THE System 应用修复方案, THE System SHALL 重新执行冲突检测并更新冲突列表

### 需求 5

**用户故事：** 作为排班管理员，我希望能够忽略某些不重要的冲突，以便专注于关键问题

#### 验收标准

1. WHEN THE User 点击冲突项的"忽略"按钮, THE System SHALL 将该冲突标记为已忽略
2. WHERE THE 冲突被标记为已忽略, THE System SHALL 在冲突列表中以不同样式显示
3. WHERE THE 冲突被标记为已忽略, THE System SHALL 不计入冲突统计数量
4. WHEN THE User 点击"全部忽略"按钮, THE System SHALL 将所有软约束冲突标记为已忽略
5. WHEN THE User 点击已忽略的冲突项, THE System SHALL 提供"取消忽略"操作
6. WHERE THE 硬约束冲突, THE System SHALL 不允许忽略操作
7. WHEN THE System 保存排班结果, THE System SHALL 保存冲突的忽略状态

### 需求 6

**用户故事：** 作为排班管理员，我希望冲突面板能够实时更新，以便在修改排班后立即看到冲突变化

#### 验收标准

1. WHEN THE User 修改班次分配, THE System SHALL 自动重新执行冲突检测
2. WHEN THE System 检测到新的冲突, THE System SHALL 立即更新冲突面板
3. WHEN THE System 检测到冲突已解决, THE System SHALL 从冲突列表中移除该冲突
4. WHEN THE 冲突数量变化, THE System SHALL 更新冲突统计信息
5. WHERE THE 冲突面板打开, THE System SHALL 在检测到冲突变化时显示更新动画
6. WHEN THE User 撤销修改, THE System SHALL 恢复之前的冲突状态

### 需求 7

**用户故事：** 作为排班管理员，我希望能够筛选和排序冲突列表，以便快速找到特定类型的冲突

#### 验收标准

1. WHEN THE User 在冲突面板, THE System SHALL 提供冲突类型筛选器
2. WHEN THE User 选择冲突类型, THE System SHALL 仅显示该类型的冲突
3. WHEN THE User 在冲突面板, THE System SHALL 提供严重程度筛选器
4. WHEN THE User 选择严重程度, THE System SHALL 仅显示对应严重程度的冲突
5. WHEN THE User 在冲突面板, THE System SHALL 提供排序选项（按类型、按日期、按严重程度）
6. WHEN THE User 选择排序方式, THE System SHALL 按选定方式重新排列冲突列表
7. WHEN THE User 在冲突面板, THE System SHALL 提供搜索框，支持按人员或哨位名称搜索冲突

### 需求 8

**用户故事：** 作为排班管理员，我希望在排班表格中直观地看到哪些单元格有冲突，以便快速识别问题区域

#### 验收标准

1. WHERE THE 单元格对应的班次有硬约束冲突, THE System SHALL 在单元格上显示红色警告图标
2. WHERE THE 单元格对应的班次有软约束冲突, THE System SHALL 在单元格上显示黄色警告图标
3. WHERE THE 单元格未分配, THE System SHALL 在单元格上显示灰色提示图标
4. WHEN THE User 将鼠标悬停在有冲突的单元格上, THE System SHALL 在工具提示中显示冲突摘要
5. WHERE THE 工具提示显示冲突摘要, THE System SHALL 包含冲突类型和简短描述
6. WHEN THE User 点击有冲突的单元格, THE System SHALL 在详情对话框中显示完整的冲突信息
7. WHERE THE 详情对话框显示冲突信息, THE System SHALL 提供"在冲突面板中查看"链接


