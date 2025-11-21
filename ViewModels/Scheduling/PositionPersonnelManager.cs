using System;
using System.Collections.Generic;
using System.Linq;
using AutoScheduling3.DTOs;

namespace AutoScheduling3.ViewModels.Scheduling;

/// <summary>
/// 哨位人员管理器
/// 管理哨位与人员的临时关联关系，跟踪临时性更改
/// </summary>
public class PositionPersonnelManager
{
    // 原始数据（从数据库加载）
    private Dictionary<int, List<int>> _originalAvailablePersonnel;
    
    // 当前数据（包含临时更改）
    private Dictionary<int, List<int>> _currentAvailablePersonnel;
    
    // 临时更改记录
    private Dictionary<int, PositionPersonnelChanges> _changes;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PositionPersonnelManager()
    {
        _originalAvailablePersonnel = new Dictionary<int, List<int>>();
        _currentAvailablePersonnel = new Dictionary<int, List<int>>();
        _changes = new Dictionary<int, PositionPersonnelChanges>();
    }

    /// <summary>
    /// 初始化管理器
    /// </summary>
    /// <param name="positions">哨位列表</param>
    public void Initialize(IEnumerable<PositionDto> positions)
    {
        // 清空所有数据
        _originalAvailablePersonnel.Clear();
        _currentAvailablePersonnel.Clear();
        _changes.Clear();

        // 从PositionDto列表初始化原始数据和当前数据
        foreach (var position in positions)
        {
            // 复制原始数据
            var originalPersonnel = new List<int>(position.AvailablePersonnelIds);
            _originalAvailablePersonnel[position.Id] = originalPersonnel;

            // 复制当前数据
            var currentPersonnel = new List<int>(position.AvailablePersonnelIds);
            _currentAvailablePersonnel[position.Id] = currentPersonnel;
        }
    }

    /// <summary>
    /// 获取哨位的当前可用人员列表（包含临时更改）
    /// </summary>
    /// <param name="positionId">哨位ID</param>
    /// <returns>人员ID列表</returns>
    public List<int> GetAvailablePersonnel(int positionId)
    {
        // 如果哨位不存在，返回空列表
        if (!_currentAvailablePersonnel.ContainsKey(positionId))
        {
            return new List<int>();
        }

        // 返回当前数据的副本（包含临时更改）
        return new List<int>(_currentAvailablePersonnel[positionId]);
    }

    /// <summary>
    /// 临时添加人员到哨位
    /// </summary>
    /// <param name="positionId">哨位ID</param>
    /// <param name="personnelId">人员ID</param>
    public void AddPersonnelTemporarily(int positionId, int personnelId)
    {
        // 如果哨位不存在，跳过
        if (!_currentAvailablePersonnel.ContainsKey(positionId))
        {
            return;
        }

        // 如果人员已经在列表中，跳过
        if (_currentAvailablePersonnel[positionId].Contains(personnelId))
        {
            return;
        }

        // 添加人员到当前数据
        _currentAvailablePersonnel[positionId].Add(personnelId);

        // 更新更改记录
        if (!_changes.ContainsKey(positionId))
        {
            _changes[positionId] = new PositionPersonnelChanges
            {
                PositionId = positionId
            };
        }

        // 如果该人员在原始数据中不存在，记录为添加
        if (!_originalAvailablePersonnel[positionId].Contains(personnelId))
        {
            if (!_changes[positionId].AddedPersonnelIds.Contains(personnelId))
            {
                _changes[positionId].AddedPersonnelIds.Add(personnelId);
            }
        }
        else
        {
            // 如果该人员在原始数据中存在，从移除列表中删除（撤销移除操作）
            _changes[positionId].RemovedPersonnelIds.Remove(personnelId);
        }
    }

