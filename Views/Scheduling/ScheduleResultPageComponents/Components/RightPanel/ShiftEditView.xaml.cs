using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.RightPanel
{
    /// <summary>
    /// 班次编辑视图组件，用于编辑班次信息
    /// </summary>
    public sealed partial class ShiftEditView : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// 视图模型属性
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel), typeof(object), typeof(ShiftEditView), new PropertyMetadata(null));

        /// <summary>
        /// 视图模型
        /// </summary>
        public object ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        public event EventHandler SaveClicked;

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        public event EventHandler CancelClicked;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ShiftEditView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// 设置班次数据
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="timeSlot">时间段</param>
        /// <param name="position">哨位</param>
        /// <param name="personnel">人员</param>
        public void SetShiftData(string date, string timeSlot, string position, string personnel = "")
        {
            DateTextBlock.Text = date;
            TimeSlotTextBlock.Text = timeSlot;
            PositionTextBlock.Text = position;
            
            // 设置人员选择
            if (!string.IsNullOrWhiteSpace(personnel))
            {
                foreach (ComboBoxItem item in PersonnelComboBox.Items)
                {
                    if (item.Content.ToString() == personnel)
                    {
                        PersonnelComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                PersonnelComboBox.SelectedItem = null;
            }
            
            // 设置时间段选择
            var timeParts = timeSlot.Split('-');
            if (timeParts.Length == 2)
            {
                foreach (ComboBoxItem item in StartTimeComboBox.Items)
                {
                    if (item.Content.ToString() == timeParts[0])
                    {
                        StartTimeComboBox.SelectedItem = item;
                        break;
                    }
                }
                
                foreach (ComboBoxItem item in EndTimeComboBox.Items)
                {
                    if (item.Content.ToString() == timeParts[1])
                    {
                        EndTimeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
            
            // 清空备注
            RemarksTextBox.Text = string.Empty;
        }

        /// <summary>
        /// 获取编辑后的班次数据
        /// </summary>
        /// <returns>编辑后的班次数据</returns>
        public ShiftEditData GetShiftEditData()
        {
            return new ShiftEditData
            {
                Date = DateTextBlock.Text,
                Position = PositionTextBlock.Text,
                Personnel = PersonnelComboBox.SelectedItem != null ? 
                    (PersonnelComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() : string.Empty,
                StartTime = StartTimeComboBox.SelectedItem != null ? 
                    (StartTimeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() : string.Empty,
                EndTime = EndTimeComboBox.SelectedItem != null ? 
                    (EndTimeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() : string.Empty,
                Remarks = RemarksTextBox.Text
            };
        }

        /// <summary>
        /// 保存按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveClicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 取消按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelClicked?.Invoke(this, EventArgs.Empty);
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
    /// 班次编辑数据类
    /// </summary>
    public class ShiftEditData
    {
        /// <summary>
        /// 日期
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// 哨位
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// 人员
        /// </summary>
        public string Personnel { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public string EndTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// 获取完整的时间段
        /// </summary>
        public string TimeSlot => $"{StartTime}-{EndTime}";
    }
}