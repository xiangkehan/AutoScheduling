# 实施计划

## 任务列表

- [x] 1. 修改数据模型层验证规则







  - 移除 `CreatePositionDto` 和 `UpdatePositionDto` 中的技能必选验证
  - _需求: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 1.1 移除 CreatePositionDto 的验证属性




  - 在 `DTOs/PositionDto.cs` 中找到 `CreatePositionDto` 类
  - 移除 `RequiredSkillIds` 字段的 `[Required]` 验证属性
  - 移除 `RequiredSkillIds` 字段的 `[MinLength(1)]` 验证属性
  - 保持字段初始化为空列表：`= new()`
  - _需求: 4.1, 4.2_



- [x] 1.2 移除 UpdatePositionDto 的验证属性

  - 在 `DTOs/PositionDto.cs` 中找到 `UpdatePositionDto` 类
  - 移除 `RequiredSkillIds` 字段的 `[Required]` 验证属性
  - 移除 `RequiredSkillIds` 字段的 `[MinLength(1)]` 验证属性
  - 保持字段初始化为空列表：`= new()`
  - _需求: 4.3, 4.4_
-

- [x] 2. 修改 UI 层验证逻辑





  - 移除 `PositionViewModel` 中的技能必选验证
  - _需求: 5.1, 5.2, 5.3_

- [x] 2.1 移除 CreatePositionAsync 方法中的验证




  - 在 `ViewModels/DataManagement/PositionViewModel.cs` 中找到 `CreatePositionAsync` 方法
  - 移除检查 `RequiredSkillIds` 是否为空的验证代码块（约第155-160行）
  - 移除对应的错误提示："至少需要选择一项所需技能"
  - 保留其他验证（名称、地点等）
  - _需求: 5.1, 5.2_

- [x] 3. 修改 UI 显示逻辑




  - 在哨位详情页面添加"无技能要求"的显示
  - _需求: 1.3, 5.4_

- [x] 3.1 更新 PositionPage.xaml 的技能显示部分


  - 在 `Views/DataManagement/PositionPage.xaml` 中找到技能显示的 `StackPanel`（约第120-135行）
  - 为 `ItemsRepeater` 添加 `Visibility` 绑定，当技能数量大于0时显示
  - 添加新的 `TextBlock` 显示"无技能要求"，当技能数量等于0时显示
  - 使用 `IntToVisibilityConverter` 进行条件显示
  - 设置"无技能要求"文本的样式：灰色、斜体
  - _需求: 1.3, 5.4_

- [ ]* 4. 添加单元测试
  - 为修改的功能添加单元测试
  - _需求: 4.5, 5.2_

- [ ]* 4.1 添加 DTO 验证测试
  - 创建测试方法验证 `CreatePositionDto` 允许空技能列表
  - 创建测试方法验证 `UpdatePositionDto` 允许空技能列表
  - 验证其他必填字段（名称、地点）仍然生效
  - _需求: 4.5_

- [ ]* 4.2 添加 ConstraintValidator 测试
  - 创建测试方法验证 `ValidateSkillMatch` 对空技能列表返回 true
  - 验证有技能要求时的正常匹配逻辑不受影响
  - _需求: 3.4_

- [ ]* 5. 添加集成测试
  - 测试完整的创建和排班流程
  - _需求: 1.1, 1.2, 3.1, 3.2, 3.3_

- [ ]* 5.1 测试创建无技能要求的哨位
  - 创建测试方法调用 `PositionService.CreateAsync` 创建无技能哨位
  - 验证创建成功且 `RequiredSkillIds` 为空列表
  - 验证可以正常查询和更新该哨位
  - _需求: 1.1, 1.2_

- [ ]* 5.2 测试排班功能
  - 创建包含无技能要求哨位的排班场景
  - 验证排哨引擎将所有可用人员视为符合条件
  - 验证排班结果正确分配人员到无技能要求的哨位
  - _需求: 3.1, 3.2, 3.3_

- [x] 6. 验证向后兼容性





  - 确保现有功能不受影响
  - _需求: 1.4_

- [x] 6.1 测试现有哨位数据


  - 查询现有有技能要求的哨位
  - 验证显示和编辑功能正常
  - 验证排班功能正常
  - _需求: 1.4_

- [x] 6.2 测试编辑功能

  - 编辑现有哨位，移除所有技能要求
  - 验证保存成功
  - 验证显示为"无技能要求"
  - 再次编辑，添加技能要求
  - 验证恢复正常显示
  - _需求: 1.4_
