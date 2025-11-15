# Implementation Plan

- [x] 1. 创建手动指定管理辅助类和视图模型





  - 创建ManualAssignmentManager类用于管理临时和已保存的手动指定
  - 创建ManualAssignmentViewModel类表示单个手动指定的视图模型
  - 创建TimeSlotOption类用于时段选择下拉列表
  - 实现临时手动指定的添加、编辑、删除逻辑
  - 实现已保存手动指定的加载和状态管理
  - _Requirements: 1.9, 4.1, 4.2, 4.3, 4.4_


- [x] 1.1 创建ManualAssignmentViewModel类

  - 定义基本属性（Id, TempId, Date, PersonnelId, PersonnelName等）
  - 实现IsTemporary属性（基于Id是否为null）
  - 实现UI辅助属性（CanEdit, CanDelete, StatusBadge）
  - 实现TimeSlotDisplay属性的格式化逻辑
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7_


- [x] 1.2 创建TimeSlotOption类

  - 定义Index和DisplayText属性
  - 实现GetAll静态方法生成12个时段选项
  - 格式化时段显示文本（例如："时段 0 (00:00-02:00)"）
  - _Requirements: 1.6_


- [x] 1.3 创建ManualAssignmentManager类

  - 定义TemporaryAssignments和SavedAssignments集合
  - 实现AllAssignments合并视图
  - 实现AddTemporary方法添加临时手动指定
  - 实现UpdateTemporary方法编辑临时手动指定
  - 实现RemoveTemporary方法删除临时手动指定
  - 实现LoadSaved方法加载已保存的手动指定
  - 实现MarkAsSaved方法将临时手动指定标记为已保存
  - 实现GetAllEnabled方法获取所有启用的手动指定
  - 实现Clear方法清空所有数据
  - _Requirements: 1.9, 2.9, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 5.2, 5.3, 5.4, 6.1, 6.2, 6.3, 6.4_

- [x] 2. 扩展SchedulingViewModel添加手动指定管理功能




  - 在SchedulingViewModel中添加ManualAssignmentManager实例
  - 添加AllManualAssignments属性绑定到UI
  - 添加表单相关属性（IsCreatingManualAssignment, NewManualAssignment等）
  - 实现StartCreateManualAssignmentCommand命令
  - 实现SubmitCreateManualAssignmentCommand命令
  - 实现CancelCreateManualAssignmentCommand命令
  - 实现StartEditManualAssignmentCommand命令
  - 实现SubmitEditManualAssignmentCommand命令
  - 实现CancelEditManualAssignmentCommand命令
  - 实现DeleteManualAssignmentCommand命令
  - _Requirements: 1.1, 1.2, 2.1, 2.2, 3.1, 3.2, 4.1, 4.2_

- [x] 2.1 添加手动指定管理相关属性


  - 添加_manualAssignmentManager字段
  - 添加AllManualAssignments属性
  - 添加IsCreatingManualAssignment属性
  - 添加IsEditingManualAssignment属性
  - 添加NewManualAssignment属性
  - 添加EditingManualAssignment属性
  - 添加EditingManualAssignmentDto属性
  - 添加TimeSlotOptions属性（静态列表）
  - _Requirements: 1.3, 1.4, 1.5, 1.6_

- [x] 2.2 实现创建手动指定命令


  - 实现StartCreateManualAssignmentCommand初始化新表单
  - 实现SubmitCreateManualAssignmentCommand验证并添加手动指定
  - 实现CancelCreateManualAssignmentCommand取消创建
  - 实现表单验证逻辑（ValidateManualAssignment方法）
  - 在提交成功后刷新列表并关闭表单
  - _Requirements: 1.1, 1.2, 1.8, 1.9, 1.10, 2.1, 2.2, 2.8, 2.9, 2.10, 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_

