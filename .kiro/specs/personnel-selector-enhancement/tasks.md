# 实施计划

- [x] 1. 创建核心辅助类
  - 实现FuzzyMatcher、PinyinHelper和PersonnelSearchHelper类
  - 这些类将被多个ViewModel复用
  - _需求：1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3_

- [x] 1.1 实现PinyinHelper类
  - 创建Helpers/PinyinHelper.cs文件
  - 实现GetPinyinInitials方法（获取拼音首字母）
  - 实现GetFullPinyin方法（获取完整拼音）
  - 添加拼音缓存机制提升性能
  - 处理异常情况（非中文字符、空字符串等）
  - _需求：1.4_

- [ ]* 1.2 为PinyinHelper编写属性测试
  - **属性 3：拼音匹配正确性**
  - **验证：需求 1.4**

- [x] 1.3 实现FuzzyMatcher类
  - 创建Helpers/FuzzyMatcher.cs文件
  - 实现Match方法（主搜索入口）
  - 实现CalculateScore方法（计算匹配分数）
  - 实现多层匹配策略（完全匹配、前缀匹配、子串匹配、拼音匹配、模糊匹配）
  - 实现MatchResult和MatchType数据模型
  - 实现FuzzyMatchOptions配置类
  - _需求：1.3, 2.1, 2.2, 2.3, 2.8_

- [ ]* 1.4 为FuzzyMatcher编写属性测试
  - **属性 2：模糊匹配完整性**
  - **验证：需求 1.3, 2.1, 2.2**

- [ ]* 1.5 为FuzzyMatcher编写属性测试
  - **属性 5：大小写不敏感性**
  - **验证：需求 2.1**

- [ ]* 1.6 为FuzzyMatcher编写属性测试
  - **属性 6：非连续字符匹配**
  - **验证：需求 2.3**

- [ ]* 1.7 为FuzzyMatcher编写属性测试
  - **属性 8：匹配度排序正确性**
  - **验证：需求 2.8**

- [x] 1.8 实现PersonnelSearchHelper类
  - 创建Helpers/PersonnelSearchHelper.cs文件
  - 实现SearchAsync方法（带防抖的异步搜索）
  - 实现SearchImmediate方法（立即搜索）
  - 集成FuzzyMatcher和PinyinHelper
  - 实现防抖逻辑（300ms延迟）
  - 处理取消令牌和异常
  - _需求：1.2, 2.6, 6.2_

- [ ]* 1.9 为PersonnelSearchHelper编写属性测试
  - **属性 1：输入响应一致性**
  - **验证：需求 1.2**

- [ ]* 1.10 为PersonnelSearchHelper编写属性测试
  - **属性 7：清空恢复一致性**
  - **验证：需求 2.6**

- [ ]* 1.11 为PersonnelSearchHelper编写属性测试
  - **属性 17：防抖效果正确性**
  - **验证：需求 6.2**

- [ ] 2. 增强EditShiftAssignmentDialog（高优先级）
  - 这是用户痛点最明显的场景
  - 需要实现搜索框和列表的双向同步
  - _需求：3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 2.1 更新EditShiftAssignmentDialog.xaml.cs
  - 添加PersonnelSearchHelper实例
  - 修改SearchBox_TextChanged方法，使用PersonnelSearchHelper
  - 实现AutoSuggestBox的SuggestionChosen事件处理
  - 实现AutoSuggestBox的QuerySubmitted事件处理
  - 实现搜索框和ListView的双向同步
  - 添加点击展开显示所有人员的功能
  - _需求：3.2, 3.3, 3.4_

- [ ] 2.2 更新EditShiftAssignmentDialog.xaml
  - 为AutoSuggestBox添加ItemsSource绑定
  - 添加ItemTemplate显示人员信息
  - 添加SuggestionChosen和QuerySubmitted事件绑定
  - 优化无障碍属性设置
  - _需求：3.1, 5.1, 5.2, 5.3_

- [ ]* 2.3 为EditShiftAssignmentDialog编写属性测试
  - **属性 9：双向同步一致性**
  - **验证：需求 3.2**

- [ ]* 2.4 为EditShiftAssignmentDialog编写属性测试
  - **属性 10：选择同步正确性**
  - **验证：需求 3.3, 3.4**

- [ ]* 2.5 为EditShiftAssignmentDialog编写属性测试
  - **属性 11：验证警告显示**
  - **验证：需求 3.5**

- [ ] 3. 增强ScheduleResultPage（中优先级）
  - 已有AutoSuggestBox基础，主要是增强搜索算法
  - _需求：1.1, 1.2, 1.3, 1.4_

