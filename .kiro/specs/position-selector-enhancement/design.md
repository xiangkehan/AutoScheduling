# 设计文档

## 概述

本设计文档描述了哨位选择器增强功能的技术实现方案。该功能将在"按哨位展示"视图中添加一个智能哨位选择器，支持下拉选择和智能搜索（包括模糊匹配和拼音首字母匹配），使用户能够快速切换和查看不同哨位的排班情况。

### 使用场景分析

通过代码分析，发现以下场景需要哨位选择功能：

1. **ScheduleResultPage.xaml - 按哨位展示视图**
   - 当前实现：只显示哨位名称（TextBlock），无法切换
   - 状态：需要添加哨位选择器
   - 位置：PositionScheduleControl上方

2. **PositionScheduleControl.xaml**
   - 当前实现：显示单个哨位的排班详情
   - 状态：需要接收选中的哨位数据
   - 位置：控件内部

### 复用现有基础设施

本功能将复用人员选择器（personnel-selector-enhancement）中已实现的核心组件：

- **FuzzyMatcher**：模糊匹配算法
- **PinyinHelper**：拼音转换工具
- **PersonnelSearchHelper**：搜索辅助类（作为参考模式）

## 架构

### 组件层次结构

基于WinUI 3原生控件和现有基础设施的轻量级方案：

```
AutoSuggestBox (WinUI 3原生控件)
├── PositionSearchHelper (新增：封装哨位搜索逻辑)
├── FuzzyMatcher (复用：模糊匹配算法)
├── PinyinHelper (复用：拼音转换)
└── ScheduleResultViewModel (业务逻辑)
```

### 数据流

```
用户输入 → AutoSuggestBox.TextChanged
         → ViewModel.UpdatePositionSuggestions()
         → PositionSearchHelper.SearchAsync()
         → FuzzyMatcher.Match()
         → 更新建议列表
         → 用户选择 → 更新SelectedPositionSchedule
         → 刷新PositionScheduleControl
```

## 组件和接口

### 1. PositionSearchHelper 类（新增）

负责封装哨位搜索逻辑，可在多个ViewModel中复用。

**接口：**
```csharp
public class PositionSearchHelper
{
    private CancellationTokenSource? _debounceTokenSource;
    private const int DebounceDelayMs = 300;
    
    // 执行搜索（带防抖）
    public async Task<List<PositionScheduleData>> SearchAsync(
        string query,
        IEnumerable<PositionScheduleData> candidates,
        CancellationToken cancellationToken = default);
    
    // 立即搜索（不防抖）
    public List<PositionScheduleData> SearchImmediate(
        string query,
        IEnumerable<PositionScheduleData> candidates);
}
```

### 2. ScheduleResultViewModel 扩展

在现有的ScheduleResultViewModel中添加哨位选择器相关属性和方法。

**新增属性：**
```csharp
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

**新增方法：**
```csharp
// 更新哨位选择器建议列表
public async void UpdatePositionSelectorSuggestions(string query);

// 选择哨位
public void SelectPosition(PositionScheduleData position);

// 清空哨位选择
public void ClearPositionSelection();
```

### 3. ScheduleResultPage.xaml 扩展

在按哨位展示视图中添加哨位选择器UI。

**UI结构：**
```xml
<Grid Grid.Row="0"
      Visibility="{x:Bind ByPositionRadioButton.IsChecked, Mode=OneWay}">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>

    <!-- 哨位选择器 -->
    <Grid Grid.Row="0" Margin="0,0,0,12">
        <AutoSuggestBox x:Name="PositionSelectorAutoSuggestBox"
                        PlaceholderText="输入哨位名称或拼音首字母搜索..."
                        ItemsSource="{x:Bind ViewModel.PositionSelectorSuggestions, Mode=OneWay}"
                        Text="{x:Bind ViewModel.PositionSelectorSearchText, Mode=TwoWay}"
                        TextChanged="PositionSelectorAutoSuggestBox_TextChanged"
                        SuggestionChosen="PositionSelectorAutoSuggestBox_SuggestionChosen"
                        QuerySubmitted="PositionSelectorAutoSuggestBox_QuerySubmitted"
                        GotFocus="PositionSelectorAutoSuggestBox_GotFocus"/>
    </Grid>

    <!-- 哨位排班详情控件 -->
    <controls:PositionScheduleControl
        Grid.Row="1"
        ScheduleData="{x:Bind ViewModel.SelectedPositionSchedule, Mode=OneWay}"/>
