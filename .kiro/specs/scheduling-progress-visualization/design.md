# 设计文档

## 概述

本设计文档描述了排班过程和结果展示页面的技术实现方案。该功能将为用户提供一个实时的、可视化的界面，展示排班算法的执行过程、进度状态和最终结果。

## 架构

### 整体架构

系统采用 MVVM 架构模式，主要组件包括：

1. **SchedulingProgressPage（视图层）**：WinUI 3 页面，负责UI展示和用户交互
2. **SchedulingProgressViewModel（视图模型层）**：管理页面状态、进度数据和用户命令
3. **SchedulingService（服务层）**：现有服务，需要扩展以支持进度报告
4. **GreedyScheduler（算法引擎）**：现有调度器，需要扩展以支持进度回调
5. **IProgress<T>接口**：.NET标准进度报告机制

### 数据流

```
用户点击"开始排班" 
  → CreateSchedulingPage 导航到 SchedulingProgressPage
  → SchedulingProgressViewModel 调用 SchedulingService.ExecuteSchedulingAsync
  → GreedyScheduler 执行算法并通过 IProgress 报告进度
  → SchedulingProgressViewModel 更新UI绑定属性
  → SchedulingProgressPage 实时显示进度和状态
  → 完成后显示结果统计和操作按钮
```

## 组件和接口

### 1. SchedulingProgressReport（进度报告模型）

```csharp
public class SchedulingProgressReport
{
    // 基本进度信息
    public double ProgressPercentage { get; set; }
    public SchedulingStage CurrentStage { get; set; }
    public string StageDescription { get; set; }
    
    // 统计信息
    public int CompletedAssignments { get; set; }
    public int TotalSlotsToAssign { get; set; }
    public int RemainingSlots { get; set; }
    
    // 当前处理信息
    public string? CurrentPositionName { get; set; }
    public int CurrentPeriodIndex { get; set; }
    public DateTime CurrentDate { get; set; }
    
    // 时间信息
    public TimeSpan ElapsedTime { get; set; }
    
    // 警告和错误
    public List<string> Warnings { get; set; }
    public bool HasErrors { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### 2. SchedulingStage（算法阶段枚举）

```csharp
public enum SchedulingStage
{
    Initializing,           // 初始化
    LoadingData,            // 加载数据
    BuildingContext,        // 构建上下文
    InitializingTensor,     // 初始化可行性张量
    ApplyingConstraints,    // 应用约束
    ApplyingManualAssignments, // 应用手动指定
    GreedyAssignment,       // 贪心分配
    UpdatingScores,         // 更新评分
    Finalizing,             // 完成处理
    Completed,              // 完成
    Failed                  // 失败
}
```

### 3. SchedulingResult（排班结果模型）

```csharp
public class SchedulingResult
{
    public bool IsSuccess { get; set; }
    public ScheduleDto? Schedule { get; set; }
    public SchedulingStatistics? Statistics { get; set; }
    public List<ConflictInfo> Conflicts { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan TotalDuration { get; set; }
}

public class SchedulingStatistics
{
    public int TotalAssignments { get; set; }
    public Dictionary<int, PersonnelWorkload> PersonnelWorkloads { get; set; }
    public Dictionary<int, PositionCoverage> PositionCoverages { get; set; }
    public SoftConstraintScores SoftScores { get; set; }
}

public class PersonnelWorkload
{
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; }
    public int TotalShifts { get; set; }
    public int DayShifts { get; set; }
    public int NightShifts { get; set; }
}

public class PositionCoverage
{
    public int PositionId { get; set; }
    public string PositionName { get; set; }
    public int AssignedSlots { get; set; }
    public int TotalSlots { get; set; }
    public double CoverageRate { get; set; }
}

public class SoftConstraintScores
{
    public double TotalScore { get; set; }
    public double RestScore { get; set; }
    public double TimeSlotBalanceScore { get; set; }
    public double HolidayBalanceScore { get; set; }
}

public class ConflictInfo
{
    public string ConflictType { get; set; }
    public string Description { get; set; }
    public int? PositionId { get; set; }
    public string? PositionName { get; set; }
    public int? PeriodIndex { get; set; }
    public DateTime? Date { get; set; }
}
```

### 3.1 ScheduleGridData（排班表格数据模型）

```csharp
public class ScheduleGridData
{
    // 表格结构
    public List<ScheduleGridColumn> Columns { get; set; }
    public List<ScheduleGridRow> Rows { get; set; }
    public Dictionary<string, ScheduleGridCell> Cells { get; set; }
    
