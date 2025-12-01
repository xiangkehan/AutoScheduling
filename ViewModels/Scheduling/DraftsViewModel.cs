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
                var draftsList = await _schedulingService.GetDraftsAsync();
                Drafts = new ObservableCollection<ScheduleSummaryDto>(draftsList);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("加载草稿列表失败", $"无法加载草稿列表：{ex.Message}\n\n请检查网络连接或稍后重试。");
                Drafts = new ObservableCollection<ScheduleSummaryDto>();
            }
            finally
            {
                IsLoading = false;
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
                await _schedulingService.ConfirmScheduleAndClearOthersAsync(scheduleId);
                await _dialogService.ShowSuccessAsync("排班已确认，草稿箱已清空");
                await LoadDraftsAsync(); // 刷新草稿列表
            }
            catch (InvalidOperationException ex)
            {
                await _dialogService.ShowErrorAsync("确认失败", $"无法确认该草稿：{ex.Message}");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("确认失败", $"系统错误：{ex.Message}\n\n请稍后重试或联系管理员。");
            }
            finally
            {
                IsLoading = false;
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
                await _schedulingService.DeleteDraftAsync(scheduleId);
                await _dialogService.ShowSuccessAsync("草稿已删除");
                await LoadDraftsAsync(); // 刷新草稿列表
            }
            catch (InvalidOperationException ex)
            {
                await _dialogService.ShowErrorAsync("删除失败", $"无法删除该草稿：{ex.Message}");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("删除失败", $"系统错误：{ex.Message}\n\n请稍后重试或联系管理员。");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
