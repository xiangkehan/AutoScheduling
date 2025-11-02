# API参考

<cite>
**本文档中引用的文件**  
- [IApplicationService.cs](file://Services/Interfaces/IApplicationService.cs)
- [IConfigurationService.cs](file://Services/Interfaces/IConfigurationService.cs)
- [ISchedulingService.cs](file://Services/Interfaces/ISchedulingService.cs)
- [ScheduleDto.cs](file://DTOs/ScheduleDto.cs)
- [PersonnelDto.cs](file://DTOs/PersonnelDto.cs)
- [PositionDto.cs](file://DTOs/PositionDto.cs)
- [SchedulingTemplateDto.cs](file://DTOs/SchedulingTemplateDto.cs)
- [SkillDto.cs](file://DTOs/SkillDto.cs)
- [FixedAssignmentDto.cs](file://DTOs/FixedAssignmentDto.cs)
- [HolidayConfigDto.cs](file://DTOs/HolidayConfigDto.cs)
- [ManualAssignmentDto.cs](file://DTOs/ManualAssignmentDto.cs)
- [ScheduleStatisticsDto.cs](file://DTOs/ScheduleStatisticsDto.cs)
- [HolidayConfig.cs](file://Models/Constraints/HolidayConfig.cs)
- [FixedPositionRule.cs](file://Models/Constraints/FixedPositionRule.cs)
- [ManualAssignment.cs](file://Models/Constraints/ManualAssignment.cs)
</cite>

## 目录
1. [简介](#简介)
2. [核心服务接口](#核心服务接口)
   - [IApplicationService](#iapplicationservice)
   - [IConfigurationService](#iconfigurationservice)
   - [ISchedulingService](#ischedulingservice)
3. [数据传输对象（DTO）](#数据传输对象dto)
   - [排班相关DTO](#排班相关dto)
   - [人员与哨位DTO](#人员与哨位dto)
   - [约束配置DTO](#约束配置dto)
   - [统计与模板DTO](#统计与模板dto)
4. [模型对象](#模型对象)
5. [使用示例](#使用示例)
6. [异常处理](#异常处理)

## 简介
本文档为AutoScheduling3系统提供完整的API参考，涵盖所有公共服务接口和数据传输对象。文档详细描述了`IApplicationService`、`IConfigurationService`、`ISchedulingService`等核心服务接口的每个方法，包括参数类型、返回值、异常情况和使用场景。同时，全面记录了`DTOs`目录中所有数据传输对象的结构、字段含义及验证规则，为开发者提供精确的编程接口参考，支持代码补全和正确调用。

## 核心服务接口

### IApplicationService
应用程序服务基础接口，定义所有服务的生命周期管理方法。

**方法列表：**

| 方法名 | 参数 | 返回值 | 描述 |
|-------|------|--------|------|
| `InitializeAsync` | 无 | `Task` | 异步初始化服务，应在应用程序启动时调用。负责加载依赖项、建立连接等初始化操作。 |
| `CleanupAsync` | 无 | `Task` | 异步清理服务资源，应在应用程序关闭时调用。负责释放资源、断开连接等清理操作。 |

**Section sources**
- [IApplicationService.cs](file://Services/Interfaces/IApplicationService.cs#L7-L18)

### IConfigurationService
配置服务接口，继承自`IApplicationService`，提供应用程序配置的读写管理功能。

**方法列表：**

| 方法名 | 参数 | 返回值 | 描述 |
|-------|------|--------|------|
| `GetValue<T>` | `string key`, `T defaultValue` | `T` | 根据配置键获取指定类型的配置值。若键不存在则返回默认值。支持泛型类型推断。 |
| `SetValueAsync<T>` | `string key`, `T value` | `Task` | 异步设置指定键的配置值。值将被序列化并持久化存储。 |
| `ContainsKey` | `string key` | `bool` | 检查指定配置键是否存在。用于在获取值前进行存在性验证。 |
| `RemoveAsync` | `string key` | `Task` | 异步移除指定的配置项。若键不存在则无操作。 |
| `ClearAsync` | 无 | `Task` | 异步清空所有配置项。谨慎使用，将删除所有用户设置。 |

**Section sources**
- [IConfigurationService.cs](file://Services/Interfaces/IConfigurationService.cs#L7-L43)

### ISchedulingService
排班服务接口，提供核心排班功能、历史管理、约束配置加载和排班引擎集成。

**方法列表：**

| 方法名 | 参数 | 返回值 | 描述 |
|-------|------|--------|------|
| `ExecuteSchedulingAsync` | `SchedulingRequestDto request`, `CancellationToken cancellationToken` | `Task<ScheduleDto>` | 执行排班算法，根据请求参数生成排班方案。支持取消操作。 |
| `GetDraftsAsync` | 无 | `Task<List<ScheduleSummaryDto>>` | 获取所有草稿排班的摘要列表，按创建时间倒序排列。 |
| `GetScheduleByIdAsync` | `int id` | `Task<ScheduleDto?>` | 根据ID获取排班详情，支持草稿和历史排班。ID不存在时返回null。 |
| `ConfirmScheduleAsync` | `int id` | `Task` | 确认指定ID的草稿排班，将其转为历史记录。确认后不可修改。 |
| `DeleteDraftAsync` | `int id` | `Task` | 删除指定ID的草稿排班。仅草稿可删除，历史排班受保护。 |
| `GetHistoryAsync` | `DateTime? startDate`, `DateTime? endDate` | `Task<List<ScheduleSummaryDto>>` | 获取历史排班摘要列表，支持按日期范围过滤。 |
| `ExportScheduleAsync` | `int id`, `string format` | `Task<byte[]>` | 导出指定ID的排班表，支持多种格式（如PDF、Excel）。返回文件字节流。 |
| `GetHolidayConfigsAsync` | 无 | `Task<List<HolidayConfig>>` | 获取全部休息日配置列表，用于前端向导展示和选择。 |
| `GetFixedPositionRulesAsync` | `bool enabledOnly` | `Task<List<FixedPositionRule>>` | 获取定岗规则列表，可选择仅返回启用的规则。 |
| `GetManualAssignmentsAsync` | `DateTime startDate`, `DateTime endDate`, `bool enabledOnly` | `Task<List<ManualAssignment>>` | 获取指定日期范围内的手动指定列表，支持启用状态过滤。 |
| `GetSchedulingEngineStatusAsync` | 无 | `Task<Dictionary<string, object>>` | 获取排班引擎的实时状态信息，用于监控和调试。 |
| `GetScheduleStatisticsAsync` | 无 | `Task<ScheduleStatisticsDto>` | 获取系统级排班统计信息，包括覆盖率、班次分布等。 |
| `ConfirmMultipleSchedulesAsync` | `List<int> scheduleIds` | `Task` | 批量确认多个草稿排班，提高操作效率。 |
| `CleanupExpiredDraftsAsync` | `int daysToKeep` | `Task` | 清理过期的草稿排班，默认保留7天内的草稿。 |

**Section sources**
- [ISchedulingService.cs](file://Services/Interfaces/ISchedulingService.cs#L12-L87)

## 数据传输对象（DTO）

### 排班相关DTO
描述排班方案、请求和冲突信息的数据结构。

#### ScheduleDto
排班表数据传输对象，包含完整的排班方案。

| 字段名 | 类型 | 必填 | 验证规则 | 描述 |
|-------|------|------|----------|------|
| `id` | `int` | 否 | 大于等于0 | 排班表ID，新建时为0，保存后由系统分配。 |
| `title` | `string` | 是 | 1-100字符 | 排班表名称，用于标识和显示。 |
| `personnelIds` | `List<int>` | 是 | 至少1人 | 参与本次排班的人员ID列表。 |
| `positionIds` | `List<int>` | 是 | 至少1个 | 参与本次排班的哨位ID列表。 |
| `shifts` | `List<ShiftDto>` | 是 | 至少1个 | 生成的单次班次列表，构成完整排班方案。 |
| `conflicts` | `List<ConflictDto>` | 否 | 无 | 排班过程中检测到的冲突/约束提示集合。 |
| `createdAt` | `DateTime` | 是 | 不能为空 | 排班表创建时间。 |
| `confirmedAt` | `DateTime?` | 否 | 无 | 确认时间，草稿状态时为null。 |
| `startDate` | `DateTime` | 是 | 不能为空 | 排班周期的开始日期。 |
| `endDate` | `DateTime` | 是 | 不能为空 | 排班周期的结束日期。 |

**Section sources**
- [ScheduleDto.cs](file://DTOs/ScheduleDto.cs#L7-L324)

#### ShiftDto
单次班次数据传输对象，表示一个人员在特定哨位的单次执勤。

| 字段名 | 类型 | 必填 | 验证规则 | 描述 |
|-------|------|------|----------|------|
| `id` | `int` | 否 | 大于等于0 | 班次ID，新建时为0。 |
| `scheduleId` | `int` | 是 | 大于0 | 所属排班表ID。 |
| `positionId` | `int` | 是 | 大于0 | 执勤哨位ID。 |
| `positionName` | `string` | 否 | 无 | 哨位名称，冗余字段便于前端显示。 |
| `personnelId` | `int` | 是 | 大于0 | 执勤人员ID。 |
| `personnelName` | `string` | 否 | 无 | 人员姓名，冗余字段便于前端显示。 |
| `startTime` | `DateTime` | 是 | 不能为空 | 班次开始时间。 |
| `endTime` | `DateTime` | 是 | 不能为空 | 班次结束时间。 |
| `periodIndex` | `int` | 是 | 0-11 | 时段索引，对应一天中的12个固定时段。 |

**Section sources**
- [ScheduleDto.cs](file://DTOs/ScheduleDto.cs#L326-L324)

#### SchedulingRequestDto
排班请求数据传输对象，用于启动排班算法。

| 字段名 | 类型 | 必填 | 验证规则 | 描述 |
|-------|------|------|----------|------|
| `title` | `string` | 是 | 1-100字符 | 排班表名称。 |
| `startDate` | `DateTime` | 是 | 不早于今天 | 排班周期开始日期。 |
| `endDate` | `DateTime` | 是 | 不早于开始日期 | 排班周期结束日期。 |
| `personnelIds` | `List<int>` | 是 | 至少1人 | 参与排班的人员ID列表。 |
| `positionIds` | `List<int>` | 是 | 至少1个 | 参与排班的哨位ID列表。 |
| `useActiveHolidayConfig` | `bool` | 否 | 默认true | 是否使用当前活动的休息日配置。 |
| `enabledFixedRuleIds` | `List<int>?` | 否 | 无 | 启用的定岗规则ID列表。 |
| `enabledManualAssignmentIds` | `List<int>?` | 否 | 无 | 启用的手动指定ID列表。 |
| `holidayConfigId` | `int?` | 否 | 无 | 指定使用的休息日配置ID。 |

**Section sources**
- [ScheduleDto.cs](file://DTOs/ScheduleDto.cs#L326-L324)

#### ConflictDto
冲突/约束提示数据传输对象，用于向用户展示排班问题。

| 字段名 | 类型 | 必填 | 验证规则 | 描述 |
|-------|------|------|----------|------|
| `type` | `string` | 是 | hard/soft/info/unassigned | 冲突类型：硬约束、软约束、信息、未分配。 |
| `message` | `string` | 是 | 1-500字符 | 冲突描述信息。 |
| `positionId` | `int?` | 否 | 无 | 相关哨位ID。 |
| `personnelId` | `int?` | 否 | 无 | 相关人员ID。 |
| `startTime` | `DateTime?` | 否 | 无 | 相关开始时间。 |
| `endTime` | `DateTime?` | 否 | 无 | 相关结束时间。 |
| `periodIndex` | `int?` | 否 | 0-11 | 相关时段索引。 |

**Section sources**
- [ScheduleDto.cs](file://DTOs/ScheduleDto.cs#L326-L324)

### 人员与哨位DTO
描述人员、哨位及其技能的数据结构。

#### PersonnelDto
人员数据传输对象，包含人员基本信息和状态。

| 字段名 | 类型 | 必填 | 验证规则 | 描述 |
|-------|------|------|----------|------|
| `id` | `int` | 否 | 大于等于0 | 人员ID。 |
| `name` | `string` | 是 | 1-50字符 | 人员姓名。 |
| `positionId` | `int` | 是 | 大于0 | 所属职位ID。 |
| `positionName` | `string` | 否 | 无 | 职位名称，冗余字段。 |
| `skillIds` | `List<int>` | 是 | 无 | 拥有的技能ID列表。 |
| `skillNames` | `List<string>` | 否 | 无 | 技能名称列表，冗余字段。 |
| `isAvailable` | `bool` | 否 | 默认true | 是否可用（在职且可排班）。 |
| `isRetired` | `bool` | 否 | 默认false | 是否已退役。 |
| `recentShiftIntervalCount` | `int` | 否 | 0-999 | 最近班次间隔计数。 |
| `recentHolidayShiftIntervalCount` | `int` | 否 | 0-999 | 最近节假日班次间隔计数。 |
| `recentPeriodShiftIntervals` | `int[]` | 是 | 长度12 | 各时段班次间隔计数数组（12个时段）。 |
| `isActive` | `bool` | 否 | 计算属性 | 是否在职且可用（只读属性）。 |

**Section sources**
- [PersonnelDto.cs](file://DTOs/PersonnelDto.cs#L7-L189)

#### PositionDto
哨位/职位数据传输对象，定义哨位的基本信息和要求。

| 字段名 | 类型 | 必填 | 验证规则 | 描述 |
|-------|------|------|----------|------|
| `id` | `int` | 否 | 大于等于0 | 哨位ID。 |
| `name` | `string` | 是 | 1-100字符 | 哨位名称。 |
| `location` | `string` | 是 | 1-200字符 | 哨位地点。 |
| `description` | `string?` | 否 | 最多500字符 | 哨位介绍。 |
| `requirements` | `string?` | 否 | 最多1000字符 | 哨位要求说明。 |
| `requiredSkillIds` | `List<int>` | 是 | 无 | 所需技能ID列表。 |
| `requiredSkillNames` | `List<string>` | 否 | 无 | 所需技能名称列表，冗余字段。 |

**Section sources**
- [PositionDto.cs](file://DTOs/PositionDto.cs#L7-L138)

#### SkillDto
技能数据传输对象，定义系统中的技能类型。

| 字段名 | 类型 | 必填 | 验证规则 | 描述 |
|-------|------|------|----------|------|
| `id` | `int` | 否 | 大于等于0 | 技能ID。 |
| `name` | `string` | 是 | 1-50字符 | 技能名称。 |
| `description` | `string?` | 否 | 最多200字符 | 技能描述。 |
| `isActive` | `bool` | 否 | 默认true | 是否激活（可用）。 |
| `createdAt` | `DateTime` | 否 | 无 | 创建时间。 |
| `updatedAt` | `DateTime` | 否 | 无 | 更新时间。 |

**Section sources**
- [SkillDto.cs](file://DTOs/SkillDto.cs#L7-L90)

### 约束配置DTO
描述排班约束规则的数据结构。

#### FixedAssignmentDto
定岗要求数据传输对象，定义人员的固定排班规则。

| 字段名 | 类型 | 必填 | 验证规则 | 描述 |
|-------|------|------|----------|------|
| `id` | `int` | 否 | 大于等于0 | 规则ID。 |
| `personnelId` | `int` | 是 | 大于0 | 关联人员ID。 |
| `personnelName` | `string` | 否 | 无 | 人员姓名，冗余字段。 |
| `allowedPositionIds` | `List<int>` | 是 | 至少1个 | 允许上哨的哨位ID列表。 |
| `allowedPositionNames` | `List<string>` | 否 | 无 | 允许哨位名称列表，冗余字段。 |
| `allowedTimeSlots` | `List<int>` | 是 | 至少1个，0-11 | 允许上哨的时段索引列表。 |
| `startDate` | `DateTime` | 是 | 不能为空 | 规则生效开始日期。 |
| `endDate` | `DateTime` | 是 | 不能为空 | 规则生效结束日期。 |
| `isEnabled` | `bool` | 否 | 默认true | 规则是否启用。 |
| `ruleName` | `string` | 是 | 1-100字符 | 规则名称。 |
| `description` | `string?` | 否 | 最多500字符 | 规则描述。 |
| `createdAt` | `DateTime` | 是 | 不能为空 | 创建时间。 |
| `updatedAt` | `DateTime` | 否 | 无 | 更新时间。 |

**Section sources**
- [FixedAssignmentDto.cs](file://DTOs/FixedAssignmentDto.cs#L7-L280)

#### HolidayConfigDto
休息日配置数据传输对象，定义如何判定休息日。

| 字段名 | 类型 | 必填 | 验证规则 | 描述 |
|-------|------|------|----------|------|
| `id` | `int` | 否 | 大于等于0 | 配置ID。 |
| `configName` | `string` | 是 | 1-100字符 | 配置名称。 |
| `enableWeekendRule` | `bool` | 否 | 默认true | 是否启用周末规则。 |
| `weekendDays` | `List<DayOfWeek>` | 否 | 无 | 周末日期列表（如周六、周日）。 |
| `legalHolidays` | `List<DateTime>` | 否 | 无 | 法定节假日列表。 |
| `customHolidays` | `List<DateTime>` | 否 | 无 | 自定义休息日列表。 |
| `excludedDates` | `List<DateTime>` | 否 | 无 | 排除日期列表（强制为工作日）。 |
| `isActive` | `bool` | 否 | 默认true | 是否为当前启用配置。 |
| `createdAt` | `DateTime` | 是 | 不能为空 | 创建时间。 |
| `updatedAt` | `DateTime` | 否 | 无 | 更新时间。 |

**Section sources**
- [HolidayConfigDto.cs](file://DTOs/HolidayConfigDto.cs#L7-L200)

#### ManualAssignmentDto
手动指定数据传输对象，定义预先固定的人员-哨位-时段分配。

| 字段名 | 类型 | 必填 | 验证规则 | 描述 |
|-------|------|------|----------|------|
| `id` | `int` | 否 | 大于等于0 | 指定ID。 |
| `positionId` | `int` | 是 | 大于0 | 哨位ID。 |
| `positionName` | `string` | 否 | 无 | 哨位名称，冗余字段。 |
| `timeSlot` | `int` | 是 | 0-11 | 时段索引。 |
| `personnelId` | `int` | 是 | 大于0 | 人员ID。 |
| `personnelName` | `string` | 否 | 无 | 人员姓名，冗余字段。 |
| `date` | `DateTime` | 是 | 不能为空 | 指定日期。 |
| `isEnabled` | `bool` | 否 | 默认true | 是否启用。 |
| `remarks` | `string?` | 否 | 最多200字符 | 备注说明。 |
| `createdAt` | `DateTime` | 是 | 不能为空 | 创建时间。 |
| `updatedAt` | `DateTime` | 否 | 无 | 更新时间。 |

**Section sources**
- [ManualAssignmentDto.cs](file://DTOs/ManualAssignmentDto.cs#L7-L182)

### 统计与模板DTO
描述排班统计信息和模板的数据结构。

#### ScheduleStatisticsDto
排班统计数据传输对象，包含系统级统计信息。

| 字段名 | 类型 | 必填 | 验证规则 | 描述 |
|-------|------|------|----------|------|
| `totalShifts` | `int` | 否 | 非负数 | 总班次数。 |
| `averageShiftsPerPerson` | `double` | 否 | 非负数 | 人均班次数。 |
| `weekendShifts` | `int` | 否 | 非负数 | 周末班次数。 |
| `shiftsByTimeOfDay` | `Dictionary<string, int>` | 是 | 不能为空 | 按时段分布的班次统计。 |
| `shiftsPerPerson` | `Dictionary<string, int>` | 是 | 不能为空 | 按人员分布的班次统计。 |
| `positionCoverage` | `double` | 否 | 0.0-1.0 | 哨位覆盖率。 |
| `totalSchedules` | `int` | 否 | 非负数 | 总排班数。 |
| `confirmedSchedules` | `int` | 否 | 非负数 | 已确认排班数。 |
| `draftSchedules` | `int` | 否 | 非负数 | 草稿排班数。 |
| `personnelShiftCounts` | `Dictionary<int, int>` | 是 | 不能为空 | 人员班次计数字典（ID -> 次数）。 |
| `timeSlotDistribution` | `Dictionary<int, double>` | 是 | 不能为空 | 时段分布字典（时段索引 -> 占比）。 |

**Section sources**
- [ScheduleStatisticsDto.cs](file://DTOs/ScheduleStatisticsDto.cs#L7-L89)

#### SchedulingTemplateDto
排班模板数据传输对象，用于保存和复用排班配置。

| 字段名 | 类型 | 必填 | 验证规则 | 描述 |
|-------|------|------|----------|------|
| `id` | `int` | 否 | 大于等于0 | 模板ID。 |
| `name` | `string` | 是 | 1-100字符 | 模板名称。 |
| `description` | `string?` | 否 | 最多500字符 | 模板描述。 |
| `templateType` | `string` | 是 | regular/holiday/special | 模板类型。 |
| `isDefault` | `bool` | 否 | 默认false | 是否为默认模板。 |
| `personnelIds` | `List<int>` | 是 | 至少1人 | 参与人员ID列表。 |
| `positionIds` | `List<int>` | 是 | 至少1个 | 参与哨位ID列表。 |
| `holidayConfigId` | `int?` | 否 | 无 | 关联的休息日配置ID。 |
| `useActiveHolidayConfig` | `bool` | 否 | 默认false | 是否使用当前活动配置。 |
| `enabledFixedRuleIds` | `List<int>` | 否 | 无 | 启用的定岗规则ID列表。 |
| `enabledManualAssignmentIds` | `List<int>` | 否 | 无 | 启用的手动指定ID列表。 |
| `createdAt` | `DateTime` | 是 | 不能为空 | 创建时间。 |
| `lastUsedAt` | `DateTime?` | 否 | 无 | 最后使用时间。 |
| `usageCount` | `int` | 否 | 非负数 | 使用次数。 |

**Section sources**
- [SchedulingTemplateDto.cs](file://DTOs/SchedulingTemplateDto.cs#L7-L321)

## 模型对象
描述系统内部使用的领域模型对象。

### HolidayConfig
休息日配置模型，定义如何判定休息日的业务规则。

**属性：**
- `Id`: 数据库主键。
- `ConfigName`: 配置名称。
- `EnableWeekendRule`: 是否启用周末规则。
- `WeekendDays`: 周末日期列表。
- `LegalHolidays`: 法定节假日列表。
- `CustomHolidays`: 自定义休息日列表。
- `ExcludedDates`: 排除日期列表（强制为工作日）。
- `IsActive`: 是否为当前启用配置。
- `IsHoliday(DateTime date)`: 判定指定日期是否为休息日的方法，遵循优先级规则。

**Section sources**
- [HolidayConfig.cs](file://Models/Constraints/HolidayConfig.cs#L7-L88)

### FixedPositionRule
定岗规则模型，定义人员的固定排班限制。

**属性：**
- `Id`: 数据库主键。
- `PersonalId`: 关联人员ID。
- `AllowedPositionIds`: 允许的哨位ID列表。
- `AllowedPeriods`: 允许的时段序号列表（0-11）。
- `IsEnabled`: 规则是否启用。
- `Description`: 规则描述。

**Section sources**
- [FixedPositionRule.cs](file://Models/Constraints/FixedPositionRule.cs#L7-L48)

### ManualAssignment
手动指定分配模型，表示预先固定的人员-哨位-时段分配。

**属性：**
- `Id`: 数据库主键。
- `PositionId`: 哨位ID。
- `PeriodIndex`: 时段序号（0-11）。
- `PersonalId`: 人员ID。
- `Date`: 指定日期。
- `IsEnabled`: 是否启用。
- `Remarks`: 备注。

**Section sources**
- [ManualAssignment.cs](file://Models/Constraints/ManualAssignment.cs#L7-L52)

## 使用示例
以下代码片段展示了如何使用核心API进行排班操作。

```csharp
// 1. 初始化服务
await applicationService.InitializeAsync();

// 2. 创建排班请求
var request = new SchedulingRequestDto
{
    Title = "国庆值班表",
    StartDate = new DateTime(2024, 10, 1),
    EndDate = new DateTime(2024, 10, 7),
    PersonnelIds = new List<int> { 1, 2, 3 },
    PositionIds = new List<int> { 101, 102 },
    UseActiveHolidayConfig = true
};

// 3. 执行排班
var schedule = await schedulingService.ExecuteSchedulingAsync(request);

// 4. 检查冲突
if (schedule.Conflicts.Any(c => c.Type == "hard"))
{
    // 处理硬约束冲突
    foreach (var conflict in schedule.Conflicts.Where(c => c.Type == "hard"))
    {
        Console.WriteLine($"硬约束冲突: {conflict.Message}");
    }
}
else
{
    // 确认排班
    await schedulingService.ConfirmScheduleAsync(schedule.Id);
}

// 5. 清理资源
await applicationService.CleanupAsync();
```

**Section sources**
- [ISchedulingService.cs](file://Services/Interfaces/ISchedulingService.cs#L12-L87)
- [ScheduleDto.cs](file://DTOs/ScheduleDto.cs#L7-L324)

## 异常处理
本系统通过返回值和异常机制处理错误情况。

**通用异常：**
- `ArgumentNullException`: 当必需参数为null时抛出。
- `ArgumentException`: 当参数值无效时抛出（如日期范围错误）。
- `InvalidOperationException`: 当操作状态不合法时抛出（如确认不存在的草稿）。
- `OperationCanceledException`: 当操作被`CancellationToken`取消时抛出。

**服务特定异常：**
- `ConfigurationNotFoundException`: 当请求的配置键不存在且无默认值时。
- `ScheduleNotFoundException`: 当请求的排班ID不存在时。
- `ValidationException`: 当DTO验证失败时，包含详细的验证错误信息。

**建议的异常处理模式：**
```csharp
try
{
    var schedule = await schedulingService.ExecuteSchedulingAsync(request);
    // 处理成功结果
}
catch (ValidationException ex)
{
    // 处理验证错误，向用户展示具体错误信息
    foreach (var error in ex.ValidationResults)
    {
        Console.WriteLine($"验证错误: {error.ErrorMessage}");
    }
}
catch (OperationCanceledException)
{
    // 处理用户取消操作
    Console.WriteLine("排班操作已取消");
}
catch (Exception ex)
{
    // 处理其他未预期异常
    Console.WriteLine($"操作失败: {ex.Message}");
}
```

**Section sources**
- [ISchedulingService.cs](file://Services/Interfaces/ISchedulingService.cs#L12-L87)
- [ScheduleDto.cs](file://DTOs/ScheduleDto.cs#L7-L324)