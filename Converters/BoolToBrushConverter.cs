using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace AutoScheduling3.Converters
{
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not bool boolValue)
                return new SolidColorBrush(Colors.Transparent);

            string[] brushes = parameter as string[] ?? ((string)parameter)?.Split('|');
            if (brushes == null || brushes.Length < 2)
            {
                return boolValue ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            }

            var trueBrush = GetBrushFromString(brushes[0]);
            var falseBrush = GetBrushFromString(brushes[1]);

            return boolValue ? trueBrush : falseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private Brush GetBrushFromString(string colorString)
        {
            return colorString.ToLower() switch
            {
                "green" => new SolidColorBrush(Colors.Green),
                "red" => new SolidColorBrush(Colors.Red),
                "gray" => new SolidColorBrush(Colors.Gray),
                "orange" => new SolidColorBrush(Colors.Orange),
                "accent" => GetAccentBrush(),
                _ => new SolidColorBrush(Colors.Transparent),
            };
        }

        private Brush GetAccentBrush()
        {
            // 从应用资源获取系统强调色
            if (Application.Current.Resources.TryGetValue("AccentFillColorDefaultBrush", out var brush) && brush is Brush accentBrush)
            {
                return accentBrush;
            }
            // 回退到默认蓝色
            return new SolidColorBrush(Colors.DodgerBlue);
        }
    }
}
