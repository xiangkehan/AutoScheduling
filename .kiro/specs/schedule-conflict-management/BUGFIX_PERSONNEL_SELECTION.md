# 修复：冲突解决对话框人员选择问题

## 问题描述

在冲突解决对话框中，无论用户选择了哪个修复方案（人员），应用时总是使用待选择人员列表中的第一个人员。

## 根本原因

在 `ConflictResolutionDialog.xaml.cs` 的 `OnPrimaryButtonClick` 方法中，`FindSelectedRadioButton` 方法无法正确获取选中的 RadioButton。

问题在于：
1. RadioButton 被包装在 ItemsControl 的 DataTemplate 中
2. `ContainerFromIndex` 返回的是容器元素，而不是 RadioButton 本身
3. 需要遍历可视化树才能找到实际的 RadioButton 控件

## 修复方案

### 修改文件
- `Views/Scheduling/ConflictResolutionDialog.xaml.cs`

### 修复内容

1. **重写 `OnPrimaryButtonClick` 方法**
   - 改用新的 `FindSelectedOption` 方法直接返回选中的 `ConflictResolutionOption`
   - 简化逻辑，直接获取选中的方案对象

2. **新增 `FindSelectedOption` 方法**
   - 遍历所有修复方案
   - 使用 `ContainerFromIndex` 获取容器
   - 调用 `FindVisualChild<RadioButton>` 在可视化树中查找 RadioButton
   - 检查 RadioButton 的 `IsChecked` 状态
   - 返回对应索引的 `ConflictResolutionOption`

3. **新增 `FindVisualChild<T>` 辅助方法**
   - 递归遍历可视化树
   - 查找指定类型的子元素
   - 使用 `VisualTreeHelper` 进行树遍历

## 修复后的代码逻辑

```csharp
// 主按钮点击 - 获取选中的方案
private void OnPrimaryButtonClick(...)
{
    var selectedOption = FindSelectedOption();
    if (selectedOption != null)
    {
        SelectedResolution = selectedOption;
    }
    // ... 后备逻辑
}

// 查找选中的方案
private ConflictResolutionOption? FindSelectedOption()
{
    for (int i = 0; i < _resolutionOptions.Count; i++)
    {
        var container = ResolutionOptionsItemsControl.ContainerFromIndex(i);
        if (container != null)
        {
            var radioButton = FindVisualChild<RadioButton>(container);
            if (radioButton != null && radioButton.IsChecked == true)
            {
                return _resolutionOptions[i];  // 返回正确索引的方案
            }
        }
    }
    return null;
}

// 可视化树遍历辅助方法
private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
{
    // 递归查找指定类型的子元素
}
```

## 测试验证

### 测试步骤
1. 打开排班结果页面
2. 点击冲突列表中的"修复"按钮
3. 在冲突解决对话框中选择不同的修复方案
4. 点击"应用方案"按钮
5. 验证应用的是选中的方案，而不是第一个方案

### 预期结果
- 选择第二个方案时，应用第二个方案的人员
- 选择第三个方案时，应用第三个方案的人员
- 不再总是应用第一个方案

## 技术要点

### WinUI 3 可视化树遍历
- ItemsControl 的 DataTemplate 会创建容器元素
- 需要使用 `VisualTreeHelper` 遍历可视化树
- `ContainerFromIndex` 返回的是容器，不是模板内的控件

### RadioButton 分组
- XAML 中使用 `GroupName="ResolutionOptions"` 确保单选
- `IsChecked` 绑定到 `IsRecommended` 作为默认值
- 用户选择会覆盖默认值

## 影响范围

- 仅影响冲突解决对话框的人员选择功能
- 不影响其他功能
- 向后兼容，无需数据迁移

## 编译状态

✅ 编译成功，无错误
⚠️ 453 个警告（与此修复无关的既有警告）
