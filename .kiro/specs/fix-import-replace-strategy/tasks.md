# 实现计划

- [x] 1. 创建文件结构和基础类





  - 创建 Services/ImportExport 文件夹（如果不存在）
  - 创建 Services/ImportExport/Locking 文件夹
  - 创建 Services/ImportExport/Batch 文件夹
  - 创建 Services/ImportExport/Comparison 文件夹
  - 创建 Services/ImportExport/Monitoring 文件夹
  - 创建 Services/ImportExport/Models 文件夹
  - _需求: 7.1, 7.2, 7.3, 7.4, 7.5, 9.1, 9.2, 9.3_
-

- [x] 2. 实现 ImportLockManager




  - 创建 Services/ImportExport/Locking/ImportLockManager.cs 文件
  - 实现 TryAcquireLockAsync 方法使用 SemaphoreSlim
  - 实现 ReleaseLock 方法
  - 实现 IsLocked 属性
  - 添加锁超时机制（30 分钟）
  - 添加日志记录（锁获取、释放、冲突事件）
  - 保持类简洁（约 100-150 行代码）
  - _需求: 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 3. 实现 BatchExistenceChecker

  - 创建 Services/ImportExport/Batch/BatchExistenceChecker.cs 文件
  - 实现 GetExistingIdsAsync 方法（批量 ID 查询）
  - 构建参数化 IN 查询
  - 处理空列表情况
  - 返回 HashSet<int> 用于快速查找
  - 实现 ClassifyRecords 泛型方法
  - 根据 existingIds 分类为 toInsert 和 toUpdate
  - 保持类简洁（约 100-150 行代码）
  - _需求: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 4. 实现数据比较器（拆分为多个文件）






  - [x] 4.1 创建基础比较器

    - 创建 Services/ImportExport/Comparison/DataComparerBase.cs 文件
    - 实现通用的数组比较方法
    - 实现通用的字符串比较方法
    - 实现通用的 null 值处理
    - 保持类简洁（约 100 行代码）
    - _需求: 4.1, 4.2, 4.3_
  
  - [x] 4.2 创建核心实体比较器


    - 创建 Services/ImportExport/Comparison/CoreEntityComparer.cs 文件
    - 实现 AreSkillsEqual 方法
    - 实现 ArePersonnelEqual 方法
    - 实现 ArePositionsEqual 方法
    - 继承 DataComparerBase
    - 保持类简洁（约 150-200 行代码）
    - _需求: 4.1, 4.2, 4.3, 11.1, 11.2, 11.4_
  

  - [x] 4.3 创建约束实体比较器

    - 创建 Services/ImportExport/Comparison/ConstraintComparer.cs 文件
    - 实现模板比较方法
    - 实现固定分配比较方法
    - 实现手动分配比较方法
    - 实现节假日配置比较方法
    - 继承 DataComparerBase
    - 保持类简洁（约 150-200 行代码）
    - _需求: 4.1, 4.2, 4.3_

- [x] 5. 实现 BatchImporter





  - 创建 Services/ImportExport/Batch/BatchImporter.cs 文件
  - 实现 BatchInsertAsync 泛型方法
  - 构建批量 INSERT 语句
  - 实现 BatchUpdateAsync 泛型方法
  - 构建 UPDATE 语句（不更新主键）
  - 使用参数化查询防止 SQL 注入
  - 支持可配置的批次大小（默认 100）
  - 处理批次循环逻辑
  - 保持类简洁（约 200-250 行代码）
  - _需求: 5.1, 5.2, 5.3, 5.4, 5.5, 2.1, 2.2, 2.3_

- [x] 6. 实现性能监控




  - [x] 6.1 创建性能监控器


    - 创建 Services/ImportExport/Monitoring/PerformanceMonitor.cs 文件
    - 实现 StartOperation 方法
    - 实现 EndOperation 方法
    - 使用 Stopwatch 记录时间
    - 维护 OperationTimings 字典
    - 保持类简洁（约 100-150 行代码）
    - _需求: 9.1, 9.2, 9.3_
  
  - [x] 6.2 创建性能报告类


    - 创建 Services/ImportExport/Monitoring/PerformanceReport.cs 文件
    - 实现 GenerateReport 静态方法
    - 计算总执行时间
    - 计算每秒处理记录数
    - 生成操作时间分解
    - 生成摘要字符串






    - 保持类简洁（约 80-100 行代码）
    - _需求: 9.1, 9.2, 9.3, 9.4, 9.5_

