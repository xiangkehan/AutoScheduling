# 实现计划

- [x] 1. 创建基础数据模型和枚举类型
  - 创建 DTOs/SchedulingMode.cs 枚举（GreedyOnly, Hybrid）
  - 创建 DTOs/SelectionStrategyType.cs 枚举
  - 创建 DTOs/CrossoverStrategyType.cs 枚举
  - 创建 DTOs/MutationStrategyType.cs 枚举
  - 创建 DTOs/GeneticProgressInfo.cs 类
  - _需求: 1.5, 9.2, 9.3, 9.4_

- [x] 2. 实现个体（Individual）类
  - 创建 SchedulingEngine/Core/Individual.cs
  - 实现基因编码（Dictionary<DateTime, int[,]>）
  - 实现 FromSchedule 静态方法
  - 实现 ToSchedule 方法
  - 实现 Clone 深拷贝方法
  - 实现未分配时段识别逻辑
  - _需求: 1.3, 8.1_

- [x]* 2.1 编写 Individual 类的属性测试
  - **属性 18: 未分配时段识别**
  - **验证需求: 8.1**

- [x] 3. 实现种群（Population）类
  - 创建 SchedulingEngine/Core/Population.cs
  - 实现 Initialize 方法（包含初始解和随机个体）
  - 实现 UpdateStatistics 方法
  - 实现 GetElites 方法
  - _需求: 1.3, 2.1, 2.5_

- [x]* 3.1 编写 Population 类的属性测试
  - **属性 2: 初始种群组成**
  - **验证需求: 1.3, 2.1**

- [x]* 3.2 编写精英保留机制的属性测试
  - **属性 6: 精英保留机制**
  - **验证需求: 2.5**

- [x] 4. 实现适应度评估器（FitnessEvaluator）
  - 创建 SchedulingEngine/Core/FitnessEvaluator.cs
  - 实现 EvaluateFitness 方法
  - 实现 CalculateHardConstraintViolations 方法（复用 ConstraintValidator）
  - 实现 CalculateSoftConstraintScore 方法（复用 SoftConstraintCalculator）
  - 实现 CalculateUnassignedPenalty 方法
  - 实现适应度综合计算公式
  - _需求: 3.1, 3.2, 3.3, 3.4, 8.3_

- [x]* 4.1 编写适应度计算的属性测试
  - **属性 7: 适应度计算完整性**
  - **验证需求: 3.1, 3.3, 3.4**

- [x]* 4.2 编写硬约束惩罚的属性测试
  - **属性 8: 硬约束惩罚单调性**
  - **验证需求: 3.2**

- [x]* 4.3 编写未分配时段惩罚的属性测试
  - **属性 19: 未分配时段惩罚**
  - **验证需求: 8.3**

- [x]* 4.4 编写适应度比较稳定性的属性测试
  - **属性 9: 适应度比较稳定性**
  - **验证需求: 3.5**

- [x] 5. 实现选择策略
  - 创建 SchedulingEngine/Strategies/Selection/ISelectionStrategy.cs 接口
  - 创建 SchedulingEngine/Strategies/Selection/RouletteWheelSelection.cs
  - 创建 SchedulingEngine/Strategies/Selection/TournamentSelection.cs
  - _需求: 4.2, 9.1, 9.2_

- [x]* 5.1 编写选择策略的属性测试
  - **属性 21: 策略配置应用（选择部分）**
  - **验证需求: 9.2**

- [x] 6. 实现交叉策略
  - 创建 SchedulingEngine/Strategies/Crossover/ICrossoverStrategy.cs 接口
  - 创建 SchedulingEngine/Strategies/Crossover/UniformCrossover.cs
  - 实现基因随机组合逻辑
  - 实现后代约束验证
  - 实现约束违反修复机制
  - 创建 SchedulingEngine/Strategies/Crossover/SinglePointCrossover.cs
  - _需求: 4.1, 4.3, 4.4, 4.5, 9.1, 9.3_

- [x]* 6.1 编写交叉操作的属性测试
  - **属性 10: 交叉操作合法性**
  - **验证需求: 4.1, 4.3**

- [x]* 6.2 编写交叉后约束验证的属性测试
  - **属性 11: 交叉后约束验证**
  - **验证需求: 4.4**

- [x]* 6.3 编写策略配置应用的属性测试（交叉部分）
  - **属性 21: 策略配置应用（交叉部分）**
  - **验证需求: 9.3**

