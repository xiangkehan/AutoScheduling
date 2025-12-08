# 实施计划

本文档定义了全局多天调度功能的实施任务列表。任务按照依赖关系和优先级组织,确保增量开发和及时验证。

## 任务列表

### 第一阶段: 核心组件

- [ ] 1. 实现时段映射工具类
  - 在 `SchedulingEngine/Core/` 目录创建 `PeriodMapper.cs` 文件
  - 实现 `ToGlobalPeriod(dayIndex, localPeriod)` 方法
  - 实现 `ToLocalPeriod(globalPeriod)` 方法
  - 实现 `ToDateTime(globalPeriod)` 和 `ToGlobalPeriod(date, localPeriod)` 方法
  - 添加 `TotalPeriods` 属性和 `IsValidGlobalPeriod()` 验证方法
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ]* 1.1 编写PeriodMapper的属性测试
  - **Property 5: 时段映射双射性**
  - **Validates: Requirements 5.1, 5.2**

- [ ] 2. 扩展FeasibilityTensor支持全局时段
  - 验证现有构造函数已支持可变 `periodCount` 参数（已支持）
  - 在 `FeasibilityTensor` 类中添加 `GetMemoryUsageBytes()` 方法
  - 添加 `IsMemoryExceeded(thresholdMB)` 方法
  - 添加 `GetMemoryStats()` 辅助方法返回详细内存统计
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [ ]* 2.1 编写FeasibilityTensor内存测试
  - **Property 6: 内存占用可预测性**
  - **Validates: Requirements 4.1, 4.2**

- [ ] 3. 实现全局MRV策略
  - 在 `SchedulingEngine/Strategies/` 目录创建 `GlobalMRVStrategy.cs` 文件
  - 实现构造函数接受 `FeasibilityTensor`, `SchedulingContext`, `PeriodMapper`
  - 实现 `SelectNextSlot()` 返回 `(positionIdx, globalPeriodIdx)`
  - 实现 `UpdateCandidateCountsAfterAssignment()` 方法
  - 实现 `UpdateAdjacentPeriodCounts()` 支持跨日相邻时段
  - 实现 `UpdateNightShiftCounts()` 支持跨日夜哨周期
  - 实现 `MarkAsAssigned()` 和 `InitializeCandidateCounts()` 方法
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ]* 3.1 编写GlobalMRV的属性测试
  - **Property 3: 全局MRV最优性**
  - **Validates: Requirements 3.1**

- [ ] 4. 实现跨日约束验证器
  - 在 `SchedulingEngine/Core/` 目录创建 `CrossDayConstraintValidator.cs` 文件
  - 实现构造函数接受 `SchedulingContext` 和 `PeriodMapper`
  - 实现 `ValidateCrossDayRestConstraint(personIdx, globalPeriodIdx)` 方法
  - 实现 `ValidateCrossDayNightShiftConstraint(personIdx, globalPeriodIdx)` 方法
  - 在 `ConstraintValidator` 中添加对跨日约束的调用
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 7.3, 7.4_

- [ ]* 4.1 编写跨日约束的属性测试
  - **Property 1: 跨日约束传递性**
  - **Validates: Requirements 1.3**

- [ ]* 4.2 编写跨日夜哨的属性测试
  - **Property 2: 跨日夜哨唯一性**
  - **Validates: Requirements 1.5**

- [ ] 5. 创建全局调度配置类
  - 在 `SchedulingEngine/Config/` 目录创建 `GlobalSchedulingConfig.cs` 文件
  - 添加 `EnableGlobalScheduling` 配置项（默认 true）
  - 添加 `MaxDaysForGlobalMode` 配置项（默认 30）
  - 添加 `MemoryThresholdMB` 配置项（默认 500）
  - 添加 `EnableSegmentedProcessing` 和 `SegmentSizeDays` 配置项
  - 在 `GreedyScheduler.cs` 中的 `GreedySchedulerConfig` 类添加 `GlobalScheduling` 属性
  - _Requirements: 6.1, 9.1, 9.4, 11.1, 11.3_

- [ ] 6. 扩展SchedulingProgressReport
  - 在 `DTOs/SchedulingProgressReport.cs` 中添加 `GlobalPeriodIndex` 字段（可空 int）
  - 添加 `TotalGlobalPeriods` 字段（可空 int）
  - 添加 `IsGlobalScheduling` 字段（bool，默认 false）
  - 添加 `CrossDayBacktracks` 字段（int，默认 0）
  - _Requirements: 8.1, 8.2, 8.3, 8.4_

### 第二阶段: 主流程集成

