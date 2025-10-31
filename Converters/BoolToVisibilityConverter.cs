using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AutoScheduling3.Converters;

/// <summary>
/// 布尔值到可见性的转换器
/// 支持通过 Inverted 属性或 "Inverse" 参数反转逻辑。
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    // 新增：供 XAML直接设置的反转属性
    public bool Inverted { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            bool inverseByParam = parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase);
            bool inverse = Inverted || inverseByParam;
            return inverse
                ? (boolValue ? Visibility.Collapsed : Visibility.Visible)
                : (boolValue ? Visibility.Visible : Visibility.Collapsed);
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            bool inverseByParam = parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase);
            bool inverse = Inverted || inverseByParam;
            return inverse
                ? visibility == Visibility.Collapsed
                : visibility == Visibility.Visible;
        }
        return false;
    }
}
