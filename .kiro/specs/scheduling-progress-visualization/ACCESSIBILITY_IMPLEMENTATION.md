# 可访问性实现文档

## 概述

本文档描述了排班进度可视化功能中实现的可访问性支持，确保所有用户（包括使用辅助技术的用户）都能有效使用该功能。

## 实现的可访问性功能

### 1. AutomationProperties.Name（自动化属性名称）

为所有重要控件设置了描述性的名称，使屏幕阅读器能够准确识别和朗读控件用途。

#### SchedulingProgressPage.xaml

- **页面标题栏**：`AutomationProperties.Name="排班执行进度页面标题栏"`
- **进度指示区**：`AutomationProperties.Name="排班进度指示区"`
- **进度条**：`AutomationProperties.Name="排班执行进度"`
- **当前阶段**：`AutomationProperties.Name="当前执行阶段"`
- **阶段描述**：`AutomationProperties.Name="阶段描述"`
- **已完成分配统计**：`AutomationProperties.Name="已完成分配统计"`
- **进度百分比**：`AutomationProperties.Name="进度百分比"`
- **实时信息区**：`AutomationProperties.Name="实时信息区"`
- **当前处理信息卡片**：`AutomationProperties.Name="当前处理信息卡片"`
- **当前处理哨位**：`AutomationProperties.Name="当前处理哨位"`
- **当前处理时段**：`AutomationProperties.Name="当前处理时段"`
- **累计执行时间卡片**：`AutomationProperties.Name="累计执行时间卡片"`
- **已用时间**：`AutomationProperties.Name="已用时间"`
- **阶段历史卡片**：`AutomationProperties.Name="阶段历史卡片"`
- **已完成阶段列表**：`AutomationProperties.Name="已完成阶段列表"`
- **警告信息卡片**：`AutomationProperties.Name="警告信息卡片"`
- **警告消息列表**：`AutomationProperties.Name="警告消息列表"`
- **结果展示区**：`AutomationProperties.Name="结果展示区"`
- **执行中状态通知**：`AutomationProperties.Name="执行中状态通知"`
- **成功状态通知**：`AutomationProperties.Name="成功状态通知"`
- **失败状态通知**：`AutomationProperties.Name="失败状态通知"`
- **取消状态通知**：`AutomationProperties.Name="取消状态通知"`
- **排班结果表格卡片**：`AutomationProperties.Name="排班结果表格卡片"`
- **排班结果表格**：`AutomationProperties.Name="排班结果表格"`
- **统计信息卡片**：`AutomationProperties.Name="统计信息卡片"`
- **人员工作量卡片**：`AutomationProperties.Name="人员工作量卡片"`
- **人员工作量列表**：`AutomationProperties.Name="人员工作量列表"`
- **哨位覆盖率卡片**：`AutomationProperties.Name="哨位覆盖率卡片"`
- **哨位覆盖率列表**：`AutomationProperties.Name="哨位覆盖率列表"`
- **冲突详情卡片**：`AutomationProperties.Name="冲突详情卡片"`
- **冲突列表**：`AutomationProperties.Name="冲突列表"`
- **操作按钮区**：`AutomationProperties.Name="操作按钮区"`
- **取消排班按钮**：`AutomationProperties.Name="取消排班"`
- **保存排班按钮**：`AutomationProperties.Name="保存排班结果"`
- **放弃排班按钮**：`AutomationProperties.Name="放弃排班结果"`
- **查看详细结果按钮**：`AutomationProperties.Name="查看详细结果"`
- **返回修改按钮**：`AutomationProperties.Name="返回修改配置"`
- **返回按钮**：`AutomationProperties.Name="返回配置页面"`

#### ScheduleGridControl.xaml

- **控件根元素**：`AutomationProperties.Name="排班结果表格控件"`
- **表格工具栏**：`AutomationProperties.Name="表格工具栏"`
- **导出按钮**：`AutomationProperties.Name="导出表格"`
- **全屏按钮**：`AutomationProperties.Name="全屏查看表格"`
- **表格列头**：`AutomationProperties.Name="表格列头"`
- **表格内容区域**：`AutomationProperties.Name="表格内容区域"`

