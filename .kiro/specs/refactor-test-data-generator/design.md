# 设计文档

## 概述

本设计文档描述了如何将 TestDataGenerator 类从一个超过1300行的单体类重构为多个职责明确、易于维护的组件。重构遵循单一职责原则（SRP）和开闭原则（OCP），同时保持向后兼容性。

### 设计目标

1. **职责分离**: 将数据生成、验证、导出等不同职责分离到独立的类中
2. **可维护性**: 每个类的代码行数控制在合理范围内（建议不超过300行）
3. **可测试性**: 各个组件可以独立测试
4. **可扩展性**: 新增实体类型时只需添加新的生成器，无需修改现有代码
5. **向后兼容**: 保持 TestDataGenerator 的公共 API 不变

## 架构

### 整体架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    TestDataGenerator                         │
│                    (协调器/门面)                              │
│  - 协调各个生成器按依赖顺序生成数据                           │
│  - 调用验证器验证数据                                         │
│  - 委托导出器执行导出操作                                     │
└────────────┬────────────────────────────────────────────────┘
             │
             │ 依赖
             │
    ┌────────┴────────┐
    │                 │
    ▼                 ▼
┌─────────┐      ┌─────────────────┐
│ Helpers │      │   Generators    │
└─────────┘      └─────────────────┘
    │                 │
    │                 │
    ├─ UniqueNameGenerator
    │                 ├─ SkillGenerator
    ├─ ExportMetadataBuilder
    │                 ├─ PersonnelGenerator
    └─ TestDataValidator
                      ├─ PositionGenerator
                      │
                      ├─ HolidayConfigGenerator
                      │
                      ├─ TemplateGenerator
                      │
                      ├─ FixedAssignmentGenerator
                      │
                      └─ ManualAssignmentGenerator

┌─────────────────────┐
│  TestDataExporter   │
│  (导出服务)          │
└─────────────────────┘
```

### 组件职责

#### 1. TestDataGenerator (协调器)
- **职责**: 作为门面模式的实现，协调各个组件完成测试数据的生成、验证和导出
- **依赖**: 所有生成器、验证器、导出器、元数据构建器
- **代码行数**: 约150-200行

#### 2. 实体生成器 (Generators)
每个生成器负责生成特定类型的实体数据：

- **SkillGenerator**: 生成技能数据
- **PersonnelGenerator**: 生成人员数据
- **PositionGenerator**: 生成哨位数据
- **HolidayConfigGenerator**: 生成节假日配置数据
- **TemplateGenerator**: 生成排班模板数据
- **FixedAssignmentGenerator**: 生成定岗规则数据
- **ManualAssignmentGenerator**: 生成手动指定数据

每个生成器的代码行数约100-200行。

#### 3. UniqueNameGenerator (辅助工具)
- **职责**: 生成唯一的名称，避免重复
- **代码行数**: 约80-100行

#### 4. TestDataValidator (验证器)
- **职责**: 验证生成的测试数据的完整性和正确性
- **代码行数**: 约400-500行（包含所有实体的验证逻辑）

#### 5. ExportMetadataBuilder (元数据构建器)
- **职责**: 构建导出数据的元数据信息
- **代码行数**: 约50-80行

#### 6. TestDataExporter (导出服务)
- **职责**: 将生成的数据导出到文件或字符串
- **代码行数**: 约80-100行

## 组件和接口

### 1. TestDataGenerator (重构后)

```csharp
namespace AutoScheduling3.TestData;

/// <summary>
/// 测试数据生成器主类（协调器）
/// </summary>
public class TestDataGenerator
{
    private readonly TestDataConfiguration _config;
    private readonly SampleDataProvider _sampleData;
    private readonly Random _random;
    
    // 生成器
    private readonly SkillGenerator _skillGenerator;
    private readonly PersonnelGenerator _personnelGenerator;
    private readonly PositionGenerator _positionGenerator;
    private readonly HolidayConfigGenerator _holidayConfigGenerator;
    private readonly TemplateGenerator _templateGenerator;
    private readonly FixedAssignmentGenerator _fixedAssignmentGenerator;
    private readonly ManualAssignmentGenerator _manualAssignmentGenerator;
    
    // 辅助工具
    private readonly TestDataValidator _validator;
    private readonly ExportMetadataBuilder _metadataBuilder;
    private readonly TestDataExporter _exporter;

    public TestDataGenerator();
    public TestDataGenerator(TestDataConfiguration config);
    
