# 实现计划

- [x] 1. 更新 TestDataConfiguration 添加新配置参数




  - 添加 MinPersonnelPerPosition 属性（默认值 3）
  - 添加 PersonnelAvailabilityRate 属性（默认值 0.85）
  - 添加 PersonnelRetirementRate 属性（默认值 0.10）
  - 更新 Validate() 方法，添加新配置参数的验证规则
  - 更新 ValidateWithResult() 方法，添加新配置参数的验证规则
  - 更新 CreateDefault()、CreateSmall()、CreateLarge() 方法，包含新配置参数
  - 添加 CreateDrillScenario() 和 CreateCombatScenario() 配置方法
  - _需求: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_

- [x] 2. 创建技能共现分析器





  - 创建 TestData/Helpers/SkillCooccurrenceAnalyzer.cs 文件
  - 实现 BuildCooccurrenceMatrix() 方法，构建技能共现矩阵
  - 实现 GetCooccurringSkills() 方法，获取高频共现技能
  - 添加必要的数据结构和辅助方法
  - _需求: 2.5_

- [x] 3. 创建智能技能分配器




  - 创建 TestData/Generators/SkillAssigner.cs 文件
  - 实现构造函数，初始化配置和随机数生成器
  - 实现 AssignSkills() 方法的主框架
  - 实现阶段1：为每个哨位分配基本人员
  - 实现阶段2：创建多技能人员（30-40%）
  - 实现补充技能逻辑（确保每个人员至少有1个技能）
  - 添加调试日志输出（每个哨位的可用人员数量、多技能人员比例）
  - _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8_


- [x] 4. 更新 PersonnelGenerator




  - 修改 Generate() 方法签名，移除技能列表依赖
  - 移除技能分配逻辑
  - 将 SkillIds 和 SkillNames 初始化为空列表
  - 使用 PersonnelAvailabilityRate 配置确定 IsAvailable 状态
  - 使用 PersonnelRetirementRate 配置确定 IsRetired 状态
  - 保留其他字段的生成逻辑不变
  - _需求: 3.1, 3.2, 3.3, 3.4, 5.9, 5.10_


- [x] 5. 更新 PositionGenerator




  - 修改 Generate() 方法签名，移除人员列表依赖
  - 移除可用人员计算逻辑
  - 将 AvailablePersonnelIds 和 AvailablePersonnelNames 初始化为空列表
  - 保留技能需求的生成逻辑不变
  - _需求: 4.1, 4.2, 4.3, 4.4_


- [x] 6. 更新 TestDataGenerator 主流程




  - 在构造函数中初始化 SkillAssigner 实例
  - 调整 GenerateTestData() 方法的生成顺序
  - 修改人员生成调用（不传递技能列表）
  - 修改哨位生成调用（不传递人员列表）
  - 在哨位生成后调用 SkillAssigner.AssignSkills()
  - 实现 UpdatePositionAvailablePersonnel() 私有方法
  - 在技能分配后调用 UpdatePositionAvailablePersonnel()
  - 更新调试日志输出
  - _需求: 1.1, 2.1, 5.8_

- [ ]* 7. 添加单元测试
  - 创建 TestData/Tests/SkillCooccurrenceAnalyzerTests.cs
  - 创建 TestData/Tests/SkillAssignerTests.cs
  - 测试技能共现矩阵构建
  - 测试高频共现技能识别
  - 测试基本技能分配功能
  - 测试多技能人员创建
  - 测试边界情况（人员不足、技能不足等）
  - _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_

- [ ]* 8. 更新集成测试
  - 更新 TestDataGeneratorTests.cs（如果存在）
  - 测试完整的生成流程
  - 验证每个哨位的可用人员数量是否满足 MinPersonnelPerPosition
  - 验证每个人员的技能数量是否在 1-3 之间
  - 验证哨位的可用人员是否符合技能要求
  - 测试不同配置场景（小型演练、大型实战）
  - _需求: 1.1, 2.2, 2.4, 2.6, 2.7_
-

- [x] 9. 更新文档和示例
  - 更新 TestData/README.md，说明新的生成流程和配置参数
  - 添加配置场景的使用示例
  - 添加技能共现策略的说明
  - 更新代码注释
  - _需求: 所有需求_

- [x] 10. 更新测试数据生成器 UI
  - 在 TestDataGeneratorPage.xaml 中添加质量控制参数配置
  - 添加 MinPersonnelPerPosition 配置（NumberBox，范围 1-20）
  - 添加 PersonnelAvailabilityRate 配置（Slider，范围 50%-100%）
  - 添加 PersonnelRetirementRate 配置（Slider，范围 0%-30%）
  - 优化 UI 布局，分为三个区域（基础数据量、质量控制、其他配置）
  - 在 TestDataGeneratorViewModel 中添加对应的属性和绑定
  - 添加实时百分比显示功能
  - 添加属性变化通知机制
  - 新增两个预设场景（演练场景、实战场景）
  - 更新 CreateConfiguration() 方法，包含新参数
  - 创建 UI-UPDATE.md 文档，详细说明 UI 更新内容
  - 更新 README.md，添加 UI 使用说明
  - _需求: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_