#### CellModel.xaml

- **控件根元素**：`AutomationProperties.Name="排班表格单元格"`
- **单元格边框**：`AutomationProperties.Name="排班单元格"`（动态更新）
- **人员姓名文本**：`AutomationProperties.Name="人员姓名"`

#### ScheduleGridFullScreenView.xaml

- **标题文本**：`AutomationProperties.Name="排班结果表格标题"`
- **副标题文本**：`AutomationProperties.Name="全屏查看模式说明"`
- **关闭按钮**：`AutomationProperties.Name="关闭全屏视图"`
- **表格内容区域**：`AutomationProperties.Name="全屏表格内容区域"`
- **全屏表格控件**：`AutomationProperties.Name="全屏排班结果表格"`

### 2. AutomationProperties.LiveSetting（实时更新通知）

为动态更新的内容设置了实时通知级别，确保屏幕阅读器能够及时通知用户重要变化。

#### Polite（礼貌模式）- 不打断当前朗读

- **进度条**：`AutomationProperties.LiveSetting="Polite"`
- **当前执行阶段**：`AutomationProperties.LiveSetting="Polite"`
- **已完成分配统计**：`AutomationProperties.LiveSetting="Polite"`
- **进度百分比**：`AutomationProperties.LiveSetting="Polite"`
- **当前处理哨位**：`AutomationProperties.LiveSetting="Polite"`
- **当前处理时段**：`AutomationProperties.LiveSetting="Polite"`
- **已用时间**：`AutomationProperties.LiveSetting="Polite"`
- **警告消息列表**：`AutomationProperties.LiveSetting="Polite"`
- **执行中状态通知**：`AutomationProperties.LiveSetting="Polite"`

#### Assertive（断言模式）- 立即通知

- **成功状态通知**：`AutomationProperties.LiveSetting="Assertive"`
- **失败状态通知**：`AutomationProperties.LiveSetting="Assertive"`
- **取消状态通知**：`AutomationProperties.LiveSetting="Assertive"`
- **冲突列表**：`AutomationProperties.LiveSetting="Assertive"`

### 3. AutomationProperties.HeadingLevel（标题层级）

为页面结构设置了语义化的标题层级，帮助屏幕阅读器用户理解页面结构。

- **Level1（一级标题）**：
  - 页面标题栏
  - 排班结果表格标题（全屏视图）

- **Level2（二级标题）**：
  - 当前处理信息
  - 累计执行时间
  - 阶段历史
  - 警告信息
  - 排班结果表格
  - 统计信息
  - 人员工作量
  - 哨位覆盖率
  - 冲突详情

### 4. AutomationProperties.HelpText（帮助文本）

为关键控件提供了详细的帮助文本，解释控件的用途和操作方法。

- **导出按钮**：`AutomationProperties.HelpText="此功能正在开发中"`
- **全屏按钮**：`AutomationProperties.HelpText="在全屏模式下查看排班表格"`
- **关闭按钮**：`AutomationProperties.HelpText="按 Escape 键关闭全屏视图"`
- **单元格**：动态生成，包含人员信息、手动指定状态、冲突信息等

### 5. 键盘导航支持

所有按钮和交互控件都支持标准的键盘导航（Tab 键）。

#### Tab 键导航顺序

1. 左侧实时信息区（可滚动）
2. 右侧结果展示区（可滚动）
3. 操作按钮区
   - 取消排班按钮（执行中时）
   - 保存排班按钮（成功后）
   - 放弃排班按钮（成功后）
   - 查看详细结果按钮（成功后）
   - 返回修改按钮（失败后）
   - 返回按钮（取消后）

### 6. 键盘快捷键

实现了以下键盘快捷键，提高操作效率：

