# Requirements Document

## Introduction

本功能旨在为排班创建流程（CreateSchedulingPage + SchedulingViewModel）提供进度持久化能力。当用户在创建排班过程中临时导航到其他页面时，系统应保存当前的创建进度，包括所有用户输入和配置。当用户返回创建排班页面时，系统应恢复之前保存的进度，使用户能够从离开的地方继续操作。此功能需要同时支持使用模板（LoadTemplateCommand）和不使用模板（LoadDataCommand）两种排班创建方式，并且需要保存ManualAssignmentManager中的临时手动指定数据。

## Glossary

- **System**: 排班管理系统（AutoScheduling3）
- **CreateSchedulingPage**: 创建排班页面（WinUI 3 Page）
- **SchedulingViewModel**: 排班视图模型，管理排班创建的业务逻辑，遵循MVVM模式
- **Draft**: 草稿，指用户尚未完成的排班创建进度数据
- **Template Mode**: 模板模式，指使用预定义模板创建排班的方式
- **Manual Mode**: 手动模式，指不使用模板手动配置所有参数创建排班的方式
- **Navigation Event**: 导航事件，指用户离开或进入页面的操作（OnNavigatedTo/OnNavigatedFrom）
- **Progress Data**: 进度数据，包括用户在创建排班过程中输入的所有配置信息
- **ApplicationData**: WinUI 3应用数据存储API，用于持久化本地数据
- **INotifyPropertyChanged**: WinUI 3数据绑定接口，用于UI状态同步
- **ManualAssignmentManager**: 手动指定管理器，管理临时和已保存的手动指定
- **ObservableCollection**: WinUI 3集合类型，支持数据绑定和变更通知
- **ConfigurationService**: 配置服务，用于管理应用配置和草稿数据

## Requirements

### Requirement 1

**User Story:** 作为用户，我希望在创建排班过程中切换到其他页面后，再返回时能看到之前的输入内容，这样我就不需要重新填写所有信息。

#### Acceptance Criteria

1. WHEN 用户在CreateSchedulingPage输入任何配置数据, THE System SHALL 通过SchedulingViewModel的ObservableProperty变更自动触发Draft保存
2. WHEN 用户从CreateSchedulingPage导航离开, THE System SHALL 在OnNavigatedFrom生命周期方法中通过ConfigurationService持久化当前所有Progress Data到ApplicationData.LocalSettings
3. WHEN 用户导航返回CreateSchedulingPage, THE System SHALL 在OnNavigatedTo生命周期方法中通过ConfigurationService从ApplicationData.LocalSettings加载最近的Draft数据
4. WHEN Draft数据加载成功, THE System SHALL 通过SchedulingViewModel的ObservableProperty和ObservableCollection恢复所有用户界面控件到之前的状态
5. THE System SHALL 在Draft中包含ScheduleTitle、StartDate、EndDate、CurrentStep、SelectedPersonnels、SelectedPositions、ManualAssignmentManager状态和所有约束配置，使用JSON序列化格式存储

### Requirement 2

**User Story:** 作为用户，我希望系统能够区分使用模板和不使用模板两种创建方式的进度，这样我在两种模式间切换时不会混淆数据。

#### Acceptance Criteria

1. WHEN 用户通过LoadTemplateCommand加载模板创建排班, THE System SHALL 在Draft中保存LoadedTemplateId和TemplateApplied标记
2. WHEN 用户通过LoadDataCommand手动创建排班, THE System SHALL 在Draft中标记TemplateApplied为false
3. WHEN Draft处于Template Mode, THE System SHALL 保存LoadedTemplateId、EnabledFixedRules、EnabledManualAssignments和HolidayConfig配置到ApplicationData
4. WHEN Draft处于Manual Mode, THE System SHALL 保存SelectedPersonnels、SelectedPositions和所有手动配置的约束参数到ApplicationData
5. WHEN 用户返回CreateSchedulingPage, THE System SHALL 根据Draft中的TemplateApplied标记决定调用LoadTemplateCommand或LoadDataCommand恢复状态

### Requirement 3

**User Story:** 作为用户，我希望在成功创建排班后系统能自动清除草稿，这样下次创建新排班时不会看到旧的数据。

#### Acceptance Criteria

1. WHEN 用户成功完成排班创建, THE System SHALL 从ApplicationData.LocalSettings删除对应的Draft数据
2. WHEN 用户点击取消或明确放弃创建, THE System SHALL 使用ContentDialog提供选项让用户决定是否保留Draft
3. WHEN Draft数据超过7天未被访问, THE System SHALL 在应用启动时通过后台任务自动清理该Draft数据
4. THE System SHALL 在删除Draft前不需要用户确认（成功创建场景）
5. THE System SHALL 在用户下次进入CreateSchedulingPage时通过ViewModel初始化显示空白表单（当无Draft存在时）

### Requirement 4

**User Story:** 作为用户，我希望系统能够保存我配置的手动分配信息，这样我在页面切换后不会丢失已经设置的人员分配。

#### Acceptance Criteria

1. WHEN 用户通过ManualAssignmentManager配置手动分配, THE System SHALL 将ManualAssignmentManager的AllAssignments数据包含在Draft中
2. WHEN Draft包含手动分配数据, THE System SHALL 使用JSON序列化保存ManualAssignmentViewModel的Date、PersonnelId、PositionId、TimeSlot、Remarks和IsEnabled到ApplicationData
3. WHEN 用户返回CreateSchedulingPage, THE System SHALL 通过ManualAssignmentManager.LoadSaved和AddTemporary方法恢复所有之前配置的手动分配
4. WHEN 恢复手动分配时人员或岗位已被删除, THE System SHALL 使用InfoBar控件显示警告信息并跳过该分配
5. THE System SHALL 在Draft中区分保存SavedAssignments（有Id）和TemporaryAssignments（无Id）两种类型的手动分配数据

### Requirement 5

**User Story:** 作为用户，我希望系统能够处理数据不一致的情况，这样即使基础数据发生变化，我也能安全地恢复进度。

#### Acceptance Criteria

1. WHEN Draft中的LoadedTemplateId引用的模板已被删除, THE System SHALL 使用DialogService.ShowWarningAsync显示错误提示并设置TemplateApplied为false切换到Manual Mode
2. WHEN Draft中的StartDate早于当前日期, THE System SHALL 使用DialogService.ShowWarningAsync提示用户并自动调整StartDate为当前日期
3. WHEN Draft数据格式不兼容当前版本, THE System SHALL 使用try-catch安全地忽略该Draft并通过System.Diagnostics.Debug记录错误日志
4. WHEN Draft加载失败, THE System SHALL 调用CancelCommand重置ViewModel状态并使用DialogService.ShowWarningAsync通知用户无法恢复进度
5. THE System SHALL 在加载Draft前验证SelectedPersonnels和SelectedPositions中的ID是否在AvailablePersonnels和AvailablePositions中存在
