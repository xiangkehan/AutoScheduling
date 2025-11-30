# 设计文档

## 概述

本设计文档描述了遗传算法优化引擎的架构和实现细节。系统将在现有贪心算法的基础上，引入遗传算法进行进一步优化，提供两种排班模式供用户选择。

### 设计目标

1. **优化质量**: 通过遗传算法获得比单纯贪心算法更优的排班方案
2. **性能兼容**: 保留仅贪心模式，确保低性能设备也能正常使用
3. **可配置性**: 提供丰富的参数配置，满足不同场景需求
4. **可扩展性**: 使用策略模式，便于未来扩展新的优化策略
5. **用户体验**: 提供实时进度反馈和取消支持

## 文件结构

### 新增文件

```
SchedulingEngine/
├── GeneticScheduler.cs                    # 遗传算法调度器（新增）
├── HybridScheduler.cs                     # 混合调度器（新增）
├── GreedyScheduler.cs                     # 贪心调度器（已存在，需修改）
├── Core/
│   ├── Individual.cs                      # 个体类（新增）
│   ├── Population.cs                      # 种群类（新增）
│   ├── FitnessEvaluator.cs               # 适应度评估器（新增）
│   ├── ConstraintValidator.cs            # 约束验证器（已存在）
│   ├── SoftConstraintCalculator.cs       # 软约束计算器（已存在）
│   └── SchedulingContext.cs              # 调度上下文（已存在）
├── Strategies/
│   ├── Selection/
│   │   ├── ISelectionStrategy.cs         # 选择策略接口（新增）
│   │   ├── RouletteWheelSelection.cs     # 轮盘赌选择（新增）
│   │   └── TournamentSelection.cs        # 锦标赛选择（新增）
│   ├── Crossover/
│   │   ├── ICrossoverStrategy.cs         # 交叉策略接口（新增）
│   │   ├── UniformCrossover.cs           # 均匀交叉（新增）
│   │   └── SinglePointCrossover.cs       # 单点交叉（新增）
│   └── Mutation/
│       ├── IMutationStrategy.cs          # 变异策略接口（新增）
│       └── SwapMutation.cs               # 交换变异（新增）
└── Config/
    └── GeneticSchedulerConfig.cs         # 遗传算法配置（新增）

DTOs/
├── SchedulingMode.cs                      # 排班模式枚举（新增）
├── SelectionStrategyType.cs               # 选择策略类型枚举（新增）
├── CrossoverStrategyType.cs               # 交叉策略类型枚举（新增）
├── MutationStrategyType.cs                # 变异策略类型枚举（新增）
├── GeneticProgressInfo.cs                 # 遗传算法进度信息（新增）
└── SchedulingProgressReport.cs            # 排班进度报告（已存在，需修改）

Services/
├── Interfaces/
│   └── ISchedulingService.cs              # 排班服务接口（已存在，需修改）
└── SchedulingService.cs                   # 排班服务实现（已存在，需修改）

ViewModels/Scheduling/
├── SchedulingViewModel.cs                 # 排班视图模型（已存在，需修改）
└── AlgorithmConfigViewModel.cs            # 算法配置视图模型（新增）

Views/Scheduling/
├── CreateSchedulingPage.xaml              # 排班创建页面（已存在，需修改）
├── CreateSchedulingPage.xaml.cs           # 排班创建页面代码（已存在，需修改）
├── AlgorithmConfigStep.xaml               # 算法配置步骤（新增）
└── AlgorithmConfigStep.xaml.cs            # 算法配置步骤代码（新增）

Constants/
└── ApplicationConstants.cs                # 应用常量（已存在，需修改）
```

### 文件修改说明

#### 需要修改的现有文件

1. **SchedulingEngine/GreedyScheduler.cs**
   - 添加返回未分配时段信息的功能
   - 确保可以被 HybridScheduler 调用

2. **DTOs/SchedulingProgressReport.cs**
   - 添加遗传算法相关的进度字段
   - 添加 GeneticProgressInfo 属性

3. **Services/Interfaces/ISchedulingService.cs**
   - 添加获取/保存遗传算法配置的方法
   - 添加模式选择参数

4. **Services/SchedulingService.cs**
   - 集成 HybridScheduler
   - 根据模式选择执行路径
   - 实现配置管理方法

5. **ViewModels/Scheduling/SchedulingViewModel.cs**
   - 添加算法模式选择属性
   - 添加遗传算法配置属性
   - 集成算法配置步骤

6. **Views/Scheduling/CreateSchedulingPage.xaml**
   - 添加算法配置步骤到向导流程

7. **Constants/ApplicationConstants.cs**
   - 添加遗传算法相关常量
   - 添加配置文件路径常量

### 文件大小估算

| 文件 | 预估行数 | 说明 |
|------|---------|------|
| GeneticScheduler.cs | 400-500 | 核心遗传算法逻辑 |
| HybridScheduler.cs | 150-200 | 协调贪心和遗传算法 |
| Individual.cs | 200-250 | 个体表示和操作 |
| Population.cs | 150-200 | 种群管理 |
| FitnessEvaluator.cs | 250-300 | 适应度评估 |
| ISelectionStrategy.cs | 20-30 | 接口定义 |
| RouletteWheelSelection.cs | 80-100 | 轮盘赌选择实现 |
| TournamentSelection.cs | 60-80 | 锦标赛选择实现 |
| ICrossoverStrategy.cs | 20-30 | 接口定义 |
| UniformCrossover.cs | 150-200 | 均匀交叉实现 |
| SinglePointCrossover.cs | 120-150 | 单点交叉实现 |
| IMutationStrategy.cs | 20-30 | 接口定义 |
| SwapMutation.cs | 150-200 | 交换变异实现 |
| GeneticSchedulerConfig.cs | 150-200 | 配置类和序列化 |
| AlgorithmConfigViewModel.cs | 200-250 | 配置界面逻辑 |
| AlgorithmConfigStep.xaml | 100-150 | 配置界面XAML |
| 其他枚举和DTO | 50-100 | 简单数据类型 |

**总计**: 约 2200-2900 行新代码，符合模块化设计原则

## 架构

### 整体架构

```
┌─────────────────────────────────────────────────────────┐
│                   SchedulingService                      │
│  (协调调度流程，根据模式选择执行路径)                    │
└────────────────────┬────────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌──────────────────────┐
│ GreedyScheduler │    │  HybridScheduler     │
│  (仅贪心模式)   │    │  (混合模式)          │
└─────────────────┘    └──────────┬───────────┘
                                  │
                       ┌──────────┴──────────┐
                       ▼                     ▼
              ┌─────────────────┐  ┌──────────────────┐
              │ GreedyScheduler │  │ GeneticScheduler │
              │  (生成初始解)   │  │  (优化解)        │
              └─────────────────┘  └──────────────────┘
```

### 核心组件

1. **HybridScheduler**: 混合调度器，协调贪心和遗传算法的执行
2. **GeneticScheduler**: 遗传算法调度器，执行遗传算法优化
3. **Individual**: 个体类，表示一个完整的排班方案
4. **Population**: 种群类，管理多个个体
5. **FitnessEvaluator**: 适应度评估器，计算个体质量
6. **SelectionStrategy**: 选择策略接口及实现
7. **CrossoverStrategy**: 交叉策略接口及实现
8. **MutationStrategy**: 变异策略接口及实现
9. **GeneticSchedulerConfig**: 遗传算法配置类

