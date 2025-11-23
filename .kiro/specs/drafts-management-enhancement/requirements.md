# 需求文档

## 简介

本功能旨在完善草稿箱（DraftsPage）的功能，使其能够正确展示已完成排哨计算但未确认应用的排班结果，并提供查看详情和应用草稿的能力。当用户应用某个草稿后，系统将清空草稿箱中的所有其他草稿，避免重复应用。

## 术语表

- **DraftsPage**: 草稿箱页面（Views/Scheduling/DraftsPage.xaml），用于展示和管理未确认的排班结果
- **ScheduleResultPage**: 排班结果详情页面（Views/Scheduling/ScheduleResultPage.xaml），用于展示排班的详细信息
- **草稿（Draft/Buffer）**: 已完成排哨计算但尚未确认应用的排班结果，存储在 IHistoryManagement 的缓冲区中，ConfirmedAt 字段为 null
- **历史记录（History）**: 已确认应用的排班结果，存储在 IHistoryManagement 的历史记录中，ConfirmedAt 字段不为 null
- **ISchedulingService**: 排班服务接口（Services/Interfaces/ISchedulingService.cs），提供排班相关的业务逻辑
- **IHistoryManagement**: 历史管理接口（History/IHistoryManagement.cs），管理排班的缓冲区和历史记录
- **DraftsViewModel**: 草稿箱视图模型（ViewModels/Scheduling/DraftsViewModel.cs），管理草稿箱的展示和操作逻辑
- **ScheduleSummaryDto**: 排班摘要数据传输对象（DTOs/ScheduleDto.cs），包含排班的基本信息
- **NavigationService**: 导航服务（Helpers/NavigationService.cs），负责页面导航
- **DialogService**: 对话框服务（Helpers/DialogService.cs），负责显示对话框

## 需求

### 需求 1

**用户故事：** 作为用户，我想在草稿箱中查看所有已完成但未确认的排班结果，以便我可以决定是否应用这些排班。

#### 验收标准

1. WHEN 用户导航到草稿箱页面 THEN 系统应通过 ISchedulingService.GetDraftsAsync 方法加载所有草稿列表
2. WHEN 系统加载草稿列表 THEN 系统应展示每个草稿的标题、日期范围、人员数、哨位数和创建时间
3. WHEN 草稿列表为空 THEN 系统应展示空状态提示信息
4. WHEN 系统正在加载草稿 THEN 系统应展示加载指示器
5. THE 系统应仅展示 ConfirmedAt 字段为 null 的排班记录作为草稿

### 需求 2

**用户故事：** 作为用户，我想点击草稿的详情按钮查看完整的排班结果，以便我可以评估排班质量。

#### 验收标准

1. WHEN 用户点击草稿的"查看"按钮 THEN 系统应通过 NavigationService 导航到 ScheduleResultPage 页面
2. WHEN 系统导航到 ScheduleResultPage THEN 系统应传递草稿的 ID 作为导航参数
3. WHEN ScheduleResultPage 接收到草稿 ID THEN 系统应通过 ISchedulingService.GetScheduleByIdAsync 方法加载草稿的完整信息
4. WHEN ScheduleResultPage 展示草稿详情 THEN 系统应展示所有班次、人员分配、冲突信息和软约束评分

### 需求 3

**用户故事：** 作为用户，我想确认应用某个草稿，以便将其转换为正式的排班记录。

#### 验收标准

1. WHEN 用户点击草稿的"确认"按钮 THEN 系统应展示确认对话框询问用户是否确认
2. WHEN 用户在确认对话框中点击"确认" THEN 系统应通过 ISchedulingService.ConfirmScheduleAsync 方法确认该草稿
3. WHEN 系统确认草稿成功 THEN 系统应将该草稿的 ConfirmedAt 字段设置为当前时间
4. WHEN 系统确认草稿成功 THEN 系统应展示成功提示信息
5. WHEN 系统确认草稿失败 THEN 系统应展示错误提示信息并保持草稿状态不变

### 需求 4

**用户故事：** 作为用户，我想在确认应用某个草稿后清空草稿箱中的所有其他草稿，以便避免重复应用排班。

#### 验收标准

1. WHEN 用户确认应用某个草稿 THEN 系统应通过 IHistoryManagement.ConfirmBufferScheduleAsync 方法确认该草稿
2. WHEN 系统确认草稿成功 THEN 系统应通过 IHistoryManagement.GetAllBufferSchedulesAsync 获取所有剩余草稿
3. WHEN 系统获取到剩余草稿 THEN 系统应调用 IHistoryManagement.DeleteBufferScheduleAsync 方法逐个删除剩余草稿
4. WHEN 系统完成删除所有剩余草稿 THEN 系统应刷新草稿列表展示空状态
5. WHEN 系统删除草稿失败 THEN 系统应记录错误日志但不阻止确认操作完成

### 需求 5

**用户故事：** 作为用户，我想删除单个草稿，以便清理不需要的排班结果。

#### 验收标准

1. WHEN 用户点击草稿的"删除"按钮 THEN 系统应展示确认对话框询问用户是否删除
2. WHEN 用户在确认对话框中点击"删除" THEN 系统应通过 ISchedulingService.DeleteDraftAsync 方法删除该草稿
3. WHEN 系统删除草稿成功 THEN 系统应从草稿列表中移除该草稿并展示成功提示
4. WHEN 系统删除草稿失败 THEN 系统应展示错误提示信息并保持草稿列表不变
5. THE 系统应从数据库中物理删除草稿记录及其关联的班次数据

### 需求 6

**用户故事：** 作为用户，我想在草稿箱页面看到清晰的操作按钮，以便快速执行查看、确认和删除操作。

#### 验收标准

1. WHEN 草稿列表展示时 THEN 每个草稿应展示"查看"、"确认"和"删除"三个操作按钮
2. WHEN 用户将鼠标悬停在操作按钮上 THEN 系统应展示视觉反馈（如高亮或阴影）
3. THE "确认"按钮应使用强调样式（AccentButtonStyle）以突出其重要性
4. THE "删除"按钮应使用图标按钮样式（AppBarButton）并展示删除图标
5. THE 所有操作按钮应具有清晰的标签和图标以提高可访问性
