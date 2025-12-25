using AutoScheduling3.DTOs.Comparison;
using Microsoft.UI.Xaml.Data;
using System;
using Windows.UI;

namespace AutoScheduling3.Converters
{
    /// <summary>
    /// 差异类型到颜色的转换器
    /// </summary>
    public class DiffTypeToColorConverter : IValueConverter
    {
        // 颜色定义（与设计文档一致）
        private static readonly Color AddedColor = Color.FromArgb(255, 223, 246, 221);    // #DFF6DD - 绿色
        private static readonly Color RemovedColor = Color.FromArgb(255, 253, 231, 233);  // #FDE7E9 - 红色
        private static readonly Color ChangedColor = Color.FromArgb(255, 255, 244, 206);  // #FFF4CE - 黄色
        private static readonly Color DefaultColor = Color.FromArgb(255, 238, 238, 238);  // #EEEEEE - 灰色

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DiffType diffType)
            {
                return diffType switch
                {
                    DiffType.Added => AddedColor,
                    DiffType.Removed => RemovedColor,
                    DiffType.Changed => ChangedColor,
                    _ => DefaultColor
                };
            }

            return DefaultColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