    // 元数据
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<int> PositionIds { get; set; }
    public int TotalDays { get; set; }
    public int TotalPeriods { get; set; }
}

public class ScheduleGridColumn
{
    public int PositionId { get; set; }
    public string PositionName { get; set; }
    public int ColumnIndex { get; set; }
}

public class ScheduleGridRow
{
    public DateTime Date { get; set; }
    public int PeriodIndex { get; set; }
    public string TimeRange { get; set; } // 如 "08:00-10:00"
    public string DisplayText { get; set; } // 如 "2025-01-15 08:00-10:00"
    public int RowIndex { get; set; }
}

public class ScheduleGridCell
{
    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }
    public int? PersonnelId { get; set; }
    public string? PersonnelName { get; set; }
    public bool IsAssigned { get; set; }
    public bool IsManualAssignment { get; set; }
    public bool HasConflict { get; set; }
    public string? ConflictMessage { get; set; }
}
```

### 3.2 IScheduleGridExporter（表格导出接口）

```csharp
public interface IScheduleGridExporter
{
    /// <summary>
    /// 导出排班表格
    /// </summary>
    /// <param name="gridData">表格数据</param>
    /// <param name="format">导出格式（如 "excel", "csv", "pdf"）</param>
    /// <param name="options">导出选项</param>
    /// <returns>导出的文件字节数组</returns>
    Task<byte[]> ExportAsync(
        ScheduleGridData gridData, 
        string format, 
        ExportOptions? options = null);
    
    /// <summary>
    /// 获取支持的导出格式列表
    /// </summary>
    List<string> GetSupportedFormats();
    
    /// <summary>
    /// 验证导出格式是否支持
    /// </summary>
    bool IsFormatSupported(string format);
}

public class ExportOptions
{
    public bool IncludeHeader { get; set; } = true;
    public bool IncludeEmptyCells { get; set; } = true;
    public bool HighlightConflicts { get; set; } = true;
    public bool HighlightManualAssignments { get; set; } = true;
    public string? Title { get; set; }
    public Dictionary<string, object>? CustomOptions { get; set; }
}
```

### 4. SchedulingProgressViewModel

```csharp
public partial class SchedulingProgressViewModel : ObservableObject
{
    // 服务依赖
    private readonly ISchedulingService _schedulingService;
    private readonly NavigationService _navigationService;
    private readonly IScheduleGridExporter _gridExporter;
    private CancellationTokenSource? _cancellationTokenSource;
    
    // 进度状态
    [ObservableProperty] private double _progressPercentage;
    [ObservableProperty] private string _currentStage;
    [ObservableProperty] private string _stageDescription;
    [ObservableProperty] private bool _isExecuting;
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private bool _isFailed;
    [ObservableProperty] private bool _isCancelled;
    
    // 统计信息
    [ObservableProperty] private int _completedAssignments;
    [ObservableProperty] private int _totalSlotsToAssign;
    [ObservableProperty] private string _currentPositionName;
    [ObservableProperty] private string _currentTimeSlot;
    [ObservableProperty] private string _elapsedTime;
    
    // 结果数据
    [ObservableProperty] private SchedulingResult? _result;
    [ObservableProperty] private ObservableCollection<PersonnelWorkload> _personnelWorkloads;
    [ObservableProperty] private ObservableCollection<PositionCoverage> _positionCoverages;
    [ObservableProperty] private ObservableCollection<ConflictInfo> _conflicts;
    
    // 表格数据
    [ObservableProperty] private ScheduleGridData? _gridData;
    [ObservableProperty] private bool _isGridFullScreen;
    
    // 命令
    public IAsyncRelayCommand<SchedulingRequestDto> StartSchedulingCommand { get; }
    public IAsyncRelayCommand CancelSchedulingCommand { get; }
    public IAsyncRelayCommand SaveScheduleCommand { get; }
    public IRelayCommand DiscardScheduleCommand { get; }
    public IRelayCommand ViewDetailedResultCommand { get; }
    public IRelayCommand ReturnToConfigCommand { get; }
    public IRelayCommand ToggleGridFullScreenCommand { get; }
    public IAsyncRelayCommand<string> ExportGridCommand { get; }
    
