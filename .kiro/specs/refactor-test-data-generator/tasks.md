# 实施计划

- [x] 1. 创建基础设施和辅助工具类





  - 创建 TestData/Generators 文件夹
  - 创建 TestData/Helpers 文件夹
  - 创建 TestData/Validation 文件夹
  - 创建 TestData/Export 文件夹
  - _需求: 6.1_

- [x] 1.1 实现 IEntityGenerator 接口


  - 在 TestData/Generators/IEntityGenerator.cs 中定义泛型接口
  - 接口包含 Generate() 方法
  - _需求: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7_

- [x] 1.2 实现 UniqueNameGenerator 类


  - 在 TestData/Helpers/UniqueNameGenerator.cs 中创建类
  - 实现 Generate() 方法，支持从预定义列表生成唯一名称
  - 实现备用名称生成逻辑（使用前缀+索引）
  - 实现详细的错误报告机制
  - _需求: 3.1, 3.2, 3.3, 3.4_

- [x] 1.3 实现 ExportMetadataBuilder 类


  - 在 TestData/Helpers/ExportMetadataBuilder.cs 中创建类
  - 实现 Build() 方法，从 ExportData 创建 ExportMetadata
  - 包含版本信息、时间戳和数据统计
  - _需求: 5.1, 5.2, 5.3_

- [x] 2. 实现实体生成器类





  - 按依赖顺序实现各个实体生成器
  - 每个生成器使用 UniqueNameGenerator 生成唯一名称
  - _需求: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7_


- [x] 2.1 实现 SkillGenerator 类

  - 在 TestData/Generators/SkillGenerator.cs 中创建类
  - 实现 Generate() 方法生成技能数据
  - 使用 UniqueNameGenerator 生成唯一技能名称
  - 从原 TestDataGenerator.GenerateSkills() 迁移逻辑
  - _需求: 1.1_

- [x] 2.2 实现 PersonnelGenerator 类


  - 在 TestData/Generators/PersonnelGenerator.cs 中创建类
  - 实现 Generate(List<SkillDto> skills) 方法生成人员数据
  - 使用 UniqueNameGenerator 生成唯一人员名称
  - 随机分配技能给人员
  - 从原 TestDataGenerator.GeneratePersonnel() 迁移逻辑
  - _需求: 1.2_

- [x] 2.3 实现 PositionGenerator 类


  - 在 TestData/Generators/PositionGenerator.cs 中创建类
  - 实现 Generate(List<SkillDto> skills, List<PersonnelDto> personnel) 方法
  - 使用 UniqueNameGenerator 生成唯一哨位名称和地点
  - 根据技能匹配可用人员
  - 从原 TestDataGenerator.GeneratePositions() 迁移逻辑
  - _需求: 1.3_

- [x] 2.4 实现 HolidayConfigGenerator 类


  - 在 TestData/Generators/HolidayConfigGenerator.cs 中创建类
  - 实现 Generate() 方法生成节假日配置数据
  - 生成标准周末配置、单休配置和自定义配置
  - 确保只有一个配置处于激活状态
  - 从原 TestDataGenerator.GenerateHolidayConfigs() 迁移逻辑
  - _需求: 1.4_

- [x] 2.5 实现 TemplateGenerator 类


  - 在 TestData/Generators/TemplateGenerator.cs 中创建类
  - 实现 Generate(List<PersonnelDto> personnel, List<PositionDto> positions, List<HolidayConfigDto> holidayConfigs) 方法
  - 生成不同类型的排班模板（regular、holiday、special）
  - 为每个模板选择合适数量的人员和哨位
  - 从原 TestDataGenerator.GenerateTemplates() 迁移逻辑
  - _需求: 1.5_

- [x] 2.6 实现 FixedAssignmentGenerator 类


  - 在 TestData/Generators/FixedAssignmentGenerator.cs 中创建类
  - 实现 Generate(List<PersonnelDto> personnel, List<PositionDto> positions) 方法
  - 为人员生成定岗规则，指定允许的哨位和时段
  - 从原 TestDataGenerator.GenerateFixedAssignments() 迁移逻辑
  - _需求: 1.6_

- [x] 2.7 实现 ManualAssignmentGenerator 类


  - 在 TestData/Generators/ManualAssignmentGenerator.cs 中创建类
  - 实现 Generate(List<PersonnelDto> personnel, List<PositionDto> positions) 方法
  - 生成手动指定数据，确保唯一性（同一哨位、日期、时段不重复）
  - 实现重试逻辑，尝试不同时段和日期
  - 从原 TestDataGenerator.GenerateManualAssignments() 迁移逻辑
  - _需求: 1.7_

- [x] 3. 实现验证器类





  - 将所有验证逻辑从 TestDataGenerator 提取到独立的验证器类
  - _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8_

