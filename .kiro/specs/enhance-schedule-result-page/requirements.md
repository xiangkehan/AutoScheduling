# 需求文档

## 简介

本功能旨在完善排班结果展示页面（ScheduleResultPage），为用户提供更丰富、更直观的排班结果查看体验。当用户在排班进度页面点击"查看详细结果"按钮后，系统将导航到此页面，展示完整的排班结果，包括多种视图模式、详细的统计信息、冲突提示和交互功能。

## 术语表

- **System**：AutoScheduling3 智能排班系统
- **User**：使用排班系统的操作人员
- **Schedule Result Page**：排班结果展示页面
- **Grid View**：网格视图，以表格形式展示排班结果，行为日期+时段，列为哨位
- **List View**：列表视图，以列表形式展示排班结果
- **Personnel View**：人员视图，按人员分组展示排班结果
- **Position View**：哨位视图，按哨位分别展示排班表，每个哨位一张表，行为时段，列为星期
- **Schedule Grid**：排班表格，行为日期+时段，列为哨位，单元格为人员
- **Cell**：单元格，表格中的一个格子，代表某个哨位在某个时段的人员分配
- **Conflict**：冲突，违反约束的分配情况
- **Statistics**：统计信息，包括总分配数、人员工作量、哨位覆盖率等
- **Export**：导出，将排班结果导出为 Excel、CSV 或 PDF 文件
- **Filter**：筛选，按日期范围、哨位、人员等条件过滤显示的数据
- **Tooltip**：工具提示，鼠标悬停时显示的详细信息

## 需求

### 需求 1

**用户故事：** 作为排班管理员，我希望在排班结果页面看到清晰的排班表格，以便快速了解整体排班情况

#### 验收标准

1. WHEN THE User 导航到 Schedule Result Page, THE System SHALL 显示排班表格，行为日期+时段，列为哨位
2. WHERE THE 排班表格显示, THE System SHALL 在单元格中显示分配的人员姓名
3. WHERE THE 排班表格显示, THE System SHALL 为未分配的单元格显示空白或占位符
4. WHERE THE 排班表格显示, THE System SHALL 固定表头和行头，支持横向和纵向滚动
5. WHERE THE 排班表格显示, THE System SHALL 使用虚拟化渲染技术，确保大数据集的流畅显示

### 需求 2

**用户故事：** 作为排班管理员，我希望能够切换不同的视图模式，以便从不同角度查看排班结果

#### 验收标准

1. WHEN THE User 在 Schedule Result Page, THE System SHALL 提供四种视图模式：Grid View、List View、Personnel View、Position View
2. WHEN THE User 选择 Grid View, THE System SHALL 以表格形式展示排班结果，行为日期+时段，列为哨位
3. WHEN THE User 选择 List View, THE System SHALL 以列表形式展示所有班次分配
4. WHEN THE User 选择 Personnel View, THE System SHALL 按人员分组展示每个人的班次安排
5. WHEN THE User 选择 Position View, THE System SHALL 显示哨位选择器，允许切换查看不同哨位的排班表
6. WHERE THE Position View 显示, THE System SHALL 展示当前选中哨位的排班表，行为时段，列为星期
7. WHEN THE User 在 Position View 切换哨位, THE System SHALL 更新表格显示对应哨位的排班数据
8. WHEN THE User 切换视图模式, THE System SHALL 保持当前的筛选条件和数据状态

### 需求 3

**用户故事：** 作为排班管理员，我希望在哨位视图中能够清晰地看到每个哨位在一周内的排班情况，以便快速了解哨位的人员安排规律

#### 验收标准

1. WHEN THE User 选择 Position View, THE System SHALL 显示哨位下拉选择器，列出所有参与排班的哨位
2. WHEN THE User 选择某个哨位, THE System SHALL 显示该哨位的排班表，行为时段（0-11），列为星期（周一到周日）
3. WHERE THE Position View 表格显示, THE System SHALL 在单元格中显示分配的人员姓名
4. WHERE THE Position View 表格显示, THE System SHALL 为跨越多周的排班数据提供周次切换器
5. WHEN THE User 切换周次, THE System SHALL 更新表格显示对应周的排班数据
6. WHERE THE Position View 表格显示, THE System SHALL 使用颜色或图标标识手动指定的分配和有冲突的分配
7. WHERE THE Position View 表格显示, THE System SHALL 支持打印和导出当前哨位的排班表

### 需求 4

**用户故事：** 作为排班管理员，我希望能够查看单元格的详细信息，以便了解每个分配的具体情况

#### 验收标准

1. WHEN THE User 将鼠标悬停在 Cell 上, THE System SHALL 显示 Tooltip，包含人员姓名、技能、当前工作量
2. WHERE THE Cell 是手动指定的分配, THE System SHALL 在 Tooltip 中标注"手动指定"
3. WHERE THE Cell 有冲突, THE System SHALL 在 Tooltip 中显示冲突类型和描述
4. WHEN THE User 点击 Cell, THE System SHALL 显示详细信息对话框，包含完整的分配信息和约束评估
5. WHERE THE 详细信息对话框显示, THE System SHALL 提供"修改分配"和"取消分配"操作按钮

