using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.Views.Scheduling
{
    /// <summary>
    /// 排班表格全屏视图页面
    /// </summary>
    public sealed partial class ScheduleGridFullScreenView : Page
    {
        /// <summary>
        /// Grid View 数据
        /// </summary>
        public ScheduleGridData? GridData { get; set; }

        /// <summary>
        /// Position View 数据
        /// </summary>
        public PositionScheduleData? PositionScheduleData { get; set; }

        /// <summary>
        /// 视图模式
        /// </summary>
        public ViewMode ViewMode { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; } = "全屏视图";

        public ScheduleGridFullScreenView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// 页面导航到此页面时触发
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 从导航参数中获取数据
            if (e.Parameter is FullScreenViewParameter parameter)
            {
                ViewMode = parameter.ViewMode;
                GridData = parameter.GridData;
                PositionScheduleData = parameter.PositionScheduleData;
                Title = parameter.Title;

                // 根据视图模式显示对应的控件
                switch (ViewMode)
                {
                    case ViewMode.Grid:
                        GridViewControl.Visibility = Visibility.Visible;
                        PositionViewControl.Visibility = Visibility.Collapsed;
                        break;

                    case ViewMode.ByPosition:
                        GridViewControl.Visibility = Visibility.Collapsed;
                        PositionViewControl.Visibility = Visibility.Visible;
                        break;

                    default:
                        GridViewControl.Visibility = Visibility.Visible;
                        PositionViewControl.Visibility = Visibility.Collapsed;
                        break;
                }

                // 更新标题
                TitleTextBlock.Text = Title;
            }

            // 设置焦点到关闭按钮，以便 Esc 键可以工作
            CloseButton.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 导航回上一页
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}
