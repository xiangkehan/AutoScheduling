using Microsoft.UI.Xaml.Data;
using System;

namespace AutoScheduling3.Converters
{
    /// <summary>
    /// 时段索引到时间文本转换器
    /// </summary>
    public class PeriodIndexToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int periodIndex && periodIndex >= 0 && periodIndex < 12)
            {
                var startHour = periodIndex * 2;
                var endHour = (periodIndex * 2 + 2) % 24;
                return $"{startHour:D2}:00-{endHour:D2}:00";
            }
            
            return "未知时段";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}