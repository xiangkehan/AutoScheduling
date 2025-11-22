# 测试数据生成器

## 概述

测试数据生成器是一个用于创建符合系统规范的示例数据的工具。它可以生成包含技能、人员、哨位、排班模板、约束规则等完整数据集的JSON文件，用于测试导入导出功能。

**核心特性：**
- **智能技能分配**：根据哨位需求为人员分配技能，确保每个哨位都有足够的可用人员
- **技能共现策略**：分析哨位技能需求的共现模式，创建多技能人员以提高人员复用率
- **配置化生成**：支持多种预设场景（演练、实战等），可灵活配置人员可用率、退役率等参数
- **数据质量保证**：自动验证生成的数据符合系统约束，确保数据可用性

## 核心类

### 1. TestDataConfiguration

配置类，用于控制生成数据的数量和特征。

**核心配置参数：**

| 参数 | 说明 | 默认值 | 范围 |
|------|------|--------|------|
| `SkillCount` | 技能数量 | 8 | 1-100 |
| `PersonnelCount` | 人员数量 | 15 | 1-1000 |
| `PositionCount` | 哨位数量 | 10 | 1-100 |
| `MinPersonnelPerPosition` | 每个哨位的最小可用人员数 | 3 | ≥1 |
| `PersonnelAvailabilityRate` | 人员可用率 | 0.85 | 0.0-1.0 |
| `PersonnelRetirementRate` | 人员退役率 | 0.10 | 0.0-1.0 |
| `TemplateCount` | 模板数量 | 3 | 0-50 |
| `FixedAssignmentCount` | 定岗规则数量 | 5 | 0-100 |
| `ManualAssignmentCount` | 手动指定数量 | 8 | 0-200 |
| `HolidayConfigCount` | 节假日配置数量 | 1 | 0-20 |
| `RandomSeed` | 随机种子 | 42 | 任意整数 |

**预设配置场景：**

- `CreateDefault()` - 中等规模（技能8个、人员15个、哨位10个）
- `CreateSmall()` - 小规模（技能5个、人员8个、哨位6个）
- `CreateLarge()` - 大规模（技能15个、人员30个、哨位20个）
- `CreateDrillScenario()` - 演练场景（高可用率95%、低退役率5%）
- `CreateCombatScenario()` - 实战场景（较低可用率75%、较高退役率15%）

**自定义配置示例：**
```csharp
var config = new TestDataConfiguration
{
    SkillCount = 10,
    PersonnelCount = 20,
    PositionCount = 15,
    MinPersonnelPerPosition = 4,           // 每个哨位至少4个可用人员
    PersonnelAvailabilityRate = 0.80,     // 80%的人员可用
    PersonnelRetirementRate = 0.12,       // 12%的人员已退役
    TemplateCount = 4,
    FixedAssignmentCount = 8,
    ManualAssignmentCount = 12,
    HolidayConfigCount = 2,
    RandomSeed = 123
};
```

### 2. SampleDataProvider

提供中文示例数据的静态数据源，包括：
- 中文姓名库（30个）
- 技能名称库（15个）
- 哨位名称库（15个）
- 地点库（15个）
- 描述和要求模板

### 3. TestDataGenerator

主生成器类，负责协调所有数据的生成。

**数据生成流程：**

```
1. 生成技能（无依赖）
   ↓
2. 生成人员（无技能分配）
   ↓
3. 生成哨位（定义技能需求，但无可用人员）
   ↓
4. 智能技能分配（根据哨位需求为人员分配技能）
   ↓
5. 更新哨位可用人员列表
   ↓
6. 生成其他数据（模板、定岗规则、手动指定等）
```

**主要方法：**

- `GenerateTestData()` - 生成完整的测试数据集
- `ExportToFileAsync(string filePath)` - 导出数据到JSON文件（传统方式）
- `ExportToStorageFileAsync(StorageFile file)` - 导出数据到StorageFile（WinUI3方式）
- `GenerateTestDataAsJson()` - 生成JSON字符串（不保存到文件）

