# 数据模型重构设计文档

## 概述

重构哨位排班系统的人员与哨位关系存储模式：从"人员存储职位ID"改为"哨位存储可用人员ID集合"，以提高排班算法查询效率。

**核心目标：**
- 提升查询效率：排班算法直接从哨位获取可用人员列表
- 优化数据模型：支持人员-哨位多对多关系
- 保持功能完整：确保现有功能正常工作

## 架构设计

### 核心变更

**Personal模型：** 移除 `PositionId` 字段
**PositionLocation模型：** 新增 `AvailablePersonnelIds` 字段（List<int>）
**数据库：** Personals表删除Position列，Positions表新增AvailablePersonnelIds列

## 组件和接口设计

### 数据模型层

#### Personal模型更新
```csharp
public class Personal
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // 移除：public int PositionId { get; set; }
    public List<int> SkillIds { get; set; } = new List<int>();
    public bool IsAvailable { get; set; } = true;
    public bool IsRetired { get; set; } = false;
    public int RecentShiftIntervalCount { get; set; } = 0;
    public int RecentHolidayShiftIntervalCount { get; set; } = 0;
    public int[] RecentPeriodShiftIntervals { get; set; } = new int[12];
    // ... 其他字段保持不变
}
```

#### PositionLocation模型更新
```csharp
public class PositionLocation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<int> RequiredSkillIds { get; set; } = new List<int>();
    // 新增：可用人员ID集合
    public List<int> AvailablePersonnelIds { get; set; } = new List<int>();
    // ... 其他字段保持不变
}
```

### 数据访问层

#### Repository接口更新

**IPersonalRepository：** 移除 `GetByPositionIdAsync` 等职位相关方法

**IPositionRepository：** 新增人员管理方法
- `AddAvailablePersonnelAsync(int positionId, int personnelId)`
- `RemoveAvailablePersonnelAsync(int positionId, int personnelId)`
- `GetAvailablePersonnelIdsAsync(int positionId)`
- `GetPositionsByPersonnelAsync(int personnelId)`

#### Repository实现要点

**PersonalRepository：**
- `CreateAsync` 方法移除Position参数
- `MapPerson` 方法移除PositionId映射

**PositionLocationRepository：**
- 实现人员管理方法（添加、移除、查询可用人员）
- `MapPosition` 方法新增AvailablePersonnelIds映射

### 业务逻辑层

#### DTO更新

**PersonnelDto：**
- 移除：`PositionId`、`PositionName` 字段
- 新增：`AvailablePositionIds`、`AvailablePositionNames` 字段

**PositionDto：**
- 新增：`AvailablePersonnelIds`、`AvailablePersonnelNames` 字段

#### 服务层更新

**PersonnelService：**
- 移除职位相关验证逻辑
- 新增 `GetAvailablePositionsAsync` 方法

**PositionService：**
- 新增人员管理方法：`AddAvailablePersonnelAsync`、`RemoveAvailablePersonnelAsync`、`GetAvailablePersonnelAsync`
- 实现技能匹配验证

**SchedulingService：**
- 使用新数据模型构建排班请求
- 验证哨位可用人员数据完整性

### 用户界面层更新

#### ViewModel更新

**PersonnelViewModel：**
- 移除职位相关属性
- 新增 `AvailablePositions` 集合显示可用哨位

**PositionViewModel：**
- 新增 `AvailablePersonnel` 和 `AllPersonnel` 集合
- 实现 `AddPersonnelCommand` 和 `RemovePersonnelCommand`

#### View更新

**PersonnelPage：**
- 移除职位选择控件
- 新增可用哨位显示区域

**PositionPage：**
- 新增可用人员管理界面
- 实现人员添加/移除功能

### 排班引擎更新

#### ConstraintValidator更新
- 验证人员是否在哨位可用人员列表中
- 使用新数据模型进行约束验证

#### GreedyScheduler更新
- `InitializeFeasibilityTensorAsync`：使用哨位的AvailablePersonnelIds初始化可行张量
- `SelectBestPersonnel`：从哨位可用人员中选择最佳人员 = request.Positions[positionIndex];


