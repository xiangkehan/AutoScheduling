# 设计文档

## 概述

本设计文档描述如何将 3012 行的 SchedulingViewModel.cs 拆分为多个职责单一的部分类文件。拆分策略基于功能模块划分,使用 C# 的 partial class 特性,确保每个文件的行数符合项目规范(不超过 1000 行),同时保持代码的可维护性和可读性。

## 架构设计

### 拆分策略

采用**按功能模块的部分类拆分**策略,将 SchedulingViewModel 拆分为以下文件:

```
ViewModels/Scheduling/
├── SchedulingViewModel.cs                          (主文件, ~400 行)
├── SchedulingViewModel.Properties.cs               (属性定义, ~300 行)
├── SchedulingViewModel.Commands.cs                 (命令定义, ~150 行)
├── SchedulingViewModel.Wizard.cs                   (向导流程, ~400 行)
├── SchedulingViewModel.ManualAssignment.cs         (手动指定管理, ~500 行)
├── SchedulingViewModel.PositionPersonnel.cs        (哨位人员管理, ~600 行)
├── SchedulingViewModel.TemplateConstraints.cs      (模板和约束, ~500 行)
├── SchedulingViewModel.StateManagement.cs          (状态管理和草稿, ~400 行)
└── SchedulingViewModel.Helpers.cs                  (辅助方法, ~300 行)
```

### 设计原则

1. **单一职责原则**: 每个文件只负责一个明确的功能领域
2. **最小化依赖**: 减少文件之间的相互依赖
3. **保持 API 稳定**: 不改变公共接口和属性
4. **便于测试**: 将复杂逻辑封装在独立方法中
5. **性能优化**: 保留现有的缓存机制和性能优化

## 组件设计

### 1. SchedulingViewModel.cs (主文件)

**职责**: 类定义、依赖注入、构造函数、核心初始化逻辑

**内容**:
- 命名空间和 using 语句
- 类声明 `public partial class SchedulingViewModel : ObservableObject`
- 依赖服务字段 (readonly)
- 管理器字段 (ManualAssignmentManager, PositionPersonnelManager)
- 缓存字段 (Dictionary)
- 构造函数
- 初始化方法 (LoadInitialDataAsync, BuildCaches)
- 缓存访问方法 (GetPersonnelFromCache, GetPositionFromCache)

**预估行数**: ~400 行

**关键代码结构**:
```csharp
namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel : ObservableObject
    {
        // 依赖服务
        private readonly ISchedulingService _schedulingService;
        private readonly IPersonnelService _personnelService;
        // ... 其他服务
        
        // 管理器
        private readonly ManualAssignmentManager _manualAssignmentManager;
        private readonly PositionPersonnelManager _positionPersonnelManager;
        
        // 缓存
        private readonly Dictionary<int, PersonnelDto> _personnelCache = new();
        private readonly Dictionary<int, PositionDto> _positionCache = new();
        
        // 构造函数
        public SchedulingViewModel(...)
        {
            // 依赖注入
            // 初始化管理器
            // 初始化命令
            // 注册事件处理
        }
        
        // 初始化方法
        private async Task LoadInitialDataAsync() { }
        private void BuildCaches() { }
        private PersonnelDto? GetPersonnelFromCache(int personnelId) { }
        private PositionDto? GetPositionFromCache(int positionId) { }
    }
}
```

### 2. SchedulingViewModel.Properties.cs (属性定义)

**职责**: 所有 ObservableProperty 属性和计算属性

