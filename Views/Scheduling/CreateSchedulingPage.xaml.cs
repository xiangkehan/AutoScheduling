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
            
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // 检查是否有草稿需要恢复
            if (await _draftService.HasDraftAsync() && !_isDraftRestored)
            {
                System.Diagnostics.Debug.WriteLine("[CreateSchedulingPage] Draft detected, asking user to restore...");
                
                SchedulingDraftDto draft = null;
                string savedTimeText = "未知";
                
                try
                {
                    // 尝试加载草稿以获取保存时间
                    draft = await _draftService.LoadDraftAsync();
                    savedTimeText = draft?.SavedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "未知";
                }
                catch (InvalidOperationException ex)
                {
                    // 草稿数据损坏，显示错误并删除草稿
                    System.Diagnostics.Debug.WriteLine($"[CreateSchedulingPage] Draft is corrupted: {ex.Message}");
                    
                    var errorDialog = new ContentDialog
                    {
                        Title = "草稿加载失败",
                        Content = $"无法恢复之前的进度，草稿数据已损坏。\n\n错误详情：{ex.Message}\n\n将显示空白表单。",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    
                    await errorDialog.ShowAsync();
                    
                    // 草稿已在LoadDraftAsync中删除，继续正常初始化
                    draft = null;
                }
                catch (Exception ex)
                {
                    // 其他未预期的错误
                    System.Diagnostics.Debug.WriteLine($"[CreateSchedulingPage] Unexpected error loading draft: {ex.Message}");
                    
                    var errorDialog = new ContentDialog
                    {
                        Title = "草稿加载失败",
                        Content = $"加载草稿时发生错误。\n\n错误详情：{ex.Message}\n\n将显示空白表单。",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    
                    await errorDialog.ShowAsync();
                    
                    // 尝试删除可能损坏的草稿
                    try
                    {
                        await _draftService.DeleteDraftAsync();
                    }
                    catch
                    {
                        // 静默失败
                    }
                    
                    draft = null;
                }
                
                // 如果成功加载草稿，询问用户是否恢复
                if (draft != null)
                {
                    // 在WinUI 3中，需要等待XamlRoot可用后再显示对话框
                    var result = await ShowDialogWithXamlRoot(async () =>
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "恢复进度",
                            Content = $"检测到未完成的排班创建，是否恢复？\n\n上次编辑时间：{savedTimeText}",
                            PrimaryButtonText = "恢复",
                            SecondaryButtonText = "重新开始",
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.XamlRoot
                        };

                        return await dialog.ShowAsync();
                    });
                    
                    if (result == ContentDialogResult.Primary)
                    {
                        // 用户选择恢复草稿
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("[CreateSchedulingPage] User chose to restore draft");
                            await ViewModel.RestoreFromDraftAsync();
                            _isDraftRestored = true;
                            
                            // 显示恢复成功的临时通知
                            ShowDraftRestoredNotification(savedTimeText);
                            
                            System.Diagnostics.Debug.WriteLine("[CreateSchedulingPage] Draft restored successfully");
                            return;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CreateSchedulingPage] Failed to restore draft: {ex.Message}");
                            
                            // 显示恢复失败的错误消息
                            var restoreErrorDialog = new ContentDialog
                            {
                                Title = "恢复失败",
                                Content = $"无法恢复草稿数据。\n\n错误详情：{ex.Message}\n\n将显示空白表单。",
                                CloseButtonText = "确定",
                                XamlRoot = this.XamlRoot
                            };
                            
                            await restoreErrorDialog.ShowAsync();
                            
                            // 删除无法恢复的草稿
                            try
                            {
                                await _draftService.DeleteDraftAsync();
                            }
                            catch
                            {
                                // 静默失败
                            }
                        }
                    }
                    else
                    {
                        // 用户选择重新开始，删除草稿
                        System.Diagnostics.Debug.WriteLine("[CreateSchedulingPage] User chose to start fresh, deleting draft");
                        await _draftService.DeleteDraftAsync();
                    }
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
                    await ViewModel.CreateDraftAsync();
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
                    // 使用安全方法显示对话框
                    if (ManualAssignmentDialog.XamlRoot == null && this.XamlRoot != null)
                    {
                        ManualAssignmentDialog.XamlRoot = this.XamlRoot;
                    }

                    // 如果XamlRoot仍不可用，使用ShowDialogWithXamlRoot
                    if (ManualAssignmentDialog.XamlRoot == null)
                    {
                        await ShowDialogWithXamlRoot(async () =>
                        {
                            if (ManualAssignmentDialog.XamlRoot == null && this.XamlRoot != null)
                            {
                                ManualAssignmentDialog.XamlRoot = this.XamlRoot;
                            }
                            return await ManualAssignmentDialog.ShowAsync();
                        });
                    }
                    else
                    {
                        await ManualAssignmentDialog.ShowAsync();
                    }
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
                    // 使用安全方法显示对话框
                    if (ManualAssignmentEditDialog.XamlRoot == null && this.XamlRoot != null)
                    {
                        ManualAssignmentEditDialog.XamlRoot = this.XamlRoot;
                    }

                    // 如果XamlRoot仍不可用，使用ShowDialogWithXamlRoot
                    if (ManualAssignmentEditDialog.XamlRoot == null)
                    {
                        await ShowDialogWithXamlRoot(async () =>
                        {
                            if (ManualAssignmentEditDialog.XamlRoot == null && this.XamlRoot != null)
                            {
                                ManualAssignmentEditDialog.XamlRoot = this.XamlRoot;
                            }
                            return await ManualAssignmentEditDialog.ShowAsync();
                        });
                    }
                    else
                    {
                        await ManualAssignmentEditDialog.ShowAsync();
                    }
                }
                else
                {
                    ManualAssignmentEditDialog.Hide();
                }
            }
            else if (e.PropertyName == nameof(ViewModel.IsAddingPersonnelToPosition))
            {
                if (AddPersonnelToPositionDialog == null)
                    return;

                if (ViewModel.IsAddingPersonnelToPosition)
                {
                    // 使用安全方法显示对话框
                    if (AddPersonnelToPositionDialog.XamlRoot == null && this.XamlRoot != null)
                    {
                        AddPersonnelToPositionDialog.XamlRoot = this.XamlRoot;
                    }

                    // 如果XamlRoot仍不可用，使用ShowDialogWithXamlRoot
                    if (AddPersonnelToPositionDialog.XamlRoot == null)
                    {
                        await ShowDialogWithXamlRoot(async () =>
                        {
                            if (AddPersonnelToPositionDialog.XamlRoot == null && this.XamlRoot != null)
                            {
                                AddPersonnelToPositionDialog.XamlRoot = this.XamlRoot;
                            }
                            return await AddPersonnelToPositionDialog.ShowAsync();
                        });
                    }
                    else
                    {
                        await AddPersonnelToPositionDialog.ShowAsync();
                    }
                }
                else
                {
                    AddPersonnelToPositionDialog.Hide();
                }
            }
            else if (e.PropertyName == nameof(ViewModel.IsManualAddingPersonnel))
            {
                if (ManualAddPersonnelDialog == null)
                    return;

                if (ViewModel.IsManualAddingPersonnel)
                {
                    // 使用安全方法显示对话框
                    if (ManualAddPersonnelDialog.XamlRoot == null && this.XamlRoot != null)
                    {
                        ManualAddPersonnelDialog.XamlRoot = this.XamlRoot;
                    }

                    // 如果XamlRoot仍不可用，使用ShowDialogWithXamlRoot
                    if (ManualAddPersonnelDialog.XamlRoot == null)
                    {
                        await ShowDialogWithXamlRoot(async () =>
                        {
                            if (ManualAddPersonnelDialog.XamlRoot == null && this.XamlRoot != null)
                            {
                                ManualAddPersonnelDialog.XamlRoot = this.XamlRoot;
                            }
                            return await ManualAddPersonnelDialog.ShowAsync();
                        });
                    }
                    else
                    {
                        await ManualAddPersonnelDialog.ShowAsync();
                    }
                }
                else
                {
                    ManualAddPersonnelDialog.Hide();
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

        private void AddPersonnelListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView listView)
            {
                ViewModel.SelectedPersonnelIdsForPosition.Clear();
                foreach (var item in listView.SelectedItems.Cast<PersonnelDto>())
                {
                    ViewModel.SelectedPersonnelIdsForPosition.Add(item.Id);
                }
            }
        }

        private void ManualAddPersonnelListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView listView)
            {
                ViewModel.SelectedPersonnelIdsForManualAdd.Clear();
                foreach (var item in listView.SelectedItems.Cast<PersonnelDto>())
                {
                    ViewModel.SelectedPersonnelIdsForManualAdd.Add(item.Id);
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
                // 使用安全方法显示对话框
                await ShowDialogWithXamlRoot(async () =>
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
                    Task autoCloseTask = Task.Run(async () =>
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

                    return await notificationDialog.ShowAsync();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateSchedulingPage] Failed to show notification: {ex.Message}");
                // 静默失败，不影响主流程
            }
        }

        /// <summary>
        /// 安全显示对话框的辅助方法
        /// 在WinUI 3中，确保XamlRoot可用后再显示对话框
        /// </summary>
        /// <typeparam name="T">对话框操作返回的类型</typeparam>
        /// <param name="dialogAction">要执行的对话框操作</param>
        /// <returns>对话框操作的返回值</returns>
        private async Task<T> ShowDialogWithXamlRoot<T>(Func<Task<T>> dialogAction)
        {
            // 如果XamlRoot已经可用，直接执行
            if (this.XamlRoot != null)
            {
                return await dialogAction();
            }

            // 否则，使用DispatcherQueue等待XamlRoot可用
            var tcs = new TaskCompletionSource<T>();

            this.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    // 等待XamlRoot可用（最多等待5秒）
                    var maxWaitTime = 5000; // 5秒
                    var waitInterval = 50; // 每50ms检查一次
                    var waitedTime = 0;

                    while (this.XamlRoot == null && waitedTime < maxWaitTime)
                    {
                        await Task.Delay(waitInterval);
                        waitedTime += waitInterval;
                    }

                    // 如果XamlRoot可用，执行对话框操作
                    if (this.XamlRoot != null)
                    {
                        var result = await dialogAction();
                        tcs.SetResult(result);
                    }
                    else
                    {
                        tcs.SetException(new InvalidOperationException("XamlRoot仍然不可用"));
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return await tcs.Task;
        }
    }
}