## 组件和接口

### 1. HybridScheduler

```csharp
public class HybridScheduler
{
    private readonly SchedulingContext _context;
    private readonly GreedyScheduler _greedyScheduler;
    private readonly GeneticScheduler _geneticScheduler;
    
    public async Task<Schedule> ExecuteAsync(
        IProgress<SchedulingProgressReport>? progress,
        CancellationToken cancellationToken);
}
```

**职责**:
- 协调贪心和遗传算法的执行顺序
- 将贪心算法的解传递给遗传算法
- 管理整体进度报告

### 2. GeneticScheduler

```csharp
public class GeneticScheduler
{
    private readonly SchedulingContext _context;
    private readonly GeneticSchedulerConfig _config;
    private readonly FitnessEvaluator _fitnessEvaluator;
    private readonly ISelectionStrategy _selectionStrategy;
    private readonly ICrossoverStrategy _crossoverStrategy;
    private readonly IMutationStrategy _mutationStrategy;
    
    public async Task<Schedule> ExecuteAsync(
        Schedule? initialSolution,
        IProgress<SchedulingProgressReport>? progress,
        CancellationToken cancellationToken);
}
```

**职责**:
- 初始化种群（包含初始解和随机个体）
- 执行遗传算法主循环
- 管理精英保留
- 返回最优解

### 3. Individual

```csharp
public class Individual
{
    // 基因编码：[日期][时段][哨位] = 人员ID（-1表示未分配）
    public Dictionary<DateTime, int[,]> Genes { get; set; }
    
    // 适应度值（越高越好）
    public double Fitness { get; set; }
    
    // 硬约束违反数量
    public int HardConstraintViolations { get; set; }
    
    // 软约束得分
    public double SoftConstraintScore { get; set; }
    
    // 未分配时段数量
    public int UnassignedSlots { get; set; }
    
    // 从 Schedule 创建个体
    public static Individual FromSchedule(Schedule schedule, SchedulingContext context);
    
    // 转换为 Schedule
    public Schedule ToSchedule(SchedulingContext context);
    
    // 深拷贝
    public Individual Clone();
}
```


### 4. Population

```csharp
public class Population
{
    public List<Individual> Individuals { get; set; }
    
    public Individual BestIndividual { get; private set; }
    
    public double AverageFitness { get; private set; }
    
    // 初始化种群
    public void Initialize(
        Schedule? initialSolution,
        int populationSize,
        SchedulingContext context);
    
    // 更新统计信息
    public void UpdateStatistics();
    
    // 获取精英个体
    public List<Individual> GetElites(int count);
}
```

### 5. FitnessEvaluator

```csharp
public class FitnessEvaluator
{
    private readonly SchedulingContext _context;
    private readonly ConstraintValidator _constraintValidator;
    private readonly SoftConstraintCalculator _softConstraintCalculator;
    
    // 评估个体适应度
    public void EvaluateFitness(Individual individual);
    
    // 计算硬约束违反数量
    private int CalculateHardConstraintViolations(Individual individual);
    
    // 计算软约束得分
    private double CalculateSoftConstraintScore(Individual individual);
    
    // 计算未分配时段惩罚
    private double CalculateUnassignedPenalty(Individual individual);
}
```

### 6. 策略接口

```csharp
// 选择策略接口
public interface ISelectionStrategy
{
    Individual Select(Population population, Random random);
}

// 交叉策略接口
public interface ICrossoverStrategy
{
    (Individual child1, Individual child2) Crossover(
        Individual parent1,
        Individual parent2,
        SchedulingContext context,
        Random random);
}

// 变异策略接口
public interface IMutationStrategy
{
    void Mutate(
        Individual individual,
        SchedulingContext context,
        Random random);
}
```

### 7. 策略实现

#### RouletteWheelSelection（轮盘赌选择）

```csharp
public class RouletteWheelSelection : ISelectionStrategy
{
    public Individual Select(Population population, Random random)
    {
        // 计算适应度总和
        // 生成随机数
        // 根据适应度比例选择个体
    }
}
```

#### TournamentSelection（锦标赛选择）

```csharp
public class TournamentSelection : ISelectionStrategy
{
    private readonly int _tournamentSize;
    
    public Individual Select(Population population, Random random)
    {
        // 随机选择 tournamentSize 个个体
        // 返回其中适应度最高的
    }
}
```

#### UniformCrossover（均匀交叉）

```csharp
public class UniformCrossover : ICrossoverStrategy
{
    public (Individual, Individual) Crossover(
        Individual parent1,
        Individual parent2,
        SchedulingContext context,
        Random random)
    {
        // 对每个基因位，随机选择来自父代1或父代2
        // 验证后代约束
        // 尝试修复违反约束的后代
    }
}
```

#### SinglePointCrossover（单点交叉）

```csharp
public class SinglePointCrossover : ICrossoverStrategy
{
    public (Individual, Individual) Crossover(
        Individual parent1,
        Individual parent2,
        SchedulingContext context,
        Random random)
    {
        // 随机选择交叉点（按日期）
        // 交换交叉点后的基因片段
        // 验证和修复
    }
}
```

#### SwapMutation（交换变异）

```csharp
public class SwapMutation : IMutationStrategy
{
    public void Mutate(
        Individual individual,
        SchedulingContext context,
        Random random)
    {
        // 随机选择一个或多个班次
        // 从可行人员中随机选择新人员
        // 优先填补未分配时段
        // 验证约束，违反则回滚
    }
}
```

### 8. GeneticSchedulerConfig

```csharp
public class GeneticSchedulerConfig
{
    // 种群大小（默认50）
    public int PopulationSize { get; set; } = 50;
    
    // 最大代数（默认100）
    public int MaxGenerations { get; set; } = 100;
    
    // 交叉率（默认0.8）
    public double CrossoverRate { get; set; } = 0.8;
    
    // 变异率（默认0.1）
    public double MutationRate { get; set; } = 0.1;
    
    // 精英保留数量（默认2）
    public int EliteCount { get; set; } = 2;
    
    // 选择策略类型
    public SelectionStrategyType SelectionStrategy { get; set; } 
        = SelectionStrategyType.Tournament;
    
    // 交叉策略类型
    public CrossoverStrategyType CrossoverStrategy { get; set; } 
        = CrossoverStrategyType.Uniform;
    
    // 变异策略类型
    public MutationStrategyType MutationStrategy { get; set; } 
        = MutationStrategyType.Swap;
    
    // 锦标赛大小（用于锦标赛选择）
    public int TournamentSize { get; set; } = 5;
    
    // 未分配时段惩罚权重
    public double UnassignedPenaltyWeight { get; set; } = 1000.0;
    
    // 硬约束违反惩罚权重
    public double HardConstraintPenaltyWeight { get; set; } = 10000.0;
    
    // 是否启用详细日志
    public bool EnableDetailedLogging { get; set; } = false;
    
    // 保存/加载配置
    public void SaveToFile(string filePath);
    public static GeneticSchedulerConfig LoadFromFile(string filePath);
    public static GeneticSchedulerConfig GetDefault();
}
```

## 数据模型

### SchedulingMode 枚举

