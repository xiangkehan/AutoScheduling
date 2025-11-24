using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using AutoScheduling3.DTOs;
using System.Linq;
using System.Collections.Generic;

namespace AutoScheduling3.Controls
{
    /// <summary>
    /// 排班结果表格控件
    /// </summary>
    public sealed partial class ScheduleGridControl : UserControl
    {
        /// <summary>
        /// GridData 依赖属性
        /// </summary>
        public static readonly DependencyProperty GridDataProperty =
            DependencyProperty.Register(
                nameof(GridData),
                typeof(ScheduleGridData),
                typeof(ScheduleGridControl),
                new PropertyMetadata(null, OnGridDataChanged));

        /// <summary>
        /// HighlightedCellKeys 依赖属性
        /// </summary>
        public static readonly DependencyProperty HighlightedCellKeysProperty =
            DependencyProperty.Register(
                nameof(HighlightedCellKeys),
                typeof(HashSet<string>),
                typeof(ScheduleGridControl),
                new PropertyMetadata(null, OnHighlightedCellKeysChanged));

        /// <summary>
        /// 表格数据
        /// </summary>
        public ScheduleGridData? GridData
        {
            get => (ScheduleGridData?)GetValue(GridDataProperty);
            set => SetValue(GridDataProperty, value);
        }

        /// <summary>
        /// 高亮显示的单元格键集合
        /// </summary>
        public HashSet<string>? HighlightedCellKeys
        {
            get => (HashSet<string>?)GetValue(HighlightedCellKeysProperty);
            set => SetValue(HighlightedCellKeysProperty, value);
        }

        /// <summary>
        /// 全屏请求事件
        /// </summary>
        public event EventHandler? FullScreenRequested;

        /// <summary>
        /// 导出请求事件
        /// </summary>
        public event EventHandler<ExportRequestedEventArgs>? ExportRequested;

        /// <summary>
        /// 单元格点击事件
        /// </summary>
        public event EventHandler<CellClickedEventArgs>? CellClicked;

