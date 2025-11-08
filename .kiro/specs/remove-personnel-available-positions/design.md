# Design Document

## Overview

本设计文档描述了如何从人员管理系统中移除"可用哨位"相关的代码。由于 Personal 数据模型中不包含可用哨位字段，但 DTO、Mapper、Service 和 UI 层仍保留了相关代码，导致数据结构不一致和代码冗余。

本设计将系统地清理这些冗余代码，确保：
1. 数据模型与 DTO 保持一致
2. UI 界面只显示实际存在的字段
3. 服务层不处理不存在的数据
4. 代码更简洁、易维护

## Architecture

### 影响范围

本次修改涉及以下层次：

```
┌─────────────────────────────────────────┐
│           UI Layer (XAML)               │
│  - PersonnelPage.xaml                   │
│  - PersonnelPage.xaml.cs                │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│        ViewModel Layer                  │
│  - PersonnelViewModel.cs                │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│         DTO Layer                       │
│  - PersonnelDto.cs                      │
│  - CreatePersonnelDto.cs                │
│  - UpdatePersonnelDto.cs                │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│        Mapper Layer                     │
│  - PersonnelMapper.cs                   │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│        Service Layer                    │
│  - PersonnelService.cs                  │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│         Model Layer                     │
│  - Personal.cs (无需修改)               │
└─────────────────────────────────────────┘
```

### 不影响的部分

- **Personal 模型**: 本身就没有可用哨位字段，无需修改
- **PersonalRepository**: 不涉及可用哨位的数据库操作，无需修改
- **数据库 Schema**: 不需要修改数据库结构

## Components and Interfaces

### 1. DTO 层修改

#### PersonnelDto.cs

**移除的属性:**
- `AvailablePositionIds` - 可用哨位ID列表
- `AvailablePositionNames` - 可用哨位名称列表（冗余字段）

**保留的属性:**
- `Id` - 人员ID
- `Name` - 姓名
- `SkillIds` - 技能ID列表
- `SkillNames` - 技能名称列表
- `IsAvailable` - 是否可用
- `IsRetired` - 是否已退役
- `RecentShiftIntervalCount` - 最近班次间隔计数
- `RecentHolidayShiftIntervalCount` - 最近节假日班次间隔计数
- `RecentPeriodShiftIntervals` - 各时段班次间隔计数
- `IsActive` - 计算属性（在职且可用）

#### CreatePersonnelDto.cs

**移除的属性:**
- `AvailablePositionIds` - 可用哨位ID列表

**保留的属性:**
- `Name` - 姓名（必填）
- `SkillIds` - 技能ID列表（必填，至少一项）
- `IsAvailable` - 是否可用
- `RecentShiftIntervalCount` - 最近班次间隔计数
- `RecentHolidayShiftIntervalCount` - 最近节假日班次间隔计数
- `RecentPeriodShiftIntervals` - 各时段班次间隔计数

#### UpdatePersonnelDto.cs

**移除的属性:**
- `AvailablePositionIds` - 可用哨位ID列表

**保留的属性:**
- `Name` - 姓名（必填）
- `SkillIds` - 技能ID列表（必填，至少一项）
- `IsAvailable` - 是否可用
- `IsRetired` - 是否已退役
- `RecentShiftIntervalCount` - 最近班次间隔计数
- `RecentHolidayShiftIntervalCount` - 最近节假日班次间隔计数
- `RecentPeriodShiftIntervals` - 各时段班次间隔计数

### 2. Mapper 层修改

#### PersonnelMapper.cs

**修改的方法:**

1. `ToDto(Personal model)` - 移除 AvailablePositionIds 和 AvailablePositionNames 的初始化
2. `ToDtoAsync(Personal model)` - 移除加载可用哨位信息的逻辑
3. `ToUpdateDto(PersonnelDto dto)` - 移除 AvailablePositionIds 的复制
4. `ToCreateDto(PersonnelDto dto)` - 移除 AvailablePositionIds 的复制

**不需要修改的方法:**
- `ToModel(CreatePersonnelDto dto)` - 本身不涉及可用哨位
- `ToModel(PersonnelDto dto)` - 本身不涉及可用哨位
- `UpdateModel(Personal model, UpdatePersonnelDto dto)` - 本身不涉及可用哨位

