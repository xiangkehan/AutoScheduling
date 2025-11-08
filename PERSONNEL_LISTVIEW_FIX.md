# 人员管理ListView显示修复

## 问题描述
人员管理页面中的"可用哨位"和"技能"ListView没有正常显示内容。

## 修复内容

### 1. 添加ListView边框和背景
为所有ListView添加了Border容器，提供：
- 边框：`ControlStrokeColorDefaultBrush`
- 背景：`ControlFillColorDefaultBrush`
- 圆角：4px
- 最小高度：80px

### 2. 添加空状态提示
当ListView数据为空时，显示友好的提示信息：
- "暂无哨位数据，请先添加哨位"
- "暂无技能数据，请先添加技能"
- 使用`CountToVisibilityConverter`自动控制显示/隐藏

### 3. 添加编辑模式的技能选择
之前编辑模式下缺少技能选择ListView，现已添加：
- `EditSkillsListView` - 编辑时的技能选择
- `EditSkillsListView_SelectionChanged` - 对应的事件处理器

### 4. 自动同步编辑选中状态
添加了`SyncEditSelections()`方法，在进入编辑模式时：
- 自动选中人员已有的哨位
- 自动选中人员已有的技能

## 测试步骤

1. **启动应用并导航到人员管理页面**

2. **测试添加表单**
   - 检查"可用哨位"ListView是否显示且有边框
   - 检查"技能"ListView是否显示且有边框
   - 尝试选择多个哨位和技能
   - 点击"添加人员"按钮

3. **测试编辑功能**
   - 选择一个已有人员
   - 点击"编辑"按钮
   - 检查编辑表单中的"可用哨位"和"技能"ListView
   - 验证已有的选项是否自动被选中
   - 修改选择并保存

4. **测试空状态**
   - 如果数据库中没有哨位或技能数据
   - ListView应该显示为空的可滚动区域（80px高度）

## 修改的文件
- `Views/DataManagement/PersonnelPage.xaml` - UI布局和空状态提示
- `Views/DataManagement/PersonnelPage.xaml.cs` - 事件处理和状态同步
- `Converters/CountToVisibilityConverter.cs` - 新增：数量到可见性转换器

## 技术实现

### CountToVisibilityConverter
```csharp
// 当集合数量为0时显示空状态提示，否则隐藏
public object Convert(object value, Type targetType, object parameter, string language)
{
    if (value is int count)
    {
        return count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    return Visibility.Collapsed;
}
```

### ListView空状态布局
```xml
<Grid>
    <ListView ItemsSource="{x:Bind ViewModel.AvailablePositions}"/>
    <TextBlock Text="暂无数据提示"
               Visibility="{x:Bind ViewModel.AvailablePositions.Count, 
                           Converter={StaticResource CountToVisibilityConverter}}"/>
</Grid>
```
