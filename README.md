# AutoScheduling3 智能排班系统

## 📋 项目简介

AutoScheduling3 是一个基于 WinUI 3 的现代化智能排班系统，专为哨位人员排班设计。系统采用约束满足和贪心算法，实现了复杂的排班约束处理和优化，提供直观的用户界面和强大的数据管理功能。

### 主要特性

- 🎯 **智能排班算法**：基于可行性张量的贪心算法，支持多种硬约束和软约束优化
- 📊 **完整数据管理**：人员、哨位、技能、约束的全生命周期管理
- 📝 **草稿与模板**：支持排班草稿保存和模板复用
- 📈 **历史记录与对比**：完整的历史记录管理和版本对比功能
- 🎨 **现代化界面**：基于 WinUI 3 的流畅用户体验，支持主题切换和动画效果
- 💾 **数据导入导出**：支持批量数据导入导出，便于数据迁移和备份
- 🔧 **灵活配置**：支持定岗规则、手动指定、休息日配置等多种约束设置

## 🏗️ 技术架构

### 技术栈

- **框架**：.NET 8.0 + WinUI 3 (Windows App SDK 1.8)
- **架构模式**：MVVM (Model-View-ViewModel)
- **依赖注入**：Microsoft.Extensions.DependencyInjection
- **数据库**：SQLite (Microsoft.Data.Sqlite)
- **UI 组件**：CommunityToolkit.WinUI.UI.Controls
- **数学计算**：MathNet.Numerics
- **序列化**：Newtonsoft.Json

### 项目结构

```
AutoScheduling3/
├── Views/                          # 视图层
│   ├── DataManagement/            # 数据管理页面
│   │   ├── PersonnelPage          # 人员管理
│   │   ├── PositionPage           # 哨位管理
│   │   └── SkillPage              # 技能管理
│   ├── Scheduling/                # 排班功能页面
│   │   ├── CreateSchedulingPage   # 创建排班
│   │   ├── TemplatePage           # 模板管理
│   │   ├── DraftsPage             # 草稿管理
│   │   └── ScheduleResultPage     # 排班结果
│   ├── History/                   # 历史记录页面
│   │   ├── HistoryPage            # 历史列表
│   │   └── ComparePage            # 版本对比
│   └── Settings/                  # 设置页面
│       ├── SettingsPage           # 系统设置
│       └── TestDataGeneratorPage  # 测试数据生成
│
├── ViewModels/                    # 视图模型层
│   ├── Base/                      # 基础视图模型
│   ├── DataManagement/            # 数据管理视图模型
│   ├── Scheduling/                # 排班视图模型
│   ├── History/                   # 历史记录视图模型
│   └── Settings/                  # 设置视图模型
│
├── Services/                      # 业务服务层
│   ├── Interfaces/                # 服务接口
│   ├── ImportExport/              # 导入导出服务
│   ├── SchedulingService.cs       # 排班服务
│   ├── PersonnelService.cs        # 人员服务
│   ├── PositionService.cs         # 哨位服务
│   ├── SkillService.cs            # 技能服务
│   ├── ConstraintService.cs       # 约束服务
│   ├── TemplateService.cs         # 模板服务
│   ├── HistoryService.cs          # 历史服务
│   ├── ThemeService.cs            # 主题服务
│   └── ConfigurationService.cs    # 配置服务
│
├── Data/                          # 数据访问层
│   ├── Interfaces/                # 仓储接口
│   ├── Models/                    # 数据模型
│   ├── Enums/                     # 枚举定义
│   ├── Exceptions/                # 自定义异常
│   ├── Logging/                   # 日志记录
│   ├── DatabaseService.cs         # 数据库服务
│   ├── DatabaseSchema.cs          # 数据库架构
│   ├── DatabaseHealthChecker.cs   # 健康检查
│   ├── DatabaseRepairService.cs   # 修复服务
│   ├── PersonalRepository.cs      # 人员仓储
│   ├── PositionLocationRepository.cs # 哨位仓储
│   ├── SkillRepository.cs         # 技能仓储
│   ├── ConstraintRepository.cs    # 约束仓储
│   └── SchedulingRepository.cs    # 排班仓储
│
├── SchedulingEngine/              # 排班算法引擎
│   ├── Core/                      # 核心组件
│   │   ├── FeasibilityTensor.cs   # 可行性张量
│   │   ├── SchedulingContext.cs   # 调度上下文
│   │   ├── ConstraintValidator.cs # 约束验证器
│   │   ├── ScoreCalculator.cs     # 评分计算器
│   │   └── IHardConstraint.cs     # 硬约束接口
│   ├── Strategies/                # 调度策略
│   │   └── MRVStrategy.cs         # 最小剩余值策略
│   └── GreedyScheduler.cs         # 贪心调度器
│
├── DTOs/                          # 数据传输对象
│   ├── Mappers/                   # 对象映射器
│   ├── ImportExport/              # 导入导出 DTO
│   └── *.Dto.cs                   # 各类 DTO
│
├── History/                       # 历史管理
│   └── HistoryManagement.cs       # 历史记录管理器
│
├── Helpers/                       # 辅助工具
│   ├── NavigationService.cs      # 导航服务
│   ├── DialogService.cs           # 对话框服务
│   ├── AnimationHelper.cs         # 动画辅助
│   └── ResponsiveHelper.cs        # 响应式布局辅助
│
├── Controls/                      # 自定义控件
│   ├── PersonnelCard.xaml         # 人员卡片
│   └── ScheduleGridControl.xaml   # 排班网格
│
├── Converters/                    # 值转换器
├── Constants/                     # 常量定义
├── Extensions/                    # 扩展方法
└── Themes/                        # 主题资源
    ├── ThemeResources.xaml        # 主题资源
    └── AnimationResources.xaml    # 动画资源
```

