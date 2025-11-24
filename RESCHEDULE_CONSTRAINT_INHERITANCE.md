# 重排功能约束继承实现

## 问题描述

重新排班时，原始排班的约束信息（休息日配置、定岗规则、手动指定）没有被保存和传递，导致用户需要重新配置所有约束条件。

## 解决方案

通过扩展数据库表和数据模型，保存并传递约束配置信息。

## 实施内容

### 1. 数据库迁移（版本 1 → 2）

**文件**: `Data/DatabaseSchema.cs`, `Data/DatabaseService.cs`

为 `Schedules` 表添加 4 个新字段：
- `HolidayConfigId` (INTEGER, nullable) - 休息日配置ID
- `UseActiveHolidayConfig` (INTEGER, default 1) - 是否使用活动配置
- `EnabledFixedRuleIds` (TEXT, default '[]') - 启用的定岗规则ID列表
- `EnabledManualAssignmentIds` (TEXT, default '[]') - 启用的手动指定ID列表

迁移逻辑自动在应用启动时执行，使用 ALTER TABLE 添加新列。

### 2. 数据模型更新

**文件**: `Models/Schedule.cs`

添加约束配置属性：
```csharp
public int? HolidayConfigId { get; set; }
public bool UseActiveHolidayConfig { get; set; } = true;
public List<int> EnabledFixedRuleIds { get; set; } = new();
public List<int> EnabledManualAssignmentIds { get; set; } = new();
```

### 3. DTO 更新

**文件**: `DTOs/ScheduleDto.cs`

同步添加约束配置字段，支持前后端数据传输。

### 4. Repository 层更新

**文件**: `Data/SchedulingRepository.cs`

更新 CRUD 操作：
- `CreateAsync`: 插入时保存约束配置
- `GetByIdAsync`: 查询时读取约束配置
- `GetAllAsync`: 批量查询时读取约束配置
- `UpdateAsync`: 更新时保存约束配置

### 5. Service 层更新

**文件**: `Services/SchedulingService.cs`

- 执行排班时保存约束配置到 Schedule 模型
- 映射 DTO 时包含约束配置信息

### 6. ViewModel 更新

**文件**: `ViewModels/Scheduling/ScheduleResultViewModel.cs`

`Reschedule` 方法现在继承原排班的约束配置：
```csharp
var request = new SchedulingRequestDto
{
    Title = $"{Schedule.Title} (重排)",
    StartDate = Schedule.StartDate,
    EndDate = Schedule.EndDate,
    PersonnelIds = Schedule.PersonnelIds.ToList(),
    PositionIds = Schedule.PositionIds.ToList(),
    // 继承约束配置
    HolidayConfigId = Schedule.HolidayConfigId,
    UseActiveHolidayConfig = Schedule.UseActiveHolidayConfig,
    EnabledFixedRuleIds = Schedule.EnabledFixedRuleIds?.ToList(),
    EnabledManualAssignmentIds = Schedule.EnabledManualAssignmentIds?.ToList()
};
```

## 兼容性

- **向后兼容**: 旧数据库自动迁移，新字段使用默认值
- **数据完整性**: 使用事务确保迁移安全
- **性能影响**: 最小，仅增加 4 个字段的存储和查询

## 测试建议

1. 创建新排班，验证约束配置被正确保存
2. 重排现有排班，验证约束配置被正确继承
3. 从旧版本数据库升级，验证迁移成功
4. 查看草稿和历史记录，验证约束信息显示正确
