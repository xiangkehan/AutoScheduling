# 未分配单元格定位功能修复

## 问题描述

冲突处理的定位功能对未分配的单元格无效。

## 根本原因

未分配冲突（`Type == "unassigned"`）没有 `RelatedShiftIds`，因为这些单元格根本没有班次。原有的定位逻辑只处理了有班次的冲突，通过遍历 `RelatedShiftIds` 来查找单元格，导致未分配冲突无法定位。

## 修复方案

在 `ScheduleResultViewModel.Conflicts.cs` 的 `LocateConflictInGridAsync` 方法中添加了对未分配冲突的特殊处理：

### 修复前的逻辑

```csharp
// 只处理有 RelatedShiftIds 的冲突
foreach (var shiftId in conflict.RelatedShiftIds)
{
    var shift = Schedule.Shifts.FirstOrDefault(s => s.Id == shiftId);
    // ... 定位单元格
}
```

### 修复后的逻辑

```csharp
// 1. 处理未分配冲突（没有 RelatedShiftIds）
if (conflict.Type == "unassigned" && conflict.PositionId.HasValue && 
    conflict.StartTime.HasValue && conflict.PeriodIndex.HasValue)
{
    // 直接根据哨位、日期和时段定位
    var row = GridData.Rows.FirstOrDefault(r =>
        r.Date.Date == conflict.StartTime.Value.Date &&
        r.PeriodIndex == conflict.PeriodIndex.Value);
    var col = GridData.Columns.FirstOrDefault(c =>
        c.PositionId == conflict.PositionId.Value);
    
    // 添加到高亮集合
}
else
{
    // 2. 处理其他类型的冲突（根据 RelatedShiftIds）
    foreach (var shiftId in conflict.RelatedShiftIds)
    {
        // ... 原有逻辑
    }
}
```

## 技术细节

### 未分配冲突的数据结构

未分配冲突由 `ConflictDetectionService.DetectUnassignedSlotsAsync()` 生成，包含以下关键字段：

- `Type`: `"unassigned"`
- `PositionId`: 哨位ID
- `StartTime`: 时段开始时间
- `PeriodIndex`: 时段索引（0-11）
- `RelatedShiftIds`: **空列表**（因为没有班次）

### 定位逻辑

未分配冲突的定位不需要查找班次，而是直接根据：
1. **哨位ID** (`PositionId`) → 找到对应的列
2. **日期** (`StartTime.Date`) + **时段索引** (`PeriodIndex`) → 找到对应的行
3. 组合行列索引生成单元格键 `"{rowIndex}_{columnIndex}"`

## 测试验证

### 测试步骤

1. 创建一个排班，确保有未分配的时段
2. 打开冲突面板，查看未分配冲突列表
3. 点击未分配冲突的"定位"按钮
4. 验证：
   - ✅ 表格自动滚动到对应的未分配单元格
   - ✅ 单元格显示橙色高亮边框和背景
   - ✅ 切换到其他冲突时，高亮正确更新

### 预期结果

- 未分配单元格能够正确高亮显示
- 表格自动滚动到高亮单元格
- 高亮效果与其他类型冲突一致

## 影响范围

### 修改的文件

- `ViewModels/Scheduling/ScheduleResultViewModel.Conflicts.cs`

### 影响的功能

- ✅ 未分配冲突的定位功能
- ✅ 冲突高亮显示
- ✅ 自动滚动到冲突单元格

### 不影响的功能

- 其他类型冲突的定位（技能不匹配、休息不足等）
- 冲突检测逻辑
- 冲突修复功能

## 相关需求

满足设计文档中的需求 3.1-3.7：

- ✅ 3.1: 点击"定位"按钮，高亮显示相关单元格
- ✅ 3.2: 单个班次冲突，高亮该单元格
- ✅ 3.3: 多个班次冲突，高亮所有相关单元格
- ✅ 3.4: 自动滚动表格使高亮单元格可见
- ✅ 3.5: 使用明显的视觉效果
- ✅ 3.6: 切换冲突时清除之前的高亮
- ✅ 3.7: 关闭冲突面板时清除所有高亮

## 总结

通过添加对未分配冲突的特殊处理逻辑，修复了定位功能对未分配单元格无效的问题。现在所有类型的冲突（包括未分配、技能不匹配、休息不足等）都能正确定位和高亮显示。
