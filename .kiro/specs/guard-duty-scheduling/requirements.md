# Requirements Document

## Introduction

哨位排班系统是一个自动化的人员调度管理系统，用于为多个哨位分配合适的人员进行值班。系统需要考虑人员技能、可用性、历史排班记录以及各种约束条件，通过贪心算法等调度策略生成最优的排班方案。

## Glossary

- **Guard_Duty_Scheduling_System**: 完整的自动化排班管理系统
- **Position**: 需要人员值守的具体位置或岗位
- **Personnel**: 可以被分配到哨位值班的工作人员
- **Skill**: 人员具备的专业能力或资质
- **Schedule**: 包含多个单次排班的完整调度方案
- **Single_Shift**: 特定哨位在特定时间段的人员分配
- **Hard_Constraint**: 必须满足的调度规则
- **Soft_Constraint**: 用于优化调度质量的评分规则
- **Feasibility_Tensor**: 三维二进制数组，表示哨位-时段-人员分配的可行性
- **History_Buffer**: 临时存储未确认排班结果的数据区域
- **Confirmed_History**: 存储已确认实施的排班记录的数据区域

## Requirements

### Requirement 1

**User Story:** 作为排班管理员，我希望能够管理哨位信息，以便为每个哨位配置正确的要求和描述。

#### Acceptance Criteria

1. THE Guard_Duty_Scheduling_System SHALL 存储哨位名称、地点、介绍、要求和数据库ID
2. THE Guard_Duty_Scheduling_System SHALL 通过SQLite数据库持久化哨位数据
3. THE Guard_Duty_Scheduling_System SHALL 提供哨位信息的创建、读取、更新和删除功能
4. THE Guard_Duty_Scheduling_System SHALL 验证哨位数据的完整性和有效性

### Requirement 2

**User Story:** 作为排班管理员，我希望能够管理人员信息和技能，以便系统能够根据人员能力进行合理分配。

#### Acceptance Criteria

1. THE Guard_Duty_Scheduling_System SHALL 存储人员的数据库ID、姓名、职位、技能集合、可用性状态、退役状态
2. THE Guard_Duty_Scheduling_System SHALL 记录人员的最近班次间隔数、最近节假日班次间隔数和12个时段的班次间隔数
3. THE Guard_Duty_Scheduling_System SHALL 管理技能数据，包括技能ID、名称和描述
4. THE Guard_Duty_Scheduling_System SHALL 建立人员与技能的多对多关联关系

### Requirement 3

**User Story:** 作为排班管理员，我希望系统能够生成排班表，以便为所有哨位安排合适的人员值班。

#### Acceptance Criteria

1. THE Guard_Duty_Scheduling_System SHALL 创建包含数据库ID、人员组、哨位ID和表头的排班表
2. THE Guard_Duty_Scheduling_System SHALL 生成单次排班记录，包含哨位ID、班次时间和人员ID
3. THE Guard_Duty_Scheduling_System SHALL 将排班结果存储为单次排班的集合
4. THE Guard_Duty_Scheduling_System SHALL 支持多种排班策略，包括贪心算法

### Requirement 4

**User Story:** 作为排班管理员，我希望系统能够管理排班历史，以便跟踪和确认排班实施情况。

#### Acceptance Criteria

1. THE Guard_Duty_Scheduling_System SHALL 在SQLite数据库中记录所有排班历史
2. THE Guard_Duty_Scheduling_System SHALL 维护确定历史区和历史缓冲区的分离存储
3. WHEN 用户生成排班结果时，THE Guard_Duty_Scheduling_System SHALL 将结果存储到历史缓冲区
4. WHEN 用户确认实施排班时，THE Guard_Duty_Scheduling_System SHALL 将缓冲区数据转移到确定历史区并清空缓冲区

### Requirement 5

**User Story:** 作为排班管理员，我希望系统能够应用硬约束条件，以便确保排班方案的合规性和可行性。

#### Acceptance Criteria

1. THE Guard_Duty_Scheduling_System SHALL 确保一个人同一个晚上只能上一个夜哨
2. THE Guard_Duty_Scheduling_System SHALL 防止一个人在相邻时段连续上哨
3. THE Guard_Duty_Scheduling_System SHALL 仅为可用状态的人员分配哨位
4. WHERE 存在定岗要求时，THE Guard_Duty_Scheduling_System SHALL 将人员限制在指定哨位或时段
5. THE Guard_Duty_Scheduling_System SHALL 验证人员技能与哨位要求的匹配性
6. THE Guard_Duty_Scheduling_System SHALL 确保每个哨位每个时段仅分配一人
7. THE Guard_Duty_Scheduling_System SHALL 确保一个人在一个时段仅在一个哨位值班
8. WHERE 存在手动指定时，THE Guard_Duty_Scheduling_System SHALL 按照指定分配执行

### Requirement 6

**User Story:** 作为排班管理员，我希望系统能够应用软约束评分，以便优化排班质量和人员工作负荷平衡。

#### Acceptance Criteria

1. THE Guard_Duty_Scheduling_System SHALL 计算充分休息得分，基于人员距离上次分配的时段间隔
2. WHEN 处理休息日排班时，THE Guard_Duty_Scheduling_System SHALL 计算休息日平衡得分
3. THE Guard_Duty_Scheduling_System SHALL 计算各时段平衡得分，基于人员在12个时段的分配历史
4. THE Guard_Duty_Scheduling_System SHALL 根据软约束得分选择最优人员分配

### Requirement 7

**User Story:** 作为排班管理员，我希望系统能够高效处理大规模排班计算，以便在合理时间内生成排班方案。

#### Acceptance Criteria

1. THE Guard_Duty_Scheduling_System SHALL 使用三维可行张量表示哨位-时段-人员的分配可行性
2. THE Guard_Duty_Scheduling_System SHALL 使用二进制存储优化可行张量的内存使用
3. THE Guard_Duty_Scheduling_System SHALL 利用MathNet.Numerics库加速数值计算
4. THE Guard_Duty_Scheduling_System SHALL 通过逐位与运算处理约束条件
5. THE Guard_Duty_Scheduling_System SHALL 按照先哨位再时段的顺序进行人员分配