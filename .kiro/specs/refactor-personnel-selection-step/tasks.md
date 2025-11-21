# 实现计划

## 任务列表

- [x] 1. 创建PositionPersonnelManager核心类




  - 实现哨位与人员的临时关联管理
  - 实现更改跟踪功能
  - 实现撤销功能
  - _Requirements: 3.1, 3.2, 3.7_


- [x] 1.1 创建PositionPersonnelManager类文件

  - 在ViewModels/Scheduling/目录下创建PositionPersonnelManager.cs
  - 定义基本的类结构和字段
  - _Requirements: 3.1, 3.2_


- [x] 1.2 实现Initialize方法

  - 从PositionDto列表初始化原始数据和当前数据
  - 清空更改记录
  - _Requirements: 3.1_


- [x] 1.3 实现GetAvailablePersonnel方法

  - 返回指定哨位的当前可用人员列表（包含临时更改）
  - _Requirements: 3.1_


- [x] 1.4 实现AddPersonnelTemporarily方法

  - 临时添加人员到指定哨位
  - 记录更改到_changes字典
  - _Requirements: 3.1_


- [x] 1.5 实现RemovePersonnelTemporarily方法

  - 临时移除人员从指定哨位
  - 记录更改到_changes字典
  - _Requirements: 3.2_


- [x] 1.6 实现GetChanges方法

  - 返回指定哨位的临时更改记录
  - 包含添加和移除的人员ID和名称
  - _Requirements: 3.7_


- [x] 1.7 实现RevertChanges和RevertAllChanges方法

  - 撤销指定哨位或所有哨位的临时更改
  - 恢复到原始数据状态
  - _Requirements: 3.5, 3.6_

- [x] 1.8 实现GetPositionsWithChanges方法


  - 返回所有有临时更改的哨位ID列表
  - _Requirements: 3.7_

- [x] 2. 创建ViewModel辅助类









  - 创建PositionPersonnelViewModel和PersonnelItemViewModel
  - 定义PersonnelSourceType枚举
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_



- [x] 2.1 创建PersonnelSourceType枚举





  - 在ViewModels/Scheduling/目录下创建PersonnelSourceType.cs
  - 定义AutoExtracted、TemporarilyAdded、TemporarilyRemoved、ManuallyAdded四种类型
  - _Requirements: 1.6, 3.1.5_


- [x] 2.2 创建PersonnelItemViewModel类






  - 在ViewModels/Scheduling/目录下创建PersonnelItemViewModel.cs
  - 定义PersonnelId、PersonnelName、SkillsDisplay等属性
  - 定义IsSelected、SourceType、IsShared等可观察属性
  - _Requirements: 2.2, 2.5_



- [x] 2.3 创建PositionPersonnelViewModel类





  - 在ViewModels/Scheduling/目录下创建PositionPersonnelViewModel.cs
  - 定义PositionId、PositionName、Location等属性
  - 定义AvailablePersonnel集合和IsExpanded、HasChanges等可观察属性
  - _Requirements: 2.1, 2.2, 2.3, 2.4_


- [x] 3. 在SchedulingViewModel中集成PositionPersonnelManager












  - 添加PositionPersonnelManager实例
  - 添加相关的可观察属性
  - 实现自动提取人员逻辑
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_



- [x] 3.1 添加PositionPersonnelManager字段







  - 在SchedulingViewModel中添加_positionPersonnelManager字段
  - 在构造函数中初始化
  - _Requirements: 1.1_




- [x] 3.2 添加新的可观察属性



  - 添加ManuallyAddedPersonnelIds集合
  - 添加AutoExtractedPersonnelCount和ManuallyAddedPersonnelCou

nt属性
  - 添加IsAddingPersonnelToPosition、CurrentEditingPosition等属性

  - _Requirements: 1.6, 3.1.3_

- [x] 3.3 实现自动提取人员逻辑



  - 在OnCurrentStepChanged中添加步骤3的处理

  - 从SelectedPositions提取可用人员

  - 初始化PositionPersonnelManager
  - 更新AutoExtractedPersonnelCount
  - _Requirements: 1.1, 1.2, 1.3_



