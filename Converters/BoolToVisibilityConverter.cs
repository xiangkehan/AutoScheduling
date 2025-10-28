using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace AutoScheduling3.Converters;

/// <summary>
/// 布尔值到可见性的转换器
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            // 如果参数是 "Inverse"，则反转逻辑
            bool inverse = parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase);
            
            if (inverse)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            bool inverse = parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase);
            
            if (inverse)
            {
                return visibility == Visibility.Collapsed;
            }
            else
            {
                return visibility == Visibility.Visible;
            }
        }

        return false;
    }
}
