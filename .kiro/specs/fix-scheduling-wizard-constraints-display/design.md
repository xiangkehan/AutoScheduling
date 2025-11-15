# Design Document

## Overview

本设计文档描述了如何解决排班向导约束页面不显示内容的问题。问题的根本原因可能包括：

1. 约束数据加载时机不正确
2. 数据库中没有约束数据
3. 数据绑定问题
4. 异步加载状态管理问题

解决方案将包括：
- 改进约束数据加载逻辑和时机
- 添加详细的日志记录以便诊断
- 改进错误处理和用户反馈
- 确保数据绑定正确工作
- 添加空状态提示

## Architecture

### 组件交互流程

```
CreateSchedulingPage (View)
    ↓ 用户导航到第4步
SchedulingViewModel
    ↓ 触发 LoadConstraintsCommand
SchedulingService
    ↓ 调用约束加载方法
ConstraintRepository
    ↓ 查询数据库
SQLite Database
    ↓ 返回约束数据
ConstraintRepository
    ↓ 映射为模型对象
SchedulingService
    ↓ 返回约束列表
SchedulingViewModel
    ↓ 更新 ObservableCollection
CreateSchedulingPage (View)
    ↓ UI 自动更新显示
```

### 数据流

1. **触发加载**：用户导航到第4步或日期范围变化
2. **设置加载状态**：`IsLoadingConstraints = true`
3. **并行加载数据**：
   - HolidayConfigs
   - FixedPositionRules
   - ManualAssignments
4. **更新集合**：将数据填充到 ObservableCollection
5. **应用模板约束**：如果从模板加载，设置启用状态
6. **清除加载状态**：`IsLoadingConstraints = false`
7. **UI 更新**：通过数据绑定自动刷新

## Components and Interfaces

### 1. SchedulingViewModel 改进

**现有问题**：
- `LoadConstraintsAsync` 方法可能在某些情况下没有被正确调用
- 缺少详细的日志记录
- 错误处理不够完善

**改进方案**：

```csharp
private async Task LoadConstraintsAsync()
{
    if (IsLoadingConstraints)
    {
        System.Diagnostics.Debug.WriteLine("约束数据正在加载中，跳过重复请求");
        return;
    }
    
    IsLoadingConstraints = true;
    System.Diagnostics.Debug.WriteLine($"=== 开始加载约束数据 ===");
    System.Diagnostics.Debug.WriteLine($"日期范围: {StartDate:yyyy-MM-dd} 到 {EndDate:yyyy-MM-dd}");
    
    try
    {
        // 并行加载三种约束数据
        var configsTask = _schedulingService.GetHolidayConfigsAsync();
        var rulesTask = _schedulingService.GetFixedPositionRulesAsync(true);
        var manualTask = _schedulingService.GetManualAssignmentsAsync(
            StartDate.Date, EndDate.Date, true);
        
        await Task.WhenAll(configsTask, rulesTask, manualTask);
        
        var configs = configsTask.Result;
        var rules = rulesTask.Result;
        var manuals = manualTask.Result;
        
        System.Diagnostics.Debug.WriteLine($"加载完成 - 休息日配置: {configs.Count}, 定岗规则: {rules.Count}, 手动指定: {manuals.Count}");
        
        // 更新 ObservableCollection
        HolidayConfigs = new ObservableCollection<HolidayConfig>(configs);
        FixedPositionRules = new ObservableCollection<FixedPositionRule>(rules);
        ManualAssignments = new ObservableCollection<ManualAssignment>(manuals);
        
        // 如果数据为空，记录警告
        if (configs.Count == 0)
            System.Diagnostics.Debug.WriteLine("警告: 没有找到休息日配置");
        if (rules.Count == 0)
            System.Diagnostics.Debug.WriteLine("警告: 没有找到定岗规则");
        if (manuals.Count == 0)
            System.Diagnostics.Debug.WriteLine("警告: 没有找到手动指定");
        
        // 应用模板约束（如果有）
        if (TemplateApplied)
        {
            System.Diagnostics.Debug.WriteLine("应用模板约束设置");
            ApplyTemplateConstraints();
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"=== 加载约束数据失败 ===");
        System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
        System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
        System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        
        await _dialogService.ShowErrorAsync("加载约束数据失败", ex);
    }
    finally
    {
        IsLoadingConstraints = false;
        System.Diagnostics.Debug.WriteLine($"=== 约束数据加载流程结束 ===");
    }
}
```

