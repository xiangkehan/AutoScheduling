# 需求文档

## 简介

本功能旨在修复和优化自动排班系统中创建新数据（人员、哨位、技能）的用户体验流程。当前系统存在严重的可用性问题：技能管理和哨位管理页面的"新建"按钮点击后没有任何响应，因为缺少创建数据的UI界面。人员管理页面虽然有内联创建表单，但缺少表单验证和用户反馈。通过实现完整的创建流程和优化用户体验，可以使系统真正可用并提高工作效率。

## 术语表

- **System**: 自动排班系统（AutoScheduling3）
- **User**: 使用系统管理数据的用户
- **Personnel**: 人员实体，代表可以被分配到哨位的工作人员
- **Position**: 哨位实体，代表需要人员值守的岗位
- **Skill**: 技能实体，代表人员具备的能力或哨位要求的能力
- **Create Form**: 创建表单，用于输入新数据的界面组件
- **Validation**: 表单验证，检查用户输入是否符合要求
- **Inline Creation**: 内联创建，在列表页面直接提供创建表单，无需跳转或弹窗
- **Dialog**: 对话框，弹出式窗口用于用户交互
- **Toast Notification**: 轻量级通知提示，用于显示操作结果

## 需求

### 需求 1：实现技能创建对话框

**用户故事：** 作为系统管理员，我希望点击技能管理页面的"新建"按钮时能弹出创建对话框，这样我可以添加新的技能类型。

#### 验收标准

1. WHEN User clicks the "新建" button on Skill management page, THE System SHALL display a ContentDialog for creating a new skill
2. THE System SHALL display a text input field for skill name in the create dialog
3. THE System SHALL display a text input field for skill description in the create dialog
4. THE System SHALL display a toggle switch for skill active status in the create dialog with default value of true
5. WHEN User clicks the primary button in the dialog, THE System SHALL validate all required fields before submission
6. WHEN validation passes and creation succeeds, THE System SHALL close the dialog and refresh the skill list
7. WHEN User clicks the secondary button or closes the dialog, THE System SHALL discard the input and close the dialog

### 需求 2：实现哨位创建对话框

**用户故事：** 作为系统管理员，我希望点击哨位管理页面的"新建"按钮时能弹出创建对话框，这样我可以添加新的哨位。

#### 验收标准

1. WHEN User clicks the "新建" button on Position management page, THE System SHALL display a ContentDialog for creating a new position
2. THE System SHALL display a text input field for position name in the create dialog
3. THE System SHALL display a text input field for position location in the create dialog
4. THE System SHALL display a text input field for position description in the create dialog
5. THE System SHALL display a multi-select control for required skills in the create dialog
6. WHEN no skills exist in the system, THE System SHALL display a message indicating that skills must be created first
7. WHEN User clicks the primary button in the dialog, THE System SHALL validate all required fields before submission
8. WHEN validation passes and creation succeeds, THE System SHALL close the dialog and refresh the position list

### 需求 3：优化人员创建表单验证

**用户故事：** 作为系统管理员，我希望在人员创建表单中填写数据时能看到实时验证反馈，这样我可以及时纠正错误。

#### 验收标准

1. WHEN User leaves the name field empty in Personnel create form, THE System SHALL display a validation error message
2. WHEN User has not selected any skills in Personnel create form, THE System SHALL display a validation error message
3. WHEN all required fields contain valid data, THE System SHALL enable the "添加人员" button
4. WHILE any required field is invalid, THE System SHALL disable the "添加人员" button
5. WHEN User clicks "重置表单" button, THE System SHALL clear all input fields and reset to initial state
6. THE System SHALL display validation messages in red color below the corresponding input field

### 需求 4：创建成功后自动选中新记录

**用户故事：** 作为系统管理员，我希望在成功创建数据后系统能自动选中新创建的记录，这样我可以立即查看或编辑它。

#### 验收标准

1. WHEN a new Personnel record is created successfully, THE System SHALL automatically select the new record in the GridView
2. WHEN a new Position record is created successfully, THE System SHALL automatically select the new record in the ListView
3. WHEN a new Skill record is created successfully, THE System SHALL automatically select the new record in the GridView
4. WHEN a new record is selected automatically, THE System SHALL scroll the list to make the new record visible
5. WHEN a new Personnel record is selected, THE System SHALL display its details in the right panel
6. WHEN a new Position record is selected, THE System SHALL display its details and available personnel in the right panel

### 需求 5：改进技能选择控件的用户体验

