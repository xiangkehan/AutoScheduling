# DateTime 格式修复总结

## 问题描述

在打开排班结果页面时出现错误：
```
加载排班数据失败
详细信息：
String '639001713406909483' was not recognized as a valid DateTime.
```

## 根本原因

数据库中的 `SingleShifts` 表的 `StartTime` 和 `EndTime` 字段存储了 Ticks 值（.NET DateTime 的内部表示），而不是标准的 ISO 8601 格式字符串。当从数据库读取数据时，`DateTime.Parse()` 无法解析这种格式。

## 修复方案

### 1. 读取兼容性修复

在以下文件中添加了兼容的日期时间解析方法：

#### `Data/SchedulingRepository.cs`
- 添加了 `ParseDateTime()` 方法，支持：
  - ISO 8601 格式（正确格式）
  - Ticks 格式（兼容旧数据）
- 修改了 `MapShift()` 方法，使用新的解析方法

#### `Data/ConstraintRepository.cs`
- 添加了 `ParseDateTime()` 方法
- 修改了 `GetManualAssignmentsByDateRangeAsync()` 方法中的日期解析

### 2. 写入格式验证

验证了所有写入数据库的地方都使用了正确的 ISO 8601 格式 (`.ToString("o")`):

#### 已验证的文件：
- ✅ `Data/SchedulingRepository.cs`
  - `CreateAsync()` - Schedules 表
  - `UpdateAsync()` - Schedules 表
  - `InsertShiftInternalAsync()` - SingleShifts 表
  - `AddShiftAsync()` - SingleShifts 表
  - `UpdateShiftAsync()` - SingleShifts 表
  - `MoveBufferToHistoryAsync()` - Schedules 表
  - `ConfirmScheduleAsync()` - Schedules 表

- ✅ `Data/ConstraintRepository.cs`
  - `CreateManualAssignmentAsync()` - ManualAssignments 表
  - `UpdateManualAssignmentAsync()` - ManualAssignments 表
  - `GetManualAssignmentsByDateRangeAsync()` - 查询参数

- ✅ `Data/PersonalRepository.cs`
  - `CreateAsync()` - Personals 表
  - `UpdateAsync()` - Personals 表
  - `UpdateRecentShiftIntervalsAsync()` - Personals 表

- ✅ `Data/PositionLocationRepository.cs`
  - `UpdateAsync()` - Positions 表

- ✅ `Data/SkillRepository.cs`
  - `UpdateAsync()` - Skills 表

- ✅ `Data/SchedulingTemplateRepository.cs`
  - `UpdateAsync()` - SchedulingTemplates 表
  - `IncrementUsageCountAsync()` - SchedulingTemplates 表

- ✅ `Data/DatabaseService.cs`
  - `SetDatabaseVersionAsync()` - DatabaseVersion 表

## 数据库字段类型

所有日期时间字段在数据库中都定义为 `TEXT` 类型，应该存储 ISO 8601 格式的字符串：

```sql
-- 示例：SingleShifts 表
CREATE TABLE IF NOT EXISTS SingleShifts (
    ...
    StartTime TEXT NOT NULL, -- ISO 8601
    EndTime TEXT NOT NULL,   -- ISO 8601
    ...
)
```

## ISO 8601 格式示例

正确的格式：
```
2024-12-01T14:30:00.0000000Z
2024-12-01T14:30:00+08:00
```

错误的格式（Ticks）：
```
639001713406909483
```

## 建议

### 短期方案（已实现）
- ✅ 添加兼容的解析方法，支持读取旧数据
- ✅ 确保所有新写入的数据使用正确格式

### 长期方案（可选）
1. **清理旧数据**：删除所有草稿排班，让用户重新创建
2. **数据迁移**：编写迁移脚本，将数据库中的 Ticks 格式转换为 ISO 8601 格式
3. **数据验证**：在应用启动时检查数据库中的日期格式，自动修复异常数据

## 测试数据生成器验证

已验证测试数据生成器中的日期时间处理：

### ✅ 正确使用 DateTime.UtcNow
- `TestData/TestDataGenerator.cs` - 使用 `DateTime.UtcNow` 生成时间戳
- `TestData/Generators/ManualAssignmentGenerator.cs` - 使用 `DateTime.UtcNow` 作为基准日期
- `TestData/Generators/PositionGenerator.cs` - 使用 `DateTime.UtcNow` 生成 CreatedAt/UpdatedAt

### ✅ 数据流正确
1. 测试数据生成器生成 DTO 对象（包含 DateTime 类型的字段）
2. DTO 通过 `DataImportExportService` 导入
3. `DataMappingService` 将 DTO 映射为 Model 对象
4. Repository 层使用 `.ToString("o")` 将 DateTime 转换为 ISO 8601 格式写入数据库

### ✅ 无需修改
测试数据生成器的代码已经是正确的，不需要任何修改。所有日期时间字段都使用标准的 `DateTime` 类型，在序列化和存储时会自动转换为 ISO 8601 格式。

## 测试建议

1. 测试打开包含旧数据的排班结果页面
2. 测试创建新的排班并查看结果
3. 测试确认草稿并查看历史记录
4. 测试手动指定功能的日期处理
5. 测试导入测试数据（验证日期格式正确）
6. 测试生成新的测试数据并导入

## 相关文件

### 核心修复
- `Data/SchedulingRepository.cs` - 主要修复（添加 ParseDateTime 方法）
- `Data/ConstraintRepository.cs` - 辅助修复（添加 ParseDateTime 方法）

### 数据库层
- `Data/DatabaseSchema.cs` - 数据库结构定义

### DTO 层
- `DTOs/ScheduleDto.cs` - 排班数据传输对象定义
- `DTOs/ManualAssignmentDto.cs` - 手动指定数据传输对象定义

### 测试数据生成器（已验证，无需修改）
- `TestData/TestDataGenerator.cs` - 测试数据生成器主类
- `TestData/Generators/ManualAssignmentGenerator.cs` - 手动指定生成器
- `TestData/Generators/PositionGenerator.cs` - 哨位生成器
- `Services/ImportExport/DataMappingService.cs` - DTO 与 Model 映射服务
