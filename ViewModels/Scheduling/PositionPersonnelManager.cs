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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Debug.WriteLine("=== PositionPersonnelManager.Initialize 开始 ===");
        
        try
        {
            // 清空所有数据
            _originalAvailablePersonnel.Clear();
            _currentAvailablePersonnel.Clear();
            _changes.Clear();

            int positionCount = 0;
            int totalPersonnelCount = 0;

            // 从PositionDto列表初始化原始数据和当前数据
            foreach (var position in positions)
            {
                if (position == null)
                {
                    System.Diagnostics.Debug.WriteLine("警告: 跳过null哨位");
                    continue;
                }

                if (position.AvailablePersonnelIds == null)
                {
                    System.Diagnostics.Debug.WriteLine($"警告: 哨位 {position.Name} (ID: {position.Id}) 的AvailablePersonnelIds为null，使用空列表");
                    position.AvailablePersonnelIds = new List<int>();
                }

                // 复制原始数据
                var originalPersonnel = new List<int>(position.AvailablePersonnelIds);
                _originalAvailablePersonnel[position.Id] = originalPersonnel;

                // 复制当前数据
                var currentPersonnel = new List<int>(position.AvailablePersonnelIds);
                _currentAvailablePersonnel[position.Id] = currentPersonnel;

                positionCount++;
                totalPersonnelCount += originalPersonnel.Count;
            }

            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine($"初始化完成: {positionCount}个哨位, 总计{totalPersonnelCount}个人员关联");
            System.Diagnostics.Debug.WriteLine($"耗时: {stopwatch.ElapsedMilliseconds}ms");
            System.Diagnostics.Debug.WriteLine("=== PositionPersonnelManager.Initialize 完成 ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("=== PositionPersonnelManager.Initialize 失败 ===");
            System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 获取哨位的当前可用人员列表（包含临时更改）
    /// </summary>
    /// <param name="positionId">哨位ID</param>
    /// <returns>人员ID列表</returns>
    public List<int> GetAvailablePersonnel(int positionId)
    {
        try
        {
            // 如果哨位不存在，返回空列表
            if (!_currentAvailablePersonnel.ContainsKey(positionId))
            {
                System.Diagnostics.Debug.WriteLine($"GetAvailablePersonnel: 哨位 {positionId} 不存在，返回空列表");
                return new List<int>();
            }

            // 返回当前数据的副本（包含临时更改）
            var result = new List<int>(_currentAvailablePersonnel[positionId]);
            //System.Diagnostics.Debug.WriteLine($"GetAvailablePersonnel: 哨位 {positionId} 返回 {result.Count} 个人员");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetAvailablePersonnel失败: 哨位ID={positionId}, 错误={ex.Message}");
            return new List<int>();
        }
    }

    /// <summary>
    /// 临时添加人员到哨位
    /// </summary>
    /// <param name="positionId">哨位ID</param>
    /// <param name="personnelId">人员ID</param>
    public void AddPersonnelTemporarily(int positionId, int personnelId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Debug.WriteLine($"AddPersonnelTemporarily: 哨位ID={positionId}, 人员ID={personnelId}");
        
        try
        {
            // 如果哨位不存在，跳过
            if (!_currentAvailablePersonnel.ContainsKey(positionId))
            {
                System.Diagnostics.Debug.WriteLine($"警告: 哨位 {positionId} 不存在");
                return;
            }

            // 使用HashSet提高Contains性能
            var currentSet = new HashSet<int>(_currentAvailablePersonnel[positionId]);
            
            // 如果人员已经在列表中，跳过
            if (currentSet.Contains(personnelId))
            {
                System.Diagnostics.Debug.WriteLine($"人员 {personnelId} 已在哨位 {positionId} 的列表中");
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

            var originalSet = new HashSet<int>(_originalAvailablePersonnel[positionId]);
            
            // 如果该人员在原始数据中不存在，记录为添加
            if (!originalSet.Contains(personnelId))
            {
                if (!_changes[positionId].AddedPersonnelIds.Contains(personnelId))
                {
                    _changes[positionId].AddedPersonnelIds.Add(personnelId);
                    System.Diagnostics.Debug.WriteLine($"记录为新添加: 人员 {personnelId} 到哨位 {positionId}");
                }
            }
            else
            {
                // 如果该人员在原始数据中存在，从移除列表中删除（撤销移除操作）
                _changes[positionId].RemovedPersonnelIds.Remove(personnelId);
                System.Diagnostics.Debug.WriteLine($"撤销移除操作: 人员 {personnelId} 从哨位 {positionId}");
            }
            
            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine($"AddPersonnelTemporarily完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
            
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ 性能警告: AddPersonnelTemporarily耗时{stopwatch.ElapsedMilliseconds}ms，超过100ms");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AddPersonnelTemporarily失败: 哨位ID={positionId}, 人员ID={personnelId}");
            System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 临时移除人员从哨位
    /// </summary>
    /// <param name="positionId">哨位ID</param>
    /// <param name="personnelId">人员ID</param>
    public void RemovePersonnelTemporarily(int positionId, int personnelId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Debug.WriteLine($"RemovePersonnelTemporarily: 哨位ID={positionId}, 人员ID={personnelId}");
        
        try
        {
            // 如果哨位不存在，跳过
            if (!_currentAvailablePersonnel.ContainsKey(positionId))
            {
                System.Diagnostics.Debug.WriteLine($"警告: 哨位 {positionId} 不存在");
                return;
            }

            // 使用HashSet提高Contains性能
            var currentSet = new HashSet<int>(_currentAvailablePersonnel[positionId]);
            
            // 如果人员不在列表中，跳过
            if (!currentSet.Contains(personnelId))
            {
                System.Diagnostics.Debug.WriteLine($"人员 {personnelId} 不在哨位 {positionId} 的列表中");
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

            var originalSet = new HashSet<int>(_originalAvailablePersonnel[positionId]);
            
            // 如果该人员在原始数据中存在，记录为移除
            if (originalSet.Contains(personnelId))
            {
                if (!_changes[positionId].RemovedPersonnelIds.Contains(personnelId))
                {
                    _changes[positionId].RemovedPersonnelIds.Add(personnelId);
                    System.Diagnostics.Debug.WriteLine($"记录为移除: 人员 {personnelId} 从哨位 {positionId}");
                }
            }
            else
            {
                // 如果该人员在原始数据中不存在，从添加列表中删除（撤销添加操作）
                _changes[positionId].AddedPersonnelIds.Remove(personnelId);
                System.Diagnostics.Debug.WriteLine($"撤销添加操作: 人员 {personnelId} 从哨位 {positionId}");
            }
            
            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine($"RemovePersonnelTemporarily完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
            
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ 性能警告: RemovePersonnelTemporarily耗时{stopwatch.ElapsedMilliseconds}ms，超过100ms");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RemovePersonnelTemporarily失败: 哨位ID={positionId}, 人员ID={personnelId}");
            System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            throw;
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
        System.Diagnostics.Debug.WriteLine($"RevertChanges: 哨位ID={positionId}");
        
        try
        {
            // 如果哨位不存在，跳过
            if (!_originalAvailablePersonnel.ContainsKey(positionId))
            {
                System.Diagnostics.Debug.WriteLine($"警告: 哨位 {positionId} 不存在");
                return;
            }

            // 记录撤销前的更改数量
            var hadChanges = _changes.ContainsKey(positionId);
            if (hadChanges)
            {
                var changes = _changes[positionId];
                System.Diagnostics.Debug.WriteLine($"撤销更改: 添加 {changes.AddedPersonnelIds.Count} 个, 移除 {changes.RemovedPersonnelIds.Count} 个");
            }

            // 恢复到原始数据
            _currentAvailablePersonnel[positionId] = new List<int>(_originalAvailablePersonnel[positionId]);

            // 清除更改记录
            _changes.Remove(positionId);
            
            System.Diagnostics.Debug.WriteLine($"RevertChanges完成: 哨位 {positionId} 已恢复到原始状态");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RevertChanges失败: 哨位ID={positionId}");
            System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 撤销所有哨位的临时更改
    /// </summary>
    public void RevertAllChanges()
    {
        System.Diagnostics.Debug.WriteLine("=== RevertAllChanges 开始 ===");
        
        try
        {
            var changesCount = _changes.Count;
            System.Diagnostics.Debug.WriteLine($"撤销 {changesCount} 个哨位的更改");

            // 遍历所有哨位，恢复到原始数据
            foreach (var positionId in _originalAvailablePersonnel.Keys.ToList())
            {
                _currentAvailablePersonnel[positionId] = new List<int>(_originalAvailablePersonnel[positionId]);
            }

            // 清空所有更改记录
            _changes.Clear();
            
            System.Diagnostics.Debug.WriteLine("RevertAllChanges完成: 所有哨位已恢复到原始状态");
            System.Diagnostics.Debug.WriteLine("=== RevertAllChanges 完成 ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("=== RevertAllChanges 失败 ===");
            System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 获取所有有临时更改的哨位
    /// </summary>
    /// <returns>哨位ID列表</returns>
    public List<int> GetPositionsWithChanges()
    {
        try
        {
            // 返回所有有更改的哨位ID列表
            var result = _changes
                .Where(kvp => kvp.Value.HasChanges)
                .Select(kvp => kvp.Key)
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"GetPositionsWithChanges: 返回 {result.Count} 个有更改的哨位");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetPositionsWithChanges失败: {ex.Message}");
            return new List<int>();
        }
    }

    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void Clear()
    {
        System.Diagnostics.Debug.WriteLine("=== PositionPersonnelManager.Clear 开始 ===");
        
        try
        {
            var positionCount = _originalAvailablePersonnel.Count;
            var changesCount = _changes.Count;
            
            _originalAvailablePersonnel.Clear();
            _currentAvailablePersonnel.Clear();
            _changes.Clear();
            
            System.Diagnostics.Debug.WriteLine($"清空完成: {positionCount}个哨位, {changesCount}个更改记录");
            System.Diagnostics.Debug.WriteLine("=== PositionPersonnelManager.Clear 完成 ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("=== PositionPersonnelManager.Clear 失败 ===");
            System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"错误消息: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            throw;
        }
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
