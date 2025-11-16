# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

AutoScheduling3 是一个基于 **WinUI 3 + .NET 8.0** 的智能排班系统，专为哨位人员排班设计。系统采用 MVVM 架构模式，使用约束满足和贪心算法实现复杂的排班优化。

## 技术栈

- **框架**：.NET 8.0 + WinUI 3 (Windows App SDK 1.8)
- **架构模式**：MVVM
- **依赖注入**：Microsoft.Extensions.DependencyInjection
- **数据库**：SQLite (Microsoft.Data.Sqlite)
- **测试**：xUnit + Moq
- **其他**：CommunityToolkit.Mvvm, MathNet.Numerics, Newtonsoft.Json

## 常用命令

### 构建与运行

```bash
# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行应用
dotnet run

# 构建发布版本
dotnet publish -c Release -r win-x64

# 清理构建输出
dotnet clean
```

### 测试

```bash
# 运行所有测试
dotnet test

# 运行特定测试项目
dotnet test Tests/

# 运行测试并查看详细输出
dotnet test --verbosity normal

# 运行测试并生成代码覆盖率报告
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

**注意**：项目中已配置 xUnit 和 Moq 依赖，但未发现实际的单元测试文件。需要添加测试代码。

## 项目架构

### 核心目录结构

```
AutoScheduling3/
├── Views/              # 视图层 (XAML + Code-behind)
├── ViewModels/         # 视图模型层
├── Services/           # 业务服务层
├── Data/               # 数据访问层 (Repository + Models)
├── SchedulingEngine/   # 排班算法引擎
├── DTOs/               # 数据传输对象
├── Helpers/            # 辅助工具
└── Extensions/         # 扩展方法
```

### 关键架构组件

#### 1. 依赖注入配置
- **位置**：[Extensions/ServiceCollectionExtensions.cs](Extensions/ServiceCollectionExtensions.cs)
- **注册顺序**：Repositories → Mappers → Business Services → Helper Services → ViewModels
- **DatabaseService** 为单例，必须在其他服务之前初始化

#### 2. 应用启动流程
- **位置**：[App.xaml.cs](App.xaml.cs:71)
- **初始化顺序**：
  1. 配置依赖注入服务
  2. 初始化数据库（通过 `InitializeAsync` 方法）
  3. 初始化配置服务
  4. 初始化所有仓储和服务
  5. 清理过期草稿（超过7天）

#### 3. 数据库配置
- **默认路径**：`%LocalAppData%\Packages\<PackageId>\LocalState\GuardDutyScheduling.db`
- **通过 `DatabaseConfiguration.GetDefaultDatabasePath()` 获取路径**
- **初始化选项**：健康检查、自动修复、连接重试等
- **健康检查**：`DatabaseHealthChecker.cs`
- **数据库修复**：`DatabaseRepairService.cs`

#### 4. 排班算法引擎
- **位置**：[SchedulingEngine/](SchedulingEngine/)
- **核心组件**：
  - **FeasibilityTensor.cs**：可行性张量，三维布尔数组 `[哨位][时段][人员]`
  - **SchedulingContext.cs**：调度上下文
  - **ConstraintValidator.cs**：约束验证器
  - **ScoreCalculator.cs**：软约束评分计算器
  - **MRVStrategy.cs**：最小剩余值策略
  - **GreedyScheduler.cs**：贪心调度器

#### 5. 服务层接口
- **位置**：[Services/Interfaces/](Services/Interfaces/)
- **核心服务**：
  - `ISchedulingService`：排班服务
  - `IPersonnelService`：人员服务
  - `IPositionService`：哨位服务
  - `ISkillService`：技能服务
  - `IConstraintService`：约束服务
  - `ITemplateService`：模板服务
  - `IHistoryService`：历史服务
  - `IDataImportExportService`：数据导入导出服务

#### 6. 数据导入导出
- **位置**：[Services/ImportExport/](Services/ImportExport/)
- **核心功能**：
  - `IDataImportExportService`：导入导出主服务
  - `IDataValidationService`：数据验证服务
  - `IDataExportService`：数据导出服务
  - `IDataMappingService`：数据映射服务
- **支持格式**：JSON
- **文档**：[Services/ImportExport/README.md](Services/ImportExport/README.md)、[QUICK_REFERENCE.md](Services/ImportExport/QUICK_REFERENCE.md)

## 数据模型

### 核心实体

1. **Personnel**（人员）- `Data/Models/Personnel.cs`
2. **Position**（哨位）- `Data/Models/PositionLocation.cs`
3. **Skill**（技能）- `Data/Models/Skill.cs`
4. **Constraint**（约束）- `Data/Models/Constraint.cs`
5. **SchedulingRecord**（排班记录）- `Data/Models/SchedulingRecord.cs`
6. **Template**（模板）- `Data/Models/SchedulingTemplate.cs`

### 时段定义

一天分为 12 个时段，每段 2 小时：
- **夜哨**：时段 11、0、1、2（22:00-06:00）
- **日哨**：时段 3-10（06:00-22:00）

## 约束系统

### 硬约束（必须满足）
1. 夜哨唯一约束
2. 时段不连续约束
3. 人员可用性约束
4. 定岗限制约束
5. 技能匹配约束
6. 单人一哨约束
7. 手动指定约束

### 软约束（优化目标）
1. 充分休息优化
2. 时段平衡优化
3. 休息日平衡优化

## 排班算法原理

### 可行性张量算法
1. **初始化**：构建三维布尔数组 `[哨位][时段][人员]`
2. **约束应用**：依次应用硬约束，标记不可行方案
3. **贪心选择**：使用 MRV 策略选择约束最紧的组合
4. **动态更新**：更新张量和人员状态

### 软约束评分公式
```
总分 = 充分休息得分 + 时段平衡得分 + 休息日平衡得分
```

## 开发指南

### 添加新功能的步骤

1. **定义服务接口**（Services/Interfaces/）
2. **实现服务类**（Services/）
3. **创建仓储接口**（Data/Interfaces/）
4. **实现仓储类**（Data/）
5. **创建视图模型**（ViewModels/）
6. **设计视图**（Views/）
7. **注册依赖注入**（Extensions/ServiceCollectionExtensions.cs）

### 代码规范

- 遵循 C# 编码规范
- 使用 MVVM 模式
- 保持单一职责原则
- 所有服务使用异步方法（Async 后缀）
- 数据库操作使用仓储模式
- 业务逻辑在服务层，不在视图模型中

### 提交规范

```
<type>(<scope>): <subject>

