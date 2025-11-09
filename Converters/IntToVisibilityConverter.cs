using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AutoScheduling3.Converters
{
    /// <summary>
    /// Converts an integer value to a Visibility value.
    /// Supports comparison operators: =, >, <, >=, <=
    /// Examples: "0" (equals 0), ">0" (greater than 0), "<=5" (less than or equal to 5)
    /// </summary>
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not int intValue || parameter is not string stringParam || string.IsNullOrWhiteSpace(stringParam))
            {
                return Visibility.Collapsed;
            }

            // Parse the parameter to extract operator and value
            string trimmedParam = stringParam.Trim();
            bool result = false;

            if (trimmedParam.StartsWith(">="))
            {
                if (int.TryParse(trimmedParam.Substring(2).Trim(), out int targetValue))
                {
                    result = intValue >= targetValue;
                }
            }
            else if (trimmedParam.StartsWith("<="))
            {
                if (int.TryParse(trimmedParam.Substring(2).Trim(), out int targetValue))
                {
                    result = intValue <= targetValue;
                }
            }
            else if (trimmedParam.StartsWith(">"))
            {
                if (int.TryParse(trimmedParam.Substring(1).Trim(), out int targetValue))
                {
                    result = intValue > targetValue;
                }
            }
            else if (trimmedParam.StartsWith("<"))
            {
                if (int.TryParse(trimmedParam.Substring(1).Trim(), out int targetValue))
                {
                    result = intValue < targetValue;
                }
            }
            else
            {
                // Default: exact match
                if (int.TryParse(trimmedParam, out int targetValue))
                {
                    result = intValue == targetValue;
                }
            }

            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