        public ScheduleGridControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// GridData 属性变化回调
        /// </summary>
        private static void OnGridDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScheduleGridControl control)
            {
                control.OnGridDataChangedInternal(e.NewValue as ScheduleGridData);
            }
        }

        /// <summary>
        /// HighlightedCellKeys 属性变化回调
        /// </summary>
        private static void OnHighlightedCellKeysChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScheduleGridControl control)
            {
                control.UpdateCellHighlights();
            }
        }

        /// <summary>
        /// 处理 GridData 变化
        /// </summary>
        private void OnGridDataChangedInternal(ScheduleGridData? newData)
        {
            if (newData == null)
            {
                // 清空表格
                ClearGrid();
                return;
            }

            // 构建表格结构（后续任务实现）
            BuildGridStructure(newData);
        }

        /// <summary>
        /// 清空表格
        /// </summary>
        private void ClearGrid()
        {
            // 清空表头
            HeaderGrid.Children.Clear();
            
            // 清空表体
            GridBody.Children.Clear();
        }

        /// <summary>
        /// 构建表格结构
        /// </summary>
        private void BuildGridStructure(ScheduleGridData data)
        {
            // 清空现有内容
            ClearGrid();

            // 1. 创建列头
            CreateColumnHeaders(data);

            // 2. 创建行头和单元格
            CreateRowsAndCells(data);
        }

        /// <summary>
        /// 创建列头（哨位列）
        /// </summary>
        private void CreateColumnHeaders(ScheduleGridData data)
        {
            // 清空列定义
            HeaderGrid.ColumnDefinitions.Clear();
            HeaderGrid.RowDefinitions.Clear();

            // 添加行定义（单行表头）
            HeaderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 第一列：空白列（对应行头）
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

            // 添加空白单元格
            var emptyHeader = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                Padding = new Thickness(8, 4, 8, 4)
            };
            var emptyText = new TextBlock
            {
                Text = "日期/时段",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            emptyHeader.Child = emptyText;
            Grid.SetColumn(emptyHeader, 0);
            Grid.SetRow(emptyHeader, 0);
            HeaderGrid.Children.Add(emptyHeader);

            // 为每个哨位创建列头
            foreach (var column in data.Columns.OrderBy(c => c.ColumnIndex))
            {
                // 添加列定义
                HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120, GridUnitType.Pixel) });

                // 创建列头单元格
                var headerBorder = new Border
                {
                    BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                    BorderThickness = new Thickness(1),
                    Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                    Padding = new Thickness(8, 4, 8, 4)
                };

                var headerText = new TextBlock
                {
                    Text = column.PositionName,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };

                headerBorder.Child = headerText;
                Grid.SetColumn(headerBorder, column.ColumnIndex + 1); // +1 因为第一列是行头
                Grid.SetRow(headerBorder, 0);
                HeaderGrid.Children.Add(headerBorder);
            }
        }

        /// <summary>
        /// 创建行头和单元格
        /// </summary>
        private void CreateRowsAndCells(ScheduleGridData data)
        {
            // 清空列定义和行定义
            GridBody.ColumnDefinitions.Clear();
            GridBody.RowDefinitions.Clear();

            // 第一列：行头列（日期+时段）
            GridBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

            // 为每个哨位添加列定义
            foreach (var column in data.Columns.OrderBy(c => c.ColumnIndex))
            {
                GridBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120, GridUnitType.Pixel) });
            }

            // 为每一行创建行头和单元格
            foreach (var row in data.Rows.OrderBy(r => r.RowIndex))
            {
                // 添加行定义
                GridBody.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40, GridUnitType.Pixel) });

                // 创建行头（日期+时段）
                CreateRowHeader(row);

                // 创建该行的所有单元格
                CreateCells(data, row);
            }
        }

        /// <summary>
        /// 创建行头（日期+时段）
        /// </summary>
        private void CreateRowHeader(ScheduleGridRow row)
        {
            var rowHeaderBorder = new Border
            {
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                Padding = new Thickness(8, 4, 8, 4)
            };

            var rowHeaderText = new TextBlock
            {
                Text = row.DisplayText,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap
            };

            rowHeaderBorder.Child = rowHeaderText;
            Grid.SetColumn(rowHeaderBorder, 0);
            Grid.SetRow(rowHeaderBorder, row.RowIndex);
            GridBody.Children.Add(rowHeaderBorder);
        }

        /// <summary>
        /// 创建人员单元格
        /// </summary>
        private void CreateCells(ScheduleGridData data, ScheduleGridRow row)
        {
            // 为该行的每一列创建单元格
            foreach (var column in data.Columns.OrderBy(c => c.ColumnIndex))
            {
                // 获取单元格数据
                var cellKey = $"{row.RowIndex}_{column.ColumnIndex}";
                var cellData = data.Cells.ContainsKey(cellKey) ? data.Cells[cellKey] : null;

                // 使用 CellModel 控件创建单元格
                var cellControl = new CellModel
                {
                    CellData = cellData ?? new ScheduleGridCell
                    {
                        RowIndex = row.RowIndex,
                        ColumnIndex = column.ColumnIndex,
                        IsAssigned = false
                    }
                };

                // 添加点击事件
                cellControl.CellClicked += (s, cell) =>
                {
                    CellClicked?.Invoke(this, new CellClickedEventArgs(row.RowIndex, column.ColumnIndex, cell));
                };

                // 设置位置
                Grid.SetColumn(cellControl, column.ColumnIndex + 1); // +1 因为第一列是行头
                Grid.SetRow(cellControl, row.RowIndex);

                GridBody.Children.Add(cellControl);
            }
        }

        /// <summary>
        /// 导出按钮点击事件
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportRequested?.Invoke(this, new ExportRequestedEventArgs("excel"));
        }

        /// <summary>
        /// 全屏按钮点击事件
        /// </summary>
        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            FullScreenRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 滚动到指定单元格
        /// </summary>
        /// <param name="rowIndex">行索引</param>
        /// <param name="columnIndex">列索引</param>
        public void ScrollToCell(int rowIndex, int columnIndex)
        {
            try
            {
                // 查找目标单元格
                var targetCell = FindCellElement(rowIndex, columnIndex);
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
        private UIElement? FindCellElement(int rowIndex, int columnIndex)
        {
            // 在 GridBody 中查找对应位置的单元格
            foreach (var child in GridBody.Children)
            {
                if (child is FrameworkElement element)
                {
                    var row = Grid.GetRow(element);
                    var col = Grid.GetColumn(element);

                    // 注意：列索引需要 +1，因为第一列是行头
                    if (row == rowIndex && col == columnIndex + 1)
                    {
                        return element;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 更新所有单元格的高亮状态
        /// </summary>
        private void UpdateCellHighlights()
        {
            var highlightKeys = HighlightedCellKeys ?? new HashSet<string>();

            // 遍历所有单元格，更新高亮状态
            foreach (var child in GridBody.Children)
            {
                if (child is CellModel cellControl)
                {
                    var row = Grid.GetRow(cellControl);
                    var col = Grid.GetColumn(cellControl) - 1; // -1 因为第一列是行头

                    var cellKey = $"{row}_{col}";
                    cellControl.IsHighlighted = highlightKeys.Contains(cellKey);
                }
            }
        }
    }

    /// <summary>
    /// 导出请求事件参数
    /// </summary>
    public class ExportRequestedEventArgs : EventArgs
    {
        public string Format { get; }

        public ExportRequestedEventArgs(string format)
        {
            Format = format;
        }
    }

    /// <summary>
    /// 单元格点击事件参数
    /// </summary>
    public class CellClickedEventArgs : EventArgs
    {
        public int RowIndex { get; }
        public int ColumnIndex { get; }
        public ScheduleGridCell? Cell { get; }

        public CellClickedEventArgs(int rowIndex, int columnIndex, ScheduleGridCell? cell)
        {
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
            Cell = cell;
        }
    }
}