- [x] 3.4 实现SelectedPositions变化监听


  - 在OnSelectedPositionsChanged中添加逻辑
  - 当哨位列表变化时重新提取人员


  - _Requirements: 1.2, 1.3_





- [x] 4. 实现为哨位添加人员的命令和逻辑










  - 实现StartAddPersonnelToPositionCommand
  - 实现SubmitAddPersonnelToPositionCommand
  - 实现CancelAddPersonnelToPositionCommand

  - _Requirements: 3.1_



- [x] 4.1 实现StartAddPersonnelToPosition方法



  - 设置CurrentEditingPosition
  - 从AvailablePersonnels中筛选可添加的人员

  - 设置IsAddingPersonnelToPosition为true
  - _Requirements: 3.1_

- [x] 4.2 实现SubmitAddPersonnelToPositionAsync方法


  - 验证选择的人员

  - 调用_positionPersonnelManager.AddPersonnelTemporarily
  - 更新UI显示
  - 设置IsAddingPersonnelToPosition为false
  - _Requirements: 3.1_

- [x] 4.3 实现CancelAddPersonnelToPosition方法


  - 清空CurrentEditingPosition
  - 设置IsAddingPersonnelToPosition为false

  - _Requirements: 3.1_

- [x] 5. 实现移除和撤销人员的命令和逻辑






  - 实现RemovePersonnelFromPositionCommand
  - 实现RevertPositionChangesCommand
  - _Requirements: 3.2, 3.7_

- [x] 5.1 实现RemovePersonnelFromPosition方法







  - 调用_positionPersonnelManager.RemovePersonnelTemporarily
  - 更新UI显示
  - _Requirements: 3.2_


- [x] 5.2 实现RevertPositionChanges方法







  - 调用_positionPersonnelManager.RevertChanges
  - 更新UI显示

  - _Requirements: 3.7_


- [x] 6. 实现保存为永久的命令和逻辑





  - 实现SavePositionChangesCommand
  - 实现确认对话框
  - 调用IPositionService.UpdateAsync
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
- [x] 6.1 实现ShowSaveConfirmationDialog方法




- [x] 6.1 实现ShowSaveConfirmationDialog方法



  - 创建ContentDialog显示将要保存的更改
  - 列出添加和移除的人员
  - 返回用户确认结果
  - _Requirements: 4.3_

- [x] 6.2 实现SavePositionChangesAsync方法





  - 获取哨位的临时更改
  - 显示确认对话框
  - 调用IPositionService.UpdateAsync更新数据库
  - 更新本地PositionDto数据
  - 重新初始化PositionPersonnelManager
  - 显示成功或错误提示
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 7. 实现手动添加参与人员的命令和逻辑




  - 实现StartManualAddPersonnelCommand
  - 实现SubmitManualAddPersonnelCommand
  - 实现RemoveManualPersonnelCommand
  - _Requirements: 3.1.1, 3.1.2, 3.1.3, 3.1.4, 3.1.5_

- [x] 7.1 实现StartManualAddPersonnel方法


  - 从AvailablePersonnels中筛选不在任何哨位可用人员列表中的人员
  - 设置可选人员列表
  - 显示手动添加人员对话框
  - _Requirements: 3.1.1, 3.1.2_

- [x] 7.2 实现SubmitManualAddPersonnelAsync方法

  - 验证选择的人员
  - 添加到ManuallyAddedPersonnelIds列表
  - 更新ManuallyAddedPersonnelCount
  - 关闭对话框
  - _Requirements: 3.1.3, 3.1.4_

- [x] 7.3 实现RemoveManualPersonnel方法

  - 从ManuallyAddedPersonnelIds列表中移除
  - 更新ManuallyAddedPersonnelCount
  - _Requirements: 3.1.4_

- [x] 8. 调整步骤顺序




  - 交换步骤2和步骤3的XAML内容
  - 更新步骤标题和说明
  - _Requirements: 5.1, 5.2, 5.3_

- [x] 8.1 在CreateSchedulingPage.xaml中交换步骤2和步骤3的内容


  - 将原步骤2（选择人员）的XAML移到步骤3的位置
  - 将原步骤3（选择哨位）的XAML移到步骤2的位置
  - 更新步骤标题文本
  - _Requirements: 5.1, 5.2_