## 🚀 快速开始

### 系统要求

- Windows 10 版本 1809 (Build 17763) 或更高版本
- .NET 8.0 Runtime
- 至少 100MB 可用磁盘空间

### 安装与运行

1. **克隆项目**
```bash
git clone <repository-url>
cd AutoScheduling3
```

2. **还原依赖**
```bash
dotnet restore
```

3. **构建项目**
```bash
dotnet build
```

4. **运行应用**
```bash
dotnet run
```

### 首次使用

1. 应用启动后会自动初始化数据库
2. 建议先在"设置 > 测试数据生成器"中生成示例数据
3. 在"数据管理"模块中查看和管理基础数据
4. 在"排班管理"模块中创建新的排班计划

## 📖 核心功能详解

### 1. 数据管理

#### 人员管理
- 添加、编辑、删除人员信息
- 设置人员技能和可用性状态
- 批量导入人员数据
- 人员退休状态管理

#### 哨位管理
- 管理哨位位置和描述信息
- 配置哨位所需技能
- 设置哨位特殊要求

#### 技能管理
- 定义技能体系
- 管理技能层级关系
- 技能与人员、哨位的关联

### 2. 排班算法

#### 硬约束（必须满足）

1. **夜哨唯一约束**：同一人员同一晚上只能上一个夜哨
2. **时段不连续约束**：相邻时段不能连续上哨
3. **人员可用性约束**：仅可用人员参与排班
4. **定岗限制约束**：特定人员只能在指定哨位/时段
5. **技能匹配约束**：人员必须具备哨位所需技能
6. **单人上哨约束**：每个哨位每个时段仅一人
7. **一人一哨约束**：同一人员同一时段仅一个哨位
8. **手动指定约束**：用户预先指定的固定分配

#### 软约束（优化目标）

1. **充分休息优化**：优先分配休息时间长的人员
2. **时段平衡优化**：均衡各时段的分配
3. **休息日平衡优化**：公平分配休息日班次

#### 时段定义

系统将一天分为 12 个时段，每个时段 2 小时：

| 时段 | 时间范围 | 类型 | 说明 |
|------|---------|------|------|
| 0 | 00:00-02:00 | 夜哨 | 凌晨 |
| 1 | 02:00-04:00 | 夜哨 | 深夜 |
| 2 | 04:00-06:00 | 夜哨 | 黎明 |
| 3 | 06:00-08:00 | 日哨 | 早晨 |
| 4 | 08:00-10:00 | 日哨 | 上午 |
| 5 | 10:00-12:00 | 日哨 | 上午 |
| 6 | 12:00-14:00 | 日哨 | 中午 |
| 7 | 14:00-16:00 | 日哨 | 下午 |
| 8 | 16:00-18:00 | 日哨 | 下午 |
| 9 | 18:00-20:00 | 日哨 | 傍晚 |
| 10 | 20:00-22:00 | 日哨 | 晚间 |
| 11 | 22:00-24:00 | 夜哨 | 夜间 |

**夜哨定义**：时段 11、0、1、2（22:00-06:00）

### 3. 约束配置

#### 休息日配置
- 支持周末规则配置
- 法定节假日设置
- 自定义休息日规则

#### 定岗规则
- 限制人员可上岗的哨位
- 限制人员可上岗的时段
- 支持多规则组合

#### 手动指定
- 预先指定特定日期的排班
- 支持批量手动指定
- 手动指定优先级最高

### 4. 模板与草稿

#### 排班模板
- 保存常用排班配置
- 快速创建新排班
- 模板参数化配置

#### 排班草稿
- 自动保存排班草稿
- 草稿预览和编辑
- 草稿确认和发布
- 自动清理过期草稿（7天）

### 5. 历史记录

#### 历史管理
- 完整的排班历史记录
- 历史数据查询和筛选
- 历史数据统计分析

#### 版本对比
- 对比不同版本的排班结果
- 高亮显示差异
- 导出对比报告

### 6. 数据导入导出