**内容**:
- 步骤和基本信息属性 (CurrentStep, ScheduleTitle, StartDate, EndDate)
- 数据集合属性 (AvailablePersonnels, SelectedPersonnels, AvailablePositions, SelectedPositions)
- 约束相关属性 (HolidayConfigs, FixedPositionRules, ManualAssignments)
- 手动指定表单属性 (IsCreatingManualAssignment, NewManualAssignment, EditingManualAssignment)
- 手动指定包装属性 (NewManualAssignmentDate, EditingManualAssignmentPersonnelId 等)
- 哨位人员管理属性 (IsAddingPersonnelToPosition, CurrentEditingPosition)
- 手动添加人员属性 (IsManualAddingPersonnel, ManuallyAddedPersonnelIds)
- 状态属性 (IsExecuting, IsLoadingInitial, IsLoadingConstraints)
- 模板属性 (LoadedTemplateId, TemplateApplied)
- 结果属性 (ResultSchedule, SummarySections)
- 验证错误属性 (DateValidationError, PersonnelValidationError 等)
- 属性变更回调方法 (OnCurrentStepChanged, OnStartDateChanged 等)

**预估行数**: ~300 行

**关键代码结构**:
```csharp
namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel
    {
        // 步骤和基本信息
        [ObservableProperty]
        private int _currentStep = 1;
        
        [ObservableProperty]
        private string _scheduleTitle = string.Empty;
        
        // ... 其他属性
        
        // 包装属性
        public DateTimeOffset? NewManualAssignmentDate
        {
            get => NewManualAssignment != null ? new DateTimeOffset(NewManualAssignment.Date) : null;
            set { /* ... */ }
        }
        
        // 属性变更回调
        partial void OnCurrentStepChanged(int value) { }
        partial void OnStartDateChanged(DateTimeOffset value) { }
        // ... 其他回调
    }
}
```

### 3. SchedulingViewModel.Commands.cs (命令定义)

**职责**: 所有命令属性的定义

**内容**:
- 核心命令 (LoadDataCommand, NextStepCommand, PreviousStepCommand)
- 执行命令 (ExecuteSchedulingCommand, CancelCommand)
- 模板命令 (LoadTemplateCommand, SaveAsTemplateCommand)
- 约束命令 (LoadConstraintsCommand)
- 手动指定命令 (StartCreateManualAssignmentCommand, SubmitCreateManualAssignmentCommand 等)
- 哨位人员命令 (StartAddPersonnelToPositionCommand, SubmitAddPersonnelToPositionCommand 等)
- 手动添加人员命令 (StartManualAddPersonnelCommand, SubmitManualAddPersonnelCommand 等)

**预估行数**: ~150 行

**关键代码结构**:
```csharp
namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel
    {
        // 核心命令
        public IAsyncRelayCommand LoadDataCommand { get; }
        public IRelayCommand NextStepCommand { get; }
        public IRelayCommand PreviousStepCommand { get; }
        public IAsyncRelayCommand ExecuteSchedulingCommand { get; }
        public IRelayCommand CancelCommand { get; }
        
        // 模板和约束命令
        public IAsyncRelayCommand<int> LoadTemplateCommand { get; }
        public IAsyncRelayCommand LoadConstraintsCommand { get; }
        public IAsyncRelayCommand SaveAsTemplateCommand { get; }
        
        // 手动指定命令
        public IRelayCommand StartCreateManualAssignmentCommand { get; }
        public IAsyncRelayCommand SubmitCreateManualAssignmentCommand { get; }
        // ... 其他命令
    }
}
```

### 4. SchedulingViewModel.Wizard.cs (向导流程)

**职责**: 向导步骤导航、验证、执行排班、构建摘要

**内容**:
- 步骤导航方法 (NextStep, PreviousStep)
- 步骤验证方法 (ValidateStep1, ValidateStep2, ValidateStep3)
- 导航条件判断 (CanGoNext, CanExecuteScheduling)
- 命令状态刷新 (RefreshCommandStates)
- 执行排班 (ExecuteSchedulingAsync, BuildSchedulingRequest)
- 取消向导 (CancelWizard, ResetViewModelState, ShouldPromptForDraftSave)
- 构建摘要 (BuildSummarySections)

**预估行数**: ~400 行