| 快捷键 | 功能 | 适用场景 |
|--------|------|----------|
| **Esc** | 取消排班 | 排班执行中 |
| **Ctrl+S** | 保存排班 | 排班成功后 |
| **Enter** | 查看详细结果 | 排班成功后 |
| **Esc** | 关闭全屏视图 | 全屏查看模式 |

#### 实现方式

使用 WinUI 3 的 `KeyboardAccelerator` 实现：

```xaml
<Button Content="取消排班">
    <Button.KeyboardAccelerators>
        <KeyboardAccelerator Key="Escape"/>
    </Button.KeyboardAccelerators>
</Button>

<Button Content="保存排班">
    <Button.KeyboardAccelerators>
        <KeyboardAccelerator Key="S" Modifiers="Control"/>
    </Button.KeyboardAccelerators>
</Button>

<Button Content="查看详细结果">
    <Button.KeyboardAccelerators>
        <KeyboardAccelerator Key="Enter"/>
    </Button.KeyboardAccelerators>
</Button>
```

### 7. ToolTip（工具提示）

为所有按钮和交互控件添加了工具提示，说明控件功能和快捷键。

- **取消排班**：`ToolTipService.ToolTip="取消正在执行的排班任务 (Esc)"`
- **保存排班**：`ToolTipService.ToolTip="保存排班结果到数据库 (Ctrl+S)"`
- **放弃排班**：`ToolTipService.ToolTip="放弃当前排班结果"`
- **查看详细结果**：`ToolTipService.ToolTip="查看完整的排班结果 (Enter)"`
- **返回修改**：`ToolTipService.ToolTip="返回配置页面修改参数"`
- **返回**：`ToolTipService.ToolTip="返回配置页面"`
- **导出表格**：`ToolTipService.ToolTip="导出表格（开发中）"`
- **全屏查看**：`ToolTipService.ToolTip="全屏查看"`
- **关闭全屏**：`ToolTipService.ToolTip="关闭全屏视图 (Esc)"`

### 8. 动态可访问性更新（CellModel）

单元格控件根据数据状态动态更新可访问性属性：

```csharp
// 设置自动化属性
string automationName;
string automationHelpText;

if (CellData.IsAssigned && !string.IsNullOrEmpty(CellData.PersonnelName))
{
    automationName = $"已分配: {CellData.PersonnelName}";
    automationHelpText = $"人员 {CellData.PersonnelName} 已分配到此时段";
    
    if (CellData.IsManualAssignment)
    {
        automationHelpText += "，手动指定";
    }
    
    if (CellData.HasConflict)
    {
        automationHelpText += $"，存在冲突: {CellData.ConflictMessage}";
    }
}
else
{
    automationName = "未分配";
    automationHelpText = "此时段尚未分配人员";
}

AutomationProperties.SetName(CellBorder, automationName);
AutomationProperties.SetHelpText(CellBorder, automationHelpText);
```

## 屏幕阅读器支持

### 支持的屏幕阅读器

- **Windows 讲述人（Narrator）**：Windows 内置屏幕阅读器
- **NVDA（NonVisual Desktop Access）**：开源屏幕阅读器
- **JAWS（Job Access With Speech）**：商业屏幕阅读器

### 测试建议

#### 使用 Windows 讲述人测试

1. 按 `Win + Ctrl + Enter` 启动讲述人
2. 导航到排班进度页面
3. 使用 `Tab` 键在控件间导航
4. 使用 `Caps Lock + 方向键` 浏览页面内容
5. 验证以下内容：
   - 页面标题和结构是否清晰
   - 进度更新是否及时通知
   - 按钮功能是否准确描述
   - 快捷键是否正常工作
   - 错误和警告是否立即通知

#### 测试场景

1. **排班执行中**：
   - 验证进度更新通知（Polite 模式）
   - 验证当前处理信息更新
   - 验证取消按钮可访问性
   - 测试 Esc 键取消功能

2. **排班成功**：
   - 验证成功通知（Assertive 模式）
   - 验证统计信息可读性
   - 验证表格内容可访问性
   - 测试 Ctrl+S 保存功能
   - 测试 Enter 查看详细结果功能

