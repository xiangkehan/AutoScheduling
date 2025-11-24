# 实现计划

- [x] 1. 创建冲突管理数据模型和枚举
  - 扩展 ConflictDto 类，添加新字段（ID、SubType、详细信息、相关班次ID、严重程度、忽略状态等）
  - 创建 ConflictSubType 枚举（技能不匹配、人员不可用、休息时间不足等）
  - 创建 ConflictStatistics 类（统计硬约束、软约束、未分配时段数量）
  - 创建 ConflictResolutionOption 类（修复方案数据模型）
  - 创建 ResolutionType 枚举（替换人员、取消分配、调整时间等）
  - 创建 ResolutionImpact 类（修复方案影响评估）
  - 创建 ConflictTrendData 类（趋势分析数据）
  - _Requirements: 1.1, 1.9, 2.1, 2.2, 4.1, 10.1_

- [ ]* 1.1 为冲突数据模型编写属性测试
  - **Property 3: 冲突信息完整性**
  - **Validates: Requirements 2.2, 2.3, 2.4, 2.5, 2.6**

- [x] 2. 实现冲突检测服务接口和基础实现
  - 创建 IConflictDetectionService 接口
  - 创建 ConflictDetectionService 基础类
  - 实现 DetectConflictsAsync 方法（协调所有检测器）
  - 实现 GetConflictStatistics 方法（生成统计信息）
  - 添加人员和哨位信息缓存机制
  - _Requirements: 1.1, 2.1_

- [x] 3. 实现各类冲突检测器
- [x] 3.1 实现技能不匹配检测
  - 实现 DetectSkillMismatchAsync 方法
  - 查询哨位要求的技能和人员拥有的技能
  - 识别缺少的必需技能
  - 生成详细的冲突信息
  - _Requirements: 1.2_

- [ ]* 3.2 为技能不匹配检测编写属性测试
  - **Property 1: 冲突检测完整性（技能不匹配部分）**
  - **Validates: Requirements 1.2**

- [x] 3.3 实现人员不可用检测
  - 实现 DetectPersonnelUnavailableAsync 方法
  - 检查人员的可用性状态（请假、休假等）
  - 生成冲突信息
  - _Requirements: 1.3_

- [ ]* 3.4 为人员不可用检测编写属性测试
  - **Property 1: 冲突检测完整性（人员不可用部分）**
  - **Validates: Requirements 1.3**

- [x] 3.5 实现休息时间不足检测
  - 实现 DetectInsufficientRestAsync 方法
  - 按人员分组计算班次间隔
  - 识别少于最小休息时间的情况
  - 生成冲突信息
  - _Requirements: 1.5_

- [ ]* 3.6 为休息时间不足检测编写属性测试
  - **Property 1: 冲突检测完整性（休息时间部分）**
  - **Validates: Requirements 1.5**

- [x] 3.7 实现工作量不均衡检测
  - 实现 DetectWorkloadImbalanceAsync 方法
  - 计算每个人员的工作量
  - 识别超出平均值一定比例的情况
  - 生成冲突信息
  - _Requirements: 1.6_

- [ ]* 3.8 为工作量不均衡检测编写属性测试
  - **Property 1: 冲突检测完整性（工作量部分）**
  - **Validates: Requirements 1.6**

- [x] 3.9 实现连续工作超时检测
  - 实现 DetectConsecutiveOvertimeAsync 方法
  - 识别连续工作时间过长的情况
  - 生成冲突信息
  - _Requirements: 1.4_

- [ ]* 3.10 为连续工作超时检测编写属性测试
  - **Property 1: 冲突检测完整性（连续工作部分）**
  - **Validates: Requirements 1.4**

- [x] 3.11 实现未分配时段检测
  - 实现 DetectUnassignedSlotsAsync 方法
  - 识别所有哨位在所有时段的分配情况
  - 找出未分配的时段
  - 生成信息提示
  - _Requirements: 1.7_

- [ ]* 3.12 为未分配时段检测编写属性测试
  - **Property 1: 冲突检测完整性（未分配部分）**
  - **Validates: Requirements 1.7**

- [x] 3.13 实现重复分配检测
  - 实现 DetectDuplicateAssignmentsAsync 方法
  - 识别同一人员在同一时段被分配到多个哨位的情况
  - 生成冲突信息
  - _Requirements: 1.8**

- [ ]* 3.14 为重复分配检测编写属性测试
  - **Property 1: 冲突检测完整性（重复分配部分）**
  - **Validates: Requirements 1.8**