### 3. Service 层修改

#### IPersonnelService.cs (接口)

**移除的方法签名:**
- `Task<List<PositionDto>> GetAvailablePositionsAsync(int personnelId)` - 获取人员可用哨位列表

**保留的方法签名:**
- `Task<List<PersonnelDto>> GetAllAsync()` - 获取所有人员
- `Task<PersonnelDto?> GetByIdAsync(int id)` - 根据ID获取人员
- `Task<PersonnelDto> CreateAsync(CreatePersonnelDto dto)` - 创建人员
- `Task UpdateAsync(int id, UpdatePersonnelDto dto)` - 更新人员
- `Task DeleteAsync(int id)` - 删除人员
- `Task<List<PersonnelDto>> SearchAsync(string keyword)` - 搜索人员
- `Task<List<PersonnelDto>> GetAvailablePersonnelAsync()` - 获取可用人员
- `Task<bool> ValidatePersonnelSkillsAsync(int personnelId, int positionId)` - 验证人员技能匹配

#### PersonnelService.cs (实现)

**移除的依赖:**
- `IPositionRepository _positionRepository` - 不再需要查询哨位信息

**移除的方法:**
- `GetAvailablePositionsAsync(int personnelId)` - 获取人员可用哨位列表

**修改的方法:**
- 构造函数 - 移除 `IPositionRepository` 参数

**保留的方法:**
- `GetAllAsync()` - 获取所有人员
- `GetByIdAsync(int id)` - 根据ID获取人员
- `CreateAsync(CreatePersonnelDto dto)` - 创建人员
- `UpdateAsync(int id, UpdatePersonnelDto dto)` - 更新人员
- `DeleteAsync(int id)` - 删除人员
- `SearchAsync(string keyword)` - 搜索人员
- `GetAvailablePersonnelAsync()` - 获取可用人员
- `ValidatePersonnelSkillsAsync(int personnelId, int positionId)` - 验证人员技能匹配

### 4. UI 层修改

#### PersonnelCard.xaml (控件)

**当前结构:**
```xml
<Grid x:Name="LayoutRoot" RowDefinitions="Auto,*,Auto,Auto">
    <StackPanel Grid.Row="0"><!-- 人员名称 --></StackPanel>
    <TextBlock Grid.Row="1" Text="{x:Bind Personnel.AvailablePositionNames[0], ...}"/>  <!-- 需要移除 -->
    <StackPanel Grid.Row="2"><!-- 技能列表 --></StackPanel>
    <StackPanel Grid.Row="3"><!-- 状态标签 --></StackPanel>
</Grid>
```

**移除的元素:**
- Grid.Row="1" 中的 TextBlock - 显示 `Personnel.AvailablePositionNames[0]`

**修改方案（推荐）:**

方案1：完全移除该行
```xml
<Grid x:Name="LayoutRoot" RowDefinitions="Auto,Auto,Auto">
    <StackPanel Grid.Row="0"><!-- 人员名称 --></StackPanel>
    <!-- 移除 Grid.Row="1" -->
    <StackPanel Grid.Row="1"><!-- 技能列表（原 Row 2）--></StackPanel>
    <StackPanel Grid.Row="2"><!-- 状态标签（原 Row 3）--></StackPanel>
</Grid>
```

方案2：替换为其他信息（可选）
```xml
<Grid x:Name="LayoutRoot" RowDefinitions="Auto,Auto,Auto,Auto">
    <StackPanel Grid.Row="0"><!-- 人员名称 --></StackPanel>
    <TextBlock Grid.Row="1" Text="{x:Bind Personnel.Id, Mode=OneWay, StringFormat='ID: {0}'}" 
               Foreground="{ThemeResource TextFillColorSecondaryBrush}" 
               FontSize="12" Margin="0,4,0,0"/>
    <StackPanel Grid.Row="2"><!-- 技能列表 --></StackPanel>
    <StackPanel Grid.Row="3"><!-- 状态标签 --></StackPanel>
</Grid>
```

**推荐使用方案1**，因为：
- 卡片更简洁
- 人员ID对用户意义不大
- 技能和状态信息更重要

#### CreateSchedulingPage.xaml