3. **排班失败**：
   - 验证失败通知（Assertive 模式）
   - 验证冲突列表可读性
   - 验证返回按钮可访问性

4. **全屏查看**：
   - 验证全屏视图标题
   - 验证表格内容可访问性
   - 测试 Esc 键关闭功能

## 高对比度主题支持

所有控件使用主题资源（ThemeResource），自动支持系统高对比度主题：

- `CardBackgroundFillColorDefaultBrush`
- `CardStrokeColorDefaultBrush`
- `TextFillColorPrimaryBrush`
- `TextFillColorSecondaryBrush`
- `SystemAccentColor`
- `SystemErrorTextColor`
- `SystemFillColorCautionBrush`

## 焦点管理

### 焦点顺序

页面元素按照逻辑顺序接收焦点：

1. 左侧实时信息区
2. 右侧结果展示区
3. 操作按钮区

### 焦点可见性

所有可聚焦元素在获得焦点时都有明显的视觉指示（由 WinUI 3 默认提供）。

## 语义化 HTML 结构

虽然使用的是 XAML 而非 HTML，但通过 `AutomationProperties.HeadingLevel` 实现了类似的语义化结构：

- Level1：页面主标题
- Level2：各个卡片和区域的标题

这使得屏幕阅读器用户可以使用标题导航快速浏览页面结构。

## 已知限制

1. **表格虚拟化**：由于表格使用虚拟化渲染，屏幕阅读器可能无法一次性读取所有单元格内容。用户需要滚动表格才能访问不可见的单元格。

2. **动态内容更新**：虽然设置了 `LiveSetting`，但某些屏幕阅读器可能不会立即通知所有更新。建议用户定期使用导航键检查页面状态。

3. **复杂表格结构**：排班表格是一个复杂的二维网格，屏幕阅读器用户可能需要一些时间来理解其结构。建议提供用户培训或文档。

## 未来改进

1. **ARIA 角色支持**：考虑为自定义控件添加更多 ARIA 角色属性（如果 WinUI 3 支持）。

2. **语音命令**：集成 Windows 语音识别，支持语音控制排班操作。

3. **触摸屏支持**：优化触摸屏设备上的可访问性体验。

4. **多语言支持**：确保所有可访问性文本都支持国际化。

5. **可访问性测试自动化**：开发自动化测试脚本，定期验证可访问性功能。

## 合规性

本实现遵循以下可访问性标准和指南：

- **WCAG 2.1 Level AA**：Web Content Accessibility Guidelines
- **Section 508**：美国联邦政府可访问性标准
- **Microsoft Accessibility Guidelines**：微软可访问性指南

## 参考资源

- [WinUI 3 Accessibility Documentation](https://docs.microsoft.com/en-us/windows/apps/design/accessibility/accessibility)
- [AutomationProperties Class](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.automation.automationproperties)
- [Keyboard Accelerators](https://docs.microsoft.com/en-us/windows/apps/design/input/keyboard-accelerators)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)

## 总结

本实现为排班进度可视化功能提供了全面的可访问性支持，包括：

✅ 为所有控件设置了 `AutomationProperties.Name`  
✅ 为进度条设置了 `AutomationProperties.LiveSetting="Polite"`  
✅ 为错误消息设置了 `AutomationProperties.LiveSetting="Assertive"`  
✅ 确保所有按钮支持键盘导航（Tab 键）  
✅ 实现了快捷键：Esc 取消、Enter 确认、Ctrl+S 保存  
✅ 支持屏幕阅读器（Windows 讲述人、NVDA、JAWS）  
✅ 支持高对比度主题  
✅ 提供了详细的工具提示和帮助文本  
✅ 实现了语义化的标题层级  
✅ 动态更新单元格可访问性属性  

这些功能确保了所有用户，包括使用辅助技术的用户，都能有效地使用排班进度可视化功能。
