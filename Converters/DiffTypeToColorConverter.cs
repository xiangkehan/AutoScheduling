using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace AutoScheduling3.Converters
{
 public class DiffTypeToColorConverter : IValueConverter
 {
 public object Convert(object value, Type targetType, object parameter, string language)
 {
 var diff = value as string ?? string.Empty;
 return diff switch
 {
 "新增班次" => Windows.UI.Color.FromArgb(255,223,246,221), // greenish
 "删除班次" => Windows.UI.Color.FromArgb(255,253,231,233), // reddish
 "人员变更" => Windows.UI.Color.FromArgb(255,255,244,206), // yellow
 _ => Windows.UI.Color.FromArgb(255,238,238,238)
 };
 }

 public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
 }
}
