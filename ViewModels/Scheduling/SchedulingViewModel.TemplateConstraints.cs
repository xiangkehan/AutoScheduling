using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using AutoScheduling3.Models.Constraints;

namespace AutoScheduling3.ViewModels.Scheduling
{
    public partial class SchedulingViewModel
    {
        #region 约束加载

        private async Task LoadConstraintsAsync()
        {
            // 重复加载检查
            if (IsLoadingConstraints)
            {
                System.Diagnostics.Debug.WriteLine("约束数据正在加载中，跳过重复请求");
                return;
            }
            
            IsLoadingConstraints = true;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            System.Diagnostics.Debug.WriteLine("=== 开始加载约束数据 ===");
            System.Diagnostics.Debug.WriteLine($"日期范围: {StartDate:yyyy-MM-dd} 到 {EndDate:yyyy-MM-dd}");
            
            try
            {
                // 并行加载三种约束数据
                var configsTask = _schedulingService.GetHolidayConfigsAsync();
                var rulesTask = _schedulingService.GetFixedPositionRulesAsync(true);
                var manualTask = _schedulingService.GetManualAssignmentsAsync(StartDate.Date, EndDate.Date, true);
                
                await Task.WhenAll(configsTask, rulesTask, manualTask);
                
                var configs = configsTask.Result;
                var rules = rulesTask.Result;
                var manuals = manualTask.Result;
                
                System.Diagnostics.Debug.WriteLine($"加载完成 - 休息日配置: {configs.Count}, 定岗规则: {rules.Count}, 手动指定: {manuals.Count}");
                
                // 更新 ObservableCollection
                HolidayConfigs = new ObservableCollection<HolidayConfig>(configs);
                FixedPositionRules = new ObservableCollection<FixedPositionRule>(rules);
                ManualAssignments = new ObservableCollection<ManualAssignment>(manuals);
                
                // 转换为DTO并加载手动指定到ManualAssignmentManager（使用缓存提高性能）
                var manualDtos = manuals.Select(m =>
                {
                    // 从缓存中查找人员和岗位名称
                    var personnel = GetPersonnelFromCache(m.PersonalId);
                    var position = GetPositionFromCache(m.PositionId);
                    
                    return new ManualAssignmentDto
                    {
                        Id = m.Id,
                        Date = m.Date,
                        PersonnelId = m.PersonalId,
                        PersonnelName = personnel?.Name ?? "未知人员",
                        PositionId = m.PositionId,
                        PositionName = position?.Name ?? "未知哨位",
                        TimeSlot = m.PeriodIndex,
                        Remarks = m.Remarks,
                        IsEnabled = m.IsEnabled,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                }).ToList();
                _manualAssignmentManager.LoadSaved(manualDtos);
                System.Diagnostics.Debug.WriteLine($"已加载 {manuals.Count} 条手动指定到ManualAssignmentManager");
                
                // 空数据警告日志
                if (configs.Count == 0)
                    System.Diagnostics.Debug.WriteLine("警告: 没有找到休息日配置");
                if (rules.Count == 0)
                    System.Diagnostics.Debug.WriteLine("警告: 没有找到定岗规则");
                if (manuals.Count == 0)
                    System.Diagnostics.Debug.WriteLine("警告: 没有找到手动指定");
                
                // 应用模板约束（如果有）
                if (TemplateApplied)
                {
                    System.Diagnostics.Debug.WriteLine("应用模板约束设置");
                    ApplyTemplateConstraints();
                }
            }
            catch (Microsoft.Data.Sqlite.SqliteException sqlEx)
            {
                System.Diagnostics.Debug.WriteLine("=== 加载约束数据失败 (数据库错误) ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {sqlEx.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误代码: {sqlEx.SqliteErrorCode}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {sqlEx.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {sqlEx.StackTrace}");
                
                await _dialogService.ShowErrorAsync(
                    "无法连接到数据库",
                    $"请检查数据库文件是否存在且未被占用。\n\n错误详情: {sqlEx.Message}");
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine("=== 加载约束数据失败 (数据格式错误) ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {jsonEx.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {jsonEx.Message}");
                System.Diagnostics.Debug.WriteLine($"路径: {jsonEx.Path}");
                System.Diagnostics.Debug.WriteLine($"行号: {jsonEx.LineNumber}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {jsonEx.StackTrace}");
                
                await _dialogService.ShowErrorAsync(
                    "约束数据格式错误",
                    $"数据库中的约束数据格式不正确，可能需要重新创建。\n\n错误详情: {jsonEx.Message}");
            }
            catch (TaskCanceledException taskEx)
            {
                System.Diagnostics.Debug.WriteLine("=== 加载约束数据失败 (超时) ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {taskEx.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {taskEx.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {taskEx.StackTrace}");
                
                await _dialogService.ShowWarningAsync("加载约束数据超时，请重试");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 加载约束数据失败 (未知错误) ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"内部异常: {ex.InnerException.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"内部异常消息: {ex.InnerException.Message}");
                }
                
                await _dialogService.ShowErrorAsync(
                    "加载约束数据失败",
                    $"发生了意外错误。\n\n错误类型: {ex.GetType().Name}\n错误消息: {ex.Message}");
            }
            finally
            {
                IsLoadingConstraints = false;
                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine($"约束数据加载耗时: {stopwatch.ElapsedMilliseconds}ms");
                System.Diagnostics.Debug.WriteLine("=== 约束数据加载流程结束 ===");
            }
        }