</Grid>
```

## 数据模型

### PositionScheduleData（已存在）

```csharp
public class PositionScheduleData
{
    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public List<WeekData> Weeks { get; set; } = new();
    public int CurrentWeekIndex { get; set; }
    
    // 新增：用于显示的统计信息
    public int TotalShifts => Weeks.SelectMany(w => w.Shifts).Count();
}
```

### MatchResult（复用）

```csharp
public class MatchResult
{
    public object Item { get; set; }  // 可以是PersonnelDto或PositionScheduleData
    public int Score { get; set; }
    public MatchType Type { get; set; }
    public int EditDistance { get; set; }
    public double Similarity { get; set; }
}
```

## 正确性属性

*属性是一个特征或行为，应该在系统的所有有效执行中保持为真——本质上是关于系统应该做什么的正式陈述。属性作为人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1：输入响应一致性
*对于任意*输入文本，系统应该返回过滤后的建议列表，且建议列表中的所有哨位都应该匹配输入文本
**验证：需求 1.3**

### 属性 2：模糊匹配完整性
*对于任意*部分匹配的输入文本，系统应该返回所有包含该文本的哨位（不区分大小写）
**验证：需求 1.4, 2.1, 2.2**

### 属性 3：拼音匹配正确性
*对于任意*拼音首字母输入，系统应该返回所有哨位名称拼音首字母匹配的哨位
**验证：需求 1.5**

### 属性 4：选择状态一致性
*对于任意*哨位的选择操作，系统应该正确设置SelectedPositionSchedule属性并刷新PositionScheduleControl
**验证：需求 1.6, 3.2**

### 属性 5：大小写不敏感性
*对于任意*输入文本的大小写变体，系统应该返回相同的匹配结果
**验证：需求 2.1**

### 属性 6：非连续字符匹配
*对于任意*非连续字符序列，如果这些字符在哨位名称中按顺序出现，系统应该返回该哨位
**验证：需求 2.3**

### 属性 7：清空恢复一致性
*对于任意*搜索状态，清空搜索文本后应该恢复显示所有参与排班的哨位
**验证：需求 2.6**

### 属性 8：匹配度排序正确性
*对于任意*匹配结果集，系统应该按照匹配度排序，完全匹配的结果排在前面，然后是前缀匹配、子串匹配、拼音匹配、模糊匹配
**验证：需求 2.8**

### 属性 9：视图切换一致性
*对于任意*视图模式切换，当切换到"按哨位"视图时，系统应该显示哨位选择器并加载哨位数据
**验证：需求 1.1, 3.1**

### 属性 10：数据同步正确性
*对于任意*哨位选择，AutoSuggestBox显示的文本应该与SelectedPositionSchedule的哨位名称一致
**验证：需求 3.3**

### 属性 11：焦点展开一致性
*对于任意*哨位选择器获得焦点的操作，系统应该显示所有可用哨位的下拉列表
**验证：需求 3.4**

### 属性 12：空数据处理正确性
*对于任意*没有可用哨位数据的情况，系统应该显示"暂无哨位数据"提示
**验证：需求 3.5**

### 属性 13：防抖效果正确性
*对于任意*快速连续输入，系统应该减少搜索调用次数（使用防抖技术）
**验证：需求 6.2**

### 属性 14：拼音完全匹配正确性
*对于任意*完整拼音输入，系统应该返回拼音完全匹配的哨位
**验证：需求 1.5, 7.1**

### 属性 15：拼音前缀匹配正确性
*对于任意*拼音前缀输入，系统应该返回拼音以该前缀开头的哨位
**验证：需求 1.5, 7.2**

### 属性 16：编辑距离容错性
*对于任意*包含少量错误的输入（编辑距离≤阈值），系统应该返回相近的匹配结果
**验证：需求 7.3**

### 属性 17：标准化一致性
*对于任意*包含空格或特殊字符的输入，标准化后应该与不含这些字符的输入产生相同的匹配结果
**验证：需求 7.4**

### 属性 18：分数计算单调性
*对于任意*两个匹配结果，更精确的匹配应该获得更高的分数
**验证：需求 7.5**

### 属性 19：建议项信息完整性
*对于任意*建议列表中的哨位项，应该显示哨位名称和排班班次总数
**验证：需求 8.1, 8.2**

## 错误处理

### 1. 空数据处理
- 当PositionSchedules为null或空时，显示"暂无哨位数据"提示
- 搜索结果为空时，显示"未找到匹配的哨位"提示

### 2. 异常处理
- 模糊匹配算法异常：降级到简单的Contains匹配
- 拼音转换异常：跳过拼音匹配，只使用其他匹配方式
- 事件触发异常：记录日志但不影响UI

### 3. 性能保护
- 输入防抖：300ms延迟
- 最大结果数限制：50个
- 超时保护：搜索超过200ms自动取消

### 4. 数据验证
- 选择的哨位必须存在于PositionSchedules集合中
- 切换视图模式时自动加载哨位数据

## 测试策略

### 单元测试

使用xUnit框架进行单元测试，覆盖以下场景：

1. **PositionSearchHelper测试**
   - 测试SearchAsync方法
   - 测试SearchImmediate方法
   - 测试防抖逻辑
   - 测试取消令牌处理

2. **ScheduleResultViewModel测试**
   - 测试UpdatePositionSelectorSuggestions方法
   - 测试SelectPosition方法
   - 测试ClearPositionSelection方法
   - 测试属性变化通知

3. **FuzzyMatcher复用测试**
   - 测试对PositionScheduleData的匹配
   - 测试匹配分数计算
   - 测试排序逻辑

### 属性基础测试（Property-Based Testing）

使用FsCheck库进行属性基础测试，每个测试运行至少100次迭代：

1. **属性 1-19 的测试**
   - 为每个正确性属性编写对应的属性测试
   - 使用随机生成的输入数据
   - 验证属性在所有情况下都成立

2. **测试数据生成器**
   - PositionScheduleData生成器：生成随机哨位数据
   - 搜索文本生成器：生成各种搜索文本（中文、拼音、混合）

### 集成测试

1. **ScheduleResultPage集成测试**
   - 测试视图模式切换到"按哨位"
   - 测试哨位选择器的初始化
   - 测试哨位选择流程
   - 测试PositionScheduleControl的更新

### UI自动化测试

使用WinAppDriver进行UI自动化测试：

1. 测试键盘导航
2. 测试鼠标交互
3. 测试无障碍功能
4. 测试响应式布局

## 实现细节

### 复用FuzzyMatcher

FuzzyMatcher已经支持泛型匹配，可以直接用于PositionScheduleData：

```csharp
public static List<MatchResult> Match<T>(
    string query,
    IEnumerable<T> candidates,
    Func<T, string> nameSelector,
    FuzzyMatchOptions? options = null)
{
    // 实现细节...
}