- [x] 7. 实现变异策略
  - 创建 SchedulingEngine/Strategies/Mutation/IMutationStrategy.cs 接口
  - 创建 SchedulingEngine/Strategies/Mutation/SwapMutation.cs
  - 实现随机班次选择逻辑
  - 实现从可行人员中选择新人员
  - 实现优先填补未分配时段的逻辑
  - 实现变异后约束验证
  - 实现约束违反回滚机制
  - _需求: 5.1, 5.2, 5.3, 5.4, 5.5, 8.4, 9.1, 9.4_

- [x]* 7.1 编写变异操作可行性的属性测试
  - **属性 12: 变异操作可行性**
  - **验证需求: 5.3**

- [x]* 7.2 编写变异后约束验证的属性测试
  - **属性 13: 变异后约束验证**
  - **验证需求: 5.4**

- [x]* 7.3 编写变异优先填补未分配的属性测试
  - **属性 20: 变异优先填补未分配**
  - **验证需求: 8.4**

- [x]* 7.4 编写策略配置应用的属性测试（变异部分）
  - **属性 21: 策略配置应用（变异部分）**
  - **验证需求: 9.4**

- [x] 8. 实现遗传算法配置类
  - 创建 SchedulingEngine/Config/GeneticSchedulerConfig.cs
  - 实现所有配置属性（种群大小、代数、交叉率、变异率等）
  - 实现 SaveToFile 方法（JSON序列化）
  - 实现 LoadFromFile 方法（JSON反序列化）
  - 实现 GetDefault 静态方法
  - 实现配置验证逻辑
  - 添加错误处理（文件损坏、无效参数等）
  - _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 7.1, 7.2, 7.3, 7.4, 7.5_

- [x]* 8.1 编写配置持久化的属性测试
  - **属性 16: 配置持久化往返**
  - **验证需求: 7.1, 7.2**

- [x]* 8.2 编写配置重置的属性测试
  - **属性 17: 配置重置正确性**
  - **验证需求: 7.4**

- [x]* 8.3 编写权重配置影响的属性测试
  - **属性 22: 权重配置影响**
  - **验证需求: 9.5**

- [x] 9. 实现遗传算法调度器（GeneticScheduler）
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

- [x]* 9.1 编写遗传算法完整流程的属性测试
  - **属性 3: 最优解返回**
  - **验证需求: 1.4**

- [x]* 9.2 编写代数终止条件的属性测试
  - **属性 5: 代数终止条件**
  - **验证需求: 2.2**

- [x]* 9.3 编写进度报告的属性测试
  - **属性 14: 进度报告完整性**
  - **验证需求: 6.1, 6.2, 6.5**

- [x]* 9.4 编写取消响应性的属性测试
  - **属性 15: 取消响应性**
  - **验证需求: 6.4**

- [x] 10. 修改 GreedyScheduler 以支持混合模式
  - 修改 SchedulingEngine/GreedyScheduler.cs
  - 添加返回未分配时段信息的功能
  - 确保生成的 Schedule 包含完整的分配信息
  - _需求: 1.1, 8.1_

- [x] 11. 实现混合调度器（HybridScheduler）
  - 创建 SchedulingEngine/HybridScheduler.cs
  - 实现构造函数（注入 GreedyScheduler 和 GeneticScheduler）
  - 实现 ExecuteAsync 方法
  - 实现先执行贪心算法的逻辑
  - 实现将贪心解传递给遗传算法的逻辑
  - 实现整体进度报告协调
  - 实现取消令牌传递
  - _需求: 1.1, 1.2, 6.1, 6.4_

- [x]* 11.1 编写混合模式执行顺序的属性测试
  - **属性 1: 混合模式执行顺序**
  - **验证需求: 1.1, 1.2**

- [x] 12. 修改 SchedulingProgressReport 以支持遗传算法进度
  - 修改 DTOs/SchedulingProgressReport.cs
  - 添加 GeneticProgressInfo 属性
  - 添加遗传算法相关的阶段枚举值
  - _需求: 6.2_

- [x] 13. 更新 ISchedulingService 接口
  - 修改 Services/Interfaces/ISchedulingService.cs
  - 添加 GetGeneticSchedulerConfigAsync 方法
  - 添加 SaveGeneticSchedulerConfigAsync 方法
  - 添加 ResetGeneticSchedulerConfigAsync 方法
  - 在 ExecuteSchedulingAsync 方法中添加 SchedulingMode 参数
  - _需求: 1.5, 7.1, 7.2, 7.4_

