# 多技能人员比例配置说明

## 功能概述

新增 `MultiSkilledPersonnelRate` 配置参数，用于控制测试数据生成器中多技能人员的比例，从而直接控制哨位之间的人员共享程度。

## 配置参数

### MultiSkilledPersonnelRate（多技能人员比例）

- **类型**: `double`
- **范围**: 0.0 - 1.0（0% - 100%）
- **默认值**: 0.35（35%）
- **UI范围**: 0% - 60%（推荐范围）

## 使用场景

### 低共享场景（0% - 30%）
- **适用**: 实战场景、人员紧张情况
- **特点**: 每个哨位有更多专属人员，人员调度灵活性较低
- **推荐值**: 25-30%

### 适度共享场景（30% - 40%）
- **适用**: 日常排班、正常情况
- **特点**: 平衡人员利用率和调度灵活性
- **推荐值**: 35%（默认）

### 高共享场景（40% - 60%）
- **适用**: 演练场景、人员充足情况
- **特点**: 人员可在多个哨位间灵活调度，利用率高
- **推荐值**: 40-50%

## 如何减少哨位之间的人员共享

### 方法1：降低多技能人员比例（推荐）
在测试数据生成器的"自定义配置"中，将"多技能人员比例"滑块调整到较低值（如 0-25%）。

### 方法2：增加人员总数
保持多技能人员比例不变，增加人员总数，让每个哨位有更多可选人员。

### 方法3：增加技能种类
增加技能数量，让哨位之间的技能需求更加独立，自然减少人员共享。

### 方法4：使用预设场景
选择"实战场景"预设，该场景已配置较低的多技能人员比例（30%）。

## 预设配置值

| 预设场景 | 多技能人员比例 | 说明 |
|---------|--------------|------|
| 小规模（快速测试） | 30% | 适度共享 |
| 中等规模（默认） | 35% | 平衡配置 |
| 大规模（压力测试） | 40% | 较高共享 |
| 演练场景 | 25% | 低共享，人员充足 |
| 实战场景 | 30% | 低共享，人员紧张 |

## 技术实现

### 配置类
- `TestDataConfiguration.MultiSkilledPersonnelRate`: 配置属性
- 在 `Validate()` 和 `ValidateWithResult()` 中添加验证逻辑

### 生成器
- `SkillAssigner.CreateMultiSkilledPersonnel()`: 使用配置值替代硬编码的 30-40% 范围

### ViewModel
- `TestDataGeneratorViewModel.MultiSkilledPersonnelRate`: 绑定属性
- `MultiSkilledPersonnelRatePercent`: 百分比显示属性

### UI
- 在"自定义配置"区域的"人员配置"部分添加 Slider 控件
- 仅在选择"自定义"规模时可见和可编辑

## 示例

### 生成最少人员共享的测试数据
```csharp
var config = new TestDataConfiguration
{
    PersonnelCount = 30,
    PositionCount = 10,
    MinPersonnelPerPosition = 3,
    MultiSkilledPersonnelRate = 0.0,  // 0%，最少共享
    PersonnelAvailabilityRate = 0.85,
    PersonnelRetirementRate = 0.10
};

var generator = new TestDataGenerator(config);
var data = generator.GenerateTestData();
```

### 生成高共享的测试数据
```csharp
var config = new TestDataConfiguration
{
    PersonnelCount = 20,
    PositionCount = 10,
    MinPersonnelPerPosition = 3,
    MultiSkilledPersonnelRate = 0.5,  // 50%，高共享
    PersonnelAvailabilityRate = 0.95,
    PersonnelRetirementRate = 0.05
};

var generator = new TestDataGenerator(config);
var data = generator.GenerateTestData();
```

## 注意事项

1. **人员数量要求**: 降低多技能人员比例时，需要确保人员总数足够满足所有哨位的需求
2. **验证提示**: 系统会自动计算推荐的人员数量，并在配置不合理时给出警告
3. **实际效果**: 多技能人员比例为 0% 时，仍可能有少量人员共享（因为基础分配阶段会确保每个哨位有足够人员）
4. **性能影响**: 该参数对生成性能影响很小，可以放心调整

## 相关文件

- `TestData/TestDataConfiguration.cs`: 配置类定义
- `TestData/Generators/SkillAssigner.cs`: 技能分配算法
- `ViewModels/Settings/TestDataGeneratorViewModel.cs`: UI 绑定
- `Views/Settings/TestDataGeneratorPage.xaml`: UI 界面
