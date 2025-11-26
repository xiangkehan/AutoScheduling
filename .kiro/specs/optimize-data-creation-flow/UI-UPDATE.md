# 测试数据生成器 UI 更新说明

## 更新内容

### 1. 新增配置参数

在自定义配置中新增了三个质量控制参数：

#### 每哨位最小人员数（MinPersonnelPerPosition）
- **类型**: 数字输入框（NumberBox）
- **范围**: 1-20
- **默认值**: 3
- **说明**: 确保每个哨位有足够的可用人员

#### 人员可用率（PersonnelAvailabilityRate）
- **类型**: 滑块（Slider）
- **范围**: 50%-100%（0.5-1.0）
- **步进**: 5%（0.05）
- **默认值**: 85%（0.85）
- **说明**: 模拟人员请假、出差等情况
- **显示**: 实时显示百分比值

#### 人员退役率（PersonnelRetirementRate）
- **类型**: 滑块（Slider）
- **范围**: 0%-30%（0.0-0.3）
- **步进**: 1%（0.01）
- **默认值**: 10%（0.10）
- **说明**: 模拟人员退役、离职等情况
- **显示**: 实时显示百分比值

### 2. UI 布局优化

将自定义配置区域分为三个部分，提高可读性：

1. **基础数据量**
   - 技能数量
   - 人员数量
   - 哨位数量
   - 模板数量
   - 定岗规则数量
   - 手动指定数量
   - 节假日配置数量

2. **质量控制**（新增）
   - 每哨位最小人员数
   - 人员可用率
   - 人员退役率

3. **其他配置**
   - 随机种子

### 3. 新增预设场景

在数据规模选项中新增两个预设场景：

#### 演练场景（高可用率）
- 技能: 5
- 人员: 12
- 哨位: 6
- 可用率: 95%
- 退役率: 5%
- 适用场景: 模拟演练环境

#### 实战场景（低可用率）
- 技能: 12
- 人员: 40
- 哨位: 15
- 可用率: 75%
- 退役率: 15%
- 适用场景: 模拟实战环境

### 4. 实时反馈

- 滑块控件提供实时的百分比显示
- 拖动滑块时，百分比值会立即更新
- 使用 `OnPropertyChanged` 通知机制确保 UI 同步

## 技术实现

### ViewModel 更新

```csharp
// 新增属性
[ObservableProperty]
private int minPersonnelPerPosition = 3;

[ObservableProperty]
private double personnelAvailabilityRate = 0.85;

public int PersonnelAvailabilityRatePercent => (int)(PersonnelAvailabilityRate * 100);

[ObservableProperty]
private double personnelRetirementRate = 0.10;

public int PersonnelRetirementRatePercent => (int)(PersonnelRetirementRate * 100);

// 属性变化通知
partial void OnPersonnelAvailabilityRateChanged(double value)
{
    OnPropertyChanged(nameof(PersonnelAvailabilityRatePercent));
}

partial void OnPersonnelRetirementRateChanged(double value)
{
    OnPropertyChanged(nameof(PersonnelRetirementRatePercent));
}
```

### XAML 绑定

```xml
<!-- 滑块控件 -->
<Slider Value="{x:Bind ViewModel.PersonnelAvailabilityRate, Mode=TwoWay}"
        Minimum="0.5" Maximum="1.0" 
        StepFrequency="0.05"
        TickFrequency="0.1"
        TickPlacement="BottomRight"/>

<!-- 百分比显示 -->
<TextBlock>
    <Run Text="{x:Bind ViewModel.PersonnelAvailabilityRatePercent, Mode=OneWay}"/>
    <Run Text="% (50%-100%，模拟人员请假、出差等情况)"/>
</TextBlock>
```

## 使用说明

### 快速开始

1. 打开"设置" → "测试数据生成器"
2. 选择预设场景（推荐）或选择"自定义"
3. 如果选择自定义，可以调整所有参数
4. 点击"生成测试数据"按钮

### 参数调整建议

#### 人员数量计算公式

```
建议人员数量 = 哨位数量 × 每哨位最小人员数 ÷ (可用率 × (1 - 退役率))
```

示例：
- 哨位数量 = 10
- 每哨位最小人员数 = 3
- 可用率 = 85%
- 退役率 = 10%