**移除的绑定:**
- PersonnelListViewTemplate 中的 TextBlock - 显示 `AvailablePositionNames[0]`（第 22 行）

**修改方案:**
```xml
<!-- 修改前: -->
<DataTemplate x:Key="PersonnelListViewTemplate" x:DataType="dto:PersonnelDto">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <SymbolIcon Symbol="Contact"/>
        <TextBlock Text="{x:Bind Name}" FontWeight="SemiBold"/>
        <TextBlock Text="{x:Bind AvailablePositionNames[0], FallbackValue='N/A'}" 
                   Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>  <!-- 移除此行 -->
    </StackPanel>
</DataTemplate>

<!-- 修改后: -->
<DataTemplate x:Key="PersonnelListViewTemplate" x:DataType="dto:PersonnelDto">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <SymbolIcon Symbol="Contact"/>
        <TextBlock Text="{x:Bind Name}" FontWeight="SemiBold"/>
        <!-- 可选：显示技能数量或其他信息 -->
        <TextBlock Text="{x:Bind SkillIds.Count, StringFormat='技能: {0}'}" 
                   Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
    </StackPanel>
</DataTemplate>
```

#### TemplatePage.xaml

**移除的绑定:**
- PersonnelListItemTemplate 中的 TextBlock - 显示 `AvailablePositionNames[0]`（第 20 行）

**修改方案:**
```xml
<!-- 修改前: -->
<DataTemplate x:Key="PersonnelListItemTemplate" x:DataType="dto:PersonnelDto">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Text="{x:Bind Name}" FontWeight="SemiBold"/>
        <TextBlock Text="{x:Bind AvailablePositionNames[0], FallbackValue='N/A'}" 
                   Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>  <!-- 移除此行 -->
    </StackPanel>
</DataTemplate>

<!-- 修改后: -->
<DataTemplate x:Key="PersonnelListItemTemplate" x:DataType="dto:PersonnelDto">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Text="{x:Bind Name}" FontWeight="SemiBold"/>
        <!-- 可选：显示技能数量或其他信息 -->
        <TextBlock Text="{x:Bind SkillIds.Count, StringFormat='技能: {0}'}" 
                   Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
    </StackPanel>
</DataTemplate>
```

#### PersonnelPage.xaml

**移除的控件:**

1. **添加人员表单中:**
   - 可用哨位选择的 StackPanel
   - AvailablePositionsListView 控件

2. **编辑表单中:**
   - 可用哨位编辑的 StackPanel
   - EditAvailablePositionsListView 控件

3. **详情显示中:**
   - 可用哨位显示的 StackPanel
   - 可用哨位名称的 ItemsRepeater

**保留的控件:**
- 姓名输入/显示
- 技能选择/显示
- 状态开关（是否可用、是否退役）
- 操作按钮（添加、编辑、删除、保存、取消）

#### PersonnelPage.xaml.cs

**移除的方法:**
- `AvailablePositionsListView_SelectionChanged` - 处理添加表单中的哨位选择
- `EditAvailablePositionsListView_SelectionChanged` - 处理编辑表单中的哨位选择

**修改的方法:**
- `SyncEditSelections()` - 移除同步哨位选择的逻辑
- `ResetForm_Click()` - 移除清空哨位选择的逻辑

**保留的方法:**
- `PersonnelPage_Loaded` - 页面加载
- `ViewModel_PropertyChanged` - 属性变化监听
- `SkillsListView_SelectionChanged` - 技能选择处理
- `EditSkillsListView_SelectionChanged` - 编辑技能选择处理
- `MainContentGrid_SizeChanged` - 响应式布局
- `ApplyResponsiveLayout` - 应用响应式布局
- `ApplyVerticalLayout` - 垂直布局
- `ApplyHorizontalLayout` - 水平布局

### 5. ViewModel 层修改

#### PersonnelViewModel.cs

**保留但不使用的属性:**
- `AvailablePositions` - 保留此属性，因为可能被其他功能使用，但不在人员表单中绑定

**修改的方法:**

