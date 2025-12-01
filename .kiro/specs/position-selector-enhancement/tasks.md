# 实施计划

- [ ] 1. 创建PositionSearchHelper辅助类
  - 实现PositionSearchHelper类，复用FuzzyMatcher和PinyinHelper
  - 这个类将被ScheduleResultViewModel使用
  - _需求：1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3_

- [ ] 1.1 实现PositionSearchHelper类
  - 创建Helpers/PositionSearchHelper.cs文件
  - 实现SearchAsync方法（带防抖的异步搜索）
  - 实现SearchImmediate方法（立即搜索）
  - 集成FuzzyMatcher进行模糊匹配
  - 集成PinyinHelper进行拼音匹配
  - 实现防抖逻辑（300ms延迟）
  - 处理取消令牌和异常
  - _需求：1.3, 2.6, 6.2_

- [ ]* 1.2 为PositionSearchHelper编写属性测试
  - **属性 1：输入响应一致性**
  - **验证：需求 1.3**

- [ ]* 1.3 为PositionSearchHelper编写属性测试
  - **属性 2：模糊匹配完整性**
  - **验证：需求 1.4, 2.1, 2.2**

- [ ]* 1.4 为PositionSearchHelper编写属性测试
  - **属性 3：拼音匹配正确性**
  - **验证：需求 1.5**

- [ ]* 1.5 为PositionSearchHelper编写属性测试
  - **属性 5：大小写不敏感性**
  - **验证：需求 2.1**

- [ ]* 1.6 为PositionSearchHelper编写属性测试
  - **属性 6：非连续字符匹配**
  - **验证：需求 2.3**

- [ ]* 1.7 为PositionSearchHelper编写属性测试
  - **属性 7：清空恢复一致性**
  - **验证：需求 2.6**

- [ ]* 1.8 为PositionSearchHelper编写属性测试
  - **属性 8：匹配度排序正确性**
  - **验证：需求 2.8**

- [ ]* 1.9 为PositionSearchHelper编写属性测试
  - **属性 13：防抖效果正确性**
  - **验证：需求 6.2**

- [ ]* 1.10 为PositionSearchHelper编写属性测试
  - **属性 14：拼音完全匹配正确性**
  - **验证：需求 1.5, 7.1**

- [ ]* 1.11 为PositionSearchHelper编写属性测试
  - **属性 15：拼音前缀匹配正确性**
  - **验证：需求 1.5, 7.2**

- [ ]* 1.12 为PositionSearchHelper编写属性测试
  - **属性 16：编辑距离容错性**
  - **验证：需求 7.3**

- [ ]* 1.13 为PositionSearchHelper编写属性测试
  - **属性 17：标准化一致性**
  - **验证：需求 7.4**

- [ ]* 1.14 为PositionSearchHelper编写属性测试
  - **属性 18：分数计算单调性**
  - **验证：需求 7.5**

- [ ] 2. 扩展ScheduleResultViewModel
  - 添加哨位选择器相关属性和方法
  - _需求：1.6, 3.2, 3.3_

- [ ] 2.1 添加哨位选择器属性到ScheduleResultViewModel
  - 添加PositionSelectorSuggestions属性（建议列表）
  - 添加PositionSelectorSearchText属性（搜索文本）
  - 添加PositionSearchHelper实例
  - 确保属性变化通知正确触发
  - _需求：1.2, 1.3_

- [ ] 2.2 实现哨位选择器方法
  - 实现UpdatePositionSelectorSuggestions方法
  - 实现SelectPosition方法
  - 实现ClearPositionSelection方法
  - 处理空数据情况
  - 处理异常情况
  - _需求：1.6, 3.2, 3.3, 3.5_

- [ ]* 2.3 为ScheduleResultViewModel编写属性测试
  - **属性 4：选择状态一致性**
  - **验证：需求 1.6, 3.2**

- [ ]* 2.4 为ScheduleResultViewModel编写属性测试
  - **属性 10：数据同步正确性**
  - **验证：需求 3.3**

- [ ]* 2.5 为ScheduleResultViewModel编写属性测试
  - **属性 12：空数据处理正确性**
  - **验证：需求 3.5**

- [ ] 3. 更新ScheduleResultPage.xaml
  - 在按哨位展示视图中添加哨位选择器UI
  - _需求：1.1, 3.1, 5.1, 5.2, 5.3_

- [ ] 3.1 修改按哨位展示视图的布局
  - 修改Views/Scheduling/ScheduleResultPage.xaml
  - 将PositionScheduleControl包装在Grid中
  - 添加哨位选择器区域（Grid.Row="0"）
  - 调整PositionScheduleControl位置（Grid.Row="1"）
  - _需求：3.1_