- [x] 2.3 实现编辑手动指定命令

  - 实现StartEditManualAssignmentCommand加载编辑表单
  - 实现SubmitEditManualAssignmentCommand验证并更新手动指定
  - 实现CancelEditManualAssignmentCommand取消编辑
  - 在提交成功后刷新列表并关闭表单
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [x] 2.4 实现删除手动指定命令

  - 实现DeleteManualAssignmentCommand显示确认对话框
  - 确认后调用ManualAssignmentManager.RemoveTemporary删除
  - 刷新手动指定列表
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 3. 更新LoadConstraintsAsync方法加载手动指定





  - 在LoadConstraintsAsync中调用ManualAssignmentManager.LoadSaved
  - 将从数据库加载的手动指定转换为ManualAssignmentViewModel
  - 设置已保存手动指定的IsTemporary为false
  - 在从模板加载时应用模板的手动指定启用状态
  - _Requirements: 1.2, 8.1, 8.2, 8.3, 8.4_


- [x] 4. 更新SaveAsTemplateAsync方法保存手动指定




  - 在保存模板前获取所有临时手动指定
  - 调用SchedulingService.CreateManualAssignmentAsync保存每个临时手动指定
  - 收集保存成功的手动指定ID
  - 调用ManualAssignmentManager.MarkAsSaved更新状态
  - 如果保存失败，显示错误消息并回滚
  - 将已保存的手动指定ID添加到模板的EnabledManualAssignmentIds
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_


- [x] 5. 更新BuildSchedulingRequest方法包含手动指定




  - 调用ManualAssignmentManager.GetAllEnabled获取所有启用的手动指定
  - 将已保存的手动指定ID添加到EnabledManualAssignmentIds
  - 将临时手动指定数据添加到TemporaryManualAssignments
  - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [ ] 6. 更新BuildSummarySections方法显示手动指定详情


  - 在确认步骤的摘要信息中添加"手动指定"部分
  - 获取所有启用的手动指定
  - 按日期和时段排序手动指定列表
  - 为每条手动指定显示详细信息（日期、人员姓名、哨位名称、时段、描述）
  - 如果没有启用的手动指定，显示"无启用的手动指定"
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_


- [ ] 7. 创建手动指定UI组件

  - 在CreateSchedulingPage.xaml的步骤4中扩展手动指定区域
  - 添加"添加手动指定"按钮
  - 使用ItemsRepeater显示手动指定列表
  - 创建手动指定卡片DataTemplate
  - 实现空状态提示
  - _Requirements: 1.1, 1.3, 1.4, 1.5_

- [ ] 7.1 创建手动指定卡片UI
  - 创建手动指定卡片DataTemplate
  - 显示状态标签（临时/已保存）
  - 显示日期和时段
  - 显示人员姓名和哨位名称
  - 显示描述（如果有）
  - 添加启用/禁用开关
  - 添加编辑和删除按钮（仅临时手动指定）
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7_

- [ ] 7.2 创建手动指定表单UI
  - 创建ContentDialog用于创建/编辑手动指定
  - 添加日期选择器（限制在排班日期范围内）
  - 添加人员下拉列表（绑定到SelectedPersonnels）
  - 添加哨位下拉列表（绑定到SelectedPositions）
  - 添加时段下拉列表（绑定到TimeSlotOptions）
  - 添加描述输入框
  - 添加启用状态开关
  - 添加提交和取消按钮
  - _Requirements: 1.3, 1.4, 1.5, 1.6, 1.7_

- [ ] 7.3 实现表单验证UI反馈
  - 在日期字段下方显示验证错误消息
  - 在人员字段下方显示验证错误消息
  - 在哨位字段下方显示验证错误消息
  - 在时段字段下方显示验证错误消息
  - 在所有验证错误修正后才允许提交表单
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_


- [ ] 8. 更新SchedulingRequestDto添加临时手动指定字段

  - 在SchedulingRequestDto中添加TemporaryManualAssignments属性
  - 定义ManualAssignmentRequestItem类
  - _Requirements: 6.1, 6.2_


- [ ] 9. 更新CancelWizard方法清空手动指定

  - 在CancelWizard方法中调用ManualAssignmentManager.Clear
  - 清空所有临时和已保存的手动指定
  - _Requirements: 6.4_

- [ ]* 10. 测试手动指定功能
  - 测试创建临时手动指定
  - 测试编辑临时手动指定
  - 测试删除临时手动指定
  - 测试保存模板时持久化手动指定
  - 测试执行排班时包含临时手动指定
  - 测试从模板加载时加载手动指定
  - 测试确认步骤显示手动指定详情
  - 测试表单验证逻辑
  - _Requirements: All_
