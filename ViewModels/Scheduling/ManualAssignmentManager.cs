using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling
{
    /// <summary>
    /// 手动指定管理器 - 管理临时和已保存的手动指定
    /// </summary>
    public class ManualAssignmentManager
    {
        /// <summary>
        /// 临时手动指定列表（未保存到数据库）
        /// </summary>
        public ObservableCollection<ManualAssignmentViewModel> TemporaryAssignments { get; }

        /// <summary>
        /// 已保存的手动指定列表（从数据库加载或已持久化）
        /// </summary>
        public ObservableCollection<ManualAssignmentViewModel> SavedAssignments { get; }

        /// <summary>
        /// 所有手动指定（合并视图）
        /// </summary>
        public ObservableCollection<ManualAssignmentViewModel> AllAssignments { get; }

        public ManualAssignmentManager()
        {
            TemporaryAssignments = new ObservableCollection<ManualAssignmentViewModel>();
            SavedAssignments = new ObservableCollection<ManualAssignmentViewModel>();
            AllAssignments = new ObservableCollection<ManualAssignmentViewModel>();

            // 监听临时和已保存列表的变化，自动更新合并视图
            TemporaryAssignments.CollectionChanged += (s, e) => RefreshAllAssignments();
            SavedAssignments.CollectionChanged += (s, e) => RefreshAllAssignments();
        }

        /// <summary>
        /// 添加临时手动指定
        /// </summary>
        /// <param name="dto">创建手动指定DTO</param>
        /// <param name="personnelName">人员姓名</param>
        /// <param name="positionName">哨位名称</param>
        public void AddTemporary(CreateManualAssignmentDto dto, string personnelName, string positionName)
        {
            var viewModel = new ManualAssignmentViewModel
            {
                Id = null, // 临时手动指定没有数据库ID
                TempId = Guid.NewGuid(),
                Date = dto.Date,
                PersonnelId = dto.PersonnelId,
                PersonnelName = personnelName,
                PositionId = dto.PositionId,
                PositionName = positionName,
                TimeSlot = dto.TimeSlot,
                Remarks = dto.Remarks ?? string.Empty,
                IsEnabled = dto.IsEnabled
            };

            TemporaryAssignments.Add(viewModel);
        }

        /// <summary>
        /// 更新临时手动指定
        /// </summary>
        /// <param name="tempId">临时ID</param>
        /// <param name="dto">更新手动指定DTO</param>
        /// <param name="personnelName">人员姓名</param>
        /// <param name="positionName">哨位名称</param>
        public void UpdateTemporary(Guid tempId, UpdateManualAssignmentDto dto, string personnelName, string positionName)
        {
            var existing = TemporaryAssignments.FirstOrDefault(a => a.TempId == tempId);
            if (existing == null)
                return;

            existing.Date = dto.Date;
            existing.PersonnelId = dto.PersonnelId;
            existing.PersonnelName = personnelName;
            existing.PositionId = dto.PositionId;
            existing.PositionName = positionName;
            existing.TimeSlot = dto.TimeSlot;
            existing.Remarks = dto.Remarks ?? string.Empty;
            existing.IsEnabled = dto.IsEnabled;

            // 触发UI更新
            RefreshAllAssignments();
        }

        /// <summary>
        /// 删除临时手动指定
        /// </summary>
        /// <param name="tempId">临时ID</param>
        public void RemoveTemporary(Guid tempId)
        {
            var existing = TemporaryAssignments.FirstOrDefault(a => a.TempId == tempId);
            if (existing != null)
            {
                TemporaryAssignments.Remove(existing);
            }
        }

        /// <summary>
        /// 加载已保存的手动指定
        /// </summary>
        /// <param name="assignments">手动指定DTO列表</param>
        public void LoadSaved(IEnumerable<ManualAssignmentDto> assignments)
        {
            SavedAssignments.Clear();

            foreach (var dto in assignments)
            {
                var viewModel = new ManualAssignmentViewModel
                {
                    Id = dto.Id,
                    TempId = Guid.Empty, // 已保存的手动指定不需要临时ID
                    Date = dto.Date,
                    PersonnelId = dto.PersonnelId,
                    PersonnelName = dto.PersonnelName,
                    PositionId = dto.PositionId,
                    PositionName = dto.PositionName,
                    TimeSlot = dto.TimeSlot,
                    Remarks = dto.Remarks ?? string.Empty,
                    IsEnabled = dto.IsEnabled
                };

                SavedAssignments.Add(viewModel);
            }
        }

        /// <summary>
        /// 将临时手动指定标记为已保存
        /// </summary>
        /// <param name="tempIdToSavedIdMap">临时ID到已保存ID的映射</param>
        public void MarkAsSaved(Dictionary<Guid, int> tempIdToSavedIdMap)
        {
            var toMove = new List<ManualAssignmentViewModel>();

            foreach (var kvp in tempIdToSavedIdMap)
            {
                var tempId = kvp.Key;
                var savedId = kvp.Value;

                var temp = TemporaryAssignments.FirstOrDefault(a => a.TempId == tempId);
                if (temp != null)
                {
                    // 更新ID，标记为已保存
                    temp.Id = savedId;
                    toMove.Add(temp);
                }
            }

            // 从临时列表移动到已保存列表
            foreach (var item in toMove)
            {
                TemporaryAssignments.Remove(item);
                SavedAssignments.Add(item);
            }
        }

        /// <summary>
        /// 获取所有启用的手动指定（用于排班请求）
        /// </summary>
        /// <returns>手动指定请求项列表</returns>
        public List<ManualAssignmentRequestItem> GetAllEnabled()
        {
            var result = new List<ManualAssignmentRequestItem>();

            // 添加已保存的启用手动指定
            foreach (var saved in SavedAssignments.Where(a => a.IsEnabled))
            {
                result.Add(new ManualAssignmentRequestItem
                {
                    Id = saved.Id,
                    Date = saved.Date,
                    PersonnelId = saved.PersonnelId,
                    PositionId = saved.PositionId,
                    TimeSlot = saved.TimeSlot,
                    Remarks = saved.Remarks
                });
            }

            // 添加临时的启用手动指定
            foreach (var temp in TemporaryAssignments.Where(a => a.IsEnabled))
            {
                result.Add(new ManualAssignmentRequestItem
                {
                    Id = null, // 临时手动指定没有ID
                    Date = temp.Date,
                    PersonnelId = temp.PersonnelId,
                    PositionId = temp.PositionId,
                    TimeSlot = temp.TimeSlot,
                    Remarks = temp.Remarks
                });
            }

            return result;
        }

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            TemporaryAssignments.Clear();
            SavedAssignments.Clear();
        }

        /// <summary>
        /// 刷新合并视图
        /// </summary>
        private void RefreshAllAssignments()
        {
            AllAssignments.Clear();

            // 先添加已保存的手动指定
            foreach (var saved in SavedAssignments)
            {
                AllAssignments.Add(saved);
            }

            // 再添加临时手动指定
            foreach (var temp in TemporaryAssignments)
            {
                AllAssignments.Add(temp);
            }
        }
    }
}