**关键代码结构**:
```csharp
namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel
    {
        // 步骤导航
        private void NextStep() { }
        private void PreviousStep() { }
        private bool CanGoNext() { }
        
        // 步骤验证
        private bool ValidateStep1([NotNullWhen(false)] out string? error) { }
        private bool ValidateStep2([NotNullWhen(false)] out string? error) { }
        private bool ValidateStep3([NotNullWhen(false)] out string? error) { }
        
        // 执行排班
        private bool CanExecuteScheduling() { }
        private async Task ExecuteSchedulingAsync() { }
        private SchedulingRequestDto BuildSchedulingRequest() { }
        
        // 取消和重置
        private async void CancelWizard() { }
        private void ResetViewModelState() { }
        private bool ShouldPromptForDraftSave() { }
        
        // 摘要构建
        private void BuildSummarySections() { }
        
        // 命令状态
        private void RefreshCommandStates() { }
    }
}
```

### 5. SchedulingViewModel.ManualAssignment.cs (手动指定管理)

**职责**: 手动指定的 CRUD 操作、表单管理、验证

**内容**:
- 创建手动指定 (StartCreateManualAssignment, SubmitCreateManualAssignmentAsync, CancelCreateManualAssignment)
- 编辑手动指定 (StartEditManualAssignment, SubmitEditManualAssignmentAsync, CancelEditManualAssignment)
- 删除手动指定 (DeleteManualAssignmentAsync)
- 表单验证 (ValidateManualAssignment - 两个重载版本)
- 验证错误管理 (ClearValidationErrors)

**预估行数**: ~500 行

**关键代码结构**:
```csharp
namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel
    {
        #region 手动指定管理方法
        
        // 创建
        private void StartCreateManualAssignment() { }
        private async Task SubmitCreateManualAssignmentAsync() { }
        private void CancelCreateManualAssignment() { }
        
        // 编辑
        private void StartEditManualAssignment(ManualAssignmentViewModel? assignment) { }
        private async Task SubmitEditManualAssignmentAsync() { }
        private void CancelEditManualAssignment() { }
        
        // 删除
        private async Task DeleteManualAssignmentAsync(ManualAssignmentViewModel? assignment) { }
        
        // 验证
        private bool ValidateManualAssignment(CreateManualAssignmentDto dto, [NotNullWhen(false)] out string? error) { }
        private bool ValidateManualAssignment(UpdateManualAssignmentDto dto, [NotNullWhen(false)] out string? error) { }
        private void ClearValidationErrors() { }
        
        #endregion
    }
}
```

### 6. SchedulingViewModel.PositionPersonnel.cs (哨位人员管理)

**职责**: 哨位人员的添加、移除、保存、手动添加参与人员

**内容**:
- 为哨位添加人员 (StartAddPersonnelToPosition, SubmitAddPersonnelToPositionAsync, CancelAddPersonnelToPosition)
- 临时移除人员 (RemovePersonnelFromPosition)
- 撤销更改 (RevertPositionChanges)
- 保存为永久 (SavePositionChangesAsync, ShowSaveConfirmationDialog)
- 手动添加参与人员 (StartManualAddPersonnel, SubmitManualAddPersonnelAsync, CancelManualAddPersonnel, RemoveManualPersonnel)
- 人员提取和视图模型更新 (ExtractPersonnelFromPositions, UpdatePositionPersonnelViewModels)

**预估行数**: ~600 行

**关键代码结构**:
```csharp
namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel
    {
        #region 为哨位添加人员方法
        
        private void StartAddPersonnelToPosition(PositionDto? position) { }
        private async Task SubmitAddPersonnelToPositionAsync() { }
        private void CancelAddPersonnelToPosition() { }
        private void RemovePersonnelFromPosition((int positionId, int personnelId) parameters) { }
        private void RevertPositionChanges(int positionId) { }
        private async Task<bool> ShowSaveConfirmationDialog(PositionPersonnelChanges changes) { }
        private async Task SavePositionChangesAsync(int positionId) { }
        
        #endregion
        
        #region 手动添加参与人员方法
        
        private void StartManualAddPersonnel() { }
        private async Task SubmitManualAddPersonnelAsync() { }
        private void CancelManualAddPersonnel() { }
        private void RemoveManualPersonnel(int personnelId) { }
        
        #endregion
        
        #region 人员提取和视图模型更新
        
        private void ExtractPersonnelFromPositions() { }
        private void UpdatePositionPersonnelViewModels() { }
        
        #endregion
    }
}
```