```csharp
public enum SchedulingMode
{
    GreedyOnly,      // 仅贪心算法
    Hybrid           // 混合模式（贪心+遗传）
}
```

### SelectionStrategyType 枚举

```csharp
public enum SelectionStrategyType
{
    RouletteWheel,   // 轮盘赌选择
    Tournament       // 锦标赛选择
}
```

### CrossoverStrategyType 枚举

```csharp
public enum CrossoverStrategyType
{
    Uniform,         // 均匀交叉
    SinglePoint      // 单点交叉
}
```

### MutationStrategyType 枚举

```csharp
public enum MutationStrategyType
{
    Swap             // 交换变异
}
```

### GeneticProgressInfo

```csharp
public class GeneticProgressInfo
{
    public int CurrentGeneration { get; set; }
    public int MaxGenerations { get; set; }
    public double BestFitness { get; set; }
    public double AverageFitness { get; set; }
    public int BestHardConstraintViolations { get; set; }
    public int BestUnassignedSlots { get; set; }
}
```


## 正确性属性

*属性是一个特征或行为，应该在系统的所有有效执行中保持为真——本质上是关于系统应该做什么的形式化陈述。属性作为人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1: 混合模式执行顺序

*对于任何*混合模式的排班请求，HybridScheduler 应该先执行 GreedyScheduler 生成初始解，然后将该解传递给 GeneticScheduler
**验证需求: 1.1, 1.2**

### 属性 2: 初始种群组成

*对于任何*遗传算法执行，初始种群应该包含贪心算法的解和随机生成的个体，且种群大小应该等于配置的种群大小
**验证需求: 1.3, 2.1**

### 属性 3: 最优解返回

*对于任何*遗传算法执行，返回的排班方案应该是最终种群中适应度最高的个体
**验证需求: 1.4**

### 属性 4: 模式选择正确性

*对于任何*仅贪心模式的排班请求，系统应该只执行 GreedyScheduler 而不调用 GeneticScheduler
**验证需求: 1.5**

### 属性 5: 代数终止条件

*对于任何*遗传算法执行，当达到配置的最大代数时，算法应该停止迭代
**验证需求: 2.2**

### 属性 6: 精英保留机制

*对于任何*遗传算法的每一代，配置数量的最优个体应该被保留到下一代
**验证需求: 2.5**

### 属性 7: 适应度计算完整性

*对于任何*个体，适应度评估应该包含硬约束违反计数、软约束得分和未分配时段惩罚
**验证需求: 3.1, 3.3, 3.4**

### 属性 8: 硬约束惩罚单调性

*对于任何*两个个体，如果个体A的硬约束违反数量大于个体B，则个体A的适应度应该低于个体B（假设其他条件相同）
**验证需求: 3.2**

### 属性 9: 适应度比较稳定性

*对于任何*两个适应度相同的个体，多次比较应该返回一致的结果
**验证需求: 3.5**

### 属性 10: 交叉操作合法性

*对于任何*交叉操作，生成的后代个体应该包含来自两个父代的基因片段
**验证需求: 4.1, 4.3**

### 属性 11: 交叉后约束验证

*对于任何*交叉操作生成的后代，系统应该验证其是否满足基本约束
**验证需求: 4.4**

### 属性 12: 变异操作可行性

*对于任何*变异操作，调整后的班次分配应该从该时段的可行人员中选择
**验证需求: 5.3**

### 属性 13: 变异后约束验证

*对于任何*变异操作，系统应该验证变异后的个体是否仍满足硬约束
**验证需求: 5.4**

### 属性 14: 进度报告完整性

*对于任何*遗传算法执行，应该报告初始化、每代迭代和完成阶段的进度信息
**验证需求: 6.1, 6.2, 6.5**

### 属性 15: 取消响应性

*对于任何*遗传算法执行，当取消令牌被触发时，算法应该在合理时间内停止执行
**验证需求: 6.4**

### 属性 16: 配置持久化往返

*对于任何*遗传算法配置，保存后重新加载应该得到相同的配置值
**验证需求: 7.1, 7.2**

### 属性 17: 配置重置正确性

*对于任何*被修改的配置，执行重置操作后应该恢复到预设的默认值
**验证需求: 7.4**

### 属性 18: 未分配时段识别

*对于任何*包含未分配时段的排班方案，系统应该正确识别所有未分配的时段
**验证需求: 8.1**

### 属性 19: 未分配时段惩罚

*对于任何*两个个体，如果个体A的未分配时段数量大于个体B，则个体A的适应度应该低于个体B（假设其他条件相同）
**验证需求: 8.3**

### 属性 20: 变异优先填补未分配

*对于任何*包含未分配时段的个体，变异操作应该优先尝试填补这些时段
**验证需求: 8.4**

### 属性 21: 策略配置应用

*对于任何*策略类型配置（选择、交叉、变异），遗传算法应该使用配置指定的策略实现
**验证需求: 9.2, 9.3, 9.4**

### 属性 22: 权重配置影响

*对于任何*两个不同的权重配置，相同个体的适应度计算结果应该不同（除非权重差异不影响该个体）
**验证需求: 9.5**


## 错误处理

### 1. 配置验证错误

**场景**: 用户提供的配置参数无效（如种群大小为负数）

**处理策略**:
- 在配置加载时进行验证
- 对无效参数使用默认值并记录警告
- 向用户显示友好的错误提示

### 2. 配置文件损坏

**场景**: 配置文件格式错误或内容损坏

**处理策略**:
- 捕获反序列化异常
- 使用默认配置
- 记录警告日志
- 可选：备份损坏的配置文件

### 3. 初始解无效

**场景**: 贪心算法返回的初始解包含大量未分配时段或约束违反

**处理策略**:
- 在种群初始化时尝试修复
- 如果无法修复，仍将其加入种群但标记为低质量
- 依赖遗传算法的优化能力改进解

### 4. 交叉/变异失败

**场景**: 交叉或变异操作无法生成有效的后代

**处理策略**:
- 重试指定次数（如3次）
- 如果仍失败，使用父代个体替代
- 记录失败次数用于调试

### 5. 适应度计算异常

**场景**: 适应度评估过程中发生异常

**处理策略**:
- 捕获异常并记录详细信息
- 为该个体分配极低的适应度值
- 继续执行算法

### 6. 取消操作

**场景**: 用户在执行过程中取消操作

**处理策略**:
- 定期检查取消令牌
- 优雅地停止执行
- 返回当前最优解（如果有）
- 清理资源

### 7. 内存不足

**场景**: 种群过大导致内存不足

**处理策略**:
- 在配置验证时检查种群大小的合理性
- 提供内存使用估算
- 建议用户减小种群大小或最大代数

## 测试策略

### 单元测试

#### 核心组件测试

1. **Individual 类测试**
   - 测试从 Schedule 创建个体
   - 测试转换回 Schedule
   - 测试深拷贝功能
   - 测试未分配时段识别

2. **Population 类测试**
   - 测试种群初始化
   - 测试统计信息更新
   - 测试精英个体获取

3. **FitnessEvaluator 测试**
   - 测试硬约束违反计数
   - 测试软约束得分计算
   - 测试未分配时段惩罚
   - 测试适应度综合计算

4. **策略实现测试**
   - 测试轮盘赌选择的概率分布
   - 测试锦标赛选择的正确性
   - 测试均匀交叉的基因组合
   - 测试单点交叉的交叉点选择
   - 测试交换变异的可行性