    // 公共 API（保持向后兼容）
    public ExportData GenerateTestData();
    public async Task ExportToFileAsync(string filePath);
    public async Task ExportToStorageFileAsync(StorageFile file);
    public string GenerateTestDataAsJson();
}
```

### 2. 基础生成器接口

```csharp
namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 实体生成器基础接口
/// </summary>
public interface IEntityGenerator<T>
{
    /// <summary>
    /// 生成实体数据
    /// </summary>
    List<T> Generate();
}
```

### 3. SkillGenerator

```csharp
namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 技能数据生成器
/// </summary>
public class SkillGenerator : IEntityGenerator<SkillDto>
{
    private readonly TestDataConfiguration _config;
    private readonly SampleDataProvider _sampleData;
    private readonly UniqueNameGenerator _nameGenerator;
    private readonly Random _random;

    public SkillGenerator(
        TestDataConfiguration config,
        SampleDataProvider sampleData,
        UniqueNameGenerator nameGenerator,
        Random random);

    public List<SkillDto> Generate();
}
```

### 4. PersonnelGenerator

```csharp
namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 人员数据生成器
/// </summary>
public class PersonnelGenerator : IEntityGenerator<PersonnelDto>
{
    private readonly TestDataConfiguration _config;
    private readonly SampleDataProvider _sampleData;
    private readonly UniqueNameGenerator _nameGenerator;
    private readonly Random _random;

    public PersonnelGenerator(
        TestDataConfiguration config,
        SampleDataProvider sampleData,
        UniqueNameGenerator nameGenerator,
        Random random);

    /// <summary>
    /// 生成人员数据
    /// </summary>
    /// <param name="skills">已生成的技能列表</param>
    public List<PersonnelDto> Generate(List<SkillDto> skills);
}
```

### 5. PositionGenerator

```csharp
namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 哨位数据生成器
/// </summary>
public class PositionGenerator : IEntityGenerator<PositionDto>
{
    private readonly TestDataConfiguration _config;
    private readonly SampleDataProvider _sampleData;
    private readonly UniqueNameGenerator _nameGenerator;
    private readonly Random _random;

    public PositionGenerator(
        TestDataConfiguration config,
        SampleDataProvider sampleData,
        UniqueNameGenerator nameGenerator,
        Random random);

    /// <summary>
    /// 生成哨位数据
    /// </summary>
    /// <param name="skills">已生成的技能列表</param>
    /// <param name="personnel">已生成的人员列表</param>
    public List<PositionDto> Generate(List<SkillDto> skills, List<PersonnelDto> personnel);
}
```

### 6. HolidayConfigGenerator

```csharp
namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 节假日配置数据生成器
/// </summary>
public class HolidayConfigGenerator : IEntityGenerator<HolidayConfigDto>
{
    private readonly TestDataConfiguration _config;
    private readonly Random _random;

    public HolidayConfigGenerator(
        TestDataConfiguration config,
        Random random);

    public List<HolidayConfigDto> Generate();
}
```

### 7. TemplateGenerator

```csharp
namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 排班模板数据生成器
/// </summary>
public class TemplateGenerator : IEntityGenerator<SchedulingTemplateDto>
{
    private readonly TestDataConfiguration _config;
    private readonly SampleDataProvider _sampleData;
    private readonly Random _random;

    public TemplateGenerator(
        TestDataConfiguration config,
        SampleDataProvider sampleData,
        Random random);

    /// <summary>
    /// 生成排班模板数据
    /// </summary>
    /// <param name="personnel">已生成的人员列表</param>
    /// <param name="positions">已生成的哨位列表</param>
    /// <param name="holidayConfigs">已生成的节假日配置列表</param>
    public List<SchedulingTemplateDto> Generate(
        List<PersonnelDto> personnel,
        List<PositionDto> positions,
        List<HolidayConfigDto> holidayConfigs);
}
```

### 8. FixedAssignmentGenerator

```csharp
namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 定岗规则数据生成器
/// </summary>
public class FixedAssignmentGenerator : IEntityGenerator<FixedAssignmentDto>
{
    private readonly TestDataConfiguration _config;
    private readonly Random _random;

    public FixedAssignmentGenerator(
        TestDataConfiguration config,
        Random random);

    /// <summary>
    /// 生成定岗规则数据
    /// </summary>
    /// <param name="personnel">已生成的人员列表</param>
    /// <param name="positions">已生成的哨位列表</param>
    public List<FixedAssignmentDto> Generate(
        List<PersonnelDto> personnel,
        List<PositionDto> positions);
}
```

### 9. ManualAssignmentGenerator

```csharp
namespace AutoScheduling3.TestData.Generators;

/// <summary>
/// 手动指定数据生成器
/// </summary>
public class ManualAssignmentGenerator : IEntityGenerator<ManualAssignmentDto>
{
    private readonly TestDataConfiguration _config;
    private readonly SampleDataProvider _sampleData;
    private readonly Random _random;

