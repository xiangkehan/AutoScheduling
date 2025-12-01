# 高亮功能调试修复

## 修复时间
2024-11-24

## 问题描述
在测试中，点击冲突项的"定位"按钮后，没有看到单元格的高亮效果。

## 根本原因

### 问题分析
`HighlightedCellKeys` 是一个 `HashSet<string>` 类型的属性。当使用 `Clear()` 和 `Add()` 方法修改集合内容时，虽然集合的内容发生了变化，但**集合对象的引用没有变化**，导致 WinUI 3 的数据绑定系统没有检测到属性变化，因此没有触发 UI 更新。

### 原始代码（有问题）
```csharp
// 清除之前的高亮
HighlightedCellKeys.Clear();  // ❌ 只清空内容，引用不变

// 添加新的高亮
foreach (var shiftId in conflict.RelatedShiftIds)
{
    // ...
    HighlightedCellKeys.Add(cellKey);  // ❌ 只添加内容，引用不变
}

// 手动触发属性变化通知
OnPropertyChanged(nameof(HighlightedCellKeys));  // ❌ 但引用没变，绑定不更新
```

### 为什么会失败？
在 WinUI 3 中，依赖属性的绑定系统通过**对象引用**来检测变化。如果属性的引用没有改变（即使内容改变了），绑定系统可能不会触发更新。

## 修复方案

### 修复后的代码
```csharp
// 创建新的高亮集合
var newHighlightKeys = new HashSet<string>();  // ✅ 创建新对象

int? firstRowIndex = null;
int? firstColumnIndex = null;

// 添加高亮单元格到新集合
foreach (var shiftId in conflict.RelatedShiftIds)
{
    var shift = Schedule.Shifts.FirstOrDefault(s => s.Id == shiftId);
    if (shift == null) continue;

    var row = GridData.Rows.FirstOrDefault(r =>
        r.Date.Date == shift.StartTime.Date &&
        r.PeriodIndex == shift.PeriodIndex);
    var col = GridData.Columns.FirstOrDefault(c =>
        c.PositionId == shift.PositionId);

    if (row != null && col != null)
    {
        var cellKey = $"{row.RowIndex}_{col.ColumnIndex}";
        newHighlightKeys.Add(cellKey);  // ✅ 添加到新集合

        if (firstRowIndex == null)
        {
            firstRowIndex = row.RowIndex;
            firstColumnIndex = col.ColumnIndex;
        }
    }
}

// 更新属性（引用改变，触发绑定更新）
HighlightedCellKeys = newHighlightKeys;  // ✅ 赋值新对象，引用改变
```

### ClearHighlights 方法也需要修复
```csharp
// 修复前
private void ClearHighlights()
{
    HighlightedCellKeys.Clear();  // ❌ 引用不变
    OnPropertyChanged(nameof(HighlightedCellKeys));
}

// 修复后
private void ClearHighlights()
{
    HighlightedCellKeys = new HashSet<string>();  // ✅ 创建新对象
}
```

## 修改的文件

### ViewModels/Scheduling/ScheduleResultViewModel.Conflicts.cs

#### 修改 1: LocateConflictInGridAsync 方法
- **位置**：约第 315-360 行
- **改动**：创建新的 `HashSet<string>` 对象，而不是修改现有对象
- **影响**：确保属性变化能被绑定系统检测到

#### 修改 2: ClearHighlights 方法
- **位置**：约第 368-373 行
- **改动**：直接赋值新的空 `HashSet<string>`
- **影响**：简化代码，确保清除高亮时也能触发更新

## 数据绑定原理

### WinUI 3 依赖属性绑定机制

```
ViewModel 属性变化
    ↓
SetProperty() 被调用
    ↓
检查新值和旧值是否相同（引用比较）
    ↓
如果引用不同 → 触发 PropertyChanged 事件
    ↓
绑定系统接收到事件
    ↓
读取新的属性值
    ↓
更新 UI 控件的依赖属性
    ↓
触发控件的属性变化回调
    ↓
UI 更新
```

### 关键点
- **引用比较**：`SetProperty()` 使用 `EqualityComparer<T>.Default.Equals()` 比较新旧值
- **集合类型**：对于引用类型（如 `HashSet<string>`），比较的是引用，不是内容
- **解决方案**：每次修改集合时，创建新的集合对象

## 测试步骤