5. **配置管理测试**
   - 测试配置保存和加载
   - 测试默认配置
   - 测试配置验证
   - 测试配置重置

#### 集成测试

1. **HybridScheduler 集成测试**
   - 测试贪心+遗传的完整流程
   - 测试模式切换
   - 测试进度报告
   - 测试取消功能

2. **GeneticScheduler 集成测试**
   - 测试完整的遗传算法流程
   - 测试不同配置参数的影响
   - 测试策略切换
   - 测试边界情况（空初始解、全未分配等）

### 属性测试

使用 C# 的属性测试库（如 FsCheck 或 CsCheck）进行以下测试：

1. **属性 1-22 的实现**
   - 为每个正确性属性编写对应的属性测试
   - 使用随机生成的排班上下文和配置
   - 验证属性在所有情况下都成立

2. **生成器设计**
   - 实现 SchedulingContext 生成器
   - 实现 Individual 生成器
   - 实现 GeneticSchedulerConfig 生成器
   - 确保生成的数据覆盖边界情况

3. **测试配置**
   - 每个属性测试至少运行 100 次
   - 使用固定种子以便重现失败
   - 记录失败的反例

### 性能测试

1. **执行时间测试**
   - 测试不同规模问题的执行时间
   - 测试不同配置参数对性能的影响
   - 建立性能基准

2. **内存使用测试**
   - 测试不同种群大小的内存占用
   - 检测内存泄漏
   - 验证内存使用在可接受范围内

3. **优化质量测试**
   - 比较贪心算法和混合算法的解质量
   - 测试遗传算法的收敛速度
   - 验证优化效果

### 用户验收测试

1. **功能完整性**
   - 验证所有需求的验收标准
   - 测试用户界面的配置功能
   - 测试进度显示和取消功能

2. **易用性测试**
   - 验证默认配置的合理性
   - 测试配置保存和加载的用户体验
   - 收集用户反馈

## 实现注意事项

### 1. 性能优化

#### 1.1 并行化策略

**适应度评估并行化**（最重要的优化）:
```csharp
// 使用 Parallel.ForEach 并行评估种群中所有个体的适应度
Parallel.ForEach(population.Individuals, individual =>
{
    _fitnessEvaluator.EvaluateFitness(individual);
});
```

**优势**:
- 适应度评估是计算密集型操作，占总执行时间的 60-70%
- 各个体的适应度评估相互独立，非常适合并行化
- 在多核CPU上可获得接近线性的加速比

**注意事项**:
- 确保 FitnessEvaluator 是线程安全的
- 使用 ThreadLocal<Random> 避免随机数生成器竞争
- 控制并行度以避免过度线程切换

#### 1.2 适应度缓存机制

**实现策略**:
```csharp
public class Individual
{
    private double? _cachedFitness;
    private bool _isDirty = true;
    
    public double Fitness
    {
        get
        {
            if (_isDirty || !_cachedFitness.HasValue)
            {
                _cachedFitness = CalculateFitness();
                _isDirty = false;
            }
            return _cachedFitness.Value;
        }
    }
    
    // 当基因改变时标记为脏
    public void MarkDirty() => _isDirty = true;
}
```

**优势**:
- 避免重复计算相同个体的适应度
- 精英个体在多代中保持不变，可以复用缓存
- 减少 30-40% 的适应度计算次数

#### 1.3 早期终止条件

**实现策略**:
```csharp
private bool ShouldTerminateEarly(List<double> bestFitnessHistory)
{
    // 如果连续 N 代最优适应度没有改进，提前终止
    const int NoImprovementGenerations = 20;
    const double ImprovementThreshold = 0.001; // 0.1% 改进阈值
    
    if (bestFitnessHistory.Count < NoImprovementGenerations)
        return false;
    
    var recentBest = bestFitnessHistory.TakeLast(NoImprovementGenerations);
    var maxRecent = recentBest.Max();
    var minRecent = recentBest.Min();
    
    // 如果改进幅度小于阈值，认为已收敛
    return (maxRecent - minRecent) / Math.Abs(minRecent) < ImprovementThreshold;
}
```

**优势**:
- 避免无意义的迭代
- 在已收敛时节省 30-50% 的执行时间
- 用户可配置终止条件的敏感度

#### 1.4 增量适应度更新

**实现策略**:
```csharp
public class IncrementalFitnessEvaluator
{
    // 仅重新计算变异影响的部分
    public void UpdateFitnessAfterMutation(
        Individual individual,
        DateTime mutatedDate,
        int mutatedPeriod,
        int mutatedPosition)
    {
        // 只重新计算受影响人员的软约束得分
        // 而不是重新计算整个个体的适应度
        var affectedPersonIds = GetAffectedPersons(
            individual, mutatedDate, mutatedPeriod, mutatedPosition);
        
        foreach (var personId in affectedPersonIds)
        {
            UpdatePersonScore(individual, personId);
        }
    }
}
```

**优势**:
- 变异通常只影响少数班次
- 避免重新计算整个排班方案的适应度
- 可减少 50-60% 的变异后适应度计算时间

#### 1.5 内存优化

**策略1: 对象池**
```csharp
public class IndividualPool
{
    private readonly ConcurrentBag<Individual> _pool = new();
    
    public Individual Rent()
    {
        return _pool.TryTake(out var individual) 
            ? individual 
            : new Individual();
    }
    
    public void Return(Individual individual)
    {
        individual.Reset();
        _pool.Add(individual);
    }
}
```

**策略2: 基因编码优化**
```csharp
// 使用紧凑的数组而不是 Dictionary
public class Individual
{
    // 一维数组: [day * 12 * positionCount + period * positionCount + position]
    private int[] _genes;
    
    // 比 Dictionary<DateTime, int[,]> 节省 40-50% 内存
}
```

**优势**:
- 减少 GC 压力
- 提高缓存命中率
- 支持更大的种群规模

#### 1.6 智能种群初始化

**实现策略**:
```csharp
private void InitializePopulationSmart(Schedule greedySolution)
{
    // 1. 添加贪心解
    population.Add(Individual.FromSchedule(greedySolution));
    
    // 2. 生成贪心解的变体（局部扰动）
    for (int i = 0; i < populationSize * 0.3; i++)
    {
        var variant = CreateVariant(greedySolution, perturbationLevel: 0.1);
        population.Add(variant);
    }
    
    // 3. 生成中等扰动的个体
    for (int i = 0; i < populationSize * 0.3; i++)
    {
        var variant = CreateVariant(greedySolution, perturbationLevel: 0.3);
        population.Add(variant);
    }
    
    // 4. 生成完全随机的个体（保持多样性）
    for (int i = 0; i < populationSize * 0.4; i++)
    {
        var random = CreateRandomIndividual();
        population.Add(random);
    }
}
```

**优势**:
- 初始种群质量更高，收敛更快
- 平衡了局部搜索和全局探索
- 可减少 20-30% 的迭代次数

#### 1.7 自适应参数调整

