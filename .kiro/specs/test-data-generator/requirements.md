# 需求文档 - 测试数据生成器

## 简介

本功能旨在为现有的数据导入导出系统创建一套完整的测试数据生成器。该生成器将能够创建符合系统数据结构的示例数据，包括技能、人员、哨位、排班模板、约束规则等，方便开发和测试人员快速验证导入导出功能。

## 术语表

- **TestDataGenerator**: 测试数据生成器，负责创建符合系统规范的示例数据
- **ExportData**: 导出数据对象，包含所有核心数据和元数据的根对象
- **DataImportExportService**: 数据导入导出服务，处理数据的导入和导出操作
- **DTO**: 数据传输对象（Data Transfer Object），用于在不同层之间传输数据
- **JSON**: JavaScript Object Notation，用于数据序列化的格式

## 需求

### 需求 1: 测试数据生成器创建

**用户故事:** 作为开发人员，我希望有一个测试数据生成器类，以便我可以快速创建符合系统规范的测试数据

#### 验收标准

1. THE TestDataGenerator SHALL 提供生成完整ExportData对象的方法
2. THE TestDataGenerator SHALL 支持配置生成数据的数量参数
3. THE TestDataGenerator SHALL 生成的数据符合所有DTO的验证规则
4. THE TestDataGenerator SHALL 确保生成的数据之间的引用关系正确
5. THE TestDataGenerator SHALL 支持生成中文示例数据

### 需求 2: 技能数据生成

**用户故事:** 作为开发人员，我希望生成器能创建技能数据，以便测试技能相关的导入导出功能

#### 验收标准

1. THE TestDataGenerator SHALL 生成至少5个不同的技能记录
2. WHEN 生成技能数据时，THE TestDataGenerator SHALL 包含有意义的中文名称和描述
3. THE TestDataGenerator SHALL 确保每个技能的名称唯一
4. THE TestDataGenerator SHALL 为技能设置合理的IsActive状态
5. THE TestDataGenerator SHALL 为技能设置CreatedAt和UpdatedAt时间戳

### 需求 3: 人员数据生成

**用户故事:** 作为开发人员，我希望生成器能创建人员数据，以便测试人员相关的导入导出功能

#### 验收标准

1. THE TestDataGenerator SHALL 生成至少10个不同的人员记录
2. WHEN 生成人员数据时，THE TestDataGenerator SHALL 使用真实的中文姓名
3. THE TestDataGenerator SHALL 为每个人员随机分配1-3个技能
4. THE TestDataGenerator SHALL 为人员设置合理的可用状态和退役状态
5. THE TestDataGenerator SHALL 为人员初始化12个时段的班次间隔计数数组
6. THE TestDataGenerator SHALL 确保人员引用的技能ID存在于生成的技能列表中

### 需求 4: 哨位数据生成

**用户故事:** 作为开发人员，我希望生成器能创建哨位数据，以便测试哨位相关的导入导出功能

#### 验收标准

1. THE TestDataGenerator SHALL 生成至少8个不同的哨位记录
2. WHEN 生成哨位数据时，THE TestDataGenerator SHALL 包含有意义的中文名称和地点
3. THE TestDataGenerator SHALL 为每个哨位分配1-2个所需技能
4. THE TestDataGenerator SHALL 为每个哨位分配符合技能要求的可用人员
5. THE TestDataGenerator SHALL 确保哨位引用的技能ID和人员ID存在于生成的列表中
6. THE TestDataGenerator SHALL 为哨位设置合理的描述和要求说明

### 需求 5: 节假日配置生成

**用户故事:** 作为开发人员，我希望生成器能创建节假日配置，以便测试节假日相关的导入导出功能

#### 验收标准

1. THE TestDataGenerator SHALL 生成至少2个节假日配置记录
2. WHEN 生成节假日配置时，THE TestDataGenerator SHALL 包含周末规则配置
3. THE TestDataGenerator SHALL 为配置添加法定节假日日期列表
4. THE TestDataGenerator SHALL 为配置添加自定义休息日列表
5. THE TestDataGenerator SHALL 设置一个配置为当前启用状态

### 需求 6: 排班模板生成

**用户故事:** 作为开发人员，我希望生成器能创建排班模板，以便测试模板相关的导入导出功能

#### 验收标准

1. THE TestDataGenerator SHALL 生成至少3个不同类型的排班模板
2. WHEN 生成模板时，THE TestDataGenerator SHALL 包含regular、holiday和special类型
3. THE TestDataGenerator SHALL 为每个模板分配至少5个人员和3个哨位
4. THE TestDataGenerator SHALL 确保模板引用的人员ID和哨位ID存在
5. THE TestDataGenerator SHALL 为模板设置合理的排班天数和使用次数

### 需求 7: 定岗规则生成

**用户故事:** 作为开发人员，我希望生成器能创建定岗规则，以便测试约束相关的导入导出功能

#### 验收标准

1. THE TestDataGenerator SHALL 生成至少3个定岗规则记录
2. WHEN 生成定岗规则时，THE TestDataGenerator SHALL 为每个规则指定人员和允许的哨位
3. THE TestDataGenerator SHALL 为规则设置允许的时段列表（0-11范围内）
4. THE TestDataGenerator SHALL 为规则设置合理的开始和结束日期
5. THE TestDataGenerator SHALL 确保规则引用的人员ID和哨位ID存在

### 需求 8: 手动指定生成

**用户故事:** 作为开发人员，我希望生成器能创建手动指定记录，以便测试手动分配相关的导入导出功能

#### 验收标准

1. THE TestDataGenerator SHALL 生成至少5个手动指定记录
2. WHEN 生成手动指定时，THE TestDataGenerator SHALL 为每条记录指定哨位、人员、日期和时段
3. THE TestDataGenerator SHALL 确保时段索引在0-11范围内
4. THE TestDataGenerator SHALL 确保引用的人员ID和哨位ID存在
5. THE TestDataGenerator SHALL 为手动指定添加有意义的备注说明

### 需求 9: 数据导出功能

**用户故事:** 作为开发人员，我希望生成器能将创建的测试数据导出为JSON文件，以便我可以用于测试导入功能

#### 验收标准

1. THE TestDataGenerator SHALL 提供导出测试数据到JSON文件的方法
2. WHEN 导出数据时，THE TestDataGenerator SHALL 使用与DataImportExportService相同的JSON格式
3. THE TestDataGenerator SHALL 在导出数据中包含完整的元数据信息
4. THE TestDataGenerator SHALL 确保导出的JSON文件可以被DataImportExportService成功导入
5. THE TestDataGenerator SHALL 支持指定导出文件路径