### 1. 重新编译项目
```bash
dotnet build
```

### 2. 运行应用程序
启动应用并导航到排班结果页面

### 3. 测试高亮功能
1. 确保选择了"网格"视图模式
2. 打开冲突面板（点击工具栏的"冲突"按钮）
3. 在冲突列表中找到任意一个冲突项
4. 点击该冲突项的"定位"按钮
5. **预期结果**：
   - 相关单元格显示橙色边框（3px）
   - 相关单元格显示半透明橙色背景
   - 表格自动滚动到高亮单元格

### 4. 测试切换高亮
1. 点击第一个冲突的"定位"按钮，观察高亮
2. 点击第二个冲突的"定位"按钮
3. **预期结果**：
   - 第一个冲突的高亮被清除
   - 第二个冲突的单元格被高亮

### 5. 测试清除高亮
1. 定位一个冲突
2. 关闭冲突面板（按 Esc 或点击关闭按钮）
3. **预期结果**：
   - 所有高亮被清除

## 调试技巧

### 如果高亮仍然不显示

#### 1. 检查 HighlightedCellKeys 是否有值
在 `LocateConflictInGridAsync` 方法中添加断点：
```csharp
// 更新属性（引用改变，触发绑定更新）
HighlightedCellKeys = newHighlightKeys;  // 在这里设置断点

// 检查 newHighlightKeys 的内容
// 应该包含类似 "0_0", "1_2" 这样的键
```

#### 2. 检查 UpdateCellHighlights 是否被调用
在 `ScheduleGridControl.xaml.cs` 的 `UpdateCellHighlights` 方法中添加断点：
```csharp
private void UpdateCellHighlights()
{
    var highlightKeys = HighlightedCellKeys ?? new HashSet<string>();
    // 在这里设置断点，检查 highlightKeys 的内容
    
    foreach (var child in GridBody.Children)
    {
        if (child is CellModel cellControl)
        {
            var row = Grid.GetRow(cellControl);
            var col = Grid.GetColumn(cellControl) - 1;
            var cellKey = $"{row}_{col}";
            cellControl.IsHighlighted = highlightKeys.Contains(cellKey);
            // 检查是否有单元格的 IsHighlighted 被设置为 true
        }
    }
}
```

#### 3. 检查 CellModel 的 IsHighlighted 属性
在 `CellModel.xaml.cs` 的 `UpdateHighlightState` 方法中添加断点：
```csharp
private void UpdateHighlightState()
{
    if (IsHighlighted)  // 在这里设置断点
    {
        ApplyHighlightStyle();  // 检查是否执行到这里
    }
    else
    {
        UpdateCellAppearance();
    }
}
```

#### 4. 检查 GridData 是否正确
确保 `GridData` 不为 null，且包含正确的行和列数据：
```csharp
if (Schedule == null || GridData == null) return;

// 检查 GridData.Rows 和 GridData.Columns 是否有数据
```

## 常见问题

### Q: 为什么不能用 ObservableCollection？
A: `ObservableCollection<T>` 适用于列表场景，它会在添加/删除项时触发 `CollectionChanged` 事件。但 `HashSet<T>` 不支持这个事件，而且我们需要的是整个集合的替换，不是单个项的变化。

### Q: 性能会受影响吗？
A: 不会。创建新的 `HashSet<string>` 对象的开销很小，而且高亮的单元格数量通常不多（一般 1-10 个）。

### Q: 为什么 OnPropertyChanged 不够？
A: `OnPropertyChanged` 只是通知绑定系统"这个属性可能变了"，但绑定系统还会检查新旧值的引用是否相同。如果引用相同，可能不会触发更新。

## 验证结果

修复后，高亮功能应该正常工作：
- ✅ 点击"定位"按钮后，单元格显示橙色高亮
- ✅ 切换冲突时，高亮正确更新
- ✅ 关闭冲突面板时，高亮被清除
- ✅ 滚动功能正常工作

## 总结

这是一个典型的 WinUI 3 数据绑定问题。关键教训：
1. **集合类型的属性**：修改内容时要创建新对象
2. **引用类型绑定**：绑定系统比较的是引用，不是内容
3. **简化代码**：直接赋值新对象比 Clear() + Add() + OnPropertyChanged() 更可靠

修复后的代码更简洁、更可靠，符合 WinUI 3 的最佳实践。
