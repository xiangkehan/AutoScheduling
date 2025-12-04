---
inclusion: always
---
# 开发规范

## 1. 代码组织

- 单个文件最大 1000 行，推荐 300-600 行
- 超过限制必须拆分：ViewModel 用 partial class，Service 拆分为专门服务，View 拆分为 UserControl
- 实现功能前提供：文件结构设计（新增/修改/删除 + 预估行数）、目录结构检查、依赖关系说明
- 类职责单一，方法不超过 50 行
- UI 设计时使用 ASCII 字符绘制布局草稿

## 2. 日期时间格式

- **存储格式**：ISO 8601 (`.ToString("o")`)，禁止 Ticks
- **时区策略**：数据库统一存储 UTC，UI 显示本地时间
- **字段类型**：SQLite 使用 `TEXT NOT NULL`
- **Kind 处理**：明确指定 DateTimeKind，避免 Unspecified

### 存储规则

```csharp
// 写入：转换为UTC后存储
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

cmd.Parameters.AddWithValue("@time", ToUtc(dateTime).ToString("o"));
```

### 读取规则

```csharp
// 读取：从UTC转换为本地时间
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
        return new DateTime(ticks, DateTimeKind.Local);
    }
    
    throw new FormatException($"无法解析日期时间: '{value}'");
}

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
```

### 关键点

1. **存储前转换**：所有写入数据库的时间必须先转换为 UTC
2. **读取后转换**：所有从数据库读取的时间必须转换为本地时间
3. **处理 Unspecified**：
   - 存储时：Unspecified 视为本地时间
   - 读取时：Unspecified 视为 UTC 时间
4. **生成时明确 Kind**：创建 DateTime 时使用 `DateTime.SpecifyKind()` 明确指定类型
5. **兼容旧数据**：保留 Ticks 格式解析，假定为本地时间

## 3. 语言使用

- 交流、注释、文档、Git 提交消息：中文
- 变量名、函数名、类名：英文（C# 命名约定）
- UI 文本、错误消息：中文
- Git 提交格式：`feat: 添加功能` 或 `fix: 修复bug`

## 4. 命名约定

- 类/接口/方法/属性：PascalCase（接口加 I 前缀）
- 私有字段：_camelCase
- 参数/局部变量：camelCase
- 异步方法：必须 `Async` 后缀
- MVVM：`{Name}ViewModel`、`I{Name}Service`、`{Name}Dto`

## 5. 依赖注入

注册顺序：Repositories → Mappers → Business Services → Helper Services → ViewModels
- `DatabaseService` 必须先 `InitializeAsync()`
- 避免循环依赖，优先构造函数注入

## 6. 数据流

View → ViewModel → Service → Repository → DatabaseService

## 7. 错误处理

- 使用具体异常类型，提供中文错误消息
- 验证层级：ViewModel（输入）→ Service（业务规则）→ Repository（数据完整性）

## 8. 性能优化

- 数据库：使用 async/await，避免 N+1 查询，适当使用事务和缓存
- UI：长操作用后台线程，提供进度反馈，使用节流和虚拟化
