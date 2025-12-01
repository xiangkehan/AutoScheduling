using Microsoft.UI.Xaml.Data;
using System;

namespace AutoScheduling3.Converters
{
    /// <summary>
    /// 将数字加 1 的转换器（用于将索引转换为显示位置）
    /// </summary>
    public class AddOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int intValue)
            {
                // 返回字符串类型，因为通常用于 TextBlock 或 Run 的 Text 属性
                return (intValue + 1).ToString();
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
