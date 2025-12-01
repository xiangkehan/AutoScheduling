using AutoScheduling3.DTOs;
using AutoScheduling3.Helpers;
using AutoScheduling3.Services;
using AutoScheduling3.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class DraftsViewModel : ObservableObject
    {
        private readonly ISchedulingService _schedulingService;
        private readonly DialogService _dialogService;
        private readonly NavigationService _navigationService;
        private readonly PaginatedDraftLoader _paginatedLoader;

        [ObservableProperty]
        private ObservableCollection<ScheduleSummaryDto> _drafts = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private int _currentPageIndex;

        [ObservableProperty]
        private int _totalPages;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private bool _hasPreviousPage;

        [ObservableProperty]
        private bool _hasNextPage;

        [ObservableProperty]
        private SchedulingMode? _filterMode;

        [ObservableProperty]
        private bool _filterResumableOnly;

        [ObservableProperty]
        private bool _showFilters;

        public IAsyncRelayCommand LoadDraftsCommand { get; }
        public IAsyncRelayCommand<int> ViewDraftCommand { get; }
        public IAsyncRelayCommand<int> ConfirmDraftCommand { get; }
        public IAsyncRelayCommand<int> DeleteDraftCommand { get; }
        public IAsyncRelayCommand<int> ResumeDraftCommand { get; }
        public IAsyncRelayCommand PreviousPageCommand { get; }
        public IAsyncRelayCommand NextPageCommand { get; }
        public IRelayCommand ToggleFiltersCommand { get; }
        public IAsyncRelayCommand ApplyFiltersCommand { get; }
        public IAsyncRelayCommand ClearFiltersCommand { get; }

        public DraftsViewModel(
            ISchedulingService schedulingService, 
            DialogService dialogService, 
            NavigationService navigationService,
            PaginatedDraftLoader paginatedLoader)
        {
            _schedulingService = schedulingService;
            _dialogService = dialogService;
            _navigationService = navigationService;
            _paginatedLoader = paginatedLoader;

            LoadDraftsCommand = new AsyncRelayCommand(LoadDraftsAsync);
            ViewDraftCommand = new AsyncRelayCommand<int>(ViewDraftAsync);
            ConfirmDraftCommand = new AsyncRelayCommand<int>(ConfirmDraftAsync);
            DeleteDraftCommand = new AsyncRelayCommand<int>(DeleteDraftAsync);
            ResumeDraftCommand = new AsyncRelayCommand<int>(ResumeDraftAsync);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => HasPreviousPage);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
            ToggleFiltersCommand = new RelayCommand(ToggleFilters);
            ApplyFiltersCommand = new AsyncRelayCommand(ApplyFiltersAsync);
            ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync);
        }

        private async Task LoadDraftsAsync()
        {
            await LoadPageAsync(CurrentPageIndex);
        }

        private async Task LoadPageAsync(int pageIndex)
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                // 使用分页加载器获取数据
                var pagedResult = await _paginatedLoader.GetDraftsPagedAsync(
                    pageIndex,
                    pageSize: 20,
                    filterMode: FilterMode,
                    filterResumableOnly: FilterResumableOnly);

                // 更新数据
                Drafts = new ObservableCollection<ScheduleSummaryDto>(pagedResult.Items);
                CurrentPageIndex = pagedResult.PageIndex;
                TotalPages = pagedResult.TotalPages;
                TotalCount = pagedResult.TotalCount;
                HasPreviousPage = pagedResult.HasPreviousPage;
                HasNextPage = pagedResult.HasNextPage;

                // 更新命令状态
                PreviousPageCommand.NotifyCanExecuteChanged();
                NextPageCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("加载草稿列表失败", $"无法加载草稿列表：{ex.Message}\n\n请检查网络连接或稍后重试。");
                Drafts = new ObservableCollection<ScheduleSummaryDto>();
                TotalCount = 0;
                TotalPages = 0;
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

        private async Task ResumeDraftAsync(int scheduleId)
        {
            if (scheduleId <= 0) return;

            // 导航到排班创建页面，并传递草稿ID以便恢复
            _navigationService.NavigateTo("CreateScheduling", new { draftId = scheduleId, isResume = true });
        }

        private async Task PreviousPageAsync()
        {
            if (HasPreviousPage)
            {
                await LoadPageAsync(CurrentPageIndex - 1);
            }
        }

        private async Task NextPageAsync()
        {
            if (HasNextPage)
            {
                await LoadPageAsync(CurrentPageIndex + 1);
            }
        }

        private void ToggleFilters()
        {
            ShowFilters = !ShowFilters;
        }

        private async Task ApplyFiltersAsync()
        {
            // 应用过滤后重新加载第一页
            await LoadPageAsync(0);
        }

        private async Task ClearFiltersAsync()
        {
            FilterMode = null;
            FilterResumableOnly = false;
            await LoadPageAsync(0);
        }
    }
}
