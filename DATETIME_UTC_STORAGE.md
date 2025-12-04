# UTC 时间存储方案

## 概述

本文档说明如何在数据库中正确存储和读取 UTC 时间，避免时区转换问题。

## 核心原则

1. **数据库存储 UTC**：所有时间统一以 UTC 格式存储
2. **UI 显示本地时间**：用户界面显示本地时间
3. **明确 DateTimeKind**：确保每个 DateTime 对象都有正确的 Kind 标记

## 实现方案

### 1. 存储时转换为 UTC

```csharp
/// <summary>
/// 将DateTime转换为UTC时间
/// 处理Unspecified类型，将其视为本地时间
/// </summary>
private DateTime ToUtc(DateTime dateTime)
{
    switch (dateTime.Kind)
    {
        case DateTimeKind.Utc:
            return dateTime;
        case DateTimeKind.Local:
            return dateTime.ToUniversalTime();
        case DateTimeKind.Unspecified:
            // Unspecified视为本地时间
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
        default:
            return dateTime.ToUniversalTime();
    }
}

// 使用示例
cmd.Parameters.AddWithValue("@time", ToUtc(dateTime).ToString("o"));
```

### 2. 读取时转换为本地时间

```csharp
/// <summary>
/// 将UTC时间转换为本地时间
/// 处理Unspecified类型，将其视为UTC时间
/// </summary>
private DateTime ToLocal(DateTime dateTime)
{
    switch (dateTime.Kind)
    {
        case DateTimeKind.Local:
            return dateTime;
        case DateTimeKind.Utc:
            return dateTime.ToLocalTime();
        case DateTimeKind.Unspecified:
            // Unspecified视为UTC时间
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();
        default:
            return dateTime.ToLocalTime();
    }
}

/// <summary>
/// 解析日期时间字符串，支持 ISO 8601 格式和 Ticks 格式（兼容旧数据）
/// 数据库存储的是UTC时间，读取后转换为本地时间
/// </summary>
private DateTime ParseDateTime(string value)
{
    // 尝试解析为 ISO 8601 格式
    if (DateTime.TryParse(value, out var dateTime))
    {
        // 数据库存储的是UTC时间，转换为本地时间
        return ToLocal(dateTime);
    }
    
    // 兼容旧 Ticks 格式
    if (long.TryParse(value, out var ticks))
    {
        // 旧数据使用Ticks存储，假定为本地时间
        return new DateTime(ticks, DateTimeKind.Local);
    }
    
    throw new FormatException($"无法将字符串 '{value}' 解析为有效的日期时间。");
}
```

### 3. 生成时明确指定 Kind

在算法生成时间时，使用 `DateTime.SpecifyKind()` 明确指定类型：

```csharp
// 在 Individual.ToSchedule 中
var startTime = DateTime.SpecifyKind(
    schedule.StartDate.Date.AddHours(period * 2),
    DateTimeKind.Local
);
```

## 数据流

```
生成时间 (Local)
    ↓
ToUtc() 转换
    ↓
数据库存储 (UTC, ISO 8601)
    ↓
ParseDateTime() 读取
    ↓
ToLocal() 转换
    ↓
UI 显示 (Local)
```

## 关键点

### 1. Unspecified 处理策略

- **存储时**：Unspecified 视为本地时间，转换为 UTC
- **读取时**：Unspecified 视为 UTC 时间，转换为本地时间

这个策略确保了即使 DateTimeKind 不明确，也能正确处理时区转换。

### 2. 兼容旧数据

保留 Ticks 格式解析，假定为本地时间：

```csharp
if (long.TryParse(value, out var ticks))
{
    return new DateTime(ticks, DateTimeKind.Local);
}
```

### 3. ISO 8601 格式

使用 `.ToString("o")` 生成 ISO 8601 格式：

```
2024-12-04T10:30:00.0000000+08:00  // 本地时间
2024-12-04T02:30:00.0000000Z       // UTC 时间
```

## 常见问题

### Q1: 为什么不直接使用 ToUniversalTime()?

**A**: 因为 `ToUniversalTime()` 对 `Unspecified` 类型的处理不明确，可能导致错误的转换。使用 `DateTime.SpecifyKind()` 明确指定类型更安全。

### Q2: 如何处理跨时区的数据？

**A**: 数据库统一存储 UTC 时间，每个客户端根据本地时区转换显示。这样可以确保不同时区的用户看到正确的时间。

### Q3: 如何验证时间是否正确？

**A**: 
1. 检查数据库中的时间是否为 UTC（通常带 Z 后缀）
2. 检查 UI 显示的时间是否为本地时间
3. 验证时间差是否等于本地时区偏移量（如 UTC+8 应该相差 8 小时）

## 修改的文件

- `Data/SchedulingRepository.cs`：添加 `ToUtc()` 和 `ToLocal()` 方法，修改所有时间存储和读取逻辑
- `SchedulingEngine/Core/Individual.cs`：使用 `DateTime.SpecifyKind()` 明确指定生成时间为本地时间
- `.kiro/steering/development-standards.md`：更新日期时间格式规范

## 测试建议

1. **时区测试**：在不同时区测试时间显示是否正确
2. **跨天测试**：测试跨越午夜的时间是否正确
3. **夏令时测试**：在夏令时切换前后测试时间是否正确
4. **旧数据兼容**：测试旧 Ticks 格式数据是否能正确读取

## 参考资料

- [DateTime.Kind Property](https://docs.microsoft.com/en-us/dotnet/api/system.datetime.kind)
- [ISO 8601 Date and Time Format](https://en.wikipedia.org/wiki/ISO_8601)
- [Best Practices for DateTime](https://docs.microsoft.com/en-us/dotnet/standard/datetime/best-practices)
