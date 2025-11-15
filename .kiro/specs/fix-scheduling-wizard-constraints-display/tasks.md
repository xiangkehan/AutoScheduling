# Implementation Plan

- [x] 1. 改进 SchedulingViewModel 的约束加载逻辑





  - 在 `LoadConstraintsAsync` 方法中添加详细的调试日志
  - 添加重复加载检查（使用 `IsLoadingConstraints` 标志）
  - 记录加载开始、完成和数据数量
  - 添加空数据警告日志
  - 改进异常处理，记录详细的错误信息和堆栈跟踪
  - _Requirements: 1.1, 1.2, 2.1, 2.2, 5.1, 5.2, 5.3, 5.4_

- [x] 2. 优化约束数据加载时机




  - 修改 `NextStep` 方法，在进入第4步时自动触发约束加载
  - 修改 `OnCurrentStepChanged` 方法，添加步骤变更日志和条件加载逻辑
  - 修改 `OnStartDateChanged` 和 `OnEndDateChanged` 方法，在日期变化且已在第4步时重新加载约束
  - 确保加载逻辑不会重复执行
  - _Requirements: 1.1, 3.1, 3.2_

- [x] 3. 改进模板加载时的约束应用





  - 在 `LoadTemplateAsync` 方法中添加约束应用的日志
  - 确保 `ApplyTemplateConstraints` 在约束数据加载完成后调用
  - 验证模板中的约束ID在数据库中存在
  - 添加缺失约束的警告提示
  - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [x] 4. 更新 CreateSchedulingPage.xaml UI





  - 在第4步添加加载指示器，绑定到 `IsLoadingConstraints`
  - 为休息日配置添加空状态提示（使用 InfoBar）
  - 为定岗规则添加空状态文本提示
  - 为手动指定添加空状态文本提示
  - 使用 `IntToVisibilityConverter` 控制空状态和内容的显示
  - 确保所有绑定使用 `Mode=OneWay` 或 `Mode=TwoWay`
  - _Requirements: 1.3, 1.4, 1.5, 2.3_

- [x] 5. 添加错误处理和用户反馈




  - 在 `LoadConstraintsAsync` 中添加特定异常类型的处理（SqliteException, JsonException, TaskCanceledException）
  - 为每种错误类型提供友好的错误消息
  - 确保错误对话框包含具体的错误信息
  - 在 finally 块中确保 `IsLoadingConstraints` 被重置
  - _Requirements: 2.1, 2.2, 2.3_

- [x] 6. 验证数据库架构和初始化




  - 检查 DatabaseSchema.cs 中约束表的定义是否正确
  - 验证 DatabaseService 是否正确创建约束表
  - 确保约束表的索引已创建
  - 测试 ConstraintRepository 的所有 CRUD 方法
  - _Requirements: 1.1, 1.3, 1.4, 1.5_

- [ ] 7. 添加性能监控和诊断工具
  - 在 `LoadConstraintsAsync` 中添加 Stopwatch 计时
  - 记录约束数据加载的耗时
  - 添加详细的步骤日志（开始、完成、数据量）
  - 确保日志格式统一且易于阅读
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [ ]* 8. 编写单元测试
  - 测试 `LoadConstraintsAsync` 正常加载场景
  - 测试数据库为空时的场景
  - 测试加载失败时的错误处理
  - 测试模板约束应用逻辑
  - 测试日期范围变化时的重新加载
  - _Requirements: 1.1, 2.1, 3.1, 4.1_

- [ ]* 9. 执行集成测试
  - 测试完整的约束数据加载流程（从数据库到 UI）
  - 测试不同日期范围的手动指定加载
  - 测试模板加载时的约束应用
  - 测试错误场景（数据库连接失败、数据格式错误）
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 3.1, 4.1_

- [ ]* 10. 执行 UI 手动测试
  - 测试正常加载场景（进入第4步，验证数据显示）
  - 测试空数据库场景（验证空状态提示）
  - 测试日期范围变化场景（验证数据更新）
  - 测试模板加载场景（验证约束自动启用）
  - 测试错误处理场景（验证错误对话框）
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 4.1, 4.2, 4.3, 4.4_
