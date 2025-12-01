# 哨位选择器使用指南

## 概述

哨位选择器是一个智能搜索组件，用于在"按哨位展示"视图中快速切换和搜索不同的哨位。它支持模糊匹配、拼音首字母匹配和完整拼音匹配，提供流畅的用户体验。

## 核心组件

### 1. PositionSearchHelper

封装哨位搜索逻辑的辅助类，支持防抖和模糊匹配。

**主要方法：**

```csharp
// 执行搜索（带防抖，300ms延迟）
public async Task<List<PositionScheduleData>> SearchAsync(
    string query,
    IEnumerable<PositionScheduleData> candidates,
    CancellationToken cancellationToken = default)

// 立即执行搜索（不防抖）
public List<PositionScheduleData> SearchImmediate(
    string query,
    IEnumerable<PositionScheduleData> candidates)
```

**使用示例：**

```csharp
// 在 ViewModel 中创建实例
private readonly PositionSearchHelper _positionSearchHelper = new();

// 执行搜索
var results = await _positionSearchHelper.SearchAsync(
    "东门",
    PositionSchedules
);

// 或立即搜索（用于 AutoSuggestBox）
var results = _positionSearchHelper.SearchImmediate(
    "dm",
    PositionSchedules
);
```

### 2. PositionFuzzyMatcher

提供增强的多层匹配策略的哨位模糊匹配器。

**匹配策略（按优先级排序）：**

1. **完全匹配**（分数：100）- 查询文本与哨位名称完全相同
2. **前缀匹配**（分数：90）- 哨位名称以查询文本开头
3. **拼音完全匹配**（分数：85）- 查询文本与哨位名称的完整拼音完全相同
4. **拼音前缀匹配**（分数：75）- 哨位名称的完整拼音以查询文本开头
5. **子串匹配**（分数：70）- 哨位名称包含查询文本
6. **拼音首字母完全匹配**（分数：65）- 查询文本与哨位名称的拼音首字母完全相同
7. **拼音首字母前缀匹配**（分数：55）- 哨位名称的拼音首字母以查询文本开头
8. **编辑距离匹配**（分数：30-50）- 查询文本与哨位名称的编辑距离在阈值内
9. **序列模糊匹配**（分数：25）- 查询字符按顺序出现在哨位名称中

**使用示例：**

```csharp
var options = new FuzzyMatchOptions
{
    EnableFuzzyMatch = true,
    EnablePinyinMatch = true,
    EnableFullPinyinMatch = true,
    EnablePinyinInitialsMatch = true,
    EnableEditDistanceMatch = true,
    CaseSensitive = false,
    MaxResults = 50,
    MinScore = 1
};

var matchResults = PositionFuzzyMatcher.Match(
    "东门",
    positionSchedules,
    options
);

// 获取匹配的哨位列表
var positions = matchResults.Select(r => r.Position).ToList();
```

### 3. PinyinHelper

提供中文转拼音和拼音首字母提取功能（复用现有组件）。

**主要方法：**

```csharp
// 获取拼音首字母
var initials = PinyinHelper.GetPinyinInitials("东门");  // "dm"

// 获取完整拼音
var fullPinyin = PinyinHelper.GetFullPinyin("东门");    // "dongmen"
```

## 在 ViewModel 中集成

### 1. 添加必要的属性

```csharp
// 搜索辅助类实例
private readonly PositionSearchHelper _positionSearchHelper = new();

// 哨位选择器建议列表
private ObservableCollection<PositionScheduleData> _positionSelectorSuggestions = new();
public ObservableCollection<PositionScheduleData> PositionSelectorSuggestions
{
    get => _positionSelectorSuggestions;
    set => SetProperty(ref _positionSelectorSuggestions, value);
}

// 哨位选择器搜索文本
private string _positionSelectorSearchText = string.Empty;
public string PositionSelectorSearchText
{
    get => _positionSelectorSearchText;
    set => SetProperty(ref _positionSelectorSearchText, value);
}
```

