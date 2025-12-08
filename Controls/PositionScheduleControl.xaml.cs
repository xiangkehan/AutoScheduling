using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using AutoScheduling3.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoScheduling3.Controls
{
    /// <summary>
    /// 哨位排班表格控件（按周显示）
    /// </summary>
    public sealed partial class PositionScheduleControl : UserControl
    {
        /// <summary>
        /// ScheduleData 依赖属性
        /// </summary>
        public static readonly DependencyProperty ScheduleDataProperty =
            DependencyProperty.Register(
                nameof(ScheduleData),
                typeof(PositionScheduleData),
                typeof(PositionScheduleControl),
                new PropertyMetadata(null, OnScheduleDataChanged));

        /// <summary>
        /// HighlightedCellKeys 依赖属性
        /// </summary>
        public static readonly DependencyProperty HighlightedCellKeysProperty =
            DependencyProperty.Register(
                nameof(HighlightedCellKeys),
                typeof(HashSet<string>),
                typeof(PositionScheduleControl),
                new PropertyMetadata(null, OnHighlightedCellKeysChanged));

        /// <summary>
        /// FocusedShiftId 依赖属性
        /// </summary>
        public static readonly DependencyProperty FocusedShiftIdProperty =
            DependencyProperty.Register(
                nameof(FocusedShiftId),
                typeof(int?),
                typeof(PositionScheduleControl),
                new PropertyMetadata(null, OnFocusedShiftIdChanged));

        /// <summary>
        /// 排班数据
        /// </summary>
        public PositionScheduleData? ScheduleData
        {
            get => (PositionScheduleData?)GetValue(ScheduleDataProperty);
            set => SetValue(ScheduleDataProperty, value);
        }

        /// <summary>
        /// 高亮显示的单元格键集合（格式：periodIndex_dayOfWeek）
        /// </summary>
        public HashSet<string>? HighlightedCellKeys
        {
            get => (HashSet<string>?)GetValue(HighlightedCellKeysProperty);
            set => SetValue(HighlightedCellKeysProperty, value);
        }

        /// <summary>
        /// 当前焦点高亮的班次ID
        /// </summary>
        public int? FocusedShiftId
        {
            get => (int?)GetValue(FocusedShiftIdProperty);
            set => SetValue(FocusedShiftIdProperty, value);
        }

        /// <summary>
        /// 单元格点击事件
        /// </summary>
        public event EventHandler<PositionCellClickedEventArgs>? CellClicked;

        /// <summary>
        /// 周次变化事件
        /// </summary>
        public event EventHandler<WeekChangedEventArgs>? WeekChanged;

        /// <summary>
        /// 导出请求事件
        /// </summary>
        public event EventHandler? ExportRequested;

        /// <summary>
        /// 打印请求事件
        /// </summary>
        public event EventHandler? PrintRequested;

        /// <summary>
        /// 全屏请求事件
        /// </summary>
        public event EventHandler? FullScreenRequested;

        // 时段描述数组（12个时段，每个时段2小时）
        private static readonly string[] TimeSlotDescriptions = new[]
        {
            "00:00-02:00", "02:00-04:00", "04:00-06:00", "06:00-08:00",
            "08:00-10:00", "10:00-12:00", "12:00-14:00", "14:00-16:00",
            "16:00-18:00", "18:00-20:00", "20:00-22:00", "22:00-00:00"
        };

        // 星期描述数组
        private static readonly string[] DayOfWeekDescriptions = new[]
        {
            "周一", "周二", "周三", "周四", "周五", "周六", "周日"
        };

        public PositionScheduleControl()
        {
            InitializeComponent();
            
            // 监听主题变化
            this.ActualThemeChanged += OnActualThemeChanged;
        }

        /// <summary>
        /// 主题变化时重新构建表格以更新颜色
        /// </summary>
        private void OnActualThemeChanged(FrameworkElement sender, object args)
        {
            // 重新构建当前显示的周视图
            if (ScheduleData != null && ScheduleData.Weeks.Count > 0)
            {
                BuildWeeklyGrid(ScheduleData.Weeks[ScheduleData.CurrentWeekIndex]);
            }
        }

        /// <summary>
        /// ScheduleData 属性变化回调
        /// </summary>
        private static void OnScheduleDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PositionScheduleControl control)
            {
                control.OnScheduleDataChangedInternal(e.NewValue as PositionScheduleData);
            }
        }

        /// <summary>
        /// HighlightedCellKeys 属性变化回调
        /// </summary>
        private static void OnHighlightedCellKeysChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PositionScheduleControl control)
            {
                control.UpdateCellHighlights();
            }
        }

        /// <summary>
        /// FocusedShiftId 属性变化回调
        /// </summary>
        private static void OnFocusedShiftIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PositionScheduleControl control)
            {
                control.UpdateCellHighlights();
            }
        }

        /// <summary>
        /// 处理 ScheduleData 变化
        /// </summary>
        private void OnScheduleDataChangedInternal(PositionScheduleData? newData)
        {
            if (newData == null)
            {
                // 清空控件
                ClearControl();
                return;
            }

            // 更新哨位名称
            PositionNameText.Text = newData.PositionName;

            // 填充周次选择器
            PopulateWeekComboBox(newData);

            // 构建表格
            if (newData.Weeks.Count > 0)
            {
                BuildWeeklyGrid(newData.Weeks[newData.CurrentWeekIndex]);
            }
        }

        /// <summary>
        /// 清空控件
        /// </summary>
        private void ClearControl()
        {
            PositionNameText.Text = "未选择";
            WeekComboBox.Items.Clear();
            WeeklyGrid.Children.Clear();
            WeeklyGrid.ColumnDefinitions.Clear();
            WeeklyGrid.RowDefinitions.Clear();
        }

        /// <summary>
        /// 填充周次选择器
        /// </summary>
        private void PopulateWeekComboBox(PositionScheduleData data)
        {
            WeekComboBox.SelectionChanged -= WeekComboBox_SelectionChanged;
            WeekComboBox.Items.Clear();

            foreach (var week in data.Weeks)
            {
                var item = new ComboBoxItem
                {
                    Content = $"第{week.WeekNumber}周 ({week.StartDate:MM-dd} ~ {week.EndDate:MM-dd})",
                    Tag = week.WeekNumber - 1 // 存储周次索引
                };
                WeekComboBox.Items.Add(item);
            }

            if (data.Weeks.Count > 0)
            {
                WeekComboBox.SelectedIndex = data.CurrentWeekIndex;
            }

            WeekComboBox.SelectionChanged += WeekComboBox_SelectionChanged;
        }

        /// <summary>
        /// 构建周视图表格
        /// </summary>
        private void BuildWeeklyGrid(WeekData weekData)
        {
            // 清空现有内容
            WeeklyGrid.Children.Clear();
            WeeklyGrid.ColumnDefinitions.Clear();
            WeeklyGrid.RowDefinitions.Clear();

            // 创建列定义：第一列为时段列，后面7列为星期列
            WeeklyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // 时段列
            for (int i = 0; i < 7; i++)
            {
                WeeklyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Star) });
            }

            // 创建行定义：第一行为表头，后面12行为时段行
            WeeklyGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 表头行
            for (int i = 0; i < 12; i++)
            {
                WeeklyGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50, GridUnitType.Pixel) });
            }

            // 创建表头
            CreateTableHeader(weekData);

            // 创建行头和单元格
            CreateRowsAndCells(weekData);
        }

        /// <summary>
        /// 创建表头（星期列）
        /// </summary>
        private void CreateTableHeader(WeekData weekData)
        {
            // 第一列：空白表头（时段列）
            var cornerHeader = CreateHeaderCell("时段", 0, 0);
            WeeklyGrid.Children.Add(cornerHeader);

            // 为每一天创建表头
            for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
            {
                var date = weekData.StartDate.AddDays(dayOfWeek);
                var headerText = $"{DayOfWeekDescriptions[dayOfWeek]}\n{date:MM-dd}";
                var headerCell = CreateHeaderCell(headerText, dayOfWeek + 1, 0);
                WeeklyGrid.Children.Add(headerCell);
            }
        }

        /// <summary>
        /// 创建表头单元格
        /// </summary>
        private Border CreateHeaderCell(string text, int column, int row)
        {
            var border = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                Padding = new Thickness(8, 4, 8, 4)
            };

            var textBlock = new TextBlock
            {
                Text = text,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            border.Child = textBlock;
            Grid.SetColumn(border, column);
            Grid.SetRow(border, row);

            return border;
        }

        /// <summary>
        /// 创建行头和单元格
        /// </summary>
        private void CreateRowsAndCells(WeekData weekData)
        {
            // 为每个时段创建行
            for (int periodIndex = 0; periodIndex < 12; periodIndex++)
            {
                // 创建行头（时段描述）
                var rowHeader = CreateRowHeaderCell(TimeSlotDescriptions[periodIndex], periodIndex + 1);
                WeeklyGrid.Children.Add(rowHeader);

                // 为该时段的每一天创建单元格
                for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
                {
                    var cellKey = $"{periodIndex}_{dayOfWeek}";
                    var cellData = weekData.Cells.ContainsKey(cellKey) ? weekData.Cells[cellKey] : null;

                    var cell = CreateScheduleCell(cellData, periodIndex, dayOfWeek);
                    Grid.SetColumn(cell, dayOfWeek + 1);
                    Grid.SetRow(cell, periodIndex + 1);
                    WeeklyGrid.Children.Add(cell);
                }
            }
        }

        /// <summary>
        /// 创建行头单元格（时段）
        /// </summary>
        private Border CreateRowHeaderCell(string timeSlot, int row)
        {
            var border = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                Padding = new Thickness(8, 4, 8, 4)
            };

            var textBlock = new TextBlock
            {
                Text = timeSlot,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap
            };

            border.Child = textBlock;
            Grid.SetColumn(border, 0);
            Grid.SetRow(border, row);

            return border;
        }

        /// <summary>
        /// 创建排班单元格
        /// </summary>
        private Border CreateScheduleCell(PositionScheduleCell? cellData, int periodIndex, int dayOfWeek)
        {
            var cellKey = $"{periodIndex}_{dayOfWeek}";
            
            // 检查是否高亮：支持多种键格式
            bool isHighlighted = false;
            
            // 1. 基于 ShiftId 的键格式：shift_{shiftId}_ByPosition
            if (cellData?.ShiftId != null && HighlightedCellKeys != null)
            {
                var shiftKey = $"shift_{cellData.ShiftId}_ByPosition";
                isHighlighted = HighlightedCellKeys.Contains(shiftKey);
            }
            
            // 2. 兼容旧的坐标格式
            if (!isHighlighted && HighlightedCellKeys != null)
            {
                isHighlighted = HighlightedCellKeys.Contains(cellKey);
            }

            var border = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                Padding = new Thickness(4),
                Tag = new CellTag { CellData = cellData, CellKey = cellKey }
            };

            // 根据单元格状态应用不同样式
            if (isHighlighted)
            {
                // 高亮单元格：深橙色边框 + 金黄色背景（更明显）
                border.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 140, 0)); // 深橙色
                border.BorderThickness = new Thickness(3);
                border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 255, 215, 0)); // 半透明金黄色
            }
            else if (cellData != null)
            {
                if (cellData.HasConflict)
                {
                    // 冲突单元格：红色边框
                    border.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    border.BorderThickness = new Thickness(2);
                }
                else if (cellData.IsManualAssignment)
                {
                    // 手动指定单元格：蓝色边框
                    border.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
                    border.BorderThickness = new Thickness(2);
                }
            }

            // 创建单元格内容
            var textBlock = new TextBlock
            {
                Text = cellData?.PersonnelName ?? "",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontSize = 13
            };

            // 根据单元格状态和当前主题设置文本颜色
            var isDarkTheme = this.ActualTheme == ElementTheme.Dark;
            
            if (cellData?.IsAssigned == true)
            {
                // 已分配：根据主题使用对比度高的颜色
                textBlock.Foreground = new SolidColorBrush(
                    isDarkTheme 
                        ? Windows.UI.Color.FromArgb(255, 255, 255, 255)  // 深色模式：白色
                        : Windows.UI.Color.FromArgb(255, 0, 0, 0)        // 浅色模式：黑色
                );
                textBlock.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
            }
            else
            {
                // 未分配：使用灰色
                textBlock.Foreground = new SolidColorBrush(
                    isDarkTheme
                        ? Windows.UI.Color.FromArgb(255, 150, 150, 150)  // 深色模式：浅灰
                        : Windows.UI.Color.FromArgb(255, 120, 120, 120)  // 浅色模式：深灰
                );
            }

            border.Child = textBlock;

            // 添加 Tooltip
            if (cellData != null && cellData.IsAssigned)
            {
                var tooltip = CreateCellTooltip(cellData);
                ToolTipService.SetToolTip(border, tooltip);
            }

            // 添加点击事件
            border.Tapped += (s, e) =>
            {
                if (cellData != null)
                {
                    CellClicked?.Invoke(this, new PositionCellClickedEventArgs(periodIndex, dayOfWeek, cellData));
                }
            };

            // 添加双击事件（用于编辑）
            border.DoubleTapped += (s, e) =>
            {
                if (cellData != null)
                {
                    // 触发编辑事件（可以在父级处理）
                    CellClicked?.Invoke(this, new PositionCellClickedEventArgs(periodIndex, dayOfWeek, cellData, isDoubleClick: true));
                }
            };

            return border;
        }

        /// <summary>
        /// 创建单元格 Tooltip
        /// </summary>
        private StackPanel CreateCellTooltip(PositionScheduleCell cellData)
        {
            var tooltip = new StackPanel { Spacing = 4 };

            // 人员姓名
            tooltip.Children.Add(new TextBlock
            {
                Text = $"人员: {cellData.PersonnelName}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });

            // 日期和时段
            tooltip.Children.Add(new TextBlock
            {
                Text = $"日期: {cellData.Date:yyyy-MM-dd}",
                FontSize = 12
            });

            tooltip.Children.Add(new TextBlock
            {
                Text = $"时段: {TimeSlotDescriptions[cellData.PeriodIndex]}",
                FontSize = 12
            });

            // 手动指定标记
            if (cellData.IsManualAssignment)
            {
                tooltip.Children.Add(new TextBlock
                {
                    Text = "🔵 手动指定",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue)
                });
            }

            // 冲突信息
            if (cellData.HasConflict && !string.IsNullOrEmpty(cellData.ConflictMessage))
            {
                tooltip.Children.Add(new TextBlock
                {
                    Text = $"⚠ 冲突: {cellData.ConflictMessage}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 300
                });
            }

            return tooltip;
        }

        /// <summary>
        /// 周次选择变化事件
        /// </summary>
        private void WeekComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WeekComboBox.SelectedItem is ComboBoxItem item && item.Tag is int weekIndex)
            {
                if (ScheduleData != null && weekIndex >= 0 && weekIndex < ScheduleData.Weeks.Count)
                {
                    // 更新当前周次索引
                    ScheduleData.CurrentWeekIndex = weekIndex;

                    // 重新构建表格
                    BuildWeeklyGrid(ScheduleData.Weeks[weekIndex]);

                    // 触发周次变化事件
                    WeekChanged?.Invoke(this, new WeekChangedEventArgs(weekIndex));
                }
            }
        }

        /// <summary>
        /// 导出按钮点击事件
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 打印按钮点击事件
        /// </summary>
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            PrintRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 全屏按钮点击事件
        /// </summary>
        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            FullScreenRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 更新所有单元格的高亮状态
        /// </summary>
        private void UpdateCellHighlights()
        {
            var highlightKeys = HighlightedCellKeys ?? new HashSet<string>();

            // 遍历所有单元格，更新高亮状态
            foreach (var child in WeeklyGrid.Children)
            {
                if (child is Border border && border.Tag is CellTag cellTag)
                {
                    // 检查是否高亮：支持多种键格式
                    bool isHighlighted = false;
                    
                    // 1. 基于 ShiftId 的键格式：shift_{shiftId}_ByPosition
                    if (cellTag.CellData?.ShiftId != null)
                    {
                        var shiftKey = $"shift_{cellTag.CellData.ShiftId}_ByPosition";
                        isHighlighted = highlightKeys.Contains(shiftKey);
                    }
                    
                    // 2. 未分配冲突的特殊格式：unassigned_{positionId}_{date}_{period}
                    if (!isHighlighted && ScheduleData != null && cellTag.CellData != null)
                    {
                        var unassignedKey = $"unassigned_{ScheduleData.PositionId}_{cellTag.CellData.Date:yyyyMMdd}_{cellTag.CellData.PeriodIndex}";
                        isHighlighted = highlightKeys.Contains(unassignedKey);
                    }
                    
                    // 3. 兼容旧的坐标格式
                    if (!isHighlighted)
                    {
                        isHighlighted = highlightKeys.Contains(cellTag.CellKey);
                    }
                    
                    var isFocused = FocusedShiftId.HasValue && cellTag.CellData?.ShiftId == FocusedShiftId.Value;

                    // 更新样式（焦点高亮优先级最高）
                    if (isFocused)
                    {
                        // 焦点高亮：使用更明显的颜色
                        try
                        {
                            border.BorderBrush = (Brush)Application.Current.Resources["FocusedHighlightBrush"];
                            border.BorderThickness = (Thickness)Application.Current.Resources["FocusedHighlightBorderThickness"];
                            border.Background = (Brush)Application.Current.Resources["FocusedHighlightBackgroundBrush"];
                        }
                        catch
                        {
                            // 回退到更明显的硬编码颜色
                            border.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 69, 0)); // 橙红色
                            border.BorderThickness = new Thickness(4);
                            border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(120, 255, 165, 0)); // 更明显的橙色背景
                        }
                        
                        // 更新文本样式
                        if (border.Child is TextBlock textBlock)
                        {
                            textBlock.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                        }
                    }
                    else if (isHighlighted)
                    {
                        // 普通高亮：使用更明显的颜色
                        try
                        {
                            border.BorderBrush = (Brush)Application.Current.Resources["SearchHighlightBrush"];
                            border.BorderThickness = (Thickness)Application.Current.Resources["SearchHighlightBorderThickness"];
                            border.Background = (Brush)Application.Current.Resources["SearchHighlightBackgroundBrush"];
                        }
                        catch
                        {
                            // 回退到更明显的硬编码颜色
                            border.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 140, 0)); // 深橙色
                            border.BorderThickness = new Thickness(3);
                            border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 255, 215, 0)); // 半透明金黄色
                        }
                        
                        // 更新文本样式
                        if (border.Child is TextBlock textBlock)
                        {
                            textBlock.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                        }
                    }
                    else
                    {
                        // 恢复默认样式
                        border.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
                        border.BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
                        border.BorderThickness = new Thickness(1);

                        // 恢复文本样式
                        if (border.Child is TextBlock textBlock)
                        {
                            textBlock.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
                        }

                        // 根据单元格状态应用特殊样式
                        if (cellTag.CellData != null)
                        {
                            if (cellTag.CellData.HasConflict)
                            {
                                border.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
                                border.BorderThickness = new Thickness(2);
                            }
                            else if (cellTag.CellData.IsManualAssignment)
                            {
                                border.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
                                border.BorderThickness = new Thickness(2);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 滚动到指定单元格
        /// </summary>
        /// <param name="periodIndex">时段索引（行索引，0-11）</param>
        /// <param name="dayOfWeek">星期索引（列索引，0-6）</param>
        public void ScrollToCell(int periodIndex, int dayOfWeek)
        {
            try
            {
                // 查找目标单元格
                var targetCell = FindCellElement(periodIndex, dayOfWeek);
                if (targetCell == null) return;

                // 获取单元格相对于 ScrollViewer 的位置
                var transform = targetCell.TransformToVisual(GridScrollViewer);
                var position = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

                // 计算滚动位置（将单元格滚动到视口中央）
                var scrollToX = position.X + GridScrollViewer.HorizontalOffset - (GridScrollViewer.ViewportWidth / 2);
                var scrollToY = position.Y + GridScrollViewer.VerticalOffset - (GridScrollViewer.ViewportHeight / 2);

                // 确保滚动位置不超出范围
                scrollToX = Math.Max(0, Math.Min(scrollToX, GridScrollViewer.ScrollableWidth));
                scrollToY = Math.Max(0, Math.Min(scrollToY, GridScrollViewer.ScrollableHeight));

                // 执行滚动（使用动画效果）
                GridScrollViewer.ChangeView(scrollToX, scrollToY, null, false);
            }
            catch
            {
                // 滚动失败时静默处理
            }
        }

        /// <summary>
        /// 查找指定位置的单元格元素
        /// </summary>
        private UIElement? FindCellElement(int periodIndex, int dayOfWeek)
        {
            var cellKey = $"{periodIndex}_{dayOfWeek}";

            // 在 WeeklyGrid 中查找对应的单元格
            foreach (var child in WeeklyGrid.Children)
            {
                if (child is Border border && border.Tag is CellTag cellTag)
                {
                    if (cellTag.CellKey == cellKey)
                    {
                        return border;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 单元格标签（用于存储单元格数据和键）
    /// </summary>
    internal class CellTag
    {
        public PositionScheduleCell? CellData { get; set; }
        public string CellKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// 哨位单元格点击事件参数
    /// </summary>
    public class PositionCellClickedEventArgs : EventArgs
    {
        public int PeriodIndex { get; }
        public int DayOfWeek { get; }
        public PositionScheduleCell Cell { get; }
        public bool IsDoubleClick { get; }

        public PositionCellClickedEventArgs(int periodIndex, int dayOfWeek, PositionScheduleCell cell, bool isDoubleClick = false)
        {
            PeriodIndex = periodIndex;
            DayOfWeek = dayOfWeek;
            Cell = cell;
            IsDoubleClick = isDoubleClick;
        }
    }

    /// <summary>
    /// 周次变化事件参数
    /// </summary>
    public class WeekChangedEventArgs : EventArgs
    {
        public int WeekIndex { get; }

        public WeekChangedEventArgs(int weekIndex)
        {
            WeekIndex = weekIndex;
        }
    }
}