- [ ]* 3.15 为冲突分类编写属性测试
  - **Property 2: 冲突分类正确性**
  - **Validates: Requirements 1.9**

- [ ]* 3.16 为冲突统计编写属性测试
  - **Property 4: 冲突统计准确性**
  - **Validates: Requirements 2.1**


- [ ] 4. 实现冲突修复服务
- [ ] 4.1 创建冲突修复服务接口和基础实现
  - 创建 IConflictResolutionService 接口
  - 创建 ConflictResolutionService 基础类
  - 实现 ValidateResolutionAsync 方法（验证方案有效性）
  - 实现 EvaluateImpactAsync 方法（评估方案影响）
  - _Requirements: 4.1, 4.7_

- [ ] 4.2 实现技能不匹配修复方案生成
  - 实现 GenerateSkillMismatchResolutionsAsync 方法
  - 查找具有匹配技能的可用人员
  - 按工作量排序候选人员
  - 生成替换人员方案
  - 生成取消分配方案
  - _Requirements: 4.1_

- [ ] 4.3 实现工作量不均衡修复方案生成
  - 实现 GenerateWorkloadImbalanceResolutionsAsync 方法
  - 识别工作量较低的人员
  - 生成重新分配班次方案
  - _Requirements: 4.2_

- [ ] 4.4 实现未分配时段修复方案生成
  - 实现 GenerateUnassignedSlotResolutionsAsync 方法
  - 查找可用且技能匹配的人员
  - 按工作量和休息时间排序
  - 生成推荐人员列表
  - _Requirements: 4.3_

- [ ] 4.5 实现休息时间不足修复方案生成
  - 实现 GenerateInsufficientRestResolutionsAsync 方法
  - 生成调整班次时间方案
  - 生成替换人员方案
  - _Requirements: 4.4_

- [ ] 4.6 实现修复方案应用逻辑
  - 实现 ApplyResolutionAsync 方法
  - 根据方案类型执行相应操作（替换人员、取消分配等）
  - 更新排班数据
  - 重新执行冲突检测
  - _Requirements: 4.8, 4.9_

- [ ]* 4.7 为修复方案生成编写属性测试
  - **Property 8: 修复方案有效性**
  - **Validates: Requirements 4.8, 4.9**

- [ ]* 4.8 为影响评估编写属性测试
  - **Property 9: 修复方案影响评估准确性**
  - **Validates: Requirements 4.7**

- [x] 5. 实现冲突报告服务
- [x] 5.1 创建冲突报告服务接口和基础实现
  - 创建 IConflictReportService 接口
  - 创建 ConflictReportService 基础类
  - _Requirements: 6.1_

- [ ] 5.2 实现 Excel 格式冲突报告导出
  - 实现 ExportConflictReportAsync 方法（Excel格式）
  - 生成冲突统计摘要工作表
  - 生成冲突详细信息工作表（按类型分组）
  - 添加排班基本信息
  - 应用格式化样式
  - _Requirements: 6.2, 6.3, 6.4, 6.5, 6.6_

- [ ] 5.3 实现 PDF 格式冲突报告导出
  - 实现 ExportConflictReportAsync 方法（PDF格式）
  - 生成报告标题和基本信息
  - 生成冲突统计摘要
  - 生成冲突详细列表（按类型分组）
  - 添加页眉页脚
  - _Requirements: 6.2, 6.3, 6.4, 6.5, 6.7_

- [ ] 5.4 实现冲突趋势数据生成
  - 实现 GenerateTrendDataAsync 方法
  - 按日期统计冲突数量
  - 按类型统计冲突分布
  - 计算解决率统计
  - _Requirements: 10.2, 10.3, 10.4_

- [ ]* 5.5 为冲突报告导出编写属性测试
  - **Property 12: 冲突报告完整性**
  - **Validates: Requirements 6.2, 6.3, 6.4, 6.5**

- [ ]* 5.6 为趋势统计编写属性测试
  - **Property 17: 趋势统计准确性**
  - **Validates: Requirements 10.4**

- [x] 6. 扩展 ScheduleResultViewModel 的冲突管理功能
- [x] 6.1 创建 ScheduleResultViewModel.Conflicts.cs partial class
  - 添加冲突相关属性（AllConflicts、FilteredConflicts、ConflictStatistics等）
  - 添加筛选和排序属性（ConflictTypeFilter、ConflictSeverityFilter等）
  - 添加高亮状态属性（HighlightedCellKeys）
  - _Requirements: 2.1, 8.1, 8.3, 8.5_

