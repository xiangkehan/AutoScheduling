using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.MainContent
{
    /// <summary>
    /// 列表视图组件，用于以列表形式显示排班结果
    /// </summary>
    public sealed partial class ListView : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// 视图模型属性
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel), typeof(object), typeof(ListView), new PropertyMetadata(null));

        /// <summary>
        /// 视图模型
        /// </summary>
        public object ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// 班次列表项集合
        /// </summary>
        public ObservableCollection<ShiftListItem> ShiftItems { get; set; }

        /// <summary>
        /// 可排序列集合
        /// </summary>
        public ObservableCollection<SortableColumn> SortableColumns { get; set; }

        /// <summary>
        /// 是否为紧凑视图
        /// </summary>
        private bool _isCompactView;
        public bool IsCompactView
        {
            get => _isCompactView;
            set
            {
                if (_isCompactView != value)
                {
                    _isCompactView = value;
                    OnPropertyChanged(nameof(IsCompactView));
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ListView()
        {
            this.InitializeComponent();
            
            // 初始化数据集合
            ShiftItems = new ObservableCollection<ShiftListItem>();
            SortableColumns = new ObservableCollection<SortableColumn>();
            
            // 初始化可排序列
            InitializeSortableColumns();
            
            // 生成模拟数据
            GenerateMockData();
        }

        /// <summary>
        /// 初始化可排序列
        /// </summary>
        private void InitializeSortableColumns()
        {
            var sortCommand = new RelayCommand<string>(SortByColumn);
            
            SortableColumns.Add(new SortableColumn("日期", "Date", sortCommand));
            SortableColumns.Add(new SortableColumn("时间", "TimeSlot", sortCommand));
            SortableColumns.Add(new SortableColumn("哨位", "PositionName", sortCommand));
            SortableColumns.Add(new SortableColumn("人员", "PersonnelName", sortCommand));
            SortableColumns.Add(new SortableColumn("状态", "Status", sortCommand));
        }

        /// <summary>
        /// 按列排序
        /// </summary>
        /// <param name="columnName">列名</param>
        private void SortByColumn(string columnName)
        {
            // 重置所有列的排序状态
            foreach (var column in SortableColumns)
            {
                if (column.ColumnName == columnName)
                {
                    // 切换当前列的排序方向
                    column.SortDirection = column.SortDirection == SortDirection.Ascending ? 
                        SortDirection.Descending : SortDirection.Ascending;
                    column.HasSortIndicator = true;
                }
                else
                {
                    column.SortDirection = SortDirection.None;
                    column.HasSortIndicator = false;
                }
            }
            
            // 根据排序方向对列表进行排序
            var sortedItems = SortableColumns
                .Where(c => c.HasSortIndicator)
                .Select(c => new { c.ColumnName, c.SortDirection })
                .FirstOrDefault();
            
            if (sortedItems != null)
            {
                var items = ShiftItems.ToList();
                
                switch (sortedItems.ColumnName)
                {
                    case "Date":
                        items = sortedItems.SortDirection == SortDirection.Ascending ?
                            items.OrderBy(i => i.Date).ToList() :
                            items.OrderByDescending(i => i.Date).ToList();
                        break;
                    case "TimeSlot":
                        items = sortedItems.SortDirection == SortDirection.Ascending ?
                            items.OrderBy(i => i.TimeSlot).ToList() :
                            items.OrderByDescending(i => i.TimeSlot).ToList();
                        break;
                    case "PositionName":
                        items = sortedItems.SortDirection == SortDirection.Ascending ?
                            items.OrderBy(i => i.PositionName).ToList() :
                            items.OrderByDescending(i => i.PositionName).ToList();
                        break;
                    case "PersonnelName":
                        items = sortedItems.SortDirection == SortDirection.Ascending ?
                            items.OrderBy(i => i.PersonnelName).ToList() :
                            items.OrderByDescending(i => i.PersonnelName).ToList();
                        break;
                    case "Status":
                        items = sortedItems.SortDirection == SortDirection.Ascending ?
                            items.OrderBy(i => i.Status).ToList() :
                            items.OrderByDescending(i => i.Status).ToList();
                        break;
                }
                
                // 更新列表
                ShiftItems.Clear();
                foreach (var item in items)
                {
                    ShiftItems.Add(item);
                }
            }
        }

        /// <summary>
        /// 生成模拟数据
        /// </summary>
        private void GenerateMockData()
        {
            // 生成模拟的班次列表项
            var mockData = new List<ShiftListItem>
            {
                new ShiftListItem
                {
                    Date = "2024-01-01",
                    TimeSlot = "08:00-12:00",
                    PositionName = "哨位A",
                    PersonnelName = "张三",
                    Status = "已分配",
                    HasConflict = false
                },
                new ShiftListItem
                {
                    Date = "2024-01-01",
                    TimeSlot = "12:00-16:00",
                    PositionName = "哨位A",
                    PersonnelName = "李四",
                    Status = "已分配",
                    HasConflict = true,
                    ConflictColor = new SolidColorBrush(Microsoft.UI.Colors.Red)
                },
                new ShiftListItem
                {
                    Date = "2024-01-01",
                    TimeSlot = "16:00-20:00",
                    PositionName = "哨位B",
                    PersonnelName = "王五",
                    Status = "已分配",
                    HasConflict = false
                },
                new ShiftListItem
                {
                    Date = "2024-01-01",
                    TimeSlot = "20:00-24:00",
                    PositionName = "哨位B",
                    PersonnelName = "赵六",
                    Status = "已分配",
                    HasConflict = true,
                    ConflictColor = new SolidColorBrush(Microsoft.UI.Colors.Yellow)
                },
                new ShiftListItem
                {
                    Date = "2024-01-02",
                    TimeSlot = "00:00-04:00",
                    PositionName = "哨位C",
                    PersonnelName = "孙七",
                    Status = "已分配",
                    HasConflict = false
                }
            };
            
            // 添加到集合
            foreach (var item in mockData)
            {
                ShiftItems.Add(item);
            }
        }

        /// <summary>
        /// 属性变化通知事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变化通知
        /// </summary>
        /// <param name="propertyName">属性名</param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 班次列表项类
    /// </summary>
    public class ShiftListItem
    {
        /// <summary>
        /// 日期
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// 时间段
        /// </summary>
        public string TimeSlot { get; set; }

        /// <summary>
        /// 哨位名称
        /// </summary>
        public string PositionName { get; set; }

        /// <summary>
        /// 人员姓名
        /// </summary>
        public string PersonnelName { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 是否有冲突
        /// </summary>
        public bool HasConflict { get; set; }

        /// <summary>
        /// 冲突颜色
        /// </summary>
        public SolidColorBrush ConflictColor { get; set; }
    }

    /// <summary>
    /// 可排序列类
    /// </summary>
    public class SortableColumn
    {
        /// <summary>
        /// 列标题
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// 列名
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// 排序命令
        /// </summary>
        public ICommand SortCommand { get; set; }

        /// <summary>
        /// 排序方向
        /// </summary>
        public SortDirection SortDirection { get; set; }

        /// <summary>
        /// 是否显示排序指示器
        /// </summary>
        public bool HasSortIndicator { get; set; }

        /// <summary>
        /// 排序指示器文本
        /// </summary>
        public string SortIndicator
        {
            get
            {
                return SortDirection == SortDirection.Ascending ? "↑" : "↓";
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="header">列标题</param>
        /// <param name="columnName">列名</param>
        /// <param name="sortCommand">排序命令</param>
        public SortableColumn(string header, string columnName, ICommand sortCommand)
        {
            Header = header;
            ColumnName = columnName;
            SortCommand = sortCommand;
            SortDirection = SortDirection.None;
            HasSortIndicator = false;
        }
    }

    /// <summary>
    /// 排序方向枚举
    /// </summary>
    public enum SortDirection
    {
        None,
        Ascending,
        Descending
    }

    /// <summary>
    /// 简单的命令实现
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="execute">执行方法</param>
        /// <param name="canExecute">是否可执行方法</param>
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 是否可执行变化事件
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// 是否可执行
        /// </summary>
        /// <param name="parameter">参数</param>
        /// <returns>是否可执行</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="parameter">参数</param>
        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        /// <summary>
        /// 触发CanExecuteChanged事件
        /// </summary>
        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
