using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using AutoScheduling3.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Text;
using Windows.UI.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoScheduling3.Controls
{
    /// <summary>
    /// 人员排班视图控件（按人员显示工作量、班次列表和日历视图）
    /// </summary>
    public sealed partial class PersonnelScheduleControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Add properties for data binding
        public ObservableCollection<PersonnelShiftItem> SelectedDateShifts { get; } = new ObservableCollection<PersonnelShiftItem>();
        
        private bool _hasSelectedDateShifts;
        public bool HasSelectedDateShifts
        {
            get => _hasSelectedDateShifts;
            set
            {
                if (_hasSelectedDateShifts != value)
                {
                    _hasSelectedDateShifts = value;
                    OnPropertyChanged();
                }
            }
        }
        // Add this constructor to ensure InitializeComponent is called
        public PersonnelScheduleControl()
        {
            InitializeComponent();
            _currentMonth = DateTime.Now;
        }

        /// <summary>
        /// ScheduleData 依赖属性
        /// </summary>
        public static readonly DependencyProperty ScheduleDataProperty =
            DependencyProperty.Register(
                nameof(ScheduleData),
                typeof(PersonnelScheduleData),
                typeof(PersonnelScheduleControl),
                new PropertyMetadata(null, OnScheduleDataChanged));

        /// <summary>
        /// 排班数据
        /// </summary>
        public PersonnelScheduleData? ScheduleData
        {
            get => (PersonnelScheduleData?)GetValue(ScheduleDataProperty);
            set => SetValue(ScheduleDataProperty, value);
        }

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

        /// <summary>
        /// 班次详情查看事件
        /// </summary>
        public event EventHandler<ShiftClickedEventArgs>? ShiftClicked;

   

        // 时段描述数组
        private static readonly string[] TimeSlotDescriptions = new[]
        {
            "00:00-02:00", "02:00-04:00", "04:00-06:00", "06:00-08:00",
            "08:00-10:00", "10:00-12:00", "12:00-14:00", "14:00-16:00",
            "16:00-18:00", "18:00-20:00", "20:00-22:00", "22:00-00:00"
        };

        // 星期数组
        private static readonly string[] DayOfWeekNames = new[]
        {
            "周日", "周一", "周二", "周三", "周四", "周五", "周六"
        };

        private DateTime _currentMonth;
        private DateTime? _selectedDate;

        /// <summary>
        /// ScheduleData 属性变化回调
        /// </summary>
        private static void OnScheduleDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PersonnelScheduleControl control)
            {
                control.OnScheduleDataChangedInternal(e.NewValue as PersonnelScheduleData);
            }
        }

        /// <summary>
        /// 处理 ScheduleData 变化
        /// </summary>
        private void OnScheduleDataChangedInternal(PersonnelScheduleData? newData)
        {
            if (newData == null)
            {
                ClearControl();
                return;
            }

            // 更新人员姓名
            PersonnelNameText.Text = newData.PersonnelName;

            // 更新工作量统计
            UpdateWorkloadStatistics(newData.Workload);

            // 填充周次选择器
            PopulateWeekComboBox(newData);

            // 初始化列表视图
            UpdateShiftsListView(newData.Shifts);

            // 初始化日历视图
            InitializeCalendarView(newData);

            // 默认显示列表视图
            ShowListView();
        }

        /// <summary>
        /// 清空控件
        /// </summary>
        private void ClearControl()
        {
            PersonnelNameText.Text = "未选择";
            TotalShiftsText.Text = "0";
            DayShiftsText.Text = "0";
            NightShiftsText.Text = "0";
            WorkHoursText.Text = "0 小时";
            WeekComboBox.Items.Clear();
            ShiftsListView.Items.Clear();
            CalendarItemsRepeater.ItemsSource = null;
            SelectedDateShifts.Clear();
            HasSelectedDateShifts = false;
        }

        /// <summary>
        /// 更新工作量统计卡片
        /// </summary>
        private void UpdateWorkloadStatistics(PersonnelWorkload workload)
        {
            // 更新总班次数
            TotalShiftsText.Text = workload.TotalShifts.ToString();

            // 更新日哨数
            DayShiftsText.Text = workload.DayShifts.ToString();

            // 更新夜哨数
            NightShiftsText.Text = workload.NightShifts.ToString();

            // 计算工作时长（每个班次2小时）
            int workHours = workload.TotalShifts * 2;
            WorkHoursText.Text = $"{workHours} 小时";

            // 更新进度条（假设最大班次数为20作为示例）
            int maxShifts = Math.Max(workload.TotalShifts * 2, 20);
            TotalShiftsProgress.Maximum = maxShifts;
            TotalShiftsProgress.Value = workload.TotalShifts;

            DayShiftsProgress.Maximum = maxShifts;
            DayShiftsProgress.Value = workload.DayShifts;

            NightShiftsProgressBar.Maximum = maxShifts;
            NightShiftsProgressBar.Value = workload.NightShifts;

            WorkHoursProgress.Maximum = maxShifts * 2;
            WorkHoursProgress.Value = workHours;
        }

        /// <summary>
        /// 填充周次选择器
        /// </summary>
        private void PopulateWeekComboBox(PersonnelScheduleData data)
        {
            WeekComboBox.SelectionChanged -= WeekComboBox_SelectionChanged;
            WeekComboBox.Items.Clear();

            // TODO: 当有周数据时，添加周次选择项
            // 这里需要根据实际的WeekData结构来实现

            WeekComboBox.SelectionChanged += WeekComboBox_SelectionChanged;
        }

        /// <summary>
        /// 更新班次列表视图
        /// </summary>
        private void UpdateShiftsListView(List<PersonnelShift> shifts)
        {
            if (shifts == null)
            {
                ShiftsListView.Items.Clear();
                return;
            }

            // 转换为列表项并排序
            var shiftItems = shifts
                .OrderBy(s => s.Date)
                .ThenBy(s => s.PeriodIndex)
                .Select(s => new PersonnelShiftItem
                {
                    Date = s.Date,
                    DateString = s.Date.ToString("MM-dd"),
                    DayOfWeek = DayOfWeekNames[(int)s.Date.DayOfWeek],
                    TimeSlot = s.TimeSlot,
                    PositionName = s.PositionName,
                    IsNightShift = s.IsNightShift,
                    IsManualAssignment = s.IsManualAssignment,
                    Remarks = s.Remarks ?? "",
                    HasRemarks = !string.IsNullOrEmpty(s.Remarks),
                    OriginalShift = s
                })
                .ToList();

            ShiftsListView.ItemsSource = shiftItems;
        }

        /// <summary>
        /// 初始化日历视图
        /// </summary>
        private void InitializeCalendarView(PersonnelScheduleData data)
        {
            _currentMonth = DateTime.Now;
            UpdateMonthYearText();
            BuildCalendarDays(data.CalendarData);
        }

        /// <summary>
        /// 构建日历天数
        /// </summary>
        private void BuildCalendarDays(Dictionary<DateTime, List<PersonnelShift>>? calendarData)
        {
            if (calendarData == null)
            {
                CalendarItemsRepeater.ItemsSource = null;
                return;
            }

            var items = new List<CalendarDayItem>();

            // 获取月份的第一天和最后一天
            var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // 获取第一周需要显示的起始日期（从周日开始）
            int daysToSubtract = (int)firstDayOfMonth.DayOfWeek;
            var startDate = firstDayOfMonth.AddDays(-daysToSubtract);

            // 生成42天的日期（6周）
            for (int i = 0; i < 42; i++)
            {
                var date = startDate.AddDays(i);
                var isCurrentMonth = date.Month == _currentMonth.Month;
                var isToday = date.Date == DateTime.Today;

                // 获取该日期的班次
                var shifts = calendarData.ContainsKey(date.Date) ? calendarData[date.Date] : new List<PersonnelShift>();

                var item = new CalendarDayItem
                {
                    Date = date,
                    DayNumber = date.Day,
                    IsCurrentMonth = isCurrentMonth,
                    IsToday = isToday,
                    HasShifts = shifts.Count > 0,
                    HasDayShift = shifts.Any(s => !s.IsNightShift),
                    HasNightShift = shifts.Any(s => s.IsNightShift),
                    DayForeground = isCurrentMonth
                        ? (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
                        : (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
                    Shifts = shifts
                };

                items.Add(item);
            }

            CalendarItemsRepeater.ItemsSource = items;
        }

        /// <summary>
        /// 更新月年份文本
        /// </summary>
        private void UpdateMonthYearText()
        {
            MonthYearText.Text = $"{_currentMonth.Year}年{_currentMonth.Month}月";
        }

        /// <summary>
        /// 显示列表视图
        /// </summary>
        private void ShowListView()
        {
            ListViewRadioButton.IsChecked = true;
            CalendarViewRadioButton.IsChecked = false;
            ShiftsListView.Visibility = Visibility.Visible;
            CalendarViewContainer.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 显示日历视图
        /// </summary>
        private void ShowCalendarView()
        {
            ListViewRadioButton.IsChecked = false;
            CalendarViewRadioButton.IsChecked = true;
            ShiftsListView.Visibility = Visibility.Collapsed;
            CalendarViewContainer.Visibility = Visibility.Visible;
        }

        #region 事件处理程序

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
        /// 周次选择变化事件
        /// </summary>
        private void WeekComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: 实现周次切换逻辑
        }

        /// <summary>
        /// 视图模式切换事件
        /// </summary>
        private void ViewModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is string viewMode)
            {
                if (viewMode == "List")
                {
                    ShowListView();
                }
                else if (viewMode == "Calendar")
                {
                    ShowCalendarView();
                }
            }
        }

        /// <summary>
        /// 上一个月按钮点击事件
        /// </summary>
        private void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            UpdateMonthYearText();
            BuildCalendarDays(ScheduleData?.CalendarData);
        }

        /// <summary>
        /// 下一个月按钮点击事件
        /// </summary>
        private void NextMonthButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            UpdateMonthYearText();
            BuildCalendarDays(ScheduleData?.CalendarData);
        }

        /// <summary>
        /// 日历日期项点击事件
        /// </summary>
        private void CalendarDayItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is CalendarDayItem dayItem)
            {
                _selectedDate = dayItem.Date;
                UpdateSelectedDateShifts(dayItem.Shifts);

                // Re-build calendar to update selection state
                BuildCalendarDays(ScheduleData?.CalendarData);
            }
        }

        /// <summary>
        /// 更新选中日期的班次
        /// </summary>
        private void UpdateSelectedDateShifts(List<PersonnelShift> shifts)
        {
            SelectedDateShifts.Clear();

            if (shifts != null && shifts.Count > 0)
            {
                var shiftItems = shifts
                    .OrderBy(s => s.PeriodIndex)
                    .Select(s => new PersonnelShiftItem
                    {
                        Date = s.Date,
                        TimeSlot = s.TimeSlot,
                        PositionName = s.PositionName,
                        IsNightShift = s.IsNightShift,
                        IsManualAssignment = s.IsManualAssignment,
                        Remarks = s.Remarks ?? "",
                        HasRemarks = !string.IsNullOrEmpty(s.Remarks),
                        OriginalShift = s
                    });

                foreach (var item in shiftItems)
                {
                    SelectedDateShifts.Add(item);
                }
            }
            HasSelectedDateShifts = SelectedDateShifts.Any();
        }

        /// <summary>
        /// 查看班次详情按钮点击事件
        /// </summary>
        private void ViewShiftDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PersonnelShiftItem shiftItem)
            {
                ShiftClicked?.Invoke(this, new ShiftClickedEventArgs(shiftItem.OriginalShift));
            }
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 人员班次列表项（用于列表视图显示）
    /// </summary>
    public class PersonnelShiftItem
    {
        public DateTime Date { get; set; }
        public string DateString { get; set; } = string.Empty;
        public string DayOfWeek { get; set; } = string.Empty;
        public string TimeSlot { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public bool IsNightShift { get; set; }
        public bool IsManualAssignment { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public bool HasRemarks { get; set; }
        public PersonnelShift OriginalShift { get; set; } = new();
    }

    /// <summary>
    /// 日历日期项（用于日历视图显示）
    /// </summary>
    public class CalendarDayItem
    {
        public DateTime Date { get; set; }
        public int DayNumber { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public bool HasShifts { get; set; }
        public bool HasDayShift { get; set; }
        public bool HasNightShift { get; set; }
        public Brush DayForeground { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Black);
        public FontWeight DayFontWeight { get; set; }
        public List<PersonnelShift> Shifts { get; set; } = new();
        public bool HasSelectedDateShifts => Shifts?.Count > 0;
    }

    #endregion

    #region 事件参数

    /// <summary>
    /// 班次点击事件参数
    /// </summary>
    public class ShiftClickedEventArgs : EventArgs
    {
        public PersonnelShift Shift { get; }

        public ShiftClickedEventArgs(PersonnelShift shift)
        {
            Shift = shift;
        }
    }

    #endregion
}
