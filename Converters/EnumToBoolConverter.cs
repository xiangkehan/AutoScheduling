using Microsoft.UI.Xaml.Data;
using System;

namespace AutoScheduling3.Converters
{
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is not string enumString)
            {
                throw new ArgumentException("ExceptionEnumToBoolConverterParameterMustBeAnEnumName");
            }

            // 处理null参数（表示"全部"选项）
            if (enumString == "null")
            {
                return value == null;
            }

            // 处理null值
            if (value == null)
            {
                return false;
            }

            if (!Enum.IsDefined(value.GetType(), value))
            {
                throw new ArgumentException("ExceptionEnumToBoolConverterValueMustBeAnEnum");
            }

            var enumValue = Enum.Parse(value.GetType(), enumString);
            return enumValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (parameter is not string enumString)
            {
                throw new ArgumentException("ExceptionEnumToBoolConverterParameterMustBeAnEnumName");
            }

            // 处理null参数（表示"全部"选项）
            if (enumString == "null")
            {
                return null;
            }

            return Enum.Parse(targetType, enumString);
        }
    }
}
