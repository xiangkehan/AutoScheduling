# 实现计划

- [x] 1. 准备工作和文件框架创建





  - 创建所有部分类文件的空框架
  - 设置正确的命名空间和基本结构
  - 备份原始文件
  - _Requirements: 1.1, 8.1, 8.2, 8.3, 8.4_

- [x] 2. 迁移属性定义到 Properties.cs




- [x] 2.1 迁移基本属性和 ObservableProperty


  - 迁移步骤和基本信息属性 (CurrentStep, ScheduleTitle, StartDate, EndDate)
  - 迁移数据集合属性 (AvailablePersonnels, SelectedPersonnels, AvailablePositions, SelectedPositions)
  - 迁移约束相关属性 (HolidayConfigs, FixedPositionRules, ManualAssignments)
  - 迁移状态属性 (IsExecuting, IsLoadingInitial, IsLoadingConstraints)
  - _Requirements: 2.1, 2.2_



- [x] 2.2 迁移手动指定表单属性

  - 迁移表单状态属性 (IsCreatingManualAssignment, IsEditingManualAssignment)
  - 迁移表单数据属性 (NewManualAssignment, EditingManualAssignment, EditingManualAssignmentDto)
  - 迁移包装属性 (NewManualAssignmentDate, EditingManualAssignmentPersonnelId 等)
  - 迁移验证错误属性 (DateValidationError, PersonnelValidationError 等)


  - _Requirements: 2.1, 3.2_


- [x] 2.3 迁移哨位人员管理属性
  - 迁移哨位人员相关属性 (IsAddingPersonnelToPosition, CurrentEditingPosition)
  - 迁移可用人员列表属性 (AvailablePersonnelForPosition, SelectedPersonnelIdsForPosition)


  - 迁移手动添加人员属性 (IsManualAddingPersonnel, ManuallyAddedPersonnelIds)
  - 迁移视图模型集合 (PositionPersonnelViewModels)




  - _Requirements: 2.1, 4.1_


- [x] 2.4 迁移其他属性和属性变更回调


  - 迁移模板属性 (LoadedTemplateId, TemplateApplied)
  - 迁移结果属性 (ResultSchedule, SummarySections)
  - 迁移所有属性变更回调方法 (OnCurrentStepChanged, OnStartDateChanged 等)
  - _Requirements: 2.1, 2.2_

- [x] 3. 迁移命令定义到 Commands.cs

  - 迁移所有命令属性声明
  - 包括核心命令、模板命令、约束命令、手动指定命令、哨位人员命令
  - 确保命令初始化代码保留在主文件构造函数中
  - _Requirements: 2.3_

- [x] 4. 迁移向导流程逻辑到 Wizard.cs



- [x] 4.1 迁移步骤导航方法


  - 迁移 NextStep 和 PreviousStep 方法
  - 迁移 CanGoNext 方法
  - 迁移 RefreshCommandStates 方法
  - _Requirements: 6.1, 6.2_



- [x] 4.2 迁移步骤验证方法

  - 迁移 ValidateStep1, ValidateStep2, ValidateStep3 方法

  - 迁移 CanExecuteScheduling 方法
  - _Requirements: 6.2_

- [x] 4.3 迁移执行排班方法

  - 迁移 ExecuteSchedulingAsync 方法
  - 迁移 BuildSchedulingRequest 方法
  - _Requirements: 6.3, 6.4_

- [x] 4.4 迁移取消和重置方法


  - 迁移 CancelWizard 方法
  - 迁移 ResetViewModelState 方法
  - 迁移 ShouldPromptForDraftSave 方法
  - _Requirements: 6.1_

- [x] 4.5 迁移摘要构建方法


  - 迁移 BuildSummarySections 方法
  - _Requirements: 6.5_

- [x] 5. 迁移手动指定管理到 ManualAssignment.cs

- [x] 5.1 迁移创建手动指定方法

  - 迁移 StartCreateManualAssignment 方法
  - 迁移 SubmitCreateManualAssignmentAsync 方法
  - 迁移 CancelCreateManualAssignment 方法
  - _Requirements: 3.1, 3.2_


- [x] 5.2 迁移编辑手动指定方法

  - 迁移 StartEditManualAssignment 方法
  - 迁移 SubmitEditManualAssignmentAsync 方法
  - 迁移 CancelEditManualAssignment 方法
  - _Requirements: 3.1, 3.2_

- [x] 5.3 迁移删除和验证方法


  - 迁移 DeleteManualAssignmentAsync 方法
  - 迁移两个 ValidateManualAssignment 重载方法
  - 迁移 ClearValidationErrors 方法
  - _Requirements: 3.1, 3.3, 3.4_



- [x] 6. 迁移哨位人员管理到 PositionPersonnel.cs