### 7. SchedulingViewModel.TemplateConstraints.cs (模板和约束)

**职责**: 模板加载保存、约束数据加载、模板约束应用

**内容**:
- 加载约束数据 (LoadConstraintsAsync)
- 加载模板 (LoadTemplateAsync)
- 应用模板约束 (ApplyTemplateConstraints)
- 保存为模板 (SaveAsTemplateAsync, CanSaveTemplate)

**预估行数**: ~500 行

**关键代码结构**:
```csharp
namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel
    {
        // 约束加载
        private async Task LoadConstraintsAsync() { }
        
        // 模板管理
        private async Task LoadTemplateAsync(int templateId) { }
        private void ApplyTemplateConstraints() { }
        private bool CanSaveTemplate() { }
        private async Task SaveAsTemplateAsync() { }
    }
}
```

### 8. SchedulingViewModel.StateManagement.cs (状态管理和草稿)

**职责**: 草稿保存和恢复、状态重置

**内容**:
- 创建草稿 (CreateDraftAsync)
- 恢复草稿 (RestoreFromDraftAsync)

**预估行数**: ~400 行

**关键代码结构**:
```csharp
namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel
    {
        #region 草稿保存和恢复
        
        /// <summary>
        /// 创建草稿（保存当前状态）
        /// </summary>
        public async Task CreateDraftAsync() { }
        
        /// <summary>
        /// 从草稿恢复状态
        /// </summary>
        public async Task<bool> RestoreFromDraftAsync() { }
        
        #endregion
    }
}
```

### 9. SchedulingViewModel.Helpers.cs (辅助方法)

**职责**: 静态数据、辅助属性、小型工具方法

**内容**:
- 时段选项 (TimeSlotOptions)
- 手动指定集合访问 (AllManualAssignments)
- 其他辅助属性和方法

**预估行数**: ~100 行

**关键代码结构**:
```csharp
namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel
    {
        // 静态数据
        public List<TimeSlotOption> TimeSlotOptions { get; } = TimeSlotOption.GetAll();
        
        // 手动指定集合访问
        public ObservableCollection<ManualAssignmentViewModel> AllManualAssignments 
            => _manualAssignmentManager.AllAssignments;
    }
}
```

## 数据模型

### 现有模型（保持不变）

- **PersonnelDto**: 人员数据传输对象
- **PositionDto**: 哨位数据传输对象
- **HolidayConfig**: 休息日配置
- **FixedPositionRule**: 固定岗位规则
- **ManualAssignment**: 手动指定
- **ManualAssignmentViewModel**: 手动指定视图模型
- **PositionPersonnelViewModel**: 哨位人员视图模型
- **PersonnelItemViewModel**: 人员项视图模型
- **SummarySection**: 摘要区块
- **SchedulingRequestDto**: 排班请求 DTO
- **SchedulingDraftDto**: 草稿 DTO

### 管理器类（保持不变）

- **ManualAssignmentManager**: 手动指定管理器
- **PositionPersonnelManager**: 哨位人员管理器

## 错误处理

### 策略

1. **保持现有错误处理**: 不改变现有的 try-catch 块和错误处理逻辑
2. **日志记录**: 保留所有 Debug.WriteLine 调用
3. **用户提示**: 保留所有 DialogService 调用
4. **异常传播**: 保持现有的异常传播机制

### 关键错误场景

- 数据加载失败 (LoadInitialDataAsync, LoadConstraintsAsync)
- 模板加载失败 (LoadTemplateAsync)
- 保存操作失败 (SaveAsTemplateAsync, SavePositionChangesAsync)
- 草稿操作失败 (CreateDraftAsync, RestoreFromDraftAsync)
- 验证失败 (各种 Validate 方法)

## 测试策略

### 单元测试重点

