using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AutoScheduling3.Converters;

/// <summary>
/// 将集合数量转换为可见性
/// Count = 0 时显示（Visible），Count > 0 时隐藏（Collapsed）
/// 使用 ConverterParameter="True" 可以反转逻辑
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count)
        {
            // 检查是否需要反转逻辑
            bool invert = parameter is string str && str.Equals("True", StringComparison.OrdinalIgnoreCase);
            
            if (invert)
            {
                // 反转逻辑：当数量为0时隐藏，否则显示
                return count == 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                // 默认逻辑：当数量为0时显示空状态提示，否则隐藏
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
