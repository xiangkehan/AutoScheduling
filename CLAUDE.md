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

## 排班创建流程（重构后）

### 步骤顺序
1. **步骤1：基本信息** - 设置排班标题和日期范围
2. **步骤2：选择哨位** - 选择参与排班的哨位（原步骤3）
3. **步骤3：选择人员** - 基于哨位的智能人员管理（重构后）
4. **步骤4：配置约束** - 设置休息日、定岗规则、手动指定
5. **步骤5：确认执行** - 查看摘要并开始排班

### 步骤3：智能人员管理（新功能）

#### PositionPersonnelManager
**位置**：`ViewModels/Scheduling/PositionPersonnelManager.cs`

**设计目的**：
管理哨位与人员的临时关联关系，支持在不修改数据库的情况下进行人员调整，并提供将临时更改保存为永久的功能。

**核心功能**：
- 管理哨位与人员的临时关联关系
- 跟踪临时性更改（添加/移除人员）
- 提供撤销功能
- 区分原始数据和当前数据
- 支持查询所有有更改的哨位

**数据结构**：
```csharp
// 原始数据（从数据库加载）
private Dictionary<int, List<int>> _originalAvailablePersonnel;

// 当前数据（包含临时更改）
private Dictionary<int, List<int>> _currentAvailablePersonnel;

// 临时更改记录
private Dictionary<int, PositionPersonnelChanges> _changes;
```

**关键方法**：
```csharp
// 初始化管理器，从PositionDto列表加载数据
void Initialize(IEnumerable<PositionDto> positions)

// 获取哨位的当前可用人员列表（包含临时更改）
List<int> GetAvailablePersonnel(int positionId)

// 临时添加人员到哨位
void AddPersonnelTemporarily(int positionId, int personnelId)

// 临时移除人员从哨位
void RemovePersonnelTemporarily(int positionId, int personnelId)

// 获取哨位的临时更改记录
PositionPersonnelChanges GetChanges(int positionId)

// 撤销指定哨位的所有临时更改
void RevertChanges(int positionId)

// 撤销所有哨位的临时更改
void RevertAllChanges()

// 获取所有有临时更改的哨位ID列表
List<int> GetPositionsWithChanges()

// 清空所有数据
void Clear()
```

**使用场景**：
1. **初始化**：在步骤3开始时，从 `SelectedPositions` 初始化
2. **临时调整**：用户在UI中添加/移除人员时，调用相应方法
3. **查询状态**：UI需要显示当前状态时，调用 `GetAvailablePersonnel` 和 `GetChanges`
4. **撤销操作**：用户点击撤销按钮时，调用 `RevertChanges`
5. **保存为永久**：用户确认保存时，获取更改并调用 `IPositionService.UpdateAsync`

**注意事项**：
- 所有临时更改仅存在于内存中，不影响数据库
- 调用 `Initialize` 会清空所有临时更改
- 保存为永久后需要重新调用 `Initialize` 以同步状态

#### 两种添加人员方式

系统提供两种不同的人员添加方式，适用于不同的场景：

**方式1：为指定哨位临时添加人员**
- **操作位置**：在哨位卡片中点击"为此哨位添加人员"按钮
- **选择范围**：从所有可用人员（`AvailablePersonnels`）中选择
- **数据变化**：`PositionPersonnelManager` 跟踪该哨位的临时更改
- **UI显示**：在该哨位的人员列表中显示，带 ➕ 图标
- **可保存为永久**：✅ 可以通过 `IPositionService.UpdateAsync` 保存到数据库
- **排班算法中的可见性**：只对该哨位可用
- **适用场景**：为特定哨位增加临时人员，如临时调配、应急支援

