# 需求文档

## 简介

TestDataGenerator 类当前包含超过1300行代码，承担了过多职责，包括数据生成、数据验证、元数据创建、文件导出等功能。这违反了单一职责原则，导致代码难以维护和测试。本需求旨在将 TestDataGenerator 合理拆分为多个职责明确的类，提高代码的可维护性和可测试性。

## 术语表

- **TestDataGenerator**: 测试数据生成器主类，负责协调整个测试数据生成流程
- **DataValidator**: 数据验证器，负责验证生成的测试数据的完整性和正确性
- **EntityGenerator**: 实体生成器，负责生成特定类型的实体数据（如技能、人员、哨位等）
- **ExportService**: 导出服务，负责将生成的数据导出到文件
- **UniqueNameGenerator**: 唯一名称生成器，负责生成不重复的名称
- **MetadataBuilder**: 元数据构建器，负责创建导出数据的元数据信息

## 需求

### 需求 1: 拆分数据生成职责

**用户故事:** 作为开发人员，我希望将不同实体的数据生成逻辑分离到独立的生成器类中，以便更容易维护和扩展各个实体的生成逻辑。

#### 验收标准

1. WHEN 系统需要生成技能数据时，THE 系统 SHALL 使用独立的 SkillGenerator 类来生成技能数据
2. WHEN 系统需要生成人员数据时，THE 系统 SHALL 使用独立的 PersonnelGenerator 类来生成人员数据
3. WHEN 系统需要生成哨位数据时，THE 系统 SHALL 使用独立的 PositionGenerator 类来生成哨位数据
4. WHEN 系统需要生成节假日配置数据时，THE 系统 SHALL 使用独立的 HolidayConfigGenerator 类来生成节假日配置数据
5. WHEN 系统需要生成排班模板数据时，THE 系统 SHALL 使用独立的 TemplateGenerator 类来生成排班模板数据
6. WHEN 系统需要生成定岗规则数据时，THE 系统 SHALL 使用独立的 FixedAssignmentGenerator 类来生成定岗规则数据
7. WHEN 系统需要生成手动指定数据时，THE 系统 SHALL 使用独立的 ManualAssignmentGenerator 类来生成手动指定数据

### 需求 2: 提取数据验证逻辑

**用户故事:** 作为开发人员，我希望将数据验证逻辑从 TestDataGenerator 中分离出来，以便独立测试和复用验证逻辑。

#### 验收标准

1. WHEN 系统需要验证生成的数据时，THE 系统 SHALL 使用独立的 TestDataValidator 类来执行验证
2. WHEN TestDataValidator 验证技能数据时，THE TestDataValidator SHALL 检查技能数据的完整性和正确性
3. WHEN TestDataValidator 验证人员数据时，THE TestDataValidator SHALL 检查人员数据的完整性和技能引用的有效性
4. WHEN TestDataValidator 验证哨位数据时，THE TestDataValidator SHALL 检查哨位数据的完整性和技能、人员引用的有效性
5. WHEN TestDataValidator 验证节假日配置时，THE TestDataValidator SHALL 检查节假日配置的完整性和规则的有效性
6. WHEN TestDataValidator 验证排班模板时，THE TestDataValidator SHALL 检查排班模板的完整性和人员、哨位、节假日配置引用的有效性
7. WHEN TestDataValidator 验证定岗规则时，THE TestDataValidator SHALL 检查定岗规则的完整性和人员、哨位引用的有效性
8. WHEN TestDataValidator 验证手动指定时，THE TestDataValidator SHALL 检查手动指定的完整性、唯一性和人员、哨位引用的有效性

### 需求 3: 提取唯一名称生成逻辑

**用户故事:** 作为开发人员，我希望将唯一名称生成逻辑提取到独立的工具类中，以便在多个生成器中复用该逻辑。

#### 验收标准

1. WHEN 任何生成器需要生成唯一名称时，THE 系统 SHALL 使用 UniqueNameGenerator 类来生成不重复的名称
2. WHEN UniqueNameGenerator 生成名称时，THE UniqueNameGenerator SHALL 优先使用预定义的名称列表
3. WHEN 预定义名称列表用完时，THE UniqueNameGenerator SHALL 使用备用名称前缀和索引生成名称
4. WHEN UniqueNameGenerator 无法生成唯一名称时，THE UniqueNameGenerator SHALL 抛出包含详细错误信息的异常

### 需求 4: 提取导出功能

**用户故事:** 作为开发人员，我希望将数据导出功能从 TestDataGenerator 中分离出来，以便独立管理导出逻辑和支持多种导出格式。

#### 验收标准

1. WHEN 系统需要导出测试数据时，THE 系统 SHALL 使用独立的 TestDataExporter 类来执行导出操作
2. WHEN TestDataExporter 导出数据到文件路径时，THE TestDataExporter SHALL 将数据序列化为 JSON 格式并写入指定文件
3. WHEN TestDataExporter 导出数据到 StorageFile 时，THE TestDataExporter SHALL 将数据序列化为 JSON 格式并写入 StorageFile
4. WHEN TestDataExporter 导出数据为字符串时，THE TestDataExporter SHALL 返回 JSON 格式的字符串

### 需求 5: 提取元数据创建逻辑

**用户故事:** 作为开发人员，我希望将元数据创建逻辑从 TestDataGenerator 中分离出来，以便独立管理元数据的构建过程。

#### 验收标准

1. WHEN 系统需要创建导出元数据时，THE 系统 SHALL 使用独立的 ExportMetadataBuilder 类来构建元数据
2. WHEN ExportMetadataBuilder 创建元数据时，THE ExportMetadataBuilder SHALL 包含导出版本、导出时间、数据库版本和应用程序版本信息
3. WHEN ExportMetadataBuilder 创建元数据时，THE ExportMetadataBuilder SHALL 统计各类数据的数量并包含在元数据中

### 需求 6: 重构 TestDataGenerator 为协调器

**用户故事:** 作为开发人员，我希望将 TestDataGenerator 重构为一个轻量级的协调器，负责协调各个生成器、验证器和导出器的工作。

#### 验收标准

1. WHEN TestDataGenerator 生成测试数据时，THE TestDataGenerator SHALL 按依赖顺序调用各个实体生成器
2. WHEN TestDataGenerator 生成完所有数据后，THE TestDataGenerator SHALL 调用 ExportMetadataBuilder 创建元数据
3. WHEN TestDataGenerator 生成完所有数据后，THE TestDataGenerator SHALL 调用 TestDataValidator 验证数据
4. WHEN TestDataGenerator 需要导出数据时，THE TestDataGenerator SHALL 委托给 TestDataExporter 执行导出操作
5. THE TestDataGenerator SHALL 保持向后兼容的公共 API，确保现有代码无需修改

### 需求 7: 保持向后兼容性

**用户故事:** 作为使用 TestDataGenerator 的开发人员，我希望重构后的代码保持向后兼容，以便现有代码无需修改即可继续工作。

#### 验收标准

1. THE 重构后的 TestDataGenerator SHALL 保留所有现有的公共方法签名
2. THE 重构后的 TestDataGenerator SHALL 保留所有现有的构造函数签名
3. WHEN 现有代码调用 TestDataGenerator 的公共方法时，THE 系统 SHALL 返回与重构前相同的结果
4. THE 重构后的代码 SHALL 通过所有现有的测试用例（如果存在）