// 使用示例
var results = FuzzyMatcher.Match(
    query,
    positionSchedules,
    p => p.PositionName,
    new FuzzyMatchOptions { EnablePinyinMatch = true }
);
```

### 复用PinyinHelper

PinyinHelper是静态类，可以直接调用：

```csharp
var initials = PinyinHelper.GetPinyinInitials("东门");  // "dm"
var fullPinyin = PinyinHelper.GetFullPinyin("东门");    // "dongmen"
```

### PositionSearchHelper实现

```csharp
public class PositionSearchHelper
{
    private CancellationTokenSource? _debounceTokenSource;
    private const int DebounceDelayMs = 300;
    
    public async Task<List<PositionScheduleData>> SearchAsync(
        string query,
        IEnumerable<PositionScheduleData> candidates,
        CancellationToken cancellationToken = default)
    {
        // 取消之前的搜索
        _debounceTokenSource?.Cancel();
        _debounceTokenSource = new CancellationTokenSource();
        
        try
        {
            // 防抖延迟
            await Task.Delay(DebounceDelayMs, _debounceTokenSource.Token);
            
            // 执行搜索
            return SearchImmediate(query, candidates);
        }
        catch (TaskCanceledException)
        {
            return new List<PositionScheduleData>();
        }
    }
    
    public List<PositionScheduleData> SearchImmediate(
        string query,
        IEnumerable<PositionScheduleData> candidates)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return candidates.ToList();
        }
        
        // 使用FuzzyMatcher进行匹配
        var results = FuzzyMatcher.Match(
            query,
            candidates,
            p => p.PositionName,
            new FuzzyMatchOptions
            {
                EnableFuzzyMatch = true,
                EnablePinyinMatch = true,
                EnableEditDistanceMatch = true,
                MaxResults = 50
            }
        );
        
        // 返回匹配的哨位
        return results
            .OrderByDescending(r => r.Score)
            .Select(r => (PositionScheduleData)r.Item)
            .ToList();
    }
}
```

### ViewModel集成

```csharp
public partial class ScheduleResultViewModel : ViewModelBase
{
    private readonly PositionSearchHelper _positionSearchHelper = new();
    
