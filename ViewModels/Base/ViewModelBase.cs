using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;

namespace AutoScheduling3.ViewModels.Base
{
    /// <summary>
    /// ViewModel 基类 - 提供通用的属性变更通知功能
    /// </summary>
    public abstract partial class ViewModelBase : ObservableObject
    {
        private bool _isBusy;
        private string _busyMessage = string.Empty;
        private string _errorMessage = string.Empty;

        /// <summary>
        /// 是否正在执行操作
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// 繁忙状态消息
        /// </summary>
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// 执行异步操作的辅助方法
        /// </summary>
        /// <param name="operation">要执行的异步操作</param>
        /// <param name="busyMessage">繁忙状态消息</param>
        /// <param name="showGlobalLoading">是否显示全局加载指示器（默认为 true）</param>
        protected async Task ExecuteAsync(Func<Task> operation, string? busyMessage = null, bool showGlobalLoading = true)
        {
            if (showGlobalLoading && IsBusy)
                return;

            if (showGlobalLoading)
            {
                IsBusy = true;
                BusyMessage = busyMessage ?? "正在加载...";
            }
            
            ErrorMessage = string.Empty;

            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                OnError(ex);
            }
            finally
            {
                if (showGlobalLoading)
                {
                    IsBusy = false;
                    BusyMessage = string.Empty;
                }
            }
        }

        /// <summary>
        /// 执行带返回值的异步操作
        /// </summary>
        /// <param name="operation">要执行的异步操作</param>
        /// <param name="busyMessage">繁忙状态消息</param>
        /// <param name="showGlobalLoading">是否显示全局加载指示器（默认为 true）</param>
        protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? busyMessage = null, bool showGlobalLoading = true)
        {
            if (showGlobalLoading && IsBusy)
                return default;

            if (showGlobalLoading)
            {
                IsBusy = true;
                BusyMessage = busyMessage ?? "正在加载...";
            }
            
            ErrorMessage = string.Empty;

            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                OnError(ex);
                return default;
            }
            finally
            {
                if (showGlobalLoading)
                {
                    IsBusy = false;
                    BusyMessage = string.Empty;
                }
            }
        }

        /// <summary>
        /// 错误处理方法（子类可重写）
        /// </summary>
        protected virtual void OnError(Exception exception)
        {
            // 默认不做任何处理，子类可以重写此方法来处理错误
            // 例如：记录日志、显示错误对话框等
        }

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isLoaded;

        [ObservableProperty]
        private bool _isEmpty;
    }
}