## 数据库设计

### 表结构更新

**Personals表：** 移除Position列
**Positions表：** 新增AvailablePersonnelIds列（JSON格式）
**Skills表：** 保持不变

### 索引优化
- 新增 `idx_positions_available_personnel` 索引提升查询性能

## 错误处理

### 数据完整性验证

**IDataIntegrityValidator：** 验证数据完整性服务
- 验证人员-哨位关系一致性
- 验证技能引用完整性
- 验证数据格式正确性

## 测试策略

### 测试覆盖范围

**Repository层测试：**
- PersonalRepository：验证移除PositionId相关操作
- PositionLocationRepository：测试新增的人员管理方法

**Service层测试：**
- PersonnelService：验证移除职位相关业务逻辑
- PositionService：测试人员管理和技能验证功能

**排班引擎测试：**
- GreedyScheduler：验证使用新数据模型的排班算法
- ConstraintValidator：测试新的约束验证逻辑



## 实施策略

### 开发阶段
1. **数据模型更新**：更新Personal和PositionLocation模型类
2. **数据访问层重构**：更新Repository接口和实现
3. **业务逻辑层适配**：更新Service层和DTO类
4. **排班引擎优化**：更新排班算法使用新数据模型
5. **用户界面更新**：更新ViewModel和View
6. **测试和验证**：执行完整功能测试

### 质量保证
- **代码审查**：确保数据模型一致性和接口兼容性
- **测试覆盖率**：Repository层100%，Service层95%以上

### 重构文档和操作指南

#### 重构操作步骤

根据需求7.2，系统需要提供详细的重构文档和操作指南：

**步骤1：数据备份**
```bash
# 备份当前数据库
cp AutoScheduling.db AutoScheduling_backup_$(date +%Y%m%d_%H%M%S).db
```

**步骤2：执行数据模型更新**
1. 更新Personal和PositionLocation模型类
2. 运行数据库结构更新脚本
3. 验证模型变更正确性

**步骤3：数据迁移**
1. 收集现有人员-职位关系数据
2. 转换为新的哨位-人员关系格式
3. 更新数据库记录
4. 验证数据完整性

**步骤4：代码更新**
1. 更新Repository层代码
2. 更新Service层代码
3. 更新排班引擎代码
4. 更新UI层代码

**步骤5：测试验证**
1. 运行单元测试
2. 运行集成测试
3. 执行功能验证
4. 性能基准测试

#### 回滚计划

如果重构过程中出现问题，可以按以下步骤回滚：

1. **停止应用程序**
2. **恢复数据库备份**
   ```bash
   cp AutoScheduling_backup_YYYYMMDD_HHMMSS.db AutoScheduling.db
   ```
3. **回滚代码到重构前版本**
4. **重启应用程序**
5. **验证系统功能正常**

#### 重构后验证清单

- [ ] 所有现有功能正常运行
- [ ] 排班算法性能未下降
- [ ] 数据完整性验证通过
- [ ] 用户界面功能正常
- [ ] 所有测试用例通过
- [ ] 系统日志无错误信息

#### 性能监控指标

重构后需要监控以下性能指标：

1. **排班算法执行时间**：应保持在原有水平或更优
2. **数据库查询响应时间**：人员-哨位关系查询应更快
3. **内存使用情况**：确保无内存泄漏
4. **用户界面响应时间**：UI操作应保持流畅

#### 故障排除指南

**常见问题及解决方案：**

1. **数据完整性错误**
   - 检查哨位的可用人员ID是否都存在
   - 验证技能引用的完整性
   - 运行数据完整性验证工具

2. **排班算法失败**
   - 确认哨位至少有一个可用人员
   - 检查人员技能是否满足哨位要求
   - 验证约束条件设置是否合理

3. **UI显示异常**
   - 检查DTO映射是否正确
   - 验证ViewModel数据绑定
   - 确认View模板更新正确

这个设计文档提供了完整的数据模型重构方案，重点关注新数据模型的设计和实现。重构后的系统将具有更好的查询性能和更合理的数据模型结构，同时确保系统的稳定性和可维护性。