- [ ] 3.1 更新ScheduleResultViewModel.cs
  - 添加PersonnelSearchHelper实例
  - 修改UpdatePersonnelSuggestions方法，使用PersonnelSearchHelper
  - 添加点击展开显示所有人员的逻辑
  - 优化性能（使用防抖）
  - _需求：1.2, 2.6, 6.2_

- [ ] 3.2 更新ScheduleResultPage.xaml.cs
  - 修改PersonnelSearchBox_TextChanged事件处理
  - 修改PersonnelSearchBox_SuggestionChosen事件处理
  - 修改PersonnelSearchBox_QuerySubmitted事件处理
  - 添加GotFocus事件处理（显示所有人员）
  - _需求：1.1, 1.5, 1.6_

- [ ]* 3.3 为ScheduleResultPage编写属性测试
  - **属性 4：选择状态一致性**
  - **验证：需求 1.5**

- [ ] 4. 添加搜索功能到CreateSchedulingPage（可选优化）
  - 在现有的双列表选择基础上添加搜索框
  - _需求：4.1, 4.2, 4.3, 4.4, 4.5_

- [ ] 4.1 更新CreateSchedulingPage.xaml
  - 在"可用人员"ListView上方添加AutoSuggestBox
  - 配置AutoSuggestBox的样式和属性
  - 添加无障碍支持
  - _需求：4.2, 5.1, 5.2_

- [ ] 4.2 更新CreateSchedulingPage.xaml.cs或ViewModel
  - 添加人员搜索逻辑
  - 实现搜索框的事件处理
  - 过滤可用人员列表
  - _需求：4.3, 4.4, 4.5_

- [ ]* 4.3 为CreateSchedulingPage编写属性测试
  - **属性 15：过滤条件应用正确性**
  - **验证：需求 4.5**

- [ ] 5. 添加搜索功能到TemplatePage（可选优化）
  - 在现有的双列表选择基础上添加搜索框
  - _需求：4.1, 4.2, 4.3, 4.4, 4.5_

- [ ] 5.1 更新TemplatePage.xaml
  - 在"可用人员"ListView上方添加AutoSuggestBox
  - 配置AutoSuggestBox的样式和属性
  - 添加无障碍支持
  - _需求：4.2, 5.1, 5.2_

- [ ] 5.2 更新TemplatePage.xaml.cs或ViewModel
  - 添加人员搜索逻辑
  - 实现搜索框的事件处理
  - 过滤可用人员列表
  - _需求：4.3, 4.4, 4.5_

- [ ] 6. 添加NuGet依赖
  - 安装TinyPinyin.NET用于拼音转换
  - 安装FsCheck用于属性基础测试（如果尚未安装）
  - _需求：1.4_

- [x] 6.1 安装TinyPinyin.NET包
  - 执行：dotnet add package TinyPinyin.NET
  - 验证包安装成功
  - _需求：1.4_

- [ ]* 6.2 安装FsCheck包（如果需要）
  - 执行：dotnet add package FsCheck
  - 执行：dotnet add package FsCheck.Xunit
  - 验证包安装成功

- [ ] 7. 文档和示例
  - 创建使用指南和代码示例
  - 更新相关文档

- [ ] 7.1 创建使用指南文档
  - 创建Helpers/PersonnelSelector.README.md
  - 说明如何使用PersonnelSearchHelper
  - 提供AutoSuggestBox配置示例
  - 说明模糊匹配和拼音匹配的工作原理
  - _需求：4.1_

- [ ] 7.2 更新项目README
  - 在README.md中添加人员选择器功能说明
  - 添加使用示例和截图
  - 说明新增的依赖项

- [ ] 8. 最终检查点
  - 确保所有测试通过，询问用户是否有问题

- [ ] 8.1 运行所有测试
  - 执行：dotnet test
  - 确保所有单元测试通过
  - 确保所有属性测试通过
  - 修复任何失败的测试

- [ ] 8.2 手动测试各个场景
  - 测试EditShiftAssignmentDialog的人员选择
  - 测试ScheduleResultPage的人员筛选
  - 测试CreateSchedulingPage的人员搜索（如果实现）
  - 测试TemplatePage的人员搜索（如果实现）
  - 验证模糊匹配和拼音匹配功能
  - 验证无障碍功能

- [ ] 8.3 性能测试
  - 测试大量人员数据（100+）的搜索性能
  - 验证防抖功能正常工作
  - 验证虚拟化渲染正常工作
  - 确保搜索响应时间在200ms以内

- [ ] 8.4 最终确认
  - 确保所有测试通过，询问用户是否有问题