```
建议人员数量 = 10 × 3 ÷ (0.85 × 0.9) ≈ 40 人
```

#### 场景选择建议

| 场景 | 适用情况 |
|------|----------|
| 小规模 | 快速功能验证、单元测试 |
| 中等规模 | 日常开发测试、集成测试 |
| 大规模 | 性能压力测试、边界测试 |
| 演练场景 | 模拟演练环境，人员充足 |
| 实战场景 | 模拟实战环境，人员紧张 |
| 自定义 | 特定需求的精确控制 |

## 验证机制

所有参数都会通过 `TestDataConfiguration.Validate()` 进行验证：

- 数值范围检查
- 逻辑关系验证（如：可用率 + 退役率 ≤ 1.0）
- 数据一致性检查（如：人员数量 ≥ 模板数量 × 3）

如果验证失败，会显示详细的错误信息。

## 新增功能（第二次更新）

### 1. 配置预览卡片

在自定义配置模式下，显示一个预览卡片，包含：

- **预估总记录数**：自动计算将要生成的总记录数
- **推荐人员数量**：根据哨位数量、每哨位最小人员数、可用率和退役率自动计算
- **配置状态**：实时显示配置验证结果（✅有效 / ⚠️警告 / ❌错误）

**推荐人员数量计算公式：**
```
推荐人员数 = 哨位数量 × 每哨位最小人员数 ÷ (可用率 × (1 - 退役率))
```

示例：
- 哨位数量 = 10
- 每哨位最小人员数 = 3
- 可用率 = 85%
- 退役率 = 10%

```
推荐人员数 = 10 × 3 ÷ (0.85 × 0.9) = 10 × 3 ÷ 0.765 ≈ 40 人
```

### 2. 一键应用推荐

当当前人员数量少于推荐值时，会显示"应用推荐"按钮，点击后自动将人员数量设置为推荐值。

### 3. 实时配置验证

参数改变时自动触发验证，实时显示：
- ✅ 配置有效
- ⚠️ 配置警告（可以生成，但可能不理想）
- ❌ 配置错误（无法生成，必须修正）

验证触发条件：
- 技能数量改变
- 人员数量改变
- 哨位数量改变
- 每哨位最小人员数改变
- 人员可用率改变
- 人员退役率改变

### 4. 详细的工具提示（ToolTip）

为质量控制参数添加了详细的工具提示：

#### 每哨位最小人员数
- 说明参数的作用
- 建议值：3-5人
- 使用注意事项

#### 人员可用率
- 说明模拟的场景
- 不同场景的参考值：
  - 演练场景：95%
  - 日常场景：85%
  - 实战场景：75%

#### 人员退役率
- 说明模拟的场景
- 不同场景的参考值：
  - 演练场景：5%
  - 日常场景：10%
  - 实战场景：15%
- 重要提示：可用率 + 退役率 ≤ 100%

## 技术实现（第二次更新）

### ViewModel 新增属性

```csharp
// 配置验证
[ObservableProperty]
private string validationMessage = string.Empty;

[ObservableProperty]
private bool isConfigurationValid = true;

[ObservableProperty]
private bool hasValidationWarnings;

// 配置预览
public int EstimatedTotalRecords => 
    SkillCount + PersonnelCount + PositionCount + TemplateCount + 
    FixedAssignmentCount + ManualAssignmentCount + HolidayConfigCount;

public int RecommendedPersonnelCount
{
    get
    {
        if (PositionCount <= 0 || MinPersonnelPerPosition <= 0) return PersonnelCount;
        
        var availableRate = PersonnelAvailabilityRate * (1 - PersonnelRetirementRate);
        if (availableRate <= 0) return PersonnelCount;
        
        var recommended = (int)Math.Ceiling(PositionCount * MinPersonnelPerPosition / availableRate);
        return Math.Max(recommended, PersonnelCount);
    }
}

public bool ShowPersonnelRecommendation => 
    IsCustomScale && PersonnelCount < RecommendedPersonnelCount;
```

### ViewModel 新增命令