- [ ] 7. 实现全局调度主流程
  - 在 `GreedyScheduler` 中添加私有方法 `ExecuteGlobalSchedulingAsync()`
  - 实现天数和内存检查，超限时调用降级方法
  - 初始化 `PeriodMapper` 和全局 `FeasibilityTensor`
  - 实现 `ApplyGlobalConstraintsAsync()` 方法遍历所有全局时段应用约束
  - 实现 `ApplyGlobalManualAssignmentsAsync()` 方法应用手动指定
  - 调用 `PerformGlobalGreedyAssignmentsAsync()` 执行分配
  - 调用 `GenerateSchedule()` 生成最终结果
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 6.1, 6.3, 9.1, 9.3_

- [ ] 7.1 实现全局贪心分配循环
  - 实现 `PerformGlobalGreedyAssignmentsAsync()` 方法
  - 初始化 `GlobalMRVStrategy` 和 `BacktrackingEngine`
  - 实现主分配循环：检测死胡同 → 回溯 → MRV选择 → 尝试分配
  - 使用 `PeriodMapper` 转换全局时段索引到日期和局部时段
  - 实现进度报告，包含全局时段信息
  - 处理跨日回溯统计
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ]* 7.2 编写跨日回溯的属性测试
  - **Property 4: 跨日回溯完整性**
  - **Validates: Requirements 2.1**

- [ ] 8. 实现降级策略
  - 在 `GreedyScheduler` 中添加私有方法 `FallbackToPerDayScheduling()`
  - 添加内存超限检测逻辑
  - 添加天数超限检测逻辑
  - 实现降级通知（通过 progress 报告）
  - 添加日志记录降级原因
  - 调用现有的 `ExecuteAsync()` 按天模式逻辑
  - _Requirements: 9.2, 11.3_

- [ ]* 8.1 编写降级策略的属性测试
  - **Property 7: 降级策略正确性**
  - **Validates: Requirements 11.3**

- [ ] 9. 实现边界条件处理
  - 在 `CrossDayConstraintValidator` 中添加边界检查
  - 处理 `globalPeriodIdx == 0` 时没有前一时段的情况
  - 处理 `globalPeriodIdx == TotalPeriods - 1` 时没有后一时段的情况
  - 在 `GlobalMRVStrategy` 中添加边界检查
  - _Requirements: 10.3_

- [ ]* 9.1 编写边界条件的属性测试
  - **Property 8: 边界条件安全性**
  - **Validates: Requirements 10.3**

- [ ] 10. 扩展BacktrackingEngine支持全局回溯
  - 在 `BacktrackingEngine` 中添加 `BacktrackGlobal()` 方法
  - 方法接受 `PeriodMapper` 参数用于时段转换
  - 确保 `AssignmentStack` 正确处理全局时段索引
  - 确保状态快照包含完整的全局张量状态
  - 更新回溯统计以区分跨日回溯
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [ ] 11. 更新GreedyScheduler主入口
  - 修改 `ExecuteAsync()` 方法开头检查 `_config.GlobalScheduling.EnableGlobalScheduling`
  - 如果启用全局调度，调用 `ExecuteGlobalSchedulingAsync()`
  - 如果禁用，继续使用现有按天处理逻辑
  - 添加模式切换日志记录
  - 确保向后兼容性（默认配置下行为不变）
  - _Requirements: 9.1, 9.2, 9.3, 9.5_

### 第三阶段: 优化和监控

- [ ] 12. 实现性能优化
  - 在 `GreedyScheduler` 中实现 `InitializeGlobalTensorIncrementallyAsync()` 方法
  - 实现 `ApplyConstraintsInParallelAsync()` 方法使用并行处理
  - 在 `BacktrackingEngine` 中实现 `RestoreStateIncremental()` 方法
  - 添加内存监控和预警逻辑
  - _Requirements: 6.1, 6.2, 6.4, 6.5_

- [ ] 13. 实现监控和诊断
  - 在 `DTOs/` 目录创建 `GlobalSchedulingMetrics.cs` 文件
  - 创建 `GlobalSchedulingDiagnosticReport.cs` 文件
  - 创建 `CrossDayConstraintViolation.cs` 文件
  - 在全局调度流程中收集性能指标
  - 实现诊断报告生成方法
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 10.5_

- [ ] 14. 实现配置迁移工具
  - 在 `GlobalSchedulingConfig` 中添加静态方法 `MigrateFromLegacyConfig()`
  - 添加配置验证方法 `Validate()`
  - 添加配置默认值处理
  - 在 `GreedySchedulerConfig` 构造函数中初始化 `GlobalScheduling` 属性
  - _Requirements: 9.1, 9.4_

### 第四阶段: 测试和验证

- [ ] 15. 编写集成测试
  - 在 `TestData/` 或新建测试项目中创建集成测试类
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
  - 验证性能目标达成情况（初始化≤5秒，MRV≤100ms）
  - 生成性能报告
  - _Requirements: 6.1, 6.2, 6.3, 6.5, 8.5_