    // 辅助方法
    private ScheduleGridData BuildGridData(ScheduleDto schedule);
}
```

### 5. SchedulingProgressPage（XAML视图）

页面布局采用以下结构：

```
┌─────────────────────────────────────────────────────────────────────┐
│  顶部标题栏                                                          │
│  - 排班标题                                                          │
│  - 总体状态图标                                                      │
└─────────────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────────────┐
│  进度指示区                                                          │
│  - 进度条（ProgressBar）                                             │
│  - 当前阶段名称和描述                                                │
│  - 已完成/总数统计（如：125/500 个时段已分配）                       │
└─────────────────────────────────────────────────────────────────────┘
┌──────────────────┬──────────────────────────────────────────────────┐
│  实时信息区      │  结果展示区                                      │
│  （左侧，窄）    │  （右侧上方，宽）                                │
│  可滚动          │  可滚动                                          │
│                  │                                                  │
│  - 当前处理信息  │  ┌────────────────────────────────────────────┐ │
│    · 哨位名称    │  │ 排班结果表格                    [导出][全屏]│ │
│    · 时段        │  ├────┬────────┬────────┬────────┬──────────┤ │
│    · 日期        │  │日期│ 哨位1  │ 哨位2  │ 哨位3  │   ...    │ │
│  - 累计执行时间  │  │时段│        │        │        │          │ │
│  - 阶段历史      │  ├────┼────────┼────────┼────────┼──────────┤ │
│    ✓ 初始化      │  │01-15│ 张三  │ 李四  │ 王五  │   ...    │ │
│    ✓ 加载数据    │  │08:00│        │        │        │          │ │
│    ⟳ 贪心分配    │  ├────┼────────┼────────┼────────┼──────────┤ │
│    ○ 完成处理    │  │01-15│ 赵六  │ 孙七  │   -   │   ...    │ │
│  - 警告信息列表  │  │10:00│        │        │        │          │ │
│    ⚠ 哨位3未分配 │  ├────┼────────┼────────┼────────┼──────────┤ │
│                  │  │ ... │  ...   │  ...   │  ...   │   ...    │ │
│                  │  └────┴────────┴────────┴────────┴──────────┘ │
│                  │                                                  │
│                  │  - 成功/失败状态卡片                             │
│                  │  - 统计信息卡片                                  │
│                  │    · 总分配数：450/500                           │
│                  │    · 平均工作量：18.5 班次/人                    │
│                  │    · 覆盖率：90%                                 │
│                  │  - 人员工作量表格（虚拟化）                      │
│                  │  - 哨位覆盖率表格（虚拟化）                      │
│                  │  - 冲突列表（如有）                              │
│                  │                                                  │
│                  ├──────────────────────────────────────────────────┤
│                  │  操作按钮区                                      │
│                  │  （右侧下方）                                    │
│                  │                                                  │
│                  │  执行中：                                        │
│                  │  [取消排班]                                      │
│                  │                                                  │
│                  │  成功后：                                        │
│                  │  [保存排班] [放弃排班] [查看详细结果]            │
│                  │                                                  │
│                  │  失败后：                                        │
│                  │  [返回修改] [查看错误详情]                       │
└──────────────────┴──────────────────────────────────────────────────┘
```

**布局说明**：
- **顶部标题栏**：全宽，固定高度约 60px
- **进度指示区**：全宽，固定高度约 80px
- **主内容区**：两列布局
  - **左列（实时信息区）**：宽度约 30%（最小 300px），可滚动
    - 显示当前处理信息
    - 阶段历史列表（带状态图标）
    - 警告信息列表
  - **右列**：宽度约 70%，分为上下两部分
    - **上部（结果展示区）**：可滚动，占右列约 85% 高度
      - **排班结果表格**（最顶部，占据主要空间）
        - 表格右上角有 [导出] 和 [全屏] 按钮
        - 列头：哨位名称（固定）
        - 行头：日期 + 时段（固定）
        - 单元格：人员名称
        - 支持横向和纵向滚动
        - 虚拟化渲染
      - 统计信息卡片（表格下方）
      - 人员工作量表格
      - 哨位覆盖率表格
      - 冲突列表
    - **下部（操作按钮区）**：固定高度约 80px，始终可见

### 5.1 结果表格详细设计

**表格结构**：
- **列头（顶部）**：哨位名称
  - 固定表头，滚动时保持可见
  - 每列宽度自适应，最小宽度 120px
- **行头（左侧）**：日期 + 时段
  - 格式：`2025-01-15 08:00-10:00`
  - 固定列，滚动时保持可见
  - 行高固定 40px
- **单元格**：人员名称
  - 显示分配的人员姓名
  - 未分配显示为空或 "-"
  - 手动指定的单元格有特殊标识（如边框颜色）
  - 有冲突的单元格有警告标识

**表格功能**：
1. **全屏模式**：
   - 右上角有全屏按钮（FullScreenIcon）
   - 点击后表格占据整个窗口
   - 全屏模式下显示关闭按钮返回正常视图
   - 使用 WinUI 3 的 ContentDialog 或自定义全屏覆盖层

2. **交互功能**：
   - 单元格悬停显示详细信息（Tooltip）
     - 人员姓名
     - 人员技能
     - 当前工作量
     - 是否手动指定
   - 支持横向和纵向滚动
   - 虚拟化渲染（仅渲染可见区域）

3. **导出功能（预留接口）**：
   - 导出按钮位于表格右上角（全屏按钮旁边）
   - 支持多种格式：Excel、CSV、PDF（未来扩展）
   - 当前版本仅实现接口定义，不实现具体导出逻辑
   - 点击导出按钮显示"功能开发中"提示

**表格实现方案**：
```xaml
<Grid x:Name="ResultGridContainer">
    <!-- 表格工具栏 -->
    <StackPanel Orientation="Horizontal" 
                HorizontalAlignment="Right"
                Margin="0,0,0,8">
        <Button x:Name="ExportGridButton"
                Content="导出"
                ToolTipService.ToolTip="导出表格（开发中）"
                IsEnabled="False"
                Margin="0,0,8,0"/>
        <Button x:Name="FullScreenButton"
                Content="&#xE740;"
                FontFamily="Segoe MDL2 Assets"
                ToolTipService.ToolTip="全屏查看"
                Command="{x:Bind ViewModel.ToggleGridFullScreenCommand}"/>
    </StackPanel>
    
    <!-- 表格主体 -->
    <ScrollViewer HorizontalScrollBarVisibility="Auto"
                  VerticalScrollBarVisibility="Auto">
        <Grid x:Name="ScheduleGrid">
            <!-- 使用 ItemsRepeater 或自定义控件实现虚拟化表格 -->
        </Grid>
    </ScrollViewer>
