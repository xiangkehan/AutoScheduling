# 数据管理编辑和删除功能设计文档

## 概述

本设计文档描述了如何完善人员管理、哨位管理和技能管理模块中的编辑和删除功能。当前系统已经有基本的ViewModel和Service层实现，但UI层的编辑和删除功能需要完善，特别是哨位和技能管理模块。

## 架构

### 当前架构分析

系统采用MVVM架构模式：
- **View层**: XAML页面（PersonnelPage, PositionPage, SkillPage）
- **ViewModel层**: 已实现完整的编辑和删除命令（PersonnelViewModel, PositionViewModel, SkillViewModel）
- **Service层**: 提供数据操作接口（IPersonnelService, IPositionService, ISkillService）
- **Repository层**: 数据库访问层

### 设计原则

1. **最小化修改**: 利用现有的ViewModel实现，主要完善UI层
2. **一致性**: 三个管理模块保持相似的用户体验
3. **用户友好**: 提供清晰的编辑界面和删除确认
4. **错误处理**: 只在失败时显示错误提示

## 组件和接口

### 1. 哨位编辑功能

#### UI组件设计

**编辑对话框 (PositionEditDialog.xaml)**
- 使用ContentDialog实现模态对话框
- 包含以下输入字段：
  - 哨位名称 (TextBox, 必填)
  - 哨位地点 (TextBox, 必填)
  - 哨位描述 (TextBox, 可选)
  - 所需技能 (ListView, 多选)
- 提供"保存"和"取消"按钮

#### 交互流程

1. 用户点击哨位详情页的"编辑"按钮
2. 触发ViewModel的EditCommand
3. 显示编辑对话框，预填充当前数据
4. 用户修改数据后点击"保存"
5. 验证输入数据
6. 调用PositionService.UpdateAsync更新数据
7. 关闭对话框，刷新列表

#### 代码后置逻辑

```csharp
// PositionPage.xaml.cs
private async void EditPosition_Click(object sender, RoutedEventArgs e)
{
    if (ViewModel.SelectedItem == null) return;
    
    var dialog = new PositionEditDialog
    {
        XamlRoot = this.XamlRoot,
        Position = ViewModel.SelectedItem,
        AvailableSkills = ViewModel.AvailableSkills
    };
    
    var result = await dialog.ShowAsync();
    if (result == ContentDialogResult.Primary)
    {
        await ViewModel.SaveCommand.ExecuteAsync(null);
    }
}
```

### 2. 哨位删除功能

#### 交互流程

1. 用户点击"删除"按钮
2. 显示确认对话框（使用DialogService.ShowConfirmAsync）
3. 用户确认后调用PositionService.DeleteAsync
4. 从列表中移除该项
5. 清除选中状态

#### 实现说明

- ViewModel已实现DeleteCommand
- UI需要绑定到DeleteCommand
- 使用现有的DialogService进行确认

### 3. 技能编辑功能

#### UI组件设计

**编辑对话框 (SkillEditDialog.xaml)**
- 使用ContentDialog实现
- 包含以下输入字段：
  - 技能名称 (TextBox, 必填, 最大50字符)
  - 技能描述 (TextBox, 可选)
- 提供"保存"和"取消"按钮

#### 交互流程

1. 用户双击技能卡片或点击编辑按钮
2. 显示编辑对话框
3. 预填充当前技能数据
4. 用户修改后点击"保存"
5. 验证输入
6. 更新数据库
7. 刷新列表

#### UI增强

在SkillPage.xaml中添加：
- 右键菜单（编辑、删除）
- 或在GridView选中时显示操作按钮

### 4. 技能删除功能

#### 交互流程

1. 用户选择技能后点击删除按钮
2. 显示确认对话框
3. 检查是否被引用（可选增强）
4. 确认后删除
5. 刷新列表

### 5. 人员编辑功能增强

#### 当前问题分析