- [x] 14. 实现 SchedulingService 的遗传算法集成
  - 修改 Services/SchedulingService.cs
  - 注入 HybridScheduler 和 GeneticSchedulerConfig
  - 实现配置管理方法（获取、保存、重置）
  - 实现根据模式选择执行路径的逻辑
  - 实现配置文件路径管理
  - 添加错误处理（配置加载失败等）
  - _需求: 1.1, 1.5, 7.1, 7.2, 7.3, 7.4, 7.5_

- [x]* 14.1 编写模式选择正确性的属性测试
  - **属性 4: 模式选择正确性**
  - **验证需求: 1.5**

- [x] 15. 更新依赖注入配置
  - 修改 Extensions/ServiceCollectionExtensions.cs
  - 注册 GeneticScheduler
  - 注册 HybridScheduler
  - 注册 FitnessEvaluator
  - 注册策略实现（使用工厂模式）
  - 注册 GeneticSchedulerConfig（单例）
  - _需求: 9.1_

- [x] 16. 创建算法配置视图模型
  - 创建 ViewModels/Scheduling/AlgorithmConfigViewModel.cs
  - 实现排班模式选择属性
  - 实现遗传算法配置属性（种群大小、代数等）
  - 实现配置验证逻辑
  - 实现恢复默认值命令
  - 实现配置保存逻辑
  - 实现配置加载逻辑
  - _需求: 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 7.1, 7.2, 7.4_

- [x] 17. 创建算法配置UI步骤
  - 创建 Views/Scheduling/AlgorithmConfigStep.xaml
  - 实现排班模式选择UI（单选按钮）
  - 实现遗传算法参数配置UI（数字输入框、滑块）
  - 实现恢复默认值按钮
  - 实现参数验证和提示
  - 创建 Views/Scheduling/AlgorithmConfigStep.xaml.cs
  - _需求: 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 7.4_

- [x] 18. 集成算法配置步骤到排班向导
  - 修改 ViewModels/Scheduling/SchedulingViewModel.cs
  - 添加 AlgorithmConfigViewModel 属性
  - 添加算法配置步骤到向导流程
  - 实现步骤间的数据传递
  - 修改 Views/Scheduling/CreateSchedulingPage.xaml
  - 添加算法配置步骤到向导UI
  - _需求: 1.5_

- [x] 19. 更新应用常量
  - 修改 Constants/ApplicationConstants.cs
  - 添加遗传算法相关常量（默认配置值、文件路径等）
  - _需求: 7.3_

- [x] 20. 检查点 - 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户

- [ ] 21. 添加详细日志记录（可选）
  - 在 GeneticScheduler 中添加详细日志
  - 记录每代的最优个体和统计信息
  - 记录交叉、变异操作的详细信息
  - 支持通过配置启用/禁用详细日志
  - _需求: 6.2_

- [ ] 22. 检查点 - 确保核心功能测试通过
  - 确保所有核心功能测试通过，如有问题请询问用户


## 模板和草稿功能更新

- [ ] 24. 更新模板功能以支持遗传算法配置（含性能优化）
  - 修改 Models/SchedulingTemplate.cs
  - 在 StrategyConfig JSON 中添加遗传算法配置字段
  - 添加 SchedulingMode 字段（GreedyOnly 或 Hybrid）
  - 添加 GeneticAlgorithmConfig 字段（种群大小、代数等）
  - 修改 Services/Interfaces/ITemplateService.cs
  - 确保 UseTemplateAsync 方法支持传递算法模式和配置
  - 修改 Services/TemplateService.cs
  - 实现模板创建时保存遗传算法配置
  - 实现使用模板时加载遗传算法配置
  - **性能优化**: 实现模板配置缓存（ConcurrentDictionary，10分钟过期）
  - **性能优化**: 使用优化的 JsonSerializerOptions 序列化配置
  - 修改 ViewModels/Scheduling/TemplateViewModel.cs
  - 添加算法配置相关属性
  - 实现保存模板时包含算法配置
  - 修改 Views/Scheduling/TemplatePage.xaml
  - 添加算法配置选项到模板编辑界面
  - _需求: 7.1, 7.2_
  - _性能目标: 模板加载提升 3-4倍_

