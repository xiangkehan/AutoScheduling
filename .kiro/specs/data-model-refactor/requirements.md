# 数据模型重构需求文档

## 介绍

本规格定义了哨位排班系统中人员与哨位关系存储模式的重构需求。当前系统中人员模型存储职位ID，现需要改为哨位模型存储可用人员ID集合，以提高排班算法的查询效率和数据模型的合理性。

## 术语表

- **Guard_Duty_Scheduling_System**: 完整的自动化排班管理系统
- **Personnel**: 可以被分配到哨位值班的工作人员
- **Position**: 需要人员值守的具体位置或岗位
- **Personnel_Position_Relationship**: 人员与哨位之间的可用性关联关系
- **Backward_Compatibility**: 确保现有功能在数据模型变更后仍能正常工作

## 需求

### 需求 1

**用户故事:** 作为系统架构师，我希望重构人员与哨位的关系存储模式，以便提高排班算法的查询效率。

#### 验收标准

1. THE Guard_Duty_Scheduling_System SHALL 从Personnel模型中移除PositionId字段
2. THE Guard_Duty_Scheduling_System SHALL 在PositionLocation模型中添加AvailablePersonnelIds字段
3. THE Guard_Duty_Scheduling_System SHALL 确保AvailablePersonnelIds字段存储可用人员的ID集合
4. THE Guard_Duty_Scheduling_System SHALL 维护人员与哨位关系的数据完整性

### 需求 2

**用户故事:** 作为开发人员，我希望更新所有相关的数据访问层代码，以便适配新的数据模型。

#### 验收标准

1. THE Guard_Duty_Scheduling_System SHALL 更新PersonalRepository以移除PositionId相关操作
2. THE Guard_Duty_Scheduling_System SHALL 更新PositionLocationRepository以支持AvailablePersonnelIds操作
3. THE Guard_Duty_Scheduling_System SHALL 更新所有相关的DTO和Mapper类
4. THE Guard_Duty_Scheduling_System SHALL 确保Repository接口与实现保持一致

### 需求 3

**用户故事:** 作为业务逻辑开发者，我希望更新服务层代码，以便使用新的数据模型进行业务操作。

#### 验收标准

1. THE Guard_Duty_Scheduling_System SHALL 更新PersonnelService以移除职位相关的业务逻辑
2. THE Guard_Duty_Scheduling_System SHALL 更新PositionService以支持可用人员管理
3. THE Guard_Duty_Scheduling_System SHALL 更新SchedulingService以使用新的人员-哨位关系查询
4. THE Guard_Duty_Scheduling_System SHALL 确保所有服务接口与实现保持一致

### 需求 4

**用户故事:** 作为排班算法开发者，我希望更新排班引擎代码，以便利用新的数据模型提高查询效率。

#### 验收标准

1. THE Guard_Duty_Scheduling_System SHALL 更新GreedyScheduler以使用PositionLocation.AvailablePersonnelIds进行人员筛选
2. THE Guard_Duty_Scheduling_System SHALL 更新ConstraintValidator以适配新的数据模型
3. THE Guard_Duty_Scheduling_System SHALL 更新FeasibilityTensor初始化逻辑

### 需求 5

**用户故事:** 作为UI开发者，我希望更新用户界面相关代码，以便正确显示和操作新的数据关系。

#### 验收标准

1. THE Guard_Duty_Scheduling_System SHALL 更新PersonnelDto以移除PositionId字段
2. THE Guard_Duty_Scheduling_System SHALL 更新PositionDto以添加AvailablePersonnelIds字段
3. THE Guard_Duty_Scheduling_System SHALL 更新所有相关的ViewModel和View代码
4. THE Guard_Duty_Scheduling_System SHALL 确保UI功能在数据模型变更后正常工作

### 需求 6

**用户故事:** 作为测试工程师，我希望更新所有相关测试代码，以便验证新数据模型的正确性。

#### 验收标准

1. THE Guard_Duty_Scheduling_System SHALL 更新所有Repository测试以适配新的数据模型
2. THE Guard_Duty_Scheduling_System SHALL 更新所有Service测试以验证新的业务逻辑
3. THE Guard_Duty_Scheduling_System SHALL 更新排班引擎测试以验证算法正确性

### 需求 7

**用户故事:** 作为系统管理员，我希望确保数据模型重构不会影响系统的稳定性。

#### 验收标准

1. THE Guard_Duty_Scheduling_System SHALL 在重构后保持所有现有功能的正常运行
2. THE Guard_Duty_Scheduling_System SHALL 提供详细的重构文档和操作指南
3. THE Guard_Duty_Scheduling_System SHALL 通过所有现有的集成测试