- [x] 6.2 实现冲突检测命令
  - 注入 IConflictDetectionService 依赖
  - 实现 DetectConflictsAsync 方法
  - 实现 RefreshConflictsCommand
  - 在加载排班时自动执行冲突检测
  - _Requirements: 1.1, 7.1_

- [x] 6.3 实现冲突筛选和排序逻辑
  - 实现 ApplyConflictFilters 方法
  - 按类型筛选
  - 按严重程度筛选
  - 按搜索文本筛选
  - 按指定方式排序
  - _Requirements: 8.2, 8.4, 8.6, 8.7_

- [ ]* 6.4 为冲突筛选编写属性测试
  - **Property 14: 冲突筛选正确性**
  - **Validates: Requirements 8.2, 8.4, 8.7**

- [ ]* 6.5 为冲突排序编写属性测试
  - **Property 15: 冲突排序一致性**
  - **Validates: Requirements 8.6**

- [x] 6.6 实现冲突定位命令
  - 实现 LocateConflictCommand
  - 根据冲突相关的班次ID找到对应单元格
  - 更新 HighlightedCellKeys 集合
  - 清除之前的高亮
  - 触发UI滚动到高亮单元格
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.6_

- [ ]* 6.7 为冲突定位编写属性测试
  - **Property 6: 冲突定位准确性**
  - **Property 7: 高亮状态唯一性**
  - **Validates: Requirements 3.1, 3.2, 3.3, 3.6**

- [x] 6.8 实现冲突忽略命令
  - 实现 IgnoreConflictCommand
  - 更新冲突的 IsIgnored 状态
  - 更新冲突统计信息
  - 实现 UnignoreConflictCommand（取消忽略）
  - 实现 IgnoreAllSoftConflictsCommand（忽略所有软约束）
  - 验证硬约束不可忽略
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

- [ ]* 6.9 为冲突忽略编写属性测试
  - **Property 10: 忽略状态持久性**
  - **Property 11: 硬约束不可忽略**
  - **Validates: Requirements 5.6, 5.7**

- [x] 6.10 实现冲突修复命令
  - 注入 IConflictResolutionService 依赖
  - 实现 FixConflictCommand
  - 打开修复对话框
  - 显示修复方案列表
  - 应用选定的修复方案
  - 重新检测冲突
  - _Requirements: 4.5, 4.6, 4.8, 4.9_

- [x] 6.11 实现冲突报告导出命令
  - 注入 IConflictReportService 依赖
  - 实现 ExportConflictReportCommand
  - 显示格式选择对话框
  - 生成报告文件
  - 保存文件
  - _Requirements: 6.1, 6.8_

- [x] 6.12 实现冲突趋势显示命令
  - 实现 ShowConflictTrendCommand
  - 打开趋势对话框
  - 加载趋势数据
  - 显示图表
  - _Requirements: 10.1, 10.5_

- [x] 6.13 实现实时更新逻辑
  - 监听排班修改事件
  - 自动重新执行冲突检测
  - 更新冲突列表和统计信息
  - 显示更新动画
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ]* 6.14 为实时更新编写属性测试
  - **Property 13: 实时更新响应性**
  - **Validates: Requirements 7.1, 7.2, 7.3, 7.4**


- [x] 7. 增强冲突面板 UI
- [x] 7.1 更新 ScheduleResultPage.xaml 的冲突面板
  - 添加冲突统计卡片（硬约束、软约束、未分配、已忽略）
  - 添加筛选和排序控件（类型筛选器、严重程度筛选器、排序选择器、搜索框）
  - 更新冲突列表显示（按类型分组、显示详细信息）
  - 为每个冲突项添加操作按钮（定位、忽略/取消忽略、修复）
  - 添加底部操作按钮（全部忽略软约束、导出报告、冲突趋势）
  - _Requirements: 2.1, 2.8, 8.1, 8.3, 8.5_

- [x] 7.2 实现冲突列表项模板
  - 根据冲突类型显示不同图标和颜色
  - 显示冲突描述和详细信息
  - 显示相关人员和哨位信息
  - 显示日期和时段信息
  - 显示忽略状态
  - _Requirements: 2.2, 2.3, 2.4, 2.5, 2.6, 5.2_

- [x] 7.3 实现冲突高亮显示
  - 在表格单元格上绑定高亮状态
  - 根据 HighlightedCellKeys 应用高亮样式
  - 实现滚动到高亮单元格的逻辑
  - _Requirements: 3.4, 3.5_