### 2. 实现搜索方法

```csharp
/// <summary>
/// 更新哨位选择器的建议列表
/// </summary>
public void UpdatePositionSelectorSuggestions(string searchText)
{
    try
    {
        if (PositionSchedules == null || !PositionSchedules.Any())
        {
            PositionSelectorSuggestions.Clear();
            return;
        }

        // 使用立即搜索（不防抖）
        var results = _positionSearchHelper.SearchImmediate(searchText, PositionSchedules);
        PositionSelectorSuggestions = new ObservableCollection<PositionScheduleData>(results);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"更新哨位选择器建议失败: {ex.Message}");
        
        // 降级到简单匹配
        if (string.IsNullOrWhiteSpace(searchText))
        {
            PositionSelectorSuggestions = new ObservableCollection<PositionScheduleData>(PositionSchedules);
        }
        else
        {
            var results = PositionSchedules
                .Where(p => p.PositionName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            PositionSelectorSuggestions = new ObservableCollection<PositionScheduleData>(results);
        }
    }
}

/// <summary>
/// 选择哨位
/// </summary>
public void SelectPosition(PositionScheduleData position)
{
    if (position == null) return;

    SelectedPositionSchedule = position;
    PositionSelectorSearchText = position.PositionName;
}

/// <summary>
/// 清空哨位选择
/// </summary>
public void ClearPositionSelection()
{
    SelectedPositionSchedule = null;
    PositionSelectorSearchText = string.Empty;
}
```

### 3. 在视图模式切换时初始化

```csharp
private async Task OnViewModeChangedAsync(ViewMode newMode)
{
    if (Schedule == null) return;

    try
    {
        IsLoading = true;

        switch (newMode)
        {
            case ViewMode.ByPosition:
                if (PositionSchedules.Count == 0)
                {
                    await BuildPositionScheduleDataAsync();
                }
                // 初始化哨位选择器
                if (PositionSchedules.Count > 0)
                {
                    PositionSelectorSuggestions = new ObservableCollection<PositionScheduleData>(PositionSchedules);
                    if (SelectedPositionSchedule == null)
                    {
                        SelectedPositionSchedule = PositionSchedules[0];
                        PositionSelectorSearchText = SelectedPositionSchedule.PositionName;
                    }
                }
                break;
        }
    }
    finally
    {
        IsLoading = false;
    }
}
```

## 在 XAML 中配置

### AutoSuggestBox 配置

```xml
<AutoSuggestBox x:Name="PositionSelectorAutoSuggestBox"
                PlaceholderText="输入哨位名称或拼音首字母搜索..."
                ItemsSource="{x:Bind ViewModel.PositionSelectorSuggestions, Mode=OneWay}"
                Text="{x:Bind ViewModel.PositionSelectorSearchText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                HorizontalAlignment="Stretch"
                MinWidth="300"
                QueryIcon="Find"
                TextChanged="PositionSelectorAutoSuggestBox_TextChanged"
                SuggestionChosen="PositionSelectorAutoSuggestBox_SuggestionChosen"
                QuerySubmitted="PositionSelectorAutoSuggestBox_QuerySubmitted"
                GotFocus="PositionSelectorAutoSuggestBox_GotFocus"
                AutomationProperties.Name="哨位选择搜索框"
                AutomationProperties.HelpText="输入哨位名称或拼音首字母搜索哨位，支持模糊匹配"
                AutomationProperties.LiveSetting="Polite">
    <AutoSuggestBox.ItemTemplate>
        <DataTemplate x:DataType="dto:PositionScheduleData">
            <Grid Padding="8">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <TextBlock Grid.Column="0"
                           Text="{x:Bind PositionName}"
                           FontWeight="SemiBold"
                           VerticalAlignment="Center"/>

                <StackPanel Grid.Column="1"
                            Orientation="Horizontal"
                            Spacing="4"
                            VerticalAlignment="Center">
                    <TextBlock Text="班次:"
                               FontSize="12"
                               Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
                    <TextBlock Text="{x:Bind TotalShifts}"
                               FontSize="12"
                               FontWeight="SemiBold"
                               Foreground="{ThemeResource AccentTextFillColorPrimaryBrush}"/>
                </StackPanel>
            </Grid>
        </DataTemplate>
    </AutoSuggestBox.ItemTemplate>
</AutoSuggestBox>
```