</Grid>
```

**全屏对话框实现**：
```csharp
public async Task ShowGridFullScreenAsync()
{
    var dialog = new ContentDialog
    {
        Title = "排班结果表格",
        Content = new ScheduleGridFullScreenView 
        { 
            DataContext = GridData 
        },
        CloseButtonText = "关闭",
        DefaultButton = ContentDialogButton.Close,
        XamlRoot = this.XamlRoot
    };
    
    // 设置对话框为全屏
    dialog.Style = (Style)Application.Current.Resources["FullScreenDialogStyle"];
    
    await dialog.ShowAsync();
}

## 数据模型

### SchedulingRequestDto（扩展）

现有的 SchedulingRequestDto 无需修改，将作为参数传递给进度页面。

### 进度报告流程

1. GreedyScheduler 在关键节点调用 `IProgress<SchedulingProgressReport>.Report()`
2. SchedulingService 接收进度报告并转发
3. SchedulingProgressViewModel 更新UI绑定属性
4. WinUI 3 数据绑定自动更新界面

## 错误处理

### 错误类型

1. **参数验证错误**：在开始执行前捕获，显示错误消息并返回配置页面
2. **算法执行错误**：
   - 无可行解：显示冲突详情和建议
   - 约束冲突：显示违反的约束和相关哨位/人员
   - 超时：显示部分结果和超时原因
3. **系统错误**：数据库错误、内存不足等，显示技术错误信息

### 错误处理策略

