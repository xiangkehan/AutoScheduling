using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;

namespace AutoScheduling3.Converters
{
    /// <summary>
    /// 冲突类型到颜色转换器
    /// </summary>
    public class ConflictTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string conflictType)
            {
                return conflictType switch
                {
                    "hard" => new SolidColorBrush(Colors.Red),
                    "soft" => new SolidColorBrush(Colors.Orange),
                    "unassigned" => new SolidColorBrush(Colors.Gray),
                    "info" => new SolidColorBrush(Colors.Blue),
                    _ => new SolidColorBrush(Colors.Yellow)
                };
            }
            
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}