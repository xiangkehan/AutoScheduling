using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// SchedulingViewModel 的哨位人员管理部分
    /// 包含哨位人员的添加、移除、保存、手动添加参与人员等逻辑
    /// </summary>
    public partial class SchedulingViewModel
    {
        #region 为哨位添加人员方法

        /// <summary>
        /// 开始为哨位添加人员
        /// </summary>
        /// <param name="position">要添加人员的哨位</param>
        private void StartAddPersonnelToPosition(PositionDto? position)
        {
            try
            {
                if (position == null)
                {
                    System.Diagnostics.Debug.WriteLine("错误: StartAddPersonnelToPosition - position is null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"=== 开始为哨位添加人员 ===");
                System.Diagnostics.Debug.WriteLine($"哨位: {position.Name} (ID: {position.Id})");

                // 设置当前正在编辑的哨位
                CurrentEditingPosition = position;

                // 从AvailablePersonnels中筛选可添加的人员
                // 可以添加所有可用人员
                if (AvailablePersonnels == null)
                {
                    System.Diagnostics.Debug.WriteLine("错误: AvailablePersonnels为null");
                    _ = _dialogService.ShowErrorAsync("无法加载可用人员列表");
                    return;
                }

                var availableToAdd = AvailablePersonnels.ToList();

                System.Diagnostics.Debug.WriteLine($"可添加的人员数量: {availableToAdd.Count}");

                // 设置可添加到当前哨位的人员列表
                AvailablePersonnelForPosition = new ObservableCollection<PersonnelDto>(availableToAdd);

                // 设置标志为true，显示添加人员对话框
                IsAddingPersonnelToPosition = true;

                System.Diagnostics.Debug.WriteLine("添加人员对话框已打开");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 开始为哨位添加人员失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                _ = _dialogService.ShowErrorAsync("无法打开添加人员对话框", ex);
            }
        }

        /// <summary>
        /// 提交添加人员到哨位
        /// </summary>
        private async Task SubmitAddPersonnelToPositionAsync()
        {
            try
            {
                if (CurrentEditingPosition == null)
                {
                    System.Diagnostics.Debug.WriteLine("错误: SubmitAddPersonnelToPositionAsync - CurrentEditingPosition is null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"=== 提交添加人员到哨位 ===");
                System.Diagnostics.Debug.WriteLine($"哨位: {CurrentEditingPosition.Name} (ID: {CurrentEditingPosition.Id})");

                // 验证选择的人员
                if (SelectedPersonnelIdsForPosition.Count == 0)
                {
                    await _dialogService.ShowWarningAsync("请至少选择一名人员");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"选择的人员数量: {SelectedPersonnelIdsForPosition.Count}");

                // 调用_positionPersonnelManager.AddPersonnelTemporarily添加人员
                foreach (var personnelId in SelectedPersonnelIdsForPosition)
                {
                    var personnel = AvailablePersonnelForPosition.FirstOrDefault(p => p.Id == personnelId);
                    if (personnel != null)
                    {
                        _positionPersonnelManager.AddPersonnelTemporarily(CurrentEditingPosition.Id, personnelId);
                        System.Diagnostics.Debug.WriteLine($"临时添加人员: {personnel.Name} (ID: {personnelId}) 到哨位 {CurrentEditingPosition.Name}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"警告: 未找到人员 ID: {personnelId}");
                    }
                }

                // 更新UI显示
                // 这里可以触发UI刷新，显示临时添加的人员
                OnPropertyChanged(nameof(SelectedPositions));

                // 清空当前编辑的哨位和选择
                CurrentEditingPosition = null;
                AvailablePersonnelForPosition.Clear();
                SelectedPersonnelIdsForPosition.Clear();

                // 设置标志为false，关闭对话框
                IsAddingPersonnelToPosition = false;

                System.Diagnostics.Debug.WriteLine("人员添加完成，对话框已关闭");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 提交添加人员到哨位失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                await _dialogService.ShowErrorAsync("添加人员失败", ex);
                
                // 确保对话框关闭
                IsAddingPersonnelToPosition = false;
            }
        }

        /// <summary>
        /// 取消添加人员到哨位
        /// </summary>
        private void CancelAddPersonnelToPosition()
        {
            System.Diagnostics.Debug.WriteLine("取消添加人员到哨位");

            // 清空当前编辑的哨位和选择
            CurrentEditingPosition = null;
            AvailablePersonnelForPosition.Clear();
            SelectedPersonnelIdsForPosition.Clear();

            // 设置标志为false，关闭对话框
            IsAddingPersonnelToPosition = false;
        }

        #endregion

        #region 移除和撤销方法

        /// <summary>
        /// 临时移除人员从哨位
        /// </summary>
        /// <param name="parameters">哨位ID和人员ID的元组</param>
        private void RemovePersonnelFromPosition((int positionId, int personnelId) parameters)
        {
            try
            {
                var (positionId, personnelId) = parameters;

                System.Diagnostics.Debug.WriteLine($"=== 临时移除人员从哨位 ===");
                System.Diagnostics.Debug.WriteLine($"哨位ID: {positionId}, 人员ID: {personnelId}");

                // 调用_positionPersonnelManager.RemovePersonnelTemporarily
                _positionPersonnelManager.RemovePersonnelTemporarily(positionId, personnelId);

                // 从缓存中获取哨位和人员名称用于日志
                var position = GetPositionFromCache(positionId);
                var personnel = GetPersonnelFromCache(personnelId);

                if (position != null && personnel != null)
                {
                    System.Diagnostics.Debug.WriteLine($"已临时移除人员: {personnel.Name} 从哨位 {position.Name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"警告: 未找到哨位或人员信息 (哨位ID: {positionId}, 人员ID: {personnelId})");
                }

                // 更新UI显示
                // 触发UI刷新，显示临时移除的人员
                OnPropertyChanged(nameof(SelectedPositions));

                System.Diagnostics.Debug.WriteLine("人员移除完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 临时移除人员失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                _ = _dialogService.ShowErrorAsync("移除人员失败", ex);
            }
        }

        /// <summary>
        /// 撤销哨位的临时更改
        /// </summary>
        /// <param name="positionId">哨位ID</param>
        private void RevertPositionChanges(int positionId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== 撤销哨位的临时更改 ===");
                System.Diagnostics.Debug.WriteLine($"哨位ID: {positionId}");

                // 获取哨位名称用于日志
                var position = SelectedPositions.FirstOrDefault(p => p.Id == positionId);
                if (position != null)
                {
                    System.Diagnostics.Debug.WriteLine($"哨位: {position.Name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"警告: 未找到哨位 ID: {positionId}");
                }

                // 调用_positionPersonnelManager.RevertChanges
                _positionPersonnelManager.RevertChanges(positionId);

                // 更新UI显示
                // 触发UI刷新，恢复到原始状态
                OnPropertyChanged(nameof(SelectedPositions));

                System.Diagnostics.Debug.WriteLine("临时更改已撤销");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 撤销哨位临时更改失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                _ = _dialogService.ShowErrorAsync("撤销更改失败", ex);
            }
        }

        #endregion

        #region 保存为永久方法

        /// <summary>
        /// 显示保存确认对话框
        /// </summary>
        /// <param name="changes">哨位人员更改记录</param>
        /// <returns>用户是否确认保存</returns>
        private async Task<bool> ShowSaveConfirmationDialog(PositionPersonnelChanges changes)
        {
            System.Diagnostics.Debug.WriteLine($"=== 显示保存确认对话框 ===");
            System.Diagnostics.Debug.WriteLine($"哨位ID: {changes.PositionId}, 哨位名称: {changes.PositionName}");

            // 构建对话框内容
            var contentPanel = new Microsoft.UI.Xaml.Controls.StackPanel
            {
                Spacing = 12
            };

            // 警告图标和文本
            var warningPanel = new Microsoft.UI.Xaml.Controls.StackPanel
            {
                Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                Spacing = 8
            };
            warningPanel.Children.Add(new Microsoft.UI.Xaml.Controls.SymbolIcon
            {
                Symbol = Microsoft.UI.Xaml.Controls.Symbol.Important,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange)
            });
            warningPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = "以下更改将永久保存到数据库:",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            contentPanel.Children.Add(warningPanel);

            // 哨位信息
            contentPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = $"哨位: {changes.PositionName}",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0)
            });

            // 添加的人员
            if (changes.AddedPersonnelIds.Any())
            {
                contentPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Text = "添加人员:",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 4)
                });

                foreach (var personnelId in changes.AddedPersonnelIds)
                {
                    var personnel = GetPersonnelFromCache(personnelId);
                    var personnelName = personnel?.Name ?? $"人员ID:{personnelId}";
                    
                    var personnelPanel = new Microsoft.UI.Xaml.Controls.StackPanel
                    {
                        Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                        Spacing = 8,
                        Margin = new Microsoft.UI.Xaml.Thickness(16, 0, 0, 4)
                    };
                    personnelPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = "➕",
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green)
                    });
                    personnelPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = personnelName
                    });
                    contentPanel.Children.Add(personnelPanel);
                }
            }

            // 移除的人员
            if (changes.RemovedPersonnelIds.Any())
            {
                contentPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Text = "移除人员:",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 4)
                });

                foreach (var personnelId in changes.RemovedPersonnelIds)
                {
                    var personnel = GetPersonnelFromCache(personnelId);
                    var personnelName = personnel?.Name ?? $"人员ID:{personnelId}";
                    
                    var personnelPanel = new Microsoft.UI.Xaml.Controls.StackPanel
                    {
                        Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                        Spacing = 8,
                        Margin = new Microsoft.UI.Xaml.Thickness(16, 0, 0, 4)
                    };
                    personnelPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = "➖",
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red)
                    });
                    personnelPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = personnelName
                    });
                    contentPanel.Children.Add(personnelPanel);
                }
            }

            // 说明文本
            contentPanel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = "这些更改将影响后续所有排班。",
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Microsoft.UI.Xaml.Thickness(0, 12, 0, 0)
            });

            // 创建确认对话框
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "确认保存为永久",
                Content = contentPanel,
                PrimaryButtonText = "确认保存",
                SecondaryButtonText = "取消",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Secondary,
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };

            if (dialog.XamlRoot == null) return false;

            try
            {
                var result = await dialog.ShowAsync();
                var confirmed = result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary;
                
                System.Diagnostics.Debug.WriteLine($"用户选择: {(confirmed ? "确认保存" : "取消")}");
                
                return confirmed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示确认对话框失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存哨位的临时更改为永久
        /// </summary>
        /// <param name="positionId">哨位ID</param>
        private async Task SavePositionChangesAsync(int positionId)
        {
            System.Diagnostics.Debug.WriteLine($"=== 开始保存哨位更改为永久 ===");
            System.Diagnostics.Debug.WriteLine($"哨位ID: {positionId}");

            try
            {
                // 获取哨位的临时更改
                var changes = _positionPersonnelManager.GetChanges(positionId);
                
                if (!changes.HasChanges)
                {
                    System.Diagnostics.Debug.WriteLine("没有需要保存的更改");
                    await _dialogService.ShowWarningAsync("没有需要保存的更改");
                    return;
                }

                // 获取哨位
                var position = SelectedPositions.FirstOrDefault(p => p.Id == positionId);
                if (position == null)
                {
                    System.Diagnostics.Debug.WriteLine($"哨位不存在: ID={positionId}");
                    await _dialogService.ShowErrorAsync("哨位不存在");
                    return;
                }

                // 设置哨位名称到changes对象中（用于对话框显示）
                changes.PositionName = position.Name;

                System.Diagnostics.Debug.WriteLine($"哨位: {position.Name}");
                System.Diagnostics.Debug.WriteLine($"添加人员数: {changes.AddedPersonnelIds.Count}");
                System.Diagnostics.Debug.WriteLine($"移除人员数: {changes.RemovedPersonnelIds.Count}");

                // 显示确认对话框
                var confirmed = await ShowSaveConfirmationDialog(changes);
                if (!confirmed)
                {
                    System.Diagnostics.Debug.WriteLine("用户取消保存");
                    return;
                }

                // 获取更新后的可用人员列表
                var updatedPersonnelIds = _positionPersonnelManager.GetAvailablePersonnel(positionId);
                
                System.Diagnostics.Debug.WriteLine($"更新后的可用人员数: {updatedPersonnelIds.Count}");

                // 创建更新DTO
                var updateDto = new UpdatePositionDto
                {
                    Name = position.Name,
                    Location = position.Location,
                    Description = position.Description,
                    Requirements = position.Requirements,
                    RequiredSkillIds = position.RequiredSkillIds,
                    AvailablePersonnelIds = updatedPersonnelIds.ToList()
                };

                // 调用IPositionService.UpdateAsync更新数据库
                System.Diagnostics.Debug.WriteLine("开始调用IPositionService.UpdateAsync");
                await _positionService.UpdateAsync(positionId, updateDto);
                System.Diagnostics.Debug.WriteLine("数据库更新成功");

                // 更新本地PositionDto数据
                position.AvailablePersonnelIds = updatedPersonnelIds.ToList();
                System.Diagnostics.Debug.WriteLine("本地数据已更新");

                // 重新初始化PositionPersonnelManager（清除临时更改标记）
                _positionPersonnelManager.Initialize(SelectedPositions);
                System.Diagnostics.Debug.WriteLine("PositionPersonnelManager已重新初始化");

                // 更新UI显示
                OnPropertyChanged(nameof(SelectedPositions));

                // 显示成功提示
                await _dialogService.ShowSuccessAsync($"哨位 '{position.Name}' 的更改已保存");
                
                System.Diagnostics.Debug.WriteLine("=== 保存哨位更改完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 保存哨位更改失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                await _dialogService.ShowErrorAsync("保存失败", ex);
            }
        }

        #endregion

        #region 手动添加参与人员方法

        /// <summary>
        /// 开始手动添加参与人员（不属于任何哨位）
        /// </summary>
        private void StartManualAddPersonnel()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 开始手动添加参与人员 ===");

                // 验证输入
                if (SelectedPositions == null || AvailablePersonnels == null)
                {
                    System.Diagnostics.Debug.WriteLine("错误: SelectedPositions或AvailablePersonnels为null");
                    _ = _dialogService.ShowErrorAsync("无法加载人员列表");
                    return;
                }

                // 从AvailablePersonnels中筛选不在任何哨位可用人员列表中的人员
                var autoExtractedPersonnelIds = SelectedPositions
                    .SelectMany(p => _positionPersonnelManager.GetAvailablePersonnel(p.Id))
                    .Distinct()
                    .ToHashSet();

                System.Diagnostics.Debug.WriteLine($"自动提取的人员ID数量: {autoExtractedPersonnelIds.Count}");

                // 可以手动添加的人员：不在任何哨位可用人员列表中的人员
                var availableForManualAdd = AvailablePersonnels
                    .Where(p => !autoExtractedPersonnelIds.Contains(p.Id))
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"可手动添加的人员数量: {availableForManualAdd.Count}");

                // 设置可选人员列表
                AvailablePersonnelForManualAdd = new ObservableCollection<PersonnelDto>(availableForManualAdd);

                // 清空之前的选择
                SelectedPersonnelIdsForManualAdd.Clear();

                // 显示手动添加人员对话框
                IsManualAddingPersonnel = true;

                System.Diagnostics.Debug.WriteLine("手动添加人员对话框已打开");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 开始手动添加参与人员失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                _ = _dialogService.ShowErrorAsync("无法打开添加人员对话框", ex);
            }
        }

        /// <summary>
        /// 提交手动添加参与人员
        /// </summary>
        private async Task SubmitManualAddPersonnelAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 提交手动添加参与人员 ===");

                // 验证选择的人员
                if (SelectedPersonnelIdsForManualAdd.Count == 0)
                {
                    await _dialogService.ShowWarningAsync("请至少选择一名人员");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"选择的人员数量: {SelectedPersonnelIdsForManualAdd.Count}");

                // 添加到ManuallyAddedPersonnelIds列表和详细信息列表
                foreach (var personnelId in SelectedPersonnelIdsForManualAdd)
                {
                    // 避免重复添加
                    if (!ManuallyAddedPersonnelIds.Contains(personnelId))
                    {
                        ManuallyAddedPersonnelIds.Add(personnelId);
                        
                        var personnel = AvailablePersonnelForManualAdd.FirstOrDefault(p => p.Id == personnelId);
                        if (personnel != null)
                        {
                            // 添加到详细信息列表
                            ManuallyAddedPersonnelDetails.Add(personnel);
                            
                            // 添加到SelectedPersonnels（如果不存在）
                            if (!SelectedPersonnels.Any(p => p.Id == personnelId))
                            {
                                SelectedPersonnels.Add(personnel);
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"手动添加人员: {personnel.Name} (ID: {personnelId})");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"警告: 未找到人员 ID: {personnelId}");
                        }
                    }
                }

                // 更新ManuallyAddedPersonnelCount
                ManuallyAddedPersonnelCount = ManuallyAddedPersonnelIds.Count;

                System.Diagnostics.Debug.WriteLine($"手动添加人员总数: {ManuallyAddedPersonnelCount}");

                // 清空选择和可选列表
                SelectedPersonnelIdsForManualAdd.Clear();
                AvailablePersonnelForManualAdd.Clear();

                // 关闭对话框
                IsManualAddingPersonnel = false;

                System.Diagnostics.Debug.WriteLine("手动添加人员完成，对话框已关闭");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 提交手动添加参与人员失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                await _dialogService.ShowErrorAsync("添加人员失败", ex);
                
                // 确保对话框关闭
                IsManualAddingPersonnel = false;
            }
        }

        /// <summary>
        /// 取消手动添加参与人员
        /// </summary>
        private void CancelManualAddPersonnel()
        {
            System.Diagnostics.Debug.WriteLine("取消手动添加参与人员");

            // 清空选择和可选列表
            SelectedPersonnelIdsForManualAdd.Clear();
            AvailablePersonnelForManualAdd.Clear();

            // 关闭对话框
            IsManualAddingPersonnel = false;
        }

        /// <summary>
        /// 移除手动添加的人员
        /// </summary>
        /// <param name="personnelId">人员ID</param>
        private void RemoveManualPersonnel(int personnelId)
        {
            System.Diagnostics.Debug.WriteLine($"=== 移除手动添加的人员 ===");
            System.Diagnostics.Debug.WriteLine($"人员ID: {personnelId}");

            // 从缓存中获取人员名称用于日志
            var personnel = GetPersonnelFromCache(personnelId);
            if (personnel != null)
            {
                System.Diagnostics.Debug.WriteLine($"人员: {personnel.Name}");
            }

            // 从ManuallyAddedPersonnelIds列表中移除
            if (ManuallyAddedPersonnelIds.Contains(personnelId))
            {
                ManuallyAddedPersonnelIds.Remove(personnelId);
                System.Diagnostics.Debug.WriteLine("人员已从手动添加列表中移除");
            }

            // 从详细信息列表中移除
            var detailToRemove = ManuallyAddedPersonnelDetails.FirstOrDefault(p => p.Id == personnelId);
            if (detailToRemove != null)
            {
                ManuallyAddedPersonnelDetails.Remove(detailToRemove);
                System.Diagnostics.Debug.WriteLine("人员已从详细信息列表中移除");
            }

            // 从SelectedPersonnels中移除（如果该人员不在任何哨位的可用人员列表中）
            var isInAnyPosition = SelectedPositions.Any(pos => 
                _positionPersonnelManager.GetAvailablePersonnel(pos.Id).Contains(personnelId));
            
            if (!isInAnyPosition)
            {
                var selectedToRemove = SelectedPersonnels.FirstOrDefault(p => p.Id == personnelId);
                if (selectedToRemove != null)
                {
                    SelectedPersonnels.Remove(selectedToRemove);
                    System.Diagnostics.Debug.WriteLine("人员已从选中列表中移除");
                }
            }

            // 更新ManuallyAddedPersonnelCount
            ManuallyAddedPersonnelCount = ManuallyAddedPersonnelIds.Count;

            System.Diagnostics.Debug.WriteLine($"手动添加人员总数: {ManuallyAddedPersonnelCount}");
            System.Diagnostics.Debug.WriteLine("=== 移除完成 ===");
        }

        #endregion

        #region 人员提取和视图模型更新

        /// <summary>
        /// 从已选哨位自动提取可用人员
        /// </summary>
        private void ExtractPersonnelFromPositions()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            System.Diagnostics.Debug.WriteLine("=== 开始自动提取人员 ===");
            System.Diagnostics.Debug.WriteLine($"已选哨位数量: {SelectedPositions.Count}");

            try
            {
                // 验证输入
                if (SelectedPositions == null)
                {
                    System.Diagnostics.Debug.WriteLine("错误: SelectedPositions为null");
                    AutoExtractedPersonnelCount = 0;
                    ManuallyAddedPersonnelCount = ManuallyAddedPersonnelIds.Count;
                    return;
                }

                // 初始化PositionPersonnelManager
                _positionPersonnelManager.Initialize(SelectedPositions);
                var initTime = stopwatch.ElapsedMilliseconds;
                System.Diagnostics.Debug.WriteLine($"PositionPersonnelManager初始化耗时: {initTime}ms");

                // 使用HashSet提高查找性能，避免重复计算
                var autoExtractedPersonnelIds = new HashSet<int>();
                foreach (var position in SelectedPositions)
                {
                    if (position.AvailablePersonnelIds == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"警告: 哨位 {position.Name} (ID: {position.Id}) 的AvailablePersonnelIds为null");
                        continue;
                    }

                    foreach (var personnelId in position.AvailablePersonnelIds)
                    {
                        autoExtractedPersonnelIds.Add(personnelId);
                    }
                }
                var extractTime = stopwatch.ElapsedMilliseconds - initTime;
                System.Diagnostics.Debug.WriteLine($"人员ID提取耗时: {extractTime}ms");
                System.Diagnostics.Debug.WriteLine($"自动提取的人员ID数量: {autoExtractedPersonnelIds.Count}");

                // 更新自动提取的人员数量
                AutoExtractedPersonnelCount = autoExtractedPersonnelIds.Count;

                // 更新手动添加的人员数量
                ManuallyAddedPersonnelCount = ManuallyAddedPersonnelIds.Count;

                // 更新哨位人员视图模型
                UpdatePositionPersonnelViewModels();

                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine($"自动提取人员数量: {AutoExtractedPersonnelCount}");
                System.Diagnostics.Debug.WriteLine($"手动添加人员数量: {ManuallyAddedPersonnelCount}");
                System.Diagnostics.Debug.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds}ms");
                
                // 性能警告
                if (stopwatch.ElapsedMilliseconds > 500 && SelectedPositions.Count <= 50)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ 性能警告: 50个以下哨位的人员提取超过500ms");
                }
                else if (stopwatch.ElapsedMilliseconds > 2000 && SelectedPositions.Count > 50)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ 性能警告: 50个以上哨位的人员提取超过2秒");
                }
                
                System.Diagnostics.Debug.WriteLine("=== 人员提取完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 自动提取人员失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                // 设置默认值，避免UI显示异常
                AutoExtractedPersonnelCount = 0;
                ManuallyAddedPersonnelCount = ManuallyAddedPersonnelIds.Count;
                
                // 显示错误提示给用户
                _ = _dialogService.ShowErrorAsync("自动提取人员失败", ex);
            }
        }

        /// <summary>
        /// 更新哨位人员视图模型（用于步骤3的UI展示）
        /// </summary>
        private void UpdatePositionPersonnelViewModels()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            System.Diagnostics.Debug.WriteLine("=== 开始更新哨位人员视图模型 ===");

            try
            {
                // 清空现有的视图模型
                PositionPersonnelViewModels.Clear();

                // 为每个选中的哨位创建视图模型
                foreach (var position in SelectedPositions)
                {
                    // 获取该哨位的可用人员ID（包含临时更改）
                    var availablePersonnelIds = _positionPersonnelManager.GetAvailablePersonnel(position.Id);
                    
                    // 创建人员项视图模型列表
                    var personnelItems = new ObservableCollection<PersonnelItemViewModel>();
                    
                    // 获取哨位的临时更改状态（在循环外获取一次）
                    var positionChanges = _positionPersonnelManager.GetChanges(position.Id);
                    
                    foreach (var personnelId in availablePersonnelIds)
                    {
                        var personnel = GetPersonnelFromCache(personnelId);
                        if (personnel == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"警告: 未找到人员 ID: {personnelId}");
                            continue;
                        }

                        // 判断人员来源类型
                        var sourceType = PersonnelSourceType.AutoExtracted;
                        
                        if (positionChanges.AddedPersonnelIds.Contains(personnelId))
                        {
                            sourceType = PersonnelSourceType.TemporarilyAdded;
                        }
                        else if (positionChanges.RemovedPersonnelIds.Contains(personnelId))
                        {
                            // 注意：如果人员被临时移除，它不应该出现在availablePersonnelIds中
                            // 这里只是为了完整性而保留此逻辑
                            sourceType = PersonnelSourceType.TemporarilyRemoved;
                        }

                        // 判断是否为共享人员（在多个哨位中可用）
                        var sharedPositionCount = SelectedPositions.Count(p => 
                            _positionPersonnelManager.GetAvailablePersonnel(p.Id).Contains(personnelId));
                        var isShared = sharedPositionCount > 1;

                        // 格式化技能显示
                        var skillsDisplay = personnel.SkillIds != null && personnel.SkillIds.Count > 0
                            ? $"技能: {personnel.SkillIds.Count} 项"
                            : "无技能";

                        // 创建人员项视图模型
                        var personnelItem = new PersonnelItemViewModel
                        {
                            PersonnelId = personnelId,
                            PersonnelName = personnel.Name,
                            SkillsDisplay = skillsDisplay,
                            SourceType = sourceType,
                            IsShared = isShared,
                            SharedPositionCount = sharedPositionCount,
                            // 自动提取的人员默认选中，其他类型根据SelectedPersonnels判断
                            IsSelected = sourceType == PersonnelSourceType.AutoExtracted || SelectedPersonnels.Any(p => p.Id == personnelId)
                        };

                        // 如果人员被选中但不在SelectedPersonnels中，添加它
                        if (personnelItem.IsSelected && !SelectedPersonnels.Any(p => p.Id == personnelId))
                        {
                            SelectedPersonnels.Add(personnel);
                            System.Diagnostics.Debug.WriteLine($"自动添加选中人员到列表: {personnel.Name}");
                        }

                        // 监听IsSelected属性变化，更新SelectedPersonnels集合
                        personnelItem.PropertyChanged += (s, e) =>
                        {
                            if (e?.PropertyName == nameof(PersonnelItemViewModel.IsSelected))
                            {
                                if (s is not PersonnelItemViewModel item) return;

                                var personnelDto = GetPersonnelFromCache(item.PersonnelId);
                                if (personnelDto == null) return;

                                if (item.IsSelected)
                                {
                                    // 添加到SelectedPersonnels（如果不存在）
                                    if (!SelectedPersonnels.Any(p => p.Id == item.PersonnelId))
                                    {
                                        SelectedPersonnels.Add(personnelDto);
                                        System.Diagnostics.Debug.WriteLine($"添加人员到选中列表: {personnelDto.Name}");
                                    }
                                }
                                else
                                {
                                    // 从SelectedPersonnels移除
                                    var toRemove = SelectedPersonnels.FirstOrDefault(p => p.Id == item.PersonnelId);
                                    if (toRemove != null)
                                    {
                                        SelectedPersonnels.Remove(toRemove);
                                        System.Diagnostics.Debug.WriteLine($"从选中列表移除人员: {personnelDto.Name}");
                                    }
                                }
                            }
                        };

                        personnelItems.Add(personnelItem);
                    }

                    // 获取哨位的临时更改状态
                    var changes = _positionPersonnelManager.GetChanges(position.Id);

                    // 创建哨位人员视图模型
                    var positionViewModel = new PositionPersonnelViewModel
                    {
                        PositionId = position.Id,
                        PositionName = position.Name,
                        Location = position.Location ?? string.Empty,
                        PositionDto = position,
                        AvailablePersonnel = personnelItems,
                        IsExpanded = true,
                        HasChanges = changes.HasChanges,
                        ChangesSummary = changes.HasChanges 
                            ? $"添加 {changes.AddedPersonnelIds.Count} 人，移除 {changes.RemovedPersonnelIds.Count} 人"
                            : string.Empty
                    };

                    PositionPersonnelViewModels.Add(positionViewModel);
                }

                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine($"哨位人员视图模型更新完成: {PositionPersonnelViewModels.Count} 个哨位");
                System.Diagnostics.Debug.WriteLine($"总耗时: {stopwatch.ElapsedMilliseconds}ms");
                System.Diagnostics.Debug.WriteLine("=== 哨位人员视图模型更新完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 更新哨位人员视图模型失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                // 清空视图模型，避免显示错误数据
                PositionPersonnelViewModels.Clear();
                
                // 显示错误提示给用户
                _ = _dialogService.ShowErrorAsync("更新哨位人员列表失败", ex);
            }
        }

        #endregion
    }
}