### 事件处理（Code-Behind）

```csharp
/// <summary>
/// 哨位选择器文本改变时的处理
/// </summary>
private void PositionSelectorAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
{
    // 只处理用户输入的情况
    if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
    {
        ViewModel.UpdatePositionSelectorSuggestions(sender.Text);
    }
}

/// <summary>
/// 哨位选择器选择建议项时的处理
/// </summary>
private void PositionSelectorAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
{
    if (args.SelectedItem is PositionScheduleData positionSchedule)
    {
        ViewModel.SelectPosition(positionSchedule);
    }
}

/// <summary>
/// 哨位选择器提交查询时的处理
/// </summary>
private void PositionSelectorAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
{
    if (args.ChosenSuggestion is PositionScheduleData positionSchedule)
    {
        // 用户从下拉列表选择了一个项
        ViewModel.SelectPosition(positionSchedule);
    }
    else if (!string.IsNullOrWhiteSpace(args.QueryText))
    {
        // 用户直接输入并按回车，选择第一个匹配项
        var matchedPosition = ViewModel.PositionSelectorSuggestions.FirstOrDefault();
        if (matchedPosition != null)
        {
            ViewModel.SelectPosition(matchedPosition);
        }
    }
}

/// <summary>
/// 哨位选择器获得焦点时显示所有哨位
/// </summary>
private void PositionSelectorAutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
{
    if (sender is AutoSuggestBox searchBox)
    {
        // 显示所有可用哨位
        ViewModel.UpdatePositionSelectorSuggestions(string.Empty);
        searchBox.IsSuggestionListOpen = true;
    }
}
```

## 搜索示例

### 1. 完全匹配

输入：`东门`
匹配：`东门` （分数：100）

### 2. 前缀匹配

输入：`东`
匹配：`东门`、`东岗` （分数：90）

### 3. 拼音首字母匹配

输入：`dm`
匹配：`东门` （分数：65）

### 4. 完整拼音匹配

输入：`dongmen`
匹配：`东门` （分数：85）

### 5. 拼音前缀匹配

输入：`dong`
匹配：`东门`、`东岗` （分数：75）

### 6. 子串匹配

输入：`门`
匹配：`东门`、`西门`、`南门` （分数：70）

### 7. 编辑距离匹配

输入：`dongmne`（拼写错误）
匹配：`东门` （分数：40，编辑距离：1）

### 8. 序列模糊匹配

输入：`dmen`
匹配：`东门` （分数：25）

## 性能优化

### 1. 防抖机制

搜索使用300ms的防抖延迟，减少频繁的搜索调用：

```csharp
private const int DebounceDelayMs = 300;
```

### 2. 拼音缓存

PinyinHelper 内部使用缓存机制，避免重复计算拼音：

```csharp
private static readonly Dictionary<string, string> _pinyinInitialsCache = new();
private static readonly Dictionary<string, string> _fullPinyinCache = new();
```

### 3. 结果数量限制

默认最多返回50个结果：

```csharp
MaxResults = 50
```

### 4. 最小分数过滤

只返回分数大于等于1的结果：

```csharp
MinScore = 1
```

## 错误处理

### 1. 空数据处理

```csharp
if (PositionSchedules == null || !PositionSchedules.Any())
{
    PositionSelectorSuggestions.Clear();
    return;
}
```