**实现策略**:
```csharp
public class AdaptiveGeneticScheduler
{
    private void AdaptParameters(int generation, double diversityIndex)
    {
        // 种群多样性低时，增加变异率
        if (diversityIndex < 0.3)
        {
            _currentMutationRate = Math.Min(0.3, _baseMutationRate * 1.5);
        }
        else
        {
            _currentMutationRate = _baseMutationRate;
        }
        
        // 后期降低交叉率，增加精英保留
        if (generation > _maxGenerations * 0.7)
        {
            _currentCrossoverRate = _baseCrossoverRate * 0.8;
            _currentEliteCount = Math.Min(10, _baseEliteCount * 2);
        }
    }
    
    private double CalculateDiversityIndex(Population population)
    {
        // 计算种群中个体的平均汉明距离
        // 距离越大，多样性越高
    }
}
```

**优势**:
- 避免早熟收敛
- 后期加强局部搜索
- 提高解的质量

#### 1.8 分层评估策略

**实现策略**:
```csharp
public class TieredFitnessEvaluator
{
    public void EvaluateFitness(Individual individual)
    {
        // 第一层：快速检查硬约束违反
        int hardViolations = QuickCheckHardConstraints(individual);
        if (hardViolations > threshold)
        {
            // 严重违反约束，直接给低分，不计算软约束
            individual.Fitness = -hardViolations * 10000;
            return;
        }
        
        // 第二层：详细计算软约束得分
        double softScore = CalculateSoftConstraintScore(individual);
        individual.Fitness = softScore - hardViolations * 10000;
    }
}
```

**优势**:
- 对低质量个体快速评估
- 将计算资源集中在有潜力的个体上
- 可减少 20-30% 的评估时间

#### 1.9 性能监控和分析

**实现策略**:
```csharp
public class PerformanceMonitor
{
    public Dictionary<string, TimeSpan> PhaseTimings { get; } = new();
    
    public void RecordPhase(string phaseName, Action action)
    {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        
        if (!PhaseTimings.ContainsKey(phaseName))
            PhaseTimings[phaseName] = TimeSpan.Zero;
        
        PhaseTimings[phaseName] += sw.Elapsed;
    }
    
    public void PrintReport()
    {
        foreach (var (phase, time) in PhaseTimings.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"{phase}: {time.TotalSeconds:F2}s");
        }
    }
}
```

**优势**:
- 识别性能瓶颈
- 指导优化方向
- 验证优化效果

### 性能优化优先级

根据实际影响，建议按以下顺序实现优化：

1. **高优先级**（任务22中实现）:
   - 适应度评估并行化（预期提升 2-4倍）
   - 适应度缓存（预期提升 30-40%）
   - 早期终止条件（预期提升 30-50%）

2. **中优先级**（可选）:
   - 智能种群初始化（预期减少 20-30% 迭代）
   - 增量适应度更新（预期提升 50-60% 变异性能）
   - 分层评估策略（预期提升 20-30%）

3. **低优先级**（未来扩展）:
   - 内存优化（主要用于超大规模问题）
   - 自适应参数调整（提高解质量，但增加复杂度）
   - 性能监控（开发和调试阶段有用）

### 预期性能提升

基于以上优化策略，预期性能提升：

- **基础实现**: 种群50，代数100，约需 30-60秒
- **并行化后**: 约需 10-20秒（2-3倍提升）
- **加上缓存和早期终止**: 约需 5-10秒（6-12倍总提升）
- **全部优化**: 约需 3-8秒（10-20倍总提升）

实际性能取决于：
- CPU核心数（并行化效果）
- 问题规模（人员数、哨位数、天数）
- 约束复杂度
- 初始解质量

### 2. 随机数生成

- 使用线程安全的随机数生成器
- 支持设置随机种子以便重现结果
- 在并行执行时为每个线程使用独立的随机数生成器

### 3. 约束处理

- 复用现有的 ConstraintValidator 和 SoftConstraintCalculator
- 在交叉和变异后进行约束检查
- 提供修复机制处理约束违反

### 4. 进度报告

- 使用节流机制避免过于频繁的进度更新
- 提供详细的统计信息（最优适应度、平均适应度、违反约束数等）
- 支持取消操作的及时响应

### 5. 可扩展性

- 使用策略模式实现选择、交叉和变异
- 提供清晰的接口定义
- 支持通过配置切换策略
- 便于未来添加新的策略实现

### 6. 调试支持

- 提供详细的日志记录（可配置）
- 记录每代的最优个体和统计信息
- 支持导出中间结果用于分析

### 7. 与现有系统集成

- 保持与 SchedulingService 的接口兼容
- 复用现有的数据模型和约束验证逻辑
- 确保配置持久化与现有配置系统一致

## 部署和配置

### 配置文件位置

```
%LocalAppData%\Packages\<PackageId>\LocalState\GeneticSchedulerConfig.json
```

### 默认配置

```json
{
  "PopulationSize": 50,
  "MaxGenerations": 100,
  "CrossoverRate": 0.8,
  "MutationRate": 0.1,
  "EliteCount": 2,
  "SelectionStrategy": "Tournament",
  "CrossoverStrategy": "Uniform",
  "MutationStrategy": "Swap",
  "TournamentSize": 5,
  "UnassignedPenaltyWeight": 1000.0,
  "HardConstraintPenaltyWeight": 10000.0,
  "EnableDetailedLogging": false
}
```

### UI 配置界面

在排班向导中添加"算法配置"步骤：

```
┌─────────────────────────────────────────────────────────┐
│  算法配置                                                │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  排班模式:  ○ 仅贪心算法 (快速)                         │
│            ● 混合模式 (推荐，质量更优)                   │
│                                                          │
│  ┌─ 遗传算法参数 ────────────────────────────┐          │
│  │                                            │          │
│  │  种群大小:     [50    ] (10-200)          │          │
│  │  最大代数:     [100   ] (10-500)          │          │
│  │  交叉率:       [0.8   ] (0.0-1.0)         │          │
│  │  变异率:       [0.1   ] (0.0-1.0)         │          │
│  │  精英保留:     [2     ] (0-10)            │          │
│  │                                            │          │
│  │  [恢复默认值]                              │          │
│  │                                            │          │
│  └────────────────────────────────────────────┘          │
│                                                          │
│  提示: 增加种群大小和代数可以获得更优解，但会增加      │
│       执行时间。低性能设备建议使用仅贪心模式。          │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

## 模板和草稿功能集成

### 1. 模板功能更新

**目标**: 使排班模板能够保存和复用遗传算法配置

#### 1.1 模板数据模型扩展

```csharp
public class SchedulingTemplate
{
    // 现有字段...
    
    // 新增：算法模式
    public SchedulingMode AlgorithmMode { get; set; } = SchedulingMode.GreedyOnly;
    
