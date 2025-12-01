using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// SchedulingViewModel 的状态管理部分
    /// 包含草稿保存和恢复、状态重置等逻辑
    /// </summary>
    public partial class SchedulingViewModel
    {
        #region 草稿保存和恢复

        /// <summary>
        /// 创建草稿（保存当前状态）
        /// </summary>
        public async Task CreateDraftAsync()
        {
            if (_draftService == null)
            {
                System.Diagnostics.Debug.WriteLine("草稿服务未初始化，跳过保存");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("=== 开始保存草稿 ===");

                // 收集所有哨位的临时更改
                var positionPersonnelChanges = new Dictionary<int, PositionPersonnelChangeDto>();
                var positionsWithChanges = _positionPersonnelManager.GetPositionsWithChanges();
                
                foreach (var positionId in positionsWithChanges)
                {
                    var changes = _positionPersonnelManager.GetChanges(positionId);
                    positionPersonnelChanges[positionId] = new PositionPersonnelChangeDto
                    {
                        PositionId = changes.PositionId,
                        AddedPersonnelIds = new List<int>(changes.AddedPersonnelIds),
                        RemovedPersonnelIds = new List<int>(changes.RemovedPersonnelIds)
                    };
                }

                System.Diagnostics.Debug.WriteLine($"保存 {positionPersonnelChanges.Count} 个哨位的临时更改");
                System.Diagnostics.Debug.WriteLine($"保存 {ManuallyAddedPersonnelIds.Count} 个手动添加的人员");

                // 获取所有启用的手动指定
                var allEnabledAssignments = _manualAssignmentManager.AllAssignments
                    .Where(a => a.IsEnabled)
                    .ToList();
                
                var enabledManualAssignmentIds = allEnabledAssignments
                    .Where(a => a.Id.HasValue)
                    .Select(a => a.Id.Value)
                    .ToList();
                
                var temporaryManualAssignments = allEnabledAssignments
                    .Where(a => !a.Id.HasValue)
                    .ToList();

                // 创建草稿DTO
                var draft = new SchedulingDraftDto
                {
                    ScheduleTitle = ScheduleTitle,
                    StartDate = StartDate.DateTime,
                    EndDate = EndDate.DateTime,
                    CurrentStep = CurrentStep,
                    TemplateApplied = TemplateApplied,
                    LoadedTemplateId = LoadedTemplateId,
                    SelectedPersonnelIds = SelectedPersonnels.Select(p => p.Id).ToList(),
                    SelectedPositionIds = SelectedPositions.Select(p => p.Id).ToList(),
                    UseActiveHolidayConfig = UseActiveHolidayConfig,
                    SelectedHolidayConfigId = SelectedHolidayConfigId,
                    EnabledFixedRuleIds = FixedPositionRules.Where(r => r.IsEnabled).Select(r => r.Id).ToList(),
                    EnabledManualAssignmentIds = enabledManualAssignmentIds,
                    TemporaryManualAssignments = temporaryManualAssignments
                         .Select(vm => new ManualAssignmentDraftDto
                         {
                             Date = vm.Date,
                             PersonnelId = vm.PersonnelId,
                             PositionId = vm.PositionId,
                             TimeSlot = vm.TimeSlot,
                             Remarks = vm.Remarks,
                             IsEnabled = vm.IsEnabled,
                             TempId = vm.TempId
                         })
                         .ToList(),
                    PositionPersonnelChanges = positionPersonnelChanges,
                    ManuallyAddedPersonnelIds = new List<int>(ManuallyAddedPersonnelIds),
                    SavedAt = DateTime.Now,
                    Version = "1.0"
                };

                await _draftService.SaveDraftAsync(draft);
                System.Diagnostics.Debug.WriteLine("草稿保存成功");
                System.Diagnostics.Debug.WriteLine("=== 草稿保存完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存草稿失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                // 不抛出异常，避免影响主流程
            }
        }

        /// <summary>
        /// 从草稿恢复状态
        /// </summary>
        public async Task<bool> RestoreFromDraftAsync()
        {
            if (_draftService == null)
            {
                System.Diagnostics.Debug.WriteLine("草稿服务未初始化，跳过恢复");
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("=== 开始恢复草稿 ===");

                var draft = await _draftService.LoadDraftAsync();
                if (draft == null)
                {
                    System.Diagnostics.Debug.WriteLine("没有找到草稿");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"找到草稿: {draft.ScheduleTitle}");
                System.Diagnostics.Debug.WriteLine($"草稿保存时间: {draft.SavedAt}");
                System.Diagnostics.Debug.WriteLine($"草稿步骤: {draft.CurrentStep}");

                // 加载基础数据（如果还没有加载）
                if (AvailablePersonnels.Count == 0 || AvailablePositions.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("加载基础数据");
                    await LoadInitialDataAsync();
                }

                // 恢复基本信息
                ScheduleTitle = draft.ScheduleTitle;
                StartDate = new DateTimeOffset(draft.StartDate);
                EndDate = new DateTimeOffset(draft.EndDate);
                TemplateApplied = draft.TemplateApplied;
                LoadedTemplateId = draft.LoadedTemplateId;
                UseActiveHolidayConfig = draft.UseActiveHolidayConfig;
                SelectedHolidayConfigId = draft.SelectedHolidayConfigId;

                // 恢复选中的人员和哨位
                var selectedPers = AvailablePersonnels.Where(p => draft.SelectedPersonnelIds.Contains(p.Id)).ToList();
                var selectedPos = AvailablePositions.Where(p => draft.SelectedPositionIds.Contains(p.Id)).ToList();
                
                SelectedPersonnels = new ObservableCollection<PersonnelDto>(selectedPers);
                SelectedPositions = new ObservableCollection<PositionDto>(selectedPos);

                System.Diagnostics.Debug.WriteLine($"恢复人员: {SelectedPersonnels.Count}, 哨位: {SelectedPositions.Count}");

                // 恢复约束数据（如果需要）
                if (draft.CurrentStep >= 4)
                {
                    System.Diagnostics.Debug.WriteLine("加载约束数据");
                    await LoadConstraintsAsync();

                    // 恢复约束启用状态
                    foreach (var rule in FixedPositionRules)
                    {
                        rule.IsEnabled = draft.EnabledFixedRuleIds.Contains(rule.Id);
                    }

                    // 恢复手动指定
                    _manualAssignmentManager.Clear();
                    
                    // 加载已保存的手动指定（使用缓存提高性能）
                    var savedManualAssignments = ManualAssignments
                        .Where(m => draft.EnabledManualAssignmentIds.Contains(m.Id))
                        .Select(m =>
                        {
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
                                IsEnabled = true,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
                            };
                        })
                        .ToList();
                    
                    _manualAssignmentManager.LoadSaved(savedManualAssignments);
                    
                    // 恢复临时手动指定
                    foreach (var tempAssignment in draft.TemporaryManualAssignments)
                    {
                        var personnel = GetPersonnelFromCache(tempAssignment.PersonnelId);
                        var position = GetPositionFromCache(tempAssignment.PositionId);
                        
                        var dto = new CreateManualAssignmentDto
                        {
                            Date = tempAssignment.Date,
                            PersonnelId = tempAssignment.PersonnelId,
                            PositionId = tempAssignment.PositionId,
                            TimeSlot = tempAssignment.TimeSlot,
                            Remarks = tempAssignment.Remarks,
                            IsEnabled = tempAssignment.IsEnabled
                        };
                        
                        _manualAssignmentManager.AddTemporary(dto, personnel?.Name ?? "未知人员", position?.Name ?? "未知哨位", tempAssignment.TempId);
                    }

                    System.Diagnostics.Debug.WriteLine($"恢复手动指定: 已保存 {savedManualAssignments.Count}, 临时 {draft.TemporaryManualAssignments.Count}");
                }

                // 恢复PositionPersonnelManager状态
                if (draft.PositionPersonnelChanges.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"恢复 {draft.PositionPersonnelChanges.Count} 个哨位的临时更改");
                    
                    // 初始化PositionPersonnelManager
                    _positionPersonnelManager.Initialize(SelectedPositions);
                    
                    // 恢复每个哨位的临时更改
                    foreach (var kvp in draft.PositionPersonnelChanges)
                    {
                        var positionId = kvp.Key;
                        var changes = kvp.Value;
                        
                        // 添加临时添加的人员
                        foreach (var personnelId in changes.AddedPersonnelIds)
                        {
                            _positionPersonnelManager.AddPersonnelTemporarily(positionId, personnelId);
                        }
                        
                        // 移除临时移除的人员
                        foreach (var personnelId in changes.RemovedPersonnelIds)
                        {
                            _positionPersonnelManager.RemovePersonnelTemporarily(positionId, personnelId);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"哨位 {positionId}: 添加 {changes.AddedPersonnelIds.Count}, 移除 {changes.RemovedPersonnelIds.Count}");
                    }
                }

                // 恢复手动添加的人员列表
                ManuallyAddedPersonnelIds.Clear();
                foreach (var personnelId in draft.ManuallyAddedPersonnelIds)
                {
                    ManuallyAddedPersonnelIds.Add(personnelId);
                }
                ManuallyAddedPersonnelCount = ManuallyAddedPersonnelIds.Count;

                System.Diagnostics.Debug.WriteLine($"恢复手动添加的人员: {ManuallyAddedPersonnelCount}");

                // 恢复步骤（最后设置，因为会触发OnCurrentStepChanged）
                CurrentStep = draft.CurrentStep;

                System.Diagnostics.Debug.WriteLine("草稿恢复成功");
                System.Diagnostics.Debug.WriteLine("=== 草稿恢复完成 ===");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"恢复草稿失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                await _dialogService.ShowErrorAsync("恢复草稿失败", ex);
                return false;
            }
        }

        #endregion
    }
}