- [ ] 3.2 添加哨位选择器AutoSuggestBox
  - 添加AutoSuggestBox控件
  - 配置PlaceholderText、ItemsSource、Text等属性
  - 添加ItemTemplate显示哨位信息（名称、班次总数）
  - 绑定TextChanged、SuggestionChosen、QuerySubmitted、GotFocus事件
  - 添加无障碍属性（AutomationProperties）
  - _需求：1.1, 1.2, 5.1, 5.2, 5.3, 8.1, 8.2, 8.3_

- [ ]* 3.3 为哨位选择器UI编写属性测试
  - **属性 19：建议项信息完整性**
  - **验证：需求 8.1, 8.2**

- [ ] 4. 实现ScheduleResultPage.xaml.cs事件处理
  - 添加哨位选择器的事件处理方法
  - _需求：1.3, 1.5, 1.6, 1.7, 3.4_

- [ ] 4.1 实现TextChanged事件处理
  - 添加PositionSelectorAutoSuggestBox_TextChanged方法
  - 调用ViewModel.UpdatePositionSelectorSuggestions
  - 处理UserInput原因
  - _需求：1.3_

- [ ] 4.2 实现SuggestionChosen事件处理
  - 添加PositionSelectorAutoSuggestBox_SuggestionChosen方法
  - 调用ViewModel.SelectPosition
  - 类型转换和空值检查
  - _需求：1.6_

- [ ] 4.3 实现QuerySubmitted事件处理
  - 添加PositionSelectorAutoSuggestBox_QuerySubmitted方法
  - 处理选择建议项的情况
  - 处理直接输入文本的情况（选择第一个匹配项）
  - _需求：1.7_

- [ ] 4.4 实现GotFocus事件处理
  - 添加PositionSelectorAutoSuggestBox_GotFocus方法
  - 显示所有可用哨位
  - 打开建议列表
  - _需求：3.4_

- [ ]* 4.5 为事件处理编写属性测试
  - **属性 11：焦点展开一致性**
  - **验证：需求 3.4**

- [ ] 5. 更新视图模式切换逻辑
  - 确保切换到"按哨位"视图时正确初始化哨位选择器
  - _需求：1.1, 3.1_

- [ ] 5.1 更新OnViewModeChangedAsync方法
  - 修改ScheduleResultViewModel.cs中的OnViewModeChangedAsync方法
  - 当切换到ViewMode.ByPosition时，初始化哨位选择器
  - 加载PositionSchedules数据（如果尚未加载）
  - 设置默认选中的哨位（第一个哨位）
  - _需求：1.1, 3.1_

- [ ]* 5.2 为视图切换编写属性测试
  - **属性 9：视图切换一致性**
  - **验证：需求 1.1, 3.1**

- [ ] 6. 增强PositionScheduleData数据模型
  - 添加用于显示的统计信息
  - _需求：8.1, 8.2_

- [ ] 6.1 扩展PositionScheduleData类
  - 修改DTOs/PositionScheduleData.cs
  - 添加TotalShifts计算属性（统计所有周次的班次总数）
  - 添加其他可能需要的显示属性
  - _需求：8.2_

- [ ] 7. 创建使用指南文档
  - 创建文档说明如何使用哨位选择器
  - _需求：4.1_

- [ ] 7.1 创建使用指南文档
  - 创建Helpers/PositionSelector.README.md
  - 说明如何使用PositionSearchHelper
  - 提供AutoSuggestBox配置示例
  - 说明模糊匹配和拼音匹配的工作原理
  - 提供代码示例
  - _需求：4.1_

- [ ] 7.2 更新项目README
  - 在README.md中添加哨位选择器功能说明
  - 添加使用示例和截图
  - 说明与人员选择器的关系

- [ ] 8. 最终检查点
  - 确保所有测试通过，询问用户是否有问题

- [ ] 8.1 运行所有测试
  - 执行：dotnet test
  - 确保所有单元测试通过
  - 确保所有属性测试通过
  - 修复任何失败的测试

- [ ] 8.2 手动测试各个场景
  - 测试切换到"按哨位"视图
  - 测试哨位选择器的搜索功能
  - 测试哨位选择和切换
  - 测试PositionScheduleControl的更新
  - 验证模糊匹配和拼音匹配功能
  - 验证无障碍功能

- [ ] 8.3 性能测试
  - 测试大量哨位数据（50+）的搜索性能
  - 验证防抖功能正常工作
  - 验证虚拟化渲染正常工作
  - 确保搜索响应时间在200ms以内

- [ ] 8.4 最终确认
  - 确保所有测试通过，询问用户是否有问题
