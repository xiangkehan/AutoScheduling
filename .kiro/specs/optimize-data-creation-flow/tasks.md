# 实施计划

- [x] 1. 实现技能创建对话框





  - 在SkillPage.xaml.cs中创建ShowCreateSkillDialogAsync方法
  - 构建ContentDialog的UI结构（名称输入框、描述输入框）
  - 配置对话框的XamlRoot属性
  - 实现对话框的PrimaryButtonClick验证逻辑
  - 将对话框输入绑定到ViewModel.NewSkill
  - 修改SkillViewModel的CreateCommand以支持对话框流程
  - _需求: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7_

- [x] 2. 实现哨位创建对话框



  - 在PositionPage.xaml.cs中创建ShowCreatePositionDialogAsync方法
  - 构建ContentDialog的UI结构（名称、地点、描述、技能选择）
  - 配置对话框的XamlRoot属性
  - 实现技能多选ListView的SelectionChanged处理
  - 实现对话框的PrimaryButtonClick验证逻辑
  - 将对话框输入绑定到ViewModel.NewPosition
  - 修改PositionViewModel的CreateCommand以支持对话框流程
  - 处理无技能数据时的提示消息
  - _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8_

- [x] 3. 优化人员创建表单验证





  - 在PersonnelViewModel中添加IsNameValid和AreSkillsValid属性
  - 在PersonnelViewModel中添加NameValidationMessage和SkillsValidationMessage属性
  - 实现ValidateName方法检查姓名是否为空和长度限制
  - 实现ValidateSkills方法检查是否至少选择一项技能
  - 添加CanCreate计算属性基于验证状态
  - 修改CreateCommand的CanExecute使用CanCreate属性
  - 在PersonnelPage.xaml中添加验证错误消息TextBlock
  - 在PersonnelPage.xaml.cs中实现NameTextBox_TextChanged调用验证
  - 在PersonnelPage.xaml.cs中更新SkillsListView_SelectionChanged调用验证
  - _需求: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_
-

- [x] 4. 实现创建成功后自动选中新记录




  - 修改PersonnelViewModel.CreatePersonnelAsync在创建后设置SelectedItem
  - 修改SkillViewModel.CreateSkillAsync在创建后设置SelectedItem
  - 修改PositionViewModel.CreatePositionAsync在创建后设置SelectedItem
  - 在PersonnelPage.xaml.cs中实现ScrollIntoView逻辑
  - 在SkillPage.xaml.cs中实现ScrollIntoView逻辑
  - 在PositionPage.xaml.cs中实现ScrollIntoView逻辑
  - _需求: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

- [x] 5. 改进技能选择控件的视觉反馈




  - 在PersonnelPage.xaml的技能ListView中确保SelectionMode="Multiple"
  - 在PositionPage对话框的技能ListView中确保SelectionMode="Multiple"
  - 验证ListView的ItemTemplate正确显示技能名称
  - 确保选中项有明显的视觉高亮
  - 在PersonnelPage.xaml.cs中实现EditSkillsListView_SelectionChanged时恢复已选技能
  - 添加选中技能数量的显示（可选）
  - _需求: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

- [x] 6. 实现智能表单重置




  - 在PersonnelViewModel中创建ResetCreateForm方法
  - ResetCreateForm方法清空NewPersonnel的Name属性
  - ResetCreateForm方法清空NewPersonnel的SkillIds列表
  - ResetCreateForm方法重置IsAvailable为true
  - ResetCreateForm方法重置验证状态属性
  - 在CreatePersonnelAsync成功后调用ResetCreateForm
  - 在PersonnelPage.xaml.cs的ResetForm_Click中清除ListView选择
  - 在PersonnelPage.xaml.cs的ResetForm_Click中聚焦到姓名输入框
  - _需求: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

- [x] 7. 确保对话框XamlRoot正确配置





  - 在SkillPage的ShowCreateSkillDialogAsync中设置dialog.XamlRoot = this.XamlRoot
  - 在PositionPage的ShowCreatePositionDialogAsync中设置dialog.XamlRoot = this.XamlRoot
  - 添加XamlRoot为null的检查和错误处理
  - 在XamlRoot为null时记录错误日志
  - 在XamlRoot为null时使用DialogService显示错误消息
  - 添加try-catch包裹ShowAsync调用处理异常
  - _需求: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_


- [x] 8. 实现错误处理和用户反馈




  - 在PersonnelViewModel.CreatePersonnelAsync中添加try-catch错误处理
  - 在SkillViewModel.CreateSkillAsync中添加try-catch错误处理
  - 在PositionViewModel.CreatePositionAsync中添加try-catch错误处理
  - 在验证失败时显示具体的验证错误消息
  - 在网络或数据库错误时显示用户友好的错误消息
  - 确保所有错误都通过DialogService.ShowErrorAsync显示
  - _需求: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

- [x] 9. 实现对话框的键盘可访问性



  - 在技能创建对话框中设置DefaultButton="Primary"
  - 在哨位创建对话框中设置DefaultButton="Primary"
  - 在对话框打开后自动聚焦第一个输入框
  - 确保Tab键可以在对话框输入框间导航
  - 确保Shift+Tab支持反向导航
  - 确保Escape键可以关闭对话框
  - 为所有输入框添加AutomationProperties.Name
  - 为必填字段在Header中添加星号标记
  - _需求: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_

- [x] 10. 优化数据刷新和列表更新





  - 确保PersonnelViewModel.CreatePersonnelAsync使用AddItem而非重新加载
  - 确保SkillViewModel.CreateSkillAsync使用AddItem而非重新加载
  - 确保PositionViewModel.CreatePositionAsync使用AddItem而非重新加载
  - 验证ObservableCollection的CollectionChanged事件正确触发
  - 测试新项添加后列表UI立即更新
  - 测试自动选中新项后滚动到可见位置
  - _需求: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6_