1. **验证逻辑**: 测试所有 ValidateStepX 方法
2. **状态转换**: 测试步骤导航逻辑
3. **数据转换**: 测试 BuildSchedulingRequest 方法
4. **缓存机制**: 测试 GetPersonnelFromCache 和 GetPositionFromCache

### 集成测试重点

1. **完整流程**: 测试从步骤 1 到步骤 5 的完整向导流程
2. **草稿功能**: 测试保存和恢复草稿
3. **模板功能**: 测试加载和保存模板
4. **手动指定**: 测试手动指定的 CRUD 操作

### 测试注意事项

- 由于是重构,主要依赖现有功能的手动测试
- 确保所有现有功能在重构后仍然正常工作
- 重点测试边界情况和错误处理

## 性能考虑

### 优化保留

1. **缓存机制**: 保留 _personnelCache 和 _positionCache
2. **批量操作**: 保留 Task.WhenAll 并行加载
3. **延迟加载**: 保留约束数据的延迟加载机制
4. **性能日志**: 保留 Stopwatch 性能监控

### 性能目标

- 文件拆分不应影响运行时性能
- 保持现有的加载速度和响应速度
- 缓存命中率保持不变

## 迁移计划

### 阶段 1: 准备工作

1. 备份原始文件
2. 创建新的部分类文件框架
3. 确认命名空间和 using 语句

### 阶段 2: 代码迁移

1. 迁移属性到 Properties.cs
2. 迁移命令到 Commands.cs
3. 迁移向导流程到 Wizard.cs
4. 迁移手动指定到 ManualAssignment.cs
5. 迁移哨位人员到 PositionPersonnel.cs
6. 迁移模板约束到 TemplateConstraints.cs
7. 迁移状态管理到 StateManagement.cs
8. 迁移辅助方法到 Helpers.cs
9. 清理主文件,保留核心内容

### 阶段 3: 验证测试

1. 编译检查
2. 功能测试
3. 性能测试
4. 代码审查

### 阶段 4: 文档更新

1. 更新代码注释
2. 更新开发文档
3. 记录重构决策

## 依赖关系

### 外部依赖（保持不变）

- CommunityToolkit.Mvvm
- AutoScheduling3.DTOs
- AutoScheduling3.Services.Interfaces
- AutoScheduling3.Helpers
- AutoScheduling3.Models.Constraints
- Microsoft.UI.Xaml.Controls

### 内部依赖

- 各部分类文件之间通过共享字段和属性进行通信
- 主文件提供核心依赖服务和管理器
- 其他文件使用这些共享资源

## 风险和缓解

### 风险 1: 编译错误

**描述**: 拆分过程中可能引入编译错误

**缓解措施**:
- 逐步迁移,每次迁移后立即编译
- 使用 IDE 的重构工具辅助
- 保持原始文件作为参考

### 风险 2: 功能回归

**描述**: 重构可能导致功能行为变化

**缓解措施**:
- 不修改任何业务逻辑
- 仅进行代码移动和组织
- 完整的功能测试

### 风险 3: 性能下降

**描述**: 文件拆分可能影响性能

**缓解措施**:
- 保留所有性能优化代码
- 不改变方法调用链
- 性能测试对比

### 风险 4: 维护复杂度

**描述**: 多个文件可能增加维护难度

**缓解措施**:
- 清晰的文件命名和职责划分
- 完善的代码注释
- 更新开发文档

## 成功标准

1. ✅ 所有文件行数 ≤ 1000 行
2. ✅ 主文件行数在 300-600 行之间
3. ✅ 编译成功,无警告
4. ✅ 所有现有功能正常工作
5. ✅ 性能无明显下降
6. ✅ 代码可读性提升
7. ✅ 职责划分清晰
8. ✅ 符合项目规范

## 后续优化建议

1. **进一步抽象**: 考虑将某些复杂逻辑提取为独立的服务类
2. **单元测试**: 为关键方法添加单元测试
3. **接口提取**: 考虑为管理器类定义接口
4. **依赖注入**: 考虑将管理器类也纳入 DI 容器