### 2. 加载时机优化

**问题**：约束数据可能在错误的时机加载或根本没有加载

**解决方案**：

1. **进入第4步时自动加载**：
```csharp
private void NextStep()
{
    if (CurrentStep < 5 && CanGoNext())
    {
        if (TemplateApplied && CurrentStep == 1)
        {
            CurrentStep = 5;
            BuildSummarySections();
        }
        else
        {
            CurrentStep++;
        }

        // 进入第4步时加载约束
        if (CurrentStep == 4)
        {
            System.Diagnostics.Debug.WriteLine("进入第4步，触发约束数据加载");
            _ = LoadConstraintsAsync();
        }
        
        if (CurrentStep == 5)
        {
            BuildSummarySections();
        }
        RefreshCommandStates();
    }
}
```

2. **CurrentStep 属性变化时加载**：
```csharp
partial void OnCurrentStepChanged(int value)
{
    System.Diagnostics.Debug.WriteLine($"当前步骤变更为: {value}");
    RefreshCommandStates();
    
    if (value == 4)
    {
        System.Diagnostics.Debug.WriteLine("步骤变更到第4步，检查是否需要加载约束");
        // 只有在数据为空时才加载
        if (FixedPositionRules.Count == 0 && !IsLoadingConstraints)
        {
            System.Diagnostics.Debug.WriteLine("约束数据为空，开始加载");
            _ = LoadConstraintsAsync();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"约束数据已存在或正在加载 (规则数: {FixedPositionRules.Count}, 加载中: {IsLoadingConstraints})");
        }
    }
    else if (value == 5)
    {
        BuildSummarySections();
    }
}
```

3. **日期范围变化时重新加载**：
```csharp
partial void OnStartDateChanged(DateTimeOffset value)
{
    if (EndDate < value) EndDate = value;
    RefreshCommandStates();
    
    // 如果已经在第4步或之后，重新加载约束
    if (CurrentStep >= 4)
    {
        System.Diagnostics.Debug.WriteLine("日期范围变化，重新加载约束数据");
        _ = LoadConstraintsAsync();
    }
    
    if (CurrentStep == 5) BuildSummarySections();
}

partial void OnEndDateChanged(DateTimeOffset value)
{
    if (value < StartDate) value = StartDate;
    RefreshCommandStates();
    
    // 如果已经在第4步或之后，重新加载约束
    if (CurrentStep >= 4)
    {
        System.Diagnostics.Debug.WriteLine("日期范围变化，重新加载约束数据");
        _ = LoadConstraintsAsync();
    }
    
    if (CurrentStep == 5) BuildSummarySections();
}
```

### 3. UI 改进

**问题**：当数据为空时，用户看不到任何提示

**解决方案**：添加空状态提示

