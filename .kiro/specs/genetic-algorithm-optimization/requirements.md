# 需求文档

## 简介

本功能旨在为排班系统引入遗传算法优化引擎，以贪心算法的解作为初始种群，通过遗传算法进一步优化排班结果。系统将提供两种排班模式：仅贪心算法（适用于低性能设备）和贪心+遗传算法（默认模式，获得更优解）。

## 术语表

- **GreedyScheduler（贪心调度器）**: 现有的基于MRV启发式策略的排班算法
- **GeneticScheduler（遗传调度器）**: 新增的遗传算法优化引擎
- **HybridScheduler（混合调度器）**: 整合贪心和遗传算法的统一调度器
- **Individual（个体）**: 遗传算法中的一个完整排班方案
- **Population（种群）**: 遗传算法中的多个排班方案集合
- **Fitness（适应度）**: 评估排班方案质量的综合得分
- **Crossover（交叉）**: 遗传算法中两个个体交换基因片段的操作
- **Mutation（变异）**: 遗传算法中随机改变个体基因的操作
- **Generation（代）**: 遗传算法的一次迭代周期
- **SchedulingMode（排班模式）**: 用户选择的算法执行模式（仅贪心或混合模式）

## 需求

### 需求 1

**用户故事:** 作为系统用户，我希望系统能够使用遗传算法优化排班结果，以便获得更优质的排班方案

#### 验收标准

1. WHEN 用户选择混合排班模式 THEN HybridScheduler SHALL 先执行 GreedyScheduler 生成初始解
2. WHEN GreedyScheduler 完成初始解生成 THEN HybridScheduler SHALL 将初始解作为种群的一部分传递给 GeneticScheduler
3. WHEN GeneticScheduler 开始执行 THEN 系统 SHALL 使用初始解和随机生成的个体构建初始种群
4. WHEN GeneticScheduler 完成优化 THEN 系统 SHALL 返回适应度最高的排班方案
5. WHEN 用户选择仅贪心模式 THEN 系统 SHALL 仅执行 GreedyScheduler 并返回结果

### 需求 2

**用户故事:** 作为系统用户，我希望能够配置遗传算法的参数，以便根据实际需求调整优化效果和执行时间

#### 验收标准

1. WHEN 用户配置种群大小 THEN GeneticScheduler SHALL 使用指定的种群大小初始化种群
2. WHEN 用户配置最大代数 THEN GeneticScheduler SHALL 在达到最大代数时停止迭代
3. WHEN 用户配置交叉率 THEN GeneticScheduler SHALL 使用指定的交叉率执行交叉操作
4. WHEN 用户配置变异率 THEN GeneticScheduler SHALL 使用指定的变异率执行变异操作
5. WHEN 用户配置精英保留数量 THEN GeneticScheduler SHALL 在每代保留指定数量的最优个体

### 需求 3

**用户故事:** 作为系统用户，我希望遗传算法能够正确评估排班方案的质量，以便优化过程朝着正确的方向进行

#### 验收标准

1. WHEN GeneticScheduler 评估个体适应度 THEN 系统 SHALL 计算硬约束违反数量
2. WHEN 个体违反硬约束 THEN 系统 SHALL 对适应度施加惩罚
3. WHEN GeneticScheduler 评估个体适应度 THEN 系统 SHALL 计算软约束得分（充分休息、时段平衡、休息日平衡、工作量平衡）
4. WHEN 计算适应度 THEN 系统 SHALL 综合硬约束惩罚和软约束得分生成最终适应度值
5. WHEN 两个个体适应度相同 THEN 系统 SHALL 使用稳定的比较规则确定优先级

### 需求 4

**用户故事:** 作为系统用户，我希望遗传算法能够有效地进行交叉操作，以便生成更优的后代个体

#### 验收标准

