# 实现计划

- [ ] 1. 创建基础数据模型和枚举类型
  - 创建 DTOs/SchedulingMode.cs 枚举（GreedyOnly, Hybrid）
  - 创建 DTOs/SelectionStrategyType.cs 枚举
  - 创建 DTOs/CrossoverStrategyType.cs 枚举
  - 创建 DTOs/MutationStrategyType.cs 枚举
  - 创建 DTOs/GeneticProgressInfo.cs 类
  - _需求: 1.5, 9.2, 9.3, 9.4_

- [ ] 2. 实现个体（Individual）类
  - 创建 SchedulingEngine/Core/Individual.cs
  - 实现基因编码（Dictionary<DateTime, int[,]>）
  - 实现 FromSchedule 静态方法
  - 实现 ToSchedule 方法
  - 实现 Clone 深拷贝方法
  - 实现未分配时段识别逻辑
  - _需求: 1.3, 8.1_

- [ ]* 2.1 编写 Individual 类的属性测试
  - **属性 18: 未分配时段识别**
  - **验证需求: 8.1**

- [ ] 3. 实现种群（Population）类
  - 创建 SchedulingEngine/Core/Population.cs
  - 实现 Initialize 方法（包含初始解和随机个体）
  - 实现 UpdateStatistics 方法
  - 实现 GetElites 方法
  - _需求: 1.3, 2.1, 2.5_

- [ ]* 3.1 编写 Population 类的属性测试
  - **属性 2: 初始种群组成**
  - **验证需求: 1.3, 2.1**

- [ ]* 3.2 编写精英保留机制的属性测试
  - **属性 6: 精英保留机制**
  - **验证需求: 2.5**

- [ ] 4. 实现适应度评估器（FitnessEvaluator）
  - 创建 SchedulingEngine/Core/FitnessEvaluator.cs
  - 实现 EvaluateFitness 方法
  - 实现 CalculateHardConstraintViolations 方法（复用 ConstraintValidator）
  - 实现 CalculateSoftConstraintScore 方法（复用 SoftConstraintCalculator）
  - 实现 CalculateUnassignedPenalty 方法
  - 实现适应度综合计算公式
  - _需求: 3.1, 3.2, 3.3, 3.4, 8.3_

- [ ]* 4.1 编写适应度计算的属性测试
  - **属性 7: 适应度计算完整性**
  - **验证需求: 3.1, 3.3, 3.4**

- [ ]* 4.2 编写硬约束惩罚的属性测试
  - **属性 8: 硬约束惩罚单调性**
  - **验证需求: 3.2**

- [ ]* 4.3 编写未分配时段惩罚的属性测试
  - **属性 19: 未分配时段惩罚**
  - **验证需求: 8.3**

- [ ]* 4.4 编写适应度比较稳定性的属性测试
  - **属性 9: 适应度比较稳定性**
  - **验证需求: 3.5**

- [ ] 5. 实现选择策略
  - 创建 SchedulingEngine/Strategies/Selection/ISelectionStrategy.cs 接口
  - 创建 SchedulingEngine/Strategies/Selection/RouletteWheelSelection.cs
  - 创建 SchedulingEngine/Strategies/Selection/TournamentSelection.cs
  - _需求: 4.2, 9.1, 9.2_

- [ ]* 5.1 编写选择策略的属性测试
  - **属性 21: 策略配置应用（选择部分）**
  - **验证需求: 9.2**

- [ ] 6. 实现交叉策略
  - 创建 SchedulingEngine/Strategies/Crossover/ICrossoverStrategy.cs 接口
  - 创建 SchedulingEngine/Strategies/Crossover/UniformCrossover.cs
  - 实现基因随机组合逻辑
  - 实现后代约束验证
  - 实现约束违反修复机制
  - 创建 SchedulingEngine/Strategies/Crossover/SinglePointCrossover.cs
  - _需求: 4.1, 4.3, 4.4, 4.5, 9.1, 9.3_

- [ ]* 6.1 编写交叉操作的属性测试
  - **属性 10: 交叉操作合法性**
  - **验证需求: 4.1, 4.3**

- [ ]* 6.2 编写交叉后约束验证的属性测试
  - **属性 11: 交叉后约束验证**
  - **验证需求: 4.4**

