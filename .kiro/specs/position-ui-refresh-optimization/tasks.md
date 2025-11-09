# 实现计划

- [x] 1. 修改 PositionDto 支持属性变更通知

  - 在 `DTOs/PositionDto.cs` 中让 `PositionDto` 类实现 `INotifyPropertyChanged` 接口
  - 为 `AvailablePersonnelIds` 属性添加 backing field `_availablePersonnelIds`
  - 在 setter 中触发 `PropertyChanged` 事件
  - 添加 `AvailablePersonnelCount` 计算属性（返回 `AvailablePersonnelIds.Count`）
  - 为 `AvailablePersonnelCount` 添加 `[JsonIgnore]` 特性
  - 在 `AvailablePersonnelIds` setter 中同时触发 `AvailablePersonnelCount` 的属性变更通知
  - 添加 `OnPropertyChanged` 保护方法（使用 `[CallerMemberName]`）
  - _需求: 1.1, 1.2, 1.4_
-

- [x] 2. 修改 XAML 中的哨位列表绑定


  - 在 `Views/DataManagement/PositionPage.xaml` 的哨位列表 ItemTemplate 中
  - 将 `{x:Bind AvailablePersonnelIds.Count}` 改为 `{x:Bind AvailablePersonnelCount, Mode=OneWay}`
  - 确保使用 `Mode=OneWay` 以支持属性变更通知
  - _需求: 1.1, 1.2, 1.4_

- [x] 3. 在 PositionViewModel 中添加列表项更新通知方法




  - 添加 `NotifyPositionListItemChanged` 私有方法
  - 接收 `PositionDto` 参数
  - 调用 `position.OnPropertyChanged(nameof(position.AvailablePersonnelCount))` 触发属性变更通知
  - _需求: 1.1, 1.2, 1.4_

- [x] 4. 在 AddPersonnelAsync 中添加列表项更新通知





  - 在调用 `UpdateSelectedItemPersonnelIds` 之后
  - 调用 `NotifyPositionListItemChanged(SelectedItem)` 触发哨位列表项的UI更新
  - 确保哨位列表中的"可用人员数量"能够及时更新
  - _需求: 1.1, 1.4_
-

- [x] 5. 在 RemovePersonnelAsync 中添加列表项更新通知




  - 在调用 `UpdateSelectedItemPersonnelIds` 之后
  - 调用 `NotifyPositionListItemChanged(SelectedItem)` 触发哨位列表项的UI更新
  - 确保哨位列表中的"可用人员数量"能够及时更新
  - _需求: 1.2, 1.4_

- [ ]* 6. 验证增量更新功能
  - 运行应用程序
  - 选择一个哨位，查看当前可用人员数量
  - 添加一个人员到哨位
  - 验证哨位列表中的"可用人员数量"立即更新（+1）
  - 移除一个人员
  - 验证哨位列表中的"可用人员数量"立即更新（-1）
  - 验证没有全局加载指示器
  - 验证选中状态和滚动位置保持不变
  - _需求: 1.1, 1.2, 1.3, 1.4, 1.5_
