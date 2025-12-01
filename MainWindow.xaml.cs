using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AutoScheduling3.Views.DataManagement;
using AutoScheduling3.Helpers;
using Microsoft.Extensions.DependencyInjection;
using AutoScheduling3.Views.Scheduling;
using AutoScheduling3.Views.History;
using AutoScheduling3.Views.Settings;
using AutoScheduling3.Services.Interfaces;
using AutoScheduling3.Helpers;

namespace AutoScheduling3
{
    /// <summary>
    /// 主窗口 - 包含导航菜单和内容区域
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private NavigationService _navigationService;
        private IThemeService? _themeService;
        private DialogService _dialogService;

        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            InitializeNavigation();
            InitializeTheme();
        }

        /// <summary>
        /// 初始化服务
        /// </summary>
        private void InitializeServices()
        {
            _themeService = ((App)Application.Current).ServiceProvider.GetRequiredService<IThemeService>();
            _dialogService = ((App)Application.Current).ServiceProvider.GetRequiredService<DialogService>();
            
            // 初始化动画辅助类
            AnimationHelper.Initialize(_themeService);
        }

        /// <summary>
        /// 初始化主题
        /// </summary>
        private async void InitializeTheme()
        {
            if (_themeService != null)
            {
                await _themeService.InitializeAsync();
                
                // 订阅主题变更事件
                _themeService.ThemeChanged += OnThemeChanged;
            }
        }

        /// <summary>
        /// 主题变更事件处理
        /// </summary>
        private void OnThemeChanged(object? sender, ElementTheme theme)
        {
            // 应用主题到窗口内容
            if (Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }
        }

        /// <summary>
        /// 初始化导航服务
        /// </summary>
        private void InitializeNavigation()
        {
            _navigationService = ((App)Application.Current).ServiceProvider.GetRequiredService<NavigationService>();
            _navigationService.Initialize(ContentFrame);

            // 数据管理页面注册
            _navigationService.RegisterPage("Personnel", typeof(PersonnelPage));
            _navigationService.RegisterPage("Position", typeof(PositionPage));
            _navigationService.RegisterPage("Skill", typeof(SkillPage));

            // 排班流程相关页面注册
            _navigationService.RegisterPage("CreateScheduling", typeof(CreateSchedulingPage));
            _navigationService.RegisterPage("SchedulingProgress", typeof(SchedulingProgressPage));
            _navigationService.RegisterPage("TemplateManage", typeof(TemplatePage));
            _navigationService.RegisterPage("ScheduleResult", typeof(ScheduleResultPage));
            _navigationService.RegisterPage("ScheduleGridFullScreen", typeof(ScheduleGridFullScreenView));
            _navigationService.RegisterPage("Drafts", typeof(DraftsPage));

            // 历史页面注册
            _navigationService.RegisterPage("History", typeof(HistoryPage));
            _navigationService.RegisterPage("Compare", typeof(ComparePage));
            // 注意：历史详情使用 ScheduleResult 页面展示，无需单独注册

            // 设置页面注册
            _navigationService.RegisterPage("Settings", typeof(SettingsPage));
            _navigationService.RegisterPage("TestDataGenerator", typeof(TestDataGeneratorPage));

            // 默认导航
            _navigationService.NavigateTo("Personnel");
        }

        /// <summary>
        /// 处理导航项点击事件
        /// </summary>
        private async void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag))
                {
                    // 应用导航动画
                    if (_themeService?.IsAnimationEnabled == true)
                    {
                        AnimationHelper.AnimateButtonPress(item);
                    }
                    
                    try
                    {
                        _navigationService.NavigateTo(tag);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            await _dialogService.ShowErrorAsync($"导航到 '{tag}' 失败。", ex);
                        }
                        catch (Exception dialogEx)
                        {
                            // 如果对话框服务也失败了，记录到调试输出
                            System.Diagnostics.Debug.WriteLine($"Failed to show error dialog: {dialogEx}");
                        }
                    }
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

        /// <summary>
        /// 处理窗口大小变化事件 - 响应式布局
        /// </summary>
        private void NavigationView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var breakpoint = ResponsiveHelper.GetCurrentBreakpoint(e.NewSize.Width);
            var paneDisplayMode = ResponsiveHelper.GetNavigationPaneDisplayMode(breakpoint);
            
            NavigationViewControl.PaneDisplayMode = paneDisplayMode;
        }
    }
}