### 第五阶段: 文档和发布

- [ ] 18. 更新文档和示例
  - 在项目根目录或 `.kiro/specs/global-multi-day-scheduling/` 创建用户指南
  - 添加配置示例（如何启用/禁用全局调度）
  - 添加使用指南（适用场景、性能考虑）
  - 更新相关代码注释和XML文档
  - _Requirements: 9.5, 12.1, 12.2, 12.3, 12.4, 12.5_

- [ ] 19. 最终验证和清理
  - 运行所有单元测试和集成测试
  - 验证所有需求都已实现
  - 代码审查和重构
  - 清理临时代码和调试注释
  - 更新版本号和变更日志
  - _Requirements: 所有需求_

## 任务依赖关系

```
第一阶段 (核心组件):
1 (PeriodMapper) ──┐
2 (FeasibilityTensor) ─┼─→ 3 (GlobalMRV) ──┐
                       │                    │
4 (CrossDayValidator) ─┤                    │
5 (Config) ────────────┤                    │
6 (ProgressReport) ────┘                    │
                                            │
第二阶段 (主流程):                           │
                                            ↓
7 (主流程) ←────────────────────────────────┘
├── 7.1 (贪心分配) ←─ 10 (BacktrackingEngine)
├── 8 (降级策略)
└── 9 (边界处理)
    │
    ↓
11 (主入口) ←─ 14 (配置迁移)

第三阶段 (优化):
12 (性能优化) ──┐
13 (监控诊断) ──┴→ 集成到主流程

第四阶段 (测试):
15 (集成测试) ──┐
16 (对比测试) ──┼→ 17 (性能测试)
                │
第五阶段 (发布): │
                ↓
18 (文档) ──→ 19 (最终验证)
```

## 实施顺序建议

### 第一阶段: 核心组件 (任务1-6)
**优先级**: 高  
**预计时间**: 2-3天  
**目标**: 实现基础组件,为主流程做准备

**关键里程碑**:
- PeriodMapper 能正确转换全局和局部时段索引
- FeasibilityTensor 支持可变时段数并能报告内存使用
- GlobalMRVStrategy 能在全局范围内选择最优时段
- CrossDayConstraintValidator 能验证跨日约束
- 配置类和进度报告扩展完成

### 第二阶段: 主流程集成 (任务7-11)
**优先级**: 高  
**预计时间**: 3-4天  
**目标**: 实现全局调度的主要逻辑

**关键里程碑**:
- ExecuteGlobalSchedulingAsync 能完整执行全局调度流程
- 降级策略在超限时能正确切换到按天模式
- 边界条件得到正确处理
- BacktrackingEngine 支持跨日回溯
- 主入口能根据配置选择调度模式

### 第三阶段: 优化和监控 (任务12-14)
**优先级**: 中  
**预计时间**: 2-3天  
**目标**: 提升性能和可观测性

**关键里程碑**:
- 增量初始化和并行约束应用提升性能
- 监控指标能准确反映执行状态
- 配置迁移工具确保平滑升级

### 第四阶段: 测试和验证 (任务15-17)
**优先级**: 高  
**预计时间**: 2-3天  
**目标**: 全面验证功能正确性和性能

**关键里程碑**:
- 集成测试覆盖小、中、大规模场景
- 对比测试证明全局模式优于按天模式
- 性能测试验证达到设计目标

### 第五阶段: 文档和发布 (任务18-19)
**优先级**: 中  
**预计时间**: 1-2天  
**目标**: 完善文档,准备发布

**关键里程碑**:
- 用户文档清晰说明如何使用全局调度
- 所有测试通过
- 代码审查完成

## 总预计时间

**10-15天** (约2-3周)

## 实施注意事项

### 开发规范
1. **增量开发**: 每完成一个任务立即进行单元测试
2. **代码审查**: 核心组件完成后进行代码审查
3. **性能监控**: 在开发过程中持续监控性能指标
4. **向后兼容**: 确保现有功能不受影响,默认配置下行为不变

### 技术要点
1. **内存管理**: 全局张量可能占用较多内存,需要实时监控
2. **时段转换**: PeriodMapper 是核心工具,必须确保转换正确
3. **跨日约束**: 特别注意时段11到次日时段0的连续性
4. **回溯机制**: 跨日回溯比单日回溯更复杂,需要仔细测试

### 测试策略
1. **单元测试**: 每个组件都要有单元测试
2. **集成测试**: 测试完整的全局调度流程
3. **对比测试**: 与按天模式对比,验证改进效果
4. **性能测试**: 验证性能目标达成

### 风险控制
1. **降级策略**: 确保在任何情况下都能降级到按天模式
2. **配置开关**: 提供配置开关,方便回退
3. **日志记录**: 详细记录关键决策点,便于调试
4. **文档同步**: 代码和文档同步更新
