using System;

namespace AutoScheduling3.DTOs
{
    /// <summary>
    /// 搜索结果项
    /// </summary>
    public class SearchResultItem
    {
        /// <summary>
        /// 班次数据
        /// </summary>
        public ShiftDto Shift { get; set; }

        /// <summary>
        /// 班次 ID
        /// </summary>
        public int ShiftId => Shift.Id;

        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date => Shift.StartTime.Date;

        /// <summary>
        /// 星期
        /// </summary>
        public string DayOfWeek => Shift.StartTime.ToString("dddd");

        /// <summary>
        /// 时段名称
        /// </summary>
        public string PeriodName => GetPeriodName(Shift.PeriodIndex);

        /// <summary>
        /// 哨位名称
        /// </summary>
        public string PositionName => Shift.PositionName;

        /// <summary>
        /// 人员姓名
        /// </summary>
        public string PersonnelName => Shift.PersonnelName;

        /// <summary>
        /// 是否为夜哨
        /// </summary>
        public bool IsNightShift => Shift.PeriodIndex is 11 or 0 or 1 or 2;

        /// <summary>
        /// 是否为手动分配
        /// </summary>
        public bool IsManualAssignment => Shift.IsManualAssignment;

        /// <summary>
        /// 是否存在冲突
        /// </summary>
        public bool HasConflict { get; set; }

        /// <summary>
        /// 在当前视图中的行索引
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// 在当前视图中的列索引
        /// </summary>
        public int ColumnIndex { get; set; }

        /// <summary>
        /// 显示文本
        /// </summary>
        public string DisplayText => $"{Date:yyyy-MM-dd} {DayOfWeek} {PeriodName} - {PositionName} - {PersonnelName}";

        public SearchResultItem(ShiftDto shift)
        {
            Shift = shift ?? throw new ArgumentNullException(nameof(shift));
        }

        /// <summary>
        /// 获取时段名称
        /// </summary>
        private string GetPeriodName(int periodIndex)
        {
            return periodIndex switch
            {
                0 => "00:00-02:00",
                1 => "02:00-04:00",
                2 => "04:00-06:00",
                3 => "06:00-08:00",
                4 => "08:00-10:00",
                5 => "10:00-12:00",
                6 => "12:00-14:00",
                7 => "14:00-16:00",
                8 => "16:00-18:00",
                9 => "18:00-20:00",
                10 => "20:00-22:00",
                11 => "22:00-00:00",
                _ => "未知"
            };
        }
    }
}
