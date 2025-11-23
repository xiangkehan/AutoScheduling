# 人员选择器使用指南

## 概述

人员选择器功能提供了强大的模糊搜索和拼音匹配能力，让用户可以快速找到目标人员。本指南说明如何在项目中使用这些功能。

## 核心组件

### 1. PinyinHelper

提供中文转拼音和拼音首字母提取功能。

```csharp
using AutoScheduling3.Helpers;

// 获取拼音首字母
var initials = PinyinHelper.GetPinyinInitials("张三");  // 返回 "zs"

// 获取完整拼音
var fullPinyin = PinyinHelper.GetFullPinyin("张三");  // 返回 "zhang san"

// 清空缓存（可选）
PinyinHelper.ClearCache();
```

### 2. FuzzyMatcher

提供多层模糊匹配算法。

```csharp
using AutoScheduling3.Helpers;
using AutoScheduling3.DTOs;

// 准备候选人员列表
var candidates = new List<PersonnelDto>
{
    new PersonnelDto { Id = 1, Name = "张三" },
    new PersonnelDto { Id = 2, Name = "李四" },
    new PersonnelDto { Id = 3, Name = "王五" }
};

// 执行模糊匹配
var results = FuzzyMatcher.Match("zs", candidates);

// 配置匹配选项
var options = new FuzzyMatchOptions
{
    EnableFuzzyMatch = true,      // 启用模糊匹配
    EnablePinyinMatch = true,     // 启用拼音匹配
    CaseSensitive = false,        // 不区分大小写
    MaxResults = 50,              // 最多返回50个结果
    MinScore = 0                  // 最小匹配分数
};

var results = FuzzyMatcher.Match("张", candidates, options);

// 结果按分数降序排序
foreach (var result in results)
{
    Console.WriteLine($"{result.Personnel.Name} - 分数: {result.Score} - 类型: {result.Type}");
}
```

### 3. PersonnelSearchHelper

封装搜索逻辑，支持防抖和异步搜索。

```csharp
using AutoScheduling3.Helpers;

// 创建搜索助手实例
var searchHelper = new PersonnelSearchHelper();

// 异步搜索（带300ms防抖）
var results = await searchHelper.SearchAsync("张三", candidates);

// 立即搜索（不防抖）
var results = searchHelper.SearchImmediate("李四", candidates);

// 取消当前搜索
searchHelper.CancelSearch();

// 释放资源
searchHelper.Dispose();
```

## 匹配策略

系统使用5层匹配策略，按优先级排序：

1. **完全匹配**（分数：100）
   - 输入："张三" → 匹配："张三"

2. **前缀匹配**（分数：90）
   - 输入："张" → 匹配："张三"、"张三丰"

3. **子串匹配**（分数：70）
   - 输入："三" → 匹配："张三"、"李三"

4. **拼音首字母匹配**（分数：60）
   - 输入："zs" → 匹配："张三"（Zhang San）
   - 输入："ls" → 匹配："李四"（Li Si）

5. **模糊匹配**（分数：40）
   - 输入："张四" → 匹配："张三四"
   - 字符按顺序出现即可

## 在AutoSuggestBox中使用

### XAML配置

```xml
<AutoSuggestBox x:Name="PersonnelSearchBox"
                PlaceholderText="搜索或选择人员"
                QueryIcon="Find"
                TextChanged="PersonnelSearchBox_TextChanged"
                GotFocus="PersonnelSearchBox_GotFocus"
                SuggestionChosen="PersonnelSearchBox_SuggestionChosen"
                QuerySubmitted="PersonnelSearchBox_QuerySubmitted"
                AutomationProperties.Name="人员搜索框"
                AutomationProperties.HelpText="输入人员姓名进行模糊搜索，支持拼音首字母"
                ToolTipService.ToolTip="支持模糊搜索和拼音首字母匹配">
    <AutoSuggestBox.ItemTemplate>
        <DataTemplate x:DataType="dto:PersonnelDto">
            <TextBlock Text="{x:Bind Name}" 
                      Padding="8,4"
                      VerticalAlignment="Center"/>
        </DataTemplate>
    </AutoSuggestBox.ItemTemplate>
</AutoSuggestBox>
```