```xaml
<!-- Step 4: Configure Constraints -->
<Grid Visibility="{x:Bind ViewModel.CurrentStep, Converter={StaticResource IntToVisibilityConverter}, ConverterParameter=4, Mode=OneWay}">
    <StackPanel Spacing="24">
        <TextBlock Text="步骤 4: 配置约束" Style="{ThemeResource SubtitleTextBlockStyle}"/>
        
        <!-- Loading Indicator -->
        <StackPanel Spacing="12" HorizontalAlignment="Center" 
                    Visibility="{x:Bind ViewModel.IsLoadingConstraints, Converter={StaticResource BoolToVisibilityConverter}, Mode=OneWay}">
            <ProgressRing IsActive="True" Width="48" Height="48"/>
            <TextBlock Text="正在加载约束数据..." Style="{ThemeResource BodyTextBlockStyle}"/>
        </StackPanel>

        <!-- Content -->
        <StackPanel Spacing="12" 
                    Visibility="{x:Bind ViewModel.IsLoadingConstraints, Converter={StaticResource BoolToVisibilityConverter}, ConverterParameter=Inverse, Mode=OneWay}">
            
            <!-- Holiday Config -->
            <StackPanel Spacing="8" BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}" 
                        BorderThickness="1" Padding="12" CornerRadius="{StaticResource ControlCornerRadius}">
                <TextBlock Text="休息日配置" Style="{ThemeResource BaseTextBlockStyle}" FontWeight="SemiBold"/>
                <ToggleSwitch IsOn="{x:Bind ViewModel.UseActiveHolidayConfig, Mode=TwoWay}" 
                              OffContent="使用自定义配置" OnContent="使用当前活动配置"/>
                
                <!-- Empty State for Holiday Configs -->
                <InfoBar Severity="Warning" IsOpen="True" 
                         Visibility="{x:Bind ViewModel.HolidayConfigs.Count, Converter={StaticResource IntToVisibilityConverter}, ConverterParameter=0, Mode=OneWay}">
                    <InfoBar.Message>
                        <TextBlock>
                            <Run Text="没有找到休息日配置。"/>
                            <Hyperlink NavigateUri="/Settings/Constraints">前往设置创建</Hyperlink>
                        </TextBlock>
                    </InfoBar.Message>
                </InfoBar>
                
                <ComboBox Header="选择一个休息日配置" 
                          ItemsSource="{x:Bind ViewModel.HolidayConfigs, Mode=OneWay}"
                          SelectedValue="{x:Bind ViewModel.SelectedHolidayConfigId, Mode=TwoWay, FallbackValue=-1}"
                          SelectedValuePath="Id" DisplayMemberPath="ConfigName"
                          IsEnabled="{x:Bind ViewModel.UseActiveHolidayConfig, Converter={StaticResource InvertedBooleanConverter}, Mode=OneWay}"
                          Visibility="{x:Bind ViewModel.HolidayConfigs.Count, Converter={StaticResource IntToVisibilityConverter}, ConverterParameter='&gt;0', Mode=OneWay}"/>
            </StackPanel>

            <!-- Fixed Position Rules -->
            <StackPanel Spacing="8" BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}" 
                        BorderThickness="1" Padding="12" CornerRadius="{StaticResource ControlCornerRadius}">
                <TextBlock Text="定岗规则" Style="{ThemeResource BaseTextBlockStyle}" FontWeight="SemiBold"/>
                
                <!-- Empty State for Fixed Rules -->
                <TextBlock Text="没有找到定岗规则" 
                           Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                           Visibility="{x:Bind ViewModel.FixedPositionRules.Count, Converter={StaticResource IntToVisibilityConverter}, ConverterParameter=0, Mode=OneWay}"/>
                
                <ListView ItemsSource="{x:Bind ViewModel.FixedPositionRules, Mode=OneWay}" 
                          SelectionMode="None" MaxHeight="200"
                          Visibility="{x:Bind ViewModel.FixedPositionRules.Count, Converter={StaticResource IntToVisibilityConverter}, ConverterParameter='&gt;0', Mode=OneWay}">
                    <ListView.ItemTemplate>
                        <DataTemplate x:DataType="models:FixedPositionRule">
                            <CheckBox Content="{x:Bind Description}" IsChecked="{x:Bind IsEnabled, Mode=TwoWay}"/>
                        </DataTemplate>
                    </ListView.ItemTemplate>
                </ListView>
            </StackPanel>

            <!-- Manual Assignments -->
            <StackPanel Spacing="8" BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}" 
                        BorderThickness="1" Padding="12" CornerRadius="{StaticResource ControlCornerRadius}">
                <TextBlock Text="手动指定" Style="{ThemeResource BaseTextBlockStyle}" FontWeight="SemiBold"/>
                
                <!-- Empty State for Manual Assignments -->
                <TextBlock Text="在当前日期范围内没有找到手动指定" 
                           Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                           Visibility="{x:Bind ViewModel.ManualAssignments.Count, Converter={StaticResource IntToVisibilityConverter}, ConverterParameter=0, Mode=OneWay}"/>
                
                <ListView ItemsSource="{x:Bind ViewModel.ManualAssignments, Mode=OneWay}" 
                          SelectionMode="None" MaxHeight="200"
                          Visibility="{x:Bind ViewModel.ManualAssignments.Count, Converter={StaticResource IntToVisibilityConverter}, ConverterParameter='&gt;0', Mode=OneWay}">
                    <ListView.ItemTemplate>
                        <DataTemplate x:DataType="models:ManualAssignment">
                            <CheckBox IsChecked="{x:Bind IsEnabled, Mode=TwoWay}">
                                <TextBlock>
                                    <Run Text="{x:Bind Date, Converter={StaticResource DateTimeFormatConverter}, ConverterParameter='yyyy-MM-dd'}"/>
                                    <Run Text=" - "/>
                                    <Run Text="{x:Bind PersonalId}"/>
                                    <Run Text=" -> "/>
                                    <Run Text="{x:Bind PositionId}"/>
                                </TextBlock>
                            </CheckBox>
                        </DataTemplate>
                    </ListView.ItemTemplate>
                </ListView>
            </StackPanel>
        </StackPanel>
    </StackPanel>
</Grid>
```