    public ManualAssignmentGenerator(
        TestDataConfiguration config,
        SampleDataProvider sampleData,
        Random random);

    /// <summary>
    /// 生成手动指定数据
    /// </summary>
    /// <param name="personnel">已生成的人员列表</param>
    /// <param name="positions">已生成的哨位列表</param>
    public List<ManualAssignmentDto> Generate(
        List<PersonnelDto> personnel,
        List<PositionDto> positions);
}
```

### 10. UniqueNameGenerator

```csharp
namespace AutoScheduling3.TestData.Helpers;

/// <summary>
/// 唯一名称生成器
/// </summary>
public class UniqueNameGenerator
{
    private readonly Random _random;

    public UniqueNameGenerator(Random random);

    /// <summary>
    /// 生成唯一名称
    /// </summary>
    /// <param name="availableNames">可用的预定义名称列表（会被修改）</param>
    /// <param name="usedNames">已使用的名称集合</param>
    /// <param name="fallbackPrefix">备用名称前缀</param>
    /// <param name="index">当前索引</param>
    /// <returns>唯一的名称</returns>
    public string Generate(
        List<string> availableNames,
        HashSet<string> usedNames,
        string fallbackPrefix,
        int index);
}
```

### 11. TestDataValidator

```csharp
namespace AutoScheduling3.TestData.Validation;

/// <summary>
/// 测试数据验证器
/// </summary>
public class TestDataValidator
{
    /// <summary>
    /// 验证生成的数据
    /// </summary>
    /// <param name="data">要验证的数据</param>
    /// <exception cref="InvalidOperationException">当数据验证失败时抛出</exception>
    public void Validate(ExportData data);

    // 私有验证方法
    private void ValidateSkills(List<SkillDto> skills, List<string> errors);
    private void ValidatePersonnel(List<PersonnelDto> personnel, HashSet<int> skillIds, List<string> errors);
    private void ValidatePositions(List<PositionDto> positions, HashSet<int> skillIds, HashSet<int> personnelIds, List<string> errors);
    private void ValidateHolidayConfigs(List<HolidayConfigDto> configs, List<string> errors);
    private void ValidateTemplates(List<SchedulingTemplateDto> templates, HashSet<int> personnelIds, HashSet<int> positionIds, HashSet<int> holidayConfigIds, List<string> errors);
    private void ValidateFixedAssignments(List<FixedAssignmentDto> assignments, HashSet<int> personnelIds, HashSet<int> positionIds, List<string> errors);
    private void ValidateManualAssignments(List<ManualAssignmentDto> assignments, HashSet<int> personnelIds, HashSet<int> positionIds, List<string> errors);
}
```

### 12. ExportMetadataBuilder

```csharp
namespace AutoScheduling3.TestData.Helpers;

/// <summary>
/// 导出元数据构建器
/// </summary>
public class ExportMetadataBuilder
{
    /// <summary>
    /// 创建导出元数据
    /// </summary>
    /// <param name="data">导出数据</param>
    /// <returns>元数据对象</returns>
    public ExportMetadata Build(ExportData data);
}
```

### 13. TestDataExporter

```csharp
namespace AutoScheduling3.TestData.Export;

/// <summary>
/// 测试数据导出器
/// </summary>
public class TestDataExporter
{
    /// <summary>
    /// 导出数据到文件路径
    /// </summary>
    public async Task ExportToFileAsync(ExportData data, string filePath);

    /// <summary>
    /// 导出数据到 StorageFile
    /// </summary>
    public async Task ExportToStorageFileAsync(ExportData data, StorageFile file);

    /// <summary>
    /// 导出数据为 JSON 字符串
    /// </summary>
    public string ExportToJson(ExportData data);

    // 私有辅助方法
    private JsonSerializerOptions GetJsonOptions();
}
```

## 数据模型

### 数据依赖关系

```
Skills (技能)
    ↓
Personnel (人员) ← 依赖 Skills
    ↓
Positions (哨位) ← 依赖 Skills, Personnel
    ↓
HolidayConfigs (节假日配置) ← 独立
    ↓
Templates (排班模板) ← 依赖 Personnel, Positions, HolidayConfigs
    ↓
FixedAssignments (定岗规则) ← 依赖 Personnel, Positions
    ↓
