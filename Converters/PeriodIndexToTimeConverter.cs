using Microsoft.UI.Xaml.Data;
using System;

namespace AutoScheduling3.Converters;

/// <summary>
/// 时段索引到时间范围的转换器
/// 将时段索引(0-11)转换为时间范围字符串
/// 例如: 0 -> "00:00-02:00", 1 -> "02:00-04:00"
/// </summary>
public class PeriodIndexToTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int periodIndex)
        {
            // 验证时段索引范围
            if (periodIndex < 0 || periodIndex > 11)
            {
                return string.Empty;
            }

            // 每个时段2小时
            int startHour = periodIndex * 2;
            int endHour = startHour + 2;

            // 格式化为 "HH:mm-HH:mm"
            return $"{startHour:D2}:00-{endHour:D2}:00";
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        // 不支持反向转换
        throw new NotImplementedException("PeriodIndexToTimeConverter does not support ConvertBack");
    }
}
