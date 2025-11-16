using AutoScheduling3.DTOs;
using AutoScheduling3.ViewModels.Scheduling;
using AutoScheduling3.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoScheduling3.Views.Scheduling
{
    public sealed partial class CreateSchedulingPage : Page
    {
        public SchedulingViewModel ViewModel { get; }
        private ISchedulingDraftService _draftService;
        private bool _isDraftRestored = false;

        public CreateSchedulingPage()
        {
            this.InitializeComponent();
            ViewModel = (App.Current as App).ServiceProvider.GetRequiredService<SchedulingViewModel>();
            _draftService = (App.Current as App).ServiceProvider.GetRequiredService<ISchedulingDraftService>();
            
            // Inject draft service into ViewModel
            ViewModel.InjectDraftService(_draftService);
            
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // 检查是否有草稿需要恢复
            if (await _draftService.HasDraftAsync() && !_isDraftRestored)
            {
                System.Diagnostics.Debug.WriteLine("[CreateSchedulingPage] Draft detected, asking user to restore...");
                
                // 加载草稿以获取保存时间
                var draft = await _draftService.LoadDraftAsync();
                var savedTimeText = draft?.SavedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "未知";
                
                // 询问用户是否恢复草稿
                var dialog = new ContentDialog
                {
                    Title = "恢复进度",
                    Content = $"检测到未完成的排班创建，是否恢复？\n\n上次编辑时间：{savedTimeText}",
                    PrimaryButtonText = "恢复",
                    SecondaryButtonText = "重新开始",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };
                
                var result = await dialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary)
                {
                    // 用户选择恢复草稿
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("[CreateSchedulingPage] User chose to restore draft");
                        await ViewModel.RestoreFromDraftAsync(draft);
                        _isDraftRestored = true;
                        
                        // 显示恢复成功的临时通知
                        ShowDraftRestoredNotification(savedTimeText);
                        
                        System.Diagnostics.Debug.WriteLine("[CreateSchedulingPage] Draft restored successfully");
                        return;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CreateSchedulingPage] Failed to restore draft: {ex.Message}");
                        // 恢复失败，继续正常初始化流程
                    }
                }
                else
                {
                    // 用户选择重新开始，删除草稿
                    System.Diagnostics.Debug.WriteLine("[CreateSchedulingPage] User chose to start fresh, deleting draft");
                    await _draftService.DeleteDraftAsync();
                }
            }
            
            // 原有的初始化逻辑（如果没有恢复草稿）
            if (!_isDraftRestored)
            {
                ViewModel.CancelCommand.Execute(null); // Reset state on new navigation

                if (e.Parameter is int templateId && templateId > 0)
                {
                    // A template is passed, load it.
                    _ = ViewModel.LoadTemplateCommand.ExecuteAsync(templateId);
                }
                else
                {
                    // Load initial data for a new schedule from scratch.
                    _ = ViewModel.LoadDataCommand.ExecuteAsync(null);
                }
            }
            
            // Always load constraints, this is now called from ViewModel when needed.
            // _ = ViewModel.LoadConstraintsCommand.ExecuteAsync(null);
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            
            // 保存草稿（如果有进度）
            if (ShouldSaveDraft())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[CreateSchedulingPage] Saving draft on navigation away...");
                    var draft = await ViewModel.CreateDraftAsync();
                    await _draftService.SaveDraftAsync(draft);
                    System.Diagnostics.Debug.WriteLine("[CreateSchedulingPage] Draft saved successfully");
                    
                    // 可选：显示简短通知（非阻塞）
                    // 注意：由于是导航离开，用户可能看不到此通知，所以这里只记录日志
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateSchedulingPage] Failed to save draft: {ex.Message}");
                    // 不阻塞导航，静默失败
                }
            }
            
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.IsCreatingManualAssignment))
            {
                if (ManualAssignmentDialog == null)
                    return;

                if (ViewModel.IsCreatingManualAssignment)
                {
                    // 确保对话框有 XamlRoot
                    if (ManualAssignmentDialog.XamlRoot == null)
                    {
                        ManualAssignmentDialog.XamlRoot = this.XamlRoot;
                    }
                    await ManualAssignmentDialog.ShowAsync();
                }
                else
                {
                    ManualAssignmentDialog.Hide();
                }
            }
            else if (e.PropertyName == nameof(ViewModel.IsEditingManualAssignment))
            {
                if (ManualAssignmentEditDialog == null)
                    return;

                if (ViewModel.IsEditingManualAssignment)
                {
                    // 确保对话框有 XamlRoot
                    if (ManualAssignmentEditDialog.XamlRoot == null)
                    {
                        ManualAssignmentEditDialog.XamlRoot = this.XamlRoot;
                    }
                    await ManualAssignmentEditDialog.ShowAsync();
                }
                else
                {
                    ManualAssignmentEditDialog.Hide();
                }
            }
        }

        private void AddPersonnel_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = AvailablePersonnelList?.SelectedItems.Cast<PersonnelDto>().ToList();
            if (selectedItems != null && selectedItems.Any())
            {
                foreach (var item in selectedItems)
                {
                    if (!ViewModel.SelectedPersonnels.Any(p => p.Id == item.Id))
                    {
                        ViewModel.SelectedPersonnels.Add(item);
                    }
                }
            }
        }

        private void RemovePersonnel_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = SelectedPersonnelList?.SelectedItems.Cast<PersonnelDto>().ToList();
            if (selectedItems != null && selectedItems.Any())
            {
                foreach (var item in selectedItems)
                {
                    ViewModel.SelectedPersonnels.Remove(item);
                }
            }
        }

        private void AddPosition_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = AvailablePositionsList?.SelectedItems.Cast<PositionDto>().ToList();
            if (selectedItems != null && selectedItems.Any())
            {
                foreach (var item in selectedItems)
                {
                    if (!ViewModel.SelectedPositions.Any(p => p.Id == item.Id))
                    {
                        ViewModel.SelectedPositions.Add(item);
                    }
                }
            }
        }

        private void RemovePosition_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = SelectedPositionsList?.SelectedItems.Cast<PositionDto>().ToList();
            if (selectedItems != null && selectedItems.Any())
            {
                foreach (var item in selectedItems)
                {
                    ViewModel.SelectedPositions.Remove(item);
                }
            }
        }

        /// <summary>
        /// 判断是否应该保存草稿
        /// 
        /// 保存条件：
        /// - 排班标题不为空，或
        /// - 已选择人员，或
        /// - 已选择岗位，或
        /// - 存在手动指定
        /// 
        /// 不保存条件：
        /// - 已成功创建排班（ResultSchedule存在）
        /// 
        /// 需求: 1.1, 3.1
        /// </summary>
        private bool ShouldSaveDraft()
        {
            // 如果已成功创建排班，不保存草稿
            if (ViewModel.ResultSchedule != null)
            {
                System.Diagnostics.Debug.WriteLine("[CreateSchedulingPage] ResultSchedule exists, not saving draft");
                return false;
            }

            // 检查是否有值得保存的进度
            var hasProgress = !string.IsNullOrWhiteSpace(ViewModel.ScheduleTitle) ||
                             ViewModel.SelectedPersonnels.Count > 0 ||
                             ViewModel.SelectedPositions.Count > 0 ||
                             ViewModel.AllManualAssignments.Count > 0;

            System.Diagnostics.Debug.WriteLine($"[CreateSchedulingPage] ShouldSaveDraft: {hasProgress}");
            return hasProgress;
        }

        /// <summary>
        /// 显示草稿恢复成功的临时通知
        /// 
        /// 通知会在3秒后自动消失
        /// 需求: 1.3, 1.4, 3.2
        /// </summary>
        private async void ShowDraftRestoredNotification(string savedTime)
        {
            try
            {
                // 创建一个简单的通知对话框，3秒后自动关闭
                var notificationDialog = new ContentDialog
                {
                    Title = "草稿已恢复",
                    Content = $"已从草稿恢复，上次编辑时间：{savedTime}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };

                // 创建自动关闭任务
                var autoCloseTask = Task.Run(async () =>
                {
                    await Task.Delay(3000); // 3秒后自动关闭

                    // 在UI线程上关闭对话框
                    if (this.DispatcherQueue != null)
                    {
                        this.DispatcherQueue.TryEnqueue(() =>
                        {
                            try
                            {
                                notificationDialog.Hide();
                            }
                            catch
                            {
                                // 对话框可能已经被用户关闭
                            }
                        });
                    }
                });

                await notificationDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateSchedulingPage] Failed to show notification: {ex.Message}");
                // 静默失败，不影响主流程
            }
        }
    }
}