    // 新增：遗传算法配置（JSON格式）
    public string GeneticAlgorithmConfig { get; set; } = string.Empty;
}
```

#### 1.2 模板配置序列化

```csharp
public class TemplateGeneticConfig
{
    public int PopulationSize { get; set; } = 50;
    public int MaxGenerations { get; set; } = 100;
    public double CrossoverRate { get; set; } = 0.8;
    public double MutationRate { get; set; } = 0.1;
    public int EliteCount { get; set; } = 2;
    public SelectionStrategyType SelectionStrategy { get; set; }
    public CrossoverStrategyType CrossoverStrategy { get; set; }
    public MutationStrategyType MutationStrategy { get; set; }
}
```

#### 1.3 使用模板创建排班

```csharp
public async Task<ScheduleDto> UseTemplateAsync(UseTemplateDto dto)
{
    var template = await GetByIdAsync(dto.TemplateId);
    
    // 加载算法配置
    var geneticConfig = string.IsNullOrEmpty(template.GeneticAlgorithmConfig)
        ? GeneticSchedulerConfig.GetDefault()
        : JsonSerializer.Deserialize<GeneticSchedulerConfig>(template.GeneticAlgorithmConfig);
    
    // 创建排班请求
    var request = new SchedulingRequestDto
    {
        Mode = template.AlgorithmMode,
        GeneticConfig = geneticConfig,
        // ... 其他配置
    };
    
    return await _schedulingService.ExecuteSchedulingAsync(request);
}
```

### 2. 草稿进度保存功能

**目标**: 允许用户在排班执行过程中保存进度，并在需要时恢复

#### 2.1 草稿数据模型扩展

```csharp
public class Schedule
{
    // 现有字段...
    
    // 新增：进度相关字段
    public double ProgressPercentage { get; set; } = 0;
    public string CurrentStage { get; set; } = string.Empty;
    public bool IsPartialResult { get; set; } = false;
    public bool IsResumable { get; set; } = false;
    
    // 新增：算法状态（JSON格式）
    public string AlgorithmState { get; set; } = string.Empty;
}
```

#### 2.2 算法状态序列化

```csharp
public class GeneticAlgorithmState
{
    public int CurrentGeneration { get; set; }
    public List<Individual> CurrentPopulation { get; set; }
    public Individual BestIndividual { get; set; }
    public List<double> FitnessHistory { get; set; }
    public DateTime SavedAt { get; set; }
}

public class GreedyAlgorithmState
{
    public int CompletedDays { get; set; }
    public DateTime CurrentDate { get; set; }
    public Dictionary<DateTime, int[,]> CompletedAssignments { get; set; }
}
```

#### 2.3 保存进度为草稿

```csharp
public async Task SaveProgressAsDraftAsync(
    Schedule partialSchedule,
    object algorithmState,
    CancellationToken cancellationToken)
{
    // 序列化算法状态
    partialSchedule.AlgorithmState = JsonSerializer.Serialize(algorithmState);
    partialSchedule.IsPartialResult = true;
    partialSchedule.IsResumable = true;
    
    // 保存到数据库
    await _scheduleRepository.SaveDraftAsync(partialSchedule);
}
```

#### 2.4 从草稿恢复排班

```csharp
public async Task<Schedule> ResumeFromDraftAsync(
    int draftId,
    IProgress<SchedulingProgressReport>? progress,
    CancellationToken cancellationToken)
{
    var draft = await GetScheduleByIdAsync(draftId);
    
    if (!draft.IsResumable)
        throw new InvalidOperationException("此草稿无法恢复");
    
    // 反序列化算法状态
    if (draft.Mode == SchedulingMode.Hybrid)
    {
        var state = JsonSerializer.Deserialize<GeneticAlgorithmState>(
            draft.AlgorithmState);
        
        // 恢复遗传算法执行
        return await _geneticScheduler.ResumeFromStateAsync(
            state, progress, cancellationToken);
    }
    else
    {
        var state = JsonSerializer.Deserialize<GreedyAlgorithmState>(
            draft.AlgorithmState);
        
        // 恢复贪心算法执行
        return await _greedyScheduler.ResumeFromStateAsync(
            state, progress, cancellationToken);
    }
}
```

#### 2.5 自动保存机制

```csharp
public class AutoSaveManager
{
    private readonly TimeSpan _autoSaveInterval = TimeSpan.FromMinutes(2);
    private DateTime _lastAutoSave = DateTime.MinValue;
    
    public async Task CheckAndAutoSave(
        Schedule currentSchedule,
        object algorithmState,
        double progressPercentage)
    {
        var now = DateTime.UtcNow;
        
        // 每2分钟或每完成10%自动保存
        if ((now - _lastAutoSave) >= _autoSaveInterval || 
            progressPercentage % 10 < 0.1)
        {
            await SaveProgressAsDraftAsync(currentSchedule, algorithmState);
            _lastAutoSave = now;
        }
    }
}
```

### 3. UI 更新

#### 3.1 模板编辑界面

```
┌─────────────────────────────────────────────────────────┐
│  编辑模板                                                │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  模板名称: [___________________________]                │
│  描述:     [___________________________]                │
│                                                          │
│  ┌─ 算法配置 ────────────────────────────┐              │
│  │                                        │              │
│  │  排班模式:  ○ 仅贪心算法              │              │
│  │            ● 混合模式（贪心+遗传）     │              │
│  │                                        │              │
│  │  [遗传算法参数配置...]                 │              │
│  │                                        │              │
│  └────────────────────────────────────────┘              │
│                                                          │
│  [保存模板]  [取消]                                      │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

#### 3.2 排班进度界面（带保存草稿）

```
┌─────────────────────────────────────────────────────────┐
│  排班进度                                                │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  当前阶段: 遗传算法优化 (第 45/100 代)                  │
│                                                          │
│  [████████████████░░░░░░░░░░] 65%                       │
│                                                          │
│  最优适应度: 8542.3                                      │
│  平均适应度: 7231.8                                      │
│                                                          │
│  ⓘ 自动保存: 2分钟前                                    │
│                                                          │
│  [保存草稿]  [取消排班]                                  │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

#### 3.3 草稿列表界面

```
┌─────────────────────────────────────────────────────────┐
│  草稿列表                                                │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌────────────────────────────────────────────┐         │
│  │ 2024-01-15 排班草稿                        │         │
│  │ 模式: 混合模式 | 进度: 65% | 可恢复       │         │
│  │ [████████████████░░░░░░░░░░]              │         │
│  │ [继续排班]  [查看]  [删除]                │         │
│  └────────────────────────────────────────────┘         │
│                                                          │
│  ┌────────────────────────────────────────────┐         │
│  │ 2024-01-14 排班草稿                        │         │
│  │ 模式: 仅贪心 | 进度: 100% | 已完成        │         │
│  │ [████████████████████████████]            │         │
│  │ [查看]  [确认]  [删除]                    │         │
│  └────────────────────────────────────────────┘         │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### 4. 配置验证

```csharp
public class GeneticConfigValidator
{
    public ValidationResult Validate(GeneticSchedulerConfig config)
    {
        var errors = new List<string>();
        
        if (config.PopulationSize < 10 || config.PopulationSize > 200)
            errors.Add("种群大小必须在 10-200 之间");
        
        if (config.MaxGenerations < 10 || config.MaxGenerations > 500)
            errors.Add("最大代数必须在 10-500 之间");
        
        if (config.CrossoverRate < 0 || config.CrossoverRate > 1)
            errors.Add("交叉率必须在 0.0-1.0 之间");
        
        if (config.MutationRate < 0 || config.MutationRate > 1)
            errors.Add("变异率必须在 0.0-1.0 之间");
        
        if (config.EliteCount < 0 || config.EliteCount > 10)
            errors.Add("精英保留数量必须在 0-10 之间");
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
```

### 5. 模板和草稿功能的性能优化

#### 5.1 模板配置缓存

**问题**: 频繁从数据库加载模板配置影响性能

