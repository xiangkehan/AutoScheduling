# 导入覆盖策略修复

## 问题描述

用户在导入中等规模测试数据时，选择了"覆盖"策略（Replace），但仍然出现唯一性约束错误：

```
错误：数据导入失败：数据违反了唯一性约束。可能存在重复的记录。
```

## 根本原因

在 `DataImportExportService.cs` 中，`Replace` 策略的实现存在问题：

**原有实现：**
```csharp
case ConflictResolutionStrategy.Replace:
    if (exists)
    {
        var skill = MapToSkill(skillDto);
        await _skillRepository.UpdateAsync(skill);  // 只更新字段
        importedCount++;
    }
    else
    {
        var skill = MapToSkill(skillDto);
        await _skillRepository.CreateAsync(skill);  // 使用INSERT
        importedCount++;
    }
    break;
```

**问题分析：**
1. `UpdateAsync` 方法只更新记录的字段值，不处理主键冲突
2. 如果导入数据中的ID与现有数据不同，但其他唯一字段（如技能名称）相同，仍会触发唯一性约束错误
3. `CreateAsync` 使用 `INSERT` 语句，如果ID已存在会导致主键冲突

## 解决方案

修改 `Replace` 策略的实现，采用"先删除后插入"的方式：

```csharp
case ConflictResolutionStrategy.Replace:
    // 覆盖策略：先删除现有记录，再创建新记录
    if (exists)
    {
        await _skillRepository.DeleteAsync(skillDto.Id);
    }
    var skillToCreate = MapToSkill(skillDto);
    await _skillRepository.CreateAsync(skillToCreate);
    importedCount++;
    break;
```

**优点：**
1. 完全删除旧记录，包括所有约束关系
2. 插入新记录时不会有任何冲突
3. 确保导入的数据与文件中的数据完全一致

## 修改的文件

- `Services/DataImportExportService.cs`
  - `ImportSkillsAsync()` - 技能导入
  - `ImportPersonnelAsync()` - 人员导入
  - `ImportPositionsAsync()` - 哨位导入
  - `ImportTemplatesAsync()` - 模板导入
  - `ImportHolidayConfigsAsync()` - 节假日配置导入
  - `ImportFixedAssignmentsAsync()` - 固定分配导入
  - `ImportManualAssignmentsAsync()` - 手动分配导入

## 测试建议

1. 生成中等规模测试数据
2. 导入一次（数据库为空）
3. 再次导入相同文件，选择"覆盖"策略
4. 验证导入成功，无唯一性约束错误
5. 验证数据与文件内容完全一致

## 注意事项

- 覆盖策略会完全删除现有记录，请确保已创建备份
- 系统会在导入前自动创建数据库备份（如果启用了该选项）
- 对于测试数据导入，"覆盖"策略是推荐的默认选项
