# 需求文档

## 简介

当前 `DataImportExportService` 中的 Replace 策略存在严重的架构缺陷，可能导致数据不一致、性能低下和并发问题。本功能通过实现适当的事务管理、批量操作和高效的冲突解决来解决这些问题。

## 术语表

- **System**: AutoScheduling3 的 DataImportExportService 数据导入导出服务
- **Replace Strategy**: 替换策略，用导入数据替换现有数据的导入冲突解决策略
- **Transaction**: 事务，确保原子操作的数据库事务
- **Batch Operation**: 批量操作，在单个操作中处理多条数据库记录
- **Primary Key Consistency**: 主键一致性，确保导入的记录保持其原始主键值
- **N+1 Query Problem**: N+1 查询问题，为每条记录执行单独查询而不是批量查询的性能问题
- **Rollback**: 回滚，操作失败时恢复数据库更改
- **Existence Check**: 存在性检查，验证记录是否已存在于数据库中

## 需求

### 需求 1：事务保护

**用户故事：** 作为系统管理员，我希望所有导入操作都是原子性的，以便部分失败不会使数据库处于不一致状态。

#### 验收标准

1. WHEN System 执行 Replace Strategy, THE System SHALL 将所有数据库操作包装在单个 Transaction 中
2. IF 导入过程中任何操作失败, THEN THE System SHALL 回滚当前 Transaction 中的所有更改
3. WHEN Transaction 成功提交, THE System SHALL 确保所有更改原子性地持久化
4. THE System SHALL 记录 Transaction 的开始、提交和回滚事件
5. WHEN Rollback 发生, THE System SHALL 将数据库恢复到导入前的状态

### 需求 2：主键一致性

**用户故事：** 作为数据管理员，我希望导入的记录保持其原始主键，以便关系和引用保持有效。

#### 验收标准

1. WHEN System 使用 Replace Strategy 导入记录, THE System SHALL 保留导入文件中的原始主键值
2. WHEN System 替换现有记录, THE System SHALL 使用 UPDATE 操作而不是 DELETE 后跟 INSERT
3. WHEN System 创建新记录, THE System SHALL 从导入数据中显式设置主键值
4. THE System SHALL 在导入完成后验证 Primary Key Consistency
5. IF 主键冲突发生, THEN THE System SHALL 记录详细的冲突信息

### 需求 3：批量存在性检查

**用户故事：** 作为系统操作员，我希望导入操作能够高效处理大型数据集，以便导入在合理时间内完成。

#### 验收标准

1. WHEN System 检查现有记录, THE System SHALL 使用批量查询而不是单独查询
2. THE System SHALL 在每个实体类型的单个查询中检索所有现有记录 ID
3. WHEN 处理 1000+ 条记录, THE System SHALL 在 5 秒内完成 Existence Check
4. THE System SHALL 使用内存集合缓存 Existence Check 结果
5. THE System SHALL 在所有导入操作中避免 N+1 Query Problem

### 需求 4：高效数据比较

**用户故事：** 作为系统操作员，我希望系统跳过不必要的更新，以便导入操作更快并减少数据库活动。

#### 验收标准

1. WHEN System 处理现有记录, THE System SHALL 比较导入数据与现有数据
2. IF 导入数据与现有数据完全匹配, THEN THE System SHALL 跳过更新操作
3. THE System SHALL 使用高效的比较方法，避免加载完整的实体对象
4. THE System SHALL 在导入摘要中记录跳过的更新数量
5. WHEN 比较检测到差异, THE System SHALL 仅更新已更改的字段

### 需求 5：批量插入和更新操作

**用户故事：** 作为系统操作员，我希望数据库操作能够批量处理，以便导入性能随数据量良好扩展。

#### 验收标准