- [x] 6.1 迁移为哨位添加人员方法

  - 迁移 StartAddPersonnelToPosition 方法
  - 迁移 SubmitAddPersonnelToPositionAsync 方法
  - 迁移 CancelAddPersonnelToPosition 方法
  - _Requirements: 4.1_

- [x] 6.2 迁移移除和撤销方法


  - 迁移 RemovePersonnelFromPosition 方法
  - 迁移 RevertPositionChanges 方法
  - _Requirements: 4.2_

- [x] 6.3 迁移保存为永久方法


  - 迁移 ShowSaveConfirmationDialog 方法
  - 迁移 SavePositionChangesAsync 方法
  - _Requirements: 4.3_

- [x] 6.4 迁移手动添加参与人员方法


  - 迁移 StartManualAddPersonnel 方法
  - 迁移 SubmitManualAddPersonnelAsync 方法
  - 迁移 CancelManualAddPersonnel 方法
  - 迁移 RemoveManualPersonnel 方法
  - _Requirements: 4.4_

- [x] 6.5 迁移人员提取和视图模型更新方法


  - 迁移 ExtractPersonnelFromPositions 方法
  - 迁移 UpdatePositionPersonnelViewModels 方法
  - _Requirements: 4.1, 4.2_

- [ ] 7. 迁移模板和约束管理到 TemplateConstraints.cs

- [ ] 7.1 迁移约束加载方法
  - 迁移 LoadConstraintsAsync 方法
  - 保留所有错误处理和日志记录
  - _Requirements: 5.3_

- [ ] 7.2 迁移模板管理方法
  - 迁移 LoadTemplateAsync 方法
  - 迁移 ApplyTemplateConstraints 方法
  - 迁移 CanSaveTemplate 方法
  - 迁移 SaveAsTemplateAsync 方法
  - _Requirements: 5.1, 5.2, 5.4_

- [ ] 8. 迁移状态管理到 StateManagement.cs

- [ ] 8.1 迁移草稿保存方法
  - 迁移 CreateDraftAsync 方法
  - 保留所有数据收集和序列化逻辑
  - _Requirements: 7.1, 7.2_

- [ ] 8.2 迁移草稿恢复方法
  - 迁移 RestoreFromDraftAsync 方法
  - 保留所有数据恢复和状态重建逻辑
  - _Requirements: 7.1, 7.2_

- [ ] 9. 迁移辅助方法到 Helpers.cs
  - 迁移 TimeSlotOptions 属性
  - 迁移 AllManualAssignments 属性
  - 迁移其他小型辅助方法
  - _Requirements: 2.1_

- [ ] 10. 清理和优化主文件
- [ ] 10.1 清理主文件内容
  - 移除已迁移的代码
  - 保留构造函数和依赖注入
  - 保留初始化方法 (LoadInitialDataAsync, BuildCaches)
  - 保留缓存访问方法 (GetPersonnelFromCache, GetPositionFromCache)
  - _Requirements: 1.2, 2.2_

- [ ] 10.2 验证文件行数
  - 确认主文件行数在 300-600 行之间
  - 确认所有部分类文件行数不超过 1000 行
  - _Requirements: 1.1, 1.2, 1.3_

- [ ] 11. 编译和功能验证
- [ ] 11.1 编译检查
  - 编译项目,确保无错误
  - 解决所有编译警告
  - _Requirements: 7.5_

- [ ]* 11.2 功能测试
  - 测试步骤 1: 基本信息配置
  - 测试步骤 2: 哨位选择
  - 测试步骤 3: 人员选择和哨位人员管理
  - 测试步骤 4: 约束配置和手动指定
  - 测试步骤 5: 摘要和执行排班
  - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [ ]* 11.3 模板和草稿功能测试
  - 测试加载模板功能
  - 测试保存模板功能
  - 测试保存草稿功能
  - 测试恢复草稿功能
  - _Requirements: 7.1, 7.2_

- [ ]* 11.4 手动指定功能测试
  - 测试创建手动指定
  - 测试编辑手动指定
  - 测试删除手动指定
  - 测试手动指定验证
  - _Requirements: 7.1, 7.2_

- [ ]* 11.5 哨位人员管理功能测试
  - 测试为哨位添加人员
  - 测试临时移除人员
  - 测试撤销更改
  - 测试保存为永久
  - 测试手动添加参与人员
  - _Requirements: 7.1, 7.2_

- [ ] 12. 代码审查和文档更新
- [ ] 12.1 代码审查
  - 检查代码风格一致性
  - 检查注释完整性
  - 检查命名规范
  - _Requirements: 8.1, 8.2, 8.3_

- [ ] 12.2 更新文档
  - 更新代码注释
  - 记录重构决策
  - 更新开发文档(如果需要)
  - _Requirements: 2.1, 2.2, 2.3, 2.4_