    // 更新哨位选择器建议列表
    public async void UpdatePositionSelectorSuggestions(string query)
    {
        if (PositionSchedules == null || !PositionSchedules.Any())
        {
            PositionSelectorSuggestions.Clear();
            return;
        }
        
        var results = await _positionSearchHelper.SearchAsync(
            query,
            PositionSchedules
        );
        
        PositionSelectorSuggestions.Clear();
        foreach (var position in results)
        {
            PositionSelectorSuggestions.Add(position);
        }
    }
    
    // 选择哨位
    public void SelectPosition(PositionScheduleData position)
    {
        if (position == null) return;
        
        SelectedPositionSchedule = position;
        PositionSelectorSearchText = position.PositionName;
    }
    
    // 清空哨位选择
    public void ClearPositionSelection()
    {
        SelectedPositionSchedule = null;
        PositionSelectorSearchText = string.Empty;
    }
}
```

### 事件处理

```csharp
// ScheduleResultPage.xaml.cs

private void PositionSelectorAutoSuggestBox_TextChanged(
    AutoSuggestBox sender,
    AutoSuggestBoxTextChangedEventArgs args)
{
    if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
    {
        ViewModel.UpdatePositionSelectorSuggestions(sender.Text);
    }
}

private void PositionSelectorAutoSuggestBox_SuggestionChosen(
    AutoSuggestBox sender,
    AutoSuggestBoxSuggestionChosenEventArgs args)
{
    if (args.SelectedItem is PositionScheduleData position)
    {
        ViewModel.SelectPosition(position);
    }
}

private void PositionSelectorAutoSuggestBox_QuerySubmitted(
    AutoSuggestBox sender,
    AutoSuggestBoxQuerySubmittedEventArgs args)
{
    if (args.ChosenSuggestion is PositionScheduleData position)
    {
        ViewModel.SelectPosition(position);
    }
    else if (!string.IsNullOrWhiteSpace(args.QueryText))
    {
        // 选择第一个匹配项
        var firstMatch = ViewModel.PositionSelectorSuggestions.FirstOrDefault();
        if (firstMatch != null)
        {
            ViewModel.SelectPosition(firstMatch);
        }
    }
}

private void PositionSelectorAutoSuggestBox_GotFocus(
    object sender,
    RoutedEventArgs e)
{
    // 显示所有可用哨位
    if (sender is AutoSuggestBox box)
    {
        ViewModel.UpdatePositionSelectorSuggestions(string.Empty);
        box.IsSuggestionListOpen = true;
    }
}
```

## 性能考虑

1. **搜索优化**
   - 复用FuzzyMatcher的优化算法
   - 缓存拼音转换结果（PinyinHelper已实现）
   - 限制最大结果数

2. **UI优化**
   - 使用虚拟化减少DOM元素
   - 使用防抖减少搜索频率
   - 异步加载哨位数据

3. **内存优化**
   - 及时释放CancellationTokenSource
   - 避免保存大量历史搜索结果
   - 使用弱引用缓存

## 无障碍支持

1. **AutomationProperties设置**
   ```xml
   <AutoSuggestBox AutomationProperties.Name="哨位选择器"
                   AutomationProperties.HelpText="输入哨位名称搜索或从下拉列表选择哨位"
                   AutomationProperties.LiveSetting="Polite"/>
   ```

2. **键盘导航**
   - Tab键：在控件间导航
   - 上下箭头：在建议列表中导航
   - 回车键：选择当前项
   - Esc键：关闭建议列表

3. **屏幕阅读器支持**
   - 为每个建议项设置AutomationProperties.Name
   - 使用LiveRegion通知状态变化
   - 提供清晰的焦点指示

## 依赖项

1. **现有组件**
   - FuzzyMatcher（已实现）
   - PinyinHelper（已实现）
   - PersonnelSearchHelper（作为参考模式）

2. **系统依赖**
   - .NET 8.0
   - WinUI 3
   - CommunityToolkit.Mvvm

## 向后兼容性

1. 保持现有的PositionScheduleControl实现不变
2. 新的哨位选择器作为可选组件添加到按哨位视图
3. 不影响其他视图模式（网格、列表、按人员）
4. 保持现有的数据模型和API不变
