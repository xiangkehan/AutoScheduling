# 实现计划

## 任务列表

- [x] 1. 创建新增的数据模型
  - 创建 `PositionScheduleData` 类，包含哨位ID、名称、周数据列表
  - 创建 `WeekData` 类，包含周次、开始结束日期、单元格数据
  - 创建 `PositionScheduleCell` 类，包含时段、星期、人员信息
  - 创建 `PersonnelScheduleData` 类，包含人员ID、名称、工作量、班次列表
  - 创建 `PersonnelShift` 类，包含日期、时段、哨位信息
  - 创建 `ShiftListItem` 类，包含班次的完整信息
  - 将模型文件放置在 `DTOs/` 目录下
  - _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8_

- [x] 2. 扩展 ViewMode 枚举
  - 将 `ScheduleViewMode` 枚举重命名为 `ViewMode` 并移动到 DTOs 目录
  - 在 `ViewMode` 枚举中添加 `ByPosition` 选项
  - 更新 ScheduleResultViewModel 和 ScheduleResultPage 中的引用
  - _需求: 2.1_

- [x] 3. 扩展 ScheduleResultViewModel
- [x] 3.1 添加新的属性
  - 添加 `PositionSchedules` 集合属性（Position View 数据）
  - 添加 `SelectedPositionSchedule` 属性（当前选中的哨位）
  - 添加 `CurrentWeekIndex` 属性（当前周次索引）
  - 添加 `PersonnelSchedules` 集合属性（Personnel View 数据）
  - 添加 `SelectedPersonnelSchedule` 属性（当前选中的人员）
  - 添加 `ShiftList` 集合属性（List View 数据）
  - 添加 `ListSortBy` 和 `ListGroupBy` 属性（列表排序和分组）
  - 添加筛选相关属性：`FilterStartDate`、`FilterEndDate`、`SelectedPositionIds`、`PersonnelSearchText`
  - 添加 `HasUnsavedChanges` 属性（是否有未保存的更改）
  - 添加统计数据属性：`PersonnelWorkloads`、`PositionCoverages`、`Statistics`
  - _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 6.3, 6.4, 6.5, 9.5_

- [x] 3.2 添加新的命令
  - 添加 `ApplyFiltersCommand`（应用筛选）
  - 添加 `ResetFiltersCommand`（重置筛选）
  - 添加 `SelectPositionCommand`（选择哨位）
  - 添加 `SelectPersonnelCommand`（选择人员）
  - 添加 `ChangeWeekCommand`（切换周次）
  - 添加 `ViewShiftDetailsCommand`（查看班次详情）
  - 添加 `EditShiftCommand`（编辑班次）
  - 添加 `SaveChangesCommand`（保存更改）
  - 添加 `DiscardChangesCommand`（撤销更改）
  - 添加 `LocateConflictCommand`（定位冲突）
  - 添加 `IgnoreConflictCommand`（忽略冲突）
  - 添加 `FixConflictCommand`（修复冲突）
  - 添加 `ToggleFullScreenCommand`（切换全屏）
  - 添加 `CompareSchedulesCommand`（比较排班）
  - _需求: 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 5.5, 7.1, 7.2, 7.3, 7.4, 7.5, 9.1, 9.2, 9.3, 9.4, 9.5, 10.1, 10.2, 10.3, 10.4, 10.5, 11.1, 11.2, 11.3, 11.4, 11.5_

- [x] 3.3 实现数据构建方法
  - 实现 `BuildPositionScheduleDataAsync` 方法，将 ScheduleDto 转换为 Position View 数据
  - 实现 `BuildPersonnelScheduleDataAsync` 方法，将 ScheduleDto 转换为 Personnel View 数据
  - 实现 `BuildShiftListDataAsync` 方法，将 ScheduleDto 转换为 List View 数据
  - 实现辅助方法：`GetTimeSlotDescription`、`IsNightShift`、`GetShiftRemarks`
  - 实现人员和哨位名称缓存机制
  - _需求: 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 3.4 实现视图模式切换逻辑
  - 实现 `OnViewModeChangedAsync` 方法，根据新模式加载对应数据
  - 在 Grid View 模式下，确保 GridData 已构建
  - 在 Position View 模式下，构建 PositionSchedules 并选择第一个哨位
  - 在 Personnel View 模式下，构建 PersonnelSchedules 并选择第一个人员
  - 在 List View 模式下，构建 ShiftList
  - 处理视图切换错误，恢复到之前的模式
  - _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8_