### 4. SkillAssigner（智能技能分配器）

根据哨位的技能需求为人员分配技能，确保每个哨位都有足够的可用人员。

**核心算法：**

**阶段1：满足基本需求**
- 按可用人员数量从少到多排序哨位
- 优先为需求人员较少的哨位分配人员
- 为每个哨位确保至少有 `MinPersonnelPerPosition` 个可用人员
- 优先选择技能数量较少的人员（避免过度分配）

**阶段2：创建多技能人员（提高复用率）**
- 分析哨位技能需求的共现模式
- 为30-40%的人员添加额外的共现技能
- 使多技能人员能够同时满足多个哨位的需求
- 确保人员技能数量不超过3个

**技能共现策略：**

通过分析哨位的技能需求，找出经常一起出现的技能组合。例如：
- 哨位A需要技能{1, 2}
- 哨位B需要技能{2, 3}
- 哨位C需要技能{1, 3}

技能共现分析会发现：
- 技能1和技能2经常共现（哨位A）
- 技能2和技能3经常共现（哨位B）
- 技能1和技能3经常共现（哨位C）

因此，创建拥有技能{1, 2, 3}的多技能人员，可以同时满足A、B、C三个哨位的需求，提高人员复用率。

### 5. SkillCooccurrenceAnalyzer（技能共现分析器）

分析哨位技能需求的共现模式，为智能技能分配提供数据支持。

**主要方法：**
- `BuildCooccurrenceMatrix()` - 构建技能共现矩阵（技能ID对 → 共现次数）
- `GetCooccurringSkills()` - 获取与指定技能集合高频共现的技能

## 使用示例

### 基本使用

```csharp
// 使用默认配置
var generator = new TestDataGenerator();
var testData = generator.GenerateTestData();
await generator.ExportToFileAsync("test-data.json");
```

### 使用预设配置场景

```csharp
// 小规模配置
var config = TestDataConfiguration.CreateSmall();
var generator = new TestDataGenerator(config);
await generator.ExportToFileAsync("test-data-small.json");

// 演练场景（高可用率、低退役率）
var drillConfig = TestDataConfiguration.CreateDrillScenario();
var drillGenerator = new TestDataGenerator(drillConfig);
await drillGenerator.ExportToFileAsync("test-data-drill.json");

// 实战场景（较低可用率、较高退役率）
var combatConfig = TestDataConfiguration.CreateCombatScenario();
var combatGenerator = new TestDataGenerator(combatConfig);
await combatGenerator.ExportToFileAsync("test-data-combat.json");
```

### 使用自定义配置

```csharp
var config = new TestDataConfiguration
{
    SkillCount = 10,
    PersonnelCount = 20,
    PositionCount = 15,
    MinPersonnelPerPosition = 4,           // 每个哨位至少4个可用人员
    PersonnelAvailabilityRate = 0.80,     // 80%的人员可用
    PersonnelRetirementRate = 0.12        // 12%的人员已退役
};
var generator = new TestDataGenerator(config);
await generator.ExportToFileAsync("test-data-custom.json");
```

### 配置验证

```csharp
var config = new TestDataConfiguration
{
    PersonnelCount = 10,
    PositionCount = 5,
    MinPersonnelPerPosition = 3
};

// 验证配置
var (isValid, errors, warnings) = config.ValidateWithResult();

if (!isValid)
{
    Console.WriteLine("配置错误：");
    foreach (var error in errors)
    {
        Console.WriteLine($"  - {error}");
    }
}

if (warnings.Any())
{
    Console.WriteLine("配置警告：");
    foreach (var warning in warnings)
    {
        Console.WriteLine($"  - {warning}");
    }
}
```

### 使用StorageFile导出（WinUI3方式）

```csharp
// 导出到ApplicationData.LocalFolder
var localFolder = ApplicationData.Current.LocalFolder;
var testDataFolder = await localFolder.CreateFolderAsync("TestData", CreationCollisionOption.OpenIfExists);
var fileName = $"test-data-{DateTime.Now:yyyyMMdd-HHmmss}.json";
var file = await testDataFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

var generator = new TestDataGenerator();
await generator.ExportToStorageFileAsync(file);
```

