# 实现计划

## 任务列表

- [x] 1. 创建进度报告数据模型和枚举





  - 创建 `SchedulingProgressReport` 类，包含进度百分比、当前阶段、统计信息等属性
  - 创建 `SchedulingStage` 枚举，定义算法执行的各个阶段
  - 创建 `SchedulingResult` 类及相关统计模型（`SchedulingStatistics`、`PersonnelWorkload`、`PositionCoverage`、`SoftConstraintScores`、`ConflictInfo`）
  - 将模型文件放置在 `DTOs/` 目录下
  - _需求: 1.1, 1.2, 1.3, 2.1, 2.2, 3.1, 3.2, 3.3_


- [x] 2. 创建排班表格数据模型和导出接口


  - 创建 `ScheduleGridData` 类，包含表格的列、行和单元格数据
  - 创建 `ScheduleGridColumn`、`ScheduleGridRow`、`ScheduleGridCell` 类
  - 创建 `IScheduleGridExporter` 接口，定义导出方法签名
  - 创建 `ExportOptions` 类，定义导出选项
  - 创建 `ScheduleGridExporter` 类，实现接口但抛出 `NotImplementedException`（预留未来实现）
  - 将接口和实现放置在 `Services/Interfaces/` 和 `Services/` 目录下
  - _需求: 5.1, 5.2, 5.3, 5.4, 5.5_

- [-] 3. 扩展 GreedyScheduler 支持进度报告





  - 在 `GreedyScheduler.ExecuteAsync` 方法中添加 `IProgress<SchedulingProgressReport>` 参数
  - 在算法的关键节点调用 `progress.Report()` 报告进度
  - 实现节流机制，避免过度频繁的进度更新（最小间隔 100ms）
  - 报告节点包括：初始化、加载数据、构建上下文、初始化张量、应用约束、贪心分配（每完成 10%）、完成
  - 计算并报告进度百分比、已完成分配数、剩余时段数等
  - _需求: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5_

- [-] 4. 扩展 SchedulingService 支持进度报告和取消

- [x] 4.1 修改 ExecuteSchedulingAsync 方法签名




- [x] 4.1 修改 ExecuteSchedulingAsync 方法签名




  - 在 `ISchedulingService` 接口中添加新的方法重载
  - 方法签名：`Task<SchedulingResult> ExecuteSchedulingAsync(SchedulingRequestDto request, IProgress<SchedulingProgressReport>? progress = null, CancellationToken cancellationToken = default)`
  - 保留原有方法签名以保持向后兼容，内部调用新方法
  - 在 `SchedulingService` 类中实现新方法
  - _需求: 1.1, 1.2, 1.3, 7.1, 7.2, 7.3_


- [x] 4.2 实现进度报告转发逻辑



  - 在 `ExecuteSchedulingAsync` 中创建内部进度报告处理器
  - 将 `IProgress<SchedulingProgressReport>` 传递给 `GreedyScheduler.ExecuteAsync`
  - 在关键步骤（验证、加载数据、构建上下文等）报告进度
  - 确保进度报告在UI线程上执行（如果需要）
  - _需求: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3_


- [x] 4.3 实现取消令牌支持

- [x] 4.3 实现取消令牌支持




  - 在 `ExecuteSchedulingAsync` 的各个阶段检查 `cancellationToken.IsCancellationRequested`
  - 在验证阶段、数据加载阶段、算法执行阶段添加取消检查点
  - 将 `cancellationToken` 传递给 `GreedyScheduler.ExecuteAsync`
  - 捕获 `OperationCanceledException`，返回取消状态的 `SchedulingResult`
  - _需求: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 4.4 实现 BuildSchedulingResult 方法

  - 创建 `BuildSchedulingResult` 方法，接收 `Schedule` 和执行时长
  - 计算统计信息：总分配数、人员工作量、哨位覆盖率、软约束评分
  - 实现 `CalculatePersonnelWorkloads` 辅助方法，统计每个人员的班次数、日哨数、夜哨数
  - 实现 `CalculatePositionCoverages` 辅助方法，计算每个哨位的覆盖率
  - 实现 `CalculateSoftConstraintScores` 辅助方法，计算软约束得分
  - 识别并收集冲突信息（未分配的时段、约束违反等）
  - 返回包含完整统计信息的 `SchedulingResult`
  - _需求: 3.1, 3.2, 3.3, 3.4, 3.5_



