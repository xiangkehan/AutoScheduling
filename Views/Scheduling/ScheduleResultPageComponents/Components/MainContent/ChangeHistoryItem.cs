using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.MainContent
{
    /// <summary>
    /// 更改历史项
    /// </summary>
    public class ChangeHistoryItem : INotifyPropertyChanged
    {
        /// <summary>
        /// 日期
        /// </summary>
        public System.DateTime Date
        {
            get { return _date; }
            set { SetProperty(ref _date, value); }
        }
        private System.DateTime _date;

        /// <summary>
        /// 哨位名称
        /// </summary>
        public string PositionName
        {
            get { return _positionName; }
            set { SetProperty(ref _positionName, value); }
        }
        private string _positionName;

        /// <summary>
        /// 班次时间
        /// </summary>
        public string ShiftTime
        {
            get { return _shiftTime; }
            set { SetProperty(ref _shiftTime, value); }
        }
        private string _shiftTime;

        /// <summary>
        /// 旧人员名称
        /// </summary>
        public string OldPersonnelName
        {
            get { return _oldPersonnelName; }
            set { SetProperty(ref _oldPersonnelName, value); }
        }
        private string _oldPersonnelName;

        /// <summary>
        /// 新人员名称
        /// </summary>
        public string NewPersonnelName
        {
            get { return _newPersonnelName; }
            set { SetProperty(ref _newPersonnelName, value); }
        }
        private string _newPersonnelName;

        /// <summary>
        /// 旧备注
        /// </summary>
        public string OldRemarks
        {
            get { return _oldRemarks; }
            set { SetProperty(ref _oldRemarks, value); }
        }
        private string _oldRemarks;

        /// <summary>
        /// 新备注
        /// </summary>
        public string NewRemarks
        {
            get { return _newRemarks; }
            set { SetProperty(ref _newRemarks, value); }
        }
        private string _newRemarks;

        /// <summary>
        /// 是否有备注更改
        /// </summary>
        public bool HasRemarksChange => !string.Equals(OldRemarks, NewRemarks, System.StringComparison.OrdinalIgnoreCase);

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            if (propertyName == nameof(OldRemarks) || propertyName == nameof(NewRemarks))
            {
                OnPropertyChanged(nameof(HasRemarksChange));
            }
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
