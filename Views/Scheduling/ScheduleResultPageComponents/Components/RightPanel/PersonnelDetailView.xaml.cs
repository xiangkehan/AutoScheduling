using AutoScheduling3.DTOs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.RightPanel
{
    /// <summary>
    /// 人员详情视图
    /// </summary>
    public partial class PersonnelDetailView : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// 人员数据
        /// </summary>
        public PersonnelDto Personnel
        {
            get { return _personnel; }
            set { SetProperty(ref _personnel, value); }
        }
        private PersonnelDto _personnel;

        /// <summary>
        /// 总班次
        /// </summary>
        public int TotalShifts
        {
            get { return _totalShifts; }
            set { SetProperty(ref _totalShifts, value); }
        }
        private int _totalShifts;

        /// <summary>
        /// 总工时
        /// </summary>
        public double TotalWorkHours
        {
            get { return _totalWorkHours; }
            set { SetProperty(ref _totalWorkHours, value); }
        }
        private double _totalWorkHours;

        /// <summary>
        /// 平均工时
        /// </summary>
        public double AverageWorkHours
        {
            get { return _averageWorkHours; }
            set { SetProperty(ref _averageWorkHours, value); }
        }
        private double _averageWorkHours;

        /// <summary>
        /// 近期排班
        /// </summary>
        public ObservableCollection<RecentShiftItem> RecentShifts
        {
            get { return _recentShifts; }
            set { SetProperty(ref _recentShifts, value); }
        }
        private ObservableCollection<RecentShiftItem> _recentShifts;

        /// <summary>
        /// 构造函数
        /// </summary>
        public PersonnelDetailView()
        {
            this.InitializeComponent();
            RecentShifts = new ObservableCollection<RecentShiftItem>();
        }

        /// <summary>
        /// 获取人员状态文本
        /// </summary>
        private string GetStatusText(PersonnelDto personnel)
        {
            if (personnel == null) return "未知";
            if (personnel.IsRetired) return "已退役";
            if (!personnel.IsAvailable) return "不可用";
            return "在职";
        }

        /// <summary>
        /// 近期排班项
        /// </summary>
        public class RecentShiftItem
        {
            /// <summary>
            /// 日期
            /// </summary>
            public string Date { get; set; }

            /// <summary>
            /// 哨位名称
            /// </summary>
            public string PositionName { get; set; }

            /// <summary>
            /// 班次时间
            /// </summary>
            public string ShiftTime { get; set; }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}