**优化策略**:
```csharp
public class TemplateConfigCache
{
    private readonly ConcurrentDictionary<int, SchedulingTemplate> _cache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);
    
    public async Task<SchedulingTemplate> GetTemplateAsync(int templateId)
    {
        if (_cache.TryGetValue(templateId, out var cached))
        {
            return cached;
        }
        
        var template = await _repository.GetByIdAsync(templateId);
        _cache.TryAdd(templateId, template);
        
        // 设置过期清理
        _ = Task.Delay(_cacheExpiration).ContinueWith(_ => 
            _cache.TryRemove(templateId, out _));
        
        return template;
    }
}
```

**优势**:
- 减少数据库查询
- 提高模板使用响应速度
- 预期提升：50-70% 模板加载速度

#### 5.2 草稿状态增量保存

**问题**: 保存完整的算法状态（包括整个种群）占用大量存储和时间

**优化策略**:
```csharp
public class IncrementalDraftSaver
{
    // 只保存必要的状态信息
    public async Task SaveMinimalStateAsync(Schedule draft)
    {
        var minimalState = new MinimalGeneticState
        {
            CurrentGeneration = _currentGeneration,
            BestIndividualGenes = _population.BestIndividual.Genes, // 只保存最优个体
            BestFitness = _population.BestIndividual.Fitness,
            RandomSeed = _random.Next(), // 保存随机种子以便重现
            FitnessHistory = _fitnessHistory.TakeLast(10).ToList() // 只保存最近10代
        };
        
        draft.AlgorithmState = JsonSerializer.Serialize(minimalState);
        await _repository.SaveDraftAsync(draft);
    }
}
```

**优势**:
- 减少存储空间占用（从几MB降至几KB）
- 加快保存速度
- 预期提升：80-90% 保存速度

#### 5.3 草稿恢复优化

**问题**: 从草稿恢复时需要重建完整的算法状态

**优化策略**:
```csharp
public class OptimizedDraftResumer
{
    public async Task<Schedule> ResumeOptimizedAsync(int draftId)
    {
        var draft = await GetDraftAsync(draftId);
        var state = JsonSerializer.Deserialize<MinimalGeneticState>(draft.AlgorithmState);
        
        // 策略1: 如果进度 < 30%，重新开始可能更快
        if (draft.ProgressPercentage < 30)
        {
            return await RestartWithBestSolutionAsync(state.BestIndividualGenes);
        }
        
        // 策略2: 如果进度 >= 30%，从最优解重建种群
        var population = new Population();
        population.Add(Individual.FromGenes(state.BestIndividualGenes));
        
        // 使用最优解的变体快速重建种群
        for (int i = 1; i < _config.PopulationSize; i++)
        {
            var variant = CreateVariant(state.BestIndividualGenes, 
                perturbationLevel: 0.1 + (i * 0.01));
            population.Add(variant);
        }
        
        // 从保存的代数继续
        return await ContinueFromGenerationAsync(
            state.CurrentGeneration, population);
    }
}
```

**优势**:
- 智能决策：低进度重启，高进度恢复
- 快速重建种群
- 预期提升：40-60% 恢复速度

#### 5.4 自动保存节流优化

**问题**: 频繁的自动保存影响算法执行性能

**优化策略**:
```csharp
public class ThrottledAutoSaver
{
    private readonly TimeSpan _minSaveInterval = TimeSpan.FromMinutes(2);
    private readonly double _minProgressDelta = 5.0; // 至少进度变化5%才保存
    private DateTime _lastSave = DateTime.MinValue;
    private double _lastSavedProgress = 0;
    
    public async Task<bool> TryAutoSaveAsync(
        Schedule draft, 
        double currentProgress)
    {
        var now = DateTime.UtcNow;
        var timeSinceLastSave = now - _lastSave;
        var progressDelta = currentProgress - _lastSavedProgress;
        
        // 条件1: 时间间隔足够 AND 进度有显著变化
        // 条件2: 或者达到关键进度点（25%, 50%, 75%）
        var shouldSave = 
            (timeSinceLastSave >= _minSaveInterval && progressDelta >= _minProgressDelta) ||
            IsKeyProgressPoint(currentProgress, _lastSavedProgress);
        
        if (!shouldSave)
            return false;
        
        // 异步保存，不阻塞主线程
        _ = Task.Run(async () =>
        {
            await SaveMinimalStateAsync(draft);
            _lastSave = now;
            _lastSavedProgress = currentProgress;
        });
        
        return true;
    }
    
    private bool IsKeyProgressPoint(double current, double last)
    {
        var keyPoints = new[] { 25.0, 50.0, 75.0 };
        return keyPoints.Any(p => last < p && current >= p);
    }
}
```

**优势**:
- 减少不必要的保存操作
- 异步保存不阻塞算法执行
- 在关键进度点确保保存
- 预期影响：< 2% 性能开销

#### 5.5 模板配置序列化优化

**问题**: JSON 序列化/反序列化影响性能

**优化策略**:
```csharp
public class OptimizedConfigSerializer
{
    // 使用 System.Text.Json 的高性能选项
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false, // 不格式化，减小体积
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() } // 枚举转字符串
    };
    
    // 使用源生成器进一步提升性能（.NET 6+）
    [JsonSerializable(typeof(GeneticSchedulerConfig))]
    [JsonSerializable(typeof(TemplateGeneticConfig))]
    internal partial class ConfigJsonContext : JsonSerializerContext { }
    
    public string Serialize<T>(T config)
    {
        return JsonSerializer.Serialize(config, _options);
    }
    
    public T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _options);
    }
}
```

**优势**:
- 使用高性能序列化选项
- 减小JSON体积
- 预期提升：30-40% 序列化速度

#### 5.6 草稿列表分页加载

**问题**: 加载大量草稿影响UI响应速度

**优化策略**:
```csharp
public class PaginatedDraftLoader
{
    private const int PageSize = 20;
    
    public async Task<PagedResult<ScheduleSummaryDto>> GetDraftsPagedAsync(
        int pageIndex = 0,
        DraftFilter? filter = null)
    {
        // 只加载当前页的草稿
        var query = _repository.GetDraftsQuery()
            .OrderByDescending(d => d.UpdatedAt)
            .Skip(pageIndex * PageSize)
            .Take(PageSize);
        
        // 应用过滤器
        if (filter?.ShowOnlyResumable == true)
            query = query.Where(d => d.IsResumable);
        
        if (filter?.Mode.HasValue == true)
            query = query.Where(d => d.Mode == filter.Mode.Value);
        
        var drafts = await query.ToListAsync();
        var total = await _repository.GetDraftsCountAsync(filter);
        
        return new PagedResult<ScheduleSummaryDto>
        {
            Items = drafts.Select(MapToDto).ToList(),
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = PageSize
        };
    }
}
```

**优势**:
- 按需加载，减少内存占用
- 提高UI响应速度
- 支持过滤，减少不必要的数据传输
- 预期提升：70-90% 列表加载速度（当草稿数量 > 50）

#### 5.7 配置验证缓存

**问题**: 重复验证相同的配置浪费CPU

**优化策略**:
```csharp
public class CachedConfigValidator
{
    private readonly ConcurrentDictionary<string, ValidationResult> _validationCache = new();
    
    public ValidationResult Validate(GeneticSchedulerConfig config)
    {
        // 使用配置的哈希作为缓存键
        var configHash = ComputeConfigHash(config);
        
        if (_validationCache.TryGetValue(configHash, out var cached))
            return cached;
        
        var result = PerformValidation(config);
        _validationCache.TryAdd(configHash, result);
        
        return result;
    }
    
    private string ComputeConfigHash(GeneticSchedulerConfig config)
    {
        var json = JsonSerializer.Serialize(config);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hash);
    }
}
```