        #endregion

        #region 模板管理

        private async Task LoadTemplateAsync(int templateId)
        {
            IsLoadingInitial = true;
            System.Diagnostics.Debug.WriteLine($"=== 开始加载模板 (ID: {templateId}) ===");
            
            try
            {
                var template = await _templateService.GetByIdAsync(templateId);
                if (template == null)
                {
                    System.Diagnostics.Debug.WriteLine($"模板不存在 (ID: {templateId})");
                    await _dialogService.ShowWarningAsync("模板不存在");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"模板加载成功: {template.Name}");
                System.Diagnostics.Debug.WriteLine($"模板包含 - 人员: {template.PersonnelIds.Count}, 岗位: {template.PositionIds.Count}");
                
                LoadedTemplateId = template.Id;

                // Load base data first
                if (AvailablePersonnels.Count == 0 || AvailablePositions.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("加载基础数据（人员和岗位）");
                    await LoadInitialDataAsync();
                }

                // Pre-fill selections
                var selectedPers = AvailablePersonnels.Where(p => template.PersonnelIds.Contains(p.Id)).ToList();
                var selectedPos = AvailablePositions.Where(p => template.PositionIds.Contains(p.Id)).ToList();
                
                // Validate and identify missing resources
                var missingPersonnelIds = template.PersonnelIds
                    .Except(selectedPers.Select(p => p.Id))
                    .ToList();
                var missingPositionIds = template.PositionIds
                    .Except(selectedPos.Select(p => p.Id))
                    .ToList();
                
                // Display warning if any resources are missing
                if (missingPersonnelIds.Any() || missingPositionIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine("警告: 模板中的部分资源已不存在");
                    if (missingPersonnelIds.Any())
                        System.Diagnostics.Debug.WriteLine($"- 缺失人员ID: {string.Join(", ", missingPersonnelIds)}");
                    if (missingPositionIds.Any())
                        System.Diagnostics.Debug.WriteLine($"- 缺失岗位ID: {string.Join(", ", missingPositionIds)}");
                    
                    var warningMsg = "模板中的部分资源已不存在：\n";
                    if (missingPersonnelIds.Any())
                        warningMsg += $"- 缺失人员ID: {string.Join(", ", missingPersonnelIds)}\n";
                    if (missingPositionIds.Any())
                        warningMsg += $"- 缺失岗位ID: {string.Join(", ", missingPositionIds)}\n";
                    warningMsg += "\n将仅加载可用的资源。";
                    await _dialogService.ShowWarningAsync(warningMsg);
                }
                
                SelectedPersonnels = new ObservableCollection<PersonnelDto>(selectedPers);
                SelectedPositions = new ObservableCollection<PositionDto>(selectedPos);

                // Store constraint IDs to be applied after they are loaded
                UseActiveHolidayConfig = template.UseActiveHolidayConfig;
                SelectedHolidayConfigId = template.HolidayConfigId;
                EnabledFixedRules = template.EnabledFixedRuleIds?.ToList() ?? new();
                EnabledManualAssignments = template.EnabledManualAssignmentIds?.ToList() ?? new();
                
                System.Diagnostics.Debug.WriteLine($"模板约束配置 - 固定规则: {EnabledFixedRules.Count}, 手动指定: {EnabledManualAssignments.Count}");
                System.Diagnostics.Debug.WriteLine($"节假日配置 - 使用活动配置: {UseActiveHolidayConfig}, 配置ID: {SelectedHolidayConfigId}");

                // Immediately load constraint data
                System.Diagnostics.Debug.WriteLine("开始加载约束数据以应用模板设置");
                await LoadConstraintsAsync();
                
                // Apply template constraints after loading
                System.Diagnostics.Debug.WriteLine("约束数据加载完成，开始应用模板约束设置");
                ApplyTemplateConstraints();

                TemplateApplied = true;
                CurrentStep = 1; // Stay on step 1
                RefreshCommandStates();
                
                // Calculate constraint counts
                var enabledFixedRulesCount = FixedPositionRules.Count(r => r.IsEnabled);
                var enabledManualAssignmentsCount = ManualAssignments.Count(a => a.IsEnabled);
                var totalConstraints = enabledFixedRulesCount + enabledManualAssignmentsCount;
                
                System.Diagnostics.Debug.WriteLine($"模板应用完成 - 已启用约束: {totalConstraints} (固定规则: {enabledFixedRulesCount}, 手动指定: {enabledManualAssignmentsCount})");
                System.Diagnostics.Debug.WriteLine("=== 模板加载流程结束 ===");
                
                // Display success message with statistics
                var successMsg = $"模板已加载\n" +
                                $"人员: {selectedPers.Count}\n" +
                                $"岗位: {selectedPos.Count}\n" +
                                $"约束: {totalConstraints}";
                await _dialogService.ShowSuccessAsync(successMsg);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 加载模板失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                await _dialogService.ShowErrorAsync("加载模板失败", ex);
            }
            finally
            {
                IsLoadingInitial = false;
            }
        }