ManualAssignments (手动指定) ← 依赖 Personnel, Positions
```

### 生成顺序

1. **Skills**: 最基础的数据，无依赖
2. **Personnel**: 依赖 Skills
3. **Positions**: 依赖 Skills 和 Personnel
4. **HolidayConfigs**: 独立数据，无依赖
5. **Templates**: 依赖 Personnel、Positions 和 HolidayConfigs
6. **FixedAssignments**: 依赖 Personnel 和 Positions
7. **ManualAssignments**: 依赖 Personnel 和 Positions

## 错误处理

### 错误类型

1. **配置验证错误**: 在 TestDataConfiguration.Validate() 中抛出 ArgumentException
2. **名称生成错误**: 在 UniqueNameGenerator 中抛出 InvalidOperationException，包含详细的错误信息
3. **数据验证错误**: 在 TestDataValidator 中抛出 InvalidOperationException，包含所有验证错误的列表
4. **导出错误**: 在 TestDataExporter 中抛出 IOException 或 JsonException

### 错误处理策略

- **配置错误**: 在生成数据前验证配置，快速失败
- **生成错误**: 提供详细的错误信息，包括当前状态和建议
- **验证错误**: 收集所有错误后一次性报告，而不是遇到第一个错误就停止
- **导出错误**: 向上传播异常，由调用者处理

## 测试策略

### 单元测试

每个组件都应该有独立的单元测试：

1. **生成器测试**: 测试每个生成器能否生成正确数量和格式的数据
2. **验证器测试**: 测试验证器能否正确识别各种错误情况
3. **名称生成器测试**: 测试唯一名称生成的各种场景
4. **导出器测试**: 测试各种导出格式和目标
5. **元数据构建器测试**: 测试元数据的正确性

### 集成测试

测试整个流程：

1. **完整流程测试**: 从配置到生成到验证到导出的完整流程
2. **向后兼容性测试**: 确保重构后的 API 与原有代码兼容
3. **大规模数据测试**: 测试生成大量数据时的性能和正确性

## 迁移策略

### 阶段 1: 创建新组件

1. 创建所有新的生成器类
2. 创建验证器、导出器和辅助工具类
3. 确保每个新组件都有单元测试

### 阶段 2: 重构 TestDataGenerator

1. 修改 TestDataGenerator 使用新组件
2. 保持公共 API 不变
3. 运行所有测试确保功能正确

### 阶段 3: 清理

1. 删除 TestDataGenerator 中的旧代码
2. 更新文档和注释
3. 进行代码审查

### 向后兼容性保证

- TestDataGenerator 的所有公共方法保持不变
- 构造函数签名保持不变
- 返回值类型和格式保持不变
- 异常类型保持不变

## 性能考虑

### 优化点

1. **对象复用**: Random 和 SampleDataProvider 在所有生成器间共享
2. **延迟初始化**: 只在需要时创建生成器实例
3. **批量操作**: 一次性生成所有数据，减少方法调用开销

### 性能目标

- 生成默认规模数据（15人员、10哨位）应在 100ms 内完成
- 生成大规模数据（100人员、50哨位）应在 1秒内完成
- 内存占用应保持在合理范围内（< 50MB）

## 扩展性

### 添加新实体类型

要添加新的实体类型，只需：

1. 创建新的生成器类实现 IEntityGenerator<T>
2. 在 TestDataGenerator 中添加对新生成器的调用
3. 在 TestDataValidator 中添加验证逻辑
4. 更新 ExportData 和 ExportMetadata

### 支持新的导出格式

要支持新的导出格式（如 XML、CSV），只需：

1. 在 TestDataExporter 中添加新的导出方法
2. 在 TestDataGenerator 中添加对应的公共方法

## 文件组织

```
TestData/
├── TestDataGenerator.cs              (协调器，约150-200行)
├── TestDataConfiguration.cs          (现有文件，保持不变)
├── SampleDataProvider.cs             (现有文件，保持不变)
├── Generators/
│   ├── IEntityGenerator.cs           (接口)
│   ├── SkillGenerator.cs             (约100-150行)
│   ├── PersonnelGenerator.cs         (约100-150行)
│   ├── PositionGenerator.cs          (约150-200行)
│   ├── HolidayConfigGenerator.cs     (约150-200行)
│   ├── TemplateGenerator.cs          (约150-200行)
│   ├── FixedAssignmentGenerator.cs   (约100-150行)
│   └── ManualAssignmentGenerator.cs  (约150-200行)
├── Helpers/
│   ├── UniqueNameGenerator.cs        (约80-100行)
│   └── ExportMetadataBuilder.cs      (约50-80行)
├── Validation/
│   └── TestDataValidator.cs          (约400-500行)
└── Export/
    └── TestDataExporter.cs           (约80-100行)
```

## 总结

这个设计将原来的1300+行单体类拆分为约12个职责明确的类，每个类的代码行数都在合理范围内。重构后的代码具有更好的可维护性、可测试性和可扩展性，同时保持了向后兼容性。