- [ ]* 6.3 编写策略配置应用的属性测试（交叉部分）
  - **属性 21: 策略配置应用（交叉部分）**
  - **验证需求: 9.3**

- [ ] 7. 实现变异策略
  - 创建 SchedulingEngine/Strategies/Mutation/IMutationStrategy.cs 接口
  - 创建 SchedulingEngine/Strategies/Mutation/SwapMutation.cs
  - 实现随机班次选择逻辑
  - 实现从可行人员中选择新人员
  - 实现优先填补未分配时段的逻辑
  - 实现变异后约束验证
  - 实现约束违反回滚机制
  - _需求: 5.1, 5.2, 5.3, 5.4, 5.5, 8.4, 9.1, 9.4_

- [ ]* 7.1 编写变异操作可行性的属性测试
  - **属性 12: 变异操作可行性**
  - **验证需求: 5.3**

- [ ]* 7.2 编写变异后约束验证的属性测试
  - **属性 13: 变异后约束验证**
  - **验证需求: 5.4**

- [ ]* 7.3 编写变异优先填补未分配的属性测试
  - **属性 20: 变异优先填补未分配**
  - **验证需求: 8.4**

- [ ]* 7.4 编写策略配置应用的属性测试（变异部分）
  - **属性 21: 策略配置应用（变异部分）**
  - **验证需求: 9.4**

- [ ] 8. 实现遗传算法配置类
  - 创建 SchedulingEngine/Config/GeneticSchedulerConfig.cs
  - 实现所有配置属性（种群大小、代数、交叉率、变异率等）
  - 实现 SaveToFile 方法（JSON序列化）
  - 实现 LoadFromFile 方法（JSON反序列化）
  - 实现 GetDefault 静态方法
  - 实现配置验证逻辑
  - 添加错误处理（文件损坏、无效参数等）
  - _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ]* 8.1 编写配置持久化的属性测试
  - **属性 16: 配置持久化往返**
  - **验证需求: 7.1, 7.2**

- [ ]* 8.2 编写配置重置的属性测试
  - **属性 17: 配置重置正确性**
  - **验证需求: 7.4**

- [ ]* 8.3 编写权重配置影响的属性测试
  - **属性 22: 权重配置影响**
  - **验证需求: 9.5**

- [ ] 9. 实现遗传算法调度器（GeneticScheduler）
  - 创建 SchedulingEngine/GeneticScheduler.cs
  - 实现构造函数（注入配置、上下文、策略）
  - 实现 ExecuteAsync 方法主循环
  - 实现种群初始化逻辑（包含初始解）
  - 实现选择操作
  - 实现交叉操作（根据交叉率）
  - 实现变异操作（根据变异率）
  - 实现精英保留机制
  - 实现代数终止条件
  - 实现进度报告逻辑
  - 实现取消令牌检查
  - _需求: 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 2.4, 2.5, 6.1, 6.2, 6.3, 6.4, 6.5, 8.2_

- [ ]* 9.1 编写遗传算法完整流程的属性测试
  - **属性 3: 最优解返回**
  - **验证需求: 1.4**

- [ ]* 9.2 编写代数终止条件的属性测试
  - **属性 5: 代数终止条件**
  - **验证需求: 2.2**

- [ ]* 9.3 编写进度报告的属性测试
  - **属性 14: 进度报告完整性**
  - **验证需求: 6.1, 6.2, 6.5**

- [ ]* 9.4 编写取消响应性的属性测试
  - **属性 15: 取消响应性**
  - **验证需求: 6.4**

- [ ] 10. 修改 GreedyScheduler 以支持混合模式
  - 修改 SchedulingEngine/GreedyScheduler.cs
  - 添加返回未分配时段信息的功能
  - 确保生成的 Schedule 包含完整的分配信息
  - _需求: 1.1, 8.1_

- [ ] 11. 实现混合调度器（HybridScheduler）
  - 创建 SchedulingEngine/HybridScheduler.cs
  - 实现构造函数（注入 GreedyScheduler 和 GeneticScheduler）
  - 实现 ExecuteAsync 方法
  - 实现先执行贪心算法的逻辑
  - 实现将贪心解传递给遗传算法的逻辑
  - 实现整体进度报告协调
  - 实现取消令牌传递
  - _需求: 1.1, 1.2, 6.1, 6.4_