- [ ] 4.5 实现 BuildScheduleGridData 方法

  - 创建 `BuildScheduleGridData` 方法，接收 `ScheduleDto`
  - 构建列数据：遍历 `PositionIds`，创建 `ScheduleGridColumn` 列表
  - 构建行数据：遍历日期范围和时段，创建 `ScheduleGridRow` 列表
  - 构建单元格数据：遍历 `Shifts`，创建 `ScheduleGridCell` 字典，键为 `"row_col"`
  - 标记手动指定的单元格（通过查询 `ManualAssignments`）
  - 标记有冲突的单元格（通过 `Conflicts` 列表）
  - 返回完整的 `ScheduleGridData` 对象



  - _需求: 5.1, 5.2, 5.3, 5.4, 5.5_


- [ ] 4.6 实现错误处理和失败结果
  - 在 `ExecuteSchedulingAsync` 中添加 try-catch 块
  - 捕获 `ArgumentException`，返回参数错误的 `SchedulingResult`
  - 捕获 `InvalidOperationException`，返回业务逻辑错误的 `SchedulingResult`


  - 捕获 `OperationCanceledException`，返回取消状态的 `SchedulingResult`
  - 捕获通用 `Exception`，返回系统错误的 `SchedulingResult`
  - 在失败结果中包含错误消息、部分完成的分配数、冲突详情
  - _需求: 4.1, 4.2, 4.3, 4.4, 4.5_

- [ ] 5. 创建 SchedulingProgressViewModel 基础结构
- [x] 5.1 创建 ViewModel 类和基础属性












  - 创建 `SchedulingProgressViewModel` 类，继承 `ObservableObject`
  - 注入依赖：`ISchedulingService`、`NavigationService`、`IScheduleGridExporter`
  - 添加进度状态属性：`ProgressPercentage`、`CurrentStage`、`StageDescription`、`IsExecuting`、`IsCompleted`、`IsFailed`、`IsCancelled`
  - 添加统计信息属性：`CompletedAssignments`、`TotalSlotsToAssign`、`CurrentPositionName`、`CurrentTimeSlot`、`ElapsedTime`
  - 添加私有字段：`_cancellationTokenSource`、`_startTime`
  - 将 ViewModel 放置在 `ViewModels/Scheduling/` 目录下
  - _需求: 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 2.4, 2.5_

- [ ] 5.2 实现排班执行和进度更新逻辑
  - 实现 `StartSchedulingCommand`，接收 `SchedulingRequestDto` 参数
  - 在命令中创建 `IProgress<SchedulingProgressReport>` 实例
  - 调用 `SchedulingService.ExecuteSchedulingAsync`，传递进度报告和取消令牌
  - 实现进度回调方法 `OnProgressReported`，更新UI绑定属性
  - 计算并更新 `ElapsedTime`（使用 `DispatcherTimer` 或 `Stopwatch`）


  - 处理执行完成后的逻辑，设置 `IsCompleted` 或 `IsFailed`
  - _需求: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5_
-

- [x] 5.3 实现结果数据处理





- [ ] 5.3 实现结果数据处理
  - 添加结果数据属性：`Result`、`PersonnelWorkloads`、`PositionCoverages`、`Conflicts`、`GridData`


  - 实现 `ProcessSchedulingResult` 方法，将 `SchedulingResult` 转换为UI可绑定的集合
  - 调用 `SchedulingService.BuildScheduleGridData` 构建表格数据
  - 填充 `PersonnelWorkloads`、`PositionCoverages`、`Conflicts` 集合
  - _需求: 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2_

