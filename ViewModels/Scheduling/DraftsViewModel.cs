using AutoScheduling3.DTOs;
using AutoScheduling3.Helpers;
using AutoScheduling3.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class DraftsViewModel : ObservableObject
    {
        private readonly ISchedulingService _schedulingService;
        private readonly DialogService _dialogService;
        private readonly NavigationService _navigationService;

        [ObservableProperty]
        private ObservableCollection<ScheduleSummaryDto> _drafts = new();

        [ObservableProperty]
        private bool _isLoading;

        public IAsyncRelayCommand LoadDraftsCommand { get; }
        public IAsyncRelayCommand<int> ViewDraftCommand { get; }
        public IAsyncRelayCommand<int> ConfirmDraftCommand { get; }
        public IAsyncRelayCommand<int> DeleteDraftCommand { get; }

        public DraftsViewModel(ISchedulingService schedulingService, DialogService dialogService, NavigationService navigationService)
        {
            _schedulingService = schedulingService;
            _dialogService = dialogService;
            _navigationService = navigationService;

            LoadDraftsCommand = new AsyncRelayCommand(LoadDraftsAsync);
            ViewDraftCommand = new AsyncRelayCommand<int>(ViewDraftAsync);
            ConfirmDraftCommand = new AsyncRelayCommand<int>(ConfirmDraftAsync);
            DeleteDraftCommand = new AsyncRelayCommand<int>(DeleteDraftAsync);
        }

        private async Task LoadDraftsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                // 记录加载操作开始
                System.Diagnostics.Debug.WriteLine("开始加载草稿列表");
                
                var draftsList = await _schedulingService.GetDraftsAsync();
                Drafts = new ObservableCollection<ScheduleSummaryDto>(draftsList);
                
                // 记录加载操作成功
                System.Diagnostics.Debug.WriteLine($"草稿列表加载成功，共 {draftsList.Count} 个草稿");
            }
            catch (Exception ex)
            {
                // 记录加载失败错误
                System.Diagnostics.Debug.WriteLine($"加载草稿列表失败: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                await _dialogService.ShowErrorAsync("加载草稿列表失败", $"无法加载草稿列表：{ex.Message}\n\n请检查网络连接或稍后重试。");
                
                // 确保 UI 显示空列表而不是保留旧数据
                Drafts = new ObservableCollection<ScheduleSummaryDto>();
            }
            finally
            {
                // 确保 UI 状态正确恢复
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine("加载草稿列表操作结束，UI 状态已恢复");
            }
        }

        private async Task ViewDraftAsync(int scheduleId)
        {
            if (scheduleId > 0)
            {
                _navigationService.NavigateTo("ScheduleResult", scheduleId);
            }
        }

        private async Task ConfirmDraftAsync(int scheduleId)
        {
            if (scheduleId <= 0) return;

            // 更新确认对话框文本，添加警告信息
            var message = "确认后，该排班将保存到历史记录，无法再修改。\n\n" +
                         "⚠️ 重要提示：确认此排班后，草稿箱中的所有其他草稿将被自动清空，以避免重复应用排班。\n\n" +
                         "是否继续？";
            var confirmed = await _dialogService.ShowConfirmAsync("确认排班", message, "确认", "取消");
            if (!confirmed) return;

            IsLoading = true;
            try
            {
                // 记录确认操作开始
                System.Diagnostics.Debug.WriteLine($"用户开始确认草稿: {scheduleId}");
                
                // 调用新的方法，确认草稿并清空其他草稿
                await _schedulingService.ConfirmScheduleAndClearOthersAsync(scheduleId);
                
                // 记录确认操作成功
                System.Diagnostics.Debug.WriteLine($"草稿 {scheduleId} 确认成功");
                
                await _dialogService.ShowSuccessAsync("排班已确认，草稿箱已清空");
                await LoadDraftsAsync(); // 刷新草稿列表
            }
            catch (InvalidOperationException ex)
            {
                // 处理业务逻辑错误（如草稿不存在、验证失败等）
                System.Diagnostics.Debug.WriteLine($"确认草稿失败 - 业务逻辑错误: {ex.Message}");
                await _dialogService.ShowErrorAsync("确认失败", $"无法确认该草稿：{ex.Message}");
            }
            catch (Exception ex)
            {
                // 处理其他未预期的错误
                System.Diagnostics.Debug.WriteLine($"确认草稿失败 - 系统错误: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                await _dialogService.ShowErrorAsync("确认失败", $"系统错误：{ex.Message}\n\n请稍后重试或联系管理员。");
            }
            finally
            {
                // 确保 UI 状态正确恢复
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine($"确认草稿操作结束，UI 状态已恢复");
            }
        }

        private async Task DeleteDraftAsync(int scheduleId)
        {
            if (scheduleId <= 0) return;

            var confirmed = await _dialogService.ShowConfirmAsync("删除草稿", "确认要删除这个排班草稿吗？此操作不可恢复。", "删除", "取消");
            if (!confirmed) return;

            IsLoading = true;
            try
            {
                // 记录删除操作开始
                System.Diagnostics.Debug.WriteLine($"用户开始删除草稿: {scheduleId}");
                
                await _schedulingService.DeleteDraftAsync(scheduleId);
                
                // 记录删除操作成功
                System.Diagnostics.Debug.WriteLine($"草稿 {scheduleId} 删除成功");
                
                await _dialogService.ShowSuccessAsync("草稿已删除");
                await LoadDraftsAsync(); // 刷新草稿列表
            }
            catch (InvalidOperationException ex)
            {
                // 处理业务逻辑错误（如草稿不存在等）
                System.Diagnostics.Debug.WriteLine($"删除草稿失败 - 业务逻辑错误: {ex.Message}");
                await _dialogService.ShowErrorAsync("删除失败", $"无法删除该草稿：{ex.Message}");
            }
            catch (Exception ex)
            {
                // 处理其他未预期的错误
                System.Diagnostics.Debug.WriteLine($"删除草稿失败 - 系统错误: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                await _dialogService.ShowErrorAsync("删除失败", $"系统错误：{ex.Message}\n\n请稍后重试或联系管理员。");
            }
            finally
            {
                // 确保 UI 状态正确恢复
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine("删除草稿操作结束，UI 状态已恢复");
            }
        }
    }
}