- [ ]* 11.1 编写混合模式执行顺序的属性测试
  - **属性 1: 混合模式执行顺序**
  - **验证需求: 1.1, 1.2**

- [ ] 12. 修改 SchedulingProgressReport 以支持遗传算法进度
  - 修改 DTOs/SchedulingProgressReport.cs
  - 添加 GeneticProgressInfo 属性
  - 添加遗传算法相关的阶段枚举值
  - _需求: 6.2_

- [ ] 13. 更新 ISchedulingService 接口
  - 修改 Services/Interfaces/ISchedulingService.cs
  - 添加 GetGeneticSchedulerConfigAsync 方法
  - 添加 SaveGeneticSchedulerConfigAsync 方法
  - 添加 ResetGeneticSchedulerConfigAsync 方法
  - 在 ExecuteSchedulingAsync 方法中添加 SchedulingMode 参数
  - _需求: 1.5, 7.1, 7.2, 7.4_

- [ ] 14. 实现 SchedulingService 的遗传算法集成
  - 修改 Services/SchedulingService.cs
  - 注入 HybridScheduler 和 GeneticSchedulerConfig
  - 实现配置管理方法（获取、保存、重置）
  - 实现根据模式选择执行路径的逻辑
  - 实现配置文件路径管理
  - 添加错误处理（配置加载失败等）
  - _需求: 1.1, 1.5, 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ]* 14.1 编写模式选择正确性的属性测试
  - **属性 4: 模式选择正确性**
  - **验证需求: 1.5**

- [ ] 15. 更新依赖注入配置
  - 修改 Extensions/ServiceCollectionExtensions.cs
  - 注册 GeneticScheduler
  - 注册 HybridScheduler
  - 注册 FitnessEvaluator
  - 注册策略实现（使用工厂模式）
  - 注册 GeneticSchedulerConfig（单例）
  - _需求: 9.1_

- [ ] 16. 创建算法配置视图模型
  - 创建 ViewModels/Scheduling/AlgorithmConfigViewModel.cs
  - 实现排班模式选择属性
  - 实现遗传算法配置属性（种群大小、代数等）
  - 实现配置验证逻辑
  - 实现恢复默认值命令
  - 实现配置保存逻辑
  - 实现配置加载逻辑
  - _需求: 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 7.1, 7.2, 7.4_

- [ ] 17. 创建算法配置UI步骤
  - 创建 Views/Scheduling/AlgorithmConfigStep.xaml
  - 实现排班模式选择UI（单选按钮）
  - 实现遗传算法参数配置UI（数字输入框、滑块）
  - 实现恢复默认值按钮
  - 实现参数验证和提示
  - 创建 Views/Scheduling/AlgorithmConfigStep.xaml.cs
  - _需求: 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 7.4_

- [ ] 18. 集成算法配置步骤到排班向导
  - 修改 ViewModels/Scheduling/SchedulingViewModel.cs
  - 添加 AlgorithmConfigViewModel 属性
  - 添加算法配置步骤到向导流程
  - 实现步骤间的数据传递
  - 修改 Views/Scheduling/CreateSchedulingPage.xaml
  - 添加算法配置步骤到向导UI
  - _需求: 1.5_

- [ ] 19. 更新应用常量
  - 修改 Constants/ApplicationConstants.cs
  - 添加遗传算法相关常量（默认配置值、文件路径等）
  - _需求: 7.3_

- [ ] 20. 检查点 - 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户

- [ ] 21. 添加详细日志记录（可选）
  - 在 GeneticScheduler 中添加详细日志
  - 记录每代的最优个体和统计信息
  - 记录交叉、变异操作的详细信息
  - 支持通过配置启用/禁用详细日志
  - _需求: 6.2_

- [ ] 22. 性能优化（高优先级优化）
  - 实现适应度评估的并行化（使用 Parallel.ForEach）
  - 实现适应度缓存机制（避免重复计算）
  - 实现早期终止条件（连续20代改进小于0.1%则终止）
  - 添加性能监控（记录各阶段耗时）
  - 实现智能种群初始化（30%局部扰动 + 30%中等扰动 + 40%随机）
  - 预期性能提升：6-12倍（从30-60秒降至5-10秒）
  - _参考设计文档"实现注意事项 > 性能优化"部分_

- [ ] 23. 最终检查点 - 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户