**方式2：手动添加参与人员（不属于任何哨位）**
- **操作位置**：在"手动添加的人员"区域点击"添加参与人员（对所有哨位可用）"按钮
- **选择范围**：从不在任何已选哨位可用人员列表中的人员中选择
- **数据变化**：添加到 `ManuallyAddedPersonnelIds` 列表
- **UI显示**：在"手动添加的人员"区域单独显示
- **可保存为永久**：❌ 不可以（因为没有关联到具体哨位）
- **排班算法中的可见性**：对所有哨位可用

**关键区别**：
| 特性 | 为哨位临时添加 | 手动添加参与人员 |
|------|--------------|----------------|
| 关联哨位 | 特定哨位 | 无（全局） |
| 数据管理 | PositionPersonnelManager | ManuallyAddedPersonnelIds |
| 可保存为永久 | ✅ 是 | ❌ 否 |
| 算法可见性 | 仅该哨位 | 所有哨位 |
| 撤销方式 | 单个哨位撤销 | 单个人员移除 |

#### ViewModel模型

**PersonnelSourceType（人员来源类型）**
```csharp
public enum PersonnelSourceType
{
    AutoExtracted,      // 自动提取（从哨位的原始可用人员列表）
    TemporarilyAdded,   // 临时添加（在本次排班中添加到哨位）
    TemporarilyRemoved, // 临时移除（在本次排班中从哨位移除）
    ManuallyAdded       // 手动添加（不属于任何哨位，手动添加到参与人员）
}
```

**PositionPersonnelViewModel（哨位人员视图模型）**
```csharp
public class PositionPersonnelViewModel : ObservableObject
{
    public int PositionId { get; set; }
    public string PositionName { get; set; }
    public string Location { get; set; }
    
    // 可用人员列表（包含临时更改）
    public ObservableCollection<PersonnelItemViewModel> AvailablePersonnel { get; set; }
    
    // 是否展开
    [ObservableProperty]
    private bool _isExpanded = true;
    
    // 是否有临时更改
    [ObservableProperty]
    private bool _hasChanges;
    
    // 临时更改摘要
    [ObservableProperty]
    private string _changesSummary;
}
```

**PersonnelItemViewModel（人员项视图模型）**
```csharp
public class PersonnelItemViewModel : ObservableObject
{
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; }
    public string SkillsDisplay { get; set; }
    
    // 是否被选中
    [ObservableProperty]
    private bool _isSelected;
    
    // 人员来源类型
    [ObservableProperty]
    private PersonnelSourceType _sourceType;
    
    // 是否被多个哨位共享
    [ObservableProperty]
    private bool _isShared;
    
    // 共享的哨位数量
    [ObservableProperty]
    private int _sharedPositionCount;
}
```

**PositionPersonnelChanges（哨位人员更改记录）**
```csharp
public class PositionPersonnelChanges
{
    public int PositionId { get; set; }
    public string PositionName { get; set; }
    public List<int> AddedPersonnelIds { get; set; }
    public List<string> AddedPersonnelNames { get; set; }
    public List<int> RemovedPersonnelIds { get; set; }
    public List<string> RemovedPersonnelNames { get; set; }
    public bool HasChanges => AddedPersonnelIds.Any() || RemovedPersonnelIds.Any();
}
```

#### 自动提取人员逻辑
```csharp
// 从所有已选哨位的AvailablePersonnelIds中提取唯一的人员ID
var autoExtractedPersonnelIds = SelectedPositions
    .SelectMany(p => p.AvailablePersonnelIds)
    .Distinct()
    .ToList();

// 从AvailablePersonnels中获取对应的人员详细信息
var autoExtractedPersonnels = AvailablePersonnels
    .Where(p => autoExtractedPersonnelIds.Contains(p.Id))
    .ToList();
```

#### 手动添加人员逻辑
```csharp
// 筛选不在任何哨位可用人员列表中的人员
var autoExtractedPersonnelIds = SelectedPositions
    .SelectMany(p => _positionPersonnelManager.GetAvailablePersonnel(p.Id))
    .Distinct()
    .ToHashSet();

var availableForManualAdd = AvailablePersonnels
    .Where(p => !autoExtractedPersonnelIds.Contains(p.Id))
    .ToList();
```