### 使用FileSavePicker让用户选择位置

```csharp
var generator = new TestDataGenerator();

// 创建FileSavePicker
var picker = new FileSavePicker();
WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
picker.FileTypeChoices.Add("JSON文件", new List<string> { ".json" });
picker.SuggestedFileName = $"test-data-{DateTime.Now:yyyyMMdd-HHmmss}.json";

var file = await picker.PickSaveFileAsync();
if (file != null)
{
    await generator.ExportToStorageFileAsync(file);
}
```

### 生成JSON字符串（不保存到文件）

```csharp
var generator = new TestDataGenerator(TestDataConfiguration.CreateSmall());
var json = generator.GenerateTestDataAsJson();
// 可以用于其他用途，如发送到API或显示在UI中
```

## 导出格式

生成的JSON文件使用与DataImportExportService相同的序列化选项：

- **格式化输出**: `WriteIndented = true`
- **命名策略**: `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`
- **忽略条件**: `DefaultIgnoreCondition = JsonIgnoreCondition.Never`

这确保了生成的测试数据可以被DataImportExportService成功导入。

## 数据生成特性

### 技能数据
- 唯一的中文技能名称（从预定义的技能库中选择）
- 有意义的描述
- 90%激活率
- 合理的时间戳

### 人员数据
- 真实的中文姓名（从预定义的姓名库中选择）
- **智能技能分配**：根据哨位需求分配1-3个技能
- **配置化状态**：使用 `PersonnelAvailabilityRate` 和 `PersonnelRetirementRate` 控制人员状态
- 初始化12个时段的班次间隔数组
- 确保技能引用正确

### 哨位数据
- 有意义的中文名称和地点（从预定义的库中选择）
- 分配1-2个所需技能
- **自动匹配可用人员**：根据技能要求和人员状态自动计算可用人员列表
- **保证最小人员数**：每个哨位至少有 `MinPersonnelPerPosition` 个可用人员
- 确保引用关系正确

### 智能技能分配特性

**优先级策略：**
1. 优先满足需求人员较少的哨位
2. 优先选择技能数量较少的人员（避免过度分配）
3. 确保每个哨位都有足够的可用人员

**多技能人员创建：**
- 30-40%的人员会被分配多个技能
- 优先组合高频共现的技能
- 提高人员复用率，避免技能需求互斥导致的人员分裂

**技能数量控制：**
- 每个人员至少有1个技能
- 每个人员最多有3个技能
- 技能分配符合实际工作场景

## 数据验证

生成器会自动验证：

**配置验证：**
- 所有数量参数必须大于0
- `MinPersonnelPerPosition` 不能超过 `PersonnelCount`
- `PersonnelAvailabilityRate` 和 `PersonnelRetirementRate` 必须在0.0-1.0之间
- 两者之和不能超过1.0
- 人员数量是否足以满足所有哨位的需求（考虑可用率和退役率）

**数据完整性验证：**
- 引用关系的完整性（技能ID、人员ID、哨位ID）
- 每个哨位至少有 `MinPersonnelPerPosition` 个可用人员
- 每个人员至少有1个技能，最多3个技能
- 哨位的可用人员都符合其技能要求
- 数据符合DTO验证规则

**验证结果：**
- 如果配置无效，会抛出 `InvalidOperationException`
- 如果有警告，会输出到调试日志
- 生成过程中会输出详细的统计信息

## 文件结构

