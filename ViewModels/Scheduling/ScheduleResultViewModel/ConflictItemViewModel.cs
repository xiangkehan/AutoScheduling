using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// 冲突项ViewModel
    /// </summary>
    public partial class ConflictItemViewModel : ObservableObject
    {
        /// <summary>
        /// 冲突ID
        /// </summary>
        [ObservableProperty]
        private string _id = string.Empty;

        /// <summary>
        /// 冲突类型（Hard/Soft）
        /// </summary>
        [ObservableProperty]
        private ConflictType _type;

        /// <summary>
        /// 冲突分类（技能不匹配、连续工作等）
        /// </summary>
        [ObservableProperty]
        private string _category = string.Empty;

        /// <summary>
        /// 人员姓名
        /// </summary>
        [ObservableProperty]
        private string _personnelName = string.Empty;

        /// <summary>
        /// 人员ID
        /// </summary>
        [ObservableProperty]
        private int _personnelId;

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
        /// 日期时间
        /// </summary>
        [ObservableProperty]
        private DateTime _dateTime;

        /// <summary>
        /// 时段索引
        /// </summary>
        [ObservableProperty]
        private int _timeSlot;

        /// <summary>
        /// 冲突描述
        /// </summary>
        [ObservableProperty]
        private string _description = string.Empty;

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
        /// 严重程度（用于排序）
        /// </summary>
        [ObservableProperty]
        private int _severity;
    }

    /// <summary>
    /// 冲突类型枚举
    /// </summary>
    public enum ConflictType
    {
        /// <summary>
        /// 硬约束冲突
        /// </summary>
        Hard,

        /// <summary>
        /// 软约束警告
        /// </summary>
        Soft
    }
}
