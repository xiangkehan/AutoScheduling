# 设计文档

## 概述

本设计解决了在 `SchedulingViewModel` 中应用模版时约束数据未被加载的问题。当前实现中，`LoadTemplateAsync` 方法只加载人员和岗位数据，而约束数据（节假日配置、固定岗位规则、手动指定）的加载被延迟到用户导航到第4步时。由于模版应用后允许直接从第1步跳到第5步，约束数据永远不会被加载。

## 架构

### 当前流程问题

```
用户选择模版 → LoadTemplateAsync()
  ├─ 加载人员和岗位 ✓
  ├─ 存储约束ID到字段 ✓
  └─ 设置 TemplateApplied = true ✓

用户点击"下一步" → NextStep()
  └─ 如果 TemplateApplied，直接跳到第5步 ✓

第5步执行排班 → ExecuteSchedulingAsync()
  └─ 构建请求时使用 FixedPositionRules 和 ManualAssignments ✗
      问题：这些集合为空，因为从未调用 LoadConstraintsAsync()
```

### 修复后的流程

```
用户选择模版 → LoadTemplateAsync()
  ├─ 加载人员和岗位 ✓
  ├─ 立即调用 LoadConstraintsAsync() ✓ (新增)
  ├─ 应用约束启用状态 ✓ (新增)
  └─ 设置 TemplateApplied = true ✓

用户点击"下一步" → NextStep()
  └─ 如果 TemplateApplied，直接跳到第5步 ✓

第5步执行排班 → ExecuteSchedulingAsync()
  └─ 构建请求时使用已加载的约束数据 ✓
```

## 组件和接口

### 修改的组件

#### SchedulingViewModel.LoadTemplateAsync()

**当前实现问题：**
- 只加载基础数据（人员、岗位）
- 将约束ID存储到私有字段 `_enabledFixedRules` 和 `_enabledManualAssignments`
- 依赖后续步骤触发 `LoadConstraintsAsync()`

**修复方案：**
```csharp
private async Task LoadTemplateAsync(int templateId)
{
    IsLoadingInitial = true;
    try
    {
        // 1. 加载模版
        var template = await _templateService.GetByIdAsync(templateId);
        if (template == null)
        {
            await _dialogService.ShowWarningAsync("模板不存在");
            return;
        }
        
        LoadedTemplateId = template.Id;

        // 2. 加载基础数据（如果尚未加载）
        if (AvailablePersonnels.Count == 0 || AvailablePositions.Count == 0)
            await LoadInitialDataAsync();

        // 3. 填充人员和岗位选择
        var selectedPers = AvailablePersonnels
            .Where(p => template.PersonnelIds.Contains(p.Id))
            .ToList();
        var selectedPos = AvailablePositions
            .Where(p => template.PositionIds.Contains(p.Id))
            .ToList();
        
        // 4. 验证缺失的资源
        var missingPersonnel = template.PersonnelIds
            .Except(selectedPers.Select(p => p.Id))
            .ToList();
        var missingPositions = template.PositionIds
            .Except(selectedPos.Select(p => p.Id))
            .ToList();
        
        if (missingPersonnel.Any() || missingPositions.Any())
        {
            var warningMsg = "模板中的部分资源已不存在：\n";
            if (missingPersonnel.Any())
                warningMsg += $"- 缺失人员ID: {string.Join(", ", missingPersonnel)}\n";
            if (missingPositions.Any())
                warningMsg += $"- 缺失岗位ID: {string.Join(", ", missingPositions)}\n";
            warningMsg += "\n将仅加载可用的资源。";
            await _dialogService.ShowWarningAsync(warningMsg);
        }
        
        SelectedPersonnels = new ObservableCollection<PersonnelDto>(selectedPers);
        SelectedPositions = new ObservableCollection<PositionDto>(selectedPos);

        // 5. 存储约束配置
        UseActiveHolidayConfig = template.UseActiveHolidayConfig;
        SelectedHolidayConfigId = template.HolidayConfigId;
        _enabledFixedRules = template.EnabledFixedRuleIds?.ToList() ?? new();
        _enabledManualAssignments = template.EnabledManualAssignmentIds?.ToList() ?? new();

        // 6. 立即加载约束数据（关键修复）
        await LoadConstraintsAsync();
        
        // 7. 应用约束启用状态
        ApplyTemplateConstraints();

        TemplateApplied = true;
        CurrentStep = 1;
        RefreshCommandStates();
        
        await _dialogService.ShowSuccessAsync(
            $"模板已加载\n" +
            $"人员: {selectedPers.Count}\n" +
            $"岗位: {selectedPos.Count}\n" +
            $"约束: {_enabledFixedRules.Count + _enabledManualAssignments.Count}"
        );
    }
    catch (Exception ex)
    {
        await _dialogService.ShowErrorAsync("加载模板失败", ex);
    }
    finally
    {
        IsLoadingInitial = false;
    }
}
```