- [x] 3.5 实现筛选功能
  - 实现 `ApplyFiltersAsync` 方法，根据筛选条件重新构建数据
  - 实现 `ResetFiltersAsync` 方法，清除筛选条件并重新加载数据
  - 筛选逻辑应用于所有视图模式
  - 筛选条件包括：日期范围、哨位、人员搜索
  - _需求: 5.1, 5.2, 5.3, 5.4, 5.5_

- [x] 3.6 实现编辑功能
  - 实现 `EditShiftAssignmentAsync` 方法，打开修改分配对话框
  - 实现 `UpdateShiftAssignmentAsync` 方法，更新班次分配
  - 实现 `SaveChangesAsync` 方法，将更改保存到数据库
  - 实现 `DiscardChangesAsync` 方法，撤销所有未保存的更改
  - 修改后自动重新检测冲突
  - 修改后标记 `HasUnsavedChanges` 为 true
  - _需求: 9.1, 9.2, 9.3, 9.4, 9.5_

- [x] 3.7 实现冲突处理功能
  - 实现 `DetectConflictsAsync` 方法，检测所有冲突
  - 实现 `LocateConflictAsync` 方法，定位并高亮冲突单元格
  - 实现 `IgnoreConflictAsync` 方法，标记冲突为已忽略
  - 实现 `FixConflictAsync` 方法，打开修复对话框
  - 冲突检测应在后台异步执行
  - _需求: 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 3.8 实现统计数据构建方法
  - 实现 `BuildPersonnelWorkloadsAsync` 方法，计算人员工作量统计
  - 实现 `BuildPositionCoveragesAsync` 方法，计算哨位覆盖率统计
  - 实现 `BuildStatisticsAsync` 方法，构建整体统计信息
  - _需求: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 3.9 实现其他命令
  - 实现 `SelectPositionCommand`，切换选中的哨位
  - 实现 `SelectPersonnelCommand`，切换选中的人员
  - 实现 `ChangeWeekCommand`，切换周次
  - 实现 `ViewShiftDetailsCommand`，显示班次详情对话框
  - 实现 `ToggleFullScreenCommand`，切换全屏模式
  - 实现 `CompareSchedulesCommand`，打开排班比较视图
  - _需求: 3.1, 3.2, 3.3, 3.4, 3.5, 10.1, 10.2, 10.3, 10.4, 10.5, 11.1, 11.2, 11.3, 11.4, 11.5_

- [x] 4. 创建 PositionScheduleControl 自定义控件
- [x] 4.1 创建控件基础结构
  - 创建 `PositionScheduleControl.xaml` 和 `PositionScheduleControl.xaml.cs`
  - 定义 `ScheduleData` 依赖属性，类型为 `PositionScheduleData`
  - 定义 `OnScheduleDataChanged` 回调方法
  - 将控件放置在 `Controls/` 目录下
  - _需求: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_

- [x] 4.2 实现表格结构构建
  - 实现 `BuildWeeklyGrid` 方法，构建周视图表格
  - 创建行头：12个时段（每个时段2小时）
  - 创建列头：7天（周一到周日）
  - 创建单元格：显示人员姓名
  - 使用 Grid 布局
  - _需求: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_

- [x] 4.3 实现单元格样式和交互
  - 创建单元格控件，显示人员姓名
  - 应用样式：普通单元格、手动指定单元格、冲突单元格
  - 添加单元格悬停 Tooltip，显示详细信息
  - 实现 `CellClicked` 事件
  - 实现双击编辑功能
  - _需求: 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 4.4 实现周次切换功能
  - 添加周次选择器（ComboBox）
  - 实现 `WeekChanged` 事件
  - 切换周次时更新表格显示
  - _需求: 3.4, 3.5_

- [x] 4.5 添加工具栏按钮
  - 添加导出按钮，支持导出当前哨位的排班表
  - 添加打印按钮
  - 添加全屏按钮
  - _需求: 3.7, 8.1, 8.2, 8.3, 8.4, 8.5, 10.1, 10.2, 10.3, 10.4, 10.5_

- [ ] 5. 创建 PersonnelScheduleControl 自定义控件
- [x] 5.1 创建控件基础结构
  - 创建 `PersonnelScheduleControl.xaml` 和 `PersonnelScheduleControl.xaml.cs`
  - 定义 `ScheduleData` 依赖属性，类型为 `PersonnelScheduleData`
  - 定义 `OnScheduleDataChanged` 回调方法
  - 将控件放置在 `Controls/` 目录下
  - _需求: 2.4_

- [x] 5.2 实现工作量统计卡片
  - 显示总班次数、日哨数、夜哨数、工作时长
  - 使用卡片样式布局
  - 添加可视化进度条
  - _需求: 2.4, 6.2_