1. **StartEditAsync()** - 移除 AvailablePositionIds 的复制
   ```csharp
   // 修改前:
   EditingPersonnel = new UpdatePersonnelDto
   {
       Name = SelectedItem.Name,
       AvailablePositionIds = new List<int>(SelectedItem.AvailablePositionIds), // 移除此行
       SkillIds = new List<int>(SelectedItem.SkillIds),
       IsAvailable = SelectedItem.IsAvailable,
       IsRetired = SelectedItem.IsRetired
   };
   
   // 修改后:
   EditingPersonnel = new UpdatePersonnelDto
   {
       Name = SelectedItem.Name,
       SkillIds = new List<int>(SelectedItem.SkillIds),
       IsAvailable = SelectedItem.IsAvailable,
       IsRetired = SelectedItem.IsRetired
   };
   ```

2. **LoadDataAsync()** - 可以选择性移除加载哨位列表的逻辑
   - 如果确认 AvailablePositions 不被其他功能使用，可以移除加载逻辑
   - 如果不确定，建议保留以避免破坏其他功能

**不需要修改的方法:**
- `CreatePersonnelAsync()` - 不涉及可用哨位
- `SavePersonnelAsync()` - 不涉及可用哨位
- `DeletePersonnelAsync()` - 不涉及可用哨位
- `CancelEdit()` - 不涉及可用哨位

## Data Models

### Personal Model (无需修改)

```csharp
public class Personal
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<int> SkillIds { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsRetired { get; set; }
    public int RecentShiftIntervalCount { get; set; }
    public int RecentHolidayShiftIntervalCount { get; set; }
    public int[] RecentPeriodShiftIntervals { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### PersonnelDto (修改后)

```csharp
public class PersonnelDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    // 移除: public List<int> AvailablePositionIds { get; set; }
    // 移除: public List<string> AvailablePositionNames { get; set; }
    public List<int> SkillIds { get; set; }
    public List<string> SkillNames { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsRetired { get; set; }
    public int RecentShiftIntervalCount { get; set; }
    public int RecentHolidayShiftIntervalCount { get; set; }
    public int[] RecentPeriodShiftIntervals { get; set; }
    public bool IsActive => IsAvailable && !IsRetired;
}
```

### CreatePersonnelDto (修改后)

```csharp
public class CreatePersonnelDto
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; }
    
    // 移除: public List<int> AvailablePositionIds { get; set; }
    
    [Required]
    [MinLength(1)]
    public List<int> SkillIds { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    [Range(0, 999)]
    public int RecentShiftIntervalCount { get; set; }
    
    [Range(0, 999)]
    public int RecentHolidayShiftIntervalCount { get; set; }
    
    [Required]
    [MinLength(12)]
    [MaxLength(12)]
    public int[] RecentPeriodShiftIntervals { get; set; } = new int[12];
}
```

## Error Handling

### 验证规则保持不变

1. **姓名验证:**
   - 必填
   - 长度 1-50 字符

2. **技能验证:**
   - 必填
   - 至少选择一项
   - 技能ID必须存在于系统中

3. **间隔计数验证:**
   - 范围 0-999

### 移除的验证

- 不再验证可用哨位ID的有效性
- 不再检查哨位是否存在

## Testing Strategy

### 单元测试

1. **DTO 测试:**
   - 验证 PersonnelDto 不包含 AvailablePositionIds 和 AvailablePositionNames
   - 验证 CreatePersonnelDto 不包含 AvailablePositionIds
   - 验证 UpdatePersonnelDto 不包含 AvailablePositionIds

2. **Mapper 测试:**
   - 测试 ToDto 方法不设置可用哨位字段
   - 测试 ToDtoAsync 方法不加载可用哨位信息
   - 测试 ToModel 方法正确映射所有保留字段

3. **Service 测试:**
   - 测试 CreateAsync 不处理可用哨位
   - 测试 UpdateAsync 不处理可用哨位
   - 验证移除 IPositionRepository 依赖后服务正常工作

### 集成测试

1. **UI 测试:**
   - 验证添加人员表单不显示可用哨位选择
   - 验证编辑人员表单不显示可用哨位选择
   - 验证人员详情不显示可用哨位信息
   - 验证创建人员功能正常（只填写姓名和技能）
   - 验证编辑人员功能正常（只修改姓名、技能和状态）

2. **端到端测试:**
   - 创建新人员 → 验证数据正确保存
   - 编辑现有人员 → 验证数据正确更新
   - 查看人员详情 → 验证显示正确信息

### 回归测试

1. **验证不影响其他功能:**
   - 哨位管理功能正常（哨位仍可配置可用人员列表）
   - 排班算法正常（通过哨位的可用人员列表进行匹配）
   - 技能管理功能正常

## Interface Changes

### IPersonnelService 接口变更

**移除的方法:**
```csharp
Task<List<PositionDto>> GetAvailablePositionsAsync(int personnelId);
```

**影响分析:**
- 此方法目前未被任何代码调用（通过代码搜索确认）
- 移除此方法不会破坏现有功能
- 如果将来需要获取人员可以胜任的哨位，可以通过以下方式实现：
  1. 在 PositionService 中添加 `GetPositionsByPersonnelAsync(int personnelId)` 方法
  2. 通过查询哨位的 `AvailablePersonnelIds` 字段来反向获取

**替代方案:**
如果需要查询"哪些哨位可以分配给某个人员"，应该：
1. 使用 `IPositionService.GetAllAsync()` 获取所有哨位
2. 筛选 `AvailablePersonnelIds` 包含该人员ID的哨位
3. 验证人员技能是否满足哨位要求

### 依赖注入变更

**PersonnelService 构造函数变更:**

```csharp
// 修改前:
public PersonnelService(
    IPersonalRepository repository, 
    ISkillRepository skillRepository,
    IPositionRepository positionRepository,  // 移除此参数
    PersonnelMapper mapper)

