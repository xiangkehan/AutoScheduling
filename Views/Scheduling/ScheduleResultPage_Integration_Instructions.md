# ScheduleResultPage集成说明

## 需要修改的文件

### 1. ScheduleResultPage.xaml

在现有的主Grid中添加新的三栏布局容器。找到以下位置：

```xml
<Grid>
    <!-- Main content visible when Schedule not null -->
    <Grid Visibility="{x:Bind ViewModel.Schedule, Converter={StaticResource NullToVisibilityConverter}, Mode=OneWay}">
```

在这个Grid之前添加：

```xml
<Grid>
    <!-- 新UI容器 - 从ScheduleResultPage_NewLayout.xaml复制内容 -->
    <Grid x:Name="NewUIContainer" 
          Visibility="{x:Bind ViewModel.UseNewUI, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
        <!-- 三栏布局内容 -->
    </Grid>
    
    <!-- 旧UI容器 - 现有内容 -->
    <Grid x:Name="OldUIContainer"
          Visibility="{x:Bind ViewModel.UseOldUI, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
        <!-- 现有的所有内容保持不变 -->
    </Grid>
</Grid>
```

### 2. ScheduleResultPage.xaml中需要添加的命名空间

在Page标签中添加：

```xml
xmlns:leftpanel="using:AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.LeftPanel"
```

### 3. ScheduleResultPage.xaml.cs

在构造函数的`this.Loaded += OnPageLoaded;`之后添加：

```csharp
// 订阅窗口大小变化事件
this.SizeChanged += OnPageSizeChanged;

// 加载布局偏好
_ = LoadLayoutPreferencesAsync();
```

添加加载布局偏好的方法：

```csharp
/// <summary>
/// 加载布局偏好
/// </summary>
private async System.Threading.Tasks.Task LoadLayoutPreferencesAsync()
{
    try
    {
        var layoutService = ((App)Application.Current).ServiceProvider
            .GetService(typeof(Services.Interfaces.ILayoutPreferenceService)) 
            as Services.Interfaces.ILayoutPreferenceService;
        
        if (layoutService != null)
        {
            var preferences = await layoutService.LoadAsync();
            
            // 应用偏好设置
            ViewModel.LeftPanelWidth = new GridLength(preferences.LeftPanelWidth, GridUnitType.Star);
            ViewModel.RightPanelWidth = new GridLength(preferences.RightPanelWidth, GridUnitType.Star);
            ViewModel.IsLeftPanelVisible = preferences.IsLeftPanelVisible;
            ViewModel.IsRightPanelVisible = preferences.IsRightPanelVisible;
        }
    }
    catch (System.Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"加载布局偏好失败: {ex.Message}");
    }
}
```

### 4. 需要在XAML中添加x:Name的元素

确保以下元素有x:Name属性：

```xml
<ColumnDefinition x:Name="LeftPanelColumn" .../>
<ColumnDefinition x:Name="RightPanelColumn" .../>
```

## 测试步骤

1. **启用新UI**：在ViewModel中设置`UseNewUI = true`（可以通过配置或Feature Flag）
2. **测试左侧面板**：
   - 检查排班信息卡片是否正确显示
   - 点击统计摘要指标，验证命令是否触发
   - 检查冲突列表是否显示
3. **测试GridSplitter**：
   - 拖拽左侧分隔符，验证左侧面板宽度是否改变
   - 拖拽右侧分隔符，验证右侧面板宽度是否改变
   - 检查宽度限制是否生效（左侧200-500px，右侧250-600px）
4. **测试响应式布局**：
   - 调整窗口大小，验证布局模式是否自动切换
   - 检查不同屏幕尺寸下的显示效果
5. **测试布局偏好**：
   - 调整面板宽度后关闭应用
   - 重新打开应用，验证宽度是否恢复

## 临时启用新UI的方法

在ScheduleResultViewModel的构造函数中添加：

```csharp
// 临时启用新UI用于测试
#if DEBUG
UseNewUI = true;
#endif
```

或者在App.xaml.cs中添加配置：

```csharp
// 在应用启动时设置Feature Flag
var config = new Dictionary<string, bool>
{
    ["UseNewScheduleResultPageUI"] = true
};
```

## 注意事项

1. **命名空间**：确保所有using语句正确
2. **转换器**：确保BoolToVisibilityConverter已在Resources中定义
3. **ViewModel属性**：确保所有绑定的属性都存在于ViewModel中
4. **事件处理**：GridSplitter的事件处理代码已在ScheduleResultPage.GridSplitter.cs中定义
5. **性能**：GridSplitter拖拽时使用节流，避免频繁更新

## 下一步

完成集成后，继续实现：
- 任务4：主内容区网格视图
- 任务5：右侧详情区
- 任务6：交互联动机制完善