#### 保存为永久

将临时更改保存到数据库的完整流程：

```csharp
private async Task SavePositionChangesAsync(int positionId)
{
    try
    {
        // 1. 获取哨位的临时更改
        var changes = _positionPersonnelManager.GetChanges(positionId);
        if (!changes.HasChanges)
        {
            await _dialogService.ShowWarningAsync("没有需要保存的更改");
            return;
        }
        
        // 2. 显示确认对话框
        var confirmed = await ShowSaveConfirmationDialog(changes);
        if (!confirmed)
            return;
        
        // 3. 获取哨位
        var position = SelectedPositions.FirstOrDefault(p => p.Id == positionId);
        if (position == null)
        {
            await _dialogService.ShowErrorAsync("哨位不存在");
            return;
        }
        
        // 4. 更新哨位的可用人员列表
        var updatedPersonnelIds = _positionPersonnelManager.GetAvailablePersonnel(positionId);
        var updateDto = new UpdatePositionDto
        {
            Name = position.Name,
            Location = position.Location,
            Description = position.Description,
            Requirements = position.Requirements,
            RequiredSkillIds = position.RequiredSkillIds,
            AvailablePersonnelIds = updatedPersonnelIds
        };
        
        // 5. 调用服务更新数据库
        await _positionService.UpdateAsync(positionId, updateDto);
        
        // 6. 更新本地数据
        position.AvailablePersonnelIds = updatedPersonnelIds;
        
        // 7. 重新初始化管理器（清除临时更改标记）
        _positionPersonnelManager.Initialize(SelectedPositions);
        
        // 8. 显示成功提示
        await _dialogService.ShowSuccessAsync("更改已保存");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"保存哨位更改失败: {ex.Message}");
        await _dialogService.ShowErrorAsync("保存失败", ex);
    }
}
```

**关键步骤说明**：
1. **验证更改**：检查是否有实际的更改需要保存
2. **用户确认**：显示详细的更改列表，让用户确认
3. **数据验证**：确保哨位存在且数据有效
4. **构建DTO**：创建包含所有字段的更新DTO
5. **数据库更新**：通过服务层更新数据库
6. **本地同步**：更新本地PositionDto对象
7. **状态重置**：重新初始化管理器，清除临时更改标记
8. **用户反馈**：显示成功或失败提示

**注意事项**：
- 保存失败时保留临时更改状态，用户可以重试
- 重新初始化后，之前的临时更改标记会被清除
- 只有为哨位临时添加的人员可以保存为永久
- 手动添加的参与人员不能保存为永久（因为没有关联到具体哨位）

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

## 实现细节和最佳实践

### ViewModel中的新增属性

在 `SchedulingViewModel` 中添加了以下属性来支持新功能：

```csharp
// 哨位人员管理器
private readonly PositionPersonnelManager _positionPersonnelManager;

// 手动添加的人员ID列表
[ObservableProperty]
private ObservableCollection<int> _manuallyAddedPersonnelIds = new();

// 统计信息
[ObservableProperty]
private int _autoExtractedPersonnelCount;

[ObservableProperty]
private int _manuallyAddedPersonnelCount;

// UI状态
[ObservableProperty]
private bool _isAddingPersonnelToPosition;

[ObservableProperty]
private PositionDto? _currentEditingPosition;

[ObservableProperty]
private ObservableCollection<PersonnelDto> _availablePersonnelForPosition = new();
```

### 新增命令

