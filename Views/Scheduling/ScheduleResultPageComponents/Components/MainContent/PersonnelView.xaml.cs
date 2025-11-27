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
    /// 按人员视图组件，用于以人员分组形式显示排班结果
    /// </summary>
    public sealed partial class PersonnelView : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// 视图模型属性
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel), typeof(object), typeof(PersonnelView), new PropertyMetadata(null));

        /// <summary>
        /// 视图模型
        /// </summary>
        public object ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// 人员分组集合
        /// </summary>
        public ObservableCollection<PersonnelGroup> PersonnelGroups { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PersonnelView()
        {
            this.InitializeComponent();
            
            // 初始化数据集合
            PersonnelGroups = new ObservableCollection<PersonnelGroup>();
            
            // 生成模拟数据
            GenerateMockData();
        }

        /// <summary>
        /// 生成模拟数据
        /// </summary>
        private void GenerateMockData()
        {
            // 生成模拟的人员分组数据
            var mockGroups = new List<PersonnelGroup>
            {
                new PersonnelGroup
                {
                    PersonnelName = "张三",
                    PersonnelId = 1,
                    ShiftCount = "12个班次",
                    ConflictCount = "2个冲突",
                    TotalHours = "48小时",
                    NightShiftCount = "3个夜哨",
                    IsExpanded = true,
                    Shifts = new ObservableCollection<PersonnelShiftItem>
                    {
                        new PersonnelShiftItem
                        {
                            Date = "2024-01-01",
                            TimeSlot = "08:00-12:00",
                            PositionName = "哨位A",
                            HasConflict = false
                        },
                        new PersonnelShiftItem
                        {
                            Date = "2024-01-02",
                            TimeSlot = "12:00-16:00",
                            PositionName = "哨位B",
                            HasConflict = true,
                            ConflictColor = new SolidColorBrush(Microsoft.UI.Colors.Red)
                        },
                        new PersonnelShiftItem
                        {
                            Date = "2024-01-03",
                            TimeSlot = "20:00-24:00",
                            PositionName = "哨位C",
                            HasConflict = false
                        }
                    }
                },
                new PersonnelGroup
                {
                    PersonnelName = "李四",
                    PersonnelId = 2,
                    ShiftCount = "10个班次",
                    ConflictCount = "1个冲突",
                    TotalHours = "40小时",
                    NightShiftCount = "2个夜哨",
                    IsExpanded = false,
                    Shifts = new ObservableCollection<PersonnelShiftItem>
                    {
                        new PersonnelShiftItem
                        {
                            Date = "2024-01-01",
                            TimeSlot = "12:00-16:00",
                            PositionName = "哨位A",
                            HasConflict = true,
                            ConflictColor = new SolidColorBrush(Microsoft.UI.Colors.Yellow)
                        },
                        new PersonnelShiftItem
                        {
                            Date = "2024-01-02",
                            TimeSlot = "16:00-20:00",
                            PositionName = "哨位B",
                            HasConflict = false
                        }
                    }
                },
                new PersonnelGroup
                {
                    PersonnelName = "王五",
                    PersonnelId = 3,
                    ShiftCount = "15个班次",
                    ConflictCount = "0个冲突",
                    TotalHours = "60小时",
                    NightShiftCount = "5个夜哨",
                    IsExpanded = false,
                    Shifts = new ObservableCollection<PersonnelShiftItem>
                    {
                        new PersonnelShiftItem
                        {
                            Date = "2024-01-01",
                            TimeSlot = "16:00-20:00",
                            PositionName = "哨位B",
                            HasConflict = false
                        },
                        new PersonnelShiftItem
                        {
                            Date = "2024-01-02",
                            TimeSlot = "00:00-04:00",
                            PositionName = "哨位C",
                            HasConflict = false
                        }
                    }
                }
            };
            
            // 添加到集合
            foreach (var group in mockGroups)
            {
                PersonnelGroups.Add(group);
            }
        }

        /// <summary>
        /// 分组头点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void GroupHeaderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PersonnelGroup group)
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
            foreach (var group in PersonnelGroups)
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
            foreach (var group in PersonnelGroups)
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
    /// 人员分组类
    /// </summary>
    public class PersonnelGroup : INotifyPropertyChanged
    {
        /// <summary>
        /// 人员ID
        /// </summary>
        public int PersonnelId { get; set; }

        /// <summary>
        /// 人员姓名
        /// </summary>
        public string PersonnelName { get; set; }

        /// <summary>
        /// 班次数量
        /// </summary>
        public string ShiftCount { get; set; }

        /// <summary>
        /// 冲突数量
        /// </summary>
        public string ConflictCount { get; set; }

        /// <summary>
        /// 总工时
        /// </summary>
        public string TotalHours { get; set; }

        /// <summary>
        /// 夜哨数量
        /// </summary>
        public string NightShiftCount { get; set; }

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
        public ObservableCollection<PersonnelShiftItem> Shifts { get; set; }

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
    /// 人员班次项类
    /// </summary>
    public class PersonnelShiftItem
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
        /// 是否有冲突
        /// </summary>
        public bool HasConflict { get; set; }

        /// <summary>
        /// 冲突颜色
        /// </summary>
        public SolidColorBrush ConflictColor { get; set; }
    }
}