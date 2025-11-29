using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// SchedulingViewModel 的向导流程部分
    /// 负责步骤导航、验证、执行排班、取消和摘要构建
    /// </summary>
    public partial class SchedulingViewModel
    {
        #region 步骤导航方法

        /// <summary>
        /// 前进到下一步
        /// </summary>
        private void NextStep()
        {
            if (CurrentStep < 6 && CanGoNext())
            {
                System.Diagnostics.Debug.WriteLine($"=== NextStep: 从步骤 {CurrentStep} 前进 ===");
                
                // 如果模板已应用，并且在第1步，直接跳到第6步（摘要）
                if (TemplateApplied && CurrentStep == 1)
                {
                    CurrentStep = 6;
                    BuildSummarySections(); // Build summary when jumping to step 6
                }
                else
                {
                    CurrentStep++;
                }

                System.Diagnostics.Debug.WriteLine($"=== NextStep: 当前步骤 {CurrentStep} ===");
                System.Diagnostics.Debug.WriteLine($"=== SelectedPersonnels 数量: {SelectedPersonnels.Count} ===");

                if (CurrentStep == 4 && !IsLoadingConstraints && FixedPositionRules.Count == 0)
                {
                    _ = LoadConstraintsAsync();
                }
                if (CurrentStep == 6)
                {
                    BuildSummarySections();
                }
                RefreshCommandStates();
                
                System.Diagnostics.Debug.WriteLine($"=== NextStep 完成，SelectedPersonnels 数量: {SelectedPersonnels.Count} ===");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"=== NextStep 被阻止: CurrentStep={CurrentStep}, CanGoNext={CanGoNext()} ===");
                if (!CanGoNext())
                {
                    // 输出详细的验证失败原因
                    if (CurrentStep == 1 && !ValidateStep1(out var e1))
                        System.Diagnostics.Debug.WriteLine($"步骤1验证失败: {e1}");
                    else if (CurrentStep == 2 && !ValidateStep2(out var e2))
                        System.Diagnostics.Debug.WriteLine($"步骤2验证失败: {e2}");
                    else if (CurrentStep == 3 && !ValidateStep3(out var e3))
                        System.Diagnostics.Debug.WriteLine($"步骤3验证失败: {e3}");
                }
            }
        }

        /// <summary>
        /// 返回到上一步
        /// </summary>
        private void PreviousStep()
        {
            if (CurrentStep > 1)
            {
                // 如果模板已应用，防止回退到人员/岗位/约束步骤，只能回到第1步
                if (TemplateApplied && CurrentStep > 1 && CurrentStep <= 6)
                {
                    CurrentStep = 1;
                }
                else
                {
                    CurrentStep--;
                }
                RefreshCommandStates();
            }
        }

        /// <summary>
        /// 判断是否可以前进到下一步
        /// </summary>
        private bool CanGoNext() => CurrentStep switch
        {
            1 => ValidateStep1(out _),
            2 => ValidateStep2(out _),
            3 => ValidateStep3(out _),
            4 => true, // Constraints step is always navigable if reached
            5 => true, // Algorithm config step is always navigable if reached
            6 => false, // Cannot go next from summary
            _ => false
        };

        /// <summary>
        /// 刷新命令状态
        /// </summary>
        private void RefreshCommandStates()
        {
            NextStepCommand.NotifyCanExecuteChanged();
            PreviousStepCommand.NotifyCanExecuteChanged();
            ExecuteSchedulingCommand.NotifyCanExecuteChanged();
            SaveAsTemplateCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region 步骤验证方法

        /// <summary>
        /// 验证步骤1：基本信息
        /// </summary>
        private bool ValidateStep1([NotNullWhen(false)] out string? error)
        {
            if (string.IsNullOrWhiteSpace(ScheduleTitle)) { error = "排班标题不能为空"; return false; }
            if (ScheduleTitle.Length > 100) { error = "排班标题长度不能超过100字符"; return false; }
            if (StartDate.Date < DateTimeOffset.Now.Date) { error = "开始日期不能早于今天"; return false; }
            if (EndDate.Date < StartDate.Date) { error = "结束日期不能早于开始日期"; return false; }
            if ((EndDate.Date - StartDate.Date).TotalDays + 1 > 365) { error = "排班周期不能超过365天"; return false; }
            error = null; return true;
        }

        /// <summary>
        /// 验证步骤2：哨位选择
        /// </summary>
        private bool ValidateStep2([NotNullWhen(false)] out string? error)
        {
            // Step 2 now validates positions (swapped with step 3)
            if (SelectedPositions == null || SelectedPositions.Count == 0) { error = "请至少选择一个岗位"; return false; }
            error = null; return true;
        }

        /// <summary>
        /// 验证步骤3：人员选择
        /// </summary>
        private bool ValidateStep3([NotNullWhen(false)] out string? error)
        {
            // Step 3 now validates personnel (swapped with step 2)
            if (SelectedPersonnels == null || SelectedPersonnels.Count == 0) { error = "请至少选择一名人员"; return false; }
            if (SelectedPersonnels.Any(p => !p.IsAvailable || p.IsRetired)) { error = "选择的人员有不可用人员，请移除"; return false; }
            error = null; return true;
        }

        /// <summary>
        /// 判断是否可以执行排班
        /// </summary>
        private bool CanExecuteScheduling()
        {
            var step6 = CurrentStep == 6;
            var notExecuting = !IsExecuting;
            var step1Valid = ValidateStep1(out var step1Error);
            var step2Valid = ValidateStep2(out var step2Error);
            var step3Valid = ValidateStep3(out var step3Error);
            
            var canExecute = step6 && notExecuting && step1Valid && step2Valid && step3Valid;
            
            // 只在步骤6且验证失败时输出调试信息
            if (step6 && !canExecute)
            {
                System.Diagnostics.Debug.WriteLine($"=== 开始排班按钮被禁用 ===");
                if (IsExecuting)
                    System.Diagnostics.Debug.WriteLine($"原因: 正在执行排班");
                if (!step1Valid)
                    System.Diagnostics.Debug.WriteLine($"原因: 步骤1验证失败 - {step1Error}");
                if (!step2Valid)
                    System.Diagnostics.Debug.WriteLine($"原因: 步骤2验证失败 - {step2Error}");
                if (!step3Valid)
                    System.Diagnostics.Debug.WriteLine($"原因: 步骤3验证失败 - {step3Error}");
            }
            
            return canExecute;
        }

        #endregion

        #region 执行排班方法

        /// <summary>
        /// 执行排班
        /// </summary>
        private async Task ExecuteSchedulingAsync()
        {
            if (!CanExecuteScheduling())
            {
                if (!ValidateStep1(out var e1)) await _dialogService.ShowWarningAsync(e1);
                else if (!ValidateStep2(out var e2)) await _dialogService.ShowWarningAsync(e2);
                else if (!ValidateStep3(out var e3)) await _dialogService.ShowWarningAsync(e3);
                return;
            }
            
            try
            {
                // 保存算法配置
                await AlgorithmConfigViewModel.SaveConfigAsync();
                
                // 在后台线程构建请求，避免UI冻结
                var request = await Task.Run(() => BuildSchedulingRequest());
                
                System.Diagnostics.Debug.WriteLine($"准备导航到排班进度页面: {request.Title}");
                System.Diagnostics.Debug.WriteLine($"人员数: {request.PersonnelIds.Count}, 哨位数: {request.PositionIds.Count}");
                System.Diagnostics.Debug.WriteLine($"排班模式: {request.SchedulingMode}");
                
                // 验证请求数据的有效性
                if (request.PersonnelIds == null || !request.PersonnelIds.Any())
                {
                    await _dialogService.ShowWarningAsync("没有选择任何人员，无法开始排班");
                    return;
                }
                
                if (request.PositionIds == null || !request.PositionIds.Any())
                {
                    await _dialogService.ShowWarningAsync("没有选择任何哨位，无法开始排班");
                    return;
                }
                
                // 导航到排班进度可视化页面，传递 SchedulingRequestDto 参数
                _navigation_service.NavigateTo("SchedulingProgress", request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"执行排班失败: {ex.Message}");
                await _dialogService.ShowErrorAsync("执行失败", $"启动排班时发生错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 构建排班请求
        /// </summary>
        private SchedulingRequestDto BuildSchedulingRequest()
        {
            // 获取所有启用的手动指定
            var allEnabledAssignments = _manualAssignmentManager.GetAllEnabled();
            
            // 分离已保存的和临时的手动指定
            var enabledManualAssignmentIds = allEnabledAssignments
                .Where(a => a.Id.HasValue)
                .Select(a => a.Id!.Value)
                .ToList();
            
            var temporaryManualAssignments = allEnabledAssignments
                .Where(a => !a.Id.HasValue)
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"BuildSchedulingRequest - 已保存的手动指定: {enabledManualAssignmentIds.Count}, 临时手动指定: {temporaryManualAssignments.Count}");
            
            return new SchedulingRequestDto
            {
                Title = ScheduleTitle.Trim(),
                StartDate = StartDate.DateTime.Date,
                EndDate = EndDate.DateTime.Date,
                PersonnelIds = SelectedPersonnels.Select(p => p.Id).ToList(),
                PositionIds = SelectedPositions.Select(p => p.Id).ToList(),
                UseActiveHolidayConfig = UseActiveHolidayConfig,
                HolidayConfigId = UseActiveHolidayConfig ? null : SelectedHolidayConfigId,
                EnabledFixedRuleIds = FixedPositionRules.Where(r => r.IsEnabled).Select(r => r.Id).ToList(),
                EnabledManualAssignmentIds = enabledManualAssignmentIds,
                TemporaryManualAssignments = temporaryManualAssignments,
                SchedulingMode = AlgorithmConfigViewModel.SelectedMode
            };
        }

        #endregion

        #region 取消和重置方法

        /// <summary>
        /// 取消向导
        /// </summary>
        private async void CancelWizard()
        {
            // 检查是否有值得保存的进度
            if (ShouldPromptForDraftSave())
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "保留进度",
                    Content = "当前进度将保存为草稿，是否保留？下次可以继续编辑。",
                    PrimaryButtonText = "保留",
                    SecondaryButtonText = "放弃",
                    DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                    XamlRoot = App.MainWindow?.Content?.XamlRoot
                };

                if (dialog.XamlRoot != null)
                {
                    try
                    {
                        var result = await dialog.ShowAsync();

                        if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Secondary)
                        {
                            // 用户选择放弃，删除草稿
                            if (_draftService != null)
                            {
                                try
                                {
                                    await _draftService.DeleteDraftAsync();
                                    System.Diagnostics.Debug.WriteLine("[SchedulingViewModel] Draft deleted after user chose to discard");
                                }
                                catch (Exception draftEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel] Failed to delete draft: {draftEx.Message}");
                                    // 不影响主流程，仅记录日志
                                }
                            }
                        }
                        // 如果选择保留，草稿会在OnNavigatedFrom时自动保存
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel] Failed to show cancel dialog: {ex.Message}");
                        // 如果对话框显示失败，继续执行重置逻辑
                    }
                }
            }

            // 重置状态
            ResetViewModelState();
        }

        /// <summary>
        /// 重置视图模型状态
        /// </summary>
        private void ResetViewModelState()
        {
            CurrentStep = 1;
            ScheduleTitle = string.Empty;
            SelectedPersonnels.Clear();
            SelectedPositions.Clear();
            EnabledFixedRules.Clear();
            EnabledManualAssignments.Clear();
            FixedPositionRules.Clear();
            ManualAssignments.Clear();
            HolidayConfigs.Clear();
            ResultSchedule = null;
            SelectedHolidayConfigId = null;
            UseActiveHolidayConfig = true;
            LoadedTemplateId = null;
            TemplateApplied = false;
            SummarySections.Clear();

            // 清空手动指定管理器
            _manualAssignmentManager.Clear();

            // 清空哨位人员管理器
            _positionPersonnelManager.Clear();

            // 清空手动添加的人员列表
            ManuallyAddedPersonnelIds.Clear();
            ManuallyAddedPersonnelDetails.Clear();
            AutoExtractedPersonnelCount = 0;
            ManuallyAddedPersonnelCount = 0;

            RefreshCommandStates();
        }

        /// <summary>
        /// 判断是否应该提示用户保存草稿
        /// </summary>
        private bool ShouldPromptForDraftSave()
        {
            // 如果已经成功创建排班，不需要保存草稿
            if (ResultSchedule != null)
            {
                return false;
            }

            // 检查是否有值得保存的进度
            return !string.IsNullOrWhiteSpace(ScheduleTitle) ||
                   SelectedPersonnels.Count > 0 ||
                   SelectedPositions.Count > 0 ||
                   AllManualAssignments.Count > 0;
        }

        #endregion

        #region 摘要构建方法

        /// <summary>
        /// 构建第5步摘要信息
        /// </summary>
        private void BuildSummarySections()
        {
            var sections = new System.Collections.Generic.List<SummarySection>();
            
            // 基本信息
            var basic = new SummarySection { Header = "基本信息" };
            basic.Lines.Add($"排班标题: {ScheduleTitle}");
            basic.Lines.Add($"日期范围: {StartDate:yyyy-MM-dd} 到 {EndDate:yyyy-MM-dd} (共计 {(EndDate.Date - StartDate.Date).TotalDays + 1} 天) ");
            sections.Add(basic);
            
            // 人员
            var per = new SummarySection { Header = $"参与人员 ({SelectedPersonnels.Count})" };
            foreach (var p in SelectedPersonnels.Take(20)) // 限制预览
                per.Lines.Add($"{p.Name} (ID:{p.Id})");
            if (SelectedPersonnels.Count > 20) per.Lines.Add($"... 等 {SelectedPersonnels.Count} 人");
            sections.Add(per);
            
            // 岗位
            var pos = new SummarySection { Header = $"涉及岗位 ({SelectedPositions.Count})" };
            foreach (var p in SelectedPositions.Take(20))
                pos.Lines.Add($"{p.Name} (ID:{p.Id})");
            if (SelectedPositions.Count > 20) pos.Lines.Add($"... 等 {SelectedPositions.Count} 个岗位");
            sections.Add(pos);
            
            // 约束
            var cons = new SummarySection { Header = "约束配置" };
            cons.Lines.Add(UseActiveHolidayConfig ? "节假日配置: 使用当前活动配置" : $"节假日配置: 自定义配置ID={SelectedHolidayConfigId}");
            var enabledRulesCount = FixedPositionRules.Count(r => r.IsEnabled);
            var enabledAssignmentsCount = ManualAssignments.Count(a => a.IsEnabled);
            cons.Lines.Add($"固定岗位规则: {enabledRulesCount} 条");
            cons.Lines.Add($"手动指定排班: {enabledAssignmentsCount} 条");
            sections.Add(cons);
            
            // 手动指定详情
            var manualAssignmentSection = new SummarySection { Header = "手动指定" };
            var allEnabledAssignments = _manualAssignmentManager.GetAllEnabled();
            
            if (allEnabledAssignments.Count > 0)
            {
                // 按日期和时段排序
                var sortedAssignments = allEnabledAssignments
                    .OrderBy(a => a.Date)
                    .ThenBy(a => a.TimeSlot)
                    .ToList();
                
                foreach (var assignment in sortedAssignments)
                {
                    // 获取人员和哨位名称
                    var personnelName = SelectedPersonnels.FirstOrDefault(p => p.Id == assignment.PersonnelId)?.Name ?? $"人员ID:{assignment.PersonnelId}";
                    var positionName = SelectedPositions.FirstOrDefault(p => p.Id == assignment.PositionId)?.Name ?? $"哨位ID:{assignment.PositionId}";
                    
                    // 格式化时段显示
                    var startHour = assignment.TimeSlot * 2;
                    var endHour = (assignment.TimeSlot + 1) * 2;
                    var timeSlotDisplay = $"时段 {assignment.TimeSlot} ({startHour:D2}:00-{endHour:D2}:00)";
                    
                    // 构建详细信息行
                    var detailLine = $"{assignment.Date:yyyy-MM-dd} | {timeSlotDisplay} | {personnelName} → {positionName}";
                    
                    // 如果有描述，添加到详细信息中
                    if (!string.IsNullOrWhiteSpace(assignment.Remarks))
                    {
                        detailLine += $" | 备注: {assignment.Remarks}";
                    }
                    
                    manualAssignmentSection.Lines.Add(detailLine);
                }
            }
            else
            {
                manualAssignmentSection.Lines.Add("无启用的手动指定");
            }
            
            sections.Add(manualAssignmentSection);
            
            // 哨位临时更改摘要
            var positionChangesSection = new SummarySection { Header = "哨位临时更改" };
            var positionsWithChanges = _positionPersonnelManager.GetPositionsWithChanges();
            
            if (positionsWithChanges.Count > 0)
            {
                foreach (var positionId in positionsWithChanges)
                {
                    var changes = _positionPersonnelManager.GetChanges(positionId);
                    if (changes.HasChanges)
                    {
                        positionChangesSection.Lines.Add($"哨位: {changes.PositionName}");
                        
                        // 显示添加的人员
                        if (changes.AddedPersonnelIds.Count > 0)
                        {
                            positionChangesSection.Lines.Add($"  ➕ 添加人员: {string.Join(", ", changes.AddedPersonnelNames)}");
                        }
                        
                        // 显示移除的人员
                        if (changes.RemovedPersonnelIds.Count > 0)
                        {
                            positionChangesSection.Lines.Add($"  ➖ 移除人员: {string.Join(", ", changes.RemovedPersonnelNames)}");
                        }
                    }
                }
            }
            else
            {
                positionChangesSection.Lines.Add("无临时更改");
            }
            
            sections.Add(positionChangesSection);
            
            // 手动添加人员摘要
            var manuallyAddedPersonnelSection = new SummarySection { Header = "手动添加的人员（对所有哨位可用）" };
            
            if (ManuallyAddedPersonnelIds.Count > 0)
            {
                foreach (var personnelId in ManuallyAddedPersonnelIds)
                {
                    var personnel = GetPersonnelFromCache(personnelId);
                    if (personnel != null)
                    {
                        var skillsDisplay = personnel.SkillNames != null && personnel.SkillNames.Any() 
                            ? $" (技能: {string.Join(", ", personnel.SkillNames)})" 
                            : "";
                        manuallyAddedPersonnelSection.Lines.Add($"{personnel.Name}{skillsDisplay}");
                    }
                    else
                    {
                        manuallyAddedPersonnelSection.Lines.Add($"人员ID: {personnelId} (未找到详细信息)");
                    }
                }
                manuallyAddedPersonnelSection.Lines.Add("");
                manuallyAddedPersonnelSection.Lines.Add("说明: 这些人员可以被分配到任何哨位，不受哨位可用人员列表限制");
            }
            else
            {
                manuallyAddedPersonnelSection.Lines.Add("无手动添加的人员");
            }
            
            sections.Add(manuallyAddedPersonnelSection);
            
            // 模板信息
            if (TemplateApplied && LoadedTemplateId.HasValue)
            {
                sections.Add(new SummarySection { Header = "模板来源", Lines = { $"来源模板ID: {LoadedTemplateId}" } });
            }
            
            // 更新摘要集合
            SummarySections = new System.Collections.ObjectModel.ObservableCollection<SummarySection>(sections);
        }

        #endregion
    }
}