// 修改后:
public PersonnelService(
    IPersonalRepository repository, 
    ISkillRepository skillRepository,
    PersonnelMapper mapper)
```

**影响分析:**
- 需要更新依赖注入配置（如果使用 DI 容器）
- PersonnelService 不再依赖 IPositionRepository
- 简化了服务依赖关系

## Migration Considerations

### 向后兼容性

由于 Personal 模型本身就没有可用哨位字段，此次修改：
- **不需要数据库迁移**
- **不影响现有数据**
- **不破坏哨位管理功能**（哨位仍保留 AvailablePersonnelIds 字段）

### 数据流向说明

**修改前（不一致）:**
```
Personal (无可用哨位) → PersonnelDto (有可用哨位) → UI (显示可用哨位)
                          ↑
                    从 Position 查询
```

**修改后（一致）:**
```
Personal (无可用哨位) → PersonnelDto (无可用哨位) → UI (不显示可用哨位)
```

### 哨位与人员的关系

修改后，人员与哨位的关系仍然存在，但方向单一：
- **哨位 → 人员**: 哨位配置可用人员列表（PositionLocation.AvailablePersonnelIds）
- **人员 ← 哨位**: 人员不配置可用哨位列表（已移除）
    
- 排班时根据哨位的可用人员列表和人员技能进行匹配

## Implementation Notes

### 实施顺序

1. **第一步**: 修改 DTO 层（移除可用哨位字段）
2. **第二步**: 修改 Mapper 层（移除可用哨位映射逻辑）
3. **第三步**: 修改 Service 层（移除 IPositionRepository 依赖）
4. **第四步**: 修改 UI 层 XAML（移除可用哨位控件）
5. **第五步**: 修改 UI 层 Code-behind（移除可用哨位事件处理）
6. **第六步**: 检查 ViewModel 层（移除可用哨位相关逻辑）
7. **第七步**: 运行测试验证功能正常

### 注意事项

1. **PersonnelService 中的 GetAvailablePositionsAsync 方法:**
   - 此方法用于获取人员可以胜任的哨位
   - 虽然人员不存储可用哨位，但此方法通过查询哨位的 AvailablePersonnelIds 来反向获取
   - 如果其他功能需要此方法，应保留；否则可以移除

2. **ViewModel 中的 AvailablePositions 属性:**
   - 需要检查是否仅用于人员管理
   - 如果其他页面（如哨位管理）需要，应保留
   - 如果仅用于人员管理，应移除

3. **编译错误处理:**
   - 移除字段后可能导致编译错误
   - 需要逐一检查并修复所有引用

4. **UI 布局调整:**
   - 移除可用哨位控件后，表单会变短
   - 可能需要调整间距和布局以保持美观