- [x] 7. 扩展数据模型





  - [x] 7.1 创建 ImportContext 类


    - 创建 Services/ImportExport/Models/ImportContext.cs 文件
    - 添加 Connection 属性
    - 添加 Transaction 属性


    - 添加 Options 属性
    - 添加 PerformanceMonitor 属性
    - 添加 Statistics 属性
    - 添加 Warnings 属性
    - 保持类简洁（约 50 行代码）
    - _需求: 1.1, 1.2, 1.3_
  
  - [x] 7.2 创建 TableImportStats 类


    - 创建 Services/ImportExport/Models/TableImportStats.cs 文件

    - 添加 Total 属性

    - 添加 Inserted 属性
    - 添加 Updated 属性
    - 添加 Unchanged 属性



    - 添加 Skipped 属性
    - 添加 Duration 属性
    - 保持类简洁（约 30 行代码）
    - _需求: 4.4, 9.1, 9.2_
  
  - [x] 7.3 扩展 ImportStatistics 类


    - 在 DTOs/ImportExport/ImportStatistics.cs 中添加新属性
    - 添加 InsertedRecords 属性
    - 添加 UpdatedRecords 属性
    - 添加 UnchangedRecords 属性
    - 添加 DetailsByTable 字典（Dictionary<string, TableImportStats>）
    - _需求: 4.4, 9.1, 9.2_

- [x] 8. 创建字段映射器（拆分为独立文件）






  - 创建 Services/ImportExport/Mappers 文件夹
  - 创建 Services/ImportExport/Mappers/FieldMapper.cs 文件
  - 实现 MapSkillToFields 静态方法
  - 实现 MapPersonnelToFields 静态方法
  - 实现 MapPositionToFields 静态方法
  - 实现 MapTemplateToFields 静态方法
  - 实现 MapFixedAssignmentToFields 静态方法
  - 实现 MapManualAssignmentToFields 静态方法
  - 实现 MapHolidayConfigToFields 静态方法
  - 保持类简洁（约 200-250 行代码）
  - _需求: 1.1, 1.2, 2.1, 2.2, 2.3_
- [x] 9. 重构 DataImportExportService 主方法




- [ ] 9. 重构 DataImportExportService 主方法

  - 在 DataImportExportService.cs 中添加 GetConnectionString 私有方法
  - 添加 _jsonOptions 字段
  - 重构 ImportDataAsync 方法：
    - 添加导入锁获取和释放（使用 ImportLockManager）
    - 创建 ImportContext 对象
    - 使用 SqliteConnection 和 SqliteTransaction
    - 包装所有导入操作在事务中
    - 实现事务提交和回滚逻辑
    - 添加性能监控（使用 PerformanceMonitor）
  - 保持方法简洁（约 150-200 行代码）
  - _需求: 1.1, 1.2, 1.3, 1.4, 1.5, 7.1, 7.2, 7.3, 9.1, 9.2_
-

- [x] 10. 创建导入处理器基类




  - 创建 Services/ImportExport/Importers 文件夹
  - 创建 Services/ImportExport/Importers/ImporterBase.cs 抽象基类
  - 实现通用的导入流程框架
  - 实现 Replace、Skip、Merge 策略的通用逻辑
  - 实现错误处理和日志记录
  - 实现进度报告
  - 定义抽象方法供子类实现（GetTableName, GetRecordId, CompareRecords 等）
  - 保持类简洁（约 200-250 行代码）
  - _需求: 1.1, 1.2, 1.3, 3.1, 3.2, 4.1, 4.2, 5.1, 5.2, 6.1, 6.2, 8.1, 8.2, 8.3_





- [x] 11. 实现技能导入器

  - 创建 Services/ImportExport/Importers/SkillImporter.cs 文件
  - 继承 ImporterBase
  - 实现 GetTableName 方法（返回 "Skills"）
  - 实现 GetRecordId 方法
  - 实现 CompareRecords 方法（使用 CoreEntityComparer）
  - 实现 MapToFields 方法（使用 FieldMapper）



  - 在 DataImportExportService 中创建 ImportSkillsWithTransactionAsync 方法
  - 使用 SkillImporter 执行导入
  - 保持 SkillImporter 类简洁（约 80-100 行代码）
  - _需求: 2.1, 2.2, 2.3, 3.1, 3.2, 4.1, 4.2, 5.1, 5.2, 8.3_

- [x] 12. 实现人员导入器

  - 创建 Services/ImportExport/Importers/PersonnelImporter.cs 文件
  - 继承 ImporterBase


  - 实现必要的抽象方法
  - 使用 CoreEntityComparer.ArePersonnelEqual 进行比较
  - 处理数组字段（SkillIds, RecentPeriodShiftIntervals）
  - 在 DataImportExportService 中创建 ImportPersonnelWithTransactionAsync 方法
  - 保持类简洁（约 80-100 行代码）
  - _需求: 2.1, 2.2, 2.3, 3.1, 3.2, 4.1, 4.2, 5.1, 5.2, 11.1, 11.2, 11.3, 11.4, 11.5_


