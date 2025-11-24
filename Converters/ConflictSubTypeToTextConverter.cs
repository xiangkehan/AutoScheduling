using AutoScheduling3.DTOs;
using Microsoft.UI.Xaml.Data;
using System;

namespace AutoScheduling3.Converters;

/// <summary>
/// 冲突子类型到中文文本的转换器
/// </summary>
public class ConflictSubTypeToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ConflictSubType subType)
        {
            return subType switch
            {
                ConflictSubType.SkillMismatch => "技能不匹配",
                ConflictSubType.PersonnelUnavailable => "人员不可用",
                ConflictSubType.DuplicateAssignment => "重复分配",
                ConflictSubType.InsufficientRest => "休息时间不足",
                ConflictSubType.ExcessiveWorkload => "工作量过大",
                ConflictSubType.WorkloadImbalance => "工作量不均衡",
                ConflictSubType.ConsecutiveOvertime => "连续工作超时",
                ConflictSubType.UnassignedSlot => "未分配时段",
                ConflictSubType.SuboptimalAssignment => "次优分配",
                _ => "未知类型"
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