#### 导入功能
- 支持 JSON 格式导入
- 批量导入人员、哨位、技能数据
- 冲突检测和解决策略
- 导入进度监控

#### 导出功能
- 导出完整数据或选定数据
- 支持多种导出格式
- 导出进度监控
- 导出数据验证

## 🔧 高级配置

### 数据库配置

数据库文件默认存储在：
```
%LocalAppData%\Packages\<PackageId>\LocalState\GuardDutyScheduling.db
```

可通过 `DatabaseConfiguration` 类自定义数据库路径：

```csharp
// 自定义数据库路径
var customPath = Path.Combine(Environment.GetFolderPath(
    Environment.SpecialFolder.MyDocuments), 
    "AutoScheduling", 
    "custom.db");

var service = new SchedulingService(customPath);
```

### 主题配置

系统支持三种主题模式：
- **浅色主题**：适合白天使用
- **深色主题**：适合夜间使用
- **跟随系统**：自动跟随系统主题设置

在"设置"页面中可以切换主题和启用/禁用动画效果。

### 性能优化

#### 数据库优化
- 自动数据库健康检查
- 数据库修复和优化
- 定期备份机制

#### 内存优化
- 大数据集分页加载
- 虚拟化列表渲染
- 及时释放资源

## 📊 算法原理

### 可行性张量算法

系统采用基于**可行性张量**的贪心算法：

1. **初始化阶段**
   - 构建三维布尔数组 `[哨位][时段][人员]`
   - 初始化所有可行性为 `true`

2. **约束应用阶段**
   - 依次应用所有硬约束
   - 通过位运算快速更新张量
   - 标记不可行的分配方案

3. **贪心选择阶段**
   - 选择约束最紧的哨位-时段组合（MRV策略）
   - 在可行人员中计算软约束评分
   - 选择评分最高的人员进行分配

4. **动态更新阶段**
   - 更新可行性张量
   - 更新人员状态
   - 继续下一轮分配

### 软约束评分公式

```
总分 = 充分休息得分 + 时段平衡得分 + 休息日平衡得分

其中：
- 充分休息得分 = 距离上次排班的时段数
- 时段平衡得分 = 距离上次在该时段排班的时段数
- 休息日平衡得分 = 距离上次休息日排班的天数（仅休息日）
```

评分越高，优先级越高。

### 算法复杂度

- **时间复杂度**：O(P × T × N × log N)
  - P: 哨位数量
  - T: 时段数量
  - N: 人员数量

- **空间复杂度**：O(P × T × N)

## 🧪 测试

### 单元测试

项目使用 xUnit 和 Moq 进行单元测试：

```bash
dotnet test
```

### 测试数据生成

在"设置 > 测试数据生成器"中可以生成测试数据：
- 自定义人员数量
- 自定义哨位数量
- 自动生成技能关联
- 生成示例约束配置

## 📝 开发指南

### 添加新功能

1. **定义服务接口**（Services/Interfaces/）
2. **实现服务类**（Services/）
3. **创建视图模型**（ViewModels/）
4. **设计视图**（Views/）
5. **注册依赖注入**（Extensions/ServiceCollectionExtensions.cs）

### 代码规范

- 遵循 C# 编码规范
- 使用 MVVM 模式
- 保持单一职责原则
- 编写清晰的注释和文档

### 提交规范

```
<type>(<scope>): <subject>

<body>

<footer>
```

类型（type）：
- feat: 新功能
- fix: 修复
- docs: 文档
- style: 格式
- refactor: 重构
- test: 测试
- chore: 构建/工具

## 🐛 故障排除

### 常见问题

#### 1. 数据库初始化失败
- 检查磁盘空间是否充足
- 确认应用有写入权限
- 查看日志文件获取详细错误信息

#### 2. 排班算法无解
- 检查约束配置是否过于严格
- 确认人员数量和技能配置是否合理
- 尝试放宽部分约束条件

#### 3. 界面显示异常
- 尝试切换主题
- 重启应用程序
- 检查 Windows 版本是否符合要求

### 日志查看

应用日志输出到调试控制台，可使用 Visual Studio 或 DebugView 查看。

## 🔮 未来规划

### 短期计划
- [ ] 支持更多导出格式（Excel、PDF）
- [ ] 增强统计分析功能
- [ ] 移动端查看支持
- [ ] 多语言支持

### 长期计划
- [ ] 遗传算法优化
- [ ] 机器学习预测
- [ ] 云端同步功能
- [ ] 多租户支持

## 📄 许可证

本项目仅供学习和内部使用。

## 👥 贡献

欢迎提出问题和建议！

### 贡献方式
1. Fork 本项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

## 📞 联系方式

如有问题或建议，请通过以下方式联系：
- 提交 Issue
- 发送邮件

---

**开发时间**：2024-2025年  
**当前版本**：v3.0  
**框架版本**：.NET 8.0 + WinUI 3  
**最后更新**：2025年11月
