using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AutoScheduling3.Views.DataManagement;
using AutoScheduling3.Helpers;
using Microsoft.Extensions.DependencyInjection;
using AutoScheduling3.Views.Scheduling;

namespace AutoScheduling3
{
    /// <summary>
    /// 主窗口 - 包含导航菜单和内容区域
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private NavigationService _navigationService;

        public MainWindow()
        {
            InitializeComponent();
            InitializeNavigation();
        }

        /// <summary>
        /// 初始化导航服务
        /// </summary>
        private void InitializeNavigation()
        {
            _navigationService = App.Services.GetRequiredService<NavigationService>();
            _navigationService.Initialize(ContentFrame);

            // 注册页面
            _navigationService.RegisterPage("Personnel", typeof(PersonnelPage));
            _navigationService.RegisterPage("Position", typeof(PositionPage));
            _navigationService.RegisterPage("Skill", typeof(SkillPage));
            _navigationService.RegisterPage("Scheduling", typeof(CreateSchedulingPage)); // 假设创建是主入口
            _navigationService.RegisterPage("SchedulingCreate", typeof(CreateSchedulingPage));
            _navigationService.RegisterPage("SchedulingResult", typeof(ScheduleResultPage));
            _navigationService.RegisterPage("SchedulingTemplates", typeof(TemplatePage));
            _navigationService.RegisterPage("SchedulingDrafts", typeof(DraftsPage));
            // 更多页面可以后续添加

            // 默认导航到人员管理页面
            _navigationService.Navigate("Personnel");
        }

        /// <summary>
        /// 处理导航项点击事件
        /// </summary>
        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag))
                {
                    _navigationService.Navigate(tag);
                }
            }
        }

        /// <summary>
        /// 处理后退按钮事件
        /// </summary>
        private void NavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (_navigationService.CanGoBack)
            {
                _navigationService.GoBack();
            }
        }
    }
}
