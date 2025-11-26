using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// 排班单元格ViewModel
    /// </summary>
    public partial class ScheduleCellViewModel : ObservableObject
    {
        /// <summary>
        /// 人员姓名
        /// </summary>
        [ObservableProperty]
        private string _personnelName = string.Empty;

        /// <summary>
        /// 人员ID
        /// </summary>
        [ObservableProperty]
        private int? _personnelId;

        /// <summary>
        /// 哨位名称
        /// </summary>
        [ObservableProperty]
        private string _positionName = string.Empty;

        /// <summary>
        /// 哨位ID
        /// </summary>
        [ObservableProperty]
        private int _positionId;

        /// <summary>
        /// 时段索引
        /// </summary>
        [ObservableProperty]
        private int _timeSlot;

        /// <summary>
        /// 日期
        /// </summary>
        [ObservableProperty]
        private System.DateTime _date;

        /// <summary>
        /// 是否有硬约束冲突
        /// </summary>
        [ObservableProperty]
        private bool _hasHardConflict;

        /// <summary>
        /// 是否有软约束冲突
        /// </summary>
        [ObservableProperty]
        private bool _hasSoftConflict;

        /// <summary>
        /// 是否被选中
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// 是否被高亮
        /// </summary>
        [ObservableProperty]
        private bool _isHighlighted;

        /// <summary>
        /// 是否为空单元格（未分配）
        /// </summary>
        [ObservableProperty]
        private bool _isEmpty;

        /// <summary>
        /// 行索引
        /// </summary>
        [ObservableProperty]
        private int _rowIndex;

        /// <summary>
        /// 列索引
        /// </summary>
        [ObservableProperty]
        private int _columnIndex;

        /// <summary>
        /// 冲突工具提示文本
        /// </summary>
        [ObservableProperty]
        private string _conflictTooltip = string.Empty;
    }
}
