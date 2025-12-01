# 人员选择器实现指南

## 概述

将原有的ComboBox替换为支持模糊搜索和拼音首字母匹配的AutoSuggestBox。

## 实现步骤

### 1. 更新XAML（ScheduleResultPage.xaml或其他页面）

将原有的ComboBox代码：

```xml
<ComboBox Grid.Column="1"
          x:Name="PersonnelSelectorComboBox"
          ItemsSource="{x:Bind ViewModel.PersonnelSchedules, Mode=OneWay}"
          SelectedItem="{x:Bind ViewModel.SelectedPersonnelSchedule, Mode=TwoWay}"
          ...>
```

替换为：

```xml
<AutoSuggestBox Grid.Column="1"
                x:Name="PersonnelSearchBox"
                PlaceholderText="搜索或选择人员（支持拼音首字母）"
                QueryIcon="Find"
                HorizontalAlignment="Stretch"
                MinWidth="300"
                TextChanged="PersonnelSearchBox_TextChanged"
                GotFocus="PersonnelSearchBox_GotFocus"
                SuggestionChosen="PersonnelSearchBox_SuggestionChosen"
                QuerySubmitted="PersonnelSearchBox_QuerySubmitted"
                AutomationProperties.Name="人员搜索框"
                AutomationProperties.HelpText="输入人员姓名进行模糊搜索，支持拼音首字母，或从下拉列表选择"
                ToolTipService.ToolTip="支持模糊搜索和拼音首字母匹配（如：输入'zs'可以找到'张三'）">
    <AutoSuggestBox.ItemTemplate>
        <DataTemplate x:DataType="dto:PersonnelScheduleData">
            <Grid Padding="8">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <TextBlock Grid.Column="0"
                           Text="{x:Bind PersonnelName}"
                           FontWeight="SemiBold"
                           VerticalAlignment="Center"/>

                <StackPanel Grid.Column="1"
                            Orientation="Horizontal"
                            Spacing="8">
                    <TextBlock Text="{x:Bind Workload.TotalShifts}"
                               Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                               VerticalAlignment="Center"/>
                    <TextBlock Text="班次"
                               Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                               VerticalAlignment="Center"/>
                </StackPanel>
            </Grid>
        </DataTemplate>
    </AutoSuggestBox.ItemTemplate>
</AutoSuggestBox>
```

### 2. 更新代码后端（ScheduleResultPage.xaml.cs）

添加以下事件处理方法：

```csharp
#region 人员搜索功能

private async void PersonnelSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
{
    if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
    {
        var searchText = sender.Text;
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            sender.ItemsSource = ViewModel.PersonnelSchedules.Take(10).ToList();
        }
        else
        {
            var results = await ViewModel.SearchPersonnelAsync(searchText);
            sender.ItemsSource = results.Take(10).ToList();
        }
    }
}

private void PersonnelSearchBox_GotFocus(object sender, RoutedEventArgs e)
{
    if (sender is AutoSuggestBox searchBox && string.IsNullOrWhiteSpace(searchBox.Text))
    {
        searchBox.ItemsSource = ViewModel.PersonnelSchedules.Take(10).ToList();
    }
}

private void PersonnelSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
{
    if (args.SelectedItem is PersonnelScheduleData personnel)
    {
        sender.Text = personnel.PersonnelName;
        ViewModel.SelectedPersonnelSchedule = personnel;
    }
}

private void PersonnelSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
{
    if (args.ChosenSuggestion is PersonnelScheduleData personnel)
    {
        ViewModel.SelectedPersonnelSchedule = personnel;
    }
    else if (!string.IsNullOrWhiteSpace(args.QueryText))
    {
        var firstMatch = (sender.ItemsSource as List<PersonnelScheduleData>)?.FirstOrDefault();
        if (firstMatch != null)
        {
            sender.Text = firstMatch.PersonnelName;
            ViewModel.SelectedPersonnelSchedule = firstMatch;
        }
    }
}

#endregion
```

### 3. 更新ViewModel（ScheduleResultViewModel.cs）

添加搜索方法：

```csharp
#region 人员搜索功能

/// <summary>
/// 搜索人员（支持模糊匹配和拼音首字母）
/// </summary>
public async Task<List<PersonnelScheduleData>> SearchPersonnelAsync(string searchText)
{
    if (string.IsNullOrWhiteSpace(searchText))
    {
        return PersonnelSchedules.ToList();
    }

    try
    {
        // 转换为PersonnelDto进行搜索
        var personnelDtos = PersonnelSchedules.Select(p => new PersonnelDto
        {
            Id = p.PersonnelId,
            Name = p.PersonnelName
        }).ToList();

        var matchedDtos = await _searchHelper.SearchAsync(searchText, personnelDtos);

        // 转换回PersonnelScheduleData
        var matchedIds = matchedDtos.Select(d => d.Id).ToHashSet();
        var results = PersonnelSchedules
            .Where(p => matchedIds.Contains(p.PersonnelId))
            .ToList();

        return results;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"人员搜索失败: {ex.Message}");
        
        // 降级到简单匹配
        return PersonnelSchedules
            .Where(p => p.PersonnelName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

#endregion
```

注意：ViewModel中已经有`_searchHelper`字段：
```csharp
private readonly PersonnelSearchHelper _searchHelper = new();
```

### 4. 添加必要的using语句

确保文件顶部包含：

```csharp
using System.Linq;
using System.Collections.Generic;
using AutoScheduling3.DTOs;
using AutoScheduling3.Helpers;
```

## 功能特性

✅ **模糊匹配**：支持完全匹配、前缀匹配、子串匹配、模糊序列匹配  
✅ **拼音首字母搜索**：输入"zs"可以找到"张三"  
✅ **实时搜索建议**：输入时自动显示匹配结果  
✅ **防抖优化**：300ms延迟避免频繁搜索  
✅ **无障碍支持**：完整的键盘导航和屏幕阅读器支持  
✅ **智能排序**：按匹配度自动排序结果  

## 搜索示例

假设有人员："张三"、"李四"、"王五"、"张三丰"、"李小龙"

- 搜索 "张" → 返回："张三"、"张三丰"（前缀匹配）
- 搜索 "三" → 返回："张三"、"张三丰"（子串匹配）
- 搜索 "zs" → 返回："张三"（拼音首字母匹配）
- 搜索 "lxl" → 返回："李小龙"（拼音首字母匹配）
- 搜索 "张丰" → 返回："张三丰"（模糊序列匹配）

## 测试建议

1. **基本搜索**：输入完整姓名，验证能找到对应人员
2. **拼音搜索**：输入拼音首字母，验证能找到对应人员
3. **模糊搜索**：输入部分字符，验证能找到相关人员
4. **空输入**：清空搜索框，验证显示所有人员
5. **无结果**：输入不存在的内容，验证无结果提示
6. **键盘导航**：使用Tab、上下箭头、回车键测试
7. **性能测试**：在大量人员数据下测试搜索响应速度

## 故障排除

### 问题1：搜索不工作
- 检查`_searchHelper`是否已初始化
- 检查TinyPinyin.NET包是否已安装
- 查看调试输出中的错误信息

### 问题2：拼音匹配不准确
- 确认使用的是TinyPinyin.NET包
- 检查PinyinHelper.cs中的实现

### 问题3：搜索响应慢
- 检查PersonnelSchedules数据量
- 考虑限制显示结果数量（已默认限制为10个）
- 验证防抖延迟设置（默认300ms）

## 参考文档

详细使用说明请参考：`Helpers/PersonnelSelector.README.md`
