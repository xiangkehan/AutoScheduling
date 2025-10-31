using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AutoScheduling3.Converters
{
 /// <summary>
 /// 根据枚举值与参数是否相等返回 Visibility。
 /// 用法：Visibility="{Binding Mode, Converter={StaticResource EnumEqualsToVisibilityConverter}, ConverterParameter=SideBySide}"。
 /// </summary>
 public class EnumEqualsToVisibilityConverter : IValueConverter
 {
 public object Convert(object value, Type targetType, object parameter, string language)
 {
 if (value == null || parameter == null) return Visibility.Collapsed;
 var valueString = value.ToString();
 var paramString = parameter.ToString();
 return string.Equals(valueString, paramString, StringComparison.OrdinalIgnoreCase)
 ? Visibility.Visible
 : Visibility.Collapsed;
 }

 public object ConvertBack(object value, Type targetType, object parameter, string language)
 {
 throw new NotSupportedException();
 }
 }
}
