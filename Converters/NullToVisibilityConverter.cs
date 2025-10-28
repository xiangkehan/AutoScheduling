using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AutoScheduling3.Converters;

/// <summary>
/// Null值到可见性的转换器
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // 如果参数是 "Inverse"，则反转逻辑
        bool inverse = parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase);

        bool isNull = value == null;

        if (inverse)
        {
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
