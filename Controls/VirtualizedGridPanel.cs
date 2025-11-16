using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace AutoScheduling3.Controls
{
    /// <summary>
    /// 虚拟化网格面板，用于高效渲染大规模排班表格
    /// 仅渲染可见区域及缓冲区的单元格，支持单元格回收和重用
    /// </summary>
    public class VirtualizedGridPanel : Panel
    {
        // 单元格尺寸常量
        private const double CellWidth = 120;
        private const double CellHeight = 40;
        private const double RowHeaderWidth = 200;
        
        // 缓冲区大小（可见区域外额外渲染的行列数）
        private const int RowBufferSize = 5;
        private const int ColumnBufferSize = 2;

        // 当前可见的单元格字典，键为 (row, col)
        private readonly Dictionary<(int row, int col), FrameworkElement> _visibleCells = new();
        
        // 单元格回收池，用于重用不可见的单元格控件
        private readonly Queue<FrameworkElement> _recycledCells = new();

        // 数据源相关属性
        private int _totalRows;
        private int _totalColumns;
        private Func<int, int, FrameworkElement>? _cellFactory;

        /// <summary>
        /// 设置总行数
        /// </summary>
        public int TotalRows
        {
            get => _totalRows;
            set
            {
                if (_totalRows != value)
                {
                    _totalRows = value;
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// 设置总列数
        /// </summary>
        public int TotalColumns
        {
            get => _totalColumns;
            set
            {
                if (_totalColumns != value)
                {
                    _totalColumns = value;
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// 设置单元格工厂方法
        /// </summary>
        public Func<int, int, FrameworkElement>? CellFactory
        {
            get => _cellFactory;
            set
            {
                _cellFactory = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// 测量子元素并返回面板所需的大小
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_cellFactory == null || _totalRows == 0 || _totalColumns == 0)
            {
                return new Size(0, 0);
            }

            // 计算可见区域
            var visibleRect = CalculateVisibleRect(availableSize);

            // 确定需要渲染的行列范围（包含缓冲区）
            var (startRow, endRow) = GetVisibleRowRange(visibleRect);
            var (startCol, endCol) = GetVisibleColumnRange(visibleRect);

            // 更新可见单元格
            UpdateVisibleCells(startRow, endRow, startCol, endCol);

            // 测量所有可见单元格
            var cellSize = new Size(CellWidth, CellHeight);
            foreach (var cell in _visibleCells.Values)
            {
                cell.Measure(cellSize);
            }

            // 返回面板总大小（包含行头列）
            var totalWidth = RowHeaderWidth + (_totalColumns * CellWidth);
            var totalHeight = _totalRows * CellHeight;
            return new Size(totalWidth, totalHeight);
        }

        /// <summary>
        /// 排列子元素到指定位置
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_cellFactory == null || _totalRows == 0 || _totalColumns == 0)
            {
                return finalSize;
            }

            // 排列所有可见单元格
            foreach (var kvp in _visibleCells)
            {
                var (row, col) = kvp.Key;
                var cell = kvp.Value;

                // 计算单元格位置（col 0 是行头列）
                var x = col == 0 ? 0 : RowHeaderWidth + ((col - 1) * CellWidth);
                var y = row * CellHeight;
                var width = col == 0 ? RowHeaderWidth : CellWidth;
                var height = CellHeight;

                var rect = new Rect(x, y, width, height);
                cell.Arrange(rect);
            }

            return finalSize;
        }

        /// <summary>
        /// 计算当前可见区域的矩形范围
        /// </summary>
        private Rect CalculateVisibleRect(Size availableSize)
        {
            // 获取父级 ScrollViewer
            var scrollViewer = FindParentScrollViewer(this);
            if (scrollViewer == null)
            {
                // 如果没有 ScrollViewer，返回整个可用区域
                return new Rect(0, 0, availableSize.Width, availableSize.Height);
            }

            // 获取滚动偏移量
            var horizontalOffset = scrollViewer.HorizontalOffset;
            var verticalOffset = scrollViewer.VerticalOffset;

            // 获取视口大小
            var viewportWidth = scrollViewer.ViewportWidth;
            var viewportHeight = scrollViewer.ViewportHeight;

            return new Rect(horizontalOffset, verticalOffset, viewportWidth, viewportHeight);
        }

        /// <summary>
        /// 获取可见行范围（包含缓冲区）
        /// </summary>
        private (int startRow, int endRow) GetVisibleRowRange(Rect visibleRect)
        {
            if (CellHeight <= 0 || _totalRows == 0)
            {
                return (0, 0);
            }

            // 计算可见的起始和结束行
            var startRow = Math.Max(0, (int)(visibleRect.Top / CellHeight) - RowBufferSize);
            var endRow = Math.Min(_totalRows - 1, (int)((visibleRect.Bottom / CellHeight) + RowBufferSize));

            return (startRow, endRow);
        }

        /// <summary>
        /// 获取可见列范围（包含缓冲区）
        /// </summary>
        private (int startCol, int endCol) GetVisibleColumnRange(Rect visibleRect)
        {
            if (CellWidth <= 0 || _totalColumns == 0)
            {
                return (0, 0);
            }

            // 列 0 是行头列，始终可见
            if (visibleRect.Left <= RowHeaderWidth)
            {
                // 视口包含行头列
                var startCol = 0;
                
                // 计算数据列的结束位置
                var dataColumnsVisible = (visibleRect.Right - RowHeaderWidth) / CellWidth;
                var endCol = Math.Min(_totalColumns, (int)(dataColumnsVisible + ColumnBufferSize));
                
                return (startCol, endCol);
            }
            else
            {
                // 视口不包含行头列（横向滚动较远）
                var dataStartCol = (visibleRect.Left - RowHeaderWidth) / CellWidth;
                var startCol = Math.Max(1, (int)(dataStartCol - ColumnBufferSize));
                
                var dataEndCol = (visibleRect.Right - RowHeaderWidth) / CellWidth;
                var endCol = Math.Min(_totalColumns, (int)(dataEndCol + ColumnBufferSize));
                
                return (startCol, endCol);
            }
        }

        /// <summary>
        /// 更新可见单元格，动态加载和卸载单元格
        /// </summary>
        private void UpdateVisibleCells(int startRow, int endRow, int startCol, int endCol)
        {
            if (_cellFactory == null)
            {
                return;
            }

            // 第一步：移除不可见的单元格并回收
            var cellsToRemove = _visibleCells
                .Where(kvp => kvp.Key.row < startRow || kvp.Key.row > endRow ||
                             kvp.Key.col < startCol || kvp.Key.col > endCol)
                .ToList();

            foreach (var kvp in cellsToRemove)
            {
                _visibleCells.Remove(kvp.Key);
                RecycleCell(kvp.Value);
            }

            // 第二步：添加新的可见单元格
            for (int row = startRow; row <= endRow; row++)
            {
                // 始终包含行头列（col 0）
                if (!_visibleCells.ContainsKey((row, 0)))
                {
                    var cell = GetOrCreateCell(row, 0);
                    _visibleCells[(row, 0)] = cell;
                }

                // 添加数据列
                for (int col = startCol; col <= endCol; col++)
                {
                    if (col == 0) continue; // 行头列已处理

                    if (!_visibleCells.ContainsKey((row, col)))
                    {
                        var cell = GetOrCreateCell(row, col);
                        _visibleCells[(row, col)] = cell;
                    }
                }
            }
        }

        /// <summary>
        /// 获取或创建单元格（优先从回收池获取）
        /// </summary>
        private FrameworkElement GetOrCreateCell(int row, int col)
        {
            FrameworkElement cell;

            // 尝试从回收池获取
            if (_recycledCells.Count > 0)
            {
                cell = _recycledCells.Dequeue();
                UpdateCellData(cell, row, col);
            }
            else
            {
                // 创建新单元格
                cell = CreateNewCell(row, col);
            }

            // 确保单元格在可视树中
            if (!Children.Contains(cell))
            {
                Children.Add(cell);
            }

            return cell;
        }

        /// <summary>
        /// 创建新单元格
        /// </summary>
        private FrameworkElement CreateNewCell(int row, int col)
        {
            if (_cellFactory == null)
            {
                throw new InvalidOperationException("CellFactory is not set");
            }

            return _cellFactory(row, col);
        }

        /// <summary>
        /// 更新单元格数据（用于回收的单元格）
        /// </summary>
        private void UpdateCellData(FrameworkElement cell, int row, int col)
        {
            // 通过 Tag 属性传递行列信息，供外部更新数据使用
            cell.Tag = (row, col);

            // 如果单元格实现了特定接口，可以调用更新方法
            // 这里简化处理，外部通过 CellFactory 创建时应包含数据绑定逻辑
        }

        /// <summary>
        /// 回收单元格到回收池
        /// </summary>
        private void RecycleCell(FrameworkElement cell)
        {
            // 从可视树中移除
            if (Children.Contains(cell))
            {
                Children.Remove(cell);
            }

            // 加入回收池
            _recycledCells.Enqueue(cell);
        }

        /// <summary>
        /// 查找父级 ScrollViewer
        /// </summary>
        private ScrollViewer? FindParentScrollViewer(DependencyObject child)
        {
            var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(child);
            
            while (parent != null)
            {
                if (parent is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }
                parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        /// <summary>
        /// 清空所有单元格和回收池
        /// </summary>
        public void Clear()
        {
            _visibleCells.Clear();
            _recycledCells.Clear();
            Children.Clear();
        }

        /// <summary>
        /// 刷新可见单元格（在数据变化时调用）
        /// </summary>
        public void Refresh()
        {
            InvalidateMeasure();
            InvalidateArrange();
        }
    }
}
