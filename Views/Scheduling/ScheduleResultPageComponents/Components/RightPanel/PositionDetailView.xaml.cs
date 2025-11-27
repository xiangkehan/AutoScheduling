using AutoScheduling3.DTOs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.RightPanel
{
    /// <summary>
    /// 哨位详情视图
    /// </summary>
    public partial class PositionDetailView : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// 哨位数据
        /// </summary>
        public PositionDto Position
        {
            get { return _position; }
            set { SetProperty(ref _position, value); }
        }
        private PositionDto _position;

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
        /// 已排班班次
        /// </summary>
        public int ScheduledShifts
        {
            get { return _scheduledShifts; }
            set { SetProperty(ref _scheduledShifts, value); }
        }
        private int _scheduledShifts;

        /// <summary>
        /// 空缺班次
        /// </summary>
        public int VacantShifts
        {
            get { return _vacantShifts; }
            set { SetProperty(ref _vacantShifts, value); }
        }
        private int _vacantShifts;

        /// <summary>
        /// 覆盖率
        /// </summary>
        public string CoverageRate
        {
            get { return _coverageRate; }
            set { SetProperty(ref _coverageRate, value); }
        }
        private string _coverageRate;

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
        public PositionDetailView()
        {
            this.InitializeComponent();
            RecentShifts = new ObservableCollection<RecentShiftItem>();
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
            /// 人员名称
            /// </summary>
            public string PersonnelName { get; set; }

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