using AutoScheduling3.DTOs;
using AutoScheduling3.Services.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace AutoScheduling3.Services;

/// <summary>
/// 排班创建草稿服务实现 - 管理排班创建进度的保存和恢复
/// 
/// 使用 ApplicationData.Current.LocalSettings 进行草稿存储。
/// 
/// 为什么使用 LocalSettings：
/// 1. 轻量级存储：适合存储小型配置和状态数据（草稿通常 < 50KB）
/// 2. 快速访问：LocalSettings 是键值对存储，读写速度快
/// 3. 自动管理：Windows 系统自动管理备份和同步
/// 4. 简单API：无需处理文件路径和文件系统操作
/// 
/// 草稿存储策略：
/// - 使用单个键存储草稿JSON字符串
/// - 使用单独的键存储草稿时间戳（用于过期检查）
/// - 草稿过期时间：7天
/// 
/// 需求: 1.2, 1.3, 3.3, 5.3
/// </summary>
public class SchedulingDraftService : ISchedulingDraftService
{
    private const string DraftKey = "SchedulingCreationDraft";
    private const string DraftTimestampKey = "SchedulingCreationDraftTimestamp";
    private const int ExpirationDays = 7;
    private const string CurrentVersion = "1.0";

    private readonly ApplicationDataContainer _localSettings;

