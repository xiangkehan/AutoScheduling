# 术语表 (Glossary)

本文档定义了 AutoScheduling3 项目中使用的核心术语和概念。

## 核心概念

### 人员 (Personnel)
执行哨位任务的个体，具有特定技能和可用性。

- **人员ID** (Personnel ID): 人员的唯一标识符
- **技能** (Skills): 人员具备的能力集合，用于匹配哨位要求
- **可用性** (Availability): 人员在特定时段是否可以被分配

### 哨位 (Position / Position Location)
需要人员值守的岗位或位置。

- **哨位ID** (Position ID): 哨位的唯一标识符
- **哨位名称** (Position Name): 哨位的描述性名称
- **可用人员列表** (Available Personnel List): 该哨位允许分配的人员ID集合
- **技能要求** (Skill Requirements): 哨位要求人员具备的技能

### 技能 (Skill)
人员能力和哨位要求的匹配标准。

- **技能ID** (Skill ID): 技能的唯一标识符
- **技能名称** (Skill Name): 技能的描述性名称
- **技能匹配** (Skill Matching): 验证人员技能是否满足哨位要求的过程

## 时间概念

### 时段 (Time Slot / Period)
一天被划分为12个时段，每个时段2小时。

- **时段索引** (Period Index): 0-11，对应一天中的12个时段
- **时段0**: 00:00-02:00
- **时段1**: 02:00-04:00
- **时段2**: 04:00-06:00
- **时段3**: 06:00-08:00
- **时段4**: 08:00-10:00
- **时段5**: 10:00-12:00
- **时段6**: 12:00-14:00
- **时段7**: 14:00-16:00
- **时段8**: 16:00-18:00
- **时段9**: 18:00-20:00
- **时段10**: 20:00-22:00
- **时段11**: 22:00-00:00

### 夜哨 (Night Shift)
夜间时段的哨位，包括时段11、0、1、2（22:00-06:00）。

### 日哨 (Day Shift)
白天时段的哨位，包括时段3-10（06:00-22:00）。

### 班次 (Shift / Single Shift)
一次具体的人员-哨位-时段分配。

- **开始时间** (Start Time): 班次开始的具体时间
- **结束时间** (End Time): 班次结束的具体时间
- **班次ID** (Shift ID): 班次的唯一标识符

## 排班概念

### 排班表 (Schedule)
完整的排班计划，包含多个班次。

- **排班表ID** (Schedule ID): 排班表的唯一标识符
- **表头** (Header): 排班表的名称或描述
- **开始日期** (Start Date): 排班计划的起始日期
- **结束日期** (End Date): 排班计划的结束日期
- **确认状态** (Confirmed Status): 排班表是否已确认实施

### 草稿 (Draft)
未确认的排班表，可以继续编辑和修改。

### 模板 (Template)
可重复使用的排班配置。

- **模板类型** (Template Type):
  - `regular`: 常规模板
  - `holiday`: 节假日模板
  - `special`: 特殊模板
- **默认模板** (Default Template): 系统默认使用的模板
- **使用次数** (Usage Count): 模板被使用的次数

### 定岗规则 (Fixed Rule)
指定某些人员必须或不能在特定哨位工作的规则。

### 手动指定 (Manual Assignment)
用户手动指定的人员-哨位-时段分配，优先级高于算法自动分配。

- **临时手动指定** (Temporary Manual Assignment): 未保存到数据库的手动指定

### 休息日配置 (Holiday Config)
定义哪些日期为休息日的配置。

- **活动配置** (Active Config): 当前生效的休息日配置

## 约束概念

### 硬约束 (Hard Constraints)
必须满足的约束条件，违反则排班方案不可行。

- **技能匹配约束** (Skill Matching Constraint): 人员必须具备哨位要求的技能
- **可用性约束** (Availability Constraint): 人员在该时段必须可用
- **单人上哨约束** (Single Person Per Slot): 每个哨位每个时段只能分配一个人
- **一人一哨约束** (One Position Per Person Per Period): 每个人每个时段只能在一个哨位

### 软约束 (Soft Constraints)
优化目标，尽量满足但不强制。

- **休息时间约束** (Rest Time Constraint): 连续工作后需要足够的休息时间
- **工作量均衡约束** (Workload Balance Constraint): 人员之间的工作量应尽量均衡
- **连续工作约束** (Consecutive Work Constraint): 限制连续工作的时长

### 约束违反 (Constraint Violation)
排班方案不满足某个约束的情况。

### 冲突 (Conflict)
排班中存在的问题或约束违反。

- **冲突类型** (Conflict Type):
  - 硬约束冲突
  - 软约束冲突
  - 信息提示
- **冲突子类型** (Conflict Sub Type):
  - `SkillMismatch`: 技能不匹配
  - `PersonnelUnavailable`: 人员不可用
  - `DuplicateAssignment`: 重复分配
  - `InsufficientRest`: 休息时间不足
  - `ExcessiveWorkload`: 工作量过大
  - `WorkloadImbalance`: 工作量不均衡
  - `ConsecutiveOvertime`: 连续工作超时
  - `UnassignedSlot`: 未分配时段
  - `SuboptimalAssignment`: 次优分配

## 算法概念

### 贪心算法 (Greedy Algorithm)
基于启发式规则的快速排班算法。

- **MRV策略** (Minimum Remaining Values): 优先处理可选人员最少的哨位时段

### 遗传算法 (Genetic Algorithm)
基于进化计算的优化算法。

- **种群** (Population): 一组候选解（个体）的集合
- **个体** (Individual): 一个完整的排班方案
- **基因** (Gene): 个体中的一个分配决策
- **染色体** (Chromosome): 个体的编码表示
- **适应度** (Fitness): 评估个体质量的分数
- **代** (Generation): 种群的迭代次数

