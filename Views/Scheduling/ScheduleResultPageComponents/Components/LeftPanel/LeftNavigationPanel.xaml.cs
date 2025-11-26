using Microsoft.UI.Xaml.Controls;
using AutoScheduling3.ViewModels.Scheduling;

namespace AutoScheduling3.Views.Scheduling.ScheduleResultPageComponents.Components.LeftPanel
{
    /// <summary>
    /// 左侧导航面板
    /// </summary>
    public sealed partial class LeftNavigationPanel : UserControl
    {
        public ScheduleResultViewModel ViewModel => DataContext as ScheduleResultViewModel;

        public LeftNavigationPanel()
        {
            this.InitializeComponent();
        }
    }
}
