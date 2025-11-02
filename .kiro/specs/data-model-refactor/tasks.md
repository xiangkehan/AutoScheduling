# 数据模型重构实施计划

- [x] 1. 更新核心数据模型





  - [x] 1.1 更新Personal模型类


    - 从Personal类中移除PositionId字段
    - 更新相关属性和方法
    - _需求: 1.1_

  - [x] 1.2 更新PositionLocation模型类


    - 在PositionLocation类中添加AvailablePersonnelIds字段
    - 更新ToString方法以显示可用人员数量
    - _需求: 1.2, 1.3_
- [x] 2. 更新数据访问层




- [ ] 2. 更新数据访问层

  - [x] 2.1 更新PersonalRepository


    - 移除PositionId相关的数据库操作
    - 更新CreateAsync和UpdateAsync方法
    - 更新MapPerson方法
    - _需求: 2.1_

  - [x] 2.2 更新PositionLocationRepository


    - 添加AvailablePersonnelIds相关的数据库操作
    - 实现AddAvailablePersonnelAsync方法
    - 实现RemoveAvailablePersonnelAsync方法
    - 实现UpdateAvailablePersonnelAsync方法
    - 实现GetAvailablePersonnelIdsAsync方法
    - 实现GetPositionsByPersonnelAsync方法
    - 更新MapPosition方法
    - _需求: 2.2_

  - [x] 2.3 更新Repository接口


    - 更新IPersonalRepository接口
    - 更新IPositionRepository接口
    - _需求: 2.4_


- [-] 3. 更新数据传输对象和映射器


  - [ ] 3.1 更新PersonnelDto
    - 移除PositionId和PositionName字段
    - 添加AvailablePositionIds和AvailablePositionNames字段
    - _需求: 5.1_

  - [ ] 3.2 更新PositionDto
    - 添加AvailablePersonnelIds和AvailablePersonnelNames字段
    - _需求: 5.2_

  - [ ] 3.3 更新映射器类
    - 更新PersonnelMapper以适配新的DTO结构
    - 更新PositionMapper以适配新的DTO结构
    - _需求: 2.3_

- [ ] 4. 更新业务逻辑层
  - [ ] 4.1 更新PersonnelService
    - 移除职位相关的验证和业务逻辑
    - 更新CreateAsync方法
    - 添加GetAvailablePositionsAsync方法
    - _需求: 3.1_

  - [ ] 4.2 更新PositionService
    - 添加AddAvailablePersonnelAsync方法
    - 添加RemoveAvailablePersonnelAsync方法
    - 添加GetAvailablePersonnelAsync方法
    - 实现ValidatePersonnelSkillsAsync方法
    - _需求: 3.2_

  - [ ] 4.3 更新SchedulingService
    - 使用新数据模型构建排班请求
    - 验证哨位可用人员数据完整性
    - _需求: 3.3_

  - [ ] 4.4 更新服务接口
    - 更新IPersonnelService接口
    - 更新IPositionService接口
    - _需求: 3.4_

- [ ] 5. 更新排班引擎
  - [ ] 5.1 更新GreedyScheduler
    - 修改InitializeFeasibilityTensorAsync方法使用新数据模型
    - 更新SelectBestPersonnel方法
    - 优化人员筛选逻辑
    - _需求: 4.1_

  - [ ] 5.2 更新ConstraintValidator
    - 适配新的数据模型结构
    - 更新约束验证逻辑
    - _需求: 4.2_

  - [ ] 5.3 更新FeasibilityTensor初始化
    - 修改初始化逻辑以使用哨位的可用人员列表
    - _需求: 4.3_

- [ ] 6. 更新用户界面层
  - [ ] 6.1 更新PersonnelPage相关代码
    - 移除职位选择相关的UI元素
    - 添加可用哨位显示功能
    - _需求: 5.3_

  - [ ] 6.2 更新PositionPage相关代码
    - 添加可用人员管理功能
    - 实现人员添加/移除界面
    - _需求: 5.3_

  - [ ] 6.3 更新ViewModel类
    - 适配新的DTO结构
    - 更新数据绑定逻辑
    - _需求: 5.3, 5.4_

- [ ] 7. 验证系统功能
  - [ ] 7.1 测试核心功能
    - 测试所有核心功能正常运行
    - 验证排班算法性能
    - _需求: 7.1, 7.3_

  - [ ] 7.2 验证数据完整性
    - 验证人员-哨位关系一致性
    - 检查数据模型变更正确性
    - _需求: 1.4, 7.1_

- [ ] 8. 更新测试代码
  - [ ] 8.1 更新Repository测试
    - 适配新的数据模型测试
    - 测试新增的人员管理方法
    - _需求: 6.1_

  - [ ] 8.2 更新Service测试
    - 验证新的业务逻辑
    - 测试人员-哨位关系管理
    - _需求: 6.2_

  - [ ] 8.3 更新排班引擎测试
    - 验证算法正确性
    - 性能基准测试
    - _需求: 6.3_