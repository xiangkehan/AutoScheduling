# 实现计划

- [x] 1. 修改 ViewModelBase 支持可选的全局加载指示器





  - 在 `ViewModels/Base/ViewModelBase.cs` 中为 `ExecuteAsync` 方法添加 `showGlobalLoading` 可选参数（默认值为 `true`）
  - 只有当 `showGlobalLoading` 为 `true` 时才设置 `IsBusy` 属性
  - 确保向后兼容，现有调用不受影响
  - _需求: 1.5_

- [x] 2. 在 PositionViewModel 中实现增量更新方法





  - [x] 2.1 添加 AddPersonnelToAvailableList 私有方法


    - 接收 `PersonnelDto` 参数
    - 将人员添加到 `AvailablePersonnel` 集合
    - 确保在UI线程上执行
    - _需求: 1.1, 1.4_

  - [x] 2.2 添加 RemovePersonnelFromAvailableList 私有方法

    - 接收 `personnelId` 参数
    - 从 `AvailablePersonnel` 集合中查找并移除对应人员
    - 确保在UI线程上执行
    - _需求: 1.2, 1.4_

  - [x] 2.3 添加 UpdateSelectedItemPersonnelIds 私有方法

    - 接收 `personnelId` 和 `isAdding` 参数
    - 更新 `SelectedItem.AvailablePersonnelIds` 列表
    - 更新 `SelectedItem.AvailablePersonnelNames` 列表
    - _需求: 1.1, 1.2, 5.5_


- [x] 3. 重构 AddPersonnelAsync 方法使用增量更新




  - 移除 `LoadDataAsync()` 调用
  - 移除恢复选中哨位的逻辑（不再需要）
  - 在服务API调用成功后，调用 `AddPersonnelToAvailableList`
  - 调用 `UpdateSelectedItemPersonnelIds` 更新哨位的人员ID列表
  - 调用 `NotifyCanExecuteChanged` 更新命令状态
  - 使用 `ExecuteAsync` 时传入 `showGlobalLoading: false`
  - _需求: 1.1, 1.3, 1.4, 1.5, 2.1, 2.5, 5.3, 5.4, 5.5_
-

- [x] 4. 重构 RemovePersonnelAsync 方法使用增量更新




  - 移除 `LoadDataAsync()` 调用
  - 移除恢复选中哨位的逻辑（不再需要）
  - 在服务API调用成功后，调用 `RemovePersonnelFromAvailableList`
  - 调用 `UpdateSelectedItemPersonnelIds` 更新哨位的人员ID列表
  - 清除 `SelectedPersonnel` 选择
  - 调用 `NotifyCanExecuteChanged` 更新命令状态
  - 使用 `ExecuteAsync` 时传入 `showGlobalLoading: false`
  - _需求: 1.2, 1.3, 1.4, 1.5, 2.2, 2.5, 5.3, 5.4, 5.5_


- [x] 5. 在 XAML 中添加动画资源




  - 在 `Views/DataManagement/PositionPage.xaml` 的 `Page.Resources` 中添加 `FadeInStoryboard`
  - 配置淡入动画：从 Opacity 0.0 到 1.0，持续时间 150ms
  - 使用 `CubicEase` 缓动函数，`EasingMode="EaseOut"`
  - 为可用人员 ListView 添加 `x:Name="AvailablePersonnelListView"`
  - _需求: 3.1, 3.3, 3.5_

- [x] 6. 在代码后置中实现动画触发逻辑




  - 在 `Views/DataManagement/PositionPage.xaml.cs` 的构造函数中订阅 `ViewModel.AvailablePersonnel.CollectionChanged` 事件
  - 实现 `OnAvailablePersonnelCollectionChanged` 方法处理集合变化
  - 当检测到 `NotifyCollectionChangedAction.Add` 时，为新项触发淡入动画
  - 实现 `TriggerFadeInAnimation` 方法：使用 `ContainerFromItem` 获取 ListViewItem，应用 FadeInStoryboard
  - 添加适当的延迟（10ms）等待UI更新
  - _需求: 3.1, 3.2, 3.3_

- [ ]* 7. 添加单元测试验证增量更新逻辑
  - 在 `Tests` 文件夹创建 `PositionViewModelIncrementalUpdateTests.cs`
  - 测试 `AddPersonnelAsync` 正确更新 `AvailablePersonnel` 集合
  - 测试 `RemovePersonnelAsync` 正确更新 `AvailablePersonnel` 集合
  - 测试 `AddPersonnelAsync` 正确更新 `SelectedItem.AvailablePersonnelIds`
  - 测试 `RemovePersonnelAsync` 正确更新 `SelectedItem.AvailablePersonnelIds`
  - 测试操作不调用 `LoadDataAsync` 或 `GetAllAsync`
  - 测试命令状态正确更新
  - _需求: 1.1, 1.2, 2.1, 2.2, 5.3, 5.4_

- [ ]* 8. 添加性能测试验证优化效果
  - 在 `Tests` 文件夹创建 `PositionViewModelPerformanceTests.cs`
  - 测试大数据集（100+人员）下的添加操作性能
  - 测试大数据集下的移除操作性能
  - 验证操作在500ms内完成
  - 使用 `Stopwatch` 测量执行时间
  - _需求: 5.1, 5.3, 5.4_