- [ ] 5.4 实现取消和操作命令

  - 实现 `CancelSchedulingCommand`，调用 `_cancellationTokenSource.Cancel()`
  - 实现 `SaveScheduleCommand`，调用 `SchedulingService` 保存排班结果
  - 实现 `DiscardScheduleCommand`，显示确认对话框后清除结果
  - 实现 `ViewDetailedResultCommand`，导航到 `ScheduleResultPage`
  - 实现 `ReturnToConfigCommand`，导航回 `CreateSchedulingPage`
  - _需求: 6.1, 6.2, 6.3, 6.4, 6.5, 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 5.5 实现表格相关命令
  - 添加属性：`IsGridFullScreen`
  - 实现 `ToggleGridFullScreenCommand`，切换全屏状态
  - 实现 `ExportGridCommand`，显示"功能开发中"对话框（预留接口）
  - 实现 `ShowGridFullScreenAsync` 方法，创建并显示全屏对话框
  - _需求: 5.1, 5.2, 5.3_

- [ ] 6. 创建 ScheduleGridControl 自定义控件
- [ ] 6.1 创建控件基础结构


  - 创建 `ScheduleGridControl` 类，继承 `Control`
  - 定义 `GridData` 依赖属性，类型为 `ScheduleGridData`
  - 定义 `OnGridDataChanged` 回调方法
  - 创建控件模板 XAML 文件 `ScheduleGridControl.xaml`
  - 定义基本布局：工具栏区域、表头区域、表体区域
  - 将控件放置在 `Controls/` 目录下
  - _需求: 5.1, 5.2_

- [ ] 6.2 实现表格结构构建


  - 实现 `BuildGridStructure` 方法，根据 `GridData` 构建表格
  - 实现 `CreateColumnHeaders` 方法，创建哨位列头
  - 实现 `CreateRowHeaders` 方法，创建日期+时段行头
  - 实现 `CreateCells` 方法，创建人员单元格
  - 使用 `Grid` 或自定义 `Panel` 布局单元格
  - _需求: 5.1, 5.2_


- [ ] 6.3 实现虚拟化渲染

  - 创建 `VirtualizedGridPanel` 类，继承 `Panel`
  - 重写 `MeasureOverride` 和 `ArrangeOverride` 方法
  - 实现 `CalculateVisibleRect` 方法，计算可见区域
  - 实现 `GetVisibleRowRange` 和 `GetVisibleColumnRange` 方法
  - 实现 `UpdateVisibleCells` 方法，动态加载和卸载单元格
  - 实现单元格回收机制，重用不可见的单元格控件
  - _需求: 5.1, 5.2_


- [ ] 6.4 实现单元格样式和交互

  - 创建单元格控件 `ScheduleGridCell`，继承 `Border`
  - 实现单元格数据绑定：`PersonnelName`、`IsManualAssignment`、`HasConflict`
  - 应用样式：普通单元格、手动指定单元格（蓝色边框）、冲突单元格（红色边框）
  - 添加单元格悬停 `ToolTip`，显示人员详细信息（姓名、技能、工作量）
  - 实现 `CellClicked` 事件
  - _需求: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 6.5 添加工具栏按钮


  - 在控件模板中添加工具栏 `StackPanel`
  - 添加导出按钮，设置 `IsEnabled="False"`，ToolTip 显示"功能开发中"
  - 添加全屏按钮，使用 Segoe MDL2 Assets 图标（&#xE740;）
  - 定义 `FullScreenRequested` 和 `ExportRequested` 事件
  - 绑定按钮点击事件到控件事件
  - _需求: 5.1, 5.2, 5.3_

- [ ] 7. 创建 SchedulingProgressPage 视图
- [ ] 7.1 创建页面基础结构和布局
  - 创建 `SchedulingProgressPage.xaml` 和 `SchedulingProgressPage.xaml.cs`
  - 在 Code-behind 中注入 `SchedulingProgressViewModel`
  - 实现 `OnNavigatedTo` 方法，接收 `SchedulingRequestDto` 参数并启动排班
  - 定义页面根布局：使用 `Grid` 分为顶部、进度区、主内容区
  - 将页面放置在 `Views/Scheduling/` 目录下
  - _需求: 1.1_