```csharp
// 为哨位添加人员
public IRelayCommand<PositionDto> StartAddPersonnelToPositionCommand { get; }
public IAsyncRelayCommand SubmitAddPersonnelToPositionCommand { get; }
public IRelayCommand CancelAddPersonnelToPositionCommand { get; }

// 移除和撤销
public IRelayCommand<(int positionId, int personnelId)> RemovePersonnelFromPositionCommand { get; }
public IAsyncRelayCommand<int> SavePositionChangesCommand { get; }
public IRelayCommand<int> RevertPositionChangesCommand { get; }

// 手动添加参与人员
public IRelayCommand StartManualAddPersonnelCommand { get; }
public IAsyncRelayCommand SubmitManualAddPersonnelCommand { get; }
public IRelayCommand CancelManualAddPersonnelCommand { get; }
public IRelayCommand<int> RemoveManualPersonnelCommand { get; }
```

### 性能优化

**虚拟化渲染**：
- 使用 `ItemsRepeater` 代替 `ListView` 实现虚拟化
- 支持大量哨位和人员的高效渲染

**缓存机制**：
```csharp
// 缓存人员ID到PersonnelDto的映射
private Dictionary<int, PersonnelDto> _personnelCache;

// 缓存哨位ID到PositionDto的映射
private Dictionary<int, PositionDto> _positionCache;
```

**性能指标**：
- 人员提取时间：< 500ms（50个哨位）
- UI更新时间：< 100ms
- 保存时间：< 1s

### 草稿和模板兼容性

**草稿保存**：
```csharp
// 扩展SchedulingDraftDto
public class SchedulingDraftDto
{
    // ... 现有字段
    
    // 新增字段
    public Dictionary<int, PositionPersonnelChanges>? PositionPersonnelChanges { get; set; }
    public List<int>? ManuallyAddedPersonnelIds { get; set; }
}
```

**模板加载**：
- 模板不保存临时更改，只保存原始的哨位和人员选择
- 加载模板后，用户可以重新进行临时调整

### 错误处理策略

**数据验证**：
```csharp
// 验证人员ID是否存在
if (!AvailablePersonnels.Any(p => p.Id == personnelId))
{
    await _dialogService.ShowErrorAsync("人员不存在");
    return;
}

// 验证哨位ID是否存在
if (!SelectedPositions.Any(p => p.Id == positionId))
{
    await _dialogService.ShowErrorAsync("哨位不存在");
    return;
}
```

**异常处理**：
- 所有关键方法都包含 try-catch 块
- 使用 `DialogService` 显示友好的错误信息
- 记录详细的调试日志

**日志记录**：
```csharp
System.Diagnostics.Debug.WriteLine($"[PositionPersonnelManager] 初始化完成，共 {positions.Count()} 个哨位");
System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel] 自动提取人员完成，共 {autoExtractedCount} 人");
```

## 注意事项

1. **数据库路径**：使用 `DatabaseConfiguration.GetDefaultDatabasePath()` 获取
2. **异步初始化**：所有服务在 `App.xaml.cs` 中通过 `InitializeAsync()` 初始化
3. **依赖注入**：DatabaseService 为单例，必须先于其他服务初始化
4. **草稿清理**：应用启动时自动清理过期草稿（>7天）
5. **异常处理**：应用级别异常处理在 `App_UnhandledException` 中
6. **WinUI 3 兼容**：需要 Windows 10 1809+ (Build 17763)
7. **步骤顺序**：步骤2和步骤3已互换，步骤2现在是选择哨位，步骤3是选择人员
8. **临时更改**：哨位的临时人员更改仅在本次排班中生效，完成或取消后自动丢弃
9. **手动添加人员**：手动添加的人员不属于任何哨位，可以被分配到任何哨位
10. **保存为永久**：只有为哨位临时添加的人员可以保存为永久，手动添加的人员不可以
11. **PositionPersonnelManager**：在步骤切换时需要正确初始化和清理
12. **UI虚拟化**：使用 `ItemsRepeater` 处理大量数据时的性能优化
13. **数据同步**：保存为永久后必须重新初始化 `PositionPersonnelManager`
14. **错误恢复**：保存失败时保留临时更改状态，允许用户重试
