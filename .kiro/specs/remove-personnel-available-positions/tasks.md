# Implementation Plan

- [-] 1. 修改 DTO 层，移除可用哨位字段



  - 从 PersonnelDto、CreatePersonnelDto 和 UpdatePersonnelDto 中移除 AvailablePositionIds 和 AvailablePositionNames 属性
  - 确保所有验证特性（Attributes）保持正确
  - _需求: 1.1, 1.2, 1.3, 1.4_

- [ ] 2. 修改 Mapper 层，移除可用哨位映射逻辑

  - 更新 PersonnelMapper.ToDto() 方法，移除可用哨位字段的初始化
  - 更新 PersonnelMapper.ToDtoAsync() 方法，移除加载可用哨位信息的逻辑
  - 更新 PersonnelMapper.ToUpdateDto() 方法，移除 AvailablePositionIds 的复制
  - 更新 PersonnelMapper.ToCreateDto() 方法，移除 AvailablePositionIds 的复制
  - _需求: 1.1, 1.2, 1.3, 1.4_

- [ ] 3. 修改 Service 层接口和实现

- [ ] 3.1 更新 IPersonnelService 接口
  - 移除 GetAvailablePositionsAsync(int personnelId) 方法签名
  - _需求: 5.1, 5.2, 5.3, 5.4_

- [ ] 3.2 更新 PersonnelService 实现
  - 移除 IPositionRepository 依赖注入
  - 更新构造函数，移除 positionRepository 参数
  - 移除 GetAvailablePositionsAsync() 方法实现
  - _需求: 5.1, 5.2, 5.3, 5.4_

- [ ] 4. 修改 UI 层 XAML，移除可用哨位控件

- [ ] 4.1 修改添加人员表单
  - 移除可用哨位选择的 StackPanel
  - 移除 AvailablePositionsListView 控件
  - 调整表单布局和间距
  - _需求: 2.1, 2.2, 2.3, 2.4_

- [ ] 4.2 修改编辑人员表单
  - 移除可用哨位编辑的 StackPanel
  - 移除 EditAvailablePositionsListView 控件
  - 调整表单布局和间距
  - _需求: 3.1, 3.2, 3.3, 3.4_

- [ ] 4.3 修改人员详情显示
  - 移除可用哨位显示的 StackPanel
  - 移除可用哨位名称的 ItemsRepeater
  - 调整详情布局
  - _需求: 4.1, 4.2, 4.3, 4.4_

- [ ] 5. 修改 UI 层 Code-behind，移除可用哨位事件处理
- [ ] 5.1 移除事件处理方法
  - 移除 AvailablePositionsListView_SelectionChanged 方法
  - 移除 EditAvailablePositionsListView_SelectionChanged 方法
  - _需求: 2.1, 3.1_

- [ ] 5.2 更新现有方法
  - 更新 SyncEditSelections() 方法，移除同步哨位选择的逻辑
  - 更新 ResetForm_Click() 方法，移除清空哨位选择的逻辑
  - _需求: 2.1, 3.1_

- [ ] 6. 修改 ViewModel 层，移除可用哨位处理逻辑
- [ ] 6.1 更新 StartEditAsync 方法
  - 移除 AvailablePositionIds 的复制逻辑
  - 确保只复制实际存在的字段
  - _需求: 3.1, 3.2, 3.3, 3.4_

- [ ] 6.2 可选：清理 LoadDataAsync 方法
  - 评估是否需要保留加载哨位列表的逻辑
  - 如果确认不需要，移除相关代码
  - _需求: 2.1, 3.1_

- [ ] 7. 验证和测试
- [ ] 7.1 编译验证
  - 确保项目编译通过，无错误
  - 检查是否有遗漏的引用需要修复
  - _需求: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 5.4_

- [ ] 7.2 功能测试
  - 测试创建新人员功能（只填写姓名和技能）
  - 测试编辑现有人员功能（修改姓名、技能和状态）
  - 测试查看人员详情功能（验证不显示可用哨位）
  - 测试删除人员功能
  - 测试搜索人员功能
  - _需求: 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4_

- [ ]* 7.3 回归测试
  - 验证哨位管理功能正常（哨位仍可配置可用人员列表）
  - 验证排班算法正常（通过哨位的可用人员列表进行匹配）
  - 验证技能管理功能正常
  - _需求: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 5.4_
