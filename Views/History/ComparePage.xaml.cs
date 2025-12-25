using AutoScheduling3.DTOs.Comparison;
using AutoScheduling3.ViewModels.History;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI;

namespace AutoScheduling3.Views.History
{
    /// <summary>
    /// 排班对比页面
    /// </summary>
    public sealed partial class ComparePage : Page, INotifyPropertyChanged
    {
        public CompareViewModel ViewModel { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 是否显示空状态
        /// </summary>
        public bool ShowEmptyState => !ViewModel.HasCompared && !ViewModel.IsComparing;

        public ComparePage()
        {
            this.InitializeComponent();
            ViewModel = (App.Current as App)!.ServiceProvider.GetRequiredService<CompareViewModel>();
            this.DataContext = ViewModel;
            
            // 监听属性变化以更新视图
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.HasCompared) || 
                e.PropertyName == nameof(ViewModel.IsComparing))
            {
                // 通知 ShowEmptyState 属性变化
                OnPropertyChanged(nameof(ShowEmptyState));
                
                // 对比完成后默认显示差异列表
                if (e.PropertyName == nameof(ViewModel.HasCompared) && ViewModel.HasCompared)
                {
                    ShowDiffListView();
                }
            }

            // 网格数据更新时重新生成网格
            if (e.PropertyName == nameof(ViewModel.GridData) && ViewModel.GridData != null)
            {
                BuildComparisonGrid();
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsLoaded)
            {
                await ViewModel.LoadAsync();
            }

            if (ViewModel.ShouldFocusSchedule2Selector)
            {
                Schedule2Selector.Focus(FocusState.Programmatic);
                ViewModel.ShouldFocusSchedule2Selector = false;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is int scheduleId)
            {
                ViewModel.SetPreselectedSchedule(scheduleId);
            }
        }

        #region 视图切换