PersonnelPage.xaml已有编辑UI，但存在以下可优化点：
- 编辑模式下的技能选择状态同步
- 验证反馈的及时性

#### 改进方案

1. **技能选择同步**: 在进入编辑模式时，自动选中当前人员的技能
2. **实时验证**: 在EditingPersonnel属性变更时触发验证
3. **取消确认**: 如果有未保存的更改，取消时提示确认

### 6. 人员删除功能增强

#### 改进方案

1. **引用检查**: 在删除前检查人员是否被分配到哨位
2. **警告信息**: 如果有引用，在确认对话框中显示警告
3. **级联处理**: 删除人员时自动从相关哨位中移除

## 数据模型

### DTO使用

系统已定义完整的DTO：
- **CreatePositionDto**: 创建哨位
- **UpdatePositionDto**: 更新哨位
- **CreateSkillDto**: 创建技能
- **UpdateSkillDto**: 更新技能
- **UpdatePersonnelDto**: 更新人员

### 数据流

```
UI Input → ViewModel (DTO) → Service → Repository → Database
Database → Repository → Service → ViewModel (DTO) → UI Display
```

## 错误处理

### 验证错误

**客户端验证**:
- 必填字段检查
- 长度限制检查
- 格式验证

**服务端验证**:
- 唯一性检查
- 引用完整性检查
- 业务规则验证

### 错误显示策略

根据需求8，只在失败时显示错误：
- 使用DialogService.ShowErrorAsync显示错误对话框
- 错误消息5秒后自动消失
- 成功操作不显示任何提示

### 异常处理层次

1. **ViewModel层**: 捕获并处理所有异常
2. **Service层**: 抛出具体的业务异常
3. **Repository层**: 抛出数据访问异常

## 测试策略

### 单元测试

- ViewModel命令测试
- 验证逻辑测试
- DTO映射测试

### 集成测试

- 完整的编辑流程测试
- 删除操作测试
- 错误处理测试

### UI测试

- 对话框显示测试
- 用户交互测试
- 响应式布局测试

## 实现细节

### 对话框设计规范

**通用样式**:
- 使用系统ContentDialog
- 主按钮: "保存" (AccentButtonStyle)
- 次要按钮: "取消"
- 最小宽度: 400px
- 最大宽度: 600px

**输入控件**:
- TextBox使用Header属性显示标签
- 必填字段标记 "*"
- 验证错误显示在控件下方

### 响应式设计

- 对话框在小屏幕上自适应宽度
- 输入字段垂直堆叠
- 按钮在移动设备上全宽显示

### 无障碍支持

- 所有控件提供AutomationProperties
- 键盘导航支持
- 屏幕阅读器友好

## 性能考虑

### 优化策略

1. **增量更新**: 编辑后只更新变更的项，不重新加载整个列表
2. **延迟加载**: 对话框内容按需加载
3. **缓存**: 技能列表等静态数据缓存在ViewModel中

### 内存管理

- 对话框使用完毕后及时释放
- 避免循环引用
- 使用WeakReference处理事件订阅

## 安全考虑

### 输入验证

- 防止SQL注入（使用参数化查询）
- XSS防护（输入转义）
- 长度限制防止DoS

### 权限控制

- 当前版本无权限系统
- 未来可扩展基于角色的访问控制

## 迁移和兼容性

### 向后兼容

- 不修改现有数据库架构
- 不改变现有API接口
- 保持现有数据格式

### 数据迁移

- 无需数据迁移
- 仅UI层改进

## 部署考虑

### 配置

- 无需额外配置
- 使用现有的数据库连接

### 监控

- 记录编辑和删除操作日志
- 错误日志记录

## 未来扩展

### 可能的增强

1. **批量操作**: 支持批量编辑和删除
2. **撤销功能**: 实现操作撤销
3. **历史记录**: 记录编辑历史
4. **权限管理**: 基于角色的编辑权限
5. **审计日志**: 详细的操作审计