### 需求 5

**用户故事：** 作为排班管理员，我希望能够筛选和搜索排班结果，以便快速定位特定的信息

#### 验收标准

1. WHEN THE User 在 Schedule Result Page, THE System SHALL 提供日期范围筛选器
2. WHEN THE User 选择日期范围, THE System SHALL 仅显示该范围内的排班数据
3. WHEN THE User 在 Schedule Result Page, THE System SHALL 提供哨位筛选器，支持多选
4. WHEN THE User 选择哨位, THE System SHALL 仅显示选中哨位的排班数据
5. WHEN THE User 在 Schedule Result Page, THE System SHALL 提供人员搜索框，支持按姓名搜索

### 需求 6

**用户故事：** 作为排班管理员，我希望能够查看详细的统计信息，以便评估排班方案的质量

#### 验收标准

1. WHEN THE User 在 Schedule Result Page, THE System SHALL 显示统计信息卡片，包含总分配数、参与人员数、参与哨位数
2. WHEN THE User 在 Schedule Result Page, THE System SHALL 显示人员工作量统计表，包含每个人员的总班次数、日哨数、夜哨数
3. WHEN THE User 在 Schedule Result Page, THE System SHALL 显示哨位覆盖率统计表，包含每个哨位的已分配时段数和覆盖率百分比
4. WHEN THE User 在 Schedule Result Page, THE System SHALL 显示软约束评分，包含总分和各项得分明细
5. WHERE THE 统计信息显示, THE System SHALL 支持按不同维度排序（如按工作量、按覆盖率）

### 需求 7

**用户故事：** 作为排班管理员，我希望能够查看和处理冲突信息，以便及时解决排班问题

#### 验收标准

1. WHEN THE Schedule Result Page 加载, THE System SHALL 自动检测并显示所有冲突
2. WHEN THE User 点击"Conflicts"按钮, THE System SHALL 打开冲突面板，显示冲突列表
3. WHERE THE 冲突面板显示, THE System SHALL 为每个冲突显示类型、描述、相关哨位和时段
4. WHEN THE User 点击冲突项, THE System SHALL 在表格中高亮显示相关的 Cell
5. WHERE THE 冲突面板显示, THE System SHALL 提供"忽略"和"修复"操作按钮

### 需求 8

**用户故事：** 作为排班管理员，我希望能够导出排班结果，以便分享或打印

#### 验收标准

1. WHEN THE User 点击"Export"按钮, THE System SHALL 显示导出格式选择对话框
2. WHERE THE 导出格式选择对话框显示, THE System SHALL 提供 Excel、CSV、PDF 三种格式选项
3. WHEN THE User 选择导出格式并确认, THE System SHALL 生成导出文件并提示用户保存位置
4. WHERE THE 导出为 Excel 格式, THE System SHALL 包含表格数据、统计信息和格式化样式
5. WHERE THE 导出为 PDF 格式, THE System SHALL 包含表格数据、统计信息和页眉页脚

### 需求 9

**用户故事：** 作为排班管理员，我希望能够对排班结果进行简单的编辑，以便快速调整个别分配

#### 验收标准

1. WHEN THE User 双击 Cell, THE System SHALL 显示人员选择对话框
2. WHERE THE 人员选择对话框显示, THE System SHALL 列出所有可用人员，并标注技能匹配情况
3. WHEN THE User 选择新的人员并确认, THE System SHALL 更新 Cell 的分配并重新计算统计信息
4. WHEN THE User 修改分配后, THE System SHALL 自动检测新的冲突并更新冲突列表
5. WHERE THE 排班结果被修改, THE System SHALL 在页面顶部显示"未保存"提示，并提供"保存"和"撤销"按钮

### 需求 10

**用户故事：** 作为排班管理员，我希望能够全屏查看排班表格，以便在大屏幕上展示或演示

#### 验收标准

1. WHEN THE User 点击表格右上角的"全屏"按钮, THE System SHALL 将表格以全屏模式显示
2. WHERE THE 表格全屏显示, THE System SHALL 隐藏其他页面元素，仅显示表格和关闭按钮
3. WHERE THE 表格全屏显示, THE System SHALL 保持表格的所有交互功能（滚动、悬停、点击）
4. WHEN THE User 点击"关闭"按钮或按 Esc 键, THE System SHALL 退出全屏模式
5. WHERE THE 表格全屏显示, THE System SHALL 自动调整表格大小以适应屏幕

### 需求 11

**用户故事：** 作为排班管理员，我希望能够比较不同的排班方案，以便选择最优方案

#### 验收标准

1. WHEN THE User 在 Schedule Result Page, THE System SHALL 提供"比较"按钮
2. WHEN THE User 点击"比较"按钮, THE System SHALL 显示历史排班列表，允许选择一个进行比较
3. WHEN THE User 选择历史排班并确认, THE System SHALL 以并排方式显示两个排班方案
4. WHERE THE 比较视图显示, THE System SHALL 高亮显示差异的 Cell
5. WHERE THE 比较视图显示, THE System SHALL 显示两个方案的统计信息对比

