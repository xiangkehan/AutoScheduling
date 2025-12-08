# 实施计划

本文档定义了全局多天调度功能的实施任务列表。任务按照依赖关系和优先级组织,确保增量开发和及时验证。

## 任务列表

- [ ] 1. 实现时段映射工具类
  - 创建 `PeriodMapper` 类,实现全局时段索引与局部索引的双向转换
  - 实现 `ToGlobalPeriod(dayIndex, localPeriod)` 方法
  - 实现 `ToLocalPeriod(globalPeriod)` 方法
  - 实现 `ToDateTime(globalPeriod)` 和 `ToGlobalPeriod(date, localPeriod)` 方法
  - 添加索引有效性验证
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ]* 1.1 编写PeriodMapper的属性测试
  - **Property 5: 时段映射双射性**
  - **Validates: Requirements 5.1, 5.2**

- [ ] 2. 扩展FeasibilityTensor支持全局时段
  - 验证现有构造函数已支持可变 `periodCount` 参数
  - 添加 `GetMemoryUsageBytes()` 方法
  - 添加 `IsMemoryExceeded(thresholdMB)` 方法
  - 确保位运算优化在大时段数下正常工作
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [ ]* 2.1 编写FeasibilityTensor内存测试
  - **Property 6: 内存占用可预测性**
  - **Validates: Requirements 4.1, 4.2**

- [ ] 3. 实现全局MRV策略
  - 创建 `GlobalMRVStrategy` 类
  - 实现 `SelectNextSlot()` 返回全局时段索引
  - 实现 `UpdateAdjacentPeriodCounts()` 支持跨日相邻时段
  - 实现 `UpdateNightShiftCounts()` 支持跨日夜哨周期
  - 添加候选计数缓存和分配标记数组
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ]* 3.1 编写GlobalMRV的属性测试
  - **Property 3: 全局MRV最优性**
  - **Validates: Requirements 3.1**

- [ ] 4. 实现跨日约束验证器
  - 创建 `CrossDayConstraintValidator` 类
  - 实现 `ValidateCrossDayRestConstraint()` 方法
  - 实现 `ValidateCrossDayNightShiftConstraint()` 方法
  - 集成到现有的 `ConstraintValidator` 中
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 7.3, 7.4_

- [ ]* 4.1 编写跨日约束的属性测试
  - **Property 1: 跨日约束传递性**
  - **Validates: Requirements 1.3**

- [ ]* 4.2 编写跨日夜哨的属性测试
  - **Property 2: 跨日夜哨唯一性**
  - **Validates: Requirements 1.5**

- [ ] 5. 创建全局调度配置类
  - 创建 `GlobalSchedulingConfig` 类
  - 添加 `EnableGlobalScheduling` 配置项
  - 添加 `MaxDaysForGlobalMode` 配置项
  - 添加 `MemoryThresholdMB` 配置项
  - 添加 `EnableSegmentedProcessing` 和 `SegmentSizeDays` 配置项
  - 集成到 `GreedySchedulerConfig` 中
  - _Requirements: 6.1, 9.1, 9.4, 11.1, 11.3_

- [ ] 6. 扩展SchedulingProgressReport
  - 添加 `GlobalPeriodIndex` 字段
  - 添加 `TotalGlobalPeriods` 字段
  - 添加 `IsGlobalScheduling` 字段
  - 添加 `CrossDayBacktracks` 字段
  - _Requirements: 8.1, 8.2, 8.3, 8.4_


- [ ] 7. 实现全局调度主流程
  - 在 `GreedyScheduler` 中添加 `ExecuteGlobalSchedulingAsync()` 方法
  - 实现全局张量初始化逻辑
  - 实现内存检查和降级逻辑
  - 实现全局约束应用 `ApplyGlobalConstraintsAsync()`
  - 实现全局手动指定应用 `ApplyGlobalManualAssignmentsAsync()`
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 6.1, 6.3, 9.1, 9.3_

- [ ] 7.1 实现全局贪心分配循环
  - 实现 `PerformGlobalGreedyAssignmentsAsync()` 方法
  - 集成 `GlobalMRVStrategy` 进行全局选择
  - 集成 `BacktrackingEngine` 进行跨日回溯
  - 实现进度报告逻辑
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ]* 7.2 编写跨日回溯的属性测试
  - **Property 4: 跨日回溯完整性**
  - **Validates: Requirements 2.1**

- [ ] 8. 实现降级策略
  - 实现 `FallbackToPerDayScheduling()` 方法
  - 添加内存超限检测
  - 添加天数超限检测
  - 添加性能超时检测
  - 实现降级通知和日志记录
  - _Requirements: 9.2, 11.3_

- [ ]* 8.1 编写降级策略的属性测试
  - **Property 7: 降级策略正确性**
  - **Validates: Requirements 11.3**

- [ ] 9. 实现边界条件处理
  - 在约束验证中添加边界检查
  - 处理第一天时段0的前置时段缺失
  - 处理最后一天时段11的后续时段缺失
  - 添加边界情况的单元测试
  - _Requirements: 10.3_

- [ ]* 9.1 编写边界条件的属性测试
  - **Property 8: 边界条件安全性**
  - **Validates: Requirements 10.3**