```csharp
try
{
    // 执行排班
    var result = await _schedulingService.ExecuteSchedulingAsync(
        request, 
        progress, 
        cancellationToken);
    
    if (result.IsSuccess)
    {
        // 显示成功结果
        ShowSuccessResult(result);
    }
    else
    {
        // 显示失败信息
        ShowFailureResult(result);
    }
}
catch (OperationCanceledException)
{
    // 用户取消
    ShowCancelledState();
}
catch (ArgumentException ex)
{
    // 参数错误
    ShowParameterError(ex.Message);
}
catch (InvalidOperationException ex)
{
    // 业务逻辑错误
    ShowBusinessError(ex.Message);
}
catch (Exception ex)
{
    // 未预期的系统错误
    ShowSystemError(ex);
}
```

## 测试策略

### 单元测试

1. **SchedulingProgressViewModel 测试**
   - 进度更新逻辑
   - 命令执行逻辑
   - 状态转换逻辑
   - 取消操作逻辑

2. **进度报告模型测试**
   - 数据验证
   - 百分比计算
   - 时间格式化

### 集成测试

1. **端到端排班流程测试**
   - 小规模数据集（5人员 × 3哨位 × 3天）
   - 中等规模数据集（20人员 × 10哨位 × 7天）
   - 大规模数据集（50人员 × 20哨位 × 30天）

2. **取消操作测试**
   - 在不同阶段取消
   - 验证资源清理
   - 验证状态恢复

3. **错误场景测试**
   - 无可行解场景
   - 约束冲突场景
   - 数据不完整场景

### UI测试

1. **进度显示测试**
   - 进度条更新流畅性
   - 信息显示准确性
   - 响应式布局适配

2. **用户交互测试**
   - 按钮状态切换
   - 对话框显示
   - 导航流程

## 性能考虑

### 进度报告频率

为避免过度更新UI导致性能问题，采用以下策略：

1. **节流机制**：最小更新间隔为 100ms
2. **批量更新**：累积多个小更新后一次性报告
3. **关键节点报告**：
   - 每个阶段开始和结束
   - 每完成 10% 的分配
   - 每处理完一天的排班
   - 发现警告或错误时

### UI更新优化

1. **虚拟化列表**：使用 ListView 的虚拟化功能显示大量数据
2. **延迟加载**：详细结果在用户点击时才加载
3. **异步操作**：所有耗时操作使用 async/await
4. **取消支持**：使用 CancellationToken 支持用户取消

### 内存管理

1. **及时释放**：完成或取消后释放大对象
2. **弱引用**：对临时数据使用弱引用
3. **分页显示**：大数据集分页显示

## 可访问性

### 屏幕阅读器支持

1. 为所有控件设置 AutomationProperties.Name
2. 进度变化时通知屏幕阅读器
3. 错误消息使用 LiveRegion

### 键盘导航

1. 所有按钮支持 Tab 键导航
2. 支持快捷键：
   - Esc：取消执行
   - Enter：确认操作
   - Ctrl+S：保存结果

### 高对比度主题

1. 支持系统高对比度主题
2. 进度条和状态指示器在高对比度下清晰可见
3. 错误和警告使用明显的视觉标识

## 国际化

### 多语言支持

1. 所有文本使用资源文件
2. 日期和时间格式本地化
3. 数字格式本地化

### 资源文件结构

```
Resources/
  zh-CN/
    SchedulingProgress.resw
  en-US/
    SchedulingProgress.resw
```

## 安全考虑

### 数据验证

1. 输入参数验证
2. 进度数据范围检查
3. 防止注入攻击

### 权限控制

1. 验证用户是否有排班权限
2. 验证用户是否有保存权限
3. 审计日志记录

## 扩展性

### 未来扩展点

1. **导出进度日志**：支持导出详细的执行日志
2. **算法参数调整**：允许用户在执行前调整算法参数
3. **实时预览**：在执行过程中显示部分结果预览
4. **多算法支持**：支持切换不同的排班算法
5. **性能分析**：显示算法性能指标和瓶颈分析

### 插件架构

预留接口支持第三方扩展：

```csharp
public interface ISchedulingProgressPlugin
{
    string Name { get; }
    void OnProgressUpdate(SchedulingProgressReport report);
    void OnCompleted(SchedulingResult result);
    void OnFailed(string error);
}
```

## 技术债务和已知限制

### 当前限制

1. **单线程执行**：当前算法在UI线程执行，可能阻塞界面
2. **内存占用**：大规模排班可能占用较多内存
3. **取消延迟**：取消操作可能需要等待当前迭代完成

