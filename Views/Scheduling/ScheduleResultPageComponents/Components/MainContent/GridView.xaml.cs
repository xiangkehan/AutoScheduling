using Microsoft.UI.Xaml.Controls;
using AutoScheduling3.ViewModels.Scheduling;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.MainContent
{
    /// <summary>
    /// 网格视图组件
    /// </summary>
    public sealed partial class GridView : UserControl
    {
        /// <summary>
        /// ViewModel
        /// </summary>
        public ScheduleResultViewModel ViewModel => DataContext as ScheduleResultViewModel;

        public GridView()
        {
            this.InitializeComponent();
        }
    }
}