        private void DiffListView_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SwitchViewCommand.Execute(CompareViewMode.DiffList);
            ShowDiffListView();
        }

        private void GridView_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SwitchViewCommand.Execute(CompareViewMode.Grid);
            ShowGridView();
        }

        private void StatisticsView_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SwitchViewCommand.Execute(CompareViewMode.Statistics);
            ShowStatisticsView();
        }

        private void ShowDiffListView()
        {
            DiffListView.Visibility = Visibility.Visible;
            GridViewPanel.Visibility = Visibility.Collapsed;
            StatisticsViewPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowGridView()
        {
            DiffListView.Visibility = Visibility.Collapsed;
            GridViewPanel.Visibility = Visibility.Visible;
            StatisticsViewPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowStatisticsView()
        {
            DiffListView.Visibility = Visibility.Collapsed;
            GridViewPanel.Visibility = Visibility.Collapsed;
            StatisticsViewPanel.Visibility = Visibility.Visible;
        }

        #endregion

        #region 筛选

        private void TypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TypeFilterComboBox.SelectedItem is ComboBoxItem item)
            {
                var tag = item.Tag?.ToString();
                if (string.IsNullOrEmpty(tag))
                {
                    ViewModel.TypeFilter = null;
                }
                else if (System.Enum.TryParse<DiffType>(tag, out var diffType))
                {
                    ViewModel.TypeFilter = diffType;
                }
            }
        }

        #endregion

        #region 网格构建

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

        // 当前选中的哨位ID
        private int? _selectedGridPositionId;
        // 当前选中的周次索引
        private int _selectedWeekIndex;
        // 周次数据
        private List<(DateTime StartDate, DateTime EndDate, int WeekNumber)> _weeks = new();

        /// <summary>
        /// 哨位选择变化
        /// </summary>
        private void GridPositionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridPositionComboBox.SelectedItem is GridPositionDto position)
            {
                _selectedGridPositionId = position.Id;
                BuildWeekSelector();
                BuildComparisonGridForPosition();
            }
        }

        /// <summary>
        /// 周次选择变化
        /// </summary>
        private void GridWeekComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridWeekComboBox.SelectedItem is ComboBoxItem item && item.Tag is int weekIndex)
            {
                _selectedWeekIndex = weekIndex;
                BuildComparisonGridForPosition();
            }
        }

        /// <summary>
        /// 构建周次选择器
        /// </summary>
        private void BuildWeekSelector()
        {
            var gridData = ViewModel.GridData;
            if (gridData == null || gridData.Dates.Count == 0)
                return;

            _weeks.Clear();
            GridWeekComboBox.Items.Clear();

            // 按周分组日期
            var startDate = gridData.Dates.First();
            var endDate = gridData.Dates.Last();
            
            // 找到第一个周一
            var currentWeekStart = startDate;
            while (currentWeekStart.DayOfWeek != DayOfWeek.Monday && currentWeekStart > startDate.AddDays(-7))
            {
                currentWeekStart = currentWeekStart.AddDays(-1);
            }
            if (currentWeekStart < startDate)
            {
                currentWeekStart = startDate;
            }

            int weekNumber = 1;
            while (currentWeekStart <= endDate)
            {
                var weekEnd = currentWeekStart.AddDays(6);
                if (weekEnd > endDate) weekEnd = endDate;

                _weeks.Add((currentWeekStart, weekEnd, weekNumber));

                var item = new ComboBoxItem
                {
                    Content = $"第{weekNumber}周 ({currentWeekStart:MM-dd} ~ {weekEnd:MM-dd})",
                    Tag = weekNumber - 1
                };
                GridWeekComboBox.Items.Add(item);

                currentWeekStart = currentWeekStart.AddDays(7);
                weekNumber++;
            }

            if (GridWeekComboBox.Items.Count > 0)
            {
                GridWeekComboBox.SelectedIndex = 0;
                _selectedWeekIndex = 0;
            }
        }

        /// <summary>
        /// 为选中的哨位构建对比网格
        /// </summary>
        private void BuildComparisonGridForPosition()
        {
            var gridData = ViewModel.GridData;
            if (gridData == null || !_selectedGridPositionId.HasValue || _weeks.Count == 0)
            {
                ComparisonGrid.Children.Clear();
                return;
            }

            if (_selectedWeekIndex < 0 || _selectedWeekIndex >= _weeks.Count)
                return;

            var week = _weeks[_selectedWeekIndex];
            var positionId = _selectedGridPositionId.Value;

            // 清空现有内容
            ComparisonGrid.Children.Clear();
            ComparisonGrid.ColumnDefinitions.Clear();
            ComparisonGrid.RowDefinitions.Clear();

            // 获取该周的日期列表
            var weekDates = gridData.Dates
                .Where(d => d >= week.StartDate && d <= week.EndDate)
                .OrderBy(d => d)
                .ToList();

            if (weekDates.Count == 0)
                return;

            // 创建列定义：第一列为时段列，后面为日期列
            ComparisonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // 时段列
            for (int i = 0; i < weekDates.Count; i++)
            {
                ComparisonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            }

            // 创建行定义：第一行为表头，后面12行为时段行
            ComparisonGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 表头行
            for (int i = 0; i < 12; i++)
            {
                ComparisonGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });
            }

            // 创建表头
            CreateComparisonTableHeader(weekDates);

            // 创建行头和单元格
            CreateComparisonRowsAndCells(gridData, positionId, weekDates);
        }

        /// <summary>
        /// 创建表头（日期列）
        /// </summary>
        private void CreateComparisonTableHeader(List<DateTime> weekDates)
        {
            // 第一列：空白表头（时段列）
            var cornerHeader = CreateComparisonHeaderCell("时段", 0, 0);
            ComparisonGrid.Children.Add(cornerHeader);

            // 为每一天创建表头
            for (int i = 0; i < weekDates.Count; i++)
            {
                var date = weekDates[i];
                var dayOfWeekIndex = ((int)date.DayOfWeek + 6) % 7; // 转换为周一=0
                var headerText = $"{DayOfWeekDescriptions[dayOfWeekIndex]}\n{date:MM-dd}";
                var headerCell = CreateComparisonHeaderCell(headerText, i + 1, 0);
                ComparisonGrid.Children.Add(headerCell);
            }
        }

        /// <summary>
        /// 创建表头单元格
        /// </summary>
        private Border CreateComparisonHeaderCell(string text, int column, int row)
        {
            var border = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                Padding = new Thickness(8, 6, 8, 6)
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
        private void CreateComparisonRowsAndCells(GridComparisonDto gridData, int positionId, List<DateTime> weekDates)
        {
            // 为每个时段创建行
            for (int periodIndex = 0; periodIndex < 12; periodIndex++)
            {
                // 创建行头（时段描述）
                var rowHeader = CreateComparisonRowHeaderCell(TimeSlotDescriptions[periodIndex], periodIndex + 1);
                ComparisonGrid.Children.Add(rowHeader);

                // 为该时段的每一天创建单元格
                for (int dayIndex = 0; dayIndex < weekDates.Count; dayIndex++)
                {
                    var date = weekDates[dayIndex];
                    
                    // 查找对应的行数据
                    var row = gridData.Rows.FirstOrDefault(r => 
                        r.Date.Date == date.Date && r.PeriodIndex == periodIndex);

                    GridCellDto? cellData = null;
                    if (row != null && row.Cells.TryGetValue(positionId, out var cell))
                    {
                        cellData = cell;
                    }

                    var cellElement = CreateComparisonScheduleCell(cellData, periodIndex, dayIndex);
                    Grid.SetColumn(cellElement, dayIndex + 1);
                    Grid.SetRow(cellElement, periodIndex + 1);
                    ComparisonGrid.Children.Add(cellElement);
                }
            }
        }

        /// <summary>
        /// 创建行头单元格（时段）
        /// </summary>
        private Border CreateComparisonRowHeaderCell(string timeSlot, int row)
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
                TextWrapping = TextWrapping.NoWrap,
                FontSize = 12
            };

            border.Child = textBlock;
            Grid.SetColumn(border, 0);
            Grid.SetRow(border, row);

            return border;
        }

        /// <summary>
        /// 创建对比单元格
        /// </summary>
        private Border CreateComparisonScheduleCell(GridCellDto? cellData, int periodIndex, int dayIndex)
        {
            var background = GetCellBackground(cellData?.DiffType);

            var border = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Background = background,
                Padding = new Thickness(4)
            };

            var stackPanel = new StackPanel 
            { 
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 2
            };

            if (cellData == null)
            {
                // 无数据
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = "-", 
                    FontSize = 12, 
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray) 
                });
            }
            else if (cellData.DiffType == DiffType.Changed)
            {
                // 变更：显示 "原人员 → 新人员"
                var changePanel = new StackPanel 
                { 
                    Orientation = Orientation.Horizontal, 
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 4
                };
                
                changePanel.Children.Add(new TextBlock
                {
                    Text = cellData.DisplayText1,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)),
                    TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough
                });
                
                changePanel.Children.Add(new TextBlock
                {
                    Text = "→",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)),
                    VerticalAlignment = VerticalAlignment.Center
                });
                
                changePanel.Children.Add(new TextBlock
                {
                    Text = cellData.DisplayText2,
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 156, 100, 0)) // 深橙色，在黄色背景上可读性好
                });

                stackPanel.Children.Add(changePanel);
            }
            else if (cellData.DiffType == DiffType.Added)
            {
                // 新增：显示新人员
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "[新增]",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 124, 16)),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                stackPanel.Children.Add(new TextBlock
                {
                    Text = cellData.DisplayText2,
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
            else if (cellData.DiffType == DiffType.Removed)
            {
                // 删除：显示原人员（删除线）
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "[删除]",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 209, 52, 56)),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                stackPanel.Children.Add(new TextBlock
                {
                    Text = cellData.DisplayText1,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
            else
            {
                // 无差异：显示人员名称
                var name = cellData.PersonnelName1 ?? cellData.PersonnelName2 ?? "-";
                stackPanel.Children.Add(new TextBlock
                {
                    Text = name,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = name == "-" 
                        ? new SolidColorBrush(Microsoft.UI.Colors.Gray)
                        : (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
                });
            }

            border.Child = stackPanel;

            // 添加 ToolTip
            if (cellData != null && cellData.HasDiff)
            {
                var tooltip = GetCellTooltip(cellData);
                ToolTipService.SetToolTip(border, tooltip);
            }

            return border;
        }

        /// <summary>
        /// 获取单元格背景色
        /// </summary>
        private Brush GetCellBackground(DiffType? diffType)
        {
            return diffType switch
            {
                DiffType.Added => (Brush)Resources["AddedBrush"],
                DiffType.Removed => (Brush)Resources["RemovedBrush"],
                DiffType.Changed => (Brush)Resources["ChangedBrush"],
                _ => (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"]
            };
        }

        /// <summary>
        /// 获取单元格提示文本
        /// </summary>
        private string GetCellTooltip(GridCellDto cell)
        {
            return cell.DiffType switch
            {
                DiffType.Added => $"新增班次\n分配给: {cell.DisplayText2}",
                DiffType.Removed => $"删除班次\n原分配: {cell.DisplayText1}",
                DiffType.Changed => $"人员变更\n{cell.DisplayText1} → {cell.DisplayText2}",
                _ => ""
            };
        }

        /// <summary>
        /// 构建对比网格（初始化）
        /// </summary>
        private void BuildComparisonGrid()
        {
            var gridData = ViewModel.GridData;
            if (gridData == null || gridData.Positions.Count == 0)
                return;

            // 默认选中第一个哨位
            if (GridPositionComboBox.Items.Count > 0 && GridPositionComboBox.SelectedIndex < 0)
            {
                GridPositionComboBox.SelectedIndex = 0;
            }
        }

        #endregion
    }
}
