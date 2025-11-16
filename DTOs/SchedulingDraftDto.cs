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
}