- [ ] 25. 实现排班进度中断时保存草稿功能（含性能优化）
  - 修改 ViewModels/Scheduling/SchedulingProgressViewModel.cs
  - 添加 SaveProgressAsDraftCommand 命令
  - 实现在排班执行过程中保存当前进度为草稿
  - **性能优化**: 实现自动保存节流（2分钟间隔 + 5%进度变化阈值）
  - **性能优化**: 异步保存，不阻塞主线程
  - 修改 Views/Scheduling/SchedulingProgressPage.xaml
  - 添加"保存草稿"按钮到进度界面
  - 添加自动保存状态提示（显示上次保存时间）
  - 修改 Services/Interfaces/ISchedulingService.cs
  - 添加 SaveProgressAsDraftAsync 方法
  - 添加 GetDraftProgressAsync 方法（获取草稿的完成进度）
  - 修改 Services/SchedulingService.cs
  - **性能优化**: 实现增量保存（只保存最优个体和必要状态，不保存整个种群）
  - 实现保存部分完成的排班结果为草稿
  - 保存当前算法状态（已完成的天数、当前代数等）
  - 实现从草稿恢复排班进度
  - 修改 Models/Schedule.cs（如果需要）
  - 添加进度相关字段（完成百分比、当前阶段等）
  - 修改 DTOs/ScheduleDto.cs
  - 添加 ProgressPercentage 字段
  - 添加 CurrentStage 字段
  - 添加 IsPartialResult 字段（标识是否为部分结果）
  - _需求: 6.4（取消操作相关）_
  - _性能目标: 草稿保存提升 8-10倍（从1-2秒降至0.1-0.2秒）_

- [ ] 26. 实现从草稿恢复排班功能（含性能优化）
  - 修改 ViewModels/Scheduling/SchedulingViewModel.cs
  - 添加检测未完成草稿的逻辑
  - 添加"继续上次排班"选项
  - 修改 Views/Scheduling/CreateSchedulingPage.xaml
  - 添加未完成草稿提示
  - 添加"继续"和"重新开始"按钮
  - 修改 Services/SchedulingService.cs
  - 实现从草稿恢复排班上下文
  - **性能优化**: 实现智能恢复策略（进度<30%重启，>=30%恢复）
  - **性能优化**: 使用最优解的变体快速重建种群
  - 实现继续执行排班算法
  - 确保遗传算法可以从中断点继续（保存种群状态）
  - 修改 SchedulingEngine/GeneticScheduler.cs
  - 添加 ResumeFromStateAsync 方法
  - 实现保存和恢复种群状态
  - 实现从最优解快速重建种群
  - 修改 SchedulingEngine/HybridScheduler.cs
  - 支持从部分完成的贪心解继续执行
  - _需求: 6.4（取消后恢复）_
  - _性能目标: 草稿恢复提升 2-3倍（从5-10秒降至2-4秒）_

- [x] 27. 更新草稿列表页面显示算法信息（含性能优化）
  - 修改 ViewModels/Scheduling/DraftsViewModel.cs（如果存在）
  - 添加显示使用的算法模式（仅贪心/混合）
  - 添加显示完成进度
  - 添加显示是否可恢复
  - **性能优化**: 实现分页加载（每页20条）
  - **性能优化**: 实现过滤功能（只显示可恢复、按模式过滤）
  - **性能优化**: 使用虚拟化列表控件（VirtualizingStackPanel）
  - 修改 Views/Scheduling/DraftsPage.xaml
  - 更新草稿列表项模板
  - 显示算法模式标签
  - 显示进度条
  - 添加"继续排班"按钮（针对未完成的草稿）
  - 添加分页控件（上一页、下一页、页码）
  - 添加过滤选项
  - 修改 DTOs/ScheduleSummaryDto.cs
  - 添加 SchedulingMode 字段
  - 添加 ProgressPercentage 字段
  - 添加 IsResumable 字段
  - _需求: 1.5（模式显示）_
  - _性能目标: 草稿列表加载提升 8-10倍（当草稿数量>50时）_

