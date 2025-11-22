using System.Collections.Generic;
using System.Collections.ObjectModel;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// SchedulingViewModel 的辅助方法部分
    /// 包含静态数据、辅助属性、小型工具方法
    /// </summary>
    public partial class SchedulingViewModel
    {
        #region 辅助属性

        /// <summary>
        /// 时段选项（静态列表）
        /// </summary>
        public List<TimeSlotOption> TimeSlotOptions { get; } = TimeSlotOption.GetAll();

        /// <summary>
        /// 所有手动指定（绑定到UI）
        /// </summary>
        public ObservableCollection<ManualAssignmentViewModel> AllManualAssignments 
            => _manualAssignmentManager.AllAssignments;

        #endregion
    }
}