- [x] 3.1 实现 TestDataValidator 类


  - 在 TestData/Validation/TestDataValidator.cs 中创建类
  - 实现 Validate(ExportData data) 公共方法
  - 从原 TestDataGenerator.ValidateGeneratedData() 迁移主验证逻辑
  - _需求: 2.1_

- [x] 3.2 迁移各实体验证方法到 TestDataValidator

  - 迁移 ValidateSkills() 方法
  - 迁移 ValidatePersonnel() 方法
  - 迁移 ValidatePositions() 方法
  - 迁移 ValidateHolidayConfigs() 方法
  - 迁移 ValidateTemplates() 方法
  - 迁移 ValidateFixedAssignments() 方法
  - 迁移 ValidateManualAssignments() 方法
  - _需求: 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8_
- [x] 4. 实现导出器类
  - 将导出逻辑从 TestDataGenerator 提取到独立的导出器类
  - _需求: 4.1, 4.2, 4.3, 4.4_


- [x] 4.1 实现 TestDataExporter 类

  - 在 TestData/Export/TestDataExporter.cs 中创建类
  - 实现 ExportToFileAsync(ExportData data, string filePath) 方法
  - 实现 ExportToStorageFileAsync(ExportData data, StorageFile file) 方法
  - 实现 ExportToJson(ExportData data) 方法
  - 实现 GetJsonOptions() 私有辅助方法
  - 从原 TestDataGenerator 迁移导出逻辑
  - _需求: 4.1, 4.2, 4.3, 4.4_

- [-] 5. 重构 TestDataGenerator 为协调器


  - 修改 TestDataGenerator 使用新创建的组件
  - 保持所有公共 API 不变
  - _需求: 6.1, 6.2, 6.3, 6.4, 6.5, 7.1, 7.2, 7.3, 7.4_

- [x] 5.1 更新 TestDataGenerator 构造函数


  - 保持现有构造函数签名不变
  - 在构造函数中初始化所有生成器、验证器和导出器
  - 创建共享的 Random 和 SampleDataProvider 实例
  - 创建 UniqueNameGenerator 实例
  - _需求: 6.1, 7.2_

- [x] 5.2 重构 GenerateTestData() 方法


  - 按依赖顺序调用各个生成器
  - 调用 ExportMetadataBuilder 创建元数据
  - 调用 TestDataValidator 验证数据
  - 保持方法签名和返回值不变
  - 保持调试输出信息
  - _需求: 6.1, 6.2, 6.3, 7.1, 7.3_

- [x] 5.3 重构导出方法


  - 修改 ExportToFileAsync() 委托给 TestDataExporter
  - 修改 ExportToStorageFileAsync() 委托给 TestDataExporter
  - 修改 GenerateTestDataAsJson() 委托给 TestDataExporter
  - 保持所有方法签名不变
  - _需求: 6.4, 7.1, 7.3_

- [x] 5.4 删除 TestDataGenerator 中的旧代码






  - 删除所有私有生成方法（GenerateSkills、GeneratePersonnel 等）
  - 删除所有私有验证方法（ValidateSkills、ValidatePersonnel 等）
  - 删除 GenerateUniqueName 私有方法
  - 删除 CreateMetadata 私有方法
  - 保留必要的字段和属性
  - _需求: 6.5_

- [x] 6. 验证和测试






  - 确保重构后的代码功能正确且向后兼容
  - _需求: 7.3, 7.4_


- [x] 6.1 验证向后兼容性

  - 检查所有公共方法签名是否保持不变
  - 检查构造函数签名是否保持不变
  - 使用现有的测试代码（如 VerifyManualAssignments.cs）验证功能
  - 生成测试数据并检查输出格式是否与重构前一致
  - _需求: 7.1, 7.2, 7.3, 7.4_


- [x] 6.2 运行诊断检查

  - 使用 getDiagnostics 工具检查所有新创建的文件
  - 修复任何编译错误或警告
  - 确保代码符合项目的编码规范
  - _需求: 7.4_

- [x] 7. 文档和清理




- [ ] 7. 文档和清理


  - 更新相关文档和注释
  - 进行最终的代码审查
  - _需求: 6.5_

- [x] 7.1 更新代码注释


  - 为所有新类添加 XML 文档注释
  - 更新 TestDataGenerator 的类注释，说明其作为协调器的角色
  - 为复杂的逻辑添加内联注释
  - _需求: 6.5_

- [ ]* 7.2 更新 README 文档
  - 在 TestData/README.md 中记录新的架构
  - 说明各个组件的职责和使用方式
  - 提供代码示例
  - _需求: 6.5_


- [x] 7.3 最终代码审查

  - 检查所有文件的代码质量
  - 确认每个类的代码行数在合理范围内
  - 验证错误处理是否完善
  - 确认所有需求都已实现
  - _需求: 6.5, 7.1, 7.2, 7.3, 7.4_