- [x] 5.3 实现班次列表
  - 使用 ListView 显示所有班次
  - 每行显示日期、时段、哨位、备注
  - 支持按日期排序
  - 标识手动指定和夜哨
  - _需求: 2.4_

- [x] 5.4 实现日历视图
  - 创建月历控件，显示有班次的日期
  - 使用标记（如圆点）表示有班次的日期
  - 点击日期可查看当天的班次详情
  - _需求: 2.4_

- [ ] 6. 更新 ScheduleResultPage 视图
- [x] 6.1 更新顶部工具栏
  - 添加第四个视图模式单选按钮：Position
  - 更新视图模式切换逻辑
  - 添加全屏按钮
  - 添加比较按钮
  - _需求: 2.1, 10.1, 10.2, 10.3, 10.4, 10.5, 11.1, 11.2, 11.3, 11.4, 11.5_

- [x] 6.2 添加筛选工具栏
  - 添加日期范围选择器（DatePicker）
  - 添加哨位多选下拉框（ComboBox with CheckBox）
  - 添加人员搜索框（AutoSuggestBox）
  - 添加"应用"和"重置"按钮
  - 绑定到 ViewModel 的筛选属性
  - _需求: 5.1, 5.2, 5.3, 5.4, 5.5_

- [x] 6.3 更新主内容区
  - 保留现有的 Grid View（ScheduleGridControl）
  - 添加 Position View（PositionScheduleControl）
  - 添加 Personnel View（PersonnelScheduleControl）
  - 更新 List View，使用 ListView 显示 ShiftList
  - 根据 CurrentViewMode 切换显示的视图
  - _需求: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_

- [x] 6.4 更新冲突面板
  - 更新冲突列表项模板，显示类型、描述、相关信息
  - 添加"定位"、"忽略"、"修复"按钮
  - 绑定到 ViewModel 的冲突命令
  - _需求: 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 6.5 添加统计信息面板
  - 创建可折叠的统计信息面板
  - 显示总分配数、参与人员数、参与哨位数
  - 显示人员工作量统计表，支持排序
  - 显示哨位覆盖率统计表，支持排序
  - 显示软约束评分
  - 使用进度条可视化数据
  - _需求: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 6.6 更新底部操作栏
  - 添加"保存更改"按钮，绑定到 SaveChangesCommand
  - 添加"撤销更改"按钮，绑定到 DiscardChangesCommand
  - 根据 HasUnsavedChanges 显示未保存提示
  - _需求: 9.5_

- [x] 7. 创建对话框视图
- [x] 7.1 创建班次详情对话框
  - 创建 `ShiftDetailsDialog.xaml` 和代码文件
  - 显示基本信息：日期、时段、哨位、人员
  - 显示人员信息：技能、工作量、上下班次
  - 显示约束评估结果
  - 提供"修改分配"、"取消分配"、"查看人员详情"按钮
  - _需求: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 7.2 创建修改分配对话框
  - 创建 `EditShiftAssignmentDialog.xaml` 和代码文件
  - 显示当前分配信息
  - 显示可用人员列表，支持搜索
  - 标注技能匹配情况、可用性、工作量
  - 推荐合适的人员
  - 提供"确定"和"取消"按钮
  - _需求: 9.1, 9.2, 9.3, 9.4_

- [x] 7.3 创建导出格式选择对话框
  - 创建 `ExportFormatDialog.xaml` 和代码文件
  - 显示支持的导出格式（Excel、CSV、PDF）
  - 提供导出选项（包含空单元格、高亮冲突等）
  - 提供"确定"和"取消"按钮
  - _需求: 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 7.4 创建排班比较对话框
  - 创建 `CompareSchedulesDialog.xaml` 和代码文件
  - 显示历史排班列表，允许选择一个进行比较
  - 提供"确定"和"取消"按钮
  - _需求: 11.1, 11.2, 11.3_

- [x] 7.5 创建排班比较视图
  - 创建 `ScheduleComparisonView.xaml` 和代码文件
  - 并排显示两个排班方案
  - 高亮显示差异的单元格
  - 显示两个方案的统计信息对比
  - 提供"关闭"按钮
  - _需求: 11.3, 11.4, 11.5_