```csharp
// 应用推荐的人员数量
[RelayCommand]
private void ApplyRecommendedPersonnelCount()
{
    PersonnelCount = RecommendedPersonnelCount;
    ValidateConfiguration();
}

// 验证当前配置
[RelayCommand]
private void ValidateConfiguration()
{
    var config = CreateConfiguration();
    var result = config.ValidateWithResult();
    
    IsConfigurationValid = result.IsValid;
    HasValidationWarnings = result.HasWarnings;
    
    // 更新验证消息
    if (!result.IsValid)
    {
        ValidationMessage = "❌ 配置错误：\n" + string.Join("\n", result.Errors);
    }
    else if (result.HasWarnings)
    {
        ValidationMessage = "⚠️ 配置警告：\n" + string.Join("\n", result.Warnings);
    }
    else
    {
        ValidationMessage = "✅ 配置有效";
    }
}
```

### 自动验证机制

```csharp
// 当关键参数改变时，自动触发验证
partial void OnSkillCountChanged(int value) => ValidateConfiguration();
partial void OnPersonnelCountChanged(int value) => ValidateConfiguration();
partial void OnPositionCountChanged(int value) => ValidateConfiguration();
partial void OnMinPersonnelPerPositionChanged(int value) => ValidateConfiguration();
partial void OnPersonnelRetirementRateChanged(double value)
{
    OnPropertyChanged(nameof(PersonnelRetirementRatePercent));
    ValidateConfiguration();
}
```

### XAML 配置预览卡片

```xml
<Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
        BorderThickness="1"
        CornerRadius="8"
        Padding="16">
    <StackPanel Spacing="12">
        <TextBlock Text="配置预览" FontWeight="SemiBold"/>
        
        <!-- 预估总记录数 -->
        <TextBlock>
            <Run Text="{x:Bind ViewModel.EstimatedTotalRecords, Mode=OneWay}"/>
            <Run Text="条"/>
        </TextBlock>
        
        <!-- 推荐人员数量 + 应用按钮 -->
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="{x:Bind ViewModel.RecommendedPersonnelCount, Mode=OneWay}"/>
            <Button Content="应用推荐" 
                    Command="{x:Bind ViewModel.ApplyRecommendedPersonnelCountCommand}"
                    Visibility="{x:Bind ViewModel.ShowPersonnelRecommendation, Mode=OneWay}"/>
        </StackPanel>
        
        <!-- 配置验证结果 -->
        <TextBlock Text="{x:Bind ViewModel.ValidationMessage, Mode=OneWay}"/>
    </StackPanel>
</Border>
```

### XAML 工具提示

```xml
<TextBlock Text="人员可用率:">
    <ToolTipService.ToolTip>
        <ToolTip>
            <TextBlock TextWrapping="Wrap" MaxWidth="300">
                <Run Text="模拟人员请假、出差、培训等不可用情况。"/>
                <LineBreak/>
                <LineBreak/>
                <Run Text="场景参考：" FontWeight="SemiBold"/>
                <LineBreak/>
                <Run Text="• 演练场景：95%（人员充足）"/>
                <LineBreak/>
                <Run Text="• 日常场景：85%（正常情况）"/>
                <LineBreak/>
                <Run Text="• 实战场景：75%（人员紧张）"/>
            </TextBlock>
        </ToolTip>
    </ToolTipService.ToolTip>
</TextBlock>
```

## 用户体验改进（第二次更新）

### 1. 智能推荐

- 自动计算合理的人员数量
- 考虑可用率和退役率的影响
- 一键应用推荐值

### 2. 实时反馈

- 参数改变时立即验证
- 实时显示配置状态
- 清晰的错误和警告提示

### 3. 上下文帮助

- 详细的工具提示
- 场景参考值
- 使用注意事项

### 4. 可视化预览

- 预估生成的数据量
- 推荐的配置参数
- 配置验证结果

## 后续优化建议

1. ~~添加参数预览功能，显示生成数据的预估统计~~ ✅ 已完成
2. ~~添加参数推荐功能，根据哨位数量自动推荐人员数量~~ ✅ 已完成
3. 添加配置保存/加载功能，保存常用的自定义配置
4. ~~添加参数说明的工具提示（Tooltip）~~ ✅ 已完成
5. 添加配置对比功能，对比不同配置的差异
6. 添加配置导入/导出功能（JSON格式）