- [x] 13. 实现哨位导入器


  - 创建 Services/ImportExport/Importers/PositionImporter.cs 文件
  - 继承 ImporterBase
  - 实现必要的抽象方法
  - 使用 CoreEntityComparer.ArePositionsEqual 进行比较
  - 处理数组字段（RequiredSkillIds, AvailablePersonnelIds）
  - 在 DataImportExportService 中创建 ImportPositionsWithTransactionAsync 方法
  - 保持类简洁（约 80-100 行代码）
  - _需求: 2.1, 2.2, 2.3, 3.1, 3.2, 4.1, 4.2, 5.1, 5.2, 11.1, 11.2, 11.3, 11.4, 11.5_
-

- [x] 14. 实现模板导入器



  - 创建 Services/ImportExport/Importers/TemplateImporter.cs 文件
  - 继承 ImporterBase
  - 实现必要的抽象方法
  - 使用 ConstraintComparer 进行比较
  - 处理复杂的模板字段
  - 在 DataImportExportService 中创建 ImportTemplatesWithTransactionAsync 方法
  - 保持类简洁（约 80-100 行代码）
  - _需求: 2.1, 2.2, 2.3, 3.1, 3.2, 5.1, 5.2_





- [x] 15. 实现约束导入器（节假日配置、固定分配、手动分配）

  - 创建 Services/ImportExport/Importers/HolidayConfigImporter.cs 文件
  - 创建 Services/ImportExport/Importers/FixedAssignmentImporter.cs 文件
  - 创建 Services/ImportExport/Importers/ManualAssignmentImporter.cs 文件
  - 每个类继承 ImporterBase
  - 实现必要的抽象方法


  - 处理约束仓储的特殊方法
  - 在 DataImportExportService 中创建对应的导入方法
  - 每个类保持简洁（约 80-100 行代码）
  - _需求: 2.1, 2.2, 2.3, 3.1, 3.2, 5.1, 5.2_

- [x] 16. 更新验证逻辑


  - 在 DataImportExportService.ValidateImportDataAsync 中添加主键重复检查
  - 检查导入文件内部的主键重复
  - 检测重复名称并记录警告（不阻止导入）
  - 确保所有验证在事务开始前完成
  - 验证外键引用完整性
  - _需求: 10.1, 10.2, 10.3, 10.4, 10.5, 11.2, 11.3, 11.5_

- [x] 17. 添加审计日志和性能日志




  - 在 ImportDataAsync 中记录导入操作开始和结束
  - 记录事务提交和回滚事件
  - 使用 PerformanceMonitor 记录性能指标
  - 记录批量操作统计（每个表的 inserted/updated/unchanged）
  - 在导入完成后生成并记录性能报告
  - _需求: 1.4, 7.5, 9.1, 9.2, 9.3, 9.4, 9.5_


- [x] 18. 更新错误处理




  - 在 ImportDataAsync 的 catch 块中改进错误处理
  - 使用 ErrorMessageTranslator 转换错误消息
  - 使用 ErrorRecoverySuggester 生成恢复建议
  - 记录详细的错误上下文（表名、记录 ID、操作类型）
  - 确保导入锁在异常时也能释放（finally 块）
  - _需求: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 19. 标记旧方法为过时




  - 在旧的 ImportSkillsAsync 方法上添加 [Obsolete] 特性
  - 在旧的 ImportPersonnelAsync 方法上添加 [Obsolete] 特性
  - 在旧的 ImportPositionsAsync 等方法上添加 [Obsolete] 特性
  - 添加注释说明迁移到新的 WithTransaction 方法
  - 保留旧方法以确保向后兼容（暂不删除）
  - _需求: 8.3, 8.4, 8.5_

- [ ]* 20. 编写单元测试
  - [ ]* 20.1 测试 ImportLockManager
    - 测试锁获取和释放
    - 测试并发锁冲突
    - 测试锁超时
  
  - [ ]* 20.2 测试 BatchExistenceChecker
    - 测试批量 ID 查询
    - 测试记录分类
    - 测试空列表处理
  
  - [ ]* 20.3 测试 DataComparer
    - 测试各实体类型的比较
    - 测试相等和不相等情况
    - 测试 null 值处理
  
  - [ ]* 20.4 测试 BatchImporter
    - 测试批量插入
    - 测试批量更新
    - 测试批次大小处理

- [ ]* 21. 编写集成测试
  - [ ]* 21.1 测试事务完整性
    - 测试成功提交
    - 测试失败回滚
    - 测试部分失败处理
  
  - [ ]* 21.2 测试性能
    - 测试 100 条记录导入
    - 测试 1000 条记录导入
    - 验证性能指标
  
  - [ ]* 21.3 测试并发
    - 测试同时发起多个导入
    - 验证锁机制
  
  - [ ]* 21.4 测试数据一致性
    - 导出数据
    - 使用 Replace 策略导入
    - 验证主键保持不变
    - 验证数据完全一致
