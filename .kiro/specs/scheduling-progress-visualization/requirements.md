# 需求文档

## 简介

本功能旨在为用户提供一个清晰、直观的页面，用于展示排班算法的执行过程和最终结果。当用户在创建排班页面点击"开始排班"按钮后，系统将导航到此页面，实时显示排班进度、算法执行状态、以及最终的排班结果和统计信息。

## 术语表

- **System**：AutoScheduling3 智能排班系统
- **User**：使用排班系统的操作人员
- **Scheduling Process**：排班算法的执行过程，包括初始化、约束应用、贪心选择等阶段
- **Scheduling Result**：排班算法执行完成后生成的排班方案
- **Progress Indicator**：进度指示器，显示当前排班执行的百分比
- **Algorithm Stage**：算法阶段，如"初始化可行性张量"、"应用硬约束"、"贪心选择分配"等
- **Assignment**：分配，指将某个人员分配到某个哨位的某个时段
- **Conflict**：冲突，指违反约束的分配情况
- **Statistics**：统计信息，包括总分配数、人员工作量分布、哨位覆盖率等

## 需求

### 需求 1

**用户故事：** 作为排班管理员，我希望在点击"开始排班"后能看到实时的排班进度，以便了解算法执行状态和预估完成时间

#### 验收标准

1. WHEN THE User 点击"开始排班"按钮, THE System SHALL 导航到排班进度可视化页面
2. WHILE THE Scheduling Process 正在执行, THE System SHALL 显示当前执行进度的百分比
3. WHILE THE Scheduling Process 正在执行, THE System SHALL 显示当前执行的 Algorithm Stage 名称和描述
4. WHILE THE Scheduling Process 正在执行, THE System SHALL 每秒至少更新一次进度信息
5. WHEN THE Scheduling Process 完成一个 Algorithm Stage, THE System SHALL 在界面上标记该阶段为已完成状态

### 需求 2

**用户故事：** 作为排班管理员，我希望看到排班过程中的关键信息，以便监控算法是否正常工作

#### 验收标准

1. WHILE THE Scheduling Process 正在执行, THE System SHALL 显示已完成的 Assignment 数量
2. WHILE THE Scheduling Process 正在执行, THE System SHALL 显示待分配的哨位-时段组合数量
3. WHILE THE Scheduling Process 正在执行, THE System SHALL 显示当前正在处理的哨位名称和时段信息
4. IF THE Scheduling Process 遇到无法分配的情况, THEN THE System SHALL 显示警告信息并记录冲突详情
5. WHILE THE Scheduling Process 正在执行, THE System SHALL 显示算法执行的累计时间

### 需求 3

**用户故事：** 作为排班管理员，我希望在排班完成后能看到详细的结果和统计信息，以便评估排班方案的质量

#### 验收标准

1. WHEN THE Scheduling Process 成功完成, THE System SHALL 显示"排班成功"的状态消息
2. WHEN THE Scheduling Process 成功完成, THE System SHALL 显示总 Assignment 数量
3. WHEN THE Scheduling Process 成功完成, THE System SHALL 显示每个人员的工作量统计（总时段数、日哨数、夜哨数）
4. WHEN THE Scheduling Process 成功完成, THE System SHALL 显示每个哨位的覆盖率（已分配时段数/总时段数）
5. WHEN THE Scheduling Process 成功完成, THE System SHALL 显示软约束评分的总分和各项得分明细

### 需求 4

**用户故事：** 作为排班管理员，我希望在排班失败时能看到清晰的错误信息和失败原因，以便调整约束配置后重新排班

#### 验收标准

1. WHEN THE Scheduling Process 失败, THE System SHALL 显示"排班失败"的状态消息
2. WHEN THE Scheduling Process 失败, THE System SHALL 显示失败的具体原因（如"无可行解"、"约束冲突"等）
3. WHEN THE Scheduling Process 失败, THE System SHALL 显示导致失败的 Conflict 列表，包括冲突的哨位、时段和约束类型
4. WHEN THE Scheduling Process 失败, THE System SHALL 提供"返回修改"按钮，允许 User 返回配置页面调整参数
5. WHEN THE Scheduling Process 失败, THE System SHALL 显示已完成的部分 Assignment 数量和进度百分比

### 需求 5

**用户故事：** 作为排班管理员，我希望能够查看排班结果的详细网格视图，以便直观地检查每个哨位每个时段的人员分配

#### 验收标准

1. WHEN THE Scheduling Process 成功完成, THE System SHALL 提供"查看详细结果"按钮
2. WHEN THE User 点击"查看详细结果"按钮, THE System SHALL 显示排班网格，行为日期，列为哨位，单元格显示分配的人员姓名
3. WHERE THE 排班网格显示, THE System SHALL 支持按日期范围筛选显示的数据
4. WHERE THE 排班网格显示, THE System SHALL 支持按哨位筛选显示的数据
5. WHERE THE 排班网格显示, THE System SHALL 为每个 Assignment 单元格提供悬停提示，显示该人员的技能和当前工作量

### 需求 6

**用户故事：** 作为排班管理员，我希望能够保存或放弃排班结果，以便决定是否采用当前方案

#### 验收标准

1. WHEN THE Scheduling Process 成功完成, THE System SHALL 提供"保存排班"按钮
2. WHEN THE User 点击"保存排班"按钮, THE System SHALL 将排班结果保存到数据库并导航到排班结果页面
3. WHEN THE Scheduling Process 成功完成, THE System SHALL 提供"放弃排班"按钮
4. WHEN THE User 点击"放弃排班"按钮, THE System SHALL 显示确认对话框询问是否确认放弃
5. WHEN THE User 确认放弃排班, THE System SHALL 清除当前排班数据并返回到创建排班页面

### 需求 7

**用户故事：** 作为排班管理员，我希望在排班过程中能够取消正在执行的排班任务，以便在发现配置错误时及时停止

#### 验收标准

1. WHILE THE Scheduling Process 正在执行, THE System SHALL 提供"取消排班"按钮
2. WHEN THE User 点击"取消排班"按钮, THE System SHALL 显示确认对话框询问是否确认取消
3. WHEN THE User 确认取消排班, THE System SHALL 停止 Scheduling Process 的执行
4. WHEN THE Scheduling Process 被取消, THE System SHALL 显示"排班已取消"的状态消息
5. WHEN THE Scheduling Process 被取消, THE System SHALL 提供"返回"按钮，允许 User 返回到创建排班页面