### 2. 异常降级

当模糊匹配失败时，自动降级到简单的 Contains 匹配：

```csharp
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"搜索执行失败: {ex.Message}");
    
    // 降级到简单的Contains匹配
    return candidateList
        .Where(p => p.PositionName.Contains(query, StringComparison.OrdinalIgnoreCase))
        .ToList();
}
```

## 无障碍支持

### 1. AutomationProperties 设置

```xml
AutomationProperties.Name="哨位选择搜索框"
AutomationProperties.HelpText="输入哨位名称或拼音首字母搜索哨位，支持模糊匹配"
AutomationProperties.LiveSetting="Polite"
```

### 2. 键盘导航

- **Tab 键**：在控件间导航
- **上下箭头**：在建议列表中导航
- **回车键**：选择当前项
- **Esc 键**：关闭建议列表

### 3. 屏幕阅读器支持

- 控件获得焦点时，屏幕阅读器会朗读控件的用途和当前状态
- 建议列表显示时，每个建议项都包含完整的信息（哨位名称和班次总数）
- 状态改变时，通过 LiveSetting 通知辅助技术

## 与人员选择器的关系

哨位选择器的实现模式与人员选择器（PersonnelSearchHelper）保持一致：

- 相同的搜索策略和匹配算法
- 相同的防抖机制
- 相同的事件处理模式
- 相同的无障碍支持

这种一致性确保了代码的可维护性和用户体验的统一性。

## 扩展 PositionScheduleData

为了在建议列表中显示班次总数，PositionScheduleData 类添加了 TotalShifts 计算属性：

```csharp
/// <summary>
/// 班次总数（统计所有周次的已分配班次）
/// </summary>
public int TotalShifts => Weeks
    .SelectMany(w => w.Cells.Values)
    .Count(c => c.IsAssigned);
```

## 最佳实践

1. **始终提供降级方案**：当模糊匹配失败时，使用简单的 Contains 匹配
2. **使用防抖减少搜索频率**：避免在用户快速输入时频繁触发搜索
3. **缓存拼音转换结果**：提升性能，避免重复计算
4. **限制结果数量**：避免返回过多结果影响性能
5. **提供清晰的视觉反馈**：在建议项中显示关键信息（哨位名称、班次总数）
6. **支持多种输入方式**：中文、拼音首字母、完整拼音、模糊匹配
7. **确保无障碍支持**：设置 AutomationProperties，支持键盘导航和屏幕阅读器

## 故障排除

### 问题：搜索结果为空

**可能原因：**
- PositionSchedules 集合为空或未初始化
- 查询文本与任何哨位名称都不匹配
- MinScore 设置过高

**解决方案：**
- 确保在切换到"按哨位"视图时已加载 PositionSchedules 数据
- 检查查询文本是否正确
- 降低 MinScore 或使用降级匹配

### 问题：拼音匹配不工作

**可能原因：**
- PinyinHelper 未正确初始化
- TinyPinyin 库未正确引用
- 哨位名称不包含中文字符

**解决方案：**
- 检查 TinyPinyin 库是否正确安装
- 确保 EnablePinyinMatch 选项已启用
- 检查哨位名称是否包含中文字符

### 问题：性能问题

**可能原因：**
- 哨位数量过多（超过100个）
- 防抖延迟过短
- 未使用缓存

**解决方案：**
- 增加防抖延迟时间
- 减少 MaxResults 限制
- 确保 PinyinHelper 的缓存机制正常工作
- 考虑使用虚拟化技术渲染建议列表

## 参考资料

- [PersonnelSearchHelper 实现](../Helpers/PersonnelSearchHelper.cs)
- [FuzzyMatcher 实现](../Helpers/FuzzyMatcher.cs)
- [PinyinHelper 实现](../Helpers/PinyinHelper.cs)
- [WinUI 3 AutoSuggestBox 文档](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.autosuggestbox)
