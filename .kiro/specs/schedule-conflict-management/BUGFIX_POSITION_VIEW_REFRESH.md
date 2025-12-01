# 修复：按哨位视图下修复功能应用后没有生效

## 问题描述

在按哨位视图下，应用冲突修复方案后，虽然数据已经更新到数据库，但界面没有刷新，用户看不到修复后的效果。

## 根本原因

在 `ScheduleResultViewModel.Conflicts.cs` 的 `FixConflictInGridAsync` 方法中，应用修复方案后只触发了 `OnPropertyChanged(nameof(GridData))`，这只会刷新网格视图的数据。

但是，按哨位视图、按人员视图和列表视图都有各自独立的数据结构：
- **网格视图**: `GridData` 属性（通过计算属性自动生成）
- **哨位视图**: `PositionSchedules` 集合
- **人员视图**: `PersonnelSchedules` 集合
- **列表视图**: `ShiftList` 集合

当 `Schedule` 对象更新后，只有网格视图会自动刷新（因为 `GridData` 是计算属性），其他视图的数据不会自动更新。

## 修复方案

### 1. 修改 `FixConflictInGridAsync` 方法

在 `ViewModels/Scheduling/ScheduleResultViewModel.Conflicts.cs` 中，添加对当前视图数据的刷新：

```csharp
if (updatedSchedule != null)
{
    // 更新排班数据
    Schedule = updatedSchedule;
    
    // 触发表格数据更新（通过 OnPropertyChanged）
    OnPropertyChanged(nameof(GridData));
    
    // 根据当前视图模式刷新对应的数据
    await RefreshCurrentViewDataAsync();
    
    // 重新检测冲突
    await DetectConflictsAsync();
    
    // 标记有未保存的更改
    HasUnsavedChanges = true;
    
    await _dialogService.ShowSuccessAsync("冲突修复成功");
}
```

### 2. 新增 `RefreshCurrentViewDataAsync` 方法

在 `ViewModels/Scheduling/ScheduleResultViewModel.cs` 中添加新方法：

```csharp
/// <summary>
/// 刷新当前视图的数据
/// </summary>
public async Task RefreshCurrentViewDataAsync()
{
    if (Schedule == null) return;

    try
    {
        switch (CurrentViewMode)
        {
            case ViewMode.Grid:
                // Grid View 通过 GridData 属性自动刷新
                OnPropertyChanged(nameof(GridData));
                break;

            case ViewMode.ByPosition:
                // 重新构建哨位视图数据
                await BuildPositionScheduleDataAsync();
                // 如果有选中的哨位，重新选择它
                if (SelectedPositionSchedule != null)
                {
                    var positionId = SelectedPositionSchedule.PositionId;
                    SelectedPositionSchedule = PositionSchedules.FirstOrDefault(p => p.PositionId == positionId);
                }
                break;

            case ViewMode.ByPersonnel:
                // 重新构建人员视图数据
                await BuildPersonnelScheduleDataAsync();
                // 如果有选中的人员，重新选择它
                if (SelectedPersonnelSchedule != null)
                {
                    var personnelId = SelectedPersonnelSchedule.PersonnelId;
                    SelectedPersonnelSchedule = PersonnelSchedules.FirstOrDefault(p => p.PersonnelId == personnelId);
                }
                break;

            case ViewMode.List:
                // 重新构建列表视图数据
                await BuildShiftListDataAsync();
                break;
        }
    }
    catch (Exception ex)
    {
        await _dialogService.ShowErrorAsync("刷新视图数据失败", ex);
    }
}
```

## 修复逻辑说明

### 视图模式判断
根据 `CurrentViewMode` 属性判断当前处于哪个视图模式，然后执行对应的刷新逻辑。

### 各视图的刷新策略

1. **网格视图 (Grid)**
   - 通过 `OnPropertyChanged(nameof(GridData))` 触发计算属性重新计算
   - `GridData` 会从 `Schedule` 重新生成数据

2. **哨位视图 (ByPosition)**
   - 调用 `BuildPositionScheduleDataAsync()` 重新构建整个哨位数据集合
   - 保持用户当前选中的哨位（通过 `PositionId` 重新查找）
   - 保持当前的周次索引 (`CurrentWeekIndex`)

3. **人员视图 (ByPersonnel)**
   - 调用 `BuildPersonnelScheduleDataAsync()` 重新构建整个人员数据集合
   - 保持用户当前选中的人员（通过 `PersonnelId` 重新查找）

4. **列表视图 (List)**
   - 调用 `BuildShiftListDataAsync()` 重新构建班次列表

### 用户体验优化

- **保持选中状态**: 刷新后重新选择之前选中的哨位/人员，避免用户失去上下文
- **保持周次**: 哨位视图中保持当前的周次索引，用户不需要重新翻页
- **异常处理**: 如果刷新失败，显示友好的错误提示

## 测试验证

### 测试步骤

1. 切换到按哨位视图
2. 选择一个哨位
3. 点击冲突列表中的"修复"按钮
4. 选择一个修复方案并应用
5. 验证哨位视图中的数据已更新

### 预期结果

- ✅ 修复后的人员立即显示在哨位视图的对应单元格中
- ✅ 当前选中的哨位保持不变
- ✅ 当前的周次保持不变
- ✅ 冲突列表自动刷新，显示最新的冲突状态

### 其他视图测试

同样的修复逻辑也适用于：
- 按人员视图
- 列表视图
- 网格视图（已有的功能，继续正常工作）

## 影响范围

- 仅影响冲突修复后的视图刷新逻辑
- 不影响其他功能
- 向后兼容，无需数据迁移

## 相关文件

### 修改的文件
- `ViewModels/Scheduling/ScheduleResultViewModel.Conflicts.cs`
- `ViewModels/Scheduling/ScheduleResultViewModel.cs`

### 相关文档
- `.kiro/specs/schedule-conflict-management/BUGFIX_PERSONNEL_SELECTION.md` - 人员选择修复
- `.kiro/specs/schedule-conflict-management/POSITION_VIEW_CONFLICT_LOCATION.md` - 哨位视图冲突定位

## 编译状态

✅ 编译成功，无错误
⚠️ 453 个警告（与此修复无关的既有警告）
