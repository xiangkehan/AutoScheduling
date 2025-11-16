using System;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// SchedulingViewModel 的草稿管理部分类
    /// 负责排班创建进度的保存、恢复和验证
    /// </summary>
    public partial class SchedulingViewModel
    {
        private ISchedulingDraftService? _draftService;

        /// <summary>
        /// 注入草稿服务依赖
        /// 此方法应在ViewModel初始化后调用
        /// </summary>
        public void InjectDraftService(ISchedulingDraftService draftService)
        {
            _draftService = draftService ?? throw new ArgumentNullException(nameof(draftService));
            System.Diagnostics.Debug.WriteLine("[SchedulingViewModel.Draft] Draft service injected");
        }

        /// <summary>
        /// 创建草稿 - 收集当前排班创建状态
        /// 
        /// 收集的数据包括：
        /// - 基本信息（标题、日期范围、当前步骤）
        /// - 模板信息（是否应用模板、模板ID）
        /// - 选择数据（人员ID、岗位ID）
        /// - 约束配置（休息日配置、固定规则、手动指定）
        /// - 临时手动指定
        /// 
        /// 需求: 1.1, 1.2
        /// </summary>
        public async Task<SchedulingDraftDto> CreateDraftAsync()
        {
            System.Diagnostics.Debug.WriteLine("[SchedulingViewModel.Draft] Creating draft...");

            var draft = new SchedulingDraftDto
            {
                // 基本信息
                ScheduleTitle = ScheduleTitle,
                StartDate = StartDate.DateTime.Date,
                EndDate = EndDate.DateTime.Date,
                CurrentStep = CurrentStep,

                // 模板信息
                TemplateApplied = TemplateApplied,
                LoadedTemplateId = LoadedTemplateId,

                // 选择的人员和岗位
                SelectedPersonnelIds = SelectedPersonnels.Select(p => p.Id).ToList(),
                SelectedPositionIds = SelectedPositions.Select(p => p.Id).ToList(),

                // 约束配置
                UseActiveHolidayConfig = UseActiveHolidayConfig,
                SelectedHolidayConfigId = SelectedHolidayConfigId,
                EnabledFixedRuleIds = FixedPositionRules.Where(r => r.IsEnabled).Select(r => r.Id).ToList(),
                EnabledManualAssignmentIds = ManualAssignments.Where(a => a.IsEnabled).Select(a => a.Id).ToList(),

                // 临时手动指定
                TemporaryManualAssignments = _manualAssignmentManager.TemporaryAssignments
                    .Select(t => new ManualAssignmentDraftDto
                    {
                        Date = t.Date,
                        PersonnelId = t.PersonnelId,
                        PositionId = t.PositionId,
                        TimeSlot = t.TimeSlot,
                        Remarks = t.Remarks ?? string.Empty,
                        IsEnabled = t.IsEnabled,
                        TempId = t.TempId
                    }).ToList()
            };

            System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Draft created:");
            System.Diagnostics.Debug.WriteLine($"  - Title: {draft.ScheduleTitle}");
            System.Diagnostics.Debug.WriteLine($"  - Date Range: {draft.StartDate:yyyy-MM-dd} to {draft.EndDate:yyyy-MM-dd}");
            System.Diagnostics.Debug.WriteLine($"  - Current Step: {draft.CurrentStep}");
            System.Diagnostics.Debug.WriteLine($"  - Template Applied: {draft.TemplateApplied}");
            System.Diagnostics.Debug.WriteLine($"  - Personnel: {draft.SelectedPersonnelIds.Count}");
            System.Diagnostics.Debug.WriteLine($"  - Positions: {draft.SelectedPositionIds.Count}");
            System.Diagnostics.Debug.WriteLine($"  - Enabled Fixed Rules: {draft.EnabledFixedRuleIds.Count}");
            System.Diagnostics.Debug.WriteLine($"  - Enabled Manual Assignments: {draft.EnabledManualAssignmentIds.Count}");
            System.Diagnostics.Debug.WriteLine($"  - Temporary Manual Assignments: {draft.TemporaryManualAssignments.Count}");

            await Task.CompletedTask;
            return draft;
        }

        /// <summary>
        /// 从草稿恢复状态
        /// 
        /// 恢复流程：
        /// 1. 验证草稿数据有效性
        /// 2. 加载基础数据（如果未加载）
        /// 3. 恢复基本属性
        /// 4. 恢复选中的人员和岗位
        /// 5. 恢复约束配置
        /// 6. 恢复手动指定
        /// 
        /// 需求: 1.4, 5.1, 5.2, 5.5
        /// </summary>
        public async Task RestoreFromDraftAsync(SchedulingDraftDto draft)
        {
            System.Diagnostics.Debug.WriteLine("[SchedulingViewModel.Draft] Restoring from draft...");

            try
            {
                // 1. 验证草稿数据
                if (!await ValidateDraftDataAsync(draft))
                {
                    System.Diagnostics.Debug.WriteLine("[SchedulingViewModel.Draft] Draft validation failed, aborting restore");
                    return;
                }

                // 2. 加载基础数据（如果未加载）
                if (AvailablePersonnels.Count == 0 || AvailablePositions.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[SchedulingViewModel.Draft] Loading base data...");
                    await LoadInitialDataAsync();
                }

                // 3. 恢复基本属性
                System.Diagnostics.Debug.WriteLine("[SchedulingViewModel.Draft] Restoring basic properties...");
                ScheduleTitle = draft.ScheduleTitle;
                StartDate = new DateTimeOffset(draft.StartDate);
                EndDate = new DateTimeOffset(draft.EndDate);
                CurrentStep = draft.CurrentStep;

                // 4. 恢复模板信息
                TemplateApplied = draft.TemplateApplied;
                LoadedTemplateId = draft.LoadedTemplateId;

                // 5. 恢复选中的人员和岗位
                await RestoreSelectedPersonnelAndPositionsAsync(draft);

                // 6. 恢复约束配置
                await RestoreConstraintsAsync(draft);

                // 7. 恢复手动指定
                await RestoreManualAssignmentsAsync(draft);

                System.Diagnostics.Debug.WriteLine("[SchedulingViewModel.Draft] Draft restored successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Failed to restore draft: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Stack trace: {ex.StackTrace}");
                await _dialogService.ShowErrorAsync("恢复草稿失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 验证草稿数据有效性
        /// 
        /// 验证项：
        /// - 版本兼容性
        /// - 日期有效性（调整过期日期）
        /// - 模板引用完整性
        /// 
        /// 需求: 5.1, 5.2, 5.3, 5.4, 5.5
        /// </summary>
        private async Task<bool> ValidateDraftDataAsync(SchedulingDraftDto draft)
        {
            System.Diagnostics.Debug.WriteLine("[SchedulingViewModel.Draft] Validating draft data...");

            // 验证版本兼容性
            if (draft.Version != "1.0")
            {
                System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Version mismatch: expected 1.0, got {draft.Version}");
                await _dialogService.ShowWarningAsync("草稿版本不兼容，无法恢复");
                return false;
            }

            // 验证日期有效性 - 如果开始日期早于今天，自动调整
            if (draft.StartDate.Date < DateTime.Now.Date)
            {
                System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Start date {draft.StartDate:yyyy-MM-dd} is in the past, adjusting to today");
                
                var daysDiff = (draft.EndDate.Date - draft.StartDate.Date).Days;
                draft.StartDate = DateTime.Now.Date;
                draft.EndDate = draft.StartDate.AddDays(daysDiff);
                
                await _dialogService.ShowWarningAsync($"开始日期已过期，已自动调整为今天\n新的日期范围：{draft.StartDate:yyyy-MM-dd} 到 {draft.EndDate:yyyy-MM-dd}");
            }

            // 验证模板引用（如果使用了模板）
            if (draft.TemplateApplied && draft.LoadedTemplateId.HasValue)
            {
                try
                {
                    var template = await _templateService.GetByIdAsync(draft.LoadedTemplateId.Value);
                    if (template == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Template {draft.LoadedTemplateId} not found, switching to manual mode");
                        await _dialogService.ShowWarningAsync("模板已不存在，将以手动模式继续");
                        draft.TemplateApplied = false;
                        draft.LoadedTemplateId = null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Failed to validate template: {ex.Message}");
                    await _dialogService.ShowWarningAsync("无法验证模板，将以手动模式继续");
                    draft.TemplateApplied = false;
                    draft.LoadedTemplateId = null;
                }
            }

            System.Diagnostics.Debug.WriteLine("[SchedulingViewModel.Draft] Draft validation passed");
            return true;
        }
    }
}

        /// <summary>
        /// 恢复选中的人员和岗位
        /// 
        /// 恢复流程：
        /// 1. 从可用列表中筛选出草稿中的人员和岗位
        /// 2. 验证是否有缺失的人员或岗位
        /// 3. 如果有缺失，显示警告信息
        /// 4. 更新选中列表
        /// 
        /// 需求: 1.4, 5.5
        /// </summary>
        private async Task RestoreSelectedPersonnelAndPositionsAsync(SchedulingDraftDto draft)
        {
            System.Diagnostics.Debug.WriteLine("[SchedulingViewModel.Draft] Restoring selected personnel and positions...");

            // 恢复选中的人员
            var selectedPersonnels = AvailablePersonnels
                .Where(p => draft.SelectedPersonnelIds.Contains(p.Id))
                .ToList();

            var missingPersonnelIds = draft.SelectedPersonnelIds
                .Except(selectedPersonnels.Select(p => p.Id))
                .ToList();

            if (missingPersonnelIds.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Missing personnel IDs: {string.Join(", ", missingPersonnelIds)}");
                await _dialogService.ShowWarningAsync(
                    $"部分人员已不存在，已自动移除\n缺失人员ID: {string.Join(", ", missingPersonnelIds)}");
            }

            SelectedPersonnels.Clear();
            foreach (var personnel in selectedPersonnels)
            {
                SelectedPersonnels.Add(personnel);
            }

            System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Restored {SelectedPersonnels.Count} personnel (missing: {missingPersonnelIds.Count})");

            // 恢复选中的岗位
            var selectedPositions = AvailablePositions
                .Where(p => draft.SelectedPositionIds.Contains(p.Id))
                .ToList();

            var missingPositionIds = draft.SelectedPositionIds
                .Except(selectedPositions.Select(p => p.Id))
                .ToList();

            if (missingPositionIds.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Missing position IDs: {string.Join(", ", missingPositionIds)}");
                await _dialogService.ShowWarningAsync(
                    $"部分岗位已不存在，已自动移除\n缺失岗位ID: {string.Join(", ", missingPositionIds)}");
            }

            SelectedPositions.Clear();
            foreach (var position in selectedPositions)
            {
                SelectedPositions.Add(position);
            }

            System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Restored {SelectedPositions.Count} positions (missing: {missingPositionIds.Count})");
        }

        /// <summary>
        /// 恢复约束配置
        /// 
        /// 恢复流程：
        /// 1. 恢复休息日配置
        /// 2. 加载约束数据（如果未加载）
        /// 3. 恢复固定规则启用状态
        /// 4. 恢复手动指定启用状态（仅已保存的）
        /// 5. 验证并报告缺失的约束
        /// 
        /// 需求: 1.4, 4.3, 4.4
        /// </summary>
        private async Task RestoreConstraintsAsync(SchedulingDraftDto draft)
        {
            System.Diagnostics.Debug.WriteLine("[SchedulingViewModel.Draft] Restoring constraints...");

            // 恢复休息日配置
            UseActiveHolidayConfig = draft.UseActiveHolidayConfig;
            SelectedHolidayConfigId = draft.SelectedHolidayConfigId;

            System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Holiday config - UseActive: {UseActiveHolidayConfig}, SelectedId: {SelectedHolidayConfigId}");

            // 加载约束数据（如果未加载）
            if (FixedPositionRules.Count == 0 && ManualAssignments.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[SchedulingViewModel.Draft] Loading constraints data...");
                await LoadConstraintsAsync();
            }

            // 恢复固定规则启用状态
            var missingFixedRuleIds = draft.EnabledFixedRuleIds
                .Except(FixedPositionRules.Select(r => r.Id))
                .ToList();

            foreach (var rule in FixedPositionRules)
            {
                rule.IsEnabled = draft.EnabledFixedRuleIds.Contains(rule.Id);
            }

            var enabledFixedRulesCount = FixedPositionRules.Count(r => r.IsEnabled);
            System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Restored {enabledFixedRulesCount} enabled fixed rules (missing: {missingFixedRuleIds.Count})");

            if (missingFixedRuleIds.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Missing fixed rule IDs: {string.Join(", ", missingFixedRuleIds)}");
            }

            // 恢复手动指定启用状态（仅已保存的）
            var missingManualAssignmentIds = draft.EnabledManualAssignmentIds
                .Except(ManualAssignments.Select(a => a.Id))
                .ToList();

            foreach (var assignment in ManualAssignments)
            {
                assignment.IsEnabled = draft.EnabledManualAssignmentIds.Contains(assignment.Id);
            }

            var enabledManualAssignmentsCount = ManualAssignments.Count(a => a.IsEnabled);
            System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Restored {enabledManualAssignmentsCount} enabled manual assignments (missing: {missingManualAssignmentIds.Count})");

            if (missingManualAssignmentIds.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Missing manual assignment IDs: {string.Join(", ", missingManualAssignmentIds)}");
            }

            // 显示缺失约束的警告
            if (missingFixedRuleIds.Any() || missingManualAssignmentIds.Any())
            {
                var warningMsg = "部分约束已不存在，已自动移除：\n";
                if (missingFixedRuleIds.Any())
                {
                    warningMsg += $"- 缺失固定规则: {missingFixedRuleIds.Count} 条\n";
                }
                if (missingManualAssignmentIds.Any())
                {
                    warningMsg += $"- 缺失手动指定: {missingManualAssignmentIds.Count} 条\n";
                }

                await _dialogService.ShowWarningAsync(warningMsg);
            }
        }

        /// <summary>
        /// 恢复手动指定
        /// 
        /// 恢复流程：
        /// 1. 同步已保存手动指定的启用状态到ManualAssignmentManager
        /// 2. 恢复临时手动指定
        /// 3. 验证人员和岗位是否存在
        /// 4. 跳过无效的手动指定并记录日志
        /// 
        /// 需求: 4.3, 4.4, 5.5
        /// </summary>
        private async Task RestoreManualAssignmentsAsync(SchedulingDraftDto draft)
        {
            System.Diagnostics.Debug.WriteLine("[SchedulingViewModel.Draft] Restoring manual assignments...");

            // 1. 同步已保存手动指定的启用状态到ManualAssignmentManager
            var manualDtos = ManualAssignments.Select(m =>
            {
                var personnel = AvailablePersonnels.FirstOrDefault(p => p.Id == m.PersonalId);
                var position = AvailablePositions.FirstOrDefault(p => p.Id == m.PositionId);

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
                    IsEnabled = draft.EnabledManualAssignmentIds.Contains(m.Id),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
            }).ToList();

            _manualAssignmentManager.LoadSaved(manualDtos);
            System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Loaded {manualDtos.Count} saved manual assignments to manager");

            // 2. 恢复临时手动指定
            var invalidCount = 0;
            foreach (var tempDto in draft.TemporaryManualAssignments)
            {
                // 验证人员和岗位是否存在
                var personnel = SelectedPersonnels.FirstOrDefault(p => p.Id == tempDto.PersonnelId);
                var position = SelectedPositions.FirstOrDefault(p => p.Id == tempDto.PositionId);

                if (personnel == null || position == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Skipping invalid temporary assignment: PersonnelId={tempDto.PersonnelId}, PositionId={tempDto.PositionId}");
                    invalidCount++;
                    continue;
                }

                var createDto = new CreateManualAssignmentDto
                {
                    Date = tempDto.Date,
                    PersonnelId = tempDto.PersonnelId,
                    PositionId = tempDto.PositionId,
                    TimeSlot = tempDto.TimeSlot,
                    Remarks = tempDto.Remarks,
                    IsEnabled = tempDto.IsEnabled
                };

                _manualAssignmentManager.AddTemporary(createDto, personnel.Name, position.Name);
            }

            System.Diagnostics.Debug.WriteLine($"[SchedulingViewModel.Draft] Restored {draft.TemporaryManualAssignments.Count - invalidCount} temporary manual assignments (invalid: {invalidCount})");

            if (invalidCount > 0)
            {
                await _dialogService.ShowWarningAsync($"部分临时手动指定无效，已自动移除（{invalidCount} 条）");
            }

            // 通知UI更新
            OnPropertyChanged(nameof(AllManualAssignments));
        }
    }
}