    /// <summary>
    /// 初始化排班草稿服务
    /// </summary>
    public SchedulingDraftService()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
        System.Diagnostics.Debug.WriteLine("[SchedulingDraftService] Initialized with LocalSettings storage");
    }

    public Task InitializeAsync()
    {
        System.Diagnostics.Debug.WriteLine("[SchedulingDraftService] Service initialized");
        return Task.CompletedTask;
    }

    public Task CleanupAsync()
    {
        System.Diagnostics.Debug.WriteLine("[SchedulingDraftService] Service cleanup completed");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 保存排班创建草稿
    /// 
    /// 保存流程：
    /// 1. 设置草稿版本和保存时间
    /// 2. 序列化为JSON字符串
    /// 3. 保存到LocalSettings
    /// 4. 保存时间戳用于过期检查
    /// 
    /// 错误处理：
    /// - 捕获序列化错误并记录日志
    /// - 捕获存储错误并记录日志
    /// 
    /// 需求: 1.2
    /// </summary>
    /// <param name="draft">草稿数据</param>
    public async Task SaveDraftAsync(SchedulingDraftDto draft)
    {
        try
        {
            // 设置元数据
            draft.SavedAt = DateTime.UtcNow;
            draft.Version = CurrentVersion;

            // 序列化为JSON
            var json = JsonSerializer.Serialize(draft, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // 保存到LocalSettings
            _localSettings.Values[DraftKey] = json;
            _localSettings.Values[DraftTimestampKey] = DateTime.UtcNow.Ticks;

            System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] Draft saved successfully at {draft.SavedAt}");
            await Task.CompletedTask;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] JSON序列化失败: {ex.Message}");
            throw new InvalidOperationException("无法序列化草稿数据", ex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] 保存草稿失败: {ex.Message}");
            throw new InvalidOperationException("保存草稿时发生错误", ex);
        }
    }

    /// <summary>
    /// 加载最近的排班创建草稿
    /// 
    /// 加载流程：
    /// 1. 检查草稿是否存在
    /// 2. 检查草稿是否过期
    /// 3. 读取JSON字符串
    /// 4. 反序列化为SchedulingDraftDto
    /// 5. 验证版本兼容性
    /// 
    /// 错误处理：
    /// - 如果草稿不存在，返回null
    /// - 如果草稿过期，删除并返回null
    /// - 如果反序列化失败，删除损坏的草稿并抛出异常（带详细错误信息）
    /// - 如果版本不兼容，删除并返回null
    /// 
    /// 需求: 1.3, 5.3, 5.4
    /// </summary>
    /// <returns>草稿数据，如果不存在或无效则返回null</returns>
    /// <exception cref="InvalidOperationException">当草稿数据损坏时抛出</exception>
    public async Task<SchedulingDraftDto?> LoadDraftAsync()
    {
        try
        {
            // 检查草稿是否存在
            if (!_localSettings.Values.ContainsKey(DraftKey))
            {
                System.Diagnostics.Debug.WriteLine("[SchedulingDraftService] No draft found");
                return null;
            }

            // 检查草稿是否过期
            if (_localSettings.Values.ContainsKey(DraftTimestampKey))
            {
                var timestampTicks = (long)_localSettings.Values[DraftTimestampKey];
                var savedTime = new DateTime(timestampTicks, DateTimeKind.Utc);
                var age = DateTime.UtcNow - savedTime;

                if (age.TotalDays > ExpirationDays)
                {
                    System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] Draft expired (age: {age.TotalDays:F1} days), deleting");
                    await DeleteDraftAsync();
                    return null;
                }
            }

            // 读取并反序列化
            var json = _localSettings.Values[DraftKey] as string;
            if (string.IsNullOrEmpty(json))
            {
                System.Diagnostics.Debug.WriteLine("[SchedulingDraftService] Draft data is empty, deleting");
                await DeleteDraftAsync();
                return null;
            }

            SchedulingDraftDto? draft = null;
            try
            {
                draft = JsonSerializer.Deserialize<SchedulingDraftDto>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] JSON反序列化失败: {jsonEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] JSON内容预览: {(json.Length > 200 ? json.Substring(0, 200) + "..." : json)}");
                
                // 删除损坏的草稿
                await DeleteDraftAsync();
                
                // 抛出更详细的异常，供上层处理
                throw new InvalidOperationException("草稿数据已损坏，无法解析。草稿已被删除。", jsonEx);
            }

            if (draft == null)
            {
                System.Diagnostics.Debug.WriteLine("[SchedulingDraftService] Deserialized draft is null, deleting");
                await DeleteDraftAsync();
                throw new InvalidOperationException("草稿数据格式错误，无法恢复。草稿已被删除。");
            }

            // 验证版本兼容性
            if (draft.Version != CurrentVersion)
            {
                System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] Draft version mismatch (expected: {CurrentVersion}, actual: {draft.Version})");
                await DeleteDraftAsync();
                return null;
            }

            // 验证必要字段
            if (draft.SelectedPersonnelIds == null || draft.SelectedPositionIds == null || 
                draft.EnabledFixedRuleIds == null || draft.EnabledManualAssignmentIds == null ||
                draft.TemporaryManualAssignments == null)
            {
                System.Diagnostics.Debug.WriteLine("[SchedulingDraftService] Draft has null required fields, deleting");
                await DeleteDraftAsync();
                throw new InvalidOperationException("草稿数据不完整，缺少必要字段。草稿已被删除。");
            }

            System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] Draft loaded successfully (saved at: {draft.SavedAt})");
            return draft;
        }
        catch (InvalidOperationException)
        {
            // 重新抛出我们自己的异常
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] 加载草稿失败（未预期的错误）: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] 堆栈跟踪: {ex.StackTrace}");
            
            // 删除可能损坏的草稿
            await DeleteDraftAsync();
            
            // 抛出包装后的异常
            throw new InvalidOperationException("加载草稿时发生未预期的错误。草稿已被删除。", ex);
        }
    }

    /// <summary>
    /// 删除排班创建草稿
    /// 
    /// 删除流程：
    /// 1. 从LocalSettings移除草稿数据
    /// 2. 从LocalSettings移除时间戳
    /// 
    /// 需求: 3.1
    /// </summary>
    public Task DeleteDraftAsync()
    {
        try
        {
            if (_localSettings.Values.ContainsKey(DraftKey))
            {
                _localSettings.Values.Remove(DraftKey);
                System.Diagnostics.Debug.WriteLine("[SchedulingDraftService] Draft data removed");
            }

            if (_localSettings.Values.ContainsKey(DraftTimestampKey))
            {
                _localSettings.Values.Remove(DraftTimestampKey);
                System.Diagnostics.Debug.WriteLine("[SchedulingDraftService] Draft timestamp removed");
            }

            System.Diagnostics.Debug.WriteLine("[SchedulingDraftService] Draft deleted successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] 删除草稿失败: {ex.Message}");
            // 不抛出异常，因为删除失败不应该阻止应用程序继续运行
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 检查是否存在草稿
    /// 
    /// 检查流程：
    /// 1. 检查LocalSettings中是否存在草稿键
    /// 2. 检查草稿是否过期
    /// 
    /// 需求: 1.3
    /// </summary>
    /// <returns>是否存在有效草稿</returns>
    public Task<bool> HasDraftAsync()
    {
        try
        {
            // 检查草稿是否存在
            if (!_localSettings.Values.ContainsKey(DraftKey))
            {
                return Task.FromResult(false);
            }

            // 检查草稿是否过期
            if (_localSettings.Values.ContainsKey(DraftTimestampKey))
            {
                var timestampTicks = (long)_localSettings.Values[DraftTimestampKey];
                var savedTime = new DateTime(timestampTicks, DateTimeKind.Utc);
                var age = DateTime.UtcNow - savedTime;

                if (age.TotalDays > ExpirationDays)
                {
                    System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] Draft expired (age: {age.TotalDays:F1} days)");
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] 检查草稿存在性失败: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 清理过期草稿（超过7天）
    /// 
    /// 清理流程：
    /// 1. 检查草稿是否存在
    /// 2. 检查草稿是否过期
    /// 3. 如果过期，删除草稿
    /// 
    /// 此方法应在应用启动时调用，用于清理旧的草稿数据。
    /// 
    /// 需求: 3.3
    /// </summary>
    public async Task CleanupExpiredDraftsAsync()
    {
        try
        {
            if (!_localSettings.Values.ContainsKey(DraftKey))
            {
                System.Diagnostics.Debug.WriteLine("[SchedulingDraftService] No draft to cleanup");
                return;
            }

            if (_localSettings.Values.ContainsKey(DraftTimestampKey))
            {
                var timestampTicks = (long)_localSettings.Values[DraftTimestampKey];
                var savedTime = new DateTime(timestampTicks, DateTimeKind.Utc);
                var age = DateTime.UtcNow - savedTime;

                if (age.TotalDays > ExpirationDays)
                {
                    System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] Cleaning up expired draft (age: {age.TotalDays:F1} days)");
                    await DeleteDraftAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] Draft is not expired (age: {age.TotalDays:F1} days)");
                }
            }
            else
            {
                // 如果没有时间戳，删除草稿（数据不完整）
                System.Diagnostics.Debug.WriteLine("[SchedulingDraftService] Draft has no timestamp, deleting");
                await DeleteDraftAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SchedulingDraftService] 清理过期草稿失败: {ex.Message}");
            // 不抛出异常，因为清理失败不应该阻止应用程序启动
        }
    }
}
