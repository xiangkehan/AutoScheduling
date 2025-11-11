# Design Document

## Overview

本设计文档描述如何修复创建排班页面（CreateSchedulingPage）中导航按钮的可见性问题。问题的根源在于XAML中使用的可见性转换器参数设置不正确，导致"下一步"按钮只在第5步显示，而实际应该在步骤1-4显示。

## Architecture

本修复涉及的组件：
- **CreateSchedulingPage.xaml**: UI层，包含导航按钮的XAML定义
- **IntToVisibilityConverter**: 现有的值转换器，用于根据整数值控制可见性

修复方案采用最小化改动原则，只需调整XAML中的转换器参数，无需修改后端逻辑或转换器实现。

## Components and Interfaces

### 1. IntToVisibilityConverter

现有转换器，支持以下参数模式：
- `ConverterParameter="N"`: 当值等于N时显示
- `ConverterParameter="Inverse"` 或 `ConverterParameter="invert"`: 反转可见性逻辑

### 2. 导航按钮布局

当前XAML结构：
```xml
<StackPanel Orientation="Horizontal" Spacing="8" Grid.Column="2">
    <Button Content="上一步" Command="{x:Bind ViewModel.PreviousStepCommand}"/>
    <Button Content="下一步" Command="{x:Bind ViewModel.NextStepCommand}" 
            Visibility="..."/>
    <Button Content="保存为模板" Command="{x:Bind ViewModel.SaveAsTemplateCommand}"
            Visibility="..."/>
    <Button Content="开始排班" Command="{x:Bind ViewModel.ExecuteSchedulingCommand}"
            Visibility="..."/>
</StackPanel>
```

## Data Models

无需修改数据模型。使用现有的：
- `CurrentStep` (int): 当前步骤编号，范围1-5

## Error Handling

本修复为UI层面的简单调整，不涉及错误处理逻辑。转换器本身已有完善的错误处理机制。

## Testing Strategy

### 手动测试场景

1. **步骤1测试**
   - 导航到创建排班页面
   - 验证：显示"下一步"按钮，隐藏"上一步"、"开始排班"、"保存为模板"按钮

2. **步骤2-4测试**
   - 点击"下一步"前进到步骤2、3、4
   - 验证：每个步骤都显示"下一步"和"上一步"按钮，隐藏"开始排班"、"保存为模板"按钮

3. **步骤5测试**
   - 前进到最后一步
   - 验证：显示"上一步"、"开始排班"、"保存为模板"按钮，隐藏"下一步"按钮

4. **导航测试**
   - 测试前进和后退功能
   - 验证：按钮状态在步骤切换时正确更新

## Implementation Details

### 问题分析

当前XAML中的问题代码：
```xml
<Button Content="下一步" ... 
        Visibility="{x:Bind ViewModel.CurrentStep, Converter={StaticResource IntToVisibilityConverter}, ConverterParameter=5, Mode=OneWay}"/>
```

这个配置表示"当CurrentStep等于5时显示"，但实际需求是"当CurrentStep不等于5时显示"（即步骤1-4显示）。

### 解决方案

IntToVisibilityConverter支持比较运算符（=, >, <, >=, <=），因此可以使用"<5"参数来表示"当CurrentStep小于5时显示"。

### 完整的按钮可见性配置

```xml
<!-- 上一步：步骤2-5显示（CurrentStep > 1） -->
<Button Content="上一步" Command="{x:Bind ViewModel.PreviousStepCommand}"
        Visibility="{x:Bind ViewModel.CurrentStep, Converter={StaticResource IntToVisibilityConverter}, ConverterParameter='>1', Mode=OneWay}"/>

<!-- 下一步：步骤1-4显示（CurrentStep < 5） -->
<Button Content="下一步" Command="{x:Bind ViewModel.NextStepCommand}" 
        Style="{ThemeResource AccentButtonStyle}"
        Visibility="{x:Bind ViewModel.CurrentStep, Converter={StaticResource IntToVisibilityConverter}, ConverterParameter='<5', Mode=OneWay}"/>

<!-- 保存为模板：仅步骤5显示（CurrentStep = 5） -->
<Button Content="保存为模板" Command="{x:Bind ViewModel.SaveAsTemplateCommand}"
        Visibility="{x:Bind ViewModel.CurrentStep, Converter={StaticResource IntToVisibilityConverter}, ConverterParameter='5', Mode=OneWay}"/>

<!-- 开始排班：仅步骤5显示（CurrentStep = 5） -->
<Button Content="开始排班" Command="{x:Bind ViewModel.ExecuteSchedulingCommand}" 
        Style="{ThemeResource AccentButtonStyle}"
        Visibility="{x:Bind ViewModel.CurrentStep, Converter={StaticResource IntToVisibilityConverter}, ConverterParameter='5', Mode=OneWay}">
```

## Design Decisions

### 为什么使用比较运算符？

1. **最小化改动**: 只需修改XAML中的转换器参数，无需修改C#代码
2. **利用现有功能**: IntToVisibilityConverter已支持比较运算符（>, <, >=, <=, =）
3. **可维护性**: 代码简洁直观，易于理解
4. **性能**: 无额外性能开销
5. **语义清晰**: "<5"明确表示"小于5时显示"，比反转逻辑更直观

### 转换器参数说明

IntToVisibilityConverter支持以下参数格式：
- `"5"`: 等于5时显示
- `">1"`: 大于1时显示
- `"<5"`: 小于5时显示
- `">=2"`: 大于等于2时显示
- `"<=4"`: 小于等于4时显示

## Dependencies

- 现有的IntToVisibilityConverter实现（已验证支持比较运算符）
