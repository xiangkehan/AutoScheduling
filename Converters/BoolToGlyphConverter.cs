using Microsoft.UI.Xaml.Data;
using System;

namespace AutoScheduling3.Converters
{
    /// <summary>
    /// 根据布尔值返回不同字形的转换器
    /// </summary>
    public class BoolToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue && parameter is string glyphs)
            {
                var parts = glyphs.Split('|');
                if (parts.Length == 2)
                {
                    return boolValue ? parts[0] : parts[1];
                }
            }
            return "●";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