1. WHEN System 插入新记录, THE System SHALL 使用 Batch Operation 进行插入
2. WHEN System 更新现有记录, THE System SHALL 使用 Batch Operation 进行更新
3. THE System SHALL 以可配置的批次大小处理记录（默认 100 条记录）
4. WHEN 达到批次大小, THE System SHALL 在继续之前执行 Batch Operation
5. THE System SHALL 向用户报告 Batch Operation 进度

### 需求 6：适当的错误处理

**用户故事：** 作为系统管理员，我希望在导入失败时获得详细的错误信息，以便快速诊断和修复问题。

#### 验收标准

1. WHEN System 在导入期间遇到错误, THE System SHALL 捕获导致错误的特定记录
2. THE System SHALL 记录包含实体类型、记录 ID 和错误消息的完整上下文的错误
3. WHEN 发生多个错误, THE System SHALL 在使 Transaction 失败之前收集所有错误
4. THE System SHALL 向用户提供可操作的错误消息
5. IF 发生验证错误, THEN THE System SHALL 在启动 Transaction 之前报告它们

### 需求 7：并发导入保护

**用户故事：** 作为系统管理员，我希望防止并发导入操作，以便不会发生数据冲突。

#### 验收标准

1. WHEN System 启动导入操作, THE System SHALL 获取导入锁
2. IF 另一个导入正在进行, THEN THE System SHALL 以清晰的消息拒绝新的导入请求
3. WHEN 导入完成或失败, THE System SHALL 释放导入锁
4. THE System SHALL 实现锁超时以防止死锁
5. THE System SHALL 记录所有锁获取和释放事件

### 需求 8：导入策略清晰性

**用户故事：** 作为开发人员，我希望 Merge 和 Skip 策略之间有明确的区别，以便它们的行为可预测且易于维护。

#### 验收标准

1. WHEN System 执行 Skip Strategy, THE System SHALL 仅插入不存在的记录
2. WHEN System 执行 Merge Strategy, THE System SHALL 插入新记录并更新现有记录
3. WHEN System 执行 Replace Strategy, THE System SHALL 更新现有记录并插入新记录
4. THE System SHALL 在代码注释中记录每个策略的确切行为
5. THE System SHALL 验证策略实现与其记录的行为匹配

### 需求 9：性能监控

**用户故事：** 作为系统操作员，我希望监控导入性能指标，以便识别瓶颈并优化操作。

#### 验收标准

1. WHEN System 完成导入, THE System SHALL 报告总执行时间
2. THE System SHALL 报告每个主要操作（验证、Existence Check、插入、更新）所花费的时间
3. THE System SHALL 报告每秒处理的记录数
4. THE System SHALL 记录超过 1000 条记录的导入的性能指标
5. WHEN 性能下降, THE System SHALL 在日志中包含诊断信息

### 需求 10：事务前验证

**用户故事：** 作为系统管理员，我希望在任何数据库更改之前进行数据验证，以便无效数据不会导致部分导入。

#### 验收标准

1. WHEN System 启动导入, THE System SHALL 在开始 Transaction 之前验证所有数据
2. THE System SHALL 在验证期间检查必需字段、数据类型和约束
3. THE System SHALL 在启动 Transaction 之前验证外键引用是否存在
4. IF 验证失败, THEN THE System SHALL 报告所有验证错误而不修改数据库
5. WHEN 验证通过, THE System SHALL 继续进行事务性导入

### 需求 11：重复名称处理

**用户故事：** 作为数据管理员，我希望系统能够正确处理重名情况，以便通过主键而不是名称来识别记录。

#### 验收标准

1. WHEN System 比较记录, THE System SHALL 使用主键 ID 而不是名称字段进行匹配
2. WHEN System 检测到导入数据中存在重复名称, THE System SHALL 记录警告但允许导入继续
3. THE System SHALL 在导入摘要中报告重复名称的数量和详细信息
4. WHEN 现有数据库中存在重复名称, THE System SHALL 基于主键 ID 正确更新对应记录
5. THE System SHALL 在验证阶段检查导入文件内部是否存在重复的主键 ID
