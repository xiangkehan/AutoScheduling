using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace AutoScheduling3.Converters
{
    /// <summary>
    /// 根据布尔值返回不同颜色的转换器
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue && parameter is string colors)
            {
                var parts = colors.Split('|');
                if (parts.Length == 2)
                {
                    var colorString = boolValue ? parts[0] : parts[1];
                    return new SolidColorBrush(ParseColor(colorString));
                }
            }
            return new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)); // Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private Color ParseColor(string colorString)
        {
            // 移除 # 符号
            colorString = colorString.TrimStart('#');
            
            if (colorString.Length == 6)
            {
                // RGB 格式
                byte r = System.Convert.ToByte(colorString.Substring(0, 2), 16);
                byte g = System.Convert.ToByte(colorString.Substring(2, 2), 16);
                byte b = System.Convert.ToByte(colorString.Substring(4, 2), 16);
                return Color.FromArgb(255, r, g, b);
            }
            
            return Color.FromArgb(255, 128, 128, 128); // Gray
        }
    }
}