### 改进计划

1. **后台线程**：将算法执行移到后台线程
2. **流式处理**：对大数据集使用流式处理
3. **即时取消**：改进取消机制，支持即时响应

## 依赖关系

### 外部依赖

1. **WinUI 3**：UI框架
2. **CommunityToolkit.Mvvm**：MVVM框架
3. **Microsoft.Extensions.DependencyInjection**：依赖注入

### 内部依赖

1. **SchedulingService**：排班服务
2. **GreedyScheduler**：排班算法
3. **NavigationService**：导航服务
4. **DialogService**：对话框服务

## 部署考虑

### 配置项

```json
{
  "SchedulingProgress": {
    "MinUpdateInterval": 100,
    "MaxExecutionTime": 300000,
    "EnableDetailedLogging": false,
    "AutoSaveOnSuccess": false
  }
}
```

### 监控指标

1. 平均执行时间
2. 成功率
3. 取消率
4. 错误类型分布

## 排班结果表格组件设计

### 组件概述

排班结果表格是一个自定义的 WinUI 3 控件，用于以网格形式展示排班结果。表格采用固定表头和行头的设计，支持大数据集的虚拟化渲染。

### 组件结构

```
ScheduleGridControl
├── GridToolbar（工具栏）
│   ├── ExportButton（导出按钮，预留）
│   └── FullScreenButton（全屏按钮）
├── GridHeader（列头区域）
│   └── PositionColumns（哨位列）
├── GridBody（表格主体）
│   ├── RowHeaders（行头：日期+时段）
│   └── CellsContainer（单元格容器）
│       └── PersonnelCells（人员单元格）
└── GridScrollViewer（滚动容器）
```

### 数据绑定

```csharp
public class ScheduleGridControl : Control
{
    // 依赖属性
    public static readonly DependencyProperty GridDataProperty = 
        DependencyProperty.Register(
            nameof(GridData),
            typeof(ScheduleGridData),
            typeof(ScheduleGridControl),
            new PropertyMetadata(null, OnGridDataChanged));
    
    public ScheduleGridData GridData
    {
        get => (ScheduleGridData)GetValue(GridDataProperty);
        set => SetValue(GridDataProperty, value);
    }
    
    // 事件
    public event EventHandler<CellClickedEventArgs> CellClicked;
    public event EventHandler FullScreenRequested;
    public event EventHandler<ExportRequestedEventArgs> ExportRequested;
    
    // 方法
    private void OnGridDataChanged(ScheduleGridData newData)
    {
        // 重建表格结构
        BuildGridStructure(newData);
        
        // 应用虚拟化
        ApplyVirtualization();
    }
    
    private void BuildGridStructure(ScheduleGridData data)
    {
        // 1. 创建列头
        CreateColumnHeaders(data.Columns);
        
        // 2. 创建行头
        CreateRowHeaders(data.Rows);
        
        // 3. 创建单元格
        CreateCells(data.Cells);
    }
}
```

### 虚拟化策略

为了支持大规模数据（如 30 天 × 12 时段 × 20 哨位 = 7200 个单元格），采用以下虚拟化策略：

1. **行虚拟化**：仅渲染可见行及其上下各 5 行（缓冲区）
2. **列虚拟化**：仅渲染可见列及其左右各 2 列（缓冲区）
3. **按需加载**：滚动时动态加载和卸载单元格
4. **回收机制**：重用不可见的单元格控件