        private void ApplyTemplateConstraints()
        {
            if (!TemplateApplied)
            {
                System.Diagnostics.Debug.WriteLine("ApplyTemplateConstraints: 模板未应用，跳过");
                return;
            }

            System.Diagnostics.Debug.WriteLine("=== 开始应用模板约束 ===");
            System.Diagnostics.Debug.WriteLine($"模板中的固定规则ID: [{string.Join(", ", EnabledFixedRules)}]");
            System.Diagnostics.Debug.WriteLine($"模板中的手动指定ID: [{string.Join(", ", EnabledManualAssignments)}]");
            System.Diagnostics.Debug.WriteLine($"数据库中的固定规则数量: {FixedPositionRules.Count}");
            System.Diagnostics.Debug.WriteLine($"数据库中的手动指定数量: {ManualAssignments.Count}");

            // 验证并应用固定规则
            var appliedFixedRules = 0;
            var missingFixedRuleIds = new List<int>();
            
            foreach (var rule in FixedPositionRules)
            {
                if (EnabledFixedRules.Contains(rule.Id))
                {
                    rule.IsEnabled = true;
                    appliedFixedRules++;
                    System.Diagnostics.Debug.WriteLine($"启用固定规则: ID={rule.Id}, 描述={rule.Description}");
                }
                else
                {
                    rule.IsEnabled = false;
                }
            }
            
            // 检查模板中的规则是否在数据库中存在
            var existingFixedRuleIds = FixedPositionRules.Select(r => r.Id).ToHashSet();
            foreach (var templateRuleId in EnabledFixedRules)
            {
                if (!existingFixedRuleIds.Contains(templateRuleId))
                {
                    missingFixedRuleIds.Add(templateRuleId);
                }
            }

            // 验证并应用手动指定
            var appliedManualAssignments = 0;
            var missingManualAssignmentIds = new List<int>();
            
            foreach (var assignment in ManualAssignments)
            {
                if (EnabledManualAssignments.Contains(assignment.Id))
                {
                    assignment.IsEnabled = true;
                    appliedManualAssignments++;
                    System.Diagnostics.Debug.WriteLine($"启用手动指定: ID={assignment.Id}, 日期={assignment.Date:yyyy-MM-dd}, 人员={assignment.PersonalId}, 岗位={assignment.PositionId}");
                }
                else
                {
                    assignment.IsEnabled = false;
                }
            }
            
            // 同步手动指定启用状态到ManualAssignmentManager
            foreach (var savedAssignment in _manualAssignmentManager.SavedAssignments)
            {
                if (savedAssignment.Id.HasValue)
                {
                    savedAssignment.IsEnabled = EnabledManualAssignments.Contains(savedAssignment.Id.Value);
                }
            }
            System.Diagnostics.Debug.WriteLine($"已同步 {_manualAssignmentManager.SavedAssignments.Count} 条手动指定的启用状态到ManualAssignmentManager");
            
            // 检查模板中的手动指定是否在数据库中存在
            var existingManualAssignmentIds = ManualAssignments.Select(a => a.Id).ToHashSet();
            foreach (var templateAssignmentId in EnabledManualAssignments)
            {
                if (!existingManualAssignmentIds.Contains(templateAssignmentId))
                {
                    missingManualAssignmentIds.Add(templateAssignmentId);
                }
            }

            System.Diagnostics.Debug.WriteLine($"应用结果 - 固定规则: {appliedFixedRules}/{EnabledFixedRules.Count}, 手动指定: {appliedManualAssignments}/{EnabledManualAssignments.Count}");

            // 记录缺失的约束
            if (missingFixedRuleIds.Any() || missingManualAssignmentIds.Any())
            {
                System.Diagnostics.Debug.WriteLine("警告: 模板中的部分约束在数据库中不存在");
                
                if (missingFixedRuleIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"- 缺失的固定规则ID: {string.Join(", ", missingFixedRuleIds)}");
                }
                
                if (missingManualAssignmentIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"- 缺失的手动指定ID: {string.Join(", ", missingManualAssignmentIds)}");
                }
                
