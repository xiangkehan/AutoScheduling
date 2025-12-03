# DateTime Ticks 格式 Bug 修复

## 问题描述

在点击"查看详细结果"按钮时出现错误：

```
加载排班数据失败
详细信息：
String '639003438244105694' was not recognized as a valid DateTime.
```

## 根本原因

`History/HIstoryManagement.cs` 中的 `UpdateBufferScheduleAsync` 方法在更新缓冲区记录时，错误地使用了 `DateTime.UtcNow.Ticks` 而不是 ISO 8601 格式字符串：

```csharp
// ❌ 错误代码（第 368 行）
cmd.Parameters.AddWithValue("@createTime", DateTime.UtcNow.Ticks);
```

这导致 `BufferSchedules` 表的 `CreateTime` 字段存储了 Ticks 格式的数据（如 `639003438244105694`），而在读取时使用 `DateTime.Parse()` 无法解析这种格式。

## 修复方案

### 1. 修复写入格式

在 `UpdateBufferScheduleAsync` 方法中，将 Ticks 格式改为 ISO 8601 格式：

```csharp
// ✅ 正确代码
cmd.Parameters.AddWithValue("@createTime", DateTime.UtcNow.ToString("o"));
```

### 2. 添加兼容解析

在 `GetAllBufferSchedulesAsync` 方法中，使用新的 `ParseDateTime` 方法替代 `DateTime.Parse`：

```csharp
// ✅ 兼容解析
DateTime createTime = ParseDateTime(reader.GetString(2));
```

### 3. 添加 ParseDateTime 辅助方法

新增 `ParseDateTime` 方法，支持 ISO 8601 格式和 Ticks 格式（兼容旧数据）：

```csharp
/// <summary>
/// 解析日期时间字符串，支持 ISO 8601 格式和 Ticks 格式（兼容旧数据）
/// </summary>
private DateTime ParseDateTime(string value)
{
    // 尝试解析为 ISO 8601 格式
    if (DateTime.TryParse(value, out var dateTime))
    {
        return dateTime.ToUniversalTime();
    }

    // 尝试解析为 Ticks 格式（兼容旧数据）
    if (long.TryParse(value, out var ticks))
    {
        try
        {
            return new DateTime(ticks, DateTimeKind.Utc);
        }
        catch
        {
            throw new FormatException($"无法将字符串 '{value}' 解析为有效的日期时间。");
        }
    }

    throw new FormatException($"无法将字符串 '{value}' 解析为有效的日期时间。");
}
```

## 修复的文件

- `History/HIstoryManagement.cs`
  - 修复 `UpdateBufferScheduleAsync` 方法（第 368 行）
  - 修复 `GetAllBufferSchedulesAsync` 方法（第 189 行）
  - 新增 `ParseDateTime` 方法

## 测试建议

1. ✅ 编译项目（已通过）
2. 运行应用程序
3. 在草稿箱页面点击"查看详细结果"按钮
4. 验证能够正常加载排班数据，不再出现错误

## 数据库清理（可选）

如果需要清理旧的 Ticks 格式数据，可以执行以下 SQL：

```sql
-- 查看当前的 Ticks 格式数据
SELECT Id, ScheduleId, CreateTime FROM BufferSchedules WHERE CreateTime NOT LIKE '%-%';

-- 更新为 ISO 8601 格式（需要在应用程序中执行，因为需要转换 Ticks）
-- 或者直接删除旧草稿，让用户重新创建
DELETE FROM BufferSchedules WHERE CreateTime NOT LIKE '%-%';
```

## 相关文档

- `DATETIME_FORMAT_FIX.md` - 之前修复的 SingleShifts 表日期时间格式问题
- `Data/SchedulingRepository.cs` - SingleShifts 表的 ParseDateTime 实现
- `Data/ConstraintRepository.cs` - ManualAssignments 表的 ParseDateTime 实现

## 总结

此次修复确保了：
1. 新写入的数据使用正确的 ISO 8601 格式
2. 能够兼容读取旧的 Ticks 格式数据
3. 用户可以正常查看排班详细结果，不会再出现解析错误
