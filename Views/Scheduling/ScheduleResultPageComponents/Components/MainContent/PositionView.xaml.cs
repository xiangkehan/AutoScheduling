using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.MainContent
{
    /// <summary>
    /// 按哨位视图组件，用于以哨位分组形式显示排班结果
    /// </summary>
    public sealed partial class PositionView : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// 视图模型属性
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel), typeof(object), typeof(PositionView), new PropertyMetadata(null));

        /// <summary>
        /// 视图模型
        /// </summary>
        public object ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// 哨位分组集合
        /// </summary>
        public ObservableCollection<PositionGroup> PositionGroups { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PositionView()
        {
            this.InitializeComponent();
            
            // 初始化数据集合
            PositionGroups = new ObservableCollection<PositionGroup>();
            
            // 生成模拟数据
            GenerateMockData();
        }

        /// <summary>
        /// 生成模拟数据
        /// </summary>
        private void GenerateMockData()
        {
            // 生成模拟的哨位分组数据
            var mockGroups = new List<PositionGroup>
            {
                new PositionGroup
                {
                    PositionName = "哨位A",
                    PositionId = 1,
                    ShiftCount = "15个班次",
                    ConflictCount = "3个冲突",
                    CoverageRate = "95%覆盖率",
                    UnassignedCount = "1个未分配",
                    IsExpanded = true,
                    Shifts = new ObservableCollection<PositionShiftItem>
                    {
                        new PositionShiftItem
                        {
                            Date = "2024-01-01",
                            TimeSlot = "08:00-12:00",
                            PersonnelName = "张三",
                            HasConflict = false
                        },
                        new PositionShiftItem
                        {
                            Date = "2024-01-01",
                            TimeSlot = "12:00-16:00",
                            PersonnelName = "李四",
                            HasConflict = true,
                            ConflictColor = new SolidColorBrush(Microsoft.UI.Colors.Red)
                        },
                        new PositionShiftItem
                        {
                            Date = "2024-01-01",
                            TimeSlot = "16:00-20:00",
                            PersonnelName = "王五",
                            HasConflict = false
                        },
                        new PositionShiftItem
                        {
                            Date = "2024-01-01",
                            TimeSlot = "20:00-24:00",
                            PersonnelName = "",
                            HasConflict = false
                        }
                    }
                },
                new PositionGroup
                {
                    PositionName = "哨位B",
                    PositionId = 2,
                    ShiftCount = "12个班次",
                    ConflictCount = "1个冲突",
                    CoverageRate = "90%覆盖率",
                    UnassignedCount = "2个未分配",
                    IsExpanded = false,
                    Shifts = new ObservableCollection<PositionShiftItem>
                    {
                        new PositionShiftItem
                        {
                            Date = "2024-01-01",
                            TimeSlot = "08:00-12:00",
                            PersonnelName = "赵六",
                            HasConflict = false
                        },
                        new PositionShiftItem
                        {
                            Date = "2024-01-01",
                            TimeSlot = "12:00-16:00",
                            PersonnelName = "孙七",
                            HasConflict = true,
                            ConflictColor = new SolidColorBrush(Microsoft.UI.Colors.Yellow)
                        }
                    }
                },
                new PositionGroup
                {
                    PositionName = "哨位C",
                    PositionId = 3,
                    ShiftCount = "10个班次",
                    ConflictCount = "0个冲突",
                    CoverageRate = "100%覆盖率",
                    UnassignedCount = "0个未分配",
                    IsExpanded = false,
                    Shifts = new ObservableCollection<PositionShiftItem>
                    {
                        new PositionShiftItem
                        {
                            Date = "2024-01-01",
                            TimeSlot = "08:00-12:00",
                            PersonnelName = "周八",
                            HasConflict = false
                        },
                        new PositionShiftItem
                        {
                            Date = "2024-01-01",
                            TimeSlot = "12:00-16:00",
                            PersonnelName = "吴九",
                            HasConflict = false
                        }
                    }
                }
            };
            
            // 添加到集合
            foreach (var group in mockGroups)
            {
                PositionGroups.Add(group);
            }
        }

        /// <summary>
        /// 分组头点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void GroupHeaderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PositionGroup group)
            {
                // 切换展开/折叠状态
                group.IsExpanded = !group.IsExpanded;
            }
        }

        /// <summary>
        /// 展开所有按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void ExpandAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var group in PositionGroups)
            {
                group.IsExpanded = true;
            }
        }

        /// <summary>
        /// 折叠所有按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void CollapseAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var group in PositionGroups)
            {
                group.IsExpanded = false;
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
    /// 哨位分组类
    /// </summary>
    public class PositionGroup : INotifyPropertyChanged
    {
        /// <summary>
        /// 哨位ID
        /// </summary>
        public int PositionId { get; set; }

        /// <summary>
        /// 哨位名称
        /// </summary>
        public string PositionName { get; set; }

        /// <summary>
        /// 班次数量
        /// </summary>
        public string ShiftCount { get; set; }

        /// <summary>
        /// 冲突数量
        /// </summary>
        public string ConflictCount { get; set; }

        /// <summary>
        /// 覆盖率
        /// </summary>
        public string CoverageRate { get; set; }

        /// <summary>
        /// 未分配数量
        /// </summary>
        public string UnassignedCount { get; set; }

        /// <summary>
        /// 是否展开
        /// </summary>
        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        /// <summary>
        /// 班次列表
        /// </summary>
        public ObservableCollection<PositionShiftItem> Shifts { get; set; }

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
    /// 哨位班次项类
    /// </summary>
    public class PositionShiftItem
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
        /// 人员姓名
        /// </summary>
        public string PersonnelName { get; set; }

        /// <summary>
        /// 是否有冲突
        /// </summary>
        public bool HasConflict { get; set; }

        /// <summary>
        /// 冲突颜色
        /// </summary>
        public SolidColorBrush ConflictColor { get; set; }
    }
}