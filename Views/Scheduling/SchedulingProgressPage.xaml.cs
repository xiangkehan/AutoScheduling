using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using AutoScheduling3.ViewModels.Scheduling;
using AutoScheduling3.DTOs;
using System;

namespace AutoScheduling3.Views.Scheduling
{
    /// <summary>
    /// 排班进度可视化页面
    /// 实时显示排班算法执行过程、进度状态和最终结果
    /// </summary>
    public sealed partial class SchedulingProgressPage : Page
    {
        /// <summary>
        /// 视图模型
        /// </summary>
        public SchedulingProgressViewModel ViewModel { get; }

        /// <summary>
        /// 初始化排班进度页面
        /// </summary>
        public SchedulingProgressPage()
        {
            this.InitializeComponent();
            
            // 从依赖注入容器获取 ViewModel
            ViewModel = ((App)Application.Current).ServiceProvider.GetRequiredService<SchedulingProgressViewModel>();
            
            // 设置数据上下文
            this.DataContext = ViewModel;
        }

        /// <summary>
        /// 页面导航到时触发
        /// 接收 SchedulingRequestDto 参数并启动排班
        /// </summary>
        /// <param name="e">导航事件参数</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 检查导航参数
            if (e.Parameter is SchedulingRequestDto request)
            {
                // 启动排班任务
                _ = ViewModel.StartSchedulingCommand.ExecuteAsync(request);
            }
            else
            {
                // 如果没有传递有效的排班请求，显示错误并返回
                System.Diagnostics.Debug.WriteLine("[SchedulingProgressPage] No valid SchedulingRequestDto parameter received");
                
                // 可以选择显示错误消息或直接返回到创建排班页面
                ViewModel.ReturnToConfigCommand.Execute(null);
            }
        }

        /// <summary>
        /// 页面导航离开时触发
        /// 清理资源
        /// </summary>
        /// <param name="e">导航事件参数</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            
            // 如果排班仍在执行，可以选择取消或警告用户
            // 当前实现：允许后台继续执行
            System.Diagnostics.Debug.WriteLine("[SchedulingProgressPage] Navigated away from progress page");
        }
    }
}
