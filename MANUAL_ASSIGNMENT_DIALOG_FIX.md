# 手动指定对话框修复说明

## 问题描述
点击"添加手动指定"按钮后没有反应，对话框无法显示。

## 根本原因
在 `CreateSchedulingPage.xaml` 中，`ManualAssignmentDialog` 和 `ManualAssignmentEditDialog` 使用了 `x:Load` 绑定：

```xml
x:Load="{x:Bind ViewModel.IsCreatingManualAssignment, Mode=OneWay}"
```

这导致以下问题：
1. `x:Load` 会延迟加载控件，直到绑定的属性为 `true`
2. 当 `IsCreatingManualAssignment` 从 `false` 变为 `true` 时，控件开始加载
3. 但是 `PropertyChanged` 事件立即触发，此时控件可能还没有完成加载
4. 在 `CreateSchedulingPage.xaml.cs` 的 `ViewModel_PropertyChanged` 方法中调用 `ShowAsync()` 时，`ManualAssignmentDialog` 仍然为 `null`

## 解决方案
移除 `x:Load` 绑定，让对话框在页面加载时就创建好，而不是延迟加载。

### 修改的文件
- `Views/Scheduling/CreateSchedulingPage.xaml`

### 具体修改
移除了两个 ContentDialog 的 `x:Load` 属性：

**修改前：**
```xml
<ContentDialog x:Name="ManualAssignmentDialog"
               x:Load="{x:Bind ViewModel.IsCreatingManualAssignment, Mode=OneWay}"
               ...>
```

**修改后：**
```xml
<ContentDialog x:Name="ManualAssignmentDialog"
               ...>
```

## 验证
修复后，对话框会在页面加载时就创建好，当用户点击"添加手动指定"按钮时：
1. `StartCreateManualAssignmentCommand` 执行
2. `IsCreatingManualAssignment` 设置为 `true`
3. `PropertyChanged` 事件触发
4. `ViewModel_PropertyChanged` 方法调用 `ManualAssignmentDialog.ShowAsync()`
5. 对话框正常显示

## 注意事项
- 移除 `x:Load` 后，对话框会占用少量额外内存，但这对于现代应用来说是可以接受的
- 对话框的显示/隐藏仍然由 `IsCreatingManualAssignment` 和 `IsEditingManualAssignment` 属性控制
- 这是 WinUI 3 中处理动态对话框的推荐方式
