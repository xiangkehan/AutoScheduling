using Microsoft.UI.Xaml.Data;

namespace AutoScheduling3.Converters;

/// <summary>
/// 日期时间格式转换器
/// </summary>
public class DateTimeFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dateTime)
        {
            // 默认格式
            string format = "yyyy-MM-dd HH:mm";

            // 如果提供了参数，使用参数作为格式
            if (parameter is string formatParam && !string.IsNullOrWhiteSpace(formatParam))
            {
                format = formatParam;
            }

            return dateTime.ToString(format);
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            string format = "yyyy-MM-dd HH:mm";
            if (parameter is string formatParam && !string.IsNullOrWhiteSpace(formatParam))
            {
                format = formatParam;
            }

            return dateTimeOffset.ToString(format);
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string str && DateTime.TryParse(str, out DateTime result))
        {
            return result;
        }

        return DateTime.MinValue;
    }
}