**用户故事：** 作为系统管理员，我希望技能选择控件能清晰显示已选择的技能，这样我可以确认我的选择是否正确。

#### 验收标准

1. WHEN User selects skills in Personnel create form, THE System SHALL visually highlight all selected items with a checkmark
2. WHEN User selects skills in Position create dialog, THE System SHALL visually highlight all selected items with a checkmark
3. WHEN no skills exist in the system, THE System SHALL display the message "暂无技能数据，请先添加技能"
4. WHEN User has selected at least one skill, THE System SHALL display the count of selected skills
5. THE System SHALL allow User to scroll through the skill list when it contains more than 5 items
6. THE System SHALL maintain skill selections when User switches between different form sections

### 需求 6：智能表单重置

**用户故事：** 作为系统管理员，我希望在成功创建人员后表单能自动重置，这样我可以快速添加下一个人员。

#### 验收标准

1. WHEN User successfully creates a new Personnel record, THE System SHALL reset the Personnel create form to its initial state
2. WHEN the Personnel create form is reset, THE System SHALL clear the name text field
3. WHEN the Personnel create form is reset, THE System SHALL deselect all skills in the skill selection control
4. WHEN the Personnel create form is reset, THE System SHALL reset the "是否可用" toggle to its default value of true
5. WHEN the form is reset, THE System SHALL keep the create form expanded for quick consecutive additions
6. THE System SHALL focus the name input field after form reset to enable immediate data entry

### 需求 7：对话框XamlRoot配置

**用户故事：** 作为系统开发者，我需要确保所有创建对话框都正确配置XamlRoot，这样对话框才能正常显示。

#### 验收标准

1. WHEN System creates a ContentDialog for skill creation, THE System SHALL set the XamlRoot property to the current page's XamlRoot
2. WHEN System creates a ContentDialog for position creation, THE System SHALL set the XamlRoot property to the current page's XamlRoot
3. WHEN System displays any ContentDialog, THE System SHALL ensure the XamlRoot is not null before calling ShowAsync
4. IF XamlRoot is null when attempting to show a dialog, THE System SHALL log an error message
5. THE System SHALL handle dialog display exceptions gracefully without crashing the application
6. WHEN a dialog fails to display, THE System SHALL show an error message to User using an alternative method

### 需求 8：错误处理和用户反馈

**用户故事：** 作为系统管理员，我希望在创建数据失败时能看到清晰的错误信息，这样我可以知道问题所在并采取相应措施。

#### 验收标准

1. WHEN creation of a Personnel record fails, THE System SHALL display an error dialog with the failure reason
2. WHEN creation of a Position record fails, THE System SHALL display an error dialog with the failure reason
3. WHEN creation of a Skill record fails, THE System SHALL display an error dialog with the failure reason
4. WHEN a validation error occurs, THE System SHALL display the specific validation message to User
5. WHEN a network or database error occurs, THE System SHALL display a user-friendly error message
6. THE System SHALL log detailed error information for debugging purposes while showing simplified messages to User

### 需求 9：创建对话框的可访问性

**用户故事：** 作为系统管理员，我希望创建对话框支持键盘操作，这样我可以更高效地录入数据。

#### 验收标准

1. WHEN User opens a create dialog, THE System SHALL automatically focus the first input field
2. WHEN User presses Tab in a create dialog, THE System SHALL move focus to the next input field in logical order
3. WHEN User presses Shift+Tab in a create dialog, THE System SHALL move focus to the previous input field
4. WHEN User presses Enter in the last input field, THE System SHALL submit the form if validation passes
5. WHEN User presses Escape in a create dialog, THE System SHALL close the dialog without saving
6. THE System SHALL ensure all interactive elements in the dialog are keyboard accessible

### 需求 10：数据刷新和列表更新

**用户故事：** 作为系统管理员，我希望在创建新数据后列表能立即更新，这样我可以看到最新的数据状态。

#### 验收标准

1. WHEN a new Personnel record is created, THE System SHALL add the new record to the Personnel list without requiring a full page refresh
2. WHEN a new Position record is created, THE System SHALL add the new record to the Position list without requiring a full page refresh
3. WHEN a new Skill record is created, THE System SHALL add the new record to the Skill grid without requiring a full page refresh
4. WHEN a new record is added to a list, THE System SHALL maintain the current scroll position if the new item is not visible
5. WHEN a new record is added and automatically selected, THE System SHALL scroll to make it visible
6. THE System SHALL update any dependent data displays when new records are created