**优势**:
- 避免重复验证
- 特别适用于使用模板时
- 预期提升：90%+ 验证速度（缓存命中时）

### 6. 综合性能优化总结

#### 6.1 各功能模块的性能优化覆盖

| 功能模块 | 优化策略 | 预期提升 | 任务关联 |
|---------|---------|---------|---------|
| **遗传算法核心** | 并行化、缓存、早期终止 | 6-12倍 | 任务1-11, 22 |
| **模板加载** | 配置缓存 | 50-70% | 任务24 |
| **模板序列化** | 优化序列化选项 | 30-40% | 任务24 |
| **草稿保存** | 增量保存、节流 | 80-90% | 任务25 |
| **草稿恢复** | 智能恢复策略 | 40-60% | 任务26 |
| **草稿列表** | 分页加载 | 70-90% | 任务27 |
| **配置验证** | 验证缓存 | 90%+ | 任务28 |

#### 6.2 整体性能目标

**基准场景**: 50人员 × 10哨位 × 7天，种群50，代数100

| 操作 | 基础实现 | 优化后 | 提升倍数 |
|-----|---------|--------|---------|
| 遗传算法执行 | 30-60秒 | 3-8秒 | 10-20x |
| 模板加载使用 | 2-3秒 | 0.5-1秒 | 3-4x |
| 草稿保存 | 1-2秒 | 0.1-0.2秒 | 8-10x |
| 草稿恢复 | 5-10秒 | 2-4秒 | 2-3x |
| 草稿列表加载 | 3-5秒 | 0.3-0.5秒 | 8-10x |

#### 6.3 性能优化实施优先级

**任务22（高优先级）** - 遗传算法核心优化:
1. ✅ 适应度评估并行化
2. ✅ 适应度缓存
3. ✅ 早期终止条件
4. ✅ 智能种群初始化
5. ✅ 性能监控

**任务24-28（中优先级）** - 模板草稿优化:
1. ✅ 模板配置缓存（任务24）
2. ✅ 草稿增量保存（任务25）
3. ✅ 自动保存节流（任务25）
4. ✅ 智能恢复策略（任务26）
5. ✅ 草稿列表分页（任务27）
6. ✅ 配置验证缓存（任务28）

**未来优化（低优先级）**:
1. 内存优化（对象池、紧凑编码）
2. 自适应参数调整
3. 分布式执行

#### 6.4 性能监控指标

```csharp
public class PerformanceMetrics
{
    // 遗传算法指标
    public TimeSpan GeneticAlgorithmDuration { get; set; }
    public TimeSpan FitnessEvaluationDuration { get; set; }
    public TimeSpan SelectionDuration { get; set; }
    public TimeSpan CrossoverDuration { get; set; }
    public TimeSpan MutationDuration { get; set; }
    public int TotalGenerations { get; set; }
    public int EarlyTerminationGeneration { get; set; }
    
    // 模板草稿指标
    public TimeSpan TemplateLoadDuration { get; set; }
    public TimeSpan DraftSaveDuration { get; set; }
    public TimeSpan DraftResumeDuration { get; set; }
    public int AutoSaveCount { get; set; }
    public int CacheHitCount { get; set; }
    public int CacheMissCount { get; set; }
    
    // 计算缓存命中率
    public double CacheHitRate => 
        CacheHitCount + CacheMissCount > 0 
            ? (double)CacheHitCount / (CacheHitCount + CacheMissCount) 
            : 0;
}
```

## 性能优化实施路线图

### 阶段1: 核心算法优化（任务22 - 高优先级）

**目标**: 将遗传算法执行时间从 30-60秒 降至 3-8秒

**实施步骤**:
1. 实现适应度评估并行化（预期提升 2-4倍）
2. 实现适应度缓存机制（预期减少 30-40% 计算）
3. 实现早期终止条件（预期节省 30-50% 时间）
4. 实现智能种群初始化（预期减少 20-30% 迭代）
5. 添加性能监控和分析

**验证方法**:
- 使用标准测试场景（50人员 × 10哨位 × 7天）
- 记录优化前后的执行时间
- 确保解的质量不降低

### 阶段2: 模板功能优化（任务24 - 中优先级）

**目标**: 将模板加载时间从 2-3秒 降至 0.5-1秒

**实施步骤**:
1. 实现模板配置缓存（10分钟过期）
2. 优化配置序列化（使用高性能选项）
3. 实现配置验证缓存

**验证方法**:
- 测试重复使用同一模板的场景
- 记录缓存命中率
- 确保配置正确加载

### 阶段3: 草稿功能优化（任务25-27 - 中优先级）

**目标**: 
- 草稿保存: 从 1-2秒 降至 0.1-0.2秒
- 草稿恢复: 从 5-10秒 降至 2-4秒
- 草稿列表: 从 3-5秒 降至 0.3-0.5秒

**实施步骤**:
1. 实现草稿增量保存（只保存必要状态）
2. 实现自动保存节流（避免频繁保存）
3. 实现智能恢复策略（低进度重启）
4. 实现草稿列表分页加载

**验证方法**:
- 测试大量草稿场景（100+ 草稿）
- 测试中断和恢复流程
- 记录保存和加载时间

### 阶段4: 配置验证优化（任务28 - 低优先级）

**目标**: 将验证时间降至几乎为零（缓存命中时）

**实施步骤**:
1. 实现验证结果缓存
2. 使用配置哈希作为缓存键

**验证方法**:
- 测试重复验证相同配置
- 记录缓存命中率

### 性能优化检查清单

在实施每个优化时，确保：

- [ ] 优化不影响功能正确性
- [ ] 添加性能监控代码
- [ ] 记录优化前后的性能数据
- [ ] 在不同规模的问题上测试
- [ ] 考虑内存占用的变化
- [ ] 添加配置开关（可选启用/禁用优化）
- [ ] 更新文档说明优化效果

### 性能测试场景

**小规模**: 20人员 × 5哨位 × 3天
- 基准时间: 5-10秒
- 优化目标: 1-2秒

**中规模**: 50人员 × 10哨位 × 7天
- 基准时间: 30-60秒
- 优化目标: 3-8秒

**大规模**: 100人员 × 20哨位 × 14天
- 基准时间: 5-10分钟
- 优化目标: 30-60秒

## 未来扩展

### 1. 自适应参数

- 根据问题规模自动调整种群大小和代数
- 动态调整交叉率和变异率
- 基于收敛速度调整参数

### 2. 多目标优化

- 支持多个优化目标（如最小化未分配、最大化公平性等）
- 使用 Pareto 前沿选择
- 提供多个候选解供用户选择

### 3. 混合策略

- 结合局部搜索算法（如模拟退火）
- 使用问题特定的启发式规则
- 自适应选择最佳策略

### 4. 分布式执行

- 支持多机并行执行
- 岛屿模型遗传算法
- 提高大规模问题的求解速度

### 5. 机器学习集成

- 使用历史数据训练适应度预测模型
- 学习最优的策略参数
- 智能初始化种群