- [x] 8. 实现导出功能
- [x] 8.1 扩展 IScheduleGridExporter 接口
  - 添加 `ExportPositionScheduleAsync` 方法，导出单个哨位的排班表
  - 添加 `ExportPersonnelScheduleAsync` 方法，导出单个人员的排班表
  - 添加 `ExportShiftListAsync` 方法，导出班次列表
  - _需求: 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 8.2 实现 Excel 导出
  - 实现 Grid View 的 Excel 导出
  - 实现 Position View 的 Excel 导出（每个哨位一个工作表）
  - 实现 Personnel View 的 Excel 导出（每个人员一个工作表）
  - 实现 List View 的 Excel 导出
  - 应用格式化样式（表头、边框、颜色）
  - 高亮手动指定和冲突的单元格
  - _需求: 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 8.3 实现 CSV 导出
  - 实现 Grid View 的 CSV 导出
  - 实现 List View 的 CSV 导出
  - 使用 UTF-8 编码
  - _需求: 8.1, 8.2, 8.3_

- [x] 8.4 实现 PDF 导出（预留）
  - 定义 PDF 导出接口
  - 抛出 NotImplementedException
  - 显示"功能开发中"提示
  - _需求: 8.1, 8.2, 8.5_

- [x] 9. 实现全屏功能
- [x] 9.1 创建全屏视图
  - 创建 `ScheduleGridFullScreenView.xaml` 和代码文件
  - 支持 Grid View 全屏
  - 支持 Position View 全屏
  - 添加关闭按钮
  - 支持 Esc 键退出
  - _需求: 10.1, 10.2, 10.3, 10.4, 10.5_

- [x] 9.2 实现全屏切换逻辑
  - 在 ViewModel 中实现 `ToggleFullScreenAsync` 方法
  - 创建全屏对话框
  - 应用全屏样式
  - 保持表格的所有交互功能
  - _需求: 10.1, 10.2, 10.3, 10.4, 10.5_

- [x] 10. 实现排班比较功能
- [x] 10.1 实现历史排班查询
  - 在 ViewModel 中添加 `LoadHistorySchedulesAsync` 方法
  - 查询历史排班列表
  - 过滤掉当前排班
  - _需求: 11.1, 11.2_

- [x] 10.2 实现比较视图数据构建
  - 实现 `BuildComparisonDataAsync` 方法
  - 加载两个排班的数据
  - 识别差异的单元格
  - 计算统计信息对比
  - _需求: 11.3, 11.4, 11.5_

- [x] 10.3 实现比较视图显示
  - 并排显示两个排班表格
  - 高亮差异单元格
  - 显示统计信息对比
  - _需求: 11.3, 11.4, 11.5_

- [x] 11. 添加样式和主题支持
  - 创建 `ScheduleResultResources.xaml` 资源字典
  - 定义单元格样式：普通、手动指定、冲突
  - 定义卡片样式、按钮样式
  - 确保样式支持浅色和深色主题
  - 确保高对比度主题下的可访问性
  - 将资源字典放置在 `Themes/` 目录下
  - _需求: 所有需求_

- [x] 12. 实现可访问性支持
  - 为所有控件设置 AutomationProperties.Name
  - 为表格设置语义化标记
  - 为状态变化设置 AutomationProperties.LiveSetting
  - 确保所有按钮支持键盘导航（Tab 键）
  - 实现快捷键：Ctrl+F（筛选）、Ctrl+E（导出）、Ctrl+S（保存）、Esc（关闭）
  - 测试屏幕阅读器支持
  - _需求: 所有需求_

- [ ]* 13. 编写单元测试
  - 为 ViewModel 的数据转换方法编写单元测试
  - 测试 `BuildPositionScheduleDataAsync` 方法
  - 测试 `BuildPersonnelScheduleDataAsync` 方法
  - 测试 `BuildShiftListDataAsync` 方法
  - 测试筛选逻辑
  - 测试排序逻辑
  - 测试冲突检测逻辑
  - 使用 xUnit 和 Moq 框架
  - 将测试文件放置在 `Tests/ViewModels/` 目录下
  - _需求: 所有需求_

- [ ]* 14. 编写集成测试
  - 测试视图模式切换流程
  - 测试筛选功能
  - 测试编辑和保存流程
  - 测试导出功能
  - 测试冲突处理流程
  - 测试全屏功能
  - 测试排班比较功能
  - 将测试文件放置在 `Tests/Integration/` 目录下
  - _需求: 所有需求_

- [ ]* 15. 性能优化和测试
  - 测试大规模数据集（50人员 × 20哨位 × 30天）的性能
  - 优化虚拟化渲染
  - 优化数据缓存机制
  - 测试视图切换性能
  - 测试筛选性能
  - 使用性能分析工具识别瓶颈
  - _需求: 所有需求_

- [ ]* 16. 文档和代码注释
  - 为所有公共类和方法添加 XML 文档注释
  - 更新 README.md，添加新功能说明
  - 创建用户指南，说明如何使用各种视图模式
  - 添加代码示例和截图
  - _需求: 所有需求_
