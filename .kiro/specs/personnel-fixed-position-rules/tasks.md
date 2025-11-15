# Implementation Plan

- [x] 1. 扩展PersonnelViewModel添加定岗规则管理功能





  - 在PersonnelViewModel中添加定岗规则相关的属性和集合
  - 添加IConstraintService和IPositionService依赖注入
  - 实现LoadFixedPositionRulesCommand命令,在选中人员变化时自动加载规则列表
  - _Requirements: 1.1, 1.2_

- [x] 1.1 实现定岗规则数据加载逻辑


  - 在OnSelectedItemChanged方法中调用LoadFixedPositionRulesCommand
  - 调用ConstraintService.GetFixedAssignmentDtosByPersonAsync获取规则列表
  - 更新FixedPositionRules集合
  - 处理加载失败的情况
  - _Requirements: 1.2, 1.3_

- [x] 1.2 实现可用哨位列表加载


  - 调用PositionService.GetAllAsync获取所有哨位
  - 更新AvailablePositions集合
  - 在ViewModel初始化时加载哨位列表
  - _Requirements: 6.1, 6.4_
- [x] 2. 实现创建定岗规则功能




- [ ] 2. 实现创建定岗规则功能

- [x] 2.1 添加创建规则的属性和命令


  - 添加NewFixedPositionRule属性(CreateFixedAssignmentDto类型)
  - 实现CreateFixedPositionRuleCommand命令
  - 添加IsCreatingRule属性控制表单显示状态
  - _Requirements: 2.1, 2.2_

- [x] 2.2 实现创建规则的业务逻辑

  - 验证表单输入(至少选择一个哨位或时段)
  - 调用ConstraintService.CreateFixedAssignmentAsync创建规则
  - 创建成功后刷新规则列表
  - 重置表单并关闭创建界面
  - 处理创建失败的情况并显示错误消息
  - _Requirements: 2.6, 2.7, 2.8, 2.9, 2.10_

- [x] 3. 实现编辑定岗规则功能




- [x] 3.1 添加编辑规则的属性和命令


  - 添加EditingFixedPositionRule属性(UpdateFixedAssignmentDto类型)
  - 添加SelectedRule属性
  - 实现StartEditRuleCommand命令
  - 实现SaveFixedPositionRuleCommand命令
  - 实现CancelRuleEditCommand命令
  - 添加IsEditingRule属性控制编辑状态
  - _Requirements: 3.1, 3.2_

- [x] 3.2 实现编辑规则的业务逻辑


  - 在StartEditRuleCommand中将选中规则数据复制到EditingFixedPositionRule
  - 保存原始数据副本用于取消操作
  - 在SaveFixedPositionRuleCommand中验证表单输入
  - 调用ConstraintService.UpdateFixedAssignmentAsync更新规则
  - 更新成功后刷新规则列表并关闭编辑界面
  - 在CancelRuleEditCommand中恢复原始数据
  - 处理更新失败的情况并显示错误消息
  - _Requirements: 3.3, 3.4, 3.5, 3.6, 3.7_
- [x] 4. 实现删除定岗规则功能




- [ ] 4. 实现删除定岗规则功能

- [x] 4.1 添加删除规则命令


  - 实现DeleteFixedPositionRuleCommand命令,接受FixedAssignmentDto参数
  - _Requirements: 4.1_

- [x] 4.2 实现删除规则的业务逻辑


  - 使用DialogService显示确认对话框
  - 确认后调用ConstraintService.DeleteFixedPositionRuleAsync删除规则
  - 删除成功后从FixedPositionRules集合中移除该规则
  - 处理删除失败的情况并显示错误消息
  - _Requirements: 4.2, 4.3, 4.4, 4.5, 4.6_


- [ ] 5. 创建UI组件 - 定岗规则区域

- [x] 5.1 在PersonnelPage.xaml中添加定岗规则Expander


  - 在人员详情StackPanel中添加Expander控件
  - 设置Header为"定岗规则"
  - 绑定Visibility到SelectedItem(仅在选中人员时显示)
  - _Requirements: 1.1_