### Code-Behind实现

```csharp
using AutoScheduling3.Helpers;
using AutoScheduling3.DTOs;

public sealed partial class MyPage : Page
{
    private readonly PersonnelSearchHelper _searchHelper = new();
    private List<PersonnelDto> _allPersonnel = new();
    
    // 文本改变事件
    private async void PersonnelSearchBox_TextChanged(
        AutoSuggestBox sender, 
        AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var searchText = sender.Text?.Trim() ?? string.Empty;
            var results = await _searchHelper.SearchAsync(searchText, _allPersonnel);
            sender.ItemsSource = results.Take(10).ToList();
        }
    }
    
    // 获得焦点时显示所有人员
    private void PersonnelSearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is AutoSuggestBox searchBox && 
            string.IsNullOrWhiteSpace(searchBox.Text))
        {
            searchBox.ItemsSource = _allPersonnel.Take(10).ToList();
        }
    }
    
    // 选择建议项
    private void PersonnelSearchBox_SuggestionChosen(
        AutoSuggestBox sender, 
        AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is PersonnelDto personnel)
        {
            sender.Text = personnel.Name;
            // 处理选择逻辑
        }
    }
    
    // 提交查询
    private void PersonnelSearchBox_QuerySubmitted(
        AutoSuggestBox sender, 
        AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is PersonnelDto personnel)
        {
            // 用户选择了建议项
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            // 用户按了回车，选择第一个匹配项
            var firstMatch = (sender.ItemsSource as List<PersonnelDto>)?.FirstOrDefault();
            if (firstMatch != null)
            {
                sender.Text = firstMatch.Name;
            }
        }
    }
}
```

## 性能优化建议

1. **使用防抖**：对于实时搜索，使用`SearchAsync`而不是`SearchImmediate`
2. **限制结果数**：只显示前10-20个结果，避免渲染过多项
3. **缓存利用**：PinyinHelper自动缓存拼音转换结果
4. **虚拟化**：对于大列表，使用ListView的虚拟化功能

## 无障碍支持

确保设置适当的AutomationProperties：

```xml
<AutoSuggestBox AutomationProperties.Name="人员搜索框"
                AutomationProperties.HelpText="输入人员姓名进行模糊搜索，支持拼音首字母"
                AutomationProperties.LiveSetting="Polite"/>
```

## 示例场景

### 场景1：EditShiftAssignmentDialog

修改班次分配对话框中的人员选择，支持搜索框和列表双向同步。

参考实现：`Views/Scheduling/EditShiftAssignmentDialog.xaml.cs`

### 场景2：ScheduleResultPage

排班结果页面的人员筛选，支持点击展开显示所有人员。

参考实现：`Views/Scheduling/ScheduleResultPage.xaml.cs`

## 常见问题

### Q: 如何自定义匹配分数？

A: 修改`FuzzyMatchOptions.MinScore`来设置最小匹配分数阈值。

### Q: 如何禁用拼音匹配？

A: 设置`FuzzyMatchOptions.EnablePinyinMatch = false`。

### Q: 搜索性能如何优化？

A: 使用`SearchAsync`启用防抖，限制`MaxResults`数量，使用虚拟化渲染。

### Q: 如何处理搜索失败？

A: PersonnelSearchHelper内置降级策略，会自动回退到简单的Contains匹配。

## 依赖项

- TinyPinyin.NET：用于拼音转换
- .NET 8.0
- WinUI 3
- CommunityToolkit.Mvvm

## 更新日志

- 2024-01: 初始版本，支持模糊匹配和拼音首字母匹配
