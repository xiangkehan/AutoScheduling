# 哨位视图冲突定位功能实现总结

## 实现日期
2025-11-24

## 功能概述
为冲突定位功能添加了对"按哨位视图"的支持，使用户可以在哨位视图中定位和高亮显示冲突。

## 实现内容

### 1. ViewModel 层增强 (`ScheduleResultViewModel.Conflicts.cs`)

#### 1.1 重构定位方法
- **`LocateConflictInGridAsync`**: 改为路由方法，根据当前视图模式选择定位策略
- **`LocateConflictInGridViewAsync`**: 网格视图的定位实现（原有逻辑）
- **`LocateConflictInPositionViewAsync`**: 新增哨位视图的定位实现

#### 1.2 哨位视图定位逻辑
```csharp
private async Task LocateConflictInPositionViewAsync(ConflictDto conflict)
{
    // 1. 确定目标哨位和班次
    // 2. 切换到目标哨位
    // 3. 计算并切换到正确的周次
    // 4. 创建高亮键集合（格式：periodIndex_dayOfWeek）
    // 5. 触发滚动到目标单元格
}
```

**关键特性**：
- 支持未分配冲突（通过 `PositionId` 和 `StartTime` 定位）
- 支持已分配冲突（通过 `RelatedShiftIds` 定位）
- 自动切换到正确的哨位和周次
- 计算单元格键：`periodIndex_dayOfWeek`（0-11 时段，0-6 星期）

### 2. 控件层增强 (`PositionScheduleControl.xaml.cs`)

#### 2.1 新增依赖属性
```csharp
public HashSet<string>? HighlightedCellKeys { get; set; }
```

#### 2.2 高亮功能
- **`UpdateCellHighlights()`**: 更新所有单元格的高亮状态
- 高亮样式：黄色背景 + 橙色边框（3px）
- 优先级：高亮 > 冲突 > 手动指定 > 默认

#### 2.3 滚动功能
- **`ScrollToCell(int periodIndex, int dayOfWeek)`**: 滚动到指定单元格
- 将目标单元格滚动到视口中央
- 使用平滑动画效果

#### 2.4 单元格标签
```csharp
internal class CellTag
{
    public PositionScheduleCell? CellData { get; set; }
    public string CellKey { get; set; }
}
```
用于存储单元格数据和键，便于查找和更新。

### 3. 页面层集成 (`ScheduleResultPage.xaml.cs`)

#### 3.1 滚动事件路由
```csharp
private void OnScrollToCellRequested(object? sender, ScrollToCellEventArgs e)
{
    switch (ViewModel.CurrentViewMode)
    {
        case ViewMode.Grid:
            ScheduleGrid?.ScrollToCell(e.RowIndex, e.ColumnIndex);
            break;
        case ViewMode.ByPosition:
            PositionScheduleControl?.ScrollToCell(e.RowIndex, e.ColumnIndex);
            break;
    }
}
```

#### 3.2 XAML 绑定
```xml
<controls:PositionScheduleControl
    x:Name="PositionScheduleControl"
    ScheduleData="{x:Bind ViewModel.SelectedPositionSchedule, Mode=OneWay}"
    HighlightedCellKeys="{x:Bind ViewModel.HighlightedCellKeys, Mode=OneWay}"/>
```

## 技术细节

### 单元格键格式
- **网格视图**: `rowIndex_columnIndex`（例如：`5_2` 表示第5行第2列）
- **哨位视图**: `periodIndex_dayOfWeek`（例如：`3_1` 表示时段3星期二）

### 周次计算
```csharp
var daysDiff = (targetDate - Schedule.StartDate.Date).Days;
var weekIndex = daysDiff / 7;
```

### 单元格位置计算
```csharp
var weekStartDate = Schedule.StartDate.AddDays(weekIndex * 7);
var dayOfWeek = (targetDate - weekStartDate).Days;
var cellKey = $"{periodIndex}_{dayOfWeek}";
```

## 用户体验

### 操作流程
1. 用户在冲突面板中点击"定位"按钮
2. 系统检测当前视图模式
3. 如果是哨位视图：
   - 自动切换到冲突所在的哨位
   - 自动切换到冲突所在的周次
   - 高亮显示冲突相关的单元格
   - 滚动到第一个冲突单元格

### 视觉反馈
- **高亮单元格**: 黄色背景 + 橙色边框（3px）
- **冲突单元格**: 红色边框（2px）
- **手动指定**: 蓝色边框（2px）
- **滚动动画**: 平滑滚动到视口中央

## 未来扩展

### 待实现功能
- 人员视图的冲突定位
- 列表视图的冲突定位
- 多冲突批量定位
- 冲突导航（上一个/下一个）

## 测试建议

### 测试场景
1. **网格视图定位**: 验证原有功能未受影响
2. **哨位视图定位**: 
   - 测试未分配冲突定位
   - 测试已分配冲突定位
   - 测试跨周冲突定位
   - 测试多班次冲突高亮
3. **视图切换**: 验证切换视图后高亮状态正确
4. **边界情况**:
   - 冲突在第一周/最后一周
   - 冲突在周一/周日
   - 冲突在第一个/最后一个时段

## 文件修改清单

### 修改的文件
1. `ViewModels/Scheduling/ScheduleResultViewModel.Conflicts.cs`
   - 重构 `LocateConflictInGridAsync` 方法
   - 新增 `LocateConflictInGridViewAsync` 方法
   - 新增 `LocateConflictInPositionViewAsync` 方法

2. `Controls/PositionScheduleControl.xaml.cs`
   - 新增 `HighlightedCellKeys` 依赖属性
   - 新增 `UpdateCellHighlights` 方法
   - 新增 `ScrollToCell` 方法
   - 新增 `FindCellElement` 方法
   - 新增 `CellTag` 内部类
   - 修改 `CreateScheduleCell` 方法支持高亮

3. `Views/Scheduling/ScheduleResultPage.xaml.cs`
   - 修改 `OnScrollToCellRequested` 方法支持多视图

4. `Views/Scheduling/ScheduleResultPage.xaml`
   - 为 `PositionScheduleControl` 添加 `x:Name` 属性
   - 绑定 `HighlightedCellKeys` 属性

## 总结

成功为冲突定位功能添加了哨位视图支持，实现了：
- ✅ 自动切换到目标哨位和周次
- ✅ 高亮显示冲突单元格
- ✅ 平滑滚动到目标位置
- ✅ 支持未分配和已分配冲突
- ✅ 保持与网格视图一致的用户体验

代码结构清晰，易于维护和扩展。