- [ ] 28. 添加模板和草稿的算法配置验证（含性能优化）
  - 创建 Validators/GeneticConfigValidator.cs
  - 实现遗传算法配置参数验证
  - 验证种群大小范围（10-200）
  - 验证代数范围（10-500）
  - 验证交叉率范围（0.0-1.0）
  - 验证变异率范围（0.0-1.0）
  - 验证精英保留数量（0-10）
  - **性能优化**: 实现验证结果缓存（使用配置哈希作为键）
  - **性能优化**: 避免重复验证相同的配置
  - 修改 Services/TemplateService.cs
  - 在保存模板时验证算法配置
  - 在使用模板时验证配置有效性
  - 修改 Services/SchedulingService.cs
  - 在保存草稿时验证算法配置
  - 在恢复草稿时验证配置兼容性
  - _需求: 2.1, 2.2, 2.3, 2.4, 2.5_
  - _性能目标: 验证速度提升 90%+（缓存命中时）_

- [ ] 29. 检查点 - 模板和草稿功能测试
  - 测试创建包含遗传算法配置的模板
  - 测试使用模板创建排班（验证算法配置正确应用）
  - 测试排班过程中保存草稿
  - 测试从草稿恢复排班
  - 测试草稿列表显示算法信息
  - 测试配置验证功能
  - 确保所有测试通过，如有问题请询问用户

## 性能优化任务

- [ ] 30. 实现适应度评估并行化
  - 修改 SchedulingEngine/Core/FitnessEvaluator.cs
  - 使用 Parallel.ForEach 并行评估种群中所有个体的适应度
  - 确保 FitnessEvaluator 是线程安全的
  - 使用 ThreadLocal<Random> 避免随机数生成器竞争
  - 控制并行度以避免过度线程切换
  - 添加性能监控（记录并行化前后的耗时）
  - _预期提升: 2-4倍（取决于CPU核心数）_
  - _参考设计文档"实现注意事项 > 性能优化 > 1.1"_

- [ ] 31. 实现适应度缓存机制
  - 修改 SchedulingEngine/Core/Individual.cs
  - 添加 _cachedFitness 私有字段和 _isDirty 标志
  - 实现 Fitness 属性的缓存逻辑
  - 添加 MarkDirty() 方法，在基因改变时调用
  - 确保交叉和变异操作后正确标记为脏
  - 添加缓存命中率统计
  - _预期减少: 30-40% 适应度计算次数_
  - _参考设计文档"实现注意事项 > 性能优化 > 1.2"_

- [ ] 32. 实现早期终止条件
  - 修改 SchedulingEngine/GeneticScheduler.cs
  - 添加 ShouldTerminateEarly 方法
  - 实现连续N代（默认20代）无显著改进的检测
  - 设置改进阈值（默认0.1%）
  - 记录最优适应度历史
  - 在主循环中检查早期终止条件
  - 在进度报告中显示早期终止信息
  - _预期节省: 30-50% 执行时间（已收敛场景）_
  - _参考设计文档"实现注意事项 > 性能优化 > 1.3"_

- [ ] 33. 实现智能种群初始化
  - 修改 SchedulingEngine/Core/Population.cs
  - 实现 InitializePopulationSmart 方法
  - 30% 个体：贪心解的局部扰动（扰动级别0.1）
  - 30% 个体：贪心解的中等扰动（扰动级别0.3）
  - 40% 个体：完全随机生成
  - 实现 CreateVariant 方法（从现有解创建变体）
  - 确保初始种群多样性
  - _预期减少: 20-30% 迭代次数_
  - _参考设计文档"实现注意事项 > 性能优化 > 1.6"_

- [ ] 34. 添加性能监控系统
  - 创建 SchedulingEngine/Core/PerformanceMonitor.cs
  - 实现 PerformanceMetrics 类（记录各阶段耗时）
  - 实现 RecordPhase 方法（记录单个阶段）
  - 实现 PrintReport 方法（输出性能报告）
  - 在 GeneticScheduler 中集成性能监控
  - 记录以下阶段：
    - 适应度评估总时间
    - 选择操作时间
    - 交叉操作时间
    - 变异操作时间
    - 种群更新时间
  - 在配置中添加 EnablePerformanceMonitoring 开关
  - _参考设计文档"实现注意事项 > 性能优化 > 1.9"_

- [ ] 35. 实现模板配置缓存
  - 创建 Services/TemplateConfigCache.cs
  - 使用 ConcurrentDictionary 存储缓存
  - 设置缓存过期时间（默认10分钟）
  - 实现 GetTemplateAsync 方法（带缓存）
  - 实现自动过期清理机制
  - 在 TemplateService 中集成缓存
  - 添加缓存命中率统计
  - _预期提升: 50-70% 模板加载速度_
  - _参考设计文档"模板和草稿功能集成 > 5.1"_