- [ ] 7.2 实现顶部标题栏和进度指示区
  - 添加顶部标题栏，显示排班标题和状态图标
  - 添加进度指示区，包含 `ProgressBar`、阶段名称、阶段描述
  - 绑定 `ProgressBar.Value` 到 `ViewModel.ProgressPercentage`
  - 绑定阶段信息到 `ViewModel.CurrentStage` 和 `ViewModel.StageDescription`
  - 显示已完成/总数统计，绑定到 `ViewModel.CompletedAssignments` 和 `ViewModel.TotalSlotsToAssign`
  - _需求: 1.1, 1.2, 1.3, 1.4, 1.5_

- [ ] 7.3 实现左侧实时信息区
  - 创建左侧列，宽度约 30%，使用 `ScrollViewer` 支持滚动
  - 添加当前处理信息卡片，显示哨位名称、时段、日期
  - 绑定到 `ViewModel.CurrentPositionName`、`ViewModel.CurrentTimeSlot`
  - 添加累计执行时间显示，绑定到 `ViewModel.ElapsedTime`
  - 添加阶段历史列表，使用 `ListView` 显示已完成的阶段（带状态图标）
  - 添加警告信息列表，使用 `ListView` 显示警告消息
  - _需求: 2.1, 2.2, 2.3, 2.4, 2.5_

- [ ] 7.4 实现右侧结果展示区
  - 创建右侧列，宽度约 70%，分为上下两部分
  - 上部使用 `ScrollViewer` 支持滚动
  - 添加 `ScheduleGridControl`，绑定 `GridData` 到 `ViewModel.GridData`
  - 添加成功/失败状态卡片，根据 `ViewModel.IsCompleted` 和 `ViewModel.IsFailed` 显示
  - 添加统计信息卡片，显示总分配数、平均工作量、覆盖率
  - 添加人员工作量表格，使用 `ListView` 绑定到 `ViewModel.PersonnelWorkloads`
  - 添加哨位覆盖率表格，使用 `ListView` 绑定到 `ViewModel.PositionCoverages`
  - 添加冲突列表，使用 `ListView` 绑定到 `ViewModel.Conflicts`
  - _需求: 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 7.5 实现操作按钮区
  - 在右侧下部添加操作按钮区，固定高度
  - 添加"取消排班"按钮，绑定到 `ViewModel.CancelSchedulingCommand`
  - 根据 `ViewModel.IsExecuting` 控制取消按钮可见性
  - 添加"保存排班"按钮，绑定到 `ViewModel.SaveScheduleCommand`
  - 添加"放弃排班"按钮，绑定到 `ViewModel.DiscardScheduleCommand`
  - 添加"查看详细结果"按钮，绑定到 `ViewModel.ViewDetailedResultCommand`
  - 根据 `ViewModel.IsCompleted` 控制成功按钮可见性
  - 添加"返回修改"按钮，绑定到 `ViewModel.ReturnToConfigCommand`
  - 根据 `ViewModel.IsFailed` 控制失败按钮可见性
  - _需求: 6.1, 6.2, 6.3, 6.4, 6.5, 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 8. 实现全屏表格对话框
  - 创建 `ScheduleGridFullScreenView.xaml` 和 `ScheduleGridFullScreenView.xaml.cs`
  - 在全屏视图中嵌入 `ScheduleGridControl`
  - 添加关闭按钮，点击后关闭对话框
  - 在 `SchedulingProgressViewModel` 中实现 `ShowGridFullScreenAsync` 方法
  - 创建全屏对话框样式 `FullScreenDialogStyle`，设置宽高为窗口大小
  - 将视图放置在 `Views/Scheduling/` 目录下
  - _需求: 5.1, 5.2, 5.3_