```
TestData/
├── TestDataConfiguration.cs           # 配置类
├── SampleDataProvider.cs              # 示例数据提供者
├── TestDataGenerator.cs               # 主生成器类
├── Generators/
│   ├── SkillGenerator.cs              # 技能生成器
│   ├── PersonnelGenerator.cs          # 人员生成器
│   ├── PositionGenerator.cs           # 哨位生成器
│   ├── SkillAssigner.cs               # 智能技能分配器
│   ├── HolidayConfigGenerator.cs      # 节假日配置生成器
│   ├── TemplateGenerator.cs           # 模板生成器
│   ├── FixedAssignmentGenerator.cs    # 定岗规则生成器
│   └── ManualAssignmentGenerator.cs   # 手动指定生成器
├── Helpers/
│   └── SkillCooccurrenceAnalyzer.cs   # 技能共现分析器
└── README.md                          # 本文档

Examples/
└── GenerateTestDataExample.cs         # 使用示例

Tests/
└── TestDataGeneratorBasicTests.cs     # 基础测试
```

## 技术细节

### 智能技能分配算法

**时间复杂度：** O(P × N)，其中P是哨位数量，N是人员数量

**算法步骤：**

1. **初始化**：为所有人员创建空的技能集合
2. **哨位排序**：按可用人员数量从少到多排序（优先满足需求人员较少的哨位）
3. **基本分配**：
   - 遍历每个哨位
   - 筛选可用且未退役的人员
   - 按技能数量排序（优先选择技能少的人员）
   - 为选中的人员添加该哨位所需的所有技能
   - 确保该哨位至少有 `MinPersonnelPerPosition` 个可用人员
4. **共现分析**：
   - 构建技能共现矩阵
   - 识别高频共现的技能组合
5. **多技能创建**：
   - 为30-40%的人员添加额外的共现技能
   - 确保技能数量不超过3个
6. **补充技能**：
   - 为技能数量为0的人员随机分配1-2个技能
   - 为技能数量为1的人员有50%概率再分配1个技能

### 配置场景对比

| 场景 | 人员可用率 | 退役率 | 每哨位最小人员 | 适用场景 |
|------|-----------|--------|---------------|---------|
| 默认 | 85% | 10% | 3 | 日常测试 |
| 演练 | 95% | 5% | 3 | 演练环境，人员充足 |
| 实战 | 75% | 15% | 4 | 实战环境，人员紧张 |
| 小规模 | 85% | 10% | 3 | 快速测试 |
| 大规模 | 85% | 10% | 3 | 压力测试 |

### 性能优化

- 使用 `HashSet<int>` 存储技能ID，提高查找效率
- 缓存可用人员列表，避免重复过滤
- 提前终止：当哨位可用人员数量达到要求时，提前终止分配

## 完整功能清单

- ✅ 技能数据生成
- ✅ 人员数据生成（无技能）
- ✅ 哨位数据生成（含技能需求）
- ✅ 智能技能分配（根据哨位需求）
- ✅ 技能共现分析
- ✅ 多技能人员创建
- ✅ 哨位可用人员更新
- ✅ 节假日配置生成
- ✅ 排班模板生成
- ✅ 定岗规则生成
- ✅ 手动指定生成
- ✅ 元数据生成
- ✅ 配置验证（错误和警告）
- ✅ JSON序列化导出（支持传统文件路径和WinUI3 StorageFile）
- ✅ 多种预设配置场景

## 常见问题

**Q: 为什么某些哨位的可用人员数量超过了 `MinPersonnelPerPosition`？**

A: `MinPersonnelPerPosition` 是最小值，不是精确值。由于多技能人员的存在，一个人员可能同时满足多个哨位的需求，因此某些哨位的可用人员数量会超过最小值。

**Q: 如何确保生成的数据符合实际场景？**

A: 使用预设的配置场景（如 `CreateDrillScenario()` 或 `CreateCombatScenario()`），或根据实际情况自定义配置参数。

**Q: 生成的数据可以直接用于生产环境吗？**

A: 不建议。测试数据生成器主要用于开发和测试，生成的数据是模拟数据。生产环境应使用真实的业务数据。

**Q: 如何调整多技能人员的比例？**

A: 当前多技能人员比例固定为30-40%。如需调整，可以修改 `SkillAssigner` 类中的相关逻辑。

**Q: 配置验证失败怎么办？**

A: 使用 `ValidateWithResult()` 方法查看详细的错误和警告信息，根据提示调整配置参数。