- [ ] 10. 实现性能优化
  - 实现增量张量初始化 `InitializeGlobalTensorIncrementallyAsync()`
  - 实现并行约束应用 `ApplyConstraintsInParallelAsync()`
  - 实现增量状态恢复 `RestoreStateIncremental()`
  - 添加内存监控和预警
  - _Requirements: 6.1, 6.2, 6.4, 6.5_

- [ ] 11. 实现监控和诊断
  - 创建 `GlobalSchedulingMetrics` 类
  - 创建 `GlobalSchedulingDiagnosticReport` 类
  - 创建 `CrossDayConstraintViolation` 类
  - 实现性能指标收集
  - 实现诊断报告生成
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 10.5_

- [ ] 12. 扩展BacktrackingEngine支持全局回溯
  - 添加 `BacktrackGlobal()` 方法接受 `PeriodMapper` 参数
  - 确保分配栈正确处理全局时段索引
  - 确保状态快照包含完整的全局张量状态
  - 验证跨日回溯的正确性
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [ ] 13. 更新GreedyScheduler主入口
  - 修改 `ExecuteAsync()` 方法检查全局调度配置
  - 根据配置选择全局模式或按天模式
  - 添加模式切换日志记录
  - 确保向后兼容性
  - _Requirements: 9.1, 9.2, 9.3, 9.5_

- [ ] 14. 实现配置迁移工具
  - 实现 `MigrateFromLegacyConfig()` 方法
  - 添加配置验证逻辑
  - 添加配置默认值处理
  - 编写配置迁移文档
  - _Requirements: 9.1, 9.4_

- [ ] 15. 编写集成测试
  - 小规模测试: 2天 × 3哨位 × 5人员
  - 中规模测试: 8天 × 10哨位 × 20人员
  - 大规模测试: 30天 × 20哨位 × 50人员
  - 跨日约束验证测试
  - 跨日回溯验证测试
  - _Requirements: 10.1, 10.2, 10.4_

- [ ] 16. 编写对比测试
  - 实现全局模式 vs 按天模式的对比测试框架
  - 对比未分配时段数量
  - 对比跨日约束违反数量
  - 对比执行时间和内存占用
  - 生成对比报告
  - _Requirements: 10.1, 10.2_

- [ ] 17. 性能基准测试
  - 实现性能基准测试框架
  - 测试不同规模场景的性能指标
  - 验证性能目标达成情况
  - 生成性能报告
  - _Requirements: 6.1, 6.2, 6.3, 6.5, 8.5_

- [ ] 18. 更新文档和示例
  - 更新用户文档说明全局调度功能
  - 添加配置示例
  - 添加使用指南
  - 更新API文档
  - _Requirements: 9.5, 12.1, 12.2, 12.3, 12.4, 12.5_

- [ ] 19. 最终验证和清理
  - 运行所有单元测试和集成测试
  - 验证所有需求都已实现
  - 代码审查和重构
  - 清理临时代码和注释
  - 更新版本号和变更日志
  - _Requirements: 所有需求_

## 任务依赖关系

```
1 (PeriodMapper)
├── 3 (GlobalMRV)
├── 4 (CrossDayValidator)
└── 7 (主流程)

2 (FeasibilityTensor)
├── 3 (GlobalMRV)
└── 7 (主流程)

3 (GlobalMRV)
└── 7.1 (贪心分配)

4 (CrossDayValidator)
└── 7 (主流程)

5 (Config)
└── 7 (主流程)

6 (ProgressReport)
└── 7.1 (贪心分配)

7 (主流程)
├── 7.1 (贪心分配)
├── 8 (降级策略)
└── 13 (主入口)

8 (降级策略)
└── 13 (主入口)

9 (边界处理)
└── 7 (主流程)

10 (性能优化)
└── 7 (主流程)

11 (监控诊断)
└── 7.1 (贪心分配)

12 (BacktrackingEngine)
└── 7.1 (贪心分配)

13 (主入口)
└── 15 (集成测试)

14 (配置迁移)
└── 13 (主入口)

15 (集成测试)
├── 16 (对比测试)
└── 17 (性能测试)

16, 17 (测试)
└── 18 (文档)

18 (文档)
└── 19 (最终验证)
```

## 实施顺序建议

### 第一阶段: 核心组件 (任务1-6)
优先级: 高
预计时间: 3-4天

实现基础组件,为主流程做准备。

### 第二阶段: 主流程集成 (任务7-9, 12-13)
优先级: 高
预计时间: 4-5天

实现全局调度的主要逻辑,包括降级策略和边界处理。

### 第三阶段: 优化和监控 (任务10-11)
优先级: 中
预计时间: 2-3天

实现性能优化和监控诊断功能。

### 第四阶段: 测试和验证 (任务15-17)
优先级: 高
预计时间: 3-4天

全面测试功能正确性和性能指标。

### 第五阶段: 文档和发布 (任务14, 18-19)
优先级: 中
预计时间: 1-2天

完善文档,进行最终验证和发布准备。

## 总预计时间

13-18天 (约2.5-3.5周)

## 注意事项

1. **增量开发**: 每完成一个任务立即进行单元测试
2. **及时集成**: 避免长时间分支开发,及时合并到主分支
3. **性能监控**: 在开发过程中持续监控性能指标
4. **向后兼容**: 确保现有功能不受影响
5. **文档同步**: 代码和文档同步更新