- [x] 8.2 更新步骤验证逻辑


  - 在ValidateStep2中验证哨位选择
  - 在ValidateStep3中验证人员选择
  - 更新CanGoNext方法
  - _Requirements: 5.3_
- [x] 9. 创建步骤3的新UI界面




- [ ] 9. 创建步骤3的新UI界面

  - 创建基于哨位的人员展示界面
  - 实现展开/折叠功能
  - 实现临时更改的视觉标识
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 3.3, 3.4, 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 9.1 创建参与人员统计区域


  - 显示自动提取人员数量
  - 显示手动添加人员数量
  - 显示总计人员数量
  - 使用InfoBar或卡片样式
  - _Requirements: 1.6_

- [x] 9.2 创建按哨位查看可用人员区域

  - 使用ItemsRepeater或ListView显示哨位列表
  - 为每个哨位创建Expander控件
  - 显示哨位名称、地点和可用人员数量
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 9.3 实现哨位人员列表

  - 在Expander内部显示人员列表
  - 使用CheckBox显示人员选择状态
  - 显示人员名称和技能
  - 使用图标标识人员来源类型（自动提取、临时添加、临时移除）
  - 使用特殊标识显示被多个哨位共享的人员
  - _Requirements: 2.2, 2.5, 3.3, 3.4_

- [x] 9.4 添加哨位操作按钮

  - 添加"为此哨位添加人员"按钮
  - 添加"保存为永久"按钮
  - 添加"撤销更改"按钮
  - 根据是否有更改控制按钮的可见性和启用状态
  - _Requirements: 3.1, 4.1, 3.7_

- [x] 9.5 创建手动添加的人员区域

  - 显示手动添加的人员列表
  - 为每个人员提供移除按钮
  - 添加"添加参与人员（对所有哨位可用）"按钮
  - 添加说明文本
  - _Requirements: 3.1.4, 3.1.5_

- [x] 10. 创建对话框UI




  - 创建为哨位添加人员对话框
  - 创建手动添加参与人员对话框
  - 创建保存为永久确认对话框
  - _Requirements: 3.1, 3.1.1, 4.3_

- [x] 10.1 在CreateSchedulingPage.xaml中添加为哨位添加人员对话框


  - 使用ContentDialog控件
  - 添加哨位信息显示
  - 添加搜索框
  - 添加人员列表（使用ListView）
  - 添加"仅显示具备所需技能的人员"筛选选项
  - _Requirements: 3.1_

- [x] 10.2 在CreateSchedulingPage.xaml中添加手动添加参与人员对话框

  - 使用ContentDialog控件
  - 添加说明信息
  - 添加搜索框
  - 添加人员列表（使用ListView）
  - _Requirements: 3.1.1, 3.1.2_

- [x] 10.3 在SchedulingViewModel中实现保存确认对话框逻辑


  - 在ShowSaveConfirmationDialog方法中创建ContentDialog
  - 显示将要保存的更改详情
  - 返回用户确认结果
  - _Requirements: 4.3_
-

- [x] 11. 更新BuildSummarySections方法




  - 添加临时更改摘要
  - 添加手动添加人员摘要
  - _Requirements: 3.7, 3.1.5_

- [x] 11.1 在BuildSummarySections中添加哨位临时更改摘要


  - 遍历所有有更改的哨位
  - 显示每个哨位的添加和移除人员信息
  - _Requirements: 3.7_

- [x] 11.2 在BuildSummarySections中添加手动添加人员摘要


  - 显示手动添加的人员列表
  - 标注这些人员对所有哨位可用
  - _Requirements: 3.1.5_

- [ ] 12. 实现草稿保存和恢复功能
  - 在CreateDraftAsync中保存PositionPersonnelManager状态
  - 在RestoreFromDraftAsync中恢复PositionPersonnelManager状态
  - _Requirements: 6.4_

- [ ] 12.1 扩展SchedulingDraftDto
  - 添加PositionPersonnelChanges字段保存临时更改
  - 添加ManuallyAddedPersonnelIds字段
  - _Requirements: 6.4_