- [x] 5.2 实现规则列表UI


  - 使用ItemsRepeater显示FixedPositionRules集合
  - 创建规则卡片DataTemplate
  - 显示允许的哨位(使用ItemsRepeater + Border标签样式)
  - 显示允许的时段(使用ItemsRepeater + Border标签样式)
  - 显示规则描述
  - 显示启用状态(使用ToggleSwitch)
  - 添加编辑和删除按钮
  - _Requirements: 1.3, 1.5_

- [ ] 5.3 实现空状态提示
  - 添加TextBlock显示"暂无定岗规则"
  - 绑定Visibility到FixedPositionRules.Count
  - _Requirements: 1.4_

- [ ] 5.4 添加"添加定岗规则"按钮
  - 绑定Command到CreateFixedPositionRuleCommand
  - _Requirements: 2.1_

- [x] 6. 创建UI组件 - 规则创建表单




- [x] 6.1 创建规则表单UI结构

  - 在Expander内添加创建表单StackPanel
  - 绑定Visibility到IsCreatingRule属性
  - _Requirements: 2.2_


- [x] 6.2 实现哨位选择UI

  - 使用ListView + SelectionMode="Multiple"
  - 绑定ItemsSource到AvailablePositions
  - 显示哨位名称
  - 添加选中数量显示TextBlock
  - 在SelectionChanged事件中更新NewFixedPositionRule.AllowedPositionIds
  - _Requirements: 2.3, 6.1, 6.2, 6.3_


- [x] 6.3 实现时段选择UI

  - 创建时段显示模型列表(0-11,包含时间范围)
  - 使用ListView + SelectionMode="Multiple"
  - 显示时段序号和时间范围(例如"0 (00:00-02:00)")
  - 添加选中数量显示TextBlock
  - 在SelectionChanged事件中更新NewFixedPositionRule.AllowedPeriods
  - _Requirements: 2.4, 5.1, 5.2, 5.3, 5.4_

- [x] 6.4 实现其他表单字段


  - 添加规则描述TextBox
  - 添加启用状态ToggleSwitch
  - 添加提交和取消按钮
  - _Requirements: 2.5_

- [x] 7. 创建UI组件 - 规则编辑表单




- [x] 7.1 创建编辑表单UI结构


  - 复用创建表单的布局结构
  - 绑定Visibility到IsEditingRule属性
  - 绑定数据到EditingFixedPositionRule
  - _Requirements: 3.2_

- [x] 7.2 实现编辑表单数据预填充


  - 在PersonnelPage.xaml.cs中监听IsEditingRule属性变化
  - 当进入编辑模式时,同步选中的哨位和时段到ListView
  - _Requirements: 3.2_

- [x] 7.3 添加编辑表单操作按钮

  - 添加保存按钮,绑定到SaveFixedPositionRuleCommand
  - 添加取消按钮,绑定到CancelRuleEditCommand
  - _Requirements: 3.6_

- [ ] 8. 实现响应式布局
- [ ]* 8.1 调整定岗规则区域的响应式行为
  - 在小屏幕(<768px)时,规则卡片垂直堆叠
  - 在大屏幕(>=768px)时,规则卡片可以水平排列
  - 确保表单在小屏幕上保持可读性
  - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [x] 9. 实现表单验证和错误处理






- [x] 9.1 添加表单验证逻辑


  - 验证至少选择一个哨位或一个时段
  - 验证规则描述长度不超过500字符
  - 验证时段索引在0-11范围内
  - 在UI中显示验证错误消息
  - _Requirements: 2.6, 2.7_

- [x] 9.2 实现错误处理


  - 捕获ConstraintService调用的异常
  - 使用DialogService显示用户友好的错误消息
  - 区分验证错误、业务逻辑错误和系统错误
  - _Requirements: 2.10, 3.7, 4.6_



- [x] 10. 添加时段时间范围转换器





- [x] 10.1 创建PeriodIndexToTimeConverter


  - 实现IValueConverter接口
  - 将时段索引(0-11)转换为时间范围字符串
  - 例如: 0 -> "00:00-02:00", 1 -> "02:00-04:00"
  - _Requirements: 5.2_

- [ ]* 10.2 在XAML中注册和使用转换器
  - 在Page.Resources中注册转换器
  - 在时段显示中使用转换器
  - _Requirements: 5.2_