<body>

<footer>
```

类型：
- `feat`：新功能
- `fix`：修复
- `docs`：文档
- `style`：格式
- `refactor`：重构
- `test`：测试
- `chore`：构建/工具

## 测试数据生成

**位置**：[TestDataGeneratorPage](Views/Settings/TestDataGeneratorPage.xaml) + [TestDataGeneratorViewModel](ViewModels/Settings/TestDataGeneratorViewModel.cs)

在"设置 > 测试数据生成器"中可以：
- 自定义人员数量
- 自定义哨位数量
- 自动生成技能关联
- 生成示例约束配置

## 导入导出功能

### 数据导入
- 支持 JSON 格式
- 批量导入人员、哨位、技能数据
- 冲突检测和解决
- 导入进度监控

### 数据导出
- 导出完整或选定数据
- 导出进度监控
- 数据验证

### 备份机制
- 自动备份数据库
- 修复前自动创建备份
- 备份位置：`LocalFolder\backups\`

## 主题与UI

- 支持浅色/深色/跟随系统主题
- 位置：[Themes/ThemeResources.xaml](Themes/ThemeResources.xaml)
- 主题服务：`ThemeService.cs`
- 动画资源：[Themes/AnimationResources.xaml](Themes/AnimationResources.xaml)

## 数据库架构

- **数据库文件**：`GuardDutyScheduling.db`
- **健康检查**：`DatabaseHealthChecker.cs`
- **自动修复**：`DatabaseRepairService.cs`
- **连接管理**：`DatabaseService.cs`
- **仓储模式**：每个实体对应一个 Repository

## 历史记录与草稿

### 历史管理
- 完整排班历史记录
- 版本对比功能
- 历史数据统计分析

### 草稿系统
- 自动保存排班草稿
- 草稿预览和编辑
- 草稿确认和发布
- **自动清理**：超过7天的草稿自动删除

## 故障排除

### 常见问题

1. **数据库初始化失败**
   - 检查磁盘空间
   - 确认应用写入权限
   - 查看日志获取详细错误

2. **排班算法无解**
   - 检查约束配置是否过严
   - 确认人员和技能配置
   - 尝试放宽约束条件

3. **界面显示异常**
   - 尝试切换主题
   - 重启应用
   - 检查 Windows 版本

### 日志查看

- 日志输出到调试控制台
- 使用 Visual Studio 或 DebugView 查看
- 所有日志使用 `[App]` 前缀

## 重要文件

- **[App.xaml.cs](App.xaml.cs)**：应用启动和初始化
- **[MainWindow.xaml.cs](MainWindow.xaml.cs)**：主窗口
- **[Extensions/ServiceCollectionExtensions.cs](Extensions/ServiceCollectionExtensions.cs)**：依赖注入配置
- **[Services/SchedulingService.cs](Services/SchedulingService.cs)**：排班核心服务
- **[Data/DatabaseService.cs](Data/DatabaseService.cs)**：数据库服务
- **[SchedulingEngine/GreedyScheduler.cs](SchedulingEngine/GreedyScheduler/GreedyScheduler.cs)**：贪心调度器

## 注意事项

1. **数据库路径**：使用 `DatabaseConfiguration.GetDefaultDatabasePath()` 获取
2. **异步初始化**：所有服务在 `App.xaml.cs` 中通过 `InitializeAsync()` 初始化
3. **依赖注入**：DatabaseService 为单例，必须先于其他服务初始化
4. **草稿清理**：应用启动时自动清理过期草稿（>7天）
5. **异常处理**：应用级别异常处理在 `App_UnhandledException` 中
6. **WinUI 3 兼容**：需要 Windows 10 1809+ (Build 17763)
