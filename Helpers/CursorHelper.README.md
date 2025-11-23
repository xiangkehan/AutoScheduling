# CursorHelper 使用说明

## 概述

`CursorHelper` 是一个辅助类，用于在 WinUI 3 应用中为可交互元素添加手型光标效果。

## 重要说明

在 WinUI 3 中，`ProtectedCursor` 是受保护成员，只能在 Page 或 Control 类内部通过 `this.ProtectedCursor` 访问。因此，光标设置必须在页面或控件的事件处理方法中进行。

## 使用方法

### 方法 1：在 Page/Control 中直接设置（推荐）

这是最简单直接的方法，适用于大多数场景。

**XAML:**
```xml
<Border x:Name="ClickableCard"
        Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        PointerEntered="Card_PointerEntered"
        PointerExited="Card_PointerExited">
    <TextBlock Text="点击我"/>
</Border>
```

**C# Code-Behind (在 Page 或 Control 类中):**
```csharp
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;

private void Card_PointerEntered(object sender, PointerRoutedEventArgs e)
{
    // 在 Page/Control 内部可以访问 this.ProtectedCursor
    this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
}

private void Card_PointerExited(object sender, PointerRoutedEventArgs e)
{
    this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
}
```

### 方法 2：使用 CursorHelper 辅助类

`CursorHelper` 提供了便捷的光标创建方法。

**C# Code:**
```csharp
using AutoScheduling3.Helpers;

private void Card_PointerEntered(object sender, PointerRoutedEventArgs e)
{
    this.ProtectedCursor = CursorHelper.CreateHandCursor();
}

private void Card_PointerExited(object sender, PointerRoutedEventArgs e)
{
    this.ProtectedCursor = CursorHelper.CreateArrowCursor();
}
```

## 应用场景

1. **统计卡片**：当卡片可点击查看详情时
2. **列表项**：自定义列表项需要整体可点击时
3. **自定义控件**：任何需要交互反馈的 Border、Grid 等容器

## 注意事项

1. **Button 控件**：Button 默认已有手型光标，无需额外设置
2. **性能考虑**：对于大量动态生成的元素，建议在模板中直接绑定事件
3. **可访问性**：添加光标效果的同时，确保元素有适当的 `AutomationProperties` 设置

## 示例：ScheduleResultPage

在 `ScheduleResultPage.xaml` 中，统计卡片使用了方法 1：

```xml
<Border x:Name="TotalAssignmentsCard"
        PointerEntered="StatCard_PointerEntered"
        PointerExited="StatCard_PointerExited">
    <!-- 卡片内容 -->
</Border>
```

对应的 code-behind：

```csharp
private void StatCard_PointerEntered(object sender, PointerRoutedEventArgs e)
{
    this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
}

private void StatCard_PointerExited(object sender, PointerRoutedEventArgs e)
{
    this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
}
```

## CursorHelper 提供的方法

- `CreateHandCursor()` - 创建手型光标（点击）
- `CreateArrowCursor()` - 创建箭头光标（默认）
- `CreateWaitCursor()` - 创建等待光标（沙漏）
- `CreateIBeamCursor()` - 创建文本选择光标（I 型）

## 可用的光标形状

`InputSystemCursorShape` 枚举提供以下光标类型：

- `Arrow` - 默认箭头
- `Hand` - 手型（点击）
- `Wait` - 等待（沙漏）
- `IBeam` - 文本选择（I 型）
- `Cross` - 十字
- `SizeAll` - 移动（四向箭头）
- `SizeNorthwestSoutheast` - 对角线调整大小
- `SizeNortheastSouthwest` - 对角线调整大小
- `SizeWestEast` - 水平调整大小
- `SizeNorthSouth` - 垂直调整大小
- `UniversalNo` - 禁止操作
- `Help` - 帮助（问号）