- [ ] 9. 修改 CreateSchedulingPage 导航逻辑
  - 修改 `CreateSchedulingPage.xaml.cs` 或 `SchedulingViewModel`
  - 在"开始排班"按钮点击时，导航到 `SchedulingProgressPage`，传递 `SchedulingRequestDto` 参数
  - 确保导航参数正确传递
  - _需求: 1.1_

- [ ] 10. 注册依赖注入和导航路由
  - 在 `Extensions/ServiceCollectionExtensions.cs` 中注册 `SchedulingProgressViewModel`
  - 在 `Extensions/ServiceCollectionExtensions.cs` 中注册 `IScheduleGridExporter` 和 `ScheduleGridExporter`
  - 在导航服务中注册 `SchedulingProgressPage` 路由
  - 确保所有依赖项正确注入
  - _需求: 所有需求_

- [ ] 11. 实现错误处理和用户反馈
  - 在 `SchedulingProgressViewModel` 中添加错误处理逻辑
  - 捕获 `ArgumentException`、`InvalidOperationException`、`OperationCanceledException` 等异常
  - 根据异常类型显示相应的错误消息
  - 在失败时显示冲突详情和建议
  - 在取消时显示"排班已取消"状态
  - 使用 `DialogService` 显示错误对话框
  - _需求: 4.1, 4.2, 4.3, 4.4, 4.5, 7.4, 7.5_

- [ ] 12. 添加样式和主题支持
  - 创建 `SchedulingProgressResources.xaml` 资源字典
  - 定义表格单元格样式：`ScheduleGridCellStyle`、`ManualAssignmentCellStyle`、`ConflictCellStyle`
  - 定义卡片样式、按钮样式、进度条样式
  - 确保样式支持浅色和深色主题
  - 确保高对比度主题下的可访问性
  - 将资源字典放置在 `Themes/` 目录下
  - _需求: 所有需求_

- [ ] 13. 实现可访问性支持
  - 为所有控件设置 `AutomationProperties.Name`
  - 为进度条设置 `AutomationProperties.LiveSetting="Polite"`
  - 为错误消息设置 `AutomationProperties.LiveSetting="Assertive"`
  - 确保所有按钮支持键盘导航（Tab 键）
  - 实现快捷键：Esc 取消、Enter 确认、Ctrl+S 保存
  - 测试屏幕阅读器支持
  - _需求: 所有需求_

- [ ]* 14. 编写单元测试
  - 为 `SchedulingProgressViewModel` 编写单元测试
  - 测试进度更新逻辑
  - 测试命令执行逻辑
  - 测试状态转换逻辑
  - 测试取消操作逻辑
  - 测试错误处理逻辑
  - 使用 xUnit 和 Moq 框架
  - 将测试文件放置在 `Tests/ViewModels/` 目录下
  - _需求: 所有需求_

- [ ]* 15. 编写集成测试
  - 测试端到端排班流程（小规模数据集：5人员 × 3哨位 × 3天）
  - 测试取消操作在不同阶段的行为
  - 测试无可行解场景
  - 测试约束冲突场景
  - 测试数据不完整场景
  - 验证资源清理和状态恢复
  - 将测试文件放置在 `Tests/Integration/` 目录下
  - _需求: 所有需求_

- [ ]* 16. 性能优化和测试
  - 测试大规模数据集（50人员 × 20哨位 × 30天）的性能
  - 优化进度报告频率，确保UI流畅（≥ 60 FPS）
  - 优化表格虚拟化，确保初始渲染时间 < 500ms
  - 测试内存占用，确保 < 50MB（10000 个单元格）
  - 使用性能分析工具识别瓶颈
  - _需求: 所有需求_

- [ ]* 17. 文档和代码注释
  - 为所有公共类和方法添加 XML 文档注释
  - 更新 README.md，添加新功能说明
  - 创建用户指南，说明如何使用排班进度页面
  - 添加代码示例和截图
  - _需求: 所有需求_