                // 构建警告消息并显示给用户
                var warningMsg = "模板中的部分约束已不存在：\n";
                if (missingFixedRuleIds.Any())
                {
                    warningMsg += $"- 缺失的固定规则: {missingFixedRuleIds.Count} 条 (ID: {string.Join(", ", missingFixedRuleIds)})\n";
                }
                if (missingManualAssignmentIds.Any())
                {
                    warningMsg += $"- 缺失的手动指定: {missingManualAssignmentIds.Count} 条 (ID: {string.Join(", ", missingManualAssignmentIds)})\n";
                }
                warningMsg += "\n将仅应用存在的约束。";
                
                // 异步显示警告（不阻塞当前流程）
                _ = _dialogService.ShowWarningAsync(warningMsg);
            }
            
            System.Diagnostics.Debug.WriteLine("=== 模板约束应用完成 ===");
        }

        private bool CanSaveTemplate() => SelectedPersonnels.Count > 0 && SelectedPositions.Count > 0;

        private async Task SaveAsTemplateAsync()
        {
            if (!CanSaveTemplate()) { await _dialogService.ShowWarningAsync("当前配置不完整，无法保存为模板"); return; }
            // 创建自定义对话框 (名称、类型、描述、是否默认)
            var nameBox = new Microsoft.UI.Xaml.Controls.TextBox { PlaceholderText = "模板名称", Text = $"模板_{DateTime.Now:yyyyMMdd}" };
            var typeBox = new Microsoft.UI.Xaml.Controls.ComboBox { ItemsSource = new string[] { "regular", "holiday", "special" }, SelectedIndex = 0, MinWidth = 160 };
            var descBox = new Microsoft.UI.Xaml.Controls.TextBox { AcceptsReturn = true, Height = 80, PlaceholderText = "描述(可选)" };
            var defaultSwitch = new Microsoft.UI.Xaml.Controls.ToggleSwitch { Header = "设为默认", IsOn = false };
            var panel = new Microsoft.UI.Xaml.Controls.StackPanel { Spacing = 8 };
            panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "名称:" });
            panel.Children.Add(nameBox);
            panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "类型:" });
            panel.Children.Add(typeBox);
            panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "描述:" });
            panel.Children.Add(descBox);
            panel.Children.Add(defaultSwitch);
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "另存为模板",
                Content = panel,
                PrimaryButtonText = "保存",
                SecondaryButtonText = "取消",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };

            if (dialog.XamlRoot == null) return;

            var result = await dialog.ShowAsync();
            if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary) return;
            var name = nameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name)) { await _dialogService.ShowWarningAsync("名称不能为空"); return; }
            var type = typeBox.SelectedItem?.ToString() ?? "regular";
            if (name.Length > 100) { await _dialogService.ShowWarningAsync("名称不能超过100字符"); return; }
            
            // 保存临时手动指定到数据库
            var tempIdToSavedIdMap = new Dictionary<Guid, int>();
            var savedManualAssignmentIds = new List<int>();
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== 开始保存模板 '{name}' ===");
                System.Diagnostics.Debug.WriteLine($"临时手动指定数量: {_manualAssignmentManager.TemporaryAssignments.Count}");
                
                // 保存所有临时手动指定
                foreach (var tempAssignment in _manualAssignmentManager.TemporaryAssignments)
                {
                    var createDto = new CreateManualAssignmentDto
                    {
                        Date = tempAssignment.Date,
                        PersonnelId = tempAssignment.PersonnelId,
                        PositionId = tempAssignment.PositionId,
                        TimeSlot = tempAssignment.TimeSlot,
                        Remarks = tempAssignment.Remarks,
                        IsEnabled = tempAssignment.IsEnabled
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"保存临时手动指定: 日期={tempAssignment.Date:yyyy-MM-dd}, 人员={tempAssignment.PersonnelName}, 哨位={tempAssignment.PositionName}, 时段={tempAssignment.TimeSlot}");
                    
                    var savedDto = await _schedulingService.CreateManualAssignmentAsync(createDto);
                    tempIdToSavedIdMap[tempAssignment.TempId] = savedDto.Id;
                    savedManualAssignmentIds.Add(savedDto.Id);
                    
                    System.Diagnostics.Debug.WriteLine($"手动指定已保存，ID={savedDto.Id}");
                }
                
                // 将临时手动指定标记为已保存
                if (tempIdToSavedIdMap.Count > 0)
                {
                    _manualAssignmentManager.MarkAsSaved(tempIdToSavedIdMap);
                    System.Diagnostics.Debug.WriteLine($"已将 {tempIdToSavedIdMap.Count} 条临时手动指定标记为已保存");
                }
                
                // 收集所有启用的手动指定ID（包括已保存的和新保存的）
                var allEnabledManualAssignmentIds = ManualAssignments.Where(a => a.IsEnabled).Select(a => a.Id).ToList();
                allEnabledManualAssignmentIds.AddRange(savedManualAssignmentIds);
                
                System.Diagnostics.Debug.WriteLine($"模板将包含 {allEnabledManualAssignmentIds.Count} 条启用的手动指定");
                
                var templateDto = new CreateTemplateDto
                {
                    Name = name,
                    TemplateType = type,
                    Description = string.IsNullOrWhiteSpace(descBox.Text) ? null : descBox.Text.Trim(),
                    IsDefault = defaultSwitch.IsOn,
                    PersonnelIds = SelectedPersonnels.Select(p => p.Id).ToList(),
                    PositionIds = SelectedPositions.Select(p => p.Id).ToList(),
                    HolidayConfigId = UseActiveHolidayConfig ? null : SelectedHolidayConfigId,
                    UseActiveHolidayConfig = UseActiveHolidayConfig,
                    EnabledFixedRuleIds = FixedPositionRules.Where(r => r.IsEnabled).Select(r => r.Id).ToList(),
                    EnabledManualAssignmentIds = allEnabledManualAssignmentIds
                };
                
                var tpl = await _templateService.CreateAsync(templateDto);
                System.Diagnostics.Debug.WriteLine($"模板 '{tpl.Name}' 保存成功，ID={tpl.Id}");
                System.Diagnostics.Debug.WriteLine("=== 模板保存完成 ===");
                
                await _dialogService.ShowSuccessAsync($"模板 '{tpl.Name}' 已保存");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 保存模板失败 ===");
                System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                
                // 如果保存失败，需要回滚已保存的手动指定
                if (tempIdToSavedIdMap.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("尝试回滚已保存的手动指定...");
                    // 注意：这里简化处理，实际应该删除已保存的手动指定
                    // 但由于这是一个复杂的回滚操作，暂时只记录日志
                    System.Diagnostics.Debug.WriteLine($"警告: {tempIdToSavedIdMap.Count} 条手动指定已保存但模板创建失败，可能需要手动清理");
                }
                
                await _dialogService.ShowErrorAsync("保存模板失败", ex);
            }
        }

        #endregion
    }
}
