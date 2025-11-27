using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.MainContent
{
    /// <summary>
    /// 更改历史对话框
    /// 用于显示未保存的班次更改列表
    /// </summary>
    public partial class ChangeHistoryDialog : ContentDialog, INotifyPropertyChanged
    {
        /// <summary>
        /// 更改项集合
        /// </summary>
        public ObservableCollection<ChangeHistoryItem> ChangeItems
        {
            get { return _changeItems; }
            set { SetProperty(ref _changeItems, value); }
        }
        private ObservableCollection<ChangeHistoryItem> _changeItems;

        /// <summary>
        /// 更改数量文本
        /// </summary>
        public string ChangeCountText
        {
            get { return _changeCountText; }
            set { SetProperty(ref _changeCountText, value); }
        }
        private string _changeCountText;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ChangeHistoryDialog()
        {
            this.InitializeComponent();
            ChangeItems = new ObservableCollection<ChangeHistoryItem>();
            UpdateChangeCountText();
        }

        /// <summary>
        /// 更新更改数量文本
        /// </summary>
        private void UpdateChangeCountText()
        {
            ChangeCountText = $"共 {ChangeItems.Count} 项更改";
        }

        /// <summary>
        /// 添加更改项
        /// </summary>
        /// <param name="item">更改项</param>
        public void AddChangeItem(ChangeHistoryItem item)
        {
            ChangeItems.Add(item);
            UpdateChangeCountText();
        }

        /// <summary>
        /// 清空更改项
        /// </summary>
        public void ClearChangeItems()
        {
            ChangeItems.Clear();
            UpdateChangeCountText();
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