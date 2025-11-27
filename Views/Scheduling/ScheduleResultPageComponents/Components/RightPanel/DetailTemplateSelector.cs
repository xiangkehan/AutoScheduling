using AutoScheduling3.DTOs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.RightPanel
{
    /// <summary>
    /// 详情模板选择器
    /// 根据选中项类型选择不同的详情模板
    /// </summary>
    public class DetailTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// 冲突详情模板
        /// </summary>
        public DataTemplate ConflictDetailTemplate { get; set; }

        /// <summary>
        /// 班次编辑模板
        /// </summary>
        public DataTemplate ShiftEditTemplate { get; set; }

        /// <summary>
        /// 人员详情模板
        /// </summary>
        public DataTemplate PersonnelDetailTemplate { get; set; }

        /// <summary>
        /// 哨位详情模板
        /// </summary>
        public DataTemplate PositionDetailTemplate { get; set; }

        /// <summary>
        /// 根据项目类型选择对应的模板
        /// </summary>
        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is ConflictDto)
            {
                return ConflictDetailTemplate;
            }
            else if (item is ScheduleGridCell)
            {
                return ShiftEditTemplate;
            }
            else if (item is PersonnelDto)
            {
                return PersonnelDetailTemplate;
            }
            else if (item is PositionDto)
            {
                return PositionDetailTemplate;
            }

            return base.SelectTemplateCore(item);
        }
    }
}