#### SchedulingViewModel.LoadConstraintsAsync()

**当前实现：**
- 检查 `IsLoadingConstraints` 防止重复加载
- 加载约束数据
- 如果 `TemplateApplied` 为 true，调用 `ApplyTemplateConstraints()`

**修改：**
无需修改，当前实现已经支持被 `LoadTemplateAsync` 调用。

#### SchedulingViewModel.ApplyTemplateConstraints()

**当前实现：**
```csharp
private void ApplyTemplateConstraints()
{
    if (!TemplateApplied) return;

    foreach (var rule in FixedPositionRules)
    {
        rule.IsEnabled = _enabledFixedRules.Contains(rule.Id);
    }
    foreach (var assignment in ManualAssignments)
    {
        assignment.IsEnabled = _enabledManualAssignments.Contains(assignment.Id);
    }
}
```

**修改：**
无需修改，当前实现正确。

## 数据模型

无需修改现有数据模型。

## 错误处理

### 模版不存在
- **场景：** 用户尝试加载已删除的模版
- **处理：** 显示警告消息并返回，不修改当前状态

### 资源缺失
- **场景：** 模版引用的人员或岗位已被删除
- **处理：** 
  1. 显示警告消息，列出缺失的资源ID
  2. 继续加载可用的资源
  3. 允许用户继续使用部分数据

### 约束加载失败
- **场景：** `LoadConstraintsAsync()` 抛出异常
- **处理：** 
  1. 显示错误消息
  2. 模版的人员和岗位数据仍然可用
  3. 用户可以手动配置约束或重试

### 日期范围问题
- **场景：** 用户在应用模版后修改日期范围
- **处理：** 
  1. 监听 `StartDate` 和 `EndDate` 变化
  2. 如果在第4步或之后，重新加载约束数据
  3. 重新应用模版的约束启用状态

## 测试策略

### 单元测试场景

1. **成功加载完整模版**
   - 模版包含有效的人员、岗位和约束
   - 验证所有数据正确填充
   - 验证约束启用状态正确

2. **加载包含缺失资源的模版**
   - 模版引用已删除的人员或岗位
   - 验证显示警告消息
   - 验证仅加载可用资源

3. **约束数据过滤**
   - 模版包含多个手动指定
   - 验证仅加载日期范围内的手动指定

4. **模版应用后修改日期**
   - 应用模版后修改开始/结束日期
   - 验证约束数据重新加载
   - 验证约束启用状态保持

### 集成测试场景

1. **完整工作流测试**
   - 选择模版 → 修改日期 → 跳到第5步 → 执行排班
   - 验证生成的排班包含所有约束

2. **取消和重新应用**
   - 应用模版 → 取消 → 重新应用同一模版
   - 验证状态正确重置和重新加载

### 手动测试场景

1. **UI 响应性**
   - 验证加载指示器正确显示
   - 验证成功/警告消息正确显示
   - 验证第5步摘要显示完整信息

2. **性能测试**
   - 使用包含大量数据的模版
   - 验证加载时间可接受
   - 验证UI不冻结

## 实现注意事项

### 加载顺序
1. 模版元数据
2. 基础数据（人员、岗位）
3. 约束数据（节假日配置、固定岗位规则、手动指定）
4. 应用约束启用状态

### 状态管理
- `IsLoadingInitial`：控制整个加载过程的UI状态
- `IsLoadingConstraints`：防止约束数据重复加载
- `TemplateApplied`：标记模版已应用，影响导航逻辑

### 性能优化
- 使用 `Task.WhenAll` 并行加载约束数据（已在 `LoadConstraintsAsync` 中实现）
- 避免重复加载：检查集合是否已有数据

### 向后兼容
- 不修改公共API
- 不影响非模版工作流
- 保持现有的步骤导航逻辑
