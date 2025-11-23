using Microsoft.UI.Xaml.Data;
using System;

namespace AutoScheduling3.Converters;

/// <summary>
/// Null值到布尔值的转换器
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool inverse = parameter is string param && param.Equals("invert", StringComparison.OrdinalIgnoreCase);

        bool isNull = value == null;

        if (inverse)
        {
            // invert: null -> true, not null -> false
            return isNull;
        }
        else
        {
            // normal: null -> false, not null -> true
            return !isNull;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