### 混合算法 (Hybrid Algorithm)
结合贪心算法和遗传算法的排班方法。

- **排班模式** (Scheduling Mode):
  - `GreedyOnly`: 仅使用贪心算法
  - `Hybrid`: 使用混合算法（贪心+遗传）

### 遗传算法参数

#### 种群参数
- **种群大小** (Population Size): 种群中个体的数量（默认50）
- **最大代数** (Max Generations): 算法运行的最大迭代次数（默认100）
- **精英保留** (Elite Count): 每代保留的最优个体数量（默认2）

#### 遗传操作
- **选择** (Selection): 从种群中选择个体进行繁殖
  - **轮盘赌选择** (Roulette Wheel Selection): 根据适应度比例选择
  - **锦标赛选择** (Tournament Selection): 随机选择若干个体，返回最优的
  - **锦标赛大小** (Tournament Size): 锦标赛中参与竞争的个体数量（默认5）

- **交叉** (Crossover): 两个父代个体产生子代
  - **交叉率** (Crossover Rate): 发生交叉操作的概率（默认0.8）
  - **均匀交叉** (Uniform Crossover): 对每个基因位随机选择来自父代1或父代2
  - **单点交叉** (Single Point Crossover): 随机选择交叉点，交换交叉点后的基因片段

- **变异** (Mutation): 随机改变个体的某些基因
  - **变异率** (Mutation Rate): 发生变异操作的概率（默认0.1）
  - **交换变异** (Swap Mutation): 交换两个基因位的值

#### 适应度评估
- **适应度函数** (Fitness Function): 评估个体质量的函数
- **适应度评估器** (Fitness Evaluator): 计算个体适应度的组件
- **未分配惩罚权重** (Unassigned Penalty Weight): 未分配时段的惩罚系数（默认1000.0）
- **硬约束惩罚权重** (Hard Constraint Penalty Weight): 硬约束违反的惩罚系数（默认10000.0）

### 可行性张量 (Feasibility Tensor)
三维布尔张量 [哨位, 时段, 人员]，用于快速判断分配方案是否可行。

- **张量维度** (Tensor Dimensions):
  - 第一维: 哨位索引
  - 第二维: 时段索引
  - 第三维: 人员索引
- **二进制存储** (Binary Storage): 使用位运算优化存储和计算
- **逐位与运算** (Bitwise AND): 快速应用约束条件

### 调度上下文 (Scheduling Context)
排班算法运行时的环境和状态信息。

### 约束验证器 (Constraint Validator)
验证排班方案是否满足约束条件的组件。

### 分数计算器 (Score Calculator)
计算排班方案软约束评分的组件。

- **软约束评分** (Soft Constraint Scores): 各项软约束的得分

## 数据管理

### 数据传输对象 (DTO - Data Transfer Object)
用于在不同层之间传递数据的对象。

### 映射器 (Mapper)
在实体模型和DTO之间进行转换的组件。

### 仓储 (Repository)
数据访问层，负责与数据库交互。

### 数据库服务 (Database Service)
管理数据库连接和初始化的服务。

## 用户界面

### 向导 (Wizard)
分步骤引导用户完成排班创建的界面流程。

### 进度页面 (Progress Page)
显示排班算法执行进度的页面。

- **进度百分比** (Progress Percentage): 0-100的完成度
- **当前阶段** (Current Stage): 算法执行到的阶段描述
- **部分结果** (Partial Result): 未完成的排班结果

### 结果页面 (Result Page)
显示排班结果和冲突信息的页面。

### 历史管理 (History Management)
记录和管理排班表的历史版本。

- **版本比较** (Version Comparison): 对比不同版本的排班表

## 导入导出

### 批量导入 (Bulk Import)
从外部文件批量导入数据。

### 批量导出 (Bulk Export)
将数据导出到外部文件。

### Excel导出 (Excel Export)
将排班结果导出为Excel格式。

## 性能优化

### 节流自动保存 (Throttled Auto Save)
限制自动保存频率以提高性能。

### 配置缓存 (Config Cache)
缓存模板配置以减少数据库访问。

### 优化配置序列化器 (Optimized Config Serializer)
高效的配置序列化和反序列化工具。

## 测试相关

### 测试数据生成器 (Test Data Generator)
生成用于测试的模拟数据。

### 验证 (Validation)
检查数据有效性的过程。

- **验证结果** (Validation Result): 包含错误、警告和信息的验证输出
- **验证消息** (Validation Message): 单条验证信息

## 系统概念

### 依赖注入 (Dependency Injection)
管理组件依赖关系的设计模式。

### MVVM (Model-View-ViewModel)
应用程序的架构模式。

- **Model**: 数据模型层
- **View**: 用户界面层
- **ViewModel**: 表示逻辑层

### 服务 (Service)
业务逻辑层的组件。

### 辅助类 (Helper)
提供通用功能的工具类。

- **导航服务** (Navigation Service): 管理页面导航
- **对话框服务** (Dialog Service): 管理对话框显示
- **动画辅助** (Animation Helper): 提供UI动画功能

## 常用缩写

- **DTO**: Data Transfer Object（数据传输对象）
- **MVVM**: Model-View-ViewModel（模型-视图-视图模型）
- **MRV**: Minimum Remaining Values（最小剩余值）
- **GA**: Genetic Algorithm（遗传算法）
- **UI**: User Interface（用户界面）
- **ID**: Identifier（标识符）
- **UTC**: Coordinated Universal Time（协调世界时）