```csharp
public class VirtualizedGridPanel : Panel
{
    private readonly Dictionary<(int row, int col), FrameworkElement> _visibleCells = new();
    private readonly Queue<FrameworkElement> _recycledCells = new();
    
    protected override Size MeasureOverride(Size availableSize)
    {
        // 计算可见区域
        var visibleRect = CalculateVisibleRect(availableSize);
        
        // 确定需要渲染的行列范围
        var (startRow, endRow) = GetVisibleRowRange(visibleRect);
        var (startCol, endCol) = GetVisibleColumnRange(visibleRect);
        
        // 更新可见单元格
        UpdateVisibleCells(startRow, endRow, startCol, endCol);
        
        return base.MeasureOverride(availableSize);
    }
    
    private void UpdateVisibleCells(int startRow, int endRow, int startCol, int endCol)
    {
        // 移除不可见的单元格
        var cellsToRemove = _visibleCells
            .Where(kvp => kvp.Key.row < startRow || kvp.Key.row > endRow ||
                         kvp.Key.col < startCol || kvp.Key.col > endCol)
            .ToList();
        
        foreach (var kvp in cellsToRemove)
        {
            _visibleCells.Remove(kvp.Key);
            _recycledCells.Enqueue(kvp.Value);
            Children.Remove(kvp.Value);
        }
        
        // 添加新的可见单元格
        for (int row = startRow; row <= endRow; row++)
        {
            for (int col = startCol; col <= endCol; col++)
            {
                if (!_visibleCells.ContainsKey((row, col)))
                {
                    var cell = GetOrCreateCell(row, col);
                    _visibleCells[(row, col)] = cell;
                    Children.Add(cell);
                }
            }
        }
    }
    
    private FrameworkElement GetOrCreateCell(int row, int col)
    {
        // 尝试从回收池获取
        if (_recycledCells.Count > 0)
        {
            var cell = _recycledCells.Dequeue();
            UpdateCellData(cell, row, col);
            return cell;
        }
        
        // 创建新单元格
        return CreateNewCell(row, col);
    }
}
```

### 全屏模式实现

```csharp
public class ScheduleGridFullScreenView : UserControl
{
    public ScheduleGridFullScreenView()
    {
        InitializeComponent();
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // 关闭全屏对话框
        var dialog = this.FindParent<ContentDialog>();
        dialog?.Hide();
    }
}
```

**全屏样式**：
```xaml
<Style x:Key="FullScreenDialogStyle" TargetType="ContentDialog">
    <Setter Property="Width" Value="{Binding ActualWidth, 
            RelativeSource={RelativeSource Mode=FindAncestor, 
            AncestorType=Window}}"/>
    <Setter Property="Height" Value="{Binding ActualHeight, 
            RelativeSource={RelativeSource Mode=FindAncestor, 
            AncestorType=Window}}"/>
    <Setter Property="Margin" Value="0"/>
    <Setter Property="Padding" Value="24"/>
</Style>
```

### 导出接口实现（预留）

```csharp
public class ScheduleGridExporter : IScheduleGridExporter
{
    public async Task<byte[]> ExportAsync(
        ScheduleGridData gridData, 
        string format, 
        ExportOptions? options = null)
    {
        // 当前版本抛出未实现异常
        throw new NotImplementedException(
            $"表格导出功能正在开发中。计划支持的格式：{string.Join(", ", GetSupportedFormats())}");
    }
    
    public List<string> GetSupportedFormats()
    {
        // 预留支持的格式列表
        return new List<string> { "excel", "csv", "pdf" };
    }
    
    public bool IsFormatSupported(string format)
    {
        return GetSupportedFormats().Contains(format.ToLower());
    }
}
```

### 单元格样式

```xaml
<Style x:Key="ScheduleGridCellStyle" TargetType="Border">
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="BorderBrush" Value="{ThemeResource CardStrokeColorDefaultBrush}"/>
    <Setter Property="Padding" Value="8,4"/>
    <Setter Property="MinWidth" Value="120"/>
    <Setter Property="MinHeight" Value="40"/>
    <Setter Property="Background" Value="{ThemeResource CardBackgroundFillColorDefaultBrush}"/>
</Style>

<Style x:Key="ManualAssignmentCellStyle" 
       TargetType="Border" 
       BasedOn="{StaticResource ScheduleGridCellStyle}">
    <Setter Property="BorderBrush" Value="{ThemeResource SystemAccentColor}"/>
    <Setter Property="BorderThickness" Value="2"/>
</Style>

<Style x:Key="ConflictCellStyle" 
       TargetType="Border" 
       BasedOn="{StaticResource ScheduleGridCellStyle}">
    <Setter Property="BorderBrush" Value="{ThemeResource SystemErrorTextColor}"/>
    <Setter Property="BorderThickness" Value="2"/>
    <Setter Property="Background" Value="{ThemeResource SystemErrorBackgroundColor}"/>
</Style>
```

### 性能指标

- **初始渲染时间**：< 500ms（1000 个单元格）
- **滚动帧率**：≥ 60 FPS
- **内存占用**：< 50MB（10000 个单元格）
- **虚拟化效率**：仅渲染可见区域 + 10% 缓冲区
