# 需求文档

## 简介

测试数据生成器在生成测试数据时出现重复内容，导致数据导入失败（违反唯一性约束）。需要改进测试数据生成逻辑，确保生成的数据不会违反数据库的唯一性约束。

## 术语表

- **TestDataGenerator**: 测试数据生成器，负责生成各类测试数据的主类
- **SampleDataProvider**: 示例数据提供者，提供预定义的中文示例数据
- **ExportData**: 导出数据对象，包含所有生成的测试数据
- **唯一性约束**: 数据库中要求某些字段或字段组合必须唯一的约束
- **HashSet**: 用于跟踪已使用值以避免重复的数据结构

## 需求

### 需求 1

**用户故事:** 作为系统用户，我希望生成的测试数据不包含重复内容，以便能够成功导入数据库

#### 验收标准

1. WHEN TestDataGenerator 生成技能数据时，THE TestDataGenerator SHALL 确保每个技能的名称唯一
2. WHEN TestDataGenerator 生成人员数据时，THE TestDataGenerator SHALL 确保每个人员的名称唯一
3. WHEN TestDataGenerator 生成哨位数据时，THE TestDataGenerator SHALL 确保每个哨位的名称唯一
4. WHEN TestDataGenerator 生成节假日配置数据时，THE TestDataGenerator SHALL 确保每个配置的名称唯一
5. WHEN TestDataGenerator 生成排班模板数据时，THE TestDataGenerator SHALL 确保每个模板的名称唯一

### 需求 2

**用户故事:** 作为系统用户，我希望在预定义名称用完后能够生成唯一的名称，以便支持生成大量测试数据

#### 验收标准

1. WHEN 预定义的技能名称列表用完时，THE TestDataGenerator SHALL 生成带有唯一编号的技能名称
2. WHEN 预定义的人员名称列表用完时，THE TestDataGenerator SHALL 生成带有唯一编号的人员名称
3. WHEN 预定义的哨位名称列表用完时，THE TestDataGenerator SHALL 生成带有唯一编号的哨位名称
4. WHEN 预定义的地点列表用完时，THE TestDataGenerator SHALL 生成带有唯一编号的地点名称
5. WHEN 生成的名称与已使用的名称冲突时，THE TestDataGenerator SHALL 继续尝试生成新的唯一名称

### 需求 3

**用户故事:** 作为系统用户，我希望手动指定数据不会产生重复的组合，以便避免违反唯一性约束

#### 验收标准

1. WHEN TestDataGenerator 生成手动指定数据时，THE TestDataGenerator SHALL 确保同一哨位、日期和时段的组合唯一
2. WHEN 生成的手动指定组合已存在时，THE TestDataGenerator SHALL 尝试使用不同的时段
3. WHEN 所有时段都已被占用时，THE TestDataGenerator SHALL 跳过该手动指定的生成
4. WHEN 手动指定数据生成完成时，THE TestDataGenerator SHALL 验证没有重复的组合

### 需求 4

**用户故事:** 作为开发人员，我希望在数据生成失败时能够获得清晰的错误信息，以便快速定位和解决问题

#### 验收标准

1. WHEN TestDataGenerator 检测到重复数据时，THE TestDataGenerator SHALL 在验证阶段报告具体的重复项
2. WHEN 数据验证失败时，THE TestDataGenerator SHALL 提供包含所有错误的详细列表
3. WHEN 生成过程中出现异常时，THE TestDataGenerator SHALL 抛出包含上下文信息的异常
4. WHEN 预定义数据不足时，THE TestDataGenerator SHALL 记录警告信息但继续生成