- [ ] 36. 实现配置序列化优化
  - 创建 Helpers/OptimizedConfigSerializer.cs
  - 配置高性能 JsonSerializerOptions
  - 设置 WriteIndented = false（减小体积）
  - 设置 DefaultIgnoreCondition = WhenWritingNull
  - 使用 JsonStringEnumConverter 处理枚举
  - 在 TemplateService 和 SchedulingService 中使用优化的序列化器
  - _预期提升: 30-40% 序列化速度_
  - _参考设计文档"模板和草稿功能集成 > 5.5"_

- [ ] 37. 实现草稿增量保存
  - 创建 Services/IncrementalDraftSaver.cs
  - 定义 MinimalGeneticState 类（只包含必要状态）
  - 只保存最优个体的基因
  - 保存随机种子以便重现
  - 只保存最近10代的适应度历史
  - 实现 SaveMinimalStateAsync 方法
  - 在 SchedulingService 中集成增量保存
  - _预期提升: 80-90% 保存速度_
  - _参考设计文档"模板和草稿功能集成 > 5.2"_

- [ ] 38. 实现自动保存节流
  - 创建 Services/ThrottledAutoSaver.cs
  - 设置最小保存间隔（默认2分钟）
  - 设置最小进度变化阈值（默认5%）
  - 实现 TryAutoSaveAsync 方法
  - 检测关键进度点（25%, 50%, 75%）
  - 使用异步保存，不阻塞主线程
  - 在 SchedulingProgressViewModel 中集成节流器
  - _预期影响: < 2% 性能开销_
  - _参考设计文档"模板和草稿功能集成 > 5.4"_

- [ ] 39. 实现智能草稿恢复策略
  - 创建 Services/OptimizedDraftResumer.cs
  - 实现进度判断逻辑（< 30% 重启，>= 30% 恢复）
  - 实现 RestartWithBestSolutionAsync 方法
  - 实现从最优解快速重建种群
  - 实现 ContinueFromGenerationAsync 方法
  - 在 SchedulingService 中集成智能恢复
  - _预期提升: 40-60% 恢复速度_
  - _参考设计文档"模板和草稿功能集成 > 5.3"_

- [ ] 40. 实现草稿列表分页加载
  - 创建 Services/PaginatedDraftLoader.cs
  - 设置每页大小（默认20条）
  - 实现 GetDraftsPagedAsync 方法
  - 实现过滤功能（按模式、可恢复状态）
  - 修改 DraftsViewModel 支持分页
  - 修改 DraftsPage.xaml 添加分页控件
  - 使用 VirtualizingStackPanel 优化列表渲染
  - _预期提升: 70-90% 列表加载速度（当草稿数量>50）_
  - _参考设计文档"模板和草稿功能集成 > 5.6"_

- [ ] 41. 实现配置验证缓存
  - 创建 Validators/CachedConfigValidator.cs
  - 使用 ConcurrentDictionary 存储验证结果
  - 实现 ComputeConfigHash 方法（使用SHA256）
  - 实现带缓存的 Validate 方法
  - 在 TemplateService 和 SchedulingService 中使用缓存验证器
  - 添加缓存命中率统计
  - _预期提升: 90%+ 验证速度（缓存命中时）_
  - _参考设计文档"模板和草稿功能集成 > 5.7"_

- [ ] 42. 性能优化验证和基准测试
  - 创建性能测试场景（小/中/大规模）
  - 小规模: 20人员 × 5哨位 × 3天
  - 中规模: 50人员 × 10哨位 × 7天
  - 大规模: 100人员 × 20哨位 × 14天
  - 记录优化前后的性能数据
  - 验证各项优化的实际效果
  - 确保优化不影响功能正确性
  - 生成性能报告
  - _参考设计文档"性能优化实施路线图"_

- [ ] 43. 最终检查点 - 性能优化完成
  - 确保所有性能优化测试通过
  - 验证性能目标达成：
    - 遗传算法：6-12倍提升
    - 模板加载：3-4倍提升
    - 草稿保存：8-10倍提升
    - 草稿恢复：2-3倍提升
    - 草稿列表：8-10倍提升
  - 如有问题请询问用户
