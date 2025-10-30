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
        bool inverse = parameter is string param && param.Equals("invert", StringComparison.OrdinalIgnoreCase);

        bool isNullOrEmpty;
        if (value is System.Collections.ICollection collection)
        {
            isNullOrEmpty = collection.Count == 0;
        }
        else
        {
            isNullOrEmpty = value == null;
        }

        if (inverse)
        {
            return isNullOrEmpty ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            return isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