    /// <summary>
    /// 临时移除人员从哨位
    /// </summary>
    /// <param name="positionId">哨位ID</param>
    /// <param name="personnelId">人员ID</param>
    public void RemovePersonnelTemporarily(int positionId, int personnelId)
    {
        // 如果哨位不存在，跳过
        if (!_currentAvailablePersonnel.ContainsKey(positionId))
        {
            return;
        }

        // 如果人员不在列表中，跳过
        if (!_currentAvailablePersonnel[positionId].Contains(personnelId))
        {
            return;
        }

        // 从当前数据中移除人员
        _currentAvailablePersonnel[positionId].Remove(personnelId);

        // 更新更改记录
        if (!_changes.ContainsKey(positionId))
        {
            _changes[positionId] = new PositionPersonnelChanges
            {
                PositionId = positionId
            };
        }

        // 如果该人员在原始数据中存在，记录为移除
        if (_originalAvailablePersonnel[positionId].Contains(personnelId))
        {
            if (!_changes[positionId].RemovedPersonnelIds.Contains(personnelId))
            {
                _changes[positionId].RemovedPersonnelIds.Add(personnelId);
            }
        }
        else
        {
            // 如果该人员在原始数据中不存在，从添加列表中删除（撤销添加操作）
            _changes[positionId].AddedPersonnelIds.Remove(personnelId);
        }
    }

    /// <summary>
    /// 获取哨位的临时更改
    /// </summary>
    /// <param name="positionId">哨位ID</param>
    /// <returns>更改记录</returns>
    public PositionPersonnelChanges GetChanges(int positionId)
    {
        // 如果没有更改记录，返回空的更改对象
        if (!_changes.ContainsKey(positionId))
        {
            return new PositionPersonnelChanges
            {
                PositionId = positionId
            };
        }

        // 返回更改记录的副本
        return new PositionPersonnelChanges
        {
            PositionId = _changes[positionId].PositionId,
            PositionName = _changes[positionId].PositionName,
            AddedPersonnelIds = new List<int>(_changes[positionId].AddedPersonnelIds),
            AddedPersonnelNames = new List<string>(_changes[positionId].AddedPersonnelNames),
            RemovedPersonnelIds = new List<int>(_changes[positionId].RemovedPersonnelIds),
            RemovedPersonnelNames = new List<string>(_changes[positionId].RemovedPersonnelNames)
        };
    }

    /// <summary>
    /// 撤销哨位的所有临时更改
    /// </summary>
    /// <param name="positionId">哨位ID</param>
    public void RevertChanges(int positionId)
    {
        // 如果哨位不存在，跳过
        if (!_originalAvailablePersonnel.ContainsKey(positionId))
        {
            return;
        }

        // 恢复到原始数据
        _currentAvailablePersonnel[positionId] = new List<int>(_originalAvailablePersonnel[positionId]);

        // 清除更改记录
        _changes.Remove(positionId);
    }

    /// <summary>
    /// 撤销所有哨位的临时更改
    /// </summary>
    public void RevertAllChanges()
    {
        // 遍历所有哨位，恢复到原始数据
        foreach (var positionId in _originalAvailablePersonnel.Keys.ToList())
        {
            _currentAvailablePersonnel[positionId] = new List<int>(_originalAvailablePersonnel[positionId]);
        }

        // 清空所有更改记录
        _changes.Clear();
    }

    /// <summary>
    /// 获取所有有临时更改的哨位
    /// </summary>
    /// <returns>哨位ID列表</returns>
    public List<int> GetPositionsWithChanges()
    {
        // 返回所有有更改的哨位ID列表
        return _changes
            .Where(kvp => kvp.Value.HasChanges)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void Clear()
    {
        _originalAvailablePersonnel.Clear();
        _currentAvailablePersonnel.Clear();
        _changes.Clear();
    }
}

/// <summary>
/// 哨位人员更改记录
/// </summary>
public class PositionPersonnelChanges
{
    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public List<int> AddedPersonnelIds { get; set; } = new();
    public List<string> AddedPersonnelNames { get; set; } = new();
    public List<int> RemovedPersonnelIds { get; set; } = new();
    public List<string> RemovedPersonnelNames { get; set; } = new();
    public bool HasChanges => AddedPersonnelIds.Any() || RemovedPersonnelIds.Any();
}