## Data Models

### 约束数据模型

现有的模型已经定义良好：

1. **HolidayConfig**：休息日配置
   - Id, ConfigName, EnableWeekendRule
   - WeekendDays, LegalHolidays, CustomHolidays, ExcludedDates
   - IsActive

2. **FixedPositionRule**：定岗规则
   - Id, PersonalId
   - AllowedPositionIds, AllowedPeriods
   - IsEnabled, Description

3. **ManualAssignment**：手动指定
   - Id, PositionId, PeriodIndex, PersonalId
   - Date, IsEnabled, Remarks

### ViewModel 属性

```csharp
// 约束数据集合
[ObservableProperty] 
private ObservableCollection<HolidayConfig> _holidayConfigs = new();

[ObservableProperty] 
private ObservableCollection<FixedPositionRule> _fixedPositionRules = new();

[ObservableProperty] 
private ObservableCollection<ManualAssignment> _manualAssignments = new();

// 加载状态
[ObservableProperty] 
private bool _isLoadingConstraints;

// 选择状态
[ObservableProperty] 
private bool _useActiveHolidayConfig = true;

[ObservableProperty] 
private int? _selectedHolidayConfigId;
```

## Error Handling

### 1. 数据库连接错误

```csharp
catch (SqliteException ex)
{
    System.Diagnostics.Debug.WriteLine($"数据库错误: {ex.Message}");
    await _dialogService.ShowErrorAsync(
        "无法连接到数据库",
        "请检查数据库文件是否存在且未被占用。\n\n" +
        $"错误详情: {ex.Message}");
}
```

### 2. 数据反序列化错误

```csharp
catch (JsonException ex)
{
    System.Diagnostics.Debug.WriteLine($"数据格式错误: {ex.Message}");
    await _dialogService.ShowErrorAsync(
        "约束数据格式错误",
        "数据库中的约束数据格式不正确，可能需要重新创建。\n\n" +
        $"错误详情: {ex.Message}");
}
```

### 3. 网络或超时错误

```csharp
catch (TaskCanceledException ex)
{
    System.Diagnostics.Debug.WriteLine($"加载超时: {ex.Message}");
    await _dialogService.ShowWarningAsync(
        "加载约束数据超时，请重试");
}
```

### 4. 通用错误处理

```csharp
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"未知错误: {ex.GetType().Name} - {ex.Message}");
    await _dialogService.ShowErrorAsync(
        "加载约束数据失败",
        $"发生了意外错误。\n\n" +
        $"错误类型: {ex.GetType().Name}\n" +
        $"错误消息: {ex.Message}");
}
```

## Testing Strategy

### 1. 单元测试

测试 ViewModel 的约束加载逻辑：