- [ ] 12.2 在CreateDraftAsync中保存状态
  - 保存所有哨位的临时更改
  - 保存手动添加的人员ID列表
  - _Requirements: 6.4_

- [ ] 12.3 在RestoreFromDraftAsync中恢复状态
  - 恢复PositionPersonnelManager的更改记录
  - 恢复ManuallyAddedPersonnelIds列表
  - _Requirements: 6.4_

- [ ] 13. 实现模板加载兼容性
  - 确保LoadTemplateAsync正常工作
  - 确保ApplyTemplateConstraints正常工作
  - _Requirements: 6.3_

- [ ] 13.1 测试模板加载功能
  - 验证加载模板后步骤2和步骤3的数据正确
  - 验证TemplateApplied标志正常工作
  - _Requirements: 6.3_

- [ ] 13.2 测试模板保存功能
  - 验证SaveAsTemplateAsync保存步骤2和步骤3的数据
  - 验证临时更改不会被保存到模板
  - _Requirements: 6.3_

- [ ] 14. 实现性能优化
  - 实现虚拟化渲染
  - 优化人员提取性能
  - 添加缓存机制
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 14.1 在UI中使用ItemsRepeater实现虚拟化
  - 将哨位列表和人员列表改用ItemsRepeater
  - 配置虚拟化参数
  - _Requirements: 7.4_

- [ ] 14.2 优化自动提取人员逻辑
  - 使用HashSet提高查找性能
  - 避免重复计算
  - 添加性能日志
  - _Requirements: 7.1, 7.2, 7.5_

- [ ] 14.3 添加人员和哨位查找缓存
  - 缓存人员ID到PersonnelDto的映射
  - 缓存哨位ID到PositionDto的映射
  - _Requirements: 7.3_

- [ ] 15. 添加错误处理和日志
  - 添加try-catch块
  - 添加调试日志
  - 实现友好的错误提示
  - _Requirements: 4.5, 6.1, 6.2_

- [ ] 15.1 在关键方法中添加错误处理
  - 在SavePositionChangesAsync中添加错误处理
  - 在自动提取人员逻辑中添加错误处理
  - 使用DialogService显示错误信息
  - _Requirements: 4.5_

- [ ] 15.2 添加调试日志
  - 在PositionPersonnelManager的关键方法中添加日志
  - 在ViewModel的关键方法中添加日志
  - 记录性能指标
  - _Requirements: 7.1, 7.2_

- [ ] 16. 更新相关文档
  - 更新README.md
  - 更新CLAUDE.md
  - _Requirements: 所有需求_

- [ ] 16.1 更新README.md
  - 添加步骤顺序调整说明
  - 添加新功能说明（基于哨位的人员管理、临时更改、手动添加人员）
  - 更新使用说明
  - _Requirements: 所有需求_

- [ ] 16.2 更新CLAUDE.md
  - 添加PositionPersonnelManager的设计说明
  - 添加两种添加人员方式的说明
  - 添加实现细节和注意事项
  - _Requirements: 所有需求_

- [ ]* 17. 测试和验证
  - 进行功能测试
  - 进行性能测试
  - 进行兼容性测试
  - _Requirements: 所有需求_

- [ ]* 17.1 功能测试
  - 测试步骤顺序调整
  - 测试自动提取人员
  - 测试为哨位临时添加/移除人员
  - 测试手动添加参与人员
  - 测试保存为永久
  - 测试撤销更改
  - 测试草稿保存和恢复
  - 测试模板加载和保存
  - _Requirements: 1.1-1.6, 2.1-2.5, 3.1-3.7, 3.1.1-3.1.6, 4.1-4.5, 5.1-5.5, 6.1-6.6_

- [ ]* 17.2 性能测试
  - 测试50个哨位的人员提取性能
  - 测试100个人员的UI渲染性能
  - 测试保存为永久的性能
  - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [ ]* 17.3 兼容性测试
  - 测试与现有功能的兼容性（约束配置、手动指定等）
  - 测试数据模型的兼容性
  - 测试服务接口的兼容性
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_
