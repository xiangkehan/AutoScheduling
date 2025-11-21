using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AutoScheduling3.DTOs;

/// <summary>
/// 排班创建草稿数据传输对象
/// 用于保存和恢复排班创建过程中的进度
/// </summary>
public class SchedulingDraftDto
{
    /// <summary>
    /// 排班标题
    /// </summary>
    [JsonPropertyName("scheduleTitle")]
    public string ScheduleTitle { get; set; } = string.Empty;

    /// <summary>
    /// 开始日期
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 当前步骤（1-5）
    /// </summary>
    [JsonPropertyName("currentStep")]
    public int CurrentStep { get; set; } = 1;

    /// <summary>
    /// 是否应用了模板
    /// </summary>
    [JsonPropertyName("templateApplied")]
    public bool TemplateApplied { get; set; }

    /// <summary>
    /// 加载的模板ID（如果使用模板）
    /// </summary>
    [JsonPropertyName("loadedTemplateId")]
    public int? LoadedTemplateId { get; set; }

    /// <summary>
    /// 选中的人员ID列表
    /// </summary>
    [JsonPropertyName("selectedPersonnelIds")]
    public List<int> SelectedPersonnelIds { get; set; } = new();

    /// <summary>
    /// 选中的岗位ID列表
    /// </summary>
    [JsonPropertyName("selectedPositionIds")]
    public List<int> SelectedPositionIds { get; set; } = new();

    /// <summary>
    /// 是否使用活动休息日配置
    /// </summary>
    [JsonPropertyName("useActiveHolidayConfig")]
    public bool UseActiveHolidayConfig { get; set; }

    /// <summary>
    /// 选中的休息日配置ID
    /// </summary>
    [JsonPropertyName("selectedHolidayConfigId")]
    public int? SelectedHolidayConfigId { get; set; }

    /// <summary>
    /// 启用的固定规则ID列表
    /// </summary>
    [JsonPropertyName("enabledFixedRuleIds")]
    public List<int> EnabledFixedRuleIds { get; set; } = new();

    /// <summary>
    /// 启用的手动指定ID列表（已保存的）
    /// </summary>
    [JsonPropertyName("enabledManualAssignmentIds")]
    public List<int> EnabledManualAssignmentIds { get; set; } = new();

    /// <summary>
    /// 临时手动指定列表（未保存到数据库的）
    /// </summary>
    [JsonPropertyName("temporaryManualAssignments")]
    public List<ManualAssignmentDraftDto> TemporaryManualAssignments { get; set; } = new();

    /// <summary>
    /// 草稿保存时间
    /// </summary>
    [JsonPropertyName("savedAt")]
    public DateTime SavedAt { get; set; }

    /// <summary>
    /// 草稿版本（用于版本兼容性检查）
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 哨位人员临时更改记录（字典：哨位ID -> 更改记录）
    /// </summary>
    [JsonPropertyName("positionPersonnelChanges")]
    public Dictionary<int, PositionPersonnelChangeDto> PositionPersonnelChanges { get; set; } = new();

    /// <summary>
    /// 手动添加的人员ID列表（不属于任何哨位）
    /// </summary>
    [JsonPropertyName("manuallyAddedPersonnelIds")]
    public List<int> ManuallyAddedPersonnelIds { get; set; } = new();
}

/// <summary>
/// 哨位人员更改记录DTO（用于草稿保存）
/// </summary>
public class PositionPersonnelChangeDto
{
    /// <summary>
    /// 哨位ID
    /// </summary>
    [JsonPropertyName("positionId")]
    public int PositionId { get; set; }

    /// <summary>
    /// 添加的人员ID列表
    /// </summary>
    [JsonPropertyName("addedPersonnelIds")]
    public List<int> AddedPersonnelIds { get; set; } = new();

    /// <summary>
    /// 移除的人员ID列表
    /// </summary>
    [JsonPropertyName("removedPersonnelIds")]
    public List<int> RemovedPersonnelIds { get; set; } = new();
}
