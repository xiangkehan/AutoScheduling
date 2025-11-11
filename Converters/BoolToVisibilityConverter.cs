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
        var boolValue = value is bool b && b;
        if (Inverted)
        {
            boolValue = !boolValue;
        }
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var visibility = value is Visibility v && v == Visibility.Visible;
        if (Inverted)
        {
            visibility = !visibility;
        }
        return visibility;
    }
}
