using AutoScheduling3.DTOs;
using AutoScheduling3.ViewModels.Scheduling;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.MainContent
{
    /// <summary>
    /// 视图模式模板选择器，根据当前视图模式选择对应的DataTemplate
    /// </summary>
    public class ViewModeTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// 网格视图模板
        /// </summary>
        public DataTemplate GridViewTemplate { get; set; }

        /// <summary>
        /// 列表视图模板
        /// </summary>
        public DataTemplate ListViewTemplate { get; set; }

        /// <summary>
        /// 按人员视图模板
        /// </summary>
        public DataTemplate PersonnelViewTemplate { get; set; }

        /// <summary>
        /// 按哨位视图模板
        /// </summary>
        public DataTemplate PositionViewTemplate { get; set; }

        /// <summary>
        /// 根据视图模式选择对应的模板
        /// </summary>
        /// <param name="item">绑定的数据项</param>
        /// <param name="container">容器控件</param>
        /// <returns>选择的DataTemplate</returns>
        protected override DataTemplate SelectTemplateCore(object item)
        {
            // 这个方法在没有容器的情况下调用，返回默认模板
            return GridViewTemplate;
        }

        /// <summary>
        /// 根据视图模式选择对应的模板
        /// </summary>
        /// <param name="item">绑定的数据项</param>
        /// <param name="container">容器控件</param>
        /// <returns>选择的DataTemplate</returns>
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            // 从容器中获取MainContentArea
            var mainContentArea = container as MainContentArea;
            if (mainContentArea == null)
            {
                // 如果不是MainContentArea，尝试从视觉树中查找
                mainContentArea = FindParent<MainContentArea>(container);
                if (mainContentArea == null) return base.SelectTemplateCore(item, container);
            }

            // 从MainContentArea获取ViewModel
            var viewModel = mainContentArea.DataContext as ScheduleResultViewModel;
            if (viewModel == null) return base.SelectTemplateCore(item, container);

            // 根据视图模式返回对应的模板
            return viewModel.CurrentViewMode switch
            {
                ViewMode.Grid => GridViewTemplate,
                ViewMode.List => ListViewTemplate,
                ViewMode.ByPersonnel => PersonnelViewTemplate,
                ViewMode.ByPosition => PositionViewTemplate,
                _ => base.SelectTemplateCore(item, container)
            };
        }

        /// <summary>
        /// 从视觉树中查找父级控件
        /// </summary>
        /// <typeparam name="T">父级控件类型</typeparam>
        /// <param name="dependencyObject">当前控件</param>
        /// <returns>找到的父级控件，找不到则返回null</returns>
        private T FindParent<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(dependencyObject);
            if (parent == null) return null;

            var parentT = parent as T;
            return parentT ?? FindParent<T>(parent);
        }
    }
}