1. WHEN GeneticScheduler 执行交叉操作 THEN 系统 SHALL 随机选择两个父代个体
2. WHEN 选择父代个体 THEN 系统 SHALL 使用轮盘赌选择或锦标赛选择策略
3. WHEN 执行交叉 THEN 系统 SHALL 随机选择交叉点将父代基因片段组合
4. WHEN 生成后代个体 THEN 系统 SHALL 验证后代是否满足基本约束
5. WHEN 后代违反约束 THEN 系统 SHALL 尝试修复或重新生成

### 需求 5

**用户故事:** 作为系统用户，我希望遗传算法能够进行变异操作，以便增加种群多样性并避免局部最优

#### 验收标准

1. WHEN GeneticScheduler 执行变异操作 THEN 系统 SHALL 根据变异率随机选择个体进行变异
2. WHEN 对个体进行变异 THEN 系统 SHALL 随机选择一个或多个班次进行调整
3. WHEN 调整班次 THEN 系统 SHALL 从该时段的可行人员中随机选择新人员
4. WHEN 变异后 THEN 系统 SHALL 验证个体是否仍满足硬约束
5. WHEN 变异导致约束违反 THEN 系统 SHALL 回滚变异或尝试修复

### 需求 6

**用户故事:** 作为系统用户，我希望能够实时查看遗传算法的优化进度，以便了解算法执行状态

#### 验收标准

1. WHEN GeneticScheduler 开始执行 THEN 系统 SHALL 报告初始化阶段进度
2. WHEN GeneticScheduler 完成一代迭代 THEN 系统 SHALL 报告当前代数、最优适应度和平均适应度
3. WHEN GeneticScheduler 执行过程中 THEN 系统 SHALL 定期更新进度百分比
4. WHEN 用户取消操作 THEN 系统 SHALL 响应取消令牌并停止执行
5. WHEN GeneticScheduler 完成 THEN 系统 SHALL 报告最终结果和执行统计信息

### 需求 7

**用户故事:** 作为系统用户，我希望系统能够保存遗传算法的配置，以便下次使用时无需重新配置

#### 验收标准

1. WHEN 用户修改遗传算法配置 THEN 系统 SHALL 将配置保存到本地存储
2. WHEN 用户启动排班 THEN 系统 SHALL 加载上次保存的配置作为默认值
3. WHEN 配置文件不存在 THEN 系统 SHALL 使用预设的默认配置
4. WHEN 用户重置配置 THEN 系统 SHALL 恢复到预设的默认值
5. WHEN 配置文件损坏 THEN 系统 SHALL 使用默认配置并记录警告

### 需求 8

**用户故事:** 作为系统用户，我希望系统能够正确处理贪心算法的不完整解，以便遗传算法能够在此基础上进行优化

#### 验收标准

1. WHEN GreedyScheduler 返回包含未分配时段的解 THEN 系统 SHALL 识别所有未分配的时段
2. WHEN 构建初始种群 THEN 系统 SHALL 尝试为未分配时段随机分配可行人员
3. WHEN 未分配时段无可行人员 THEN 系统 SHALL 在适应度计算中对该时段施加高惩罚
4. WHEN GeneticScheduler 执行变异操作 THEN 系统 SHALL 优先尝试填补未分配时段
5. WHEN 最终解仍包含未分配时段 THEN 系统 SHALL 在结果中明确标记这些时段

### 需求 9

**用户故事:** 作为系统开发者，我希望遗传算法引擎具有良好的可扩展性，以便未来添加新的优化策略

#### 验收标准

1. WHEN 实现 GeneticScheduler THEN 系统 SHALL 使用接口定义选择、交叉和变异策略
2. WHEN 添加新的选择策略 THEN 系统 SHALL 能够通过配置切换策略
3. WHEN 添加新的交叉策略 THEN 系统 SHALL 能够通过配置切换策略
4. WHEN 添加新的变异策略 THEN 系统 SHALL 能够通过配置切换策略
5. WHEN 扩展适应度函数 THEN 系统 SHALL 支持自定义权重配置
