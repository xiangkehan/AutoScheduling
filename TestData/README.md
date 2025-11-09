# 测试数据生成器

## 概述

测试数据生成器是一个用于创建符合系统规范的示例数据的工具。它可以生成包含技能、人员、哨位、排班模板、约束规则等完整数据集的JSON文件，用于测试导入导出功能。

## 核心类

### 1. TestDataConfiguration

配置类，用于控制生成数据的数量和特征。

**预设配置：**
- `CreateDefault()` - 中等规模（技能8个、人员15个、哨位10个）
- `CreateSmall()` - 小规模（技能5个、人员8个、哨位6个）
- `CreateLarge()` - 大规模（技能15个、人员30个、哨位20个）

**自定义配置：**
```csharp
var config = new TestDataConfiguration
{
    SkillCount = 10,
    PersonnelCount = 20,
    PositionCount = 15,
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

**主要方法：**

- `GenerateTestData()` - 生成完整的测试数据集
- `ExportToFileAsync(string filePath)` - 导出数据到JSON文件（传统方式）
- `ExportToStorageFileAsync(StorageFile file)` - 导出数据到StorageFile（WinUI3方式）
- `GenerateTestDataAsJson()` - 生成JSON字符串（不保存到文件）

## 使用示例

### 基本使用

```csharp
// 使用默认配置
var generator = new TestDataGenerator();
var testData = generator.GenerateTestData();
await generator.ExportToFileAsync("test-data.json");
```

### 使用预设配置

```csharp
// 小规模配置
var config = TestDataConfiguration.CreateSmall();
var generator = new TestDataGenerator(config);
await generator.ExportToFileAsync("test-data-small.json");
```

### 使用自定义配置

```csharp
var config = new TestDataConfiguration
{
    SkillCount = 10,
    PersonnelCount = 20,
    PositionCount = 15
};
var generator = new TestDataGenerator(config);
await generator.ExportToFileAsync("test-data-custom.json");
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
- 唯一的中文技能名称
- 有意义的描述
- 90%激活率
- 合理的时间戳

### 人员数据
- 真实的中文姓名
- 随机分配1-3个技能
- 85%可用率，10%退役率
- 初始化12个时段的班次间隔数组
- 确保技能引用正确

### 哨位数据
- 有意义的中文名称和地点
- 分配1-2个所需技能
- 自动匹配符合技能要求的可用人员
- 确保引用关系正确

## 数据验证

生成器会自动验证：
- 配置参数的合理性
- 引用关系的完整性
- 数据符合DTO验证规则

## 文件结构

```
TestData/
├── TestDataConfiguration.cs    # 配置类
├── SampleDataProvider.cs       # 示例数据提供者
├── TestDataGenerator.cs        # 主生成器类
└── README.md                   # 本文档

Examples/
└── GenerateTestDataExample.cs  # 使用示例

Tests/
└── TestDataGeneratorBasicTests.cs  # 基础测试
```

## 下一步

当前实现完成了核心数据生成类的基础结构，包括：

- ✅ 技能数据生成
- ✅ 人员数据生成
- ✅ 哨位数据生成
- ✅ 节假日配置生成
- ✅ 排班模板生成
- ✅ 定岗规则生成
- ✅ 手动指定生成
- ✅ 元数据生成
- ✅ JSON序列化导出（支持传统文件路径和WinUI3 StorageFile）

后续任务将实现文件位置管理器和UI界面。