```csharp
[Fact]
public async Task LoadConstraintsAsync_ShouldLoadAllConstraintTypes()
{
    // Arrange
    var mockService = new Mock<ISchedulingService>();
    mockService.Setup(s => s.GetHolidayConfigsAsync())
        .ReturnsAsync(new List<HolidayConfig> { /* test data */ });
    mockService.Setup(s => s.GetFixedPositionRulesAsync(true))
        .ReturnsAsync(new List<FixedPositionRule> { /* test data */ });
    mockService.Setup(s => s.GetManualAssignmentsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), true))
        .ReturnsAsync(new List<ManualAssignment> { /* test data */ });
    
    var viewModel = new SchedulingViewModel(mockService.Object, /* other deps */);
    
    // Act
    await viewModel.LoadConstraintsCommand.ExecuteAsync(null);
    
    // Assert
    Assert.NotEmpty(viewModel.HolidayConfigs);
    Assert.NotEmpty(viewModel.FixedPositionRules);
    Assert.NotEmpty(viewModel.ManualAssignments);
}

[Fact]
public async Task LoadConstraintsAsync_WhenDatabaseEmpty_ShouldShowEmptyCollections()
{
    // Arrange
    var mockService = new Mock<ISchedulingService>();
    mockService.Setup(s => s.GetHolidayConfigsAsync())
        .ReturnsAsync(new List<HolidayConfig>());
    mockService.Setup(s => s.GetFixedPositionRulesAsync(true))
        .ReturnsAsync(new List<FixedPositionRule>());
    mockService.Setup(s => s.GetManualAssignmentsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), true))
        .ReturnsAsync(new List<ManualAssignment>());
    
    var viewModel = new SchedulingViewModel(mockService.Object, /* other deps */);
    
    // Act
    await viewModel.LoadConstraintsCommand.ExecuteAsync(null);
    
    // Assert
    Assert.Empty(viewModel.HolidayConfigs);
    Assert.Empty(viewModel.FixedPositionRules);
    Assert.Empty(viewModel.ManualAssignments);
}
```

### 2. 集成测试

测试完整的数据加载流程：

```csharp
[Fact]
public async Task ConstraintLoading_EndToEnd_ShouldWork()
{
    // Arrange
    var dbPath = CreateTestDatabase();
    await SeedConstraintData(dbPath);
    var repository = new ConstraintRepository(dbPath);
    var service = new SchedulingService(/* deps with real repository */);
    
    // Act
    var configs = await service.GetHolidayConfigsAsync();
    var rules = await service.GetFixedPositionRulesAsync(true);
    var manuals = await service.GetManualAssignmentsAsync(
        DateTime.Today, DateTime.Today.AddDays(7), true);
    
    // Assert
    Assert.NotEmpty(configs);
    Assert.NotEmpty(rules);
    // manuals may be empty if no data in date range
}
```

### 3. UI 测试

手动测试场景：

1. **场景1：正常加载**
   - 进入排班向导
   - 导航到第4步
   - 验证：显示加载指示器
   - 验证：加载完成后显示约束数据

2. **场景2：空数据库**
   - 清空约束数据
   - 进入排班向导第4步
   - 验证：显示空状态提示

3. **场景3：日期范围变化**
   - 在第1步设置日期范围
   - 进入第4步
   - 返回第1步修改日期
   - 再次进入第4步
   - 验证：手动指定数据根据新日期范围更新

4. **场景4：模板加载**
   - 从模板加载排班
   - 进入第4步
   - 验证：约束按模板设置自动启用

5. **场景5：错误处理**
   - 模拟数据库错误
   - 进入第4步
   - 验证：显示错误对话框

## Diagnostic Tools

### 调试日志格式

```
=== 开始加载约束数据 ===
日期范围: 2024-01-01 到 2024-01-07
加载完成 - 休息日配置: 2, 定岗规则: 5, 手动指定: 3
警告: 没有找到手动指定
应用模板约束设置
=== 约束数据加载流程结束 ===
```

### 性能监控

```csharp
private async Task LoadConstraintsAsync()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    try
    {
        // ... loading logic ...
    }
    finally
    {
        stopwatch.Stop();
        System.Diagnostics.Debug.WriteLine(
            $"约束数据加载耗时: {stopwatch.ElapsedMilliseconds}ms");
    }
}
```

## Implementation Notes

### 关键注意事项

1. **避免重复加载**：使用 `IsLoadingConstraints` 标志防止并发加载
2. **保持数据一致性**：日期范围变化时重新加载手动指定
3. **模板约束应用**：确保在数据加载完成后应用模板设置
4. **UI 响应性**：使用异步加载，不阻塞 UI 线程
5. **错误恢复**：加载失败后允许用户重试

### 潜在问题

1. **数据库未初始化**：确保约束表已创建
2. **空数据**：提供友好的空状态提示
3. **数据绑定失败**：检查 XAML 绑定路径
4. **异步竞态条件**：使用标志位防止并发加载

### 性能考虑

1. **并行加载**：三种约束数据并行加载以提高速度
2. **按需加载**：只在需要时加载，避免不必要的数据库查询
3. **缓存策略**：在同一会话中重用已加载的数据（除非日期范围变化）