- [x] 7.4 在表格单元格上显示冲突图标
  - 为有硬约束冲突的单元格显示红色警告图标
  - 为有软约束冲突的单元格显示黄色警告图标
  - 为未分配的单元格显示灰色提示图标
  - 在工具提示中显示冲突摘要
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [ ]* 7.5 为视觉提示编写属性测试
  - **Property 16: 视觉提示准确性**
  - **Validates: Requirements 9.1, 9.2, 9.3**

- [x] 8. 创建冲突修复对话框
- [x] 8.1 创建 ConflictResolutionDialog.xaml
  - 显示冲突详情（类型、人员、哨位、时间、问题描述）
  - 显示修复方案列表（单选）
  - 为每个方案显示标题、描述、优缺点、影响
  - 标识推荐方案
  - 添加应用和取消按钮
  - _Requirements: 4.5, 4.6, 4.7_

- [x] 8.2 创建 ConflictResolutionDialog.xaml.cs
  - 实现对话框初始化逻辑
  - 加载修复方案
  - 处理方案选择
  - 验证方案有效性
  - 返回选定的方案
  - _Requirements: 4.5, 4.6_

- [x] 9. 创建冲突趋势对话框
- [x] 9.1 创建 ConflictTrendDialog.xaml
  - 添加时间范围选择器
  - 显示冲突数量趋势图表（折线图）
  - 显示冲突类型分布图表（饼图或条形图）
  - 显示解决率统计（总数、已解决、已忽略、待处理）
  - 添加导出报告按钮
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.6_

- [x] 9.2 创建 ConflictTrendDialog.xaml.cs
  - 实现对话框初始化逻辑
  - 加载趋势数据
  - 渲染图表
  - 处理时间范围筛选
  - 处理导出操作
  - _Requirements: 10.1, 10.5, 10.6_

- [x] 10. 注册服务和依赖注入
- [x] 10.1 更新 ServiceCollectionExtensions.cs
  - 注册 IConflictDetectionService
  - 注册 IConflictResolutionService
  - 注册 IConflictReportService
  - 更新 ScheduleResultViewModel 的构造函数注入
  - _Requirements: 所有_

- [ ] 11. 集成测试和验证
- [ ] 11.1 测试完整的冲突检测流程
  - 创建包含各种冲突的测试排班数据
  - 验证所有冲突类型都被正确检测
  - 验证冲突统计信息准确
  - 验证冲突分类正确
  - _Requirements: 1.1-1.9_

- [ ] 11.2 测试冲突定位功能
  - 选择不同类型的冲突
  - 验证相关单元格被正确高亮
  - 验证表格自动滚动到高亮位置
  - 验证切换冲突时高亮正确更新
  - _Requirements: 3.1-3.7_

- [ ] 11.3 测试冲突修复功能
  - 为不同类型的冲突生成修复方案
  - 验证修复方案的有效性
  - 应用修复方案
  - 验证冲突被解决
  - 验证影响评估准确
  - _Requirements: 4.1-4.9_

- [ ] 11.4 测试冲突忽略功能
  - 忽略软约束冲突
  - 验证忽略状态正确更新
  - 验证统计信息更新
  - 尝试忽略硬约束冲突（应失败）
  - 取消忽略
  - _Requirements: 5.1-5.7_

- [ ] 11.5 测试冲突报告导出
  - 导出 Excel 格式报告
  - 导出 PDF 格式报告
  - 验证报告内容完整
  - 验证报告格式正确
  - _Requirements: 6.1-6.8_

- [ ] 11.6 测试冲突筛选和排序
  - 按类型筛选
  - 按严重程度筛选
  - 按搜索文本筛选
  - 按不同方式排序
  - 验证筛选和排序结果正确
  - _Requirements: 8.1-8.7_

- [ ] 11.7 测试实时更新
  - 修改排班数据
  - 验证冲突自动重新检测
  - 验证冲突列表自动更新
  - 验证统计信息自动更新
  - _Requirements: 7.1-7.6_

- [ ] 11.8 测试冲突趋势分析
  - 生成趋势数据
  - 验证按日期统计正确
  - 验证按类型统计正确
  - 验证解决率计算正确
  - _Requirements: 10.1-10.6_

- [ ] 12. 最终检查点 - 确保所有测试通过
  - 确保所有测试通过，如有问题请向用户询问

