using Microsoft.UI.Xaml.Data;
using System;

namespace AutoScheduling3.Converters;

/// <summary>
/// 将数值加1的转换器（用于将0基索引转换为1基显示）
/// </summary>
public class AddOneConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int intValue)
        {
            // 返回字符串类型，因为通常用于 TextBlock 的 Text 属性
            return (intValue + 1).ToString();
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string stringValue && int.TryParse(stringValue, out int intValue))
        {
            return intValue - 1;
        }
        if (value is int intVal)
        {
            return intVal - 1;
        }
        return value;
    }
}
