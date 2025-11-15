using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// 手动指定视图模型 - 表示单个手动指定的UI数据
    /// </summary>
    public partial class ManualAssignmentViewModel : ObservableObject
    {
        /// <summary>
        /// 数据库ID，临时手动指定为null
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 临时唯一标识符，用于临时手动指定
        /// </summary>
        public Guid TempId { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 人员ID
        /// </summary>
        public int PersonnelId { get; set; }

        /// <summary>
        /// 人员姓名
        /// </summary>
        public string PersonnelName { get; set; } = string.Empty;

        /// <summary>
        /// 哨位ID
        /// </summary>
        public int PositionId { get; set; }

        /// <summary>
        /// 哨位名称
        /// </summary>
        public string PositionName { get; set; } = string.Empty;

        /// <summary>
        /// 时段（0-11）
        /// </summary>
        public int TimeSlot { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用
        /// </summary>
        [ObservableProperty]
        private bool _isEnabled = true;

        /// <summary>
        /// 是否为临时手动指定（true=临时，false=已保存）
        /// </summary>
        public bool IsTemporary => Id == null;

        /// <summary>
        /// 时段显示文本（例如："时段 3 (06:00-08:00)"）
        /// </summary>
        public string TimeSlotDisplay
        {
            get
            {
                var startHour = TimeSlot * 2;
                var endHour = (TimeSlot + 1) * 2;
                return $"时段 {TimeSlot} ({startHour:D2}:00-{endHour:D2}:00)";
            }
        }

        /// <summary>
        /// 是否可以编辑（仅临时手动指定可编辑）
        /// </summary>
        public bool CanEdit => IsTemporary;

        /// <summary>
        /// 是否可以删除（仅临时手动指定可删除）
        /// </summary>
        public bool CanDelete => IsTemporary;

        /// <summary>
        /// 状态标签文本
        /// </summary>
        public string StatusBadge => IsTemporary ? "临时" : "已保存";
